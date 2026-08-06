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
| `PM-CAP-001` Capture coordinator | P2 | implemented, unreleased | Единый capture domain/state machine, fake backend, overlap guard и deterministic pre/capture/post-roll. Experimental Unity `ExternalGPUProfiler` изолирован для Editor/Development builds, attached external tool и явных RenderDoc/PIX platform/API combinations; completion не заявляет authoritative artifact path. | P1 stabilization |
| `PM-CAP-002` Correlated artifact bundle | P2 | planned | Атомарно связывать manifest, session/samples, alerts, device/camera/render context, screenshot и authoritative external-capture metadata; добавить MCP request/status/cancel/export/capabilities. | `PM-CAP-001` |
| `PM-COMP-001` Compatibility status and matrix | P2 | planned | Явно различать `ImportCompatible`, `CoreRuntimeCompatible` и `RenderIntegrationCompatible`; проверять заявленный import floor отдельно от Unity 6000.4+ runtime support. | P1 stabilization |
| `PM-OBS-001` Dynamic Profiler metric catalog | P2 | resolved, released `2026.8.6-1` | Discover/cache recorder descriptors only at runtime startup and explicit refresh/reconfigure; resolve semantic metrics through exact names and aliases; publish additive API/MCP capability provenance and distinguish unavailable/no-sample/sampled while keeping existing numeric metrics as compatibility values. | P1 stabilization |
| `PM-OBS-002` Profiler instrumentation | P2 | resolved, released `2026.8.6-1` | Internal Scripts-category end-of-frame gauge markers/counters for collection, providers, snapshots, bottleneck, capture/export, CPU/GPU timing, thermal hook and state codes; no public schema changes or overhead subtraction. | `PM-OBS-001` |
| `PM-OBS-003` Self-observability and overhead budgets | P2 | resolved, released `2026.8.6-1` | Fixed 120-frame CPU callback windows для collector, custom providers, CPU-core provider, overlay и URP/HDRP integration; additive API/status/MCP snapshots и per-invocation allocation/performance budgets. GPU attribution остается `Unavailable`, основные CPU/GPU-метрики не корректируются. | `PM-UI-002`, `PM-OBS-002` |
| `PM-PLAT-001` Adaptive Performance telemetry | P2 | planned | Optional provider для thermal/power trends, CPU/GPU performance levels, alerts и session columns с provider provenance. | capability/provider seams, `PM-CAP-002` |
| `PM-MEM-001` Memory snapshot trigger | P3 | planned | Optional Memory Profiler backend для manual/threshold/leak capture с cooldown, free-space guard, capture flags и bundle manifest. | `PM-CAP-001`, `PM-CAP-002` |
| `PM-GFX-001` PSO and shader-stutter diagnostics | P3 | planned | Коррелировать shader/graphics-pipeline creation markers, graphics API и optional `GraphicsStateCollection` trace/prewarm workflow. | `PM-OBS-001`, `PM-CAP-001` |
| `PM-REN-001` Render integration context | P3 | planned | Расширить camera/SRP/pass/GRD/VRS context и перейти к integration-neutral snapshot API через additive compatibility facade. Добавлять Editor navigation только при наличии стабильного public Unity API. | `PM-OBS-001` |
| `PM-GRD-001` GPU Resident Drawer telemetry | P3 | planned | Показывать GRD/Forward+/compute support, фактическую активность, effectiveness counters и fallback/degraded reasons. | `PM-OBS-001`, `PM-REN-001` |
| `PM-CI-001` Profile Analyzer and benchmark CI | P3 | planned | Коррелировать session IDs/custom markers с Profile Analyzer и добавить performance tests, baseline thresholds и CI/JUnit artifacts. | `PM-OBS-002` |
| `PM-SESSION-001` Session analysis UI | P3 | design backlog | Timeline, worst-frame inspector, budget violations и scene-scope summaries поверх существующего recorder/export. | stable session/artifact schemas |
| `PM-UI-003` Widgets, themes and layout descriptors | P4 | design backlog | Расширяемые bounded widgets, semantic theme tokens, manifests, layout descriptors и safety limits без steady-state tree rebuild. | `PM-UI-001`, `PM-UI-002`, `PM-OBS-003` |
| `PM-RG-001` Deeper Render Graph diagnostics | P4 | waiting for stable public APIs | Добавлять pass/resource/aliasing/merge counters только через стабильные Unity APIs; сохранять degraded state вместо reflection. | Unity public APIs |
| `PM-ANDROID-001` Android Perfetto/AGI | P4 | experimental candidate | Low-overhead ATrace/ADPF correlation и Editor sidecar для `adb`, Perfetto config, artifact import и AGI workflow. | `PM-CAP-002`, `PM-PLAT-001` |
| `PM-APPLE-001` Apple thermal, MetricKit and Metal | P4 | experimental candidate | Optional native provider/backend для thermal/low-power/MetricKit и Development-only Metal capture. | `PM-CAP-002`, provider seams |
| `PM-PIX-001` Native PIX timing capture | P4 | experimental candidate | Windows-only circular timing capture, завершаемый после alert, с authoritative artifact provenance. | `PM-CAP-001`, `PM-CAP-002` |
| `PM-RDOC-001` Native RenderDoc backend | P4 | experimental candidate | Dynamic-load только уже подключенного RenderDoc API для naming/comments/path/enumeration; не поставлять и не inject RenderDoc binary. | stable `PM-CAP-001` and Unity backend |
| `PM-HDRP-001` HDRP overdraw/heatmap parity | Deferred | research only | Отдельный HDRP Custom Pass/shader/readback prototype и device/API matrix; не включать в ранний основной roadmap. | dedicated research validation |
| `PM-OTEL-001` Remote streaming/OpenTelemetry | Deferred | policy undefined | Opt-in batching/export требует transport, credentials, redaction, privacy и support policy до проектирования API. | security/transport decision |

