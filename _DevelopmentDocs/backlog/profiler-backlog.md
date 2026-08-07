# Profiler Backlog

Здесь зафиксированы реализованные observability scopes и связанные идеи, которые еще не входят в release scope.

## PM-OBS-002 Unity Profiler Instrumentation

Текущий статус: resolved, released `2026.8.6-1`.

Реализованный scope:

- Internal markers охватывают collect/frame timing, custom metrics, CPU core, device/camera snapshots, bottleneck classification, session/alert capture scopes и JSON/CSV export.
- Internal `Scripts`-category counters используют `FlushOnEndOfFrame`: CPU/GPU frame timing хранится в nanoseconds, availability/active — как `0`/`1`, bottleneck/session/overdraw — как enum codes, custom metric — как count.
- `SGG.PerfMeter.Thermal.Sample` и `SGG.PerfMeter.Thermal.Available` оставлены как reserved internal provider hook: synthetic thermal sample не создается, а availability сбрасывается в `0` до PM-PLAT-001 с реальным provider.

Границы:

- PM-OBS-003 отдельно публикует self-overhead и budget reporting; Profiler instrumentation не вычитает overhead и не меняет export schemas.
- PM-PLAT-001 отвечает за реальный thermal provider; до его появления `Thermal.Available` не означает наличие synthetic sample.

## PM-OBS-003 Self-Observability And Overhead Budgets

Текущий статус: resolved, released `2026.8.6-1`.

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

Текущий статус: реализовано для `PM-SESSION-001`, ожидает общий P3/P3.5 release pass.

- Read-only Editor UI Toolkit window читает только текущие `GetSessionSummary()` и `GetSessionSamples()` и не запускает runtime.
- Virtualized timeline показывает retained samples, timing, cumulative spikes, scene boundaries и graphics-state trace correlation.
- Worst-frame inspector использует authoritative summary и добавляет детали только при наличии matching retained sample.
- Derived budget violations используют strict `>`; CPU-main исключает present wait, GPU требует explicit availability.
- Scene view показывает только authoritative `WholeRun` и `CurrentScene`, не создавая ложные historical durations.
- Unavailable timing остаётся текстовым `Unavailable`; runtime retention и session/export schemas не меняются.

## Rendering Debugger Integration

Текущий статус: не реализовано.

Возможный путь:

- Development-build only panel через Unity rendering/debug UI, если API стабилен для Unity 6000.4+.
- Не делать dependency для release builds.
- Не заменять runtime UI Toolkit overlay.

## XR / World-Space Overlay

Текущий статус: вне scope.

Вернуться к задаче только при наличии конкретного XR target. Постоянный 2D overlay остается основным вариантом для текущего release.
