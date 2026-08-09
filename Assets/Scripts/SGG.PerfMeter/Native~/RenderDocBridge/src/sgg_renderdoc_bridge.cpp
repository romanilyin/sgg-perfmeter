#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <renderdoc_app.h>

#include "sgg_renderdoc_bridge.h"

#include <cstdint>
#include <cstring>
#include <mutex>

namespace
{
constexpr uint32_t kMaxPathBytes = SGG_RD_MAX_PATH_BYTES;
constexpr uint32_t kMaxTitleBytes = SGG_RD_MAX_TITLE_BYTES;
constexpr uint32_t kMaxCommentsBytes = SGG_RD_MAX_COMMENTS_BYTES;

static_assert(sizeof(SggRdResult) == 4u, "SggRdResult must remain a 32-bit ABI value");
static_assert(sizeof(SggRdFeatureBitsV1) == 4u, "SggRdFeatureBitsV1 must remain a 32-bit ABI value");
static_assert(sizeof(SggRdCapabilitiesV1) == 72u, "V1 capabilities layout changed");
static_assert(sizeof(SggRdCaptureTokenV1) == 32u, "V1 token layout changed");
static_assert(sizeof(SggRdArtifactV1) == 32u, "V1 artifact layout changed");

struct ApiFunctions
{
    pRENDERDOC_GetAPIVersion get_api_version = nullptr;
    pRENDERDOC_SetCaptureFilePathTemplate set_capture_path_template = nullptr;
    pRENDERDOC_GetCaptureFilePathTemplate get_capture_path_template = nullptr;
    pRENDERDOC_GetNumCaptures get_num_captures = nullptr;
    pRENDERDOC_GetCapture get_capture = nullptr;
    pRENDERDOC_SetCaptureFileComments set_capture_comments = nullptr;
    pRENDERDOC_IsTargetControlConnected is_target_control_connected = nullptr;
    pRENDERDOC_StartFrameCapture start_frame_capture = nullptr;
    pRENDERDOC_IsFrameCapturing is_frame_capturing = nullptr;
    pRENDERDOC_EndFrameCapture end_frame_capture = nullptr;
    pRENDERDOC_DiscardFrameCapture discard_frame_capture = nullptr;
    pRENDERDOC_SetCaptureTitle set_capture_title = nullptr;
    pRENDERDOC_SetObjectAnnotation set_object_annotation = nullptr;
    pRENDERDOC_SetCommandAnnotation set_command_annotation = nullptr;

    uint32_t actual_major = 0u;
    uint32_t actual_minor = 0u;
    uint32_t actual_patch = 0u;
    uint32_t capability_level = 0u;
    uint32_t supports_discard = 0u;
    uint32_t supports_comments = 0u;
    uint32_t supports_title = 0u;
    uint32_t supports_annotations = 0u;
};

struct BridgeState
{
    ApiFunctions api{};
    uint32_t api_valid = 0u;

    uint32_t active = 0u;
    SggRdCaptureTokenV1 active_token{};
    uint32_t previous_template_valid = 0u;
    char previous_template[kMaxPathBytes]{};
    char active_template[kMaxPathBytes]{};
    char active_parent_directory[kMaxPathBytes]{};
    uint32_t active_parent_directory_bytes = 0u;

    uint32_t completed = 0u;
    SggRdCaptureTokenV1 completed_token{};
    char completed_parent_directory[kMaxPathBytes]{};
    uint32_t completed_parent_directory_bytes = 0u;
    uint32_t observed = 0u;
    char observed_path[kMaxPathBytes]{};
    uint32_t observed_path_bytes = 0u;
    uint32_t observed_index = 0u;
    uint64_t observed_timestamp_seconds = 0u;
    uint64_t observed_unix_ns = 0u;
};

std::mutex g_bridge_mutex;
BridgeState g_state{};

#ifdef SGG_RD_TESTING
uint32_t g_test_module_present = 0u;
pRENDERDOC_GetAPI g_test_get_api = nullptr;
#endif

struct TryLock
{
    explicit TryLock(std::mutex &mutex)
        : mutex_(mutex), owns_lock_(mutex_.try_lock())
    {
    }

    ~TryLock()
    {
        if (owns_lock_)
            mutex_.unlock();
    }

    TryLock(const TryLock &) = delete;
    TryLock &operator=(const TryLock &) = delete;

