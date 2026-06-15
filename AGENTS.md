# AGENTS.md

## Repository Snapshot

- This is a Unity project that contains the Git UPM package `com.sungeargames.perfmeter`.
- It is not a standalone .NET or npm repository; there are no repo-local build/test scripts.
- Unity project version: `6000.4.7f1`.
- Package path: `Assets/Scripts/SGG.PerfMeter/`.
- Main validation scene: `Assets/Scenes/SampleScene.unity`.
- First public release version: `2026.6.5-1`; the first npm release is `2026.6.5-2`; current HDRP release candidate is `2026.6.11-1`.
- Public distribution uses GitHub Releases, Git UPM tags, and npm scoped-registry installs.

## Supported Scope

- Supported runtime target: Unity `6000.4+` with URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration.
- Built-in Render Pipeline is unsupported and not planned.
- HDRP overdraw and heatmap are unsupported; core FPS/CPU/GPU/memory/session/alert/camera/device/MCP diagnostics remain available in HDRP.
- Prefer `FrameTimingManager` and zero-allocation `ProfilerRecorder` collection; do not base profiler timing on `Time.deltaTime`.
- Runtime overlay work should stay on UI Toolkit, not uGUI or IMGUI.
- URP render integration should use Render Graph paths such as `RecordRenderGraph` / `AddRasterRenderPass`; HDRP render integration should stay in the optional HDRP assembly and use `CustomPass`.

## Documentation Map

- User documentation: root `README.md` plus localized docs under `docs/en/`, `docs/ru/`, `docs/de/`, `docs/es/`, `docs/fr/`, `docs/it/`, `docs/ja/`, `docs/ko/`, `docs/pt-br/`, and `docs/zh-cn/`.
- Package-local `.Documentation/` files are intentionally minimal link entry points to GitHub docs.
- Development docs: `_DevelopmentDocs/README.md`, `_DevelopmentDocs/decisions/`, `_DevelopmentDocs/backlog/`, `_DevelopmentDocs/release/`.
- Release checklist: `_DevelopmentDocs/release/2026.6.5-1-public-release-checklist.md`.
- Internal local workflow notes: `_DevelopmentDocs/agents/internal-agent-notes.md`.

## Package Work

- Source code lives under `Assets/Scripts/SGG.PerfMeter/Runtime/`, `Editor/`, and `Tests/`.
- Package samples live under `Assets/Scripts/SGG.PerfMeter/Samples~/`.
- Do not edit generated `*.csproj`, `*.sln`, or `*.slnx` files as source.
- Keep `.meta` files with any Unity asset or folder changes.
- Avoid committing generated or local state: `Library/`, `Logs/`, `UserSettings/`, `Build*/`, `Temp/`, and `Obj/`.
- Do not commit secrets, `.env` files, device dumps, local APKs, or private logs.

## Validation

Use an installed Unity editor for compile and tests. Replace `<Unity>` with the local Unity executable path.

Compile check:

```bash
<Unity> -batchmode -quit -projectPath "<repo>" -logFile "<repo>/Logs/compile.log"
```

EditMode and PlayMode tests must run without `-quit`:

```bash
<Unity> -batchmode -projectPath "<repo>" -runTests -testPlatform EditMode -testResults "<repo>/Logs/editmode-results.xml" -logFile "<repo>/Logs/editmode.log"
<Unity> -batchmode -projectPath "<repo>" -runTests -testPlatform PlayMode -testResults "<repo>/Logs/playmode-results.xml" -logFile "<repo>/Logs/playmode.log"
```

For docs-only changes, run at minimum:

```bash
git diff --check
```

## Pull Requests

- Changes to `main` should go through a pull request after branch protection is enabled.
- Keep PRs focused and update tests plus user docs when runtime behavior changes.
- User-facing docs must not include internal `_DevelopmentDocs` workflow details.
