# SGG PerfMeter RenderDoc Bridge

This directory contains the `PM-RDOC-002` Windows x64 source bridge used by the
optional managed RenderDoc backend. The UPM package remains binary-free. A
release may publish the bridge as a separate verified artifact for project-local
Editor-only installation. The bridge never loads or injects RenderDoc;
production resolution accepts only an already-loaded `renderdoc.dll`. The next
bridge artifact also carries the additive GPU command-annotation ABI and Unity
native-plugin lifecycle; the currently published `2026.8.11-1` artifact exposes
only the capture ABI.

## Build And Test

Run from an x64 Visual Studio developer environment and keep the build directory
outside the package source:

```powershell
cmake -S <bridge-source> -B <build-directory> -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=ON -DSGG_RD_ARTIFACT_VERSION=<package-version> -DSGG_RD_FILE_VERSION=<year>,<month>,<day>,<revision> -DSGG_UNITY_PLUGIN_API_DIR=<Unity-Editor-Data-PluginAPI>
cmake --build <build-directory>
ctest --test-dir <build-directory> --output-on-failure
```

CMake downloads `renderdoc_app.h` from the exact revision recorded in
`ThirdPartyNotices.md` and rejects any SHA-256 mismatch. Release builds use the
static MSVC runtime and embed the requested artifact version in the PE resource.

## Package Release Artifact

From a clean release commit, create the deterministic Windows x64 ZIP and
checksum from the package root:

```powershell
./package-release.ps1 -PackageRoot <package-root> -BuildDirectory <new-empty-build-directory> -OutputDirectory <output-directory> -PackageVersion <package-version> -SourceCommit <full-git-head> -UnityPluginApiDirectory <Unity-Editor-Data-PluginAPI>
```

The packager clean-builds the configured Release/MSVC x64 tree, runs CTest, and
verifies the package/CMake/PE versions, exports and static-runtime imports before
writing the artifact. The archive contains only `sgg_renderdoc_bridge.dll`, an
artifact manifest and the required package/third-party notices. It never
contains RenderDoc binaries.

## Annotation ABI And Transport

Four additive exports expose annotation capabilities, the Unity rendering event,
bounded packet creation, and packet release: `SggRd_GetAnnotationCapabilitiesV1`,
`SggRd_GetAnnotationEventV1`, `SggRd_CreateAnnotationPacketV1`, and
`SggRd_ReleaseAnnotationPacketV1`. Missing exports are reported to managed code
as `BridgeTooOld` and do not affect the existing capture ABI.

The bridge copies each batch into one of 64 fixed generation-safe packets before
Unity records `IssuePluginEventAndData`. The render-thread callback consumes the
opaque packet handle once and never retains managed memory. The initial transport
is Windows x64 Unity Editor/D3D12 only and requires RenderDoc App API `1.7` plus
an active capture. D3D11, Vulkan, Players, and resource/object annotations remain
separate future validation gates.

## Live Resolver Probe

The optional probe verifies the production already-loaded-module path without
starting a frame capture:

```powershell
<build-directory>\sgg_renderdoc_bridge_live_probe.exe
renderdoccmd capture --wait-for-exit <build-directory>\sgg_renderdoc_bridge_live_probe.exe --expect-loaded
```

The first command should report `SGG_RD_NOT_LOADED` outside RenderDoc. The second
uses RenderDoc's own launch flow and should report a negotiated app API. This is
not a Unity or `.rdc` capture smoke and does not establish support for a graphics
API matrix row.

The reusable Unity annotation smoke requires a locally installed bridge built
from this source revision. It records a set packet, annotated D3D12 work, and a
matching delete packet, then validates the resulting `.rdc` in RenderDoc. Do not
use the currently pinned `2026.8.11-1` artifact for that smoke because it predates
the annotation exports.
