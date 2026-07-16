<div align="center">

# SGG PerfMeter

**Легкая runtime-диагностика производительности и agent-readable profiling для Unity 6 URP+HDRP (FPS meter).**

[English](../../README.md) |
[Русский](./README.md) |
[Deutsch](../de/README.md) |
[Español](../es/README.md) |
[Français](../fr/README.md) |
[Italiano](../it/README.md) |
[日本語](../ja/README.md) |
[한국어](../ko/README.md) |
[Português (Brasil)](../pt-br/README.md) |
[简体中文](../zh-cn/README.md)

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
  <a href="./presets.md#default"><img src="../assets/screenshots/presets/preset-default-landing.png" alt="SGG PerfMeter landing screenshot" width="960"></a>
</p>

</div>

SGG PerfMeter - легкая runtime-диагностика производительности и agent-readable profiling для Unity 6 URP+HDRP (FPS meter).

Находите узкие места кадра, сравнивайте изменения производительности, записывайте воспроизводимые сессии и отдавайте структурированные данные профилирования инструментам и автоматизации.

SGG PerfMeter помогает понять, упирается ли кадр в CPU, GPU, render thread, present/VSync, overdraw или недоступные платформенные счетчики, и позволяет сохранить это состояние для последующего анализа.

## Главное для пользователя

- Видеть контекст узкого места кадра прямо во время игры.
- Переключать визуальные пресеты, графики, полосы метрик, компактные режимы компоновки и строки пользовательских метрик под разные сценарии отладки.
- Записывать воспроизводимые сессии профилирования с warm-up, привязкой к сцене, сводками худших кадров, экспортом JSON/CSV, метаданными устройства и камеры.
- Использовать alerts/оповещения, структурированные логи, callback-и и паузы между Editor warnings, чтобы ловить регрессии без постоянного наблюдения за оверлеем.
- Давать инструментам и автоматизации структурированные данные для сравнений, A/B-тестов и поиска проблемных мест вместо скриншотов или парсинга Console.

## Как показываем и используем данные

- **Runtime-оверлей**: визуальные пресеты, компактные режимы компоновки, графики, полосы метрик и строки пользовательских метрик для просмотра во время игры.
- **Публичный C# API**: неизменяемые снимки для status, metrics, device, camera, Render Graph, alerts/оповещений, sessions и custom metrics.
- **Запись сессий**: ограниченные захваты с warm-up, привязкой к сцене, худшими кадрами, метаданными устройства/камеры и экспортом JSON/CSV.
- **Alerts/оповещения**: структурированные логи, callback-и, паузы между Editor warnings и снимки последних alerts/оповещений.
- **Автоматизация**: метаданные команд MCP позволяют смотреть состояние проекта, сравнивать прогоны, делать A/B-тесты и искать проблемные места через структурированные данные.

## Что умеем измерять

- Состояние Unity `6000.4+` / URP `17.4+` Render Graph и HDRP `17.4+` Custom Pass во время выполнения.
- Тайминги CPU/GPU через FrameTimingManager: CPU frame, main thread, render thread, present wait и GPU frame time, когда доступно.
- Счетчики рендера через ProfilerRecorder: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, upload bytes, memory и GPU memory, когда доступно.
- Классификацию узких мест для GPU, CPU main thread, CPU render thread, present/VSync, balanced или unknown frames.
- Числовое измерение overdraw по запросу и визуальную heatmap overdraw через URP Render Graph; в HDRP overdraw/heatmap возвращают unsupported, при этом core diagnostics остаются доступны.
- Снимки для device, URP/HDRP camera, render integration, status, metrics, alerts/оповещений, session и custom metrics для кода и автоматизации через MCP.

## Быстрый старт

1. Установите пакет Unity через npm registry или Git UPM.
2. Откройте `SGG/Perfmeter/Setup` в Unity.
3. Запустите рекомендованную настройку, войдите в Play Mode и проверьте, что оверлей появился.

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.7.16-1"
  }
}
```

Варианты Git UPM и локальной установки описаны в [Установка](./installation.md) и [Быстрый старт](./quick-start.md).

## Основные сценарии работы

- **Оверлей без кода**: создайте `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` из окна настройки и дайте PerfMeter стартовать автоматически.
- **Runtime API**: вызовите `PerformanceMeter.EnsureRunning()`, затем читайте неизменяемые снимки status, metrics, device, camera и session.
- **Экспорт сессий**: записывайте ограниченные окна профилирования и экспортируйте JSON/CSV со сценой, устройством, камерой, настройками, счетчиками, предупреждениями и метаданными худших кадров.
- **Диагностика overdraw**: запускайте ограниченное числовое измерение или включайте визуальную heatmap, когда URP renderer feature установлен; HDRP явно возвращает unsupported для overdraw/heatmap.
- **MCP-автоматизация**: используйте метаданные команд MCP, чтобы запускать сбор, переключать режимы оверлея, экспортировать сессии, читать alerts/оповещения и снимки.

Подробнее: [Сценарии работы](./workflows.md), [API](./api.md) и [MCP](./mcp.md).

## Скриншоты

Галереи показывают оверлей по умолчанию, страницы окна настройки, визуальные пресеты и виджеты оверлея.

Начните с [Визуальные пресеты](./presets.md), [Скриншоты окна настройки](./setup-window-screenshots.md), [Реализованные виджеты](./widgets.md) и [Скриншоты](./screenshots.md).

## Сравнение с FPS-счетчиками

Advanced FPS Counter и Graphy - сильные универсальные визуальные оверлеи, которые легко подключить к проекту. SGG PerfMeter намеренно фокусируется на диагностике современных проектов Unity URP/HDRP: структурированные тайминги и счетчики рендера, классификация узких мест, воспроизводимые сессии, снимки устройства/камеры, диагностика overdraw в URP, состояние URP Render Graph, состояние HDRP Custom Pass и автоматизация через MCP/API.

Используйте [Сравнение](./comparison.md) как контекст по продукту и архитектуре, а не как измеренный бенчмарк во время выполнения.

## Требования

- Unity `6000.4+` для поддерживаемого использования во время выполнения.
- URP `17.4+` с Render Graph или HDRP `17.4+` с Custom Pass integration.
- Frame Timing Stats включен перед использованием FrameTimingManager в билдах.
- Vulkan предпочтителен на Android, если важен GPU timing.

Unity от `2022.3` до `6000.3` может импортироваться для проверки компиляции, но оверлей во время выполнения, render integration, overdraw passes и ожидаемая поддержка требуют Unity `6000.4+` с URP `17.4+` или HDRP `17.4+`. HDRP overdraw/heatmap не поддерживаются, но core diagnostics остаются доступны.

## Лицензия

Пакет лицензирован по **Stinger Royalty-Free EULA 1.0**.

- Основной текст лицензии: [LICENSE.ru.md](../../LICENSE.ru.md)
- Английский справочный перевод: [LICENSE.md](../../LICENSE.md)
- Уведомления: [NOTICE.md](../../NOTICE.md) и [NOTICE.ru.md](../../NOTICE.ru.md)
- Политика использования бренда: [русская](./brand.md), [английская](../en/brand.md), [немецкая](../de/brand.md), [испанская](../es/brand.md), [французская](../fr/brand.md), [итальянская](../it/brand.md), [японская](../ja/brand.md), [корейская](../ko/brand.md), [бразильская португальская](../pt-br/brand.md), [китайская упрощенная](../zh-cn/brand.md)
