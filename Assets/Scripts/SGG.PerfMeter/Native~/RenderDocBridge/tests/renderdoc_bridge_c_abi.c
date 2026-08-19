#include "sgg_renderdoc_bridge.h"

#include <stddef.h>

#define SGG_RD_C_ASSERT(name, expression) typedef char name[(expression) ? 1 : -1]

SGG_RD_C_ASSERT(sgg_rd_result_is_u32, sizeof(SggRdResult) == 4u);
SGG_RD_C_ASSERT(sgg_rd_capabilities_size_v1, sizeof(SggRdCapabilitiesV1) == 72u);
SGG_RD_C_ASSERT(sgg_rd_token_size_v1, sizeof(SggRdCaptureTokenV1) == 32u);
SGG_RD_C_ASSERT(sgg_rd_artifact_size_v1, sizeof(SggRdArtifactV1) == 32u);
SGG_RD_C_ASSERT(sgg_rd_annotation_capabilities_size_v1,
                sizeof(SggRdAnnotationCapabilitiesV1) == 88u);
SGG_RD_C_ASSERT(sgg_rd_annotation_entry_size_v1, sizeof(SggRdAnnotationEntryV1) == 440u);
SGG_RD_C_ASSERT(sgg_rd_annotation_value_data_offset_v1,
                offsetof(SggRdAnnotationEntryV1, value_data) == 24u);
SGG_RD_C_ASSERT(sgg_rd_annotation_key_offset_v1,
                offsetof(SggRdAnnotationEntryV1, key) == 56u);
SGG_RD_C_ASSERT(sgg_rd_annotation_string_offset_v1,
                offsetof(SggRdAnnotationEntryV1, string_value) == 184u);
SGG_RD_C_ASSERT(sgg_rd_token_nonce_offset_v1,
                offsetof(SggRdCaptureTokenV1, request_nonce) == 8u);
SGG_RD_C_ASSERT(sgg_rd_artifact_timestamp_offset_v1,
                offsetof(SggRdArtifactV1, renderdoc_timestamp_seconds) == 8u);

void SggRd_CAbiCompileProbe(void)
{
    SggRdResult(SGG_RD_CALL *get_capabilities)(SggRdCapabilitiesV1 *) =
        &SggRd_GetCapabilitiesV1;
    SggRdResult(SGG_RD_CALL *begin_capture)(uint64_t, const char *, uint32_t, const char *,
                                            uint32_t, SggRdCaptureTokenV1 *) =
        &SggRd_BeginCaptureV1;
    SggRdResult(SGG_RD_CALL *get_annotation_capabilities)(SggRdAnnotationCapabilitiesV1 *) =
        &SggRd_GetAnnotationCapabilitiesV1;
    SggRdResult(SGG_RD_CALL *create_annotation_packet)(const SggRdAnnotationEntryV1 *,
                                                       uint32_t, void **) =
        &SggRd_CreateAnnotationPacketV1;
    SggRdResult(SGG_RD_CALL *get_annotation_event)(void **, int32_t *) =
        &SggRd_GetAnnotationEventV1;
    SggRdResult(SGG_RD_CALL *release_annotation_packet)(void *) =
        &SggRd_ReleaseAnnotationPacketV1;

    (void)get_capabilities;
    (void)begin_capture;
    (void)get_annotation_capabilities;
    (void)create_annotation_packet;
    (void)get_annotation_event;
    (void)release_annotation_packet;
}
