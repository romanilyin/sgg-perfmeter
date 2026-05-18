# SGG PerfMeter Iteration Plan

This file is the working implementation plan. Update readiness after every iteration and commit.

## Readiness Legend
- Planned: not started.
- In Progress: implementation is being edited or verified.
- Done: implemented, batchmode-verified where applicable, documented, and committed.
- Blocked: requires user attention or an external prerequisite.

## Agent-Facing Runtime API Contract
- Public namespace: `SGG.PerfMeter`.
- Agents must be able to query profiler state without reading UI or Unity logs.
- Required status methods: `PerformanceMeter.GetStatus()`, `PerformanceMeter.TryGetStatus(out PerfMeterStatusSnapshot status)`, `PerformanceMeter.GetLatestMetrics()`, `PerformanceMeter.TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)`.
- Required lifecycle methods: `PerformanceMeter.EnsureRunning()` and `PerformanceMeter.Stop()`.
- Status snapshots must include at least: state, availability, frame timing availability, graphics API warning, collection frame, and last error.
- Metrics snapshots must be immutable value snapshots so agents can safely cache and compare them between frames.

## Iterations
| Iteration | Readiness | Scope | Batchmode Verification | Commit |
| :-- | :-- | :-- | :-- | :-- |
| 0. Plan and orchestration | Done | Create this plan, define agent API contract, verify notification and Unity batchmode paths. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter0.log`, exit code 0. | `938a4b0` |
| 1. Package bootstrap and agent API | Done | Correct package identity, add runtime/editor/test asmdefs, implement public status API scaffolding, add bilingual user docs. | Batchmode compile passed with Unity 6000.4.5f1, exit code 0. EditMode CLI test execution is a known verification issue below. | `21e8c2c` |
| 2. Core metric collection | Done | Implement `FrameTimingManager` capture and zero-allocation `ProfilerRecorder` counters for CPU/GPU/render/memory/BRG metrics. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter2-compile.log`, exit code 0. EditMode CLI test execution exited 0 but did not write XML; see known issue below. | `502fa72` |
| 3. Low-overhead overlay | Done | Add UI Toolkit runtime overlay with throttled updates and no uGUI/IMGUI runtime dependency. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter3-compile.log`, exit code 0. PlayMode tests not run due known CLI test issue below. | `7f41034` |
| 4. URP Render Graph integration | Done | Add URP 17 Render Graph renderer feature scaffold with a dedicated profiling sampler for tool overhead. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter4-compile.log`, exit code 0. First WSL-style path launch failed before compile; rerun with Windows-style paths passed. | `b380710` |
| 5. Overdraw tools | Done | Add opt-in overdraw measurement scaffold API, state snapshots, overlay readout, Render Graph marker pass, and AsyncGPUReadback counter-buffer scaffold. Full shader instrumentation remains a documented limitation. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter5-compile.log`, exit code 0. Manual device validation pending. | `bfa2d41` |
| 6. Packaging and docs hardening | Done | Harden package metadata and bilingual user/dev docs without adding large new profiler features. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter6-compile.log`, exit code 0. `-runTests` issue intentionally not investigated. | `a78dd89` |
| 7. Shader-instrumented overdraw | Done | Replace the overdraw scaffold with a hidden replacement shader, fragment atomic counter buffer, Render Graph renderer-list pass, and `AsyncGPUReadback` completion path. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-overdraw-compile-3.log`, exit code 0. Manual target-device validation remains pending. | `92c34a8` |
| 8. Render counter completion | Done | Complete render, SRP Batcher, BRG/GRD, index upload, and GPU memory snapshot/overlay/MCP counters with profiler counter name fallbacks. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-counters-compile.log`, exit code 0. Runtime device validation remains pending for platform-specific counter availability. | `514de50` |
| 9. Unity 6000 counter aggregation | Done | Fix `DrawCalls`, `Batches`, BRG draw calls, and BRG instances by aggregating Unity 6000 component counters instead of expecting removed aggregate recorder names. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-counter-aggregate-final-compile.log`, exit code 0. Local collector dump showed no unavailable counters. | `484cf21` |
| 10. Overlay corner placement | Done | Add configurable overlay corner placement, default the runtime overlay to `TopRight`, and configure UI Toolkit theme/text settings so overlay text renders reliably. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-overlay-text-compile.log`, exit code 0. | `a41aae5` |
| 11. Overlay modes and graphs | Done | Add FPS/lows/spikes snapshots, overlay modes (`FpsOnly`, `TextCompact`, `Graphs`, `Full`), CPU/GPU UI Toolkit graphs, MCP mode control, and docs/tests. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-overlay-graphs-final-compile.log`, exit code 0. | `50b3161` |
| 12. Stacked graph layout and setup runtime tab | Done | Refine graph scale/color/placeholder formatting, align legend order with stacked CPU order, add dynamic initialization code settings, and add Setup/Runtime tabs with Play Mode runtime controls. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-overlay-setup-tabs-compile-2.log`, exit code 0. | `5ee83c6` |
| 13. Target FPS controls | Done | Add `PerfMeterTargetFps` API, target FPS setup/runtime controls, target-line graph budget updates, new `SGG/Perfmeter/Setup` menu path, and exact repository URLs in license files. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-target-fps-compile.log`, exit code 0. | `2b219ca` |
| 14. Project identity and documentation backlog | Done | Update Unity project identity, package author, and documentation backlog for zero-allocation overlay work. | Settings/docs-only hardening; covered by later batchmode compiles. | `105249e` |
| 15. Selectable renderer feature setup | Done | Let setup users select specific renderer assets, install all missing features, and select missing editable renderers. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter2-setup-ui-compile.log`, exit code 0. | `abb1f0d` |
| 16. Bottleneck classification hardening | Done | Add `PresentLimited` classification, make classifier testable, and add EditMode classifier coverage. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter3-bottleneck-compile.log`, exit code 0. | `675d2e0` |
| 17. Collection frame naming | Done | Replace misleading `LastSampleFrame` public naming with `CollectionFrame` and MCP `collection_frame`. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-iter4-collection-frame-compile.log`, exit code 0. | `0355fd9` |
| 18. Overdraw capability gate | Done | Report unsupported overdraw targets as `Unsupported` before scheduling Render Graph work when required GPU/readback capabilities are missing. | First compile failed on an unsupported test constraint helper; rerun passed with `Logs/opencode-iter5-overdraw-unsupported-compile-2.log`, exit code 0. | `05a6520` |
| 19. Public runtime API rename | Done | Rename the public runtime entry point from `PerfMeter` to `PerformanceMeter` and update setup references. | Covered by later batchmode compile, `Logs/opencode-setup-discovery-compile.log`, exit code 0. | `363b184` |
| 20. Active URP renderer discovery | Done | Discover active URP renderer assets from Graphics/Quality settings first, report active/package/editable status, and avoid modifying package-owned renderers. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-setup-discovery-compile.log`, exit code 0. | `c65e440` |
| 21. Marker pass opt-in | Done | Keep the empty overlay Render Graph marker pass disabled by default while preserving overdraw pass recording during active measurements. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-marker-optin-compile.log`, exit code 0. | `839b84b` |
| 22. PlayMode runtime smoke tests | Done | Add PlayMode tests for runtime/overlay object lifecycle, snapshot updates across frames, and overdraw terminal or actionable degraded states. | `Unity.exe -batchmode -quit` passed, `Logs/opencode-playmode-smoke-compile.log`, exit code 0. | `4fc496d` |

## Current Notes
- Unity executable verified at `/mnt/c/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe`.
- Telegram notification workflow is repo-local at `Tools/TelegramNotify/telegram_notify.py`; messages for this work use prefix `Perf:`.
- `Tools/TelegramNotify/.env` is intentionally ignored and must not be committed.
- Latest local compile verification passed in `Logs/opencode-hardening-compile.log`; latest Test Runner XML results passed in `Logs/opencode-hardening-editmode-results.xml` and `Logs/opencode-hardening-playmode-results.xml`.
- `_Docs/temp_feedback_15_05_2026.md` is an untracked user feedback file; do not commit it unless explicitly requested.

## Known Verification Issues
- Unity `-runTests` must be launched without `-quit`; adding `-quit` exits batchmode before Test Runner writes XML results.
- Android player build validation is blocked until Android Build Support, SDK/NDK Tools, and OpenJDK are installed for Unity 6000.4.5f1.