    bool owns_lock() const { return owns_lock_; }

private:
    std::mutex &mutex_;
    bool owns_lock_;
};

enum class ResolveResult : uint32_t
{
    ModuleMissing,
    ExportMissing,
    Ready
};

struct ResolvedExport
{
    ResolveResult result = ResolveResult::ModuleMissing;
    pRENDERDOC_GetAPI get_api = nullptr;
};

struct Candidate
{
    uint32_t index = 0u;
    uint64_t timestamp_seconds = 0u;
    uint64_t observed_unix_ns = 0u;
    uint32_t path_bytes = 0u;
    char path[kMaxPathBytes]{};
};

uint32_t VersionLevel(int major, int minor, int patch)
{
    if (major != 1 || minor < 0 || minor > 99 || patch < 0 || patch > 99)
        return 0u;

    return static_cast<uint32_t>(major * 10000 + minor * 100 + patch);
}

bool IsContinuationByte(uint8_t value)
{
    return value >= 0x80u && value <= 0xBFu;
}

bool IsValidUtf8(const char *data, uint32_t bytes)
{
    if (bytes != 0u && data == nullptr)
        return false;

    uint32_t offset = 0u;
    while (offset < bytes)
    {
        const uint8_t first = static_cast<uint8_t>(data[offset]);
        if (first == 0u)
            return false;

        if (first <= 0x7Fu)
        {
            ++offset;
            continue;
        }

        if (first >= 0xC2u && first <= 0xDFu)
        {
            if (offset + 1u >= bytes ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 1u])))
                return false;
            offset += 2u;
            continue;
        }

        if (first == 0xE0u)
        {
            if (offset + 2u >= bytes ||
                static_cast<uint8_t>(data[offset + 1u]) < 0xA0u ||
                static_cast<uint8_t>(data[offset + 1u]) > 0xBFu ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])))
                return false;
            offset += 3u;
            continue;
        }

        if ((first >= 0xE1u && first <= 0xECu) || (first >= 0xEEu && first <= 0xEFu))
        {
            if (offset + 2u >= bytes ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 1u])) ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])))
                return false;
            offset += 3u;
            continue;
        }

        if (first == 0xEDu)
        {
            if (offset + 2u >= bytes ||
                static_cast<uint8_t>(data[offset + 1u]) < 0x80u ||
                static_cast<uint8_t>(data[offset + 1u]) > 0x9Fu ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])))
                return false;
            offset += 3u;
            continue;
        }

        if (first == 0xF0u)
        {
            if (offset + 3u >= bytes ||
                static_cast<uint8_t>(data[offset + 1u]) < 0x90u ||
                static_cast<uint8_t>(data[offset + 1u]) > 0xBFu ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])) ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 3u])))
                return false;
            offset += 4u;
            continue;
        }

        if (first >= 0xF1u && first <= 0xF3u)
        {
            if (offset + 3u >= bytes ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 1u])) ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])) ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 3u])))
                return false;
            offset += 4u;
            continue;
        }

        if (first == 0xF4u)
        {
            if (offset + 3u >= bytes ||
                static_cast<uint8_t>(data[offset + 1u]) < 0x80u ||
                static_cast<uint8_t>(data[offset + 1u]) > 0x8Fu ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 2u])) ||
                !IsContinuationByte(static_cast<uint8_t>(data[offset + 3u])))
                return false;
            offset += 4u;
            continue;
        }

        return false;
    }

    return true;
}

bool CopyUtf8(const char *data, uint32_t bytes, char *destination, uint32_t capacity)
{
    if (destination == nullptr || bytes >= capacity || !IsValidUtf8(data, bytes))
        return false;

    if (bytes != 0u)
        std::memcpy(destination, data, bytes);
    destination[bytes] = '\0';
    return true;
}

bool CopyOptionalUtf8(const char *data, uint32_t bytes, char *destination, uint32_t capacity)
{
    if (data == nullptr && bytes != 0u)
        return false;
    return CopyUtf8(data == nullptr ? "" : data, bytes, destination, capacity);
}

bool BoundedStringLength(const char *data, uint32_t capacity, uint32_t *out_bytes)
{
    if (data == nullptr || out_bytes == nullptr)
        return false;

    for (uint32_t index = 0u; index < capacity; ++index)
    {
        if (data[index] == '\0')
        {
            *out_bytes = index;
            return true;
        }
    }

    return false;
}

uint64_t NowUnixNanoseconds()
{
    FILETIME file_time{};
    GetSystemTimeAsFileTime(&file_time);

    ULARGE_INTEGER value{};
    value.LowPart = file_time.dwLowDateTime;
    value.HighPart = file_time.dwHighDateTime;

    constexpr uint64_t kWindowsEpoch100ns = 116444736000000000ULL;
    if (value.QuadPart < kWindowsEpoch100ns)
        return 0u;

    return (value.QuadPart - kWindowsEpoch100ns) * 100u;
}

bool IsPathSeparator(char value)
{
    return value == '\\' || value == '/';
}

char FoldPathCharacter(char value)
{
    if (value == '/')
        return '\\';
    if (value >= 'A' && value <= 'Z')
        return static_cast<char>(value - 'A' + 'a');
    return value;
}

