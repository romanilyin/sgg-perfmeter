# AGENTS.md

## Project Snapshot
- Unity project, not a standalone .NET/npm repo; target editor is Unity `6000.4.5f1` with URP `17.4.0`.
- Main build scene is `Assets/Scenes/SampleScene.unity`.
- Read `_Docs/perfmeter_theory.md` before implementing profiler features; it is the current product/architecture spec.
- Chat with the user in Russian.

## Package Layout
- Profiler package is `Assets/Scripts/SGG.PerfMeter/` with populated `Runtime/`, `Editor/`, `Tests/`, and bilingual `.Documentation/` directories.
- `Assets/TutorialInfo/` and `Assets/Readme.asset` are Unity template/readme assets, not profiler code.
- `Assets/Scripts/SGG.PerfMeter/package.json` is `com.sungeargames.perfmeter` / `SGG PerfMeter`.

## Documentation
- User-facing documentation lives in `Assets/Scripts/SGG.PerfMeter/.Documentation/` and must be maintained in both Russian and English.
- Development/spec documentation lives in `_Docs/`; read it before changing profiler architecture.

## Unity/URP Settings
- Active pipeline is `Assets/Settings/PC_RPAsset.asset`; Quality maps Standalone to `PC` and Android/iOS to `Mobile`.
- `PC_RPAsset` uses `PC_Renderer`, Forward+ (`m_RenderingMode: 2`), SSAO enabled, and requires depth plus opaque textures.
- `Mobile_RPAsset` uses `Mobile_Renderer`, Forward (`m_RenderingMode: 0`), no renderer features, render scale `0.8`, and no depth/opaque texture.
- Both URP assets have SRP Batcher on, Dynamic Batching off, GPU Resident Drawer off, and Native Render Pass on.
- `ProjectSettings/ProjectSettings.asset` has `enableFrameTimingStats: 1`; keep Frame Timing Stats enabled before relying on `FrameTimingManager` in builds.
- Android graphics APIs are explicit (`m_Automatic: 0`) with Vulkan before OpenGLES3; GPU timing can differ or be unavailable on GLES.

## Commands
- There are no repo-local build/test/lint scripts or CI workflows; validation normally requires the Unity editor/batchmode for this project.
- Reliable local compile check: `<Unity> -batchmode -quit -projectPath C:\Work\Unity\sgg-perfmeter-local -logFile C:\Work\Unity\sgg-perfmeter-local\Logs\opencode-compile.log`.
- EditMode/PlayMode `-runTests` currently exits without XML results in this WSL/Windows Unity setup; use compile batchmode unless the verification setup changes.
- Do not edit generated `*.csproj`, `*.sln`, or `*.slnx` as source; `.gitignore` excludes them.

## Style And Assets
- `.editorconfig` sets C# tabs width 4, CRLF, no final newline, block-scoped namespaces, explicit types preferred over `var`, and private/internal fields as `_camelCase`.
- Unity serialization is Force Text; keep `.meta` files with asset and folder changes.
- Avoid touching generated/ignored Unity state: `Library/`, `Logs/`, `UserSettings/`, `Build*/`, `Temp/`, and `Obj/`.

## Profiler Constraints
- Prefer `FrameTimingManager` and zero-allocation `ProfilerRecorder` collection; do not base profiler timing on `Time.deltaTime`.
- Runtime snapshots expose render, SRP Batcher, BRG/GRD, index upload, memory, timing, and overdraw counters; use `AvailableCounters` / `UnavailableCounters` when a platform lacks a recorder.
- Avoid uGUI/IMGUI runtime overlays for this profiler; the theory doc calls for low-overhead UI Toolkit plus Render Graph integration.
- For URP 17.4 renderer features, implement Render Graph paths (`RecordRenderGraph`/`AddRasterRenderPass`) instead of assuming legacy-only render passes.
- Overdraw measurement is opt-in and uses `PerfMeterRenderGraphFeature`, hidden replacement shader `Hidden/SGG/PerfMeter/OverdrawCounter`, fragment atomic counter, and `AsyncGPUReadback`; visual heatmap output is still pending.

## User Notifications
- Use `python3 Tools/TelegramNotify/telegram_notify.py --message "Perf: ..."` after each committed iteration or when user attention is required.
- Do not commit `Tools/TelegramNotify/.env`.