`PM-LOG-001`, `PM-UI-001` и `PM-UI-002` выпущены в `2026.8.5-2`; GitHub issues #1 и #2 закрыты. Внешнее включение StructuredLog toggle остаётся задачей consuming projects и не держит package feature открытой.

`PM-OBS-001`, `PM-OBS-002` и `PM-OBS-003` выпущены в `2026.8.6-1`; Git tag, normal GitHub Release и npm package опубликованы.

`PM-CAP-001` реализован в feature-ветке; Unity `6000.5.6f1` EditMode `136/136` и PlayMode `12/12` прошли. Полный Unity/URP/HDRP/platform matrix остается обязательным release-candidate gate.

## Release Sequence

| Phase | Scope | Exit Gate |
| --- | --- | --- |
| Stabilization | `PM-UI-001`, `PM-UI-002` | Released in `2026.8.5-2`; Unity 6000.4/6000.5 URP/HDRP editor suites and Windows player smoke passed, with Android device smoke explicitly waived. |
| Capture preview | `PM-CAP-001` | Fake-backend contract tests, no overlap, deterministic state transitions, guarded attached-tool backend and no claim of an authoritative `.rdc`/`.wpix` path from the Unity wrapper. |
| Capture stable | `PM-CAP-002` | Atomic versioned bundle, truthful artifact provenance, session/alert correlation and external-tool smoke matrix. |
| Observability | `PM-OBS-001` through `PM-OBS-003` | Startup-only discovery, capability dump, custom markers/counters and measured overhead budgets. |
| Platform telemetry | `PM-PLAT-001`, then selected P3 integrations | Optional assemblies, no core hard dependency, explicit unavailable states and device validation. |
| Platform capture previews | Selected P4 backends | Experimental feature flags, native lifecycle/IL2CPP tests and tool-specific artifact confirmation. |

## Required Gates

- Compile/runtime matrix: latest Unity 6000.4 and 6000.5 patches; import-only checks for the declared import floor and Unity 6000.3.
- URP 17.4 and HDRP 17.4; Windows D3D11/D3D12, Linux Vulkan, macOS/iOS Metal, Android Vulkan and explicit GLES degraded mode as applicable.
- Fake backends are mandatory in automated tests; real RenderDoc/PIX/platform-tool smokes are release-candidate gates.
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