bool PathPrefixEquals(const char *path, uint32_t path_bytes, const char *prefix, uint32_t prefix_bytes)
{
    if (path == nullptr || prefix == nullptr || path_bytes < prefix_bytes)
        return false;

    for (uint32_t index = 0u; index < prefix_bytes; ++index)
    {
        if (FoldPathCharacter(path[index]) != FoldPathCharacter(prefix[index]))
            return false;
    }
    return true;
}

bool DeriveParentDirectory(const char *capture_template, uint32_t template_bytes,
                           char *parent_directory, uint32_t *parent_bytes)
{
    if (capture_template == nullptr || parent_directory == nullptr || parent_bytes == nullptr ||
        template_bytes >= kMaxPathBytes)
        return false;

    uint32_t last_separator = template_bytes;
    for (uint32_t index = template_bytes; index > 0u; --index)
    {
        if (IsPathSeparator(capture_template[index - 1u]))
        {
            last_separator = index - 1u;
            break;
        }
    }

    if (last_separator == template_bytes)
    {
        parent_directory[0] = '\0';
        *parent_bytes = 0u;
        return true;
    }

    uint32_t length = last_separator;
    if (length == 0u)
        length = 1u;

    if (length == 2u && template_bytes >= 3u && capture_template[1u] == ':' &&
        IsPathSeparator(capture_template[2u]))
        length = 3u;

    if (length >= kMaxPathBytes)
        return false;

    if (length != 3u)
    {
        while (length > 0u && IsPathSeparator(capture_template[length - 1u]))
            --length;
        if (length == 0u)
            return false;
    }

    std::memcpy(parent_directory, capture_template, length);
    parent_directory[length] = '\0';
    *parent_bytes = length;
    return true;
}

bool IsPathInsideParent(const char *candidate_path, uint32_t candidate_bytes,
                        const char *parent_directory, uint32_t parent_bytes)
{
    if (candidate_path == nullptr || parent_directory == nullptr || parent_bytes == 0u ||
        candidate_bytes <= parent_bytes ||
        !PathPrefixEquals(candidate_path, candidate_bytes, parent_directory, parent_bytes))
        return false;

    if (IsPathSeparator(parent_directory[parent_bytes - 1u]))
        return true;
    return IsPathSeparator(candidate_path[parent_bytes]);
}

ResolvedExport ResolveRenderDoc()
{
#ifdef SGG_RD_TESTING
    if (g_test_module_present == 0u)
        return {ResolveResult::ModuleMissing, nullptr};
    if (g_test_get_api == nullptr)
        return {ResolveResult::ExportMissing, nullptr};
    return {ResolveResult::Ready, g_test_get_api};
#else
    HMODULE module = GetModuleHandleW(L"renderdoc.dll");
    if (module == nullptr)
        return {ResolveResult::ModuleMissing, nullptr};

    FARPROC export_address = GetProcAddress(module, "RENDERDOC_GetAPI");
    if (export_address == nullptr)
        return {ResolveResult::ExportMissing, nullptr};

    return {ResolveResult::Ready, reinterpret_cast<pRENDERDOC_GetAPI>(export_address)};
#endif
}

