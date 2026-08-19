#include "sgg_renderdoc_bridge_test_hooks.h"

#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <cstdio>
#include <cstring>
#include <thread>
#include <atomic>

namespace
{
constexpr uint32_t kMaxPathBytes = SGG_RD_MAX_PATH_BYTES;
constexpr uint32_t kCaptureCapacity = 8u;
constexpr uint32_t kAnnotationCallCapacity = 128u;

struct FakeAnnotationCall
{
    void *device = nullptr;
    void *command = nullptr;
    char key[SGG_RD_MAX_ANNOTATION_KEY_BYTES_V1 + 1u]{};
    uint32_t value_type = 0u;
    uint32_t vector_width = 0u;
    uint64_t value_data[4]{};
    char string_value[SGG_RD_MAX_ANNOTATION_STRING_BYTES_V1 + 1u]{};
};

struct FakeState
{
    uint32_t supported_version = 0u;
    int actual_major = 1;
    int actual_minor = 4;
    int actual_patch = 0;
    RENDERDOC_API_1_7_0 table{};

    char current_template[kMaxPathBytes] = "C:\\old\\capture";
    char capture_paths[kCaptureCapacity][kMaxPathBytes]{};
    uint64_t capture_timestamps[kCaptureCapacity]{};
    uint32_t capture_count = 0u;

    uint32_t requested_versions[3]{};
    uint32_t requested_version_count = 0u;
    uint32_t start_calls = 0u;
    uint32_t end_calls = 0u;
    uint32_t discard_calls = 0u;
    uint32_t title_calls = 0u;
    uint32_t comments_calls = 0u;
    uint32_t annotation_calls = 0u;
    uint32_t annotation_result = 0u;
    uint32_t set_template_calls = 0u;
    uint32_t start_success = 1u;
    uint32_t start_reports_capturing = 1u;
    uint32_t capturing = 0u;
    uint32_t target_control_connected = 0u;
    uint32_t return_null_template = 0u;
    uint32_t end_success = 1u;
    uint32_t discard_success = 1u;
    void *start_device = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    void *start_window = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    void *end_device = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    void *end_window = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    void *discard_device = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    void *discard_window = reinterpret_cast<void *>(static_cast<uintptr_t>(1u));
    char comment_path[kMaxPathBytes]{};
    char comment_text[SGG_RD_MAX_COMMENTS_BYTES + 1u]{};
    FakeAnnotationCall annotation_log[kAnnotationCallCapacity]{};
};

FakeState g_fake;
std::atomic<uint32_t> g_block_start{0u};
std::atomic<uint32_t> g_start_entered{0u};
std::atomic<uint32_t> g_release_start{0u};

void CopyFixed(char *destination, uint32_t capacity, const char *source)
{
    const size_t length = std::strlen(source);
    if (length >= capacity)
        std::abort();
    std::memcpy(destination, source, length + 1u);
}

void ResetFake()
{
    g_fake = FakeState{};
    CopyFixed(g_fake.current_template, kMaxPathBytes, "C:\\old\\capture");
    g_block_start.store(0u);
    g_start_entered.store(0u);
    g_release_start.store(0u);
}

void RENDERDOC_CC FakeGetAPIVersion(int *major, int *minor, int *patch)
{
    if (major != nullptr)
        *major = g_fake.actual_major;
    if (minor != nullptr)
        *minor = g_fake.actual_minor;
    if (patch != nullptr)
        *patch = g_fake.actual_patch;
}

void RENDERDOC_CC FakeSetCapturePathTemplate(const char *capture_template)
{
    ++g_fake.set_template_calls;
    if (capture_template == nullptr)
        return;
    CopyFixed(g_fake.current_template, kMaxPathBytes, capture_template);
}

const char *RENDERDOC_CC FakeGetCapturePathTemplate()
{
    if (g_fake.return_null_template != 0u)
        return nullptr;
    return g_fake.current_template;
}

uint32_t RENDERDOC_CC FakeGetNumCaptures()
{
    return g_fake.capture_count;
}

uint32_t RENDERDOC_CC FakeIsTargetControlConnected()
{
    return g_fake.target_control_connected;
}

uint32_t RENDERDOC_CC FakeGetCapture(uint32_t index, char *filename, uint32_t *pathlength,
                                     uint64_t *timestamp)
{
    if (index >= g_fake.capture_count || index >= kCaptureCapacity)
        return 0u;

    const uint32_t path_bytes = static_cast<uint32_t>(std::strlen(g_fake.capture_paths[index]) + 1u);
    if (pathlength != nullptr)
        *pathlength = path_bytes;
    if (timestamp != nullptr)
        *timestamp = g_fake.capture_timestamps[index];
    if (filename != nullptr)
        std::memcpy(filename, g_fake.capture_paths[index], path_bytes);
    return 1u;
}

void RENDERDOC_CC FakeSetCaptureComments(const char *path, const char *comments)
{
    ++g_fake.comments_calls;
    CopyFixed(g_fake.comment_path, kMaxPathBytes, path == nullptr ? "" : path);
    CopyFixed(g_fake.comment_text, sizeof(g_fake.comment_text), comments == nullptr ? "" : comments);
}

void RENDERDOC_CC FakeStartFrameCapture(void *device, void *window)
{
    ++g_fake.start_calls;
    g_fake.start_device = device;
    g_fake.start_window = window;
    if (g_block_start.load() != 0u)
    {
        g_start_entered.store(1u);
        while (g_release_start.load() == 0u)
            std::this_thread::yield();
    }
    if (g_fake.start_success != 0u)
        g_fake.capturing = g_fake.start_reports_capturing;
}

uint32_t RENDERDOC_CC FakeIsFrameCapturing()
{
    return g_fake.capturing;
}

uint32_t RENDERDOC_CC FakeEndFrameCapture(void *device, void *window)
{
    ++g_fake.end_calls;
    g_fake.end_device = device;
    g_fake.end_window = window;
    if (g_fake.end_success == 0u)
        return 0u;
    g_fake.capturing = 0u;
    return 1u;
}

uint32_t RENDERDOC_CC FakeDiscardFrameCapture(void *device, void *window)
{
    ++g_fake.discard_calls;
    g_fake.discard_device = device;
    g_fake.discard_window = window;
    if (g_fake.discard_success == 0u)
        return 0u;
    g_fake.capturing = 0u;
    return 1u;
}

void RENDERDOC_CC FakeSetCaptureTitle(const char *)
{
    ++g_fake.title_calls;
}

uint32_t RENDERDOC_CC FakeSetObjectAnnotation(RENDERDOC_DevicePointer, void *, const char *,
                                              RENDERDOC_AnnotationType, uint32_t,
                                              const RENDERDOC_AnnotationValue *)
{
    return 0u;
}

uint32_t RENDERDOC_CC FakeSetCommandAnnotation(RENDERDOC_DevicePointer device, void *command,
                                               const char *key, RENDERDOC_AnnotationType value_type,
                                               uint32_t vector_width,
                                               const RENDERDOC_AnnotationValue *value)
{
    if (g_fake.annotation_calls < kAnnotationCallCapacity)
    {
        FakeAnnotationCall &call = g_fake.annotation_log[g_fake.annotation_calls];
        call.device = device;
        call.command = command;
        CopyFixed(call.key, sizeof(call.key), key == nullptr ? "" : key);
        call.value_type = static_cast<uint32_t>(value_type);
        call.vector_width = vector_width;
        if (value_type == eRENDERDOC_String && value != nullptr)
            CopyFixed(call.string_value, sizeof(call.string_value), value->string);
        else if (value != nullptr)
        {
            for (uint32_t index = 0u; index < vector_width && index < 4u; ++index)
            {
                switch (value_type)
                {
                case eRENDERDOC_Bool:
                    call.value_data[index] = value->vector.boolean[index] ? 1u : 0u;
                    break;
                case eRENDERDOC_Int32:
                    call.value_data[index] = static_cast<uint32_t>(value->vector.int32[index]);
                    break;
                case eRENDERDOC_UInt32:
                    call.value_data[index] = value->vector.uint32[index];
                    break;
                case eRENDERDOC_Int64:
                    call.value_data[index] = static_cast<uint64_t>(value->vector.int64[index]);
                    break;
                case eRENDERDOC_UInt64:
                    call.value_data[index] = value->vector.uint64[index];
                    break;
                case eRENDERDOC_Float:
                {
                    uint32_t bits = 0u;
                    std::memcpy(&bits, &value->vector.float32[index], sizeof(bits));
                    call.value_data[index] = bits;
                    break;
                }
                case eRENDERDOC_Double:
                    std::memcpy(&call.value_data[index], &value->vector.float64[index],
                                sizeof(call.value_data[index]));
                    break;
                default:
                    break;
                }
            }
        }
    }
    ++g_fake.annotation_calls;
    return g_fake.annotation_result;
}

int RENDERDOC_CC FakeGetAPI(RENDERDOC_Version version, void **out_api)
{
    if (g_fake.requested_version_count < 3u)
        g_fake.requested_versions[g_fake.requested_version_count++] = static_cast<uint32_t>(version);

    if (out_api == nullptr || static_cast<uint32_t>(version) > g_fake.supported_version ||
        static_cast<uint32_t>(version) < static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0))
        return 0;

