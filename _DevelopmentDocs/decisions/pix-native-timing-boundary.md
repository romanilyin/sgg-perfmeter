# Native PIX Timing Capture Boundary

Status: accepted for `PM-PIX-001`; implementation waits for a documented bounded circular Windows timing-capture API.

## Decision

PerfMeter will not implement the current `PM-PIX-001` circular pre-alert timing-capture scope through undocumented PIX exports, PIX UI automation, private ETW reconstruction, proprietary `.wpix` parsing, output-directory guessing, or Unity's GPU-frame `ExternalGPUProfiler` wrapper.

Microsoft's documented `pix3.h` programmatic timing API can start and end a forward timing capture with an exact caller-selected `.wpix` path. On Windows, however, the documented API ignores `PIXCaptureParameters` storage selection and maximum tooling-memory size, and `PIXEndCapture` ignores the discard argument. It therefore does not provide a bounded circular store, a documented pre-trigger ring, or a way to flush such a bounded pre-trigger store on alert.

The current `PM-PIX-001` request remains waiting rather than silently degrading:

- native circular or pre-alert PIX remains unimplemented until Microsoft publishes a bounded Windows contract; PIX requests selecting a native backend mode continue to be rejected as `InvalidRequest`;
- generic PIX through Unity remains GPU frame capture only and non-authoritative;
- caller-supplied or PIX-UI-produced `.wpix` files remain observed, never authoritative;
- extension, timestamp, PID, directory observation, or file stability alone never establishes request association;
- PerfMeter does not bundle, copy, install, inject, or redistribute PIX capturer/UI/analysis binaries.

## Documented Forward Candidate

A separately approved future scope may implement forward timing capture through the public `pix3.h` API. It is not an implementation of the current circular requirement. Admission would require all of the following:

- Windows x64 and an explicitly validated Unity Editor or Development Player row;
- explicit opt-in and an elevated target process;
- a user-installed PIX version and verified installed `WinPixTimingCapturer.dll` identity;
- an exact package-owned, nonce-bound `.wpix` output path supplied to `PIXBeginCapture`;
- no generic fallback after begin is attempted or native state becomes uncertain;
- successful `PIXEndCapture`, bounded exact-file stabilization, stable handle identity, SHA-256, and generation/request binding;
- sensitive process-trace privacy and quota policy independent of RenderDoc GPU-capture policy;
- official PIX validation of the produced artifact before claiming semantic GPU-timing contents.

Loading a verified user-installed timing capturer in place would require a separate product and licensing decision. The MIT `PixEvents`/WinPixEventRuntime license does not establish redistribution rights for the PIX-installed timing capturer, PIX UI, or analysis components.

## Existing Behavior Preserved

- `PerfMeterCaptureTool.Pix = 2` and generic `GenericUnity` request behavior remain unchanged.
- The generic Windows Direct3D 12 matrix still requires an already attached external profiler and confirms only Unity's begin/end lifecycle.
- Generic `.wpix` bundle ingestion remains project-local, hashed, sensitive, and non-authoritative.
- Native RenderDoc registration, authority, storage, and release boundaries are unchanged.

## Re-evaluation Gate

Re-open circular native PIX implementation only when an official Microsoft source documents all of:

1. bounded circular storage on Windows;
2. deterministic arm/trigger/end or flush semantics;
3. cancellation/discard behavior;
4. exact output-path and completion association;
5. supported process privilege, architecture, and lifecycle constraints.

## Sources

- Microsoft PIX programmatic capture: <https://devblogs.microsoft.com/pix/programmatic-capture/>
- Microsoft PIX programmatic timing captures: <https://devblogs.microsoft.com/pix/programmatic-timing-captures-now-available/>
- Microsoft PIX capture setup: <https://devblogs.microsoft.com/pix/taking-a-capture/>
- Public PIX headers: <https://github.com/microsoft/PixEvents/blob/main/include/pix3.h>
- WinPixEventRuntime boundary: <https://devblogs.microsoft.com/pix/winpixeventruntime/>
- Unity `ExternalGPUProfiler`: <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Experimental.Rendering.ExternalGPUProfiler.html>
