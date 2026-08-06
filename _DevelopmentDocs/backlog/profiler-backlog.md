# Profiler Backlog

Полезные идеи, которые еще не являются реализованным release scope.

## PM-OBS-002 Unity Profiler Instrumentation

Текущий статус: реализовано на `feature/profilers`, не выпущено отдельно.

Реализованный scope:

- Internal markers охватывают collect/frame timing, custom metrics, CPU core, device/camera snapshots, bottleneck classification, session/alert capture scopes и JSON/CSV export.
- Internal `Scripts`-category counters используют `FlushOnEndOfFrame`: CPU/GPU frame timing хранится в nanoseconds, availability/active — как `0`/`1`, bottleneck/session/overdraw — как enum codes, custom metric — как count.
- `SGG.PerfMeter.Thermal.Sample` и `SGG.PerfMeter.Thermal.Available` оставлены как reserved internal provider hook: synthetic thermal sample не создается, а availability сбрасывается в `0` до PM-PLAT-001 с реальным provider.

Границы:

- PM-OBS-003 отдельно публикует self-overhead и budget reporting; Profiler instrumentation не вычитает overhead и не меняет export schemas.
- PM-PLAT-001 отвечает за реальный thermal provider; до его появления `Thermal.Available` не означает наличие synthetic sample.

## PM-OBS-003 Self-Observability And Overhead Budgets

Текущий статус: реализовано на `feature/profilers`, не выпущено отдельно.

Реализованный scope:

- Low-overhead CPU timing и current-thread allocations измеряются в фиксированных окнах по 120 кадров; средние считаются на один callback invocation.
- Компоненты: collector, custom metric providers, CPU-core provider, overlay, URP Render Graph integration и HDRP Custom Pass integration.
- Additive `PerformanceMeter.GetSelfOverhead()`, `PerfMeterStatusSnapshot.SelfOverhead` и объект `self_overhead` в `perfmeter.runtime.status` публикуют состояния, counts, average/max CPU time, total/average allocations и budget states.
- Diagnostic budgets per invocation: collector `0.5 ms`/`0 B`, custom providers `0.5 ms`/`4096 B`, CPU core `1.0 ms`/`0 B`, overlay `2.0 ms`/`131072 B`, URP/HDRP integration `0.5 ms`/`0 B`.

Границы:

- GPU callback attribution остается `Unavailable`: надежное отделение GPU overhead render integration от остального frame work не заявляется.
- Inactive render integration возвращает `Unsupported`; поддерживаемый компонент без вызовов — `NotMeasured`.
- Accounting носит диагностический характер. Self-overhead не вычитается из существующих CPU/GPU metrics, adjusted metrics не добавляются.
- Session JSON/CSV schemas не меняются; performance/allocation budgets не являются автоматической коррекцией или release benchmark guarantee.

## Deeper Render Graph Diagnostics

Текущий статус: есть безопасный URP snapshot с degraded counters.

Будущая работа:

- Использовать публичные Unity APIs, если Unity откроет стабильные counters для pass/resource/aliasing/merge.
- Сохранять degraded `-1` вместо ломкого reflection, когда counters недоступны.
- Добавить явные warnings по custom renderer features, которые ломают mobile-friendly Render Graph paths, только если это можно определить надежно.

## Session Analysis UI

Текущий статус: session recording/export есть, полноценного analysis UI нет.

Будущая работа:

- Timeline с frame time, CPU/GPU, spikes и events.
- Worst-frame inspector.
- Краткий список budget violations.
- Scene-scope summaries в отдельном visual view.

## Rendering Debugger Integration

Текущий статус: не реализовано.

Возможный путь:

- Development-build only panel через Unity rendering/debug UI, если API стабилен для Unity 6000.4+.
- Не делать dependency для release builds.
- Не заменять runtime UI Toolkit overlay.

## XR / World-Space Overlay

Текущий статус: вне scope.

Вернуться к задаче только при наличии конкретного XR target. Постоянный 2D overlay остается основным вариантом для текущего release candidate.