SggRdResult NegotiateApiLocked(pRENDERDOC_GetAPI get_api)
{
    constexpr int kRequestedVersions[] = {
        eRENDERDOC_API_Version_1_7_0,
        eRENDERDOC_API_Version_1_6_0,
        eRENDERDOC_API_Version_1_4_0};

    for (const int requested_version : kRequestedVersions)
    {
        void *raw_table = nullptr;
        if (get_api(static_cast<RENDERDOC_Version>(requested_version), &raw_table) == 0 ||
            raw_table == nullptr)
            continue;

        RENDERDOC_API_1_7_0 *table = reinterpret_cast<RENDERDOC_API_1_7_0 *>(raw_table);

        /* GetAPIVersion is the first member in every supported prefix. */
        pRENDERDOC_GetAPIVersion get_api_version = table->GetAPIVersion;
        if (get_api_version == nullptr)
            continue;

        int actual_major = 0;
        int actual_minor = 0;
        int actual_patch = 0;
        get_api_version(&actual_major, &actual_minor, &actual_patch);
        const uint32_t actual_level = VersionLevel(actual_major, actual_minor, actual_patch);
        if (actual_major != 1 || actual_level < static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0))
            continue;

        const uint32_t capability_level = actual_level < static_cast<uint32_t>(requested_version)
                                              ? actual_level
                                              : static_cast<uint32_t>(requested_version);

        ApiFunctions candidate{};
        candidate.get_api_version = get_api_version;
        candidate.set_capture_path_template = table->SetCaptureFilePathTemplate;
        candidate.get_capture_path_template = table->GetCaptureFilePathTemplate;
        candidate.get_num_captures = table->GetNumCaptures;
        candidate.get_capture = table->GetCapture;
        candidate.set_capture_comments = table->SetCaptureFileComments;
        candidate.is_target_control_connected = table->IsTargetControlConnected;
        candidate.start_frame_capture = table->StartFrameCapture;
        candidate.is_frame_capturing = table->IsFrameCapturing;
        candidate.end_frame_capture = table->EndFrameCapture;
        candidate.discard_frame_capture = table->DiscardFrameCapture;

        if (candidate.set_capture_path_template == nullptr ||
            candidate.get_capture_path_template == nullptr ||
            candidate.get_num_captures == nullptr || candidate.get_capture == nullptr ||
            candidate.set_capture_comments == nullptr || candidate.start_frame_capture == nullptr ||
            candidate.is_target_control_connected == nullptr || candidate.is_frame_capturing == nullptr ||
            candidate.end_frame_capture == nullptr ||
            candidate.discard_frame_capture == nullptr)
            continue;

        candidate.actual_major = static_cast<uint32_t>(actual_major);
        candidate.actual_minor = static_cast<uint32_t>(actual_minor);
        candidate.actual_patch = static_cast<uint32_t>(actual_patch);
        candidate.capability_level = capability_level;
        candidate.supports_discard = capability_level >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0)
                                         ? 1u
                                         : 0u;
        candidate.supports_comments = capability_level >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_2_0)
                                          ? 1u
                                          : 0u;

        if (capability_level >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0))
        {
            /* SetCaptureTitle was appended in 1.6 and is not read for older prefixes. */
            candidate.set_capture_title = table->SetCaptureTitle;
            if (candidate.set_capture_title == nullptr)
                continue;
            candidate.supports_title = 1u;
        }

        if (capability_level >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0))
        {
            /* These are the final legal members of the negotiated 1.7 prefix. */
            candidate.set_object_annotation = table->SetObjectAnnotation;
            candidate.set_command_annotation = table->SetCommandAnnotation;
            candidate.supports_annotations = candidate.set_object_annotation != nullptr &&
                                                     candidate.set_command_annotation != nullptr
                                                 ? 1u
                                                 : 0u;
        }

        g_state.api = candidate;
        g_state.api_valid = 1u;
        return SGG_RD_OK;
    }

    return SGG_RD_API_NEGOTIATION_FAILED;
}

SggRdResult EnsureApiLocked(uint32_t *module_loaded, uint32_t *export_available)
{
    if (module_loaded == nullptr || export_available == nullptr)
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.api_valid != 0u)
    {
        *module_loaded = 1u;
        *export_available = 1u;
        return SGG_RD_OK;
    }

    const ResolvedExport resolved = ResolveRenderDoc();
    if (resolved.result == ResolveResult::ModuleMissing)
    {
        *module_loaded = 0u;
        *export_available = 0u;
        return SGG_RD_NOT_LOADED;
    }

    *module_loaded = 1u;
    if (resolved.result == ResolveResult::ExportMissing)
    {
        *export_available = 0u;
        return SGG_RD_EXPORT_MISSING;
    }

    *export_available = 1u;
    return NegotiateApiLocked(resolved.get_api);
}

bool IsStructLargeEnough(const uint32_t *struct_size, uint32_t required_size)
{
    return struct_size != nullptr && *struct_size >= required_size;
}

bool IsTokenStructValid(const SggRdCaptureTokenV1 *token)
{
    return token != nullptr && IsStructLargeEnough(&token->struct_size, sizeof(SggRdCaptureTokenV1));
}

bool IsOutputTokenValid(const SggRdCaptureTokenV1 *token)
{
    return IsTokenStructValid(token);
}

bool IsOutputCapabilitiesValid(const SggRdCapabilitiesV1 *capabilities)
{
    return capabilities != nullptr &&
           IsStructLargeEnough(&capabilities->struct_size, sizeof(SggRdCapabilitiesV1));
}

bool IsOutputArtifactValid(const SggRdArtifactV1 *artifact)
{
    return artifact != nullptr && IsStructLargeEnough(&artifact->struct_size, sizeof(SggRdArtifactV1));
}

bool TokensMatch(const SggRdCaptureTokenV1 &left, const SggRdCaptureTokenV1 &right)
{
    return left.reserved0 == right.reserved0 && left.request_nonce == right.request_nonce &&
           left.count_before == right.count_before && left.reserved1 == right.reserved1 &&
           left.start_unix_ns == right.start_unix_ns;
}

void WriteToken(SggRdCaptureTokenV1 *destination, const SggRdCaptureTokenV1 &token)
{
    SggRdCaptureTokenV1 output = token;
    output.struct_size = destination->struct_size;
    std::memcpy(destination, &output, sizeof(output));
}