    *out_api = &g_fake.table;
    return 1;
}

void SetupFakeTable(uint32_t version)
{
    g_fake.supported_version = version;
    g_fake.actual_minor = static_cast<int>((version % 10000u) / 100u);
    g_fake.actual_patch = static_cast<int>(version % 100u);
    g_fake.table = RENDERDOC_API_1_7_0{};
    g_fake.table.GetAPIVersion = FakeGetAPIVersion;
    g_fake.table.SetCaptureFilePathTemplate = FakeSetCapturePathTemplate;
    g_fake.table.GetCaptureFilePathTemplate = FakeGetCapturePathTemplate;
    g_fake.table.GetNumCaptures = FakeGetNumCaptures;
    g_fake.table.GetCapture = FakeGetCapture;
    g_fake.table.SetCaptureFileComments = FakeSetCaptureComments;
    g_fake.table.IsTargetControlConnected = FakeIsTargetControlConnected;
    g_fake.table.StartFrameCapture = FakeStartFrameCapture;
    g_fake.table.IsFrameCapturing = FakeIsFrameCapturing;
    g_fake.table.EndFrameCapture = FakeEndFrameCapture;
    g_fake.table.DiscardFrameCapture = FakeDiscardFrameCapture;
    if (version >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0))
        g_fake.table.SetCaptureTitle = FakeSetCaptureTitle;
    if (version >= static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0))
    {
        g_fake.table.SetObjectAnnotation = FakeSetObjectAnnotation;
        g_fake.table.SetCommandAnnotation = FakeSetCommandAnnotation;
    }
}

void ConfigureFake(uint32_t version)
{
    SggRd_TestReset();
    ResetFake();
    SetupFakeTable(version);
    SggRd_TestSetResolver(1u, FakeGetAPI);
}

void AddCapture(uint32_t index, const char *path, uint64_t timestamp)
{
    if (index >= kCaptureCapacity)
        std::abort();
    CopyFixed(g_fake.capture_paths[index], kMaxPathBytes, path);
    g_fake.capture_timestamps[index] = timestamp;
    if (g_fake.capture_count <= index)
        g_fake.capture_count = index + 1u;
}

SggRdAnnotationEntryV1 MakeAnnotationEntry(const char *key, uint32_t value_type,
                                           uint32_t vector_width)
{
    SggRdAnnotationEntryV1 entry{};
    entry.struct_size = sizeof(entry);
    entry.value_type = value_type;
    entry.vector_width = vector_width;
    entry.key_bytes = static_cast<uint32_t>(std::strlen(key));
    CopyFixed(entry.key, sizeof(entry.key), key);
    return entry;
}

SggRdAnnotationEntryV1 MakeStringAnnotationEntry(const char *key, const char *value)
{
    SggRdAnnotationEntryV1 entry =
        MakeAnnotationEntry(key, SGG_RD_ANNOTATION_STRING_V1, 1u);
    entry.string_bytes = static_cast<uint32_t>(std::strlen(value));
    CopyFixed(entry.string_value, sizeof(entry.string_value), value);
    return entry;
}

bool Check(bool condition, const char *expression, int line)
{
    if (!condition)
    {
        std::printf("FAIL line %d: %s\n", line, expression);
        return false;
    }
    return true;
}

