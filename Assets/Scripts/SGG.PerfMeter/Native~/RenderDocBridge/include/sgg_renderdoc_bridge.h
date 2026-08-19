#ifndef SGG_RENDERDOC_BRIDGE_H
#define SGG_RENDERDOC_BRIDGE_H

#include <stdint.h>

#if defined(_WIN32)
#  if defined(SGG_RD_BUILD_DLL)
#    define SGG_RD_API __declspec(dllexport)
#  else
#    define SGG_RD_API __declspec(dllimport)
#  endif
#  define SGG_RD_CALL __cdecl
#else
#  define SGG_RD_API
#  define SGG_RD_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define SGG_RD_ABI_MAJOR_V1 1u
#define SGG_RD_ABI_MINOR_V1 0u
#define SGG_RD_MAX_TITLE_BYTES 256u
#define SGG_RD_MAX_COMMENTS_BYTES 1024u
#define SGG_RD_MAX_PATH_BYTES 32768u
#define SGG_RD_ANNOTATION_ABI_MAJOR_V1 1u
#define SGG_RD_ANNOTATION_ABI_MINOR_V1 0u
#define SGG_RD_MAX_ANNOTATION_ENTRIES_V1 32u
#define SGG_RD_MAX_ANNOTATION_PACKETS_V1 64u
#define SGG_RD_MAX_ANNOTATION_KEY_BYTES_V1 127u
#define SGG_RD_MAX_ANNOTATION_STRING_BYTES_V1 255u

typedef uint32_t SggRdResult;

typedef enum SggRdResultValue
{
    SGG_RD_OK = 0,
    SGG_RD_NOT_LOADED = 1,
    SGG_RD_EXPORT_MISSING = 2,
    SGG_RD_API_NEGOTIATION_FAILED = 3,
    SGG_RD_ALREADY_CAPTURING = 4,
    SGG_RD_NOT_CAPTURING = 5,
    SGG_RD_CAPTURE_FAILED = 6,
    SGG_RD_CAPTURE_NOT_OBSERVED = 7,
    SGG_RD_BUFFER_TOO_SMALL = 8,
    SGG_RD_UNSUPPORTED_PLATFORM = 9,
    SGG_RD_INVALID_ARGUMENT = 10,
    SGG_RD_INTERNAL_ERROR = 11,
    SGG_RD_ANNOTATIONS_UNAVAILABLE = 12,
    SGG_RD_CAPTURE_INACTIVE = 13,
    SGG_RD_BACKEND_UNSUPPORTED = 14,
    SGG_RD_PACKET_POOL_EXHAUSTED = 15,
    SGG_RD_ANNOTATION_REJECTED = 16
} SggRdResultValue;

typedef uint32_t SggRdFeatureBitsV1;

typedef enum SggRdFeatureBitsV1Value
{
    SGG_RD_FEATURE_DISCARD_V1 = 1u << 0,
    SGG_RD_FEATURE_COMMENTS_V1 = 1u << 1,
    SGG_RD_FEATURE_TITLE_V1 = 1u << 2,
    SGG_RD_FEATURE_ANNOTATIONS_V1 = 1u << 3
} SggRdFeatureBitsV1Value;

typedef uint32_t SggRdAnnotationTypeV1;

typedef enum SggRdAnnotationTypeV1Value
{
    SGG_RD_ANNOTATION_EMPTY_V1 = 0,
    SGG_RD_ANNOTATION_BOOL_V1 = 1,
    SGG_RD_ANNOTATION_INT32_V1 = 2,
    SGG_RD_ANNOTATION_UINT32_V1 = 3,
    SGG_RD_ANNOTATION_INT64_V1 = 4,
    SGG_RD_ANNOTATION_UINT64_V1 = 5,
    SGG_RD_ANNOTATION_FLOAT_V1 = 6,
    SGG_RD_ANNOTATION_DOUBLE_V1 = 7,
    SGG_RD_ANNOTATION_STRING_V1 = 8
} SggRdAnnotationTypeV1Value;

#pragma pack(push, 8)