void WriteArtifact(SggRdArtifactV1 *destination, const SggRdArtifactV1 &artifact)
{
    SggRdArtifactV1 output = artifact;
    output.struct_size = destination->struct_size;
    std::memcpy(destination, &output, sizeof(output));
}

void ClearCompletedLocked()
{
    g_state.completed = 0u;
    g_state.completed_token = {};
    g_state.completed_parent_directory[0] = '\0';
    g_state.completed_parent_directory_bytes = 0u;
    g_state.observed = 0u;
    g_state.observed_path[0] = '\0';
    g_state.observed_path_bytes = 0u;
    g_state.observed_index = 0u;
    g_state.observed_timestamp_seconds = 0u;
    g_state.observed_unix_ns = 0u;
}

void RestorePreviousTemplateLocked()
{
    if (g_state.previous_template_valid != 0u && g_state.api.set_capture_path_template != nullptr)
        g_state.api.set_capture_path_template(g_state.previous_template);
}

void ClearActiveLocked()
{
    g_state.active = 0u;
    g_state.active_token = {};
    g_state.previous_template_valid = 0u;
    g_state.previous_template[0] = '\0';
    g_state.active_template[0] = '\0';
    g_state.active_parent_directory[0] = '\0';
    g_state.active_parent_directory_bytes = 0u;
}

SggRdResult ReadCandidateLocked(uint32_t index, const char *parent_directory,
                                uint32_t parent_bytes, Candidate *out_candidate)
{
    if (out_candidate == nullptr)
        return SGG_RD_INTERNAL_ERROR;

    uint32_t reported_path_bytes = 0u;
    uint64_t queried_timestamp = 0u;
    if (g_state.api.get_capture(index, nullptr, &reported_path_bytes, &queried_timestamp) == 0u)
        return SGG_RD_CAPTURE_NOT_OBSERVED;

    if (reported_path_bytes == 0u || reported_path_bytes > kMaxPathBytes)
        return SGG_RD_CAPTURE_FAILED;

    Candidate candidate{};
    candidate.index = index;
    candidate.timestamp_seconds = queried_timestamp;

    uint32_t returned_path_bytes = 0u;
    uint64_t returned_timestamp = 0u;
    if (g_state.api.get_capture(index, candidate.path, &returned_path_bytes, &returned_timestamp) == 0u)
        return SGG_RD_CAPTURE_NOT_OBSERVED;

    uint32_t measured_path_bytes = 0u;
    if (!BoundedStringLength(candidate.path, kMaxPathBytes, &measured_path_bytes) ||
        !IsValidUtf8(candidate.path, measured_path_bytes))
        return SGG_RD_CAPTURE_FAILED;

    if (measured_path_bytes >= kMaxPathBytes || returned_path_bytes != measured_path_bytes + 1u ||
        reported_path_bytes != measured_path_bytes + 1u)
        return SGG_RD_CAPTURE_FAILED;

    candidate.path_bytes = measured_path_bytes;
    candidate.timestamp_seconds = returned_timestamp;
    candidate.observed_unix_ns = NowUnixNanoseconds();

    if (!IsPathInsideParent(candidate.path, candidate.path_bytes, parent_directory, parent_bytes))
        return SGG_RD_CAPTURE_NOT_OBSERVED;

    *out_candidate = candidate;
    return SGG_RD_OK;
}

void FillCapabilities(const uint32_t module_loaded, const uint32_t export_available,
                      SggRdCapabilitiesV1 *out_capabilities)
{
    const uint32_t caller_struct_size = out_capabilities->struct_size;
    SggRdCapabilitiesV1 capabilities{};
    capabilities.struct_size = caller_struct_size;
    capabilities.bridge_abi_major = SGG_RD_ABI_MAJOR_V1;
    capabilities.bridge_abi_minor = SGG_RD_ABI_MINOR_V1;
    capabilities.platform_supported = 1u;
    capabilities.module_loaded = module_loaded;
    capabilities.export_available = export_available;

    if (g_state.api_valid != 0u)
    {
        capabilities.api_negotiated = 1u;
        capabilities.target_control_connected = g_state.api.is_target_control_connected();
        capabilities.is_capturing = g_state.api.is_frame_capturing();
        capabilities.api_major = g_state.api.actual_major;
        capabilities.api_minor = g_state.api.actual_minor;
        capabilities.api_patch = g_state.api.actual_patch;
        capabilities.supports_discard = g_state.api.supports_discard;
        capabilities.supports_comments = g_state.api.supports_comments;
        capabilities.supports_title = g_state.api.supports_title;
        capabilities.supports_annotations = g_state.api.supports_annotations;
        if (capabilities.supports_discard != 0u)
            capabilities.feature_flags |= SGG_RD_FEATURE_DISCARD_V1;
        if (capabilities.supports_comments != 0u)
            capabilities.feature_flags |= SGG_RD_FEATURE_COMMENTS_V1;
        if (capabilities.supports_title != 0u)
            capabilities.feature_flags |= SGG_RD_FEATURE_TITLE_V1;
        if (capabilities.supports_annotations != 0u)
            capabilities.feature_flags |= SGG_RD_FEATURE_ANNOTATIONS_V1;
        capabilities.capture_count = g_state.api.get_num_captures();
    }

    std::memcpy(out_capabilities, &capabilities, sizeof(capabilities));
}