#define CHECK(value) \
    do \
    { \
        if (!Check((value), #value, __LINE__)) \
            return false; \
    } while (0)

bool TestAbiAndResultValues()
{
    CHECK(sizeof(SggRdResult) == 4u);
    CHECK(sizeof(SggRdFeatureBitsV1) == 4u);
    CHECK(SGG_RD_OK == 0);
    CHECK(SGG_RD_NOT_LOADED == 1);
    CHECK(SGG_RD_EXPORT_MISSING == 2);
    CHECK(SGG_RD_API_NEGOTIATION_FAILED == 3);
    CHECK(SGG_RD_ALREADY_CAPTURING == 4);
    CHECK(SGG_RD_NOT_CAPTURING == 5);
    CHECK(SGG_RD_CAPTURE_FAILED == 6);
    CHECK(SGG_RD_CAPTURE_NOT_OBSERVED == 7);
    CHECK(SGG_RD_BUFFER_TOO_SMALL == 8);
    CHECK(SGG_RD_UNSUPPORTED_PLATFORM == 9);
    CHECK(SGG_RD_INVALID_ARGUMENT == 10);
    CHECK(SGG_RD_INTERNAL_ERROR == 11);
    CHECK(SGG_RD_ANNOTATIONS_UNAVAILABLE == 12);
    CHECK(SGG_RD_CAPTURE_INACTIVE == 13);
    CHECK(SGG_RD_BACKEND_UNSUPPORTED == 14);
    CHECK(SGG_RD_PACKET_POOL_EXHAUSTED == 15);
    CHECK(SGG_RD_ANNOTATION_REJECTED == 16);

    CHECK(alignof(SggRdCapabilitiesV1) == 4u);
    CHECK(sizeof(SggRdCapabilitiesV1) == 72u);
    CHECK(offsetof(SggRdCapabilitiesV1, struct_size) == 0u);
    CHECK(offsetof(SggRdCapabilitiesV1, bridge_abi_major) == 4u);
    CHECK(offsetof(SggRdCapabilitiesV1, bridge_abi_minor) == 8u);
    CHECK(offsetof(SggRdCapabilitiesV1, platform_supported) == 12u);
    CHECK(offsetof(SggRdCapabilitiesV1, module_loaded) == 16u);
    CHECK(offsetof(SggRdCapabilitiesV1, export_available) == 20u);
    CHECK(offsetof(SggRdCapabilitiesV1, api_negotiated) == 24u);
    CHECK(offsetof(SggRdCapabilitiesV1, target_control_connected) == 28u);
    CHECK(offsetof(SggRdCapabilitiesV1, is_capturing) == 32u);
    CHECK(offsetof(SggRdCapabilitiesV1, api_major) == 36u);
    CHECK(offsetof(SggRdCapabilitiesV1, api_minor) == 40u);
    CHECK(offsetof(SggRdCapabilitiesV1, api_patch) == 44u);
    CHECK(offsetof(SggRdCapabilitiesV1, feature_flags) == 48u);
    CHECK(offsetof(SggRdCapabilitiesV1, supports_discard) == 52u);
    CHECK(offsetof(SggRdCapabilitiesV1, supports_comments) == 56u);
    CHECK(offsetof(SggRdCapabilitiesV1, supports_title) == 60u);
    CHECK(offsetof(SggRdCapabilitiesV1, supports_annotations) == 64u);
    CHECK(offsetof(SggRdCapabilitiesV1, capture_count) == 68u);

    CHECK(alignof(SggRdCaptureTokenV1) == 8u);
    CHECK(sizeof(SggRdCaptureTokenV1) == 32u);
    CHECK(offsetof(SggRdCaptureTokenV1, struct_size) == 0u);
    CHECK(offsetof(SggRdCaptureTokenV1, reserved0) == 4u);
    CHECK(offsetof(SggRdCaptureTokenV1, request_nonce) == 8u);
    CHECK(offsetof(SggRdCaptureTokenV1, count_before) == 16u);
    CHECK(offsetof(SggRdCaptureTokenV1, reserved1) == 20u);
    CHECK(offsetof(SggRdCaptureTokenV1, start_unix_ns) == 24u);

    CHECK(alignof(SggRdArtifactV1) == 8u);
    CHECK(sizeof(SggRdArtifactV1) == 32u);
    CHECK(offsetof(SggRdArtifactV1, struct_size) == 0u);
    CHECK(offsetof(SggRdArtifactV1, index) == 4u);
    CHECK(offsetof(SggRdArtifactV1, renderdoc_timestamp_seconds) == 8u);
    CHECK(offsetof(SggRdArtifactV1, observed_unix_ns) == 16u);
    CHECK(offsetof(SggRdArtifactV1, required_path_bytes) == 24u);
    CHECK(offsetof(SggRdArtifactV1, reserved0) == 28u);

    CHECK(alignof(SggRdAnnotationCapabilitiesV1) == 4u);
    CHECK(sizeof(SggRdAnnotationCapabilitiesV1) == 88u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, struct_size) == 0u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, annotation_abi_major) == 4u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, is_capturing) == 48u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, event_id) == 56u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, packet_capacity) == 60u);
    CHECK(offsetof(SggRdAnnotationCapabilitiesV1, annotation_errors) == 84u);

    CHECK(alignof(SggRdAnnotationEntryV1) == 8u);
    CHECK(sizeof(SggRdAnnotationEntryV1) == 440u);
    CHECK(offsetof(SggRdAnnotationEntryV1, struct_size) == 0u);
    CHECK(offsetof(SggRdAnnotationEntryV1, value_type) == 4u);
    CHECK(offsetof(SggRdAnnotationEntryV1, vector_width) == 8u);
    CHECK(offsetof(SggRdAnnotationEntryV1, value_data) == 24u);
    CHECK(offsetof(SggRdAnnotationEntryV1, key) == 56u);
    CHECK(offsetof(SggRdAnnotationEntryV1, string_value) == 184u);
    return true;
}

bool TestMissingModuleAndExport()
{
    SggRd_TestReset();
    ResetFake();
    SetupFakeTable(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    SggRdCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    SggRd_TestSetResolver(0u, nullptr);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_NOT_LOADED);
    CHECK(capabilities.platform_supported == 1u);
    CHECK(capabilities.module_loaded == 0u);
    CHECK(capabilities.export_available == 0u);
    CHECK(capabilities.api_negotiated == 0u);

    SggRd_TestSetResolver(1u, nullptr);
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_EXPORT_MISSING);
    CHECK(capabilities.module_loaded == 1u);
    CHECK(capabilities.export_available == 0u);

    SggRd_TestSetResolver(1u, FakeGetAPI);
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.api_negotiated == 1u);
    return true;
}

bool TestNegotiationOrderAndFailure()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_3_0));
    SggRdCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_API_NEGOTIATION_FAILED);
    CHECK(g_fake.requested_version_count == 3u);
    CHECK(g_fake.requested_versions[0] == static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    CHECK(g_fake.requested_versions[1] == static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0));
    CHECK(g_fake.requested_versions[2] == static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0));

    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_API_NEGOTIATION_FAILED);
    CHECK(g_fake.requested_version_count == 3u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.actual_minor = 100;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_API_NEGOTIATION_FAILED);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.table.DiscardFrameCapture = nullptr;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_API_NEGOTIATION_FAILED);
    CHECK(g_fake.requested_version_count == 3u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.table.IsTargetControlConnected = nullptr;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_API_NEGOTIATION_FAILED);
    CHECK(g_fake.requested_version_count == 3u);
    return true;
}

