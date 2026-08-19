#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <dxgi1_4.h>
#include <d3d12.h>

#include <renderdoc_app.h>

#include <IUnityGraphics.h>
#include <IUnityGraphicsD3D12.h>
#include <IUnityInterface.h>

#include "sgg_renderdoc_bridge.h"

#include <array>
#include <atomic>
#include <cstdint>
#include <cstring>
#include <limits>
#include <mutex>

namespace
{
constexpr uint32_t kMaxPathBytes = SGG_RD_MAX_PATH_BYTES;
constexpr uint32_t kMaxTitleBytes = SGG_RD_MAX_TITLE_BYTES;
constexpr uint32_t kMaxCommentsBytes = SGG_RD_MAX_COMMENTS_BYTES;
constexpr uint32_t kMaxAnnotationEntries = SGG_RD_MAX_ANNOTATION_ENTRIES_V1;
constexpr uint32_t kMaxAnnotationPackets = SGG_RD_MAX_ANNOTATION_PACKETS_V1;

static_assert(sizeof(SggRdResult) == 4u, "SggRdResult must remain a 32-bit ABI value");
static_assert(sizeof(SggRdFeatureBitsV1) == 4u, "SggRdFeatureBitsV1 must remain a 32-bit ABI value");
static_assert(sizeof(SggRdAnnotationTypeV1) == 4u,
              "SggRdAnnotationTypeV1 must remain a 32-bit ABI value");
static_assert(sizeof(SggRdCapabilitiesV1) == 72u, "V1 capabilities layout changed");
static_assert(sizeof(SggRdCaptureTokenV1) == 32u, "V1 token layout changed");
static_assert(sizeof(SggRdArtifactV1) == 32u, "V1 artifact layout changed");
static_assert(sizeof(SggRdAnnotationCapabilitiesV1) == 88u,
              "V1 annotation capabilities layout changed");
static_assert(sizeof(SggRdAnnotationEntryV1) == 440u, "V1 annotation entry layout changed");

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

enum class AnnotationPacketState : uint64_t
{
    Free = 0u,
    Preparing = 1u,
    Allocated = 2u,
    Executing = 3u
};

constexpr uint32_t kAnnotationHandleIndexBits = 8u;
constexpr uintptr_t kAnnotationHandleIndexMask = (uintptr_t{1u} << kAnnotationHandleIndexBits) - 1u;
constexpr uint64_t kAnnotationPacketStateMask = 0x3u;
constexpr uint32_t kMaxAnnotationHandleGeneration =
    sizeof(uintptr_t) >= sizeof(uint64_t)
        ? (std::numeric_limits<uint32_t>::max)()
        : static_cast<uint32_t>((std::numeric_limits<uintptr_t>::max)() >> kAnnotationHandleIndexBits);

uint64_t MakeAnnotationPacketControl(uint32_t generation, AnnotationPacketState state)
{
    return (static_cast<uint64_t>(generation) << 2u) | static_cast<uint64_t>(state);
}

uint32_t GetAnnotationPacketGeneration(uint64_t control)
{
    return static_cast<uint32_t>(control >> 2u);
}

AnnotationPacketState GetAnnotationPacketState(uint64_t control)
{
    return static_cast<AnnotationPacketState>(control & kAnnotationPacketStateMask);
}

struct AnnotationPacket
{
    std::atomic<uint64_t> control{MakeAnnotationPacketControl(0u, AnnotationPacketState::Free)};
    uint32_t entry_count = 0u;
    SggRdAnnotationEntryV1 entries[kMaxAnnotationEntries]{};
};

std::mutex g_bridge_mutex;
BridgeState g_state{};
std::array<AnnotationPacket, kMaxAnnotationPackets> g_annotation_packets{};
std::atomic<uint32_t> g_unity_plugin_loaded{0u};
std::atomic<uint32_t> g_graphics_renderer{static_cast<uint32_t>(kUnityGfxRendererNull)};
std::atomic<int32_t> g_annotation_event_id{-1};
std::atomic<IUnityGraphicsD3D12v7 *> g_unity_d3d12{nullptr};
IUnityInterfaces *g_unity_interfaces = nullptr;
IUnityGraphics *g_unity_graphics = nullptr;
std::atomic<uint32_t> g_packets_in_use{0u};
std::atomic<uint32_t> g_packets_created{0u};
std::atomic<uint32_t> g_packets_executed{0u};
std::atomic<uint32_t> g_packets_dropped{0u};
std::atomic<uint32_t> g_annotation_calls{0u};
std::atomic<uint32_t> g_annotation_errors{0u};
std::atomic<uint32_t> g_graphics_epoch{1u};

bool IsStructLargeEnough(const uint32_t *struct_size, uint32_t required_size);

#ifdef SGG_RD_TESTING
uint32_t g_test_module_present = 0u;
pRENDERDOC_GetAPI g_test_get_api = nullptr;
void *g_test_annotation_device = nullptr;
void *g_test_annotation_command = nullptr;
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

bool IsAnnotationKeyCharacter(char value)
{
    return (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') ||
           (value >= '0' && value <= '9') || value == '_' || value == '-' || value == '.';
}

bool IsValidAnnotationKey(const char *key, uint32_t key_bytes)
{
    if (key == nullptr || key_bytes == 0u || key_bytes > SGG_RD_MAX_ANNOTATION_KEY_BYTES_V1 ||
        key[0] == '.' || key[key_bytes - 1u] == '.' || !IsValidUtf8(key, key_bytes))
        return false;

    char previous = '\0';
    for (uint32_t index = 0u; index < key_bytes; ++index)
    {
        const char value = key[index];
        if (!IsAnnotationKeyCharacter(value) || (value == '.' && previous == '.'))
            return false;
        previous = value;
    }
    return true;
}

bool IsZeroValueData(const uint64_t *value_data)
{
    if (value_data == nullptr)
        return false;
    for (uint32_t index = 0u; index < 4u; ++index)
    {
        if (value_data[index] != 0u)
            return false;
    }
    return true;
}

bool AreUnusedValueLanesZero(const uint64_t *value_data, uint32_t vector_width)
{
    if (value_data == nullptr || vector_width > 4u)
        return false;
    for (uint32_t index = vector_width; index < 4u; ++index)
    {
        if (value_data[index] != 0u)
            return false;
    }
    return true;
}

bool AreUsedValueLanesCanonical32(const uint64_t *value_data, uint32_t vector_width)
{
    if (value_data == nullptr || vector_width > 4u)
        return false;
    for (uint32_t index = 0u; index < vector_width; ++index)
    {
        if ((value_data[index] & 0xFFFFFFFF00000000ULL) != 0u)
            return false;
    }
    return true;
}

bool IsAnnotationEntryValid(const SggRdAnnotationEntryV1 &entry)
{
    /* The V1 array export has no explicit stride, so every element must use the exact V1 size. */
    if (entry.struct_size != sizeof(SggRdAnnotationEntryV1) || entry.reserved0 != 0u ||
        !IsValidAnnotationKey(entry.key, entry.key_bytes) || entry.key[entry.key_bytes] != '\0')
        return false;

    switch (entry.value_type)
    {
    case SGG_RD_ANNOTATION_EMPTY_V1:
        return entry.vector_width == 0u && entry.string_bytes == 0u &&
               entry.string_value[0] == '\0' && IsZeroValueData(entry.value_data);
    case SGG_RD_ANNOTATION_STRING_V1:
        return entry.vector_width == 1u && entry.string_bytes <= SGG_RD_MAX_ANNOTATION_STRING_BYTES_V1 &&
               entry.string_value[entry.string_bytes] == '\0' &&
               IsValidUtf8(entry.string_value, entry.string_bytes) && IsZeroValueData(entry.value_data);
    case SGG_RD_ANNOTATION_BOOL_V1:
        if (entry.vector_width == 0u || entry.vector_width > 4u || entry.string_bytes != 0u ||
            entry.string_value[0] != '\0' ||
            !AreUnusedValueLanesZero(entry.value_data, entry.vector_width))
            return false;
        for (uint32_t index = 0u; index < entry.vector_width; ++index)
        {
            if (entry.value_data[index] > 1u)
                return false;
        }
        return true;
    case SGG_RD_ANNOTATION_INT32_V1:
    case SGG_RD_ANNOTATION_UINT32_V1:
    case SGG_RD_ANNOTATION_FLOAT_V1:
        return entry.vector_width >= 1u && entry.vector_width <= 4u && entry.string_bytes == 0u &&
               entry.string_value[0] == '\0' &&
               AreUsedValueLanesCanonical32(entry.value_data, entry.vector_width) &&
               AreUnusedValueLanesZero(entry.value_data, entry.vector_width);
    case SGG_RD_ANNOTATION_INT64_V1:
    case SGG_RD_ANNOTATION_UINT64_V1:
    case SGG_RD_ANNOTATION_DOUBLE_V1:
        return entry.vector_width >= 1u && entry.vector_width <= 4u && entry.string_bytes == 0u &&
               entry.string_value[0] == '\0' &&
               AreUnusedValueLanesZero(entry.value_data, entry.vector_width);
    default:
        return false;
    }
}

AnnotationPacket *TryAcquireAnnotationPacket(uint32_t *out_generation, uint32_t *out_index)
{
    if (out_generation == nullptr || out_index == nullptr)
        return nullptr;
    *out_generation = 0u;
    *out_index = 0u;

    for (uint32_t index = 0u; index < g_annotation_packets.size(); ++index)
    {
        AnnotationPacket &packet = g_annotation_packets[index];
        uint64_t expected = packet.control.load(std::memory_order_acquire);
        while (GetAnnotationPacketState(expected) == AnnotationPacketState::Free)
        {
            uint32_t generation = GetAnnotationPacketGeneration(expected) + 1u;
            if (generation == 0u || generation > kMaxAnnotationHandleGeneration)
                generation = 1u;
            const uint64_t desired =
                MakeAnnotationPacketControl(generation, AnnotationPacketState::Preparing);
            if (packet.control.compare_exchange_weak(expected, desired, std::memory_order_acq_rel,
                                                     std::memory_order_acquire))
            {
                packet.entry_count = 0u;
                std::memset(packet.entries, 0, sizeof(packet.entries));
                g_packets_in_use.fetch_add(1u, std::memory_order_relaxed);
                *out_generation = generation;
                *out_index = index;
                return &packet;
            }
        }
    }
    return nullptr;
}

void *MakeAnnotationPacketHandle(uint32_t index, uint32_t generation)
{
    const uintptr_t value = (static_cast<uintptr_t>(generation) << kAnnotationHandleIndexBits) |
                            static_cast<uintptr_t>(index + 1u);
    return reinterpret_cast<void *>(value);
}

bool ResolveAnnotationPacketHandle(void *raw_packet, AnnotationPacket **out_packet,
                                   uint32_t *out_generation)
{
    if (out_packet == nullptr || out_generation == nullptr)
        return false;
    *out_packet = nullptr;
    *out_generation = 0u;

    const uintptr_t value = reinterpret_cast<uintptr_t>(raw_packet);
    const uintptr_t encoded_index = value & kAnnotationHandleIndexMask;
    const uintptr_t encoded_generation = value >> kAnnotationHandleIndexBits;
    if (encoded_index == 0u || encoded_index > g_annotation_packets.size() ||
        encoded_generation == 0u || encoded_generation > kMaxAnnotationHandleGeneration)
        return false;

    *out_packet = &g_annotation_packets[encoded_index - 1u];
    *out_generation = static_cast<uint32_t>(encoded_generation);
    return true;
}

void PublishAnnotationPacket(AnnotationPacket &packet, uint32_t generation)
{
    packet.control.store(MakeAnnotationPacketControl(generation, AnnotationPacketState::Allocated),
                         std::memory_order_release);
}

void FinishAnnotationPacket(AnnotationPacket &packet, uint32_t generation)
{
    packet.entry_count = 0u;
    std::memset(packet.entries, 0, sizeof(packet.entries));
    packet.control.store(MakeAnnotationPacketControl(generation, AnnotationPacketState::Free),
                         std::memory_order_release);
    g_packets_in_use.fetch_sub(1u, std::memory_order_relaxed);
}

bool TryClaimAnnotationPacket(AnnotationPacket &packet, uint32_t generation)
{
    uint64_t expected = MakeAnnotationPacketControl(generation, AnnotationPacketState::Allocated);
    return packet.control.compare_exchange_strong(
        expected, MakeAnnotationPacketControl(generation, AnnotationPacketState::Executing),
        std::memory_order_acq_rel, std::memory_order_relaxed);
}

void DropAllocatedAnnotationPackets()
{
    for (AnnotationPacket &packet : g_annotation_packets)
    {
        const uint64_t control = packet.control.load(std::memory_order_acquire);
        const uint32_t generation = GetAnnotationPacketGeneration(control);
        if (GetAnnotationPacketState(control) == AnnotationPacketState::Allocated &&
            TryClaimAnnotationPacket(packet, generation))
        {
            g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
            FinishAnnotationPacket(packet, generation);
        }
    }
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

bool TryGetAnnotationTarget(void **out_device, void **out_command)
{
    if (out_device == nullptr || out_command == nullptr)
        return false;
    *out_device = nullptr;
    *out_command = nullptr;

#ifdef SGG_RD_TESTING
    if (g_test_annotation_device != nullptr && g_test_annotation_command != nullptr)
    {
        *out_device = g_test_annotation_device;
        *out_command = g_test_annotation_command;
        return true;
    }
#endif

    IUnityGraphicsD3D12v7 *d3d12 = g_unity_d3d12.load(std::memory_order_acquire);
    if (d3d12 == nullptr || d3d12->GetDevice == nullptr || d3d12->CommandRecordingState == nullptr)
        return false;

    UnityGraphicsD3D12RecordingState recording_state{};
    if (!d3d12->CommandRecordingState(&recording_state) || recording_state.commandList == nullptr)
        return false;

    ID3D12Device *device = d3d12->GetDevice();
    if (device == nullptr)
        return false;

    *out_device = device;
    *out_command = recording_state.commandList;
    return true;
}

RENDERDOC_AnnotationType ToRenderDocAnnotationType(uint32_t value_type)
{
    switch (value_type)
    {
    case SGG_RD_ANNOTATION_EMPTY_V1:
        return eRENDERDOC_Empty;
    case SGG_RD_ANNOTATION_BOOL_V1:
        return eRENDERDOC_Bool;
    case SGG_RD_ANNOTATION_INT32_V1:
        return eRENDERDOC_Int32;
    case SGG_RD_ANNOTATION_UINT32_V1:
        return eRENDERDOC_UInt32;
    case SGG_RD_ANNOTATION_INT64_V1:
        return eRENDERDOC_Int64;
    case SGG_RD_ANNOTATION_UINT64_V1:
        return eRENDERDOC_UInt64;
    case SGG_RD_ANNOTATION_FLOAT_V1:
        return eRENDERDOC_Float;
    case SGG_RD_ANNOTATION_DOUBLE_V1:
        return eRENDERDOC_Double;
    case SGG_RD_ANNOTATION_STRING_V1:
        return eRENDERDOC_String;
    default:
        return eRENDERDOC_AnnotationMax;
    }
}

const RENDERDOC_AnnotationValue *BuildRenderDocAnnotationValue(
    const SggRdAnnotationEntryV1 &entry, RENDERDOC_AnnotationValue *out_value)
{
    if (entry.value_type == SGG_RD_ANNOTATION_EMPTY_V1)
        return nullptr;
    if (out_value == nullptr)
        return nullptr;

    *out_value = {};
    if (entry.value_type == SGG_RD_ANNOTATION_STRING_V1)
    {
        out_value->string = entry.string_value;
        return out_value;
    }

    for (uint32_t index = 0u; index < entry.vector_width; ++index)
    {
        switch (entry.value_type)
        {
        case SGG_RD_ANNOTATION_BOOL_V1:
            out_value->vector.boolean[index] = entry.value_data[index] != 0u;
            break;
        case SGG_RD_ANNOTATION_INT32_V1:
        {
            const uint32_t bits = static_cast<uint32_t>(entry.value_data[index]);
            std::memcpy(&out_value->vector.int32[index], &bits, sizeof(bits));
            break;
        }
        case SGG_RD_ANNOTATION_UINT32_V1:
            out_value->vector.uint32[index] = static_cast<uint32_t>(entry.value_data[index]);
            break;
        case SGG_RD_ANNOTATION_INT64_V1:
            out_value->vector.int64[index] = static_cast<int64_t>(entry.value_data[index]);
            break;
        case SGG_RD_ANNOTATION_UINT64_V1:
            out_value->vector.uint64[index] = entry.value_data[index];
            break;
        case SGG_RD_ANNOTATION_FLOAT_V1:
        {
            const uint32_t bits = static_cast<uint32_t>(entry.value_data[index]);
            std::memcpy(&out_value->vector.float32[index], &bits, sizeof(bits));
            break;
        }
        case SGG_RD_ANNOTATION_DOUBLE_V1:
        {
            const uint64_t bits = entry.value_data[index];
            std::memcpy(&out_value->vector.float64[index], &bits, sizeof(bits));
            break;
        }
        default:
            return nullptr;
        }
    }
    return out_value;
}

void ExecuteAnnotationPacket(AnnotationPacket &packet, void *device, void *command)
{
    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
    {
        g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
        g_annotation_errors.fetch_add(1u, std::memory_order_relaxed);
        return;
    }

    uint32_t module_loaded = 0u;
    uint32_t export_available = 0u;
    if (EnsureApiLocked(&module_loaded, &export_available) != SGG_RD_OK ||
        g_state.api.supports_annotations == 0u || g_state.api.set_command_annotation == nullptr ||
        g_state.api.is_frame_capturing == nullptr || g_state.api.is_frame_capturing() == 0u ||
        device == nullptr || command == nullptr)
    {
        g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
        return;
    }

    bool packet_error = false;
    for (uint32_t index = 0u; index < packet.entry_count; ++index)
    {
        const SggRdAnnotationEntryV1 &entry = packet.entries[index];
        RENDERDOC_AnnotationValue value{};
        const RENDERDOC_AnnotationValue *value_pointer = BuildRenderDocAnnotationValue(entry, &value);
        const RENDERDOC_AnnotationType value_type = ToRenderDocAnnotationType(entry.value_type);
        if (value_type == eRENDERDOC_AnnotationMax ||
            (entry.value_type != SGG_RD_ANNOTATION_EMPTY_V1 && value_pointer == nullptr))
        {
            packet_error = true;
            g_annotation_errors.fetch_add(1u, std::memory_order_relaxed);
            continue;
        }

        /* RenderDoc requires a scalar width of zero for strings; numeric scalar width 1 is legal. */
        const uint32_t renderdoc_vector_width =
            entry.value_type == SGG_RD_ANNOTATION_STRING_V1 ? 0u : entry.vector_width;
        const uint32_t renderdoc_result = g_state.api.set_command_annotation(
            device, command, entry.key, value_type, renderdoc_vector_width, value_pointer);
        g_annotation_calls.fetch_add(1u, std::memory_order_relaxed);
        if (renderdoc_result != 0u)
        {
            packet_error = true;
            g_annotation_errors.fetch_add(1u, std::memory_order_relaxed);
        }
    }

    g_packets_executed.fetch_add(1u, std::memory_order_relaxed);
    if (packet_error)
        g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
}

void UNITY_INTERFACE_API OnAnnotationEvent(int event_id, void *raw_packet)
{
    AnnotationPacket *packet = nullptr;
    uint32_t generation = 0u;
    if (!ResolveAnnotationPacketHandle(raw_packet, &packet, &generation) ||
        !TryClaimAnnotationPacket(*packet, generation))
    {
        g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
        return;
    }

    try
    {
        void *device = nullptr;
        void *command = nullptr;
        if (event_id != g_annotation_event_id.load(std::memory_order_acquire) ||
            !TryGetAnnotationTarget(&device, &command))
        {
            g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
        }
        else
        {
            ExecuteAnnotationPacket(*packet, device, command);
        }
    }
    catch (...)
    {
        g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
        g_annotation_errors.fetch_add(1u, std::memory_order_relaxed);
    }

    FinishAnnotationPacket(*packet, generation);
}

SggRdResult GetAnnotationCapabilitiesImpl(SggRdAnnotationCapabilitiesV1 *out_capabilities)
{
    if (out_capabilities == nullptr ||
        !IsStructLargeEnough(&out_capabilities->struct_size, sizeof(SggRdAnnotationCapabilitiesV1)))
        return SGG_RD_INVALID_ARGUMENT;

    TryLock lock(g_bridge_mutex);
    if (!lock.owns_lock())
        return SGG_RD_INTERNAL_ERROR;

    const uint32_t requested_size = out_capabilities->struct_size;
    uint32_t module_loaded = 0u;
    uint32_t export_available = 0u;
    const SggRdResult api_result = EnsureApiLocked(&module_loaded, &export_available);

    SggRdAnnotationCapabilitiesV1 capabilities{};
    capabilities.struct_size = requested_size;
    capabilities.annotation_abi_major = SGG_RD_ANNOTATION_ABI_MAJOR_V1;
    capabilities.annotation_abi_minor = SGG_RD_ANNOTATION_ABI_MINOR_V1;
    capabilities.unity_plugin_loaded = g_unity_plugin_loaded.load(std::memory_order_acquire);
    capabilities.graphics_renderer = g_graphics_renderer.load(std::memory_order_acquire);
    capabilities.backend_supported = g_unity_d3d12.load(std::memory_order_acquire) != nullptr ? 1u : 0u;
    capabilities.module_loaded = module_loaded;
    capabilities.api_negotiated = g_state.api_valid;
    capabilities.supports_annotations = g_state.api.supports_annotations;
    capabilities.renderdoc_api_major = g_state.api.actual_major;
    capabilities.renderdoc_api_minor = g_state.api.actual_minor;
    capabilities.renderdoc_api_patch = g_state.api.actual_patch;
    capabilities.is_capturing = g_state.api_valid != 0u && g_state.api.is_frame_capturing != nullptr
                                    ? g_state.api.is_frame_capturing()
                                    : 0u;
    const int32_t event_id = g_annotation_event_id.load(std::memory_order_acquire);
    capabilities.event_id_valid = event_id >= 0 ? 1u : 0u;
    capabilities.event_id = event_id >= 0 ? static_cast<uint32_t>(event_id) : 0u;
    capabilities.packet_capacity = kMaxAnnotationPackets;
    capabilities.packets_in_use = g_packets_in_use.load(std::memory_order_relaxed);
    capabilities.packets_created = g_packets_created.load(std::memory_order_relaxed);
    capabilities.packets_executed = g_packets_executed.load(std::memory_order_relaxed);
    capabilities.packets_dropped = g_packets_dropped.load(std::memory_order_relaxed);
    capabilities.annotation_calls = g_annotation_calls.load(std::memory_order_relaxed);
    capabilities.annotation_errors = g_annotation_errors.load(std::memory_order_relaxed);
    std::memcpy(out_capabilities, &capabilities, sizeof(capabilities));

    if (api_result != SGG_RD_OK)
        return api_result;
    if (capabilities.supports_annotations == 0u)
        return SGG_RD_ANNOTATIONS_UNAVAILABLE;
    if (capabilities.unity_plugin_loaded == 0u || capabilities.backend_supported == 0u ||
        capabilities.event_id_valid == 0u)
        return SGG_RD_BACKEND_UNSUPPORTED;
    if (capabilities.is_capturing == 0u)
        return SGG_RD_CAPTURE_INACTIVE;
    if (capabilities.packets_in_use >= capabilities.packet_capacity)
        return SGG_RD_PACKET_POOL_EXHAUSTED;
    return SGG_RD_OK;
}

SggRdResult GetAnnotationEventImpl(void **out_callback, int32_t *out_event_id)
{
    if (out_callback == nullptr || out_event_id == nullptr)
        return SGG_RD_INVALID_ARGUMENT;
    *out_callback = nullptr;
    *out_event_id = -1;

    const int32_t event_id = g_annotation_event_id.load(std::memory_order_acquire);
    if (g_unity_plugin_loaded.load(std::memory_order_acquire) == 0u ||
        g_unity_d3d12.load(std::memory_order_acquire) == nullptr || event_id < 0)
        return SGG_RD_BACKEND_UNSUPPORTED;

    *out_callback = reinterpret_cast<void *>(&OnAnnotationEvent);
    *out_event_id = event_id;
    return SGG_RD_OK;
}

SggRdResult CreateAnnotationPacketImpl(const SggRdAnnotationEntryV1 *entries,
                                       uint32_t entry_count, void **out_packet)
{
    if (out_packet == nullptr)
        return SGG_RD_INVALID_ARGUMENT;
    *out_packet = nullptr;
    if (entries == nullptr || entry_count == 0u || entry_count > kMaxAnnotationEntries)
        return SGG_RD_INVALID_ARGUMENT;

    SggRdAnnotationCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    const SggRdResult capability_result = GetAnnotationCapabilitiesImpl(&capabilities);
    if (capability_result != SGG_RD_OK)
        return capability_result;

    for (uint32_t index = 0u; index < entry_count; ++index)
    {
        if (!IsAnnotationEntryValid(entries[index]))
            return SGG_RD_INVALID_ARGUMENT;
    }

    const uint32_t graphics_epoch = g_graphics_epoch.load(std::memory_order_acquire);
    uint32_t generation = 0u;
    uint32_t packet_index = 0u;
    AnnotationPacket *packet = TryAcquireAnnotationPacket(&generation, &packet_index);
    if (packet == nullptr)
        return SGG_RD_PACKET_POOL_EXHAUSTED;

    packet->entry_count = entry_count;
    std::memcpy(packet->entries, entries, sizeof(SggRdAnnotationEntryV1) * entry_count);
    PublishAnnotationPacket(*packet, generation);
    if (g_graphics_epoch.load(std::memory_order_acquire) != graphics_epoch ||
        g_unity_d3d12.load(std::memory_order_acquire) == nullptr ||
        g_annotation_event_id.load(std::memory_order_acquire) < 0)
    {
        if (TryClaimAnnotationPacket(*packet, generation))
            FinishAnnotationPacket(*packet, generation);
        return SGG_RD_BACKEND_UNSUPPORTED;
    }
    g_packets_created.fetch_add(1u, std::memory_order_relaxed);
    *out_packet = MakeAnnotationPacketHandle(packet_index, generation);
    return SGG_RD_OK;
}

SggRdResult ReleaseAnnotationPacketImpl(void *raw_packet)
{
    AnnotationPacket *packet = nullptr;
    uint32_t generation = 0u;
    if (!ResolveAnnotationPacketHandle(raw_packet, &packet, &generation) ||
        !TryClaimAnnotationPacket(*packet, generation))
        return SGG_RD_INVALID_ARGUMENT;

    g_packets_dropped.fetch_add(1u, std::memory_order_relaxed);
    FinishAnnotationPacket(*packet, generation);
    return SGG_RD_OK;
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

void ConfigureD3D12AnnotationEvent(IUnityGraphicsD3D12v7 *d3d12)
{
    const int32_t event_id = g_annotation_event_id.load(std::memory_order_acquire);
    if (d3d12 == nullptr || event_id < 0 || d3d12->ConfigureEvent == nullptr)
        return;

    UnityD3D12PluginEventConfig config{};
    config.graphicsQueueAccess = kUnityD3D12GraphicsQueueAccess_DontCare;
    config.flags = 0u;
    config.ensureActiveRenderTextureIsBound = false;
    d3d12->ConfigureEvent(event_id, &config);
}

void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
{
    if (event_type == kUnityGfxDeviceEventShutdown || event_type == kUnityGfxDeviceEventBeforeReset)
    {
        g_graphics_epoch.fetch_add(1u, std::memory_order_acq_rel);
        g_unity_d3d12.store(nullptr, std::memory_order_release);
        g_graphics_renderer.store(static_cast<uint32_t>(kUnityGfxRendererNull),
                                  std::memory_order_release);
        DropAllocatedAnnotationPackets();
        return;
    }

    if ((event_type != kUnityGfxDeviceEventInitialize && event_type != kUnityGfxDeviceEventAfterReset) ||
        g_unity_graphics == nullptr || g_unity_interfaces == nullptr)
        return;

    const UnityGfxRenderer renderer = g_unity_graphics->GetRenderer();
    g_graphics_renderer.store(static_cast<uint32_t>(renderer), std::memory_order_release);
    if (renderer != kUnityGfxRendererD3D12)
    {
        g_unity_d3d12.store(nullptr, std::memory_order_release);
        return;
    }

    IUnityGraphicsD3D12v7 *d3d12 = g_unity_interfaces->Get<IUnityGraphicsD3D12v7>();
    g_unity_d3d12.store(d3d12, std::memory_order_release);
    ConfigureD3D12AnnotationEvent(d3d12);
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

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetAnnotationCapabilitiesV1(
    SggRdAnnotationCapabilitiesV1 *out_capabilities)
{
    try
    {
        return GetAnnotationCapabilitiesImpl(out_capabilities);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetAnnotationEventV1(
    void **out_callback, int32_t *out_event_id)
{
    try
    {
        return GetAnnotationEventImpl(out_callback, out_event_id);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_CreateAnnotationPacketV1(
    const SggRdAnnotationEntryV1 *entries, uint32_t entry_count, void **out_packet)
{
    try
    {
        return CreateAnnotationPacketImpl(entries, entry_count, out_packet);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" SGG_RD_API SggRdResult SGG_RD_CALL SggRd_ReleaseAnnotationPacketV1(
    void *packet)
{
    try
    {
        return ReleaseAnnotationPacketImpl(packet);
    }
    catch (...)
    {
        return SGG_RD_INTERNAL_ERROR;
    }
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(
    IUnityInterfaces *unity_interfaces)
{
    g_unity_interfaces = unity_interfaces;
    g_unity_graphics = unity_interfaces != nullptr ? unity_interfaces->Get<IUnityGraphics>() : nullptr;
    g_unity_plugin_loaded.store(g_unity_graphics != nullptr ? 1u : 0u, std::memory_order_release);
    if (g_unity_graphics == nullptr)
        return;

    const int reserved_event_id = g_unity_graphics->ReserveEventIDRange(1);
    g_annotation_event_id.store(reserved_event_id, std::memory_order_release);
    g_unity_graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    if (g_unity_graphics != nullptr)
        g_unity_graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventShutdown);
    g_annotation_event_id.store(-1, std::memory_order_release);
    g_unity_plugin_loaded.store(0u, std::memory_order_release);
    g_unity_graphics = nullptr;
    g_unity_interfaces = nullptr;
}

#ifdef SGG_RD_TESTING
extern "C" void SGG_RD_CALL SggRd_TestReset()
{
    std::lock_guard<std::mutex> lock(g_bridge_mutex);
    g_state = BridgeState{};
    g_test_module_present = 0u;
    g_test_get_api = nullptr;
    g_test_annotation_device = nullptr;
    g_test_annotation_command = nullptr;
    g_unity_plugin_loaded.store(0u, std::memory_order_release);
    g_graphics_renderer.store(static_cast<uint32_t>(kUnityGfxRendererNull),
                              std::memory_order_release);
    g_annotation_event_id.store(-1, std::memory_order_release);
    g_unity_d3d12.store(nullptr, std::memory_order_release);
    DropAllocatedAnnotationPackets();
    g_packets_in_use.store(0u, std::memory_order_relaxed);
    g_packets_created.store(0u, std::memory_order_relaxed);
    g_packets_executed.store(0u, std::memory_order_relaxed);
    g_packets_dropped.store(0u, std::memory_order_relaxed);
    g_annotation_calls.store(0u, std::memory_order_relaxed);
    g_annotation_errors.store(0u, std::memory_order_relaxed);
    g_graphics_epoch.store(1u, std::memory_order_release);
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

extern "C" void SGG_RD_CALL SggRd_TestSetAnnotationTarget(void *device, void *command,
                                                            int32_t event_id)
{
    g_test_annotation_device = device;
    g_test_annotation_command = command;
    g_unity_plugin_loaded.store(device != nullptr && command != nullptr ? 1u : 0u,
                                std::memory_order_release);
    g_graphics_renderer.store(device != nullptr && command != nullptr
                                  ? static_cast<uint32_t>(kUnityGfxRendererD3D12)
                                  : static_cast<uint32_t>(kUnityGfxRendererNull),
                              std::memory_order_release);
    g_annotation_event_id.store(event_id, std::memory_order_release);
    g_unity_d3d12.store(device != nullptr && command != nullptr
                            ? reinterpret_cast<IUnityGraphicsD3D12v7 *>(device)
                            : nullptr,
                        std::memory_order_release);
}

extern "C" void SGG_RD_CALL SggRd_TestExecuteAnnotationEvent(int32_t event_id, void *packet)
{
    OnAnnotationEvent(event_id, packet);
}
#endif
