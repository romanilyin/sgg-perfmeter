# Profiler Backlog

Полезные идеи, которые еще не являются реализованным release scope.

## PM-OBS-002 Unity Profiler Instrumentation

Текущий статус: реализовано на `feature/profilers`, не выпущено отдельно.

Реализованный scope:

- Internal markers охватывают collect/frame timing, custom metrics, CPU core, device/camera snapshots, bottleneck classification, session/alert capture scopes и JSON/CSV export.
- Internal `Scripts`-category counters используют `FlushOnEndOfFrame`: CPU/GPU frame timing хранится в nanoseconds, availability/active — как `0`/`1`, bottleneck/session/overdraw — как enum codes, custom metric — как count.
- `SGG.PerfMeter.Thermal.Sample` и `SGG.PerfMeter.Thermal.Available` оставлены как reserved internal provider hook: synthetic thermal sample не создается, а availability сбрасывается в `0` до PM-PLAT-001 с реальным provider.

Границы:

- PM-OBS-003 отвечает за публикацию self-overhead, accounting и budget/gate reporting; текущая instrumentation не вычитает overhead и не меняет public API, status, MCP или export schemas.
- PM-PLAT-001 отвечает за реальный thermal provider; до его появления `Thermal.Available` не означает наличие synthetic sample.

## Self-Overhead Accounting

Текущий статус: частично подготовлено, не завершено.

- Overlay marker pass сделан opt-in diagnostic mode.
- Полное вычитание собственного overhead из frame metrics не реализовано.

Что нужно:

1. Отдельно измерять стоимость overlay/render integration markers.
2. Показывать self-overhead как диагностическую метрику, а не скрытую поправку.
3. Не вычитать автоматически из основных цифр, пока нет стабильной валидации на Editor/player/mobile.

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