bool TestCapabilitiesForSupportedVersions()
{
    const uint32_t versions[] = {
        static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0),
        static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0),
        static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0)};

    for (const uint32_t version : versions)
    {
        ConfigureFake(version);
        g_fake.target_control_connected = version != static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0)
                                              ? 1u
                                              : 0u;
        g_fake.capturing = version == static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0) ? 1u : 0u;
        SggRdCapabilitiesV1 capabilities{};
        capabilities.struct_size = sizeof(capabilities);
        CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_OK);
        CHECK(capabilities.module_loaded == 1u);
        CHECK(capabilities.export_available == 1u);
        CHECK(capabilities.api_negotiated == 1u);
        CHECK(capabilities.api_major == 1u);
        CHECK(capabilities.api_minor == (version % 10000u) / 100u);
        CHECK(capabilities.api_patch == version % 100u);
        CHECK(capabilities.target_control_connected == g_fake.target_control_connected);
        CHECK(capabilities.is_capturing == g_fake.capturing);
        CHECK(capabilities.supports_discard == 1u);
        CHECK(capabilities.supports_comments == 1u);
        CHECK(capabilities.supports_title == (version >= 10600u ? 1u : 0u));
        CHECK(capabilities.supports_annotations == (version >= 10700u ? 1u : 0u));
        CHECK(capabilities.capture_count == 0u);
    }
    return true;
}

bool TestAnnotationFeatureTruth()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    SggRdCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.supports_annotations == 1u);
    CHECK((capabilities.feature_flags & SGG_RD_FEATURE_ANNOTATIONS_V1) != 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.table.SetObjectAnnotation = nullptr;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.supports_annotations == 0u);
    CHECK((capabilities.feature_flags & SGG_RD_FEATURE_ANNOTATIONS_V1) == 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.table.SetCommandAnnotation = nullptr;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.supports_annotations == 0u);
    CHECK((capabilities.feature_flags & SGG_RD_FEATURE_ANNOTATIONS_V1) == 0u);
    return true;
}

bool BeginWithVersion(uint32_t version, SggRdCaptureTokenV1 *out_token)
{
    ConfigureFake(version);
    CHECK(SggRd_BeginCaptureV1(0x0102030405060708ULL, "C:\\captures\\nonce\\capture", 25u,
                               "title", 5u, out_token) == SGG_RD_OK);
    CHECK(out_token->request_nonce == 0x0102030405060708ULL);
    CHECK(out_token->count_before == 0u);
    CHECK(g_fake.start_device == nullptr);
    CHECK(g_fake.start_window == nullptr);
    CHECK(std::strcmp(g_fake.current_template, "C:\\captures\\nonce\\capture") == 0);
    return true;
}

bool TestBeginEndDiscardAndRestore()
{
    SggRdCaptureTokenV1 token{};
    token.struct_size = sizeof(token);
    CHECK(BeginWithVersion(static_cast<uint32_t>(eRENDERDOC_API_Version_1_4_0), &token));
    CHECK(g_fake.title_calls == 0u);
    CHECK(SggRd_EndCaptureV1(&token) == SGG_RD_OK);
    CHECK(g_fake.end_calls == 1u);
    CHECK(g_fake.end_device == nullptr);
    CHECK(g_fake.end_window == nullptr);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);

    CHECK(BeginWithVersion(static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0), &token));
    CHECK(g_fake.title_calls == 1u);
    CHECK(SggRd_DiscardCaptureV1(&token) == SGG_RD_OK);
    CHECK(g_fake.discard_calls == 1u);
    CHECK(g_fake.discard_device == nullptr);
    CHECK(g_fake.discard_window == nullptr);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);

    CHECK(BeginWithVersion(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0), &token));
    g_fake.end_success = 0u;
    CHECK(SggRd_EndCaptureV1(&token) == SGG_RD_CAPTURE_FAILED);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);
    g_fake.end_success = 1u;
    g_fake.discard_success = 0u;
    CHECK(SggRd_DiscardCaptureV1(&token) == SGG_RD_CAPTURE_FAILED);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);
    g_fake.discard_success = 1u;
    CHECK(SggRd_DiscardCaptureV1(&token) == SGG_RD_OK);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);
    return true;
}