SggRdResult BeginCaptureImpl(uint64_t request_nonce, const char *capture_path_template,
                             uint32_t capture_path_template_bytes, const char *title,
                             uint32_t title_bytes, SggRdCaptureTokenV1 *out_token)
{
    if (!IsOutputTokenValid(out_token) || request_nonce == 0u || capture_path_template == nullptr ||
        capture_path_template_bytes == 0u || capture_path_template_bytes >= kMaxPathBytes ||
        !IsValidUtf8(capture_path_template, capture_path_template_bytes) ||
        IsPathSeparator(capture_path_template[capture_path_template_bytes - 1u]) ||
        title_bytes > kMaxTitleBytes || (title == nullptr && title_bytes != 0u) ||
        !IsValidUtf8(title == nullptr ? "" : title, title_bytes))
        return SGG_RD_INVALID_ARGUMENT;

    char supplied_template[kMaxPathBytes]{};
    char requested_title[kMaxTitleBytes + 1u]{};
    char previous_template[kMaxPathBytes]{};
    char parent_directory[kMaxPathBytes]{};
    uint32_t parent_bytes = 0u;

    if (!CopyUtf8(capture_path_template, capture_path_template_bytes, supplied_template, kMaxPathBytes) ||
        !CopyOptionalUtf8(title, title_bytes, requested_title, sizeof(requested_title)) ||
        !DeriveParentDirectory(supplied_template, capture_path_template_bytes, parent_directory,
                               &parent_bytes) || parent_bytes == 0u)
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.active != 0u)
        return SGG_RD_ALREADY_CAPTURING;

    uint32_t module_loaded = 0u;
    uint32_t export_available = 0u;
    const SggRdResult api_result = EnsureApiLocked(&module_loaded, &export_available);
    (void)module_loaded;
    (void)export_available;
    if (api_result != SGG_RD_OK)
        return api_result;

    if (g_state.api.is_frame_capturing() != 0u)
        return SGG_RD_ALREADY_CAPTURING;

    const uint32_t count_before = g_state.api.get_num_captures();
    const char *current_template = g_state.api.get_capture_path_template();
    uint32_t previous_bytes = 0u;
    if (current_template == nullptr || !BoundedStringLength(current_template, kMaxPathBytes, &previous_bytes) ||
        !CopyUtf8(current_template, previous_bytes, previous_template, kMaxPathBytes))
        return SGG_RD_CAPTURE_FAILED;

    g_state.api.set_capture_path_template(supplied_template);
    const uint64_t start_unix_ns = NowUnixNanoseconds();
    g_state.api.start_frame_capture(nullptr, nullptr);

    if (g_state.api.is_frame_capturing() == 0u)
    {
        g_state.api.set_capture_path_template(previous_template);
        return SGG_RD_CAPTURE_FAILED;
    }

    if (title_bytes != 0u && g_state.api.supports_title != 0u)
        g_state.api.set_capture_title(requested_title);

    SggRdCaptureTokenV1 token{};
    token.struct_size = sizeof(SggRdCaptureTokenV1);
    token.request_nonce = request_nonce;
    token.count_before = count_before;
    token.start_unix_ns = start_unix_ns;

    g_state.active = 1u;
    g_state.active_token = token;
    g_state.previous_template_valid = 1u;
    std::memcpy(g_state.previous_template, previous_template, sizeof(previous_template));
    std::memcpy(g_state.active_template, supplied_template, sizeof(supplied_template));
    std::memcpy(g_state.active_parent_directory, parent_directory, sizeof(parent_directory));
    g_state.active_parent_directory_bytes = parent_bytes;
    ClearCompletedLocked();
    WriteToken(out_token, token);
    return SGG_RD_OK;
}

