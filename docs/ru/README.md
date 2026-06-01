<div align="center">

# SGG PerfMeter

**Диагностика производительности во время выполнения и API профилирования для Unity 6000+ URP Render Graph проектов.**

[English](../../README.md) |
[Russian](./README.md)

—

[Установка](./installation.md) |
[Быстрый старт](./quick-start.md) |
[Сценарии работы](./workflows.md) |
[Визуальные пресеты](./presets.md) |
[Реализованные виджеты](./widgets.md) |
[API](./api.md) |
[MCP](./mcp.md) |
[Сравнение](./comparison.md) |
[Ограничения](./limitations.md) |
[Диагностика проблем](./troubleshooting.md) |
[Скриншоты](./screenshots.md) |
[Скриншоты окна настройки](./setup-window-screenshots.md) |
[Проверки изменений](./contributor-checks.md) |
[История изменений](../../CHANGELOG.md)

<p>
  <a href="./installation.md"><img src="../assets/readme/cards/unity.svg" alt="Unity" height="48"></a>
  <a href="./installation.md"><img src="../assets/readme/cards/urp.svg" alt="URP" height="48"></a>
  <a href="./workflows.md"><img src="../assets/readme/cards/uitk.svg" alt="UI Toolkit" height="48"></a>
  <a href="./api.md"><img src="../assets/readme/cards/csharp.svg" alt="C#" height="48"></a>
  <a href="./limitations.md"><img src="../assets/readme/cards/android.svg" alt="Android" height="48"></a>
  <a href="./limitations.md"><img src="../assets/readme/cards/ios.svg" alt="iOS" height="48"></a>
  <a href="./quick-start.md"><img src="../assets/readme/cards/docs.svg" alt="Docs" height="48"></a>
</p>

<p>
  <a href="./presets.md#default"><img src="../assets/screenshots/presets/preset-default.png" alt="SGG PerfMeter default overlay preset" width="960"></a>
</p>

</div>

SGG PerfMeter - не просто счетчик FPS. Это легкий слой runtime-диагностики для Unity URP проектов, который помогает понять, почему кадр стал медленным, что изменилось и как воспроизвести захват.

В отличие от привычных FPS-оверлеев, SGG PerfMeter не просто показывает текущий FPS. Он помогает понять, упирается ли кадр в CPU, GPU, render thread, present/VSync, overdraw или недоступные платформенные счетчики, и позволяет сохранить это состояние для последующего анализа.

## Главное для пользователя

- Понять, почему кадр стал медленным прямо во время игры, а не просто увидеть текущий FPS.
- Переключать визуальные пресеты, графики, полосы метрик, компактные layout-режимы и строки пользовательских метрик под разные сценарии отладки.
- Записывать воспроизводимые сессии профилирования с warm-up, привязкой к сцене, сводками худших кадров, экспортом JSON/CSV, метаданными устройства и камеры.
- Использовать alerts, структурированные логи, callbacks и паузы между Editor warnings, чтобы ловить регрессии без постоянного наблюдения за оверлеем.
- Давать инструментам и агентам структурированные данные для сравнений, A/B-тестов и поиска проблемных мест вместо скриншотов или парсинга Console.

## Как показываем и используем данные

- **Runtime-оверлей**: визуальные пресеты, компактные layout-режимы, графики, полосы метрик и строки пользовательских метрик для просмотра во время игры.
- **Публичный C# API**: неизменяемые снимки status, metrics, device, camera, Render Graph, alerts, sessions и custom metrics.
- **Запись сессий**: ограниченные захваты с warm-up, привязкой к сцене, худшими кадрами, метаданными устройства/камеры и экспортом JSON/CSV.
- **Alerts**: структурированные логи, callbacks, паузы между Editor warnings и снимки последних alerts.
- **Агентский слой**: метаданные команд MCP позволяют агентам смотреть состояние проекта, сравнивать прогоны, делать A/B-тесты и искать проблемные места через структурированные данные.