bool TestBeginFailuresAndTokenOwnership()
{
    SggRdCaptureTokenV1 token{};
    token.struct_size = sizeof(token);
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0));
    g_fake.start_reports_capturing = 0u;
    CHECK(SggRd_BeginCaptureV1(11u, "C:\\captures\\nonce\\capture", 25u, "title", 5u, &token) ==
          SGG_RD_CAPTURE_FAILED);
    CHECK(std::strcmp(g_fake.current_template, "C:\\old\\capture") == 0);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0));
    g_fake.capturing = 1u;
    CHECK(SggRd_BeginCaptureV1(12u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_ALREADY_CAPTURING);
    CHECK(g_fake.set_template_calls == 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0));
    CHECK(SggRd_BeginCaptureV1(13u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_OK);
    SggRdCaptureTokenV1 wrong = token;
    wrong.request_nonce++;
    CHECK(SggRd_EndCaptureV1(&wrong) == SGG_RD_INVALID_ARGUMENT);
    CHECK(std::strcmp(g_fake.current_template, "C:\\captures\\nonce\\capture") == 0);
    CHECK(SggRd_DiscardCaptureV1(&token) == SGG_RD_OK);

    SggRdCaptureTokenV1 too_small{};
    too_small.struct_size = sizeof(too_small) - 1u;
    CHECK(SggRd_BeginCaptureV1(14u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u,
                               &too_small) == SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_BeginCaptureV1(0u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_BeginCaptureV1(15u, nullptr, 1u, nullptr, 0u, &token) == SGG_RD_INVALID_ARGUMENT);
    const char embedded_template[] = {'C', ':', '\\', 'x', '\0', 'y'};
    CHECK(SggRd_BeginCaptureV1(15u, embedded_template, 6u, nullptr, 0u, &token) ==
          SGG_RD_INVALID_ARGUMENT);
    const char invalid_utf8[] = {'C', ':', '\\', static_cast<char>(0xC0), static_cast<char>(0x80)};
    CHECK(SggRd_BeginCaptureV1(15u, invalid_utf8, sizeof(invalid_utf8), nullptr, 0u, &token) ==
          SGG_RD_INVALID_ARGUMENT);
    const char invalid_title[] = {static_cast<char>(0xE0), static_cast<char>(0x80), static_cast<char>(0x80)};
    CHECK(SggRd_BeginCaptureV1(15u, "C:\\captures\\nonce\\capture", 25u, invalid_title,
                               sizeof(invalid_title), &token) == SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_BeginCaptureV1(15u, "C:\\captures\\nonce\\capture", 25u, nullptr, 1u, &token) ==
          SGG_RD_INVALID_ARGUMENT);
    const char embedded_title[] = {'t', '\0', 'x'};
    CHECK(SggRd_BeginCaptureV1(15u, "C:\\captures\\nonce\\capture", 25u, embedded_title,
                               sizeof(embedded_title), &token) == SGG_RD_INVALID_ARGUMENT);

    char long_title[SGG_RD_MAX_TITLE_BYTES + 1u]{};
    std::memset(long_title, 'a', sizeof(long_title));
    CHECK(SggRd_BeginCaptureV1(15u, "C:\\captures\\nonce\\capture", 25u, long_title,
                               sizeof(long_title), &token) == SGG_RD_INVALID_ARGUMENT);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    CHECK(SggRd_BeginCaptureV1(16u, "capture", 7u, nullptr, 0u, &token) == SGG_RD_INVALID_ARGUMENT);
    CHECK(g_fake.set_template_calls == 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.return_null_template = 1u;
    CHECK(SggRd_BeginCaptureV1(17u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_CAPTURE_FAILED);
    CHECK(g_fake.set_template_calls == 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    std::memset(g_fake.current_template, 'x', sizeof(g_fake.current_template));
    CHECK(SggRd_BeginCaptureV1(18u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_CAPTURE_FAILED);
    CHECK(g_fake.set_template_calls == 0u);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    g_fake.current_template[0] = 'C';
    g_fake.current_template[1] = ':';
    g_fake.current_template[2] = '\\';
    g_fake.current_template[3] = static_cast<char>(0xC0);
    g_fake.current_template[4] = static_cast<char>(0x80);
    g_fake.current_template[5] = '\0';
    CHECK(SggRd_BeginCaptureV1(19u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &token) ==
          SGG_RD_CAPTURE_FAILED);
    CHECK(g_fake.set_template_calls == 0u);
    return true;
}

bool BeginAndEndForArtifact(SggRdCaptureTokenV1 *token)
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    token->struct_size = sizeof(*token);
    CHECK(SggRd_BeginCaptureV1(0xAABBCCDDu, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u,
                               token) == SGG_RD_OK);
    CHECK(SggRd_EndCaptureV1(token) == SGG_RD_OK);
    return true;
}

bool TestArtifactObservationAndBuffers()
{
    SggRdCaptureTokenV1 token{};
    CHECK(BeginAndEndForArtifact(&token));
    CHECK(g_fake.capture_count == 0u);

    SggRdArtifactV1 artifact{};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_NOT_OBSERVED);
    CHECK(artifact.required_path_bytes == 0u);

    AddCapture(0u, "C:\\captures\\nonce\\capture_frame42.rdc", 42u);
    char unchanged[64];
    std::memset(unchanged, 'X', sizeof(unchanged));
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_BUFFER_TOO_SMALL);
    CHECK(artifact.index == 0u);
    CHECK(artifact.renderdoc_timestamp_seconds == 42u);
    CHECK(artifact.required_path_bytes ==
          static_cast<uint32_t>(std::strlen("C:\\captures\\nonce\\capture_frame42.rdc") + 1u));

    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, unchanged, 5u) == SGG_RD_BUFFER_TOO_SMALL);
    for (const char value : unchanged)
        CHECK(value == 'X');

    char path[128];
    std::memset(path, 'Y', sizeof(path));
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    CHECK(std::strcmp(path, "C:\\captures\\nonce\\capture_frame42.rdc") == 0);
    CHECK(path[artifact.required_path_bytes] == 'Y');
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    return true;
}

bool TestArtifactReenumeratesAfterObservation()
{
    SggRdCaptureTokenV1 token{};
    CHECK(BeginAndEndForArtifact(&token));

    AddCapture(0u, "C:\\captures\\nonce\\first.rdc", 42u);
    SggRdArtifactV1 artifact{};
    artifact.struct_size = sizeof(artifact);
    char path[128]{};
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    const uint64_t first_observed_unix_ns = artifact.observed_unix_ns;
    CHECK(artifact.index == 0u);
    CHECK(std::strcmp(path, "C:\\captures\\nonce\\first.rdc") == 0);

    AddCapture(1u, "C:\\captures\\nonce\\second.rdc", 43u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_FAILED);
    CHECK(artifact.required_path_bytes == 0u);

    g_fake.capture_count = 1u;
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    std::memset(path, 0, sizeof(path));
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    CHECK(artifact.index == 0u);
    CHECK(artifact.observed_unix_ns == first_observed_unix_ns);
    CHECK(std::strcmp(path, "C:\\captures\\nonce\\first.rdc") == 0);

    g_fake.capture_timestamps[0] = 44u;
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_FAILED);

    CHECK(BeginAndEndForArtifact(&token));
    AddCapture(0u, "C:\\captures\\nonce\\original.rdc", 45u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    CopyFixed(g_fake.capture_paths[0], kMaxPathBytes, "C:\\captures\\nonce\\replacement.rdc");
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_FAILED);
    return true;
}

bool TestForeignMultipleAndCountCandidates()
{
    SggRdCaptureTokenV1 token{};
    CHECK(BeginAndEndForArtifact(&token));
    AddCapture(0u, "C:\\other\\foreign.rdc", 1u);
    SggRdArtifactV1 artifact{};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_NOT_OBSERVED);

    CHECK(BeginAndEndForArtifact(&token));
    AddCapture(0u, "C:\\captures\\nonce\\one.rdc", 2u);
    AddCapture(1u, "C:\\captures\\nonce\\two.rdc", 3u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_FAILED);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    AddCapture(0u, "C:\\captures\\previous\\capture.rdc", 10u);
    token = {};
    token.struct_size = sizeof(token);
    CHECK(SggRd_BeginCaptureV1(0x1111u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u,
                               &token) == SGG_RD_OK);
    CHECK(token.count_before == 1u);
    CHECK(SggRd_EndCaptureV1(&token) == SGG_RD_OK);
    AddCapture(1u, "C:\\captures\\nonce\\new.rdc", 11u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_BUFFER_TOO_SMALL);
    CHECK(artifact.index == 1u);

    CHECK(BeginAndEndForArtifact(&token));
    AddCapture(0u, "C:\\captures\\nonce\\one.rdc", 4u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_BUFFER_TOO_SMALL);

    CHECK(BeginAndEndForArtifact(&token));
    CHECK(g_fake.capture_count == 0u);
    artifact = {};
    artifact.struct_size = sizeof(artifact);
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, nullptr, 0u) == SGG_RD_CAPTURE_NOT_OBSERVED);
    return true;
}