SggRdResult EndCaptureImpl(const SggRdCaptureTokenV1 *token)
{
    if (!IsTokenStructValid(token))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.active == 0u)
        return SGG_RD_NOT_CAPTURING;
    if (!TokensMatch(*token, g_state.active_token))
        return SGG_RD_INVALID_ARGUMENT;

    if (g_state.api.is_frame_capturing() == 0u)
    {
        RestorePreviousTemplateLocked();
        ClearActiveLocked();
        return SGG_RD_NOT_CAPTURING;
    }

    const uint32_t ended = g_state.api.end_frame_capture(nullptr, nullptr);
    RestorePreviousTemplateLocked();
    if (ended == 0u)
        return SGG_RD_CAPTURE_FAILED;

    g_state.completed = 1u;
    g_state.completed_token = g_state.active_token;
    std::memcpy(g_state.completed_parent_directory, g_state.active_parent_directory,
                sizeof(g_state.completed_parent_directory));
    g_state.completed_parent_directory_bytes = g_state.active_parent_directory_bytes;
    g_state.observed = 0u;
    g_state.observed_path[0] = '\0';
    g_state.observed_path_bytes = 0u;
    g_state.observed_index = 0u;
    g_state.observed_timestamp_seconds = 0u;
    g_state.observed_unix_ns = 0u;
    ClearActiveLocked();
    return SGG_RD_OK;
}

SggRdResult DiscardCaptureImpl(const SggRdCaptureTokenV1 *token)
{
    if (!IsTokenStructValid(token))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.active == 0u)
        return SGG_RD_NOT_CAPTURING;
    if (!TokensMatch(*token, g_state.active_token))
        return SGG_RD_INVALID_ARGUMENT;

    if (g_state.api.is_frame_capturing() == 0u)
    {
        RestorePreviousTemplateLocked();
        ClearActiveLocked();
        return SGG_RD_NOT_CAPTURING;
    }

    const uint32_t discarded = g_state.api.discard_frame_capture(nullptr, nullptr);
    RestorePreviousTemplateLocked();
    if (discarded == 0u)
        return SGG_RD_CAPTURE_FAILED;

    ClearActiveLocked();
    ClearCompletedLocked();
    return SGG_RD_OK;
}

SggRdResult TryGetNewArtifactImpl(const SggRdCaptureTokenV1 *token, SggRdArtifactV1 *out_artifact,
                                  char *path_buffer, uint32_t path_buffer_bytes)
{
    if (!IsTokenStructValid(token) || !IsOutputArtifactValid(out_artifact) ||
        (path_buffer == nullptr && path_buffer_bytes != 0u))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.completed == 0u)
        return SGG_RD_NOT_CAPTURING;
    if (!TokensMatch(*token, g_state.completed_token))
        return SGG_RD_INVALID_ARGUMENT;

    const uint32_t count_now = g_state.api.get_num_captures();
    uint32_t matching_candidates = 0u;
    bool candidate_read_failed = false;
    Candidate selected{};
    for (uint64_t index = g_state.completed_token.count_before; index < count_now; ++index)
    {
        Candidate candidate{};
        const SggRdResult candidate_result =
            ReadCandidateLocked(static_cast<uint32_t>(index), g_state.completed_parent_directory,
                                g_state.completed_parent_directory_bytes, &candidate);
        if (candidate_result == SGG_RD_CAPTURE_FAILED)
        {
            candidate_read_failed = true;
            continue;
        }
        if (candidate_result != SGG_RD_OK)
            continue;

        ++matching_candidates;
        if (matching_candidates == 1u)
            selected = candidate;
    }

    if (candidate_read_failed)
        return SGG_RD_CAPTURE_FAILED;

    if (matching_candidates == 0u)
    {
        SggRdArtifactV1 empty{};
        empty.struct_size = out_artifact->struct_size;
        WriteArtifact(out_artifact, empty);
        return SGG_RD_CAPTURE_NOT_OBSERVED;
    }

    if (matching_candidates > 1u)
        return SGG_RD_CAPTURE_FAILED;

    if (g_state.observed != 0u)
    {
        if (selected.index != g_state.observed_index ||
            selected.timestamp_seconds != g_state.observed_timestamp_seconds ||
            selected.path_bytes != g_state.observed_path_bytes ||
            std::memcmp(selected.path, g_state.observed_path, selected.path_bytes) != 0)
            return SGG_RD_CAPTURE_FAILED;

        selected.observed_unix_ns = g_state.observed_unix_ns;
    }
    else
    {
        g_state.observed = 1u;
        g_state.observed_index = selected.index;
        g_state.observed_timestamp_seconds = selected.timestamp_seconds;
        g_state.observed_unix_ns = selected.observed_unix_ns;
        g_state.observed_path_bytes = selected.path_bytes;
        std::memcpy(g_state.observed_path, selected.path, sizeof(g_state.observed_path));
    }

    SggRdArtifactV1 artifact{};
    artifact.struct_size = sizeof(SggRdArtifactV1);
    artifact.index = selected.index;
    artifact.renderdoc_timestamp_seconds = selected.timestamp_seconds;
    artifact.observed_unix_ns = selected.observed_unix_ns;
    artifact.required_path_bytes = selected.path_bytes + 1u;
    WriteArtifact(out_artifact, artifact);

    if (path_buffer == nullptr || path_buffer_bytes < artifact.required_path_bytes)
        return SGG_RD_BUFFER_TOO_SMALL;

    std::memcpy(path_buffer, selected.path, artifact.required_path_bytes);
    return SGG_RD_OK;
}