typedef struct SggRdCapabilitiesV1
{
    uint32_t struct_size;
    uint32_t bridge_abi_major;
    uint32_t bridge_abi_minor;
    uint32_t platform_supported;
    uint32_t module_loaded;
    uint32_t export_available;
    uint32_t api_negotiated;
    uint32_t target_control_connected;
    uint32_t is_capturing;
    uint32_t api_major;
    uint32_t api_minor;
    uint32_t api_patch;
    uint32_t feature_flags;
    uint32_t supports_discard;
    uint32_t supports_comments;
    uint32_t supports_title;
    uint32_t supports_annotations;
    uint32_t capture_count;
} SggRdCapabilitiesV1;

typedef struct SggRdCaptureTokenV1
{
    uint32_t struct_size;
    uint32_t reserved0;
    uint64_t request_nonce;
    uint32_t count_before;
    uint32_t reserved1;
    uint64_t start_unix_ns;
} SggRdCaptureTokenV1;

typedef struct SggRdArtifactV1
{
    uint32_t struct_size;
    uint32_t index;
    uint64_t renderdoc_timestamp_seconds;
    uint64_t observed_unix_ns;
    uint32_t required_path_bytes;
    uint32_t reserved0;
} SggRdArtifactV1;

typedef struct SggRdAnnotationCapabilitiesV1
{
    uint32_t struct_size;
    uint32_t annotation_abi_major;
    uint32_t annotation_abi_minor;
    uint32_t unity_plugin_loaded;
    uint32_t graphics_renderer;
    uint32_t backend_supported;
    uint32_t module_loaded;
    uint32_t api_negotiated;
    uint32_t supports_annotations;
    uint32_t renderdoc_api_major;
    uint32_t renderdoc_api_minor;
    uint32_t renderdoc_api_patch;
    uint32_t is_capturing;
    uint32_t event_id_valid;
    uint32_t event_id;
    uint32_t packet_capacity;
    uint32_t packets_in_use;
    uint32_t packets_created;
    uint32_t packets_executed;
    uint32_t packets_dropped;
    uint32_t annotation_calls;
    uint32_t annotation_errors;
} SggRdAnnotationCapabilitiesV1;

typedef struct SggRdAnnotationEntryV1
{
    uint32_t struct_size;
    uint32_t value_type;
    uint32_t vector_width;
    uint32_t key_bytes;
    uint32_t string_bytes;
    uint32_t reserved0;
    uint64_t value_data[4];
    char key[SGG_RD_MAX_ANNOTATION_KEY_BYTES_V1 + 1u];
    char string_value[SGG_RD_MAX_ANNOTATION_STRING_BYTES_V1 + 1u];
} SggRdAnnotationEntryV1;

#pragma pack(pop)

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetCapabilitiesV1(
    SggRdCapabilitiesV1 *out_capabilities);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_BeginCaptureV1(
    uint64_t request_nonce,
    const char *capture_path_template,
    uint32_t capture_path_template_bytes,
    const char *title,
    uint32_t title_bytes,
    SggRdCaptureTokenV1 *out_token);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_EndCaptureV1(
    const SggRdCaptureTokenV1 *token);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_DiscardCaptureV1(
    const SggRdCaptureTokenV1 *token);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_TryGetNewArtifactV1(
    const SggRdCaptureTokenV1 *token,
    SggRdArtifactV1 *out_artifact,
    char *path_buffer,
    uint32_t path_buffer_bytes);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_SetCaptureCommentsV1(
    const SggRdCaptureTokenV1 *token,
    const char *observed_path,
    uint32_t observed_path_bytes,
    const char *comments,
    uint32_t comments_bytes);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetAnnotationCapabilitiesV1(
    SggRdAnnotationCapabilitiesV1 *out_capabilities);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_GetAnnotationEventV1(
    void **out_callback,
    int32_t *out_event_id);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_CreateAnnotationPacketV1(
    const SggRdAnnotationEntryV1 *entries,
    uint32_t entry_count,
    void **out_packet);

SGG_RD_API SggRdResult SGG_RD_CALL SggRd_ReleaseAnnotationPacketV1(
    void *packet);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* SGG_RENDERDOC_BRIDGE_H */