bool TestCommentsAuthorizationAndLimits()
{
    SggRdCaptureTokenV1 token{};
    CHECK(BeginAndEndForArtifact(&token));
    const char observed_path[] = "C:\\captures\\nonce\\capture.rdc";
    AddCapture(0u, observed_path, 9u);
    SggRdArtifactV1 artifact{};
    artifact.struct_size = sizeof(artifact);
    char path[128]{};
    CHECK(SggRd_TryGetNewArtifactV1(&token, &artifact, path, sizeof(path)) == SGG_RD_OK);
    CHECK(SggRd_SetCaptureCommentsV1(&token, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), "hello", 5u) ==
          SGG_RD_OK);
    CHECK(g_fake.comments_calls == 1u);
    CHECK(std::strcmp(g_fake.comment_path, observed_path) == 0);
    CHECK(std::strcmp(g_fake.comment_text, "hello") == 0);

    CHECK(SggRd_SetCaptureCommentsV1(&token, "C:\\captures\\nonce\\other.rdc",
                                     static_cast<uint32_t>(std::strlen("C:\\captures\\nonce\\other.rdc")), "x", 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    SggRdCaptureTokenV1 wrong = token;
    wrong.count_before++;
    CHECK(SggRd_SetCaptureCommentsV1(&wrong, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), "x", 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    const char invalid[] = {static_cast<char>(0xC0), static_cast<char>(0x80)};
    CHECK(SggRd_SetCaptureCommentsV1(&token, invalid, sizeof(invalid), "x", 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    const char invalid_comments[] = {static_cast<char>(0xE0), static_cast<char>(0x80), static_cast<char>(0x80)};
    CHECK(SggRd_SetCaptureCommentsV1(&token, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), invalid_comments,
                                     sizeof(invalid_comments)) == SGG_RD_INVALID_ARGUMENT);
    const char embedded[] = {'C', ':', '\\', 'x', '\0', 'r', 'd', 'c'};
    CHECK(SggRd_SetCaptureCommentsV1(&token, embedded, sizeof(embedded), "x", 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_SetCaptureCommentsV1(&token, nullptr, 0u, "x", 1u) == SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_SetCaptureCommentsV1(&token, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), nullptr, 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    const char embedded_comments[] = {'x', '\0', 'y'};
    CHECK(SggRd_SetCaptureCommentsV1(&token, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), embedded_comments,
                                     sizeof(embedded_comments)) == SGG_RD_INVALID_ARGUMENT);

    char long_comments[SGG_RD_MAX_COMMENTS_BYTES + 1u]{};
    std::memset(long_comments, 'c', sizeof(long_comments));
    CHECK(SggRd_SetCaptureCommentsV1(&token, observed_path,
                                     static_cast<uint32_t>(std::strlen(observed_path)), long_comments,
                                     sizeof(long_comments)) == SGG_RD_INVALID_ARGUMENT);
    char long_path[kMaxPathBytes]{};
    std::memset(long_path, 'p', sizeof(long_path));
    CHECK(SggRd_SetCaptureCommentsV1(&token, long_path, kMaxPathBytes, "x", 1u) ==
          SGG_RD_INVALID_ARGUMENT);
    return true;
}

bool TestKnownPrefixAndConcurrentRejection()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    struct LargerCapabilities
    {
        SggRdCapabilitiesV1 value;
        uint8_t tail[16];
    } capabilities{};
    capabilities.value.struct_size = sizeof(capabilities);
    std::memset(capabilities.tail, 0xA5, sizeof(capabilities.tail));
    CHECK(SggRd_GetCapabilitiesV1(&capabilities.value) == SGG_RD_OK);
    CHECK(capabilities.value.struct_size == sizeof(capabilities));
    for (const uint8_t value : capabilities.tail)
        CHECK(value == 0xA5u);

    g_block_start.store(1u);
    g_release_start.store(0u);
    SggRdCaptureTokenV1 worker_token{};
    worker_token.struct_size = sizeof(worker_token);
    std::atomic<uint32_t> worker_result{static_cast<uint32_t>(SGG_RD_INTERNAL_ERROR)};
    std::thread worker([&]() {
        worker_result.store(static_cast<uint32_t>(SggRd_BeginCaptureV1(
            99u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u, &worker_token)));
    });
    while (g_start_entered.load() == 0u)
        std::this_thread::yield();

    SggRdCaptureTokenV1 second_token{};
    second_token.struct_size = sizeof(second_token);
    CHECK(SggRd_BeginCaptureV1(100u, "C:\\captures\\nonce\\capture", 25u, nullptr, 0u,
                               &second_token) == SGG_RD_INTERNAL_ERROR);
    g_release_start.store(1u);
    worker.join();
    CHECK(worker_result.load() == static_cast<uint32_t>(SGG_RD_OK));
    CHECK(SggRd_DiscardCaptureV1(&worker_token) == SGG_RD_OK);
    return true;
}

bool TestAnnotationCapabilitiesAndEvent()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    SggRdAnnotationCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_BACKEND_UNSUPPORTED);
    CHECK(capabilities.annotation_abi_major == SGG_RD_ANNOTATION_ABI_MAJOR_V1);
    CHECK(capabilities.packet_capacity == SGG_RD_MAX_ANNOTATION_PACKETS_V1);
    CHECK(capabilities.supports_annotations == 1u);
    CHECK(capabilities.backend_supported == 0u);

    void *device = reinterpret_cast<void *>(static_cast<uintptr_t>(0x1111u));
    void *command = reinterpret_cast<void *>(static_cast<uintptr_t>(0x2222u));
    SggRd_TestSetAnnotationTarget(device, command, 73);
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_CAPTURE_INACTIVE);
    CHECK(capabilities.unity_plugin_loaded == 1u);
    CHECK(capabilities.graphics_renderer == 18u);
    CHECK(capabilities.backend_supported == 1u);
    CHECK(capabilities.event_id == 73u);

    g_fake.capturing = 1u;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.is_capturing == 1u);
    void *callback = nullptr;
    int32_t event_id = -1;
    CHECK(SggRd_GetAnnotationEventV1(&callback, &event_id) == SGG_RD_OK);
    CHECK(callback != nullptr);
    CHECK(event_id == 73);

    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_6_0));
    SggRd_TestSetAnnotationTarget(device, command, 74);
    g_fake.capturing = 1u;
    capabilities = {};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_ANNOTATIONS_UNAVAILABLE);
    CHECK(capabilities.supports_annotations == 0u);
    return true;
}

