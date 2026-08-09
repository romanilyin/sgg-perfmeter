#include "sgg_renderdoc_bridge.h"

#include <cstdio>
#include <cstring>

int main(int argc, char **argv)
{
    const bool expect_loaded = argc == 2 && std::strcmp(argv[1], "--expect-loaded") == 0;
    if (argc > 2 || (argc == 2 && !expect_loaded))
    {
        std::fprintf(stderr, "usage: sgg_renderdoc_bridge_live_probe [--expect-loaded]\n");
        return 2;
    }

    SggRdCapabilitiesV1 capabilities{};
    capabilities.struct_size = sizeof(capabilities);
    const SggRdResult result = SggRd_GetCapabilitiesV1(&capabilities);

    std::printf(
        "result=%u module_loaded=%u export_available=%u api_negotiated=%u api=%u.%u.%u "
        "discard=%u comments=%u title=%u annotations=%u target_control=%u capturing=%u "
        "capture_count=%u\n",
        result, capabilities.module_loaded, capabilities.export_available,
        capabilities.api_negotiated, capabilities.api_major, capabilities.api_minor,
        capabilities.api_patch, capabilities.supports_discard, capabilities.supports_comments,
        capabilities.supports_title, capabilities.supports_annotations,
        capabilities.target_control_connected, capabilities.is_capturing,
        capabilities.capture_count);

    if (!expect_loaded)
        return result == SGG_RD_NOT_LOADED || result == SGG_RD_OK ? 0 : 1;

    return result == SGG_RD_OK && capabilities.module_loaded == 1u &&
                   capabilities.export_available == 1u && capabilities.api_negotiated == 1u &&
                   capabilities.api_major == 1u && capabilities.supports_discard == 1u &&
                   capabilities.supports_comments == 1u
               ? 0
               : 1;
}
