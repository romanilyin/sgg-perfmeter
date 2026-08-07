# SGG PerfMeter Product Roadmap

Статус: внутренний приоритизированный backlog. Порядок отражает зависимости и продуктовую ценность, но не обещает календарный release scope.

Основание: review текущего backlog и локального research report `C:\Work\Unity\deep-research-report-perfmeter-new-26-8-4.md` от августа 2026 года.

## Модель приоритетов

| Priority | Значение |
| --- | --- |
| P0 | Блокирующий дефект или обязательная работа текущего release pass. |
| P1 | Следующая стабилизационная итерация; закрывает текущий пользовательский дефект или высокий compatibility risk. |
| P2 | Стратегическая foundation-работа с высокой продуктовой ценностью после P1. |
| P3 | Следующая самостоятельная feature-итерация после соответствующей foundation. |
| P3.5 | Release-integration cleanup после P3, обязательный до общего build/release pass. |
| P4 | Дорогая или platform-native experimental работа без обязательства ближайшего release. |
| Deferred | Осознанно не входит в ближайшую основную программу. |

## Архитектурные ограничения

- PerfMeter остается low-overhead диагностическим и capture-coordination слоем, а не заменой Unity Profiler, RenderDoc, PIX, Profile Analyzer или Frame Debugger.
- Core package не получает hard dependencies на Adaptive Performance, Memory Profiler или platform-native tooling; интеграции поставляются через optional assemblies/providers/backends.
- Heavy captures и native integrations выключены по умолчанию и требуют явного API, MCP, user action или настроенного alert rule.
- Недоступные метрики и providers возвращают explicit `Unavailable`/degraded state, а не ложный `0`.
- Public API и artifact schemas развиваются additively; старые контракты сохраняются минимум на один стабильный replacement cycle.

## Roadmap

