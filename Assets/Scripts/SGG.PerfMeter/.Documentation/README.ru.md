# SGG PerfMeter: Package Notes

Эта package-local документация намеренно короткая. Основная пользовательская документация поддерживается на уровне репозитория, чтобы GitHub показывал единую структуру по языкам.

## Основные Документы

- English: `../../../../docs/en/README.md`
- Русский: `../../../../docs/ru/README.md`
- Установка: `../../../../docs/ru/installation.md`
- Быстрый старт: `../../../../docs/ru/quick-start.md`
- API: `../../../../docs/ru/api.md`
- MCP: `../../../../docs/ru/mcp.md`
- Сравнение: `../../../../docs/ru/comparison.md`

## Что Дает Пакет

- UI Toolkit runtime performance overlay.
- FrameTimingManager CPU/GPU timing и ProfilerRecorder counters.
- Bottleneck classification и target-FPS budget visualization.
- Session recording с JSON/CSV export.
- Device, camera, Render Graph, status, metrics, alerts и custom metric snapshots.
- Rule alerts с callback/log/Editor warning cooldowns.
- Opt-in overdraw measurement и visual heatmap через URP Render Graph.
- MCP command metadata для editor/agent automation.

## Поддерживаемая Цель

- Unity `6000.4+`.
- URP `17.4+`.
- Render Graph path.

Старые Unity versions могут импортироваться только для compile-safety и не являются поддерживаемой runtime целью.

## Первый Setup

Откройте `SGG/Perfmeter/Setup`, запустите recommended setup, сохраните JSON settings для zero-code startup и войдите в Play Mode.
