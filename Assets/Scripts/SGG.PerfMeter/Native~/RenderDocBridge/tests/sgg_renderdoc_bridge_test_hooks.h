#ifndef SGG_RENDERDOC_BRIDGE_TEST_HOOKS_H
#define SGG_RENDERDOC_BRIDGE_TEST_HOOKS_H

#include <renderdoc_app.h>

#include "sgg_renderdoc_bridge.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifdef SGG_RD_TESTING
void SGG_RD_CALL SggRd_TestReset();
void SGG_RD_CALL SggRd_TestSetResolver(uint32_t module_present, pRENDERDOC_GetAPI get_api);
#endif

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* SGG_RENDERDOC_BRIDGE_TEST_HOOKS_H */
