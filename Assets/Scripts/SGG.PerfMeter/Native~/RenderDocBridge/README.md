# SGG PerfMeter RenderDoc Bridge

This directory contains the isolated `PM-RDOC-002` Windows x64 source bridge. It
has no managed or public capture wiring, ships no RenderDoc or bridge binary,
and never loads or injects RenderDoc. Production resolution accepts only an
already-loaded `renderdoc.dll`.

## Build And Test

Run from an x64 Visual Studio developer environment and keep the build directory
outside the package source:

```powershell
cmake -S <bridge-source> -B <build-directory> -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=ON
cmake --build <build-directory>
ctest --test-dir <build-directory> --output-on-failure
```

CMake downloads `renderdoc_app.h` from the exact revision recorded in
`ThirdPartyNotices.md` and rejects any SHA-256 mismatch.

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