## Что умеем измерять

- Состояние во время выполнения Unity `6000.4+` / URP `17.4+` Render Graph.
- Тайминги CPU/GPU через FrameTimingManager: CPU frame, main thread, render thread, present wait и GPU frame time, когда доступно.
- Счетчики рендера через ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, memory и GPU memory, когда доступно.
- Классификацию узких мест для GPU, CPU main thread, CPU render thread, present/VSync, balanced или unknown frames.
- Числовое измерение overdraw по запросу и визуальную heatmap overdraw через URP Render Graph.
- Снимки device, camera, Render Graph, status, metrics, alerts, session и custom metrics для кода и автоматизации через MCP.

## Быстрый старт

1. Установите пакет Unity из этого репозитория с путем пакета `Assets/Scripts/SGG.PerfMeter`.
2. Откройте `SGG/Perfmeter/Setup` в Unity.
3. Запустите рекомендованную настройку, войдите в Play Mode и проверьте, что оверлей появился.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Полное руководство по настройке: [Установка](./installation.md) и [Быстрый старт](./quick-start.md).

## Основные сценарии работы

- **Оверлей без кода**: создайте `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` из окна настройки и дайте PerfMeter стартовать автоматически.
- **Runtime API**: вызовите `PerformanceMeter.EnsureRunning()`, затем читайте неизменяемые снимки status, metrics, device, camera и session.
- **Экспорт сессий**: записывайте ограниченные окна профилирования и экспортируйте JSON/CSV со сценой, устройством, камерой, настройками, счетчиками, предупреждениями и метаданными худших кадров.
- **Диагностика overdraw**: запускайте ограниченное числовое измерение или включайте визуальную heatmap, когда renderer feature установлен.
- **Автоматизация агентов**: используйте метаданные команд MCP, чтобы запускать сбор, переключать режимы оверлея, экспортировать сессии, читать alerts и снимки.

Подробнее: [Сценарии работы](./workflows.md), [API](./api.md) и [MCP](./mcp.md).

## Скриншоты

Галереи показывают оверлей по умолчанию, страницы окна настройки, визуальные пресеты и runtime-виджеты.

Начните с [Визуальные пресеты](./presets.md), [Скриншоты окна настройки](./setup-window-screenshots.md), [Реализованные виджеты](./widgets.md) и [Скриншоты](./screenshots.md).

## Сравнение с FPS-счетчиками

Advanced FPS Counter и Graphy - сильные универсальные визуальные оверлеи, которые легко подключить к проекту. SGG PerfMeter намеренно фокусируется на диагностике современных Unity URP проектов: структурированные тайминги и счетчики рендера, классификация узких мест, воспроизводимые сессии, снимки устройства/камеры, диагностика overdraw, состояние Render Graph и автоматизация, понятная агентам.

Это сравнение продукта и архитектуры, а не измеренный runtime-бенчмарк. См. [Сравнение](./comparison.md).

## Требования

- Unity `6000.4+` для поддерживаемого использования во время выполнения.
- URP `17.4+` с Render Graph.
- Frame Timing Stats включен перед использованием FrameTimingManager в билдах.
- Vulkan предпочтителен на Android, если важен GPU timing.

Unity от `2022.3` до `6000.3` может импортироваться для проверки компиляции, но runtime-оверлей, фичи Render Graph, overdraw passes и ожидаемая поддержка требуют Unity `6000.4+` с URP `17.4+`.

## Лицензия

Пакет лицензирован по **Stinger Royalty-Free EULA 1.0**.

- Основной текст лицензии: [LICENSE.ru.md](../../LICENSE.ru.md)
- Английский справочный перевод: [LICENSE.md](../../LICENSE.md)
- Уведомления: [NOTICE.md](../../NOTICE.md) и [NOTICE.ru.md](../../NOTICE.ru.md)