bool TestAnnotationPacketLifecycleAndValues()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    void *device = reinterpret_cast<void *>(static_cast<uintptr_t>(0x1111u));
    void *command = reinterpret_cast<void *>(static_cast<uintptr_t>(0x2222u));
    SggRd_TestSetAnnotationTarget(device, command, 81);
    g_fake.capturing = 1u;

    SggRdAnnotationEntryV1 entries[4]{};
    entries[0] = MakeStringAnnotationEntry("SGG.Module", "com.sungeargames.sky");
    entries[1] = MakeAnnotationEntry("SGG.Weather.Command.Sequence",
                                     SGG_RD_ANNOTATION_UINT64_V1, 1u);
    entries[1].value_data[0] = 42u;
    entries[2] = MakeAnnotationEntry("SGG.Sky.Resolution", SGG_RD_ANNOTATION_FLOAT_V1, 2u);
    const float widths[2] = {1920.0f, 1080.0f};
    for (uint32_t index = 0u; index < 2u; ++index)
    {
        uint32_t bits = 0u;
        std::memcpy(&bits, &widths[index], sizeof(bits));
        entries[2].value_data[index] = bits;
    }
    entries[3] = MakeAnnotationEntry("SGG.RenderGraph.Pass", SGG_RD_ANNOTATION_EMPTY_V1, 0u);

    void *wrong_event_packet = nullptr;
    CHECK(SggRd_CreateAnnotationPacketV1(entries, 4u, &wrong_event_packet) == SGG_RD_OK);
    CHECK(wrong_event_packet != nullptr);
    SggRd_TestExecuteAnnotationEvent(80, wrong_event_packet);
    CHECK(g_fake.annotation_calls == 0u);

    void *packet = nullptr;
    CHECK(SggRd_CreateAnnotationPacketV1(entries, 4u, &packet) == SGG_RD_OK);
    CHECK(packet != nullptr);
    SggRd_TestExecuteAnnotationEvent(81, packet);
    CHECK(g_fake.annotation_calls == 4u);
    CHECK(g_fake.annotation_log[0].device == device);
    CHECK(g_fake.annotation_log[0].command == command);
    CHECK(std::strcmp(g_fake.annotation_log[0].key, "SGG.Module") == 0);
    CHECK(g_fake.annotation_log[0].value_type == static_cast<uint32_t>(eRENDERDOC_String));
    CHECK(std::strcmp(g_fake.annotation_log[0].string_value, "com.sungeargames.sky") == 0);
    CHECK(g_fake.annotation_log[1].value_data[0] == 42u);
    CHECK(g_fake.annotation_log[2].vector_width == 2u);
    CHECK(g_fake.annotation_log[2].value_data[0] == entries[2].value_data[0]);
    CHECK(g_fake.annotation_log[2].value_data[1] == entries[2].value_data[1]);
    CHECK(g_fake.annotation_log[3].value_type == static_cast<uint32_t>(eRENDERDOC_Empty));

    SggRdAnnotationCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.packets_in_use == 0u);
    CHECK(capabilities.packets_created == 2u);
    CHECK(capabilities.packets_executed == 1u);
    CHECK(capabilities.packets_dropped == 1u);
    CHECK(capabilities.annotation_calls == 4u);
    CHECK(capabilities.annotation_errors == 0u);
    return true;
}

bool TestAnnotationAllNumericMappings()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    void *device = reinterpret_cast<void *>(static_cast<uintptr_t>(0x5111u));
    void *command = reinterpret_cast<void *>(static_cast<uintptr_t>(0x5222u));
    SggRd_TestSetAnnotationTarget(device, command, 86);
    g_fake.capturing = 1u;

    SggRdAnnotationEntryV1 entries[8]{};
    entries[0] = MakeAnnotationEntry("SGG.Bool", SGG_RD_ANNOTATION_BOOL_V1, 4u);
    entries[0].value_data[0] = 1u;
    entries[0].value_data[2] = 1u;
    entries[0].value_data[3] = 1u;
    entries[1] = MakeAnnotationEntry("SGG.Int32", SGG_RD_ANNOTATION_INT32_V1, 2u);
    entries[1].value_data[0] = 0xFFFFFFFFu;
    entries[1].value_data[1] = 0x80000000u;
    entries[2] = MakeAnnotationEntry("SGG.UInt32", SGG_RD_ANNOTATION_UINT32_V1, 1u);
    entries[2].value_data[0] = 0xFEDCBA98u;
    entries[3] = MakeAnnotationEntry("SGG.Int64", SGG_RD_ANNOTATION_INT64_V1, 1u);
    entries[3].value_data[0] = static_cast<uint64_t>(-123456789LL);
    entries[4] = MakeAnnotationEntry("SGG.UInt64", SGG_RD_ANNOTATION_UINT64_V1, 1u);
    entries[4].value_data[0] = 0xFEDCBA9876543210ULL;
    entries[5] = MakeAnnotationEntry("SGG.Float", SGG_RD_ANNOTATION_FLOAT_V1, 2u);
    const float float_values[2] = {-3.5f, 17.25f};
    for (uint32_t index = 0u; index < 2u; ++index)
    {
        uint32_t bits = 0u;
        std::memcpy(&bits, &float_values[index], sizeof(bits));
        entries[5].value_data[index] = bits;
    }
    entries[6] = MakeAnnotationEntry("SGG.Double", SGG_RD_ANNOTATION_DOUBLE_V1, 2u);
    const double double_values[2] = {-0.125, 9007199254740992.0};
    for (uint32_t index = 0u; index < 2u; ++index)
        std::memcpy(&entries[6].value_data[index], &double_values[index], sizeof(double_values[index]));
    entries[7] = MakeStringAnnotationEntry("SGG.String", "typed");

    void *packet = nullptr;
    CHECK(SggRd_CreateAnnotationPacketV1(entries, 8u, &packet) == SGG_RD_OK);
    SggRd_TestExecuteAnnotationEvent(86, packet);
    CHECK(g_fake.annotation_calls == 8u);
    for (uint32_t index = 0u; index < 8u; ++index)
    {
        CHECK(g_fake.annotation_log[index].value_type == entries[index].value_type);
        const uint32_t expected_width =
            entries[index].value_type == SGG_RD_ANNOTATION_STRING_V1 ? 0u : entries[index].vector_width;
        CHECK(g_fake.annotation_log[index].vector_width == expected_width);
    }
    for (uint32_t entry_index = 0u; entry_index < 7u; ++entry_index)
    {
        for (uint32_t lane = 0u; lane < entries[entry_index].vector_width; ++lane)
            CHECK(g_fake.annotation_log[entry_index].value_data[lane] ==
                  entries[entry_index].value_data[lane]);
    }
    CHECK(std::strcmp(g_fake.annotation_log[7].string_value, "typed") == 0);
    return true;
}