SggRdResult SetCaptureCommentsImpl(const SggRdCaptureTokenV1 *token, const char *observed_path,
                                   uint32_t observed_path_bytes, const char *comments,
                                   uint32_t comments_bytes)
{
    if (!IsTokenStructValid(token) || observed_path == nullptr || observed_path_bytes == 0u ||
        observed_path_bytes >= kMaxPathBytes || !IsValidUtf8(observed_path, observed_path_bytes) ||
        comments_bytes > kMaxCommentsBytes || (comments == nullptr && comments_bytes != 0u) ||
        !IsValidUtf8(comments == nullptr ? "" : comments, comments_bytes))
        return SGG_RD_INVALID_ARGUMENT;

    char path_copy[kMaxPathBytes]{};
    char comments_copy[kMaxCommentsBytes + 1u]{};
    if (!CopyUtf8(observed_path, observed_path_bytes, path_copy, kMaxPathBytes) ||
        !CopyOptionalUtf8(comments, comments_bytes, comments_copy, sizeof(comments_copy)))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    if (g_state.completed == 0u)
        return SGG_RD_NOT_CAPTURING;
    if (!TokensMatch(*token, g_state.completed_token))
        return SGG_RD_INVALID_ARGUMENT;
    if (g_state.observed == 0u)
        return SGG_RD_CAPTURE_NOT_OBSERVED;
    if (observed_path_bytes != g_state.observed_path_bytes ||
        std::memcmp(path_copy, g_state.observed_path, observed_path_bytes) != 0)
        return SGG_RD_INVALID_ARGUMENT;

    g_state.api.set_capture_comments(path_copy, comments_copy);
    return SGG_RD_OK;
}

SggRdResult GetCapabilitiesImpl(SggRdCapabilitiesV1 *out_capabilities)
{
    if (!IsOutputCapabilitiesValid(out_capabilities))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    uint32_t module_loaded = 0u;
    uint32_t export_available = 0u;
    const SggRdResult result = EnsureApiLocked(&module_loaded, &export_available);
    FillCapabilities(module_loaded, export_available, out_capabilities);
    return result;
}
} // namespace

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetCapabilitiesV1(
    SggRdCapabilitiesV1 *out_capabilities)
{
    try
    {
        return GetCapabilitiesImpl(out_capabilities);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_BeginCaptureV1(
    uint64_t request_nonce, const char *capture_path_template, uint32_t capture_path_template_bytes,
    const char *title, uint32_t title_bytes, SggRdCaptureTokenV1 *out_token)
{
    try
    {
        return BeginCaptureImpl(request_nonce, capture_path_template, capture_path_template_bytes, title,
                                title_bytes, out_token);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_EndCaptureV1(
    const SggRdCaptureTokenV1 *token)
{
    try
    {
        return EndCaptureImpl(token);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_DiscardCaptureV1(
    const SggRdCaptureTokenV1 *token)
{
    try
    {
        return DiscardCaptureImpl(token);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_TryGetNewArtifactV1(
    const SggRdCaptureTokenV1 *token, SggRdArtifactV1 *out_artifact, char *path_buffer,
    uint32_t path_buffer_bytes)
{
    try
    {
        return TryGetNewArtifactImpl(token, out_artifact, path_buffer, path_buffer_bytes);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_SetCaptureCommentsV1(
    const SggRdCaptureTokenV1 *token, const char *observed_path, uint32_t observed_path_bytes,
    const char *comments, uint32_t comments_bytes)
{
    try
    {
        return SetCaptureCommentsImpl(token, observed_path, observed_path_bytes, comments, comments_bytes);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

#ifdef SGG_RD_TESTING
extern "C" void SGG_RD_CALL SggRd_TestReset()
{
    std::lock_guard<std::mutex> lock(g_bridge_mutex);
    g_state = BridgeState{};
    g_test_module_present = 0u;
    g_test_get_api = nullptr;
}

extern "C" void SGG_RD_CALL SggRd_TestSetResolver(uint32_t module_present,
                                                    pRENDERDOC_GetAPI get_api)
{
    std::lock_guard<std::mutex> lock(g_bridge_mutex);
    g_state.api = ApiFunctions{};
    g_state.api_valid = 0u;
    g_test_module_present = module_present != 0u ? 1u : 0u;
    g_test_get_api = get_api;
}
#endif