| ID | Priority | Status | Scope | Depends On |
| --- | --- | --- | --- | --- |
| `PM-LOG-001` StructuredLog toggle | P0 | resolved, released `2026.8.5-2` | Независимый public toggle отключает только structured info `Debug.Log`, сохраняя callbacks, alerts/history, overlay warnings, EditorWarning и sessions. | - |
| `PM-UI-001` Stable numeric geometry | P1 | resolved, released `2026.8.5-2`, [GitHub #2](https://github.com/romanilyin/sgg-perfmeter/issues/2) | Prefix/value/unit cells, worst-case widths, numeric monospace role, bounded `FpsOnly` fallback, wrapping widgets and geometry tests. | - |
| `PM-UI-002` Owned versioned panel host | P1 | resolved, released `2026.8.5-2`, [GitHub #1](https://github.com/romanilyin/sgg-perfmeter/issues/1) | Owned `UIDocument` host on Unity 6000.4 and `PanelRenderer` on 6000.5+ preserve foreign UI trees/settings and remove only the PerfMeter container. | `PM-UI-001` |
| `PM-CAP-001` Capture coordinator | P2 | resolved, released `2026.8.6-2` | Единый capture domain/state machine, fake backend, overlap guard и deterministic pre/capture/post-roll. Experimental Unity `ExternalGPUProfiler` изолирован для Editor/Development builds, attached external tool и явных RenderDoc/PIX platform/API combinations; completion не заявляет authoritative artifact path. | P1 stabilization |
| `PM-CAP-002` Correlated artifact bundle | P2 | resolved, released `2026.8.6-2` | Versioned atomic bundle связывает отдельно классифицированные baseline/capture samples, alerts, device/camera/render context, optional screenshot и truthful external-artifact observation; project-local quota/retention/redaction policy и MCP request/status/cancel/export/capabilities не заявляют authority без tool-authenticated provenance. | `PM-CAP-001` |
| `PM-COMP-001` Compatibility status and matrix | P2 | resolved, released `2026.8.6-2` | Additive Editor snapshot и structured MCP отдельно сообщают `ImportCompatible` для declared Unity `2022.3` floor, `CoreRuntimeCompatible` для supported Unity `6000.4+` и active URP/HDRP `17.4+` `RenderIntegrationCompatible`; configuration readiness остается отдельной. | P1 stabilization |
| `PM-OBS-001` Dynamic Profiler metric catalog | P2 | resolved, released `2026.8.6-1` | Discover/cache recorder descriptors only at runtime startup and explicit refresh/reconfigure; resolve semantic metrics through exact names and aliases; publish additive API/MCP capability provenance and distinguish unavailable/no-sample/sampled while keeping existing numeric metrics as compatibility values. | P1 stabilization |
| `PM-OBS-002` Profiler instrumentation | P2 | resolved, released `2026.8.6-1` | Internal Scripts-category end-of-frame gauge markers/counters for collection, providers, snapshots, bottleneck, capture/export, CPU/GPU timing, thermal hook and state codes; no public schema changes or overhead subtraction. | `PM-OBS-001` |
| `PM-OBS-003` Self-observability and overhead budgets | P2 | resolved, released `2026.8.6-1` | Fixed 120-frame CPU callback windows для collector, custom providers, CPU-core provider, overlay и URP/HDRP integration; additive API/status/MCP snapshots и per-invocation allocation/performance budgets. GPU attribution остается `Unavailable`, основные CPU/GPU-метрики не корректируются. | `PM-UI-002`, `PM-OBS-002` |
| `PM-PLAT-001` Adaptive Performance telemetry | P2 | resolved, released `2026.8.6-2` | Optional `SGG.PerfMeter.AdaptivePerformance` provider для thermal state/temperature trends, CPU/GPU performance levels, alerts и session/capture samples с provider provenance; core package не получает hard dependency на `com.unity.adaptiveperformance`, а assembly активируется через version define для `5.1.0+` и явно сохраняет unavailable states. | capability/provider seams, `PM-CAP-002` |
| `PM-MEM-001` Memory snapshot trigger | P3 | resolved, released `2026.8.7-1` | Optional Memory Profiler backend для manual/threshold/leak capture с cooldown, free-space guard, capture flags и bundle manifest. | `PM-CAP-001`, `PM-CAP-002` |
| `PM-GFX-001` PSO and shader-stutter diagnostics | P3 | resolved, released `2026.8.7-1` | Dynamic shader/graphics-pipeline creation marker catalog с exact/alias provenance, graphics API и parallel-PSO capability context, а также optional `GraphicsStateCollection` trace/prewarm workflow с active-session correlation, `IsBusy`/`HasPendingCleanup`, StopSession cancellation, persisted owned-cleanup sidecars и bounded project-local artifact. | `PM-OBS-001`, `PM-CAP-001` |
| `PM-REN-001` Render integration context | P3 | resolved, released `2026.8.7-1` | Integration-neutral public snapshot для camera/SRP/pass/freshness/GRD/VRS context через additive compatibility facade. URP сообщает public current-frame rendering mode и фактически scheduled PerfMeter passes; HDRP сообщает observed Custom Pass, но не effective rendering mode. Private RenderGraph pass/resource counters и Editor navigation не заявляются без стабильного public Unity API. | `PM-OBS-001` |
| `PM-GRD-001` GPU Resident Drawer telemetry | P3 | resolved, released `2026.8.7-1` | Показывать GRD/Forward+/compute support, фактическую global runtime activity, aggregate BRG effectiveness counters с provenance и structured fallback/degraded reasons без ложного per-renderer claim. | `PM-OBS-001`, `PM-REN-001` |
| `PM-CI-001` Profile Analyzer and benchmark CI | P3 | resolved, released `2026.8.7-1` | Additive session ID в API/MCP/JSON и последнем CSV-столбце, корреляционные Profiler markers, optional Profile Analyzer opener без hard dependency, versioned zero-allocation/CPU baselines и Unity `6000.4`/`6000.5` CI с NUnit/JUnit/performance artifacts. | `PM-OBS-002` |
| `PM-SESSION-001` Session analysis UI | P3 | resolved, released `2026.8.7-1` | Read-only virtualized Editor UI Toolkit window с retained-sample timeline, worst-frame inspector, derived CPU/GPU budget violations и authoritative whole-run/current-scene scopes без runtime/schema changes. | stable session/artifact schemas |
| `PM-SETUP-001` Setup UX completeness | P3.5 | resolved, baseline released `2026.8.7-1`; FTUE follow-up released `2026.8.7-2` | Полное представление persisted P2/P3 settings, optional integrations и analysis entry points в Setup window; отдельная live FTUE-вкладка с required checks, optional install/skip actions и auto-hide/reappearance; read-only compatibility/schema/reserved-metadata/diagnostic states и корректная граница runtime-only inputs. | completed P3 scope |
| `PM-UI-003` Widgets, themes and layout descriptors | P4 | design backlog | Расширяемые bounded widgets, semantic theme tokens, manifests, layout descriptors и safety limits без steady-state tree rebuild. | `PM-UI-001`, `PM-UI-002`, `PM-OBS-003` |
| `PM-RG-001` Deeper Render Graph diagnostics | P4 | waiting for stable public APIs | Добавлять pass/resource/aliasing/merge counters только через стабильные Unity APIs; сохранять degraded state вместо reflection. | Unity public APIs |
| `PM-ANDROID-001` Android Perfetto/AGI | P4 | Deferred, deferred by explicit product direction | Low-overhead ATrace/ADPF correlation и Editor sidecar для `adb`, Perfetto config, artifact import и AGI workflow. | `PM-CAP-002`, `PM-PLAT-001` |
| `PM-APPLE-001` Apple thermal, MetricKit and Metal | P4 | Deferred, deferred by explicit product direction | Optional native provider/backend для thermal/low-power/MetricKit и Development-only Metal capture. | `PM-CAP-002`, provider seams |
| `PM-PIX-001` Native PIX timing capture | P4 | experimental candidate | Windows-only circular timing capture, завершаемый после alert, с authoritative artifact provenance. | `PM-CAP-001`, `PM-CAP-002` |
| `PM-RDOC-001` Native RenderDoc backend | P4 | experimental candidate | Dynamic-load только уже подключенного RenderDoc API для naming/comments/path/enumeration; не поставлять и не inject RenderDoc binary. | stable `PM-CAP-001` and Unity backend |
| `PM-HDRP-001` HDRP overdraw/heatmap parity | Deferred | research only | Отдельный HDRP Custom Pass/shader/readback prototype и device/API matrix; не включать в ранний основной roadmap. | dedicated research validation |
| `PM-OTEL-001` Remote streaming/OpenTelemetry | Deferred | policy undefined | Opt-in batching/export требует transport, credentials, redaction, privacy и support policy до проектирования API. | security/transport decision |

`PM-LOG-001`, `PM-UI-001` и `PM-UI-002` выпущены в `2026.8.5-2`; GitHub issues #1 и #2 закрыты. Внешнее включение StructuredLog toggle остаётся задачей consuming projects и не держит package feature открытой.

`PM-OBS-001`, `PM-OBS-002` и `PM-OBS-003` выпущены в `2026.8.6-1`; Git tag, normal GitHub Release и npm package опубликованы.

`PM-CAP-001` выпущен в `2026.8.6-2`; release-version Unity `6000.4.12f1`/`6000.5.6f1` URP/HDRP full EditMode `172/172` и full PlayMode `13/13` прошли. Attached-tool artifact smoke явно waived из-за отсутствия подключенного RenderDoc/PIX; authoritative artifact result не заявляется.

`PM-CAP-002` выпущен в `2026.8.6-2`; targeted capture bundle EditMode `13/13` и release-version Unity `6000.4.12f1`/`6000.5.6f1` URP/HDRP full suites прошли. Real RenderDoc/PIX artifact smoke явно waived для этого pass; bundle provenance по-прежнему не заявляет неподтвержденную authority.

`PM-COMP-001` выпущен в `2026.8.6-2`; import-only compile на Unity `2022.3.62f1`/`6000.3.20f1` и release-version Unity `6000.4.12f1`/`6000.5.6f1` URP/HDRP full suites прошли.

`PM-PLAT-001` выпущен в `2026.8.6-2`; targeted platform telemetry EditMode `7/7`, telemetry lifecycle PlayMode `1/1` и full matrix прошли. Optional `SGG.PerfMeter.AdaptivePerformance` assembly успешно скомпилирована с `com.unity.adaptiveperformance@5.1.6`; target-device behavior явно waived из-за отсутствия поддерживаемого устройства и не заявляется как проверенное.

`PM-MEM-001` выпущен в `2026.8.7-1`: targeted memory EditMode `9/9`, capture-bundle EditMode `14/14`, PlayMode threshold `1/1`, optional assembly compile с реальным `com.unity.memoryprofiler@1.1.12`, а также Unity `6000.4.12f1` full EditMode `182/182` и full PlayMode `14/14` подтверждены. Windows player startup прошёл; target-device memory behavior явно waived и не заявляется.

`PM-GFX-001` выпущен в `2026.8.7-1`. Unity `6000.4.12f1` compile прошёл; targeted GSC EditMode `25/25`, `PerformanceMeter` API EditMode `47/47`, capture-bundle EditMode `14/14`, PlayMode smoke `12/12`, full post-fix EditMode `208/208` и full post-fix PlayMode `16/16` прошли. Unity `6000.5` tests и Windows player startup прошли; target-device `GraphicsStateCollection` behavior явно waived и не заявляется.

`PM-REN-001` выпущен в `2026.8.7-1`: Unity `6000.4.12f1` main compile passed; targeted `PerformanceMeterApiTests` `53/53`, `PerfMeterCaptureBundleTests` `15/15` и `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `215/215` и full PlayMode `16/16` прошли. Focused review P1/P2 resolved. URP/HDRP matrix и Windows player startup прошли.

`PM-GRD-001` выпущен в `2026.8.7-1`: Unity `6000.4.12f1` compile passed; targeted `PerformanceMeterApiTests` `58/58`, `PerfMeterCaptureBundleTests` `15/15` и `PerformanceMeterPlayModeSmokeTests` `12/12`; final full EditMode `220/220` и full PlayMode `16/16` прошли. Focused review P1/P2 resolved. URP/HDRP matrix и Windows player startup прошли; target-device GRD/BRG behavior явно waived и не заявляется.

`PM-CI-001` выпущен в `2026.8.7-1`: Unity `6000.4.12f1` compile passed; targeted `PerfMeterSessionCorrelationTests` `5/5`, `PerformanceMeterApiTests` `58/58` и performance tests `2/2` на Unity `6000.4.12f1`/`6000.5.6f1` прошли, warmed instrumentation и session-boundary paths подтвердили `0 B` allocations. Local matrix и player startup прошли; GitHub-hosted execution был явно waived после activation-only failure без Unity test execution, а workflow оставлен opt-in до настройки GameCI-compatible credentials.

`PM-SESSION-001` выпущен в `2026.8.7-1`: Unity `6000.4.12f1` compile passed; targeted `PerfMeterSessionAnalysisTests` `11/11`, `PerformanceMeterApiTests` `58/58`, `PerfMeterSessionCorrelationTests` `5/5` и `PerfMeterCaptureBundleTests` `15/15` прошли; final full EditMode `238/238` и PlayMode `16/16` прошли. Matrix и Windows player startup прошли. Окно не меняет runtime retention/API/schema и не запускает runtime.

`PM-SETUP-001` выпущен в `2026.8.7-1`: Unity `6000.4.12f1` compile прошёл; targeted `PerfMeterSetupWindowTests` `9/9`, `PerfMeterSettingsTests` `22/22`, `PerfMeterSessionAnalysisTests` `11/11`, `PerfMeterMemorySnapshotTests` `9/9` и `PerfMeterGraphicsStateCollectionTests` `25/25` прошли; final full EditMode `247/247` и PlayMode `16/16` прошли. Matrix и Windows player startup прошли. Focused review P1/P2 resolved.

FTUE follow-up `PM-SETUP-001` выпущен в `2026.8.7-2`: targeted `PerfMeterFtueTests` `15/15`, `PerfMeterSetupWindowTests` `11/11`, `PerfMeterSettingsTests` `23/23` и `PerformanceMeterApiTests` `59/59`; full EditMode `266/266` и PlayMode `16/16` прошли на Unity `6000.4.12f1` и `6000.5.6f1`. GitHub Release, npm Trusted Publishing OIDC, `latest`, integrity и SLSA provenance v1 подтверждены.

Release-version validation for `2026.8.7-1` passed on Unity `6000.4.12f1`/`6000.5.6f1` URP/HDRP, import floors, optional package compiles, Windows player startup, performance tests, and npm dry-runs. Target-device Memory Profiler, `GraphicsStateCollection`, GRD/BRG, and Android physical-device behavior are explicitly waived because the required hardware, Android Build Support, and `adb` are unavailable; no target-device result is claimed. GitHub-hosted Unity execution was explicitly waived because no GameCI-compatible credentials are configured. GitHub Release, npm Trusted Publishing OIDC, `latest`, integrity, and SLSA provenance v1 are verified.

## Release Sequence

| Phase | Scope | Exit Gate |
| --- | --- | --- |
| Stabilization | `PM-UI-001`, `PM-UI-002` | Released in `2026.8.5-2`; Unity 6000.4/6000.5 URP/HDRP editor suites and Windows player smoke passed, with Android device smoke explicitly waived. |
| Capture preview | `PM-CAP-001` | Fake-backend contract tests, no overlap, deterministic state transitions, guarded attached-tool backend and no claim of an authoritative `.rdc`/`.wpix` path from the Unity wrapper. |
| Capture stable | `PM-CAP-002` | Atomic versioned bundle, truthful artifact provenance, session/alert correlation and external-tool smoke matrix. |
| Observability | `PM-OBS-001` through `PM-OBS-003` | Startup-only discovery, capability dump, custom markers/counters and measured overhead budgets. |
| Platform telemetry | `PM-PLAT-001`, then selected P3 integrations | Optional assemblies, no core hard dependency, explicit unavailable states and device validation. |
| Graphics diagnostics | `PM-GFX-001` | Post-fix Unity 6000.4/6000.5 compile and targeted/full EditMode/PlayMode validation, dynamic-marker provenance checks, plus release-player and target-device behavior before release. |
| P3 analysis and setup | `PM-CI-001`, `PM-SESSION-001`, `PM-SETUP-001` | Session correlation/analysis and complete Setup presentation validated before one combined `2026.8.7-1` build/release pass. |
| Platform capture previews | Selected P4 backends | Experimental feature flags, native lifecycle/IL2CPP tests and tool-specific artifact confirmation. |

## Required Gates

- Compile/runtime matrix: latest Unity 6000.4 and 6000.5 patches; import-only checks for the declared import floor and Unity 6000.3.
- URP 17.4 and HDRP 17.4; Windows D3D11/D3D12, Linux Vulkan, macOS/iOS Metal, Android Vulkan and explicit GLES degraded mode as applicable.
- Fake backends are mandatory in automated tests; real RenderDoc/PIX/platform-tool smokes are release-candidate gates.
- Adaptive Performance `5.1+` package integration and real target-device thermal/performance validation remain release-candidate gates; fake providers and contract tests do not replace them.
- Memory Profiler `1.1.0+` real-package integration, release-player behavior, and target-device validation remain the `PM-MEM-001` release gate; targeted tests and the `1.1.12` optional compile check do not replace it.
- `PM-GFX-001` release gate remains the Unity 6000.4/6000.5 namespace/assembly matrix, full Unity 6000.5 tests, release-player behavior, and target-device checks. The final Unity `6000.4.12f1` evidence above validates the post-fix lifecycle, dynamic marker provenance, active-session correlation, overlap/cancel/prewarm cleanup, and 64 MiB owned-artifact limit, but does not replace those release gates.
- Capture samples are classified separately from normal baseline samples; capture overhead must not silently contaminate normal performance evidence.
- Steady-state collector and hidden overlay target `0 B/frame`; dynamic discovery runs only at startup/reconfigure and export stays outside frame-critical paths.
- Bundles enforce project-local path validation, atomic commit, disk quota/retention and redaction of sensitive paths, screenshots and device metadata.

## Not Planned

- Built-in Render Pipeline support remains unsupported and is not planned.
- XR/world-space overlay remains outside scope until a concrete supported target exists.

## Detailed Backlogs

- UI ownership, geometry, widgets and themes: `ui-widgets-and-themes.md`.
- Collector, sessions, self-overhead and rendering diagnostics: `profiler-backlog.md`.
- HDRP status and deferred overdraw work: `hdrp-support.md`.
- Resolved PerfMeter-owned MCP reports: `mcp-problem-reports.md`.
