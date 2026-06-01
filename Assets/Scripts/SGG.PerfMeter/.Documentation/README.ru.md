# SGG PerfMeter: заметки пакета

Эта локальная документация пакета намеренно короткая. Основная пользовательская документация поддерживается на уровне репозитория, чтобы GitHub показывал единую структуру по языкам.

## Основные документы

- Английский: `../../../../README.md`
- Русский: `../../../../docs/ru/README.md`
- Установка: `../../../../docs/ru/installation.md`
- Быстрый старт: `../../../../docs/ru/quick-start.md`
- API: `../../../../docs/ru/api.md`
- MCP: `../../../../docs/ru/mcp.md`
- Сравнение: `../../../../docs/ru/comparison.md`

## Что дает пакет

- Runtime-оверлей производительности на UI Toolkit.
- Тайминги CPU/GPU через FrameTimingManager и счетчики ProfilerRecorder.
- Классификация узких мест и визуализация бюджета target FPS.
- Запись сессий с экспортом JSON/CSV.
- Снимки device, camera, Render Graph, status, metrics, alerts и custom metrics.
- Alerts на правилах с callbacks, логами и паузами между Editor warnings.
- Измерение overdraw по запросу и визуальная heatmap через URP Render Graph.
- Метаданные команд MCP для автоматизации editor/agent-сценариев.

## Поддерживаемая цель

- Unity `6000.4+`.
- URP `17.4+`.
- Render Graph.

Старые версии Unity могут импортироваться только для проверки компиляции и не являются поддерживаемой runtime-целью.

## Первичная настройка

Откройте `SGG/Perfmeter/Setup`, запустите рекомендованную настройку, сохраните JSON-настройки для запуска без кода и войдите в Play Mode.