bool TestAnnotationValidationPoolAndRejection()
{
    ConfigureFake(static_cast<uint32_t>(eRENDERDOC_API_Version_1_7_0));
    void *device = reinterpret_cast<void *>(static_cast<uintptr_t>(0x3333u));
    void *command = reinterpret_cast<void *>(static_cast<uintptr_t>(0x4444u));
    SggRd_TestSetAnnotationTarget(device, command, 91);
    g_fake.capturing = 1u;

    SggRdAnnotationEntryV1 invalid_key =
        MakeAnnotationEntry("SGG.Bad Key", SGG_RD_ANNOTATION_UINT32_V1, 1u);
    void *packet = nullptr;
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_key, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);
    SggRdAnnotationEntryV1 invalid_stride =
        MakeAnnotationEntry("SGG.Stride", SGG_RD_ANNOTATION_UINT32_V1, 1u);
    invalid_stride.struct_size = sizeof(invalid_stride) + 8u;
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_stride, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);
    SggRdAnnotationEntryV1 invalid_bool =
        MakeAnnotationEntry("SGG.Bool", SGG_RD_ANNOTATION_BOOL_V1, 1u);
    invalid_bool.value_data[0] = 2u;
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_bool, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);
    SggRdAnnotationEntryV1 invalid_unused =
        MakeAnnotationEntry("SGG.Unused", SGG_RD_ANNOTATION_UINT64_V1, 1u);
    invalid_unused.value_data[1] = 3u;
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_unused, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);
    SggRdAnnotationEntryV1 invalid_high_bits =
        MakeAnnotationEntry("SGG.HighBits", SGG_RD_ANNOTATION_UINT32_V1, 1u);
    invalid_high_bits.value_data[0] = 1ULL << 40u;
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_high_bits, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);
    SggRdAnnotationEntryV1 invalid_string =
        MakeAnnotationEntry("SGG.String", SGG_RD_ANNOTATION_STRING_V1, 1u);
    invalid_string.string_bytes = 2u;
    invalid_string.string_value[0] = static_cast<char>(0xC0);
    invalid_string.string_value[1] = static_cast<char>(0x80);
    CHECK(SggRd_CreateAnnotationPacketV1(&invalid_string, 1u, &packet) == SGG_RD_INVALID_ARGUMENT);

    SggRdAnnotationEntryV1 valid =
        MakeAnnotationEntry("SGG.Value", SGG_RD_ANNOTATION_UINT32_V1, 1u);
    valid.value_data[0] = 7u;
    void *packets[SGG_RD_MAX_ANNOTATION_PACKETS_V1]{};
    for (uint32_t index = 0u; index < SGG_RD_MAX_ANNOTATION_PACKETS_V1; ++index)
        CHECK(SggRd_CreateAnnotationPacketV1(&valid, 1u, &packets[index]) == SGG_RD_OK);
    SggRdAnnotationCapabilitiesV1 full_capabilities{};
    full_capabilities.struct_size = sizeof(full_capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&full_capabilities) == SGG_RD_PACKET_POOL_EXHAUSTED);
    CHECK(full_capabilities.packets_in_use == SGG_RD_MAX_ANNOTATION_PACKETS_V1);
    CHECK(SggRd_CreateAnnotationPacketV1(&valid, 1u, &packet) == SGG_RD_PACKET_POOL_EXHAUSTED);
    for (void *allocated : packets)
        CHECK(SggRd_ReleaseAnnotationPacketV1(allocated) == SGG_RD_OK);
    CHECK(SggRd_ReleaseAnnotationPacketV1(packets[0]) == SGG_RD_INVALID_ARGUMENT);

    void *stale_packet = nullptr;
    void *replacement_packet = nullptr;
    CHECK(SggRd_CreateAnnotationPacketV1(&valid, 1u, &stale_packet) == SGG_RD_OK);
    CHECK(SggRd_ReleaseAnnotationPacketV1(stale_packet) == SGG_RD_OK);
    CHECK(SggRd_CreateAnnotationPacketV1(&valid, 1u, &replacement_packet) == SGG_RD_OK);
    CHECK(replacement_packet != stale_packet);
    CHECK(SggRd_ReleaseAnnotationPacketV1(stale_packet) == SGG_RD_INVALID_ARGUMENT);
    CHECK(SggRd_ReleaseAnnotationPacketV1(replacement_packet) == SGG_RD_OK);

    g_fake.annotation_result = 2u;
    CHECK(SggRd_CreateAnnotationPacketV1(&valid, 1u, &packet) == SGG_RD_OK);
    SggRd_TestExecuteAnnotationEvent(91, packet);
    SggRdAnnotationCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    CHECK(SggRd_GetAnnotationCapabilitiesV1(&capabilities) == SGG_RD_OK);
    CHECK(capabilities.packets_in_use == 0u);
    CHECK(capabilities.annotation_calls == 1u);
    CHECK(capabilities.annotation_errors == 1u);
    CHECK(capabilities.packets_dropped == SGG_RD_MAX_ANNOTATION_PACKETS_V1 + 3u);
    return true;
}

bool RunTest(const char *name, bool (*test)())
{
    const bool passed = test();
    std::printf("%s: %s\n", name, passed ? "PASS" : "FAIL");
    return passed;
}
} // namespace

int main()
{
    uint32_t passed = 0u;
    uint32_t failed = 0u;
    const struct TestCase
    {
        const char *name;
        bool (*test)();
    } tests[] = {
        {"abi_and_result_values", TestAbiAndResultValues},
        {"missing_module_and_export", TestMissingModuleAndExport},
        {"negotiation_order_and_failure", TestNegotiationOrderAndFailure},
        {"capabilities_for_supported_versions", TestCapabilitiesForSupportedVersions},
        {"annotation_feature_truth", TestAnnotationFeatureTruth},
        {"begin_end_discard_restore", TestBeginEndDiscardAndRestore},
        {"begin_failures_and_token_ownership", TestBeginFailuresAndTokenOwnership},
        {"artifact_observation_and_buffers", TestArtifactObservationAndBuffers},
        {"artifact_reenumerates_after_observation", TestArtifactReenumeratesAfterObservation},
        {"foreign_multiple_and_count_candidates", TestForeignMultipleAndCountCandidates},
        {"comments_authorization_and_limits", TestCommentsAuthorizationAndLimits},
        {"known_prefix_and_concurrent_rejection", TestKnownPrefixAndConcurrentRejection},
        {"annotation_capabilities_and_event", TestAnnotationCapabilitiesAndEvent},
        {"annotation_packet_lifecycle_and_values", TestAnnotationPacketLifecycleAndValues},
        {"annotation_all_numeric_mappings", TestAnnotationAllNumericMappings},
        {"annotation_validation_pool_and_rejection", TestAnnotationValidationPoolAndRejection}};

    for (const TestCase &test : tests)
    {
        if (RunTest(test.name, test.test))
            ++passed;
        else
            ++failed;
    }

    std::printf("Tests: %u passed, %u failed\n", passed, failed);
    return failed == 0u ? 0 : 1;
}
