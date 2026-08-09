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

uint32_t RENDERDOC_CC FakeSetCommandAnnotation(RENDERDOC_DevicePointer, void *, const char *,
                                               RENDERDOC_AnnotationType, uint32_t,
                                               const RENDERDOC_AnnotationValue *)
{
    return 0u;
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
        {"foreign_multiple_and_count_candidates", TestForeignMultipleAndCountCandidates},
        {"comments_authorization_and_limits", TestCommentsAuthorizationAndLimits},
        {"known_prefix_and_concurrent_rejection", TestKnownPrefixAndConcurrentRejection}};

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
