<div align="center">

# SGG PerfMeter

**Runtime-диагностика производительности и API профилирования для Unity 6000+ URP Render Graph проектов.**

[English](../../README.md) |
[Russian](./README.md)

—

[Быстрый старт](./quick-start.md) |
[API](./api.md) |
[Сравнение](./comparison.md) |
[Changelog](../../CHANGELOG.md)

<p>
  <a href="./installation.md"><img src="../assets/readme/cards/unity.svg" alt="Unity" height="48"></a>
  <a href="./installation.md"><img src="../assets/readme/cards/urp.svg" alt="URP" height="48"></a>
  <a href="./workflows.md#runtime-overlay"><img src="../assets/readme/cards/uitk.svg" alt="UI Toolkit" height="48"></a>
  <a href="./api.md"><img src="../assets/readme/cards/csharp.svg" alt="C#" height="48"></a>
  <a href="./limitations.md"><img src="../assets/readme/cards/android.svg" alt="Android" height="48"></a>
  <a href="./limitations.md"><img src="../assets/readme/cards/ios.svg" alt="iOS" height="48"></a>
  <a href="./quick-start.md"><img src="../assets/readme/cards/docs.svg" alt="Docs" height="48"></a>
</p>

<p>
  <a href="./presets.md#default"><img src="../assets/screenshots/presets/preset-default.png" alt="SGG PerfMeter default overlay preset" width="960"></a>
</p>

</div>

SGG PerfMeter - не просто FPS counter. Это легкий слой runtime-диагностики для Unity URP проектов, который помогает понять, почему кадр стал медленным, что изменилось и как воспроизвести capture.

Одни и те же performance data доступны через несколько вариантов: runtime overlay, публичные C# API snapshots, JSON/CSV session exports, alerts и MCP command metadata для editor/agent automation.

Большинство FPS overlays отвечают: **какой FPS сейчас?**

SGG PerfMeter отвечает: **кадр упирается в CPU, GPU, render thread, present/VSync, overdraw или недоступные platform counters, и можно ли это состояние экспортировать или автоматизировать?**

## Highlights

- Фокус на Unity `6000.4+` и URP `17.4+`, с интеграцией через Render Graph renderer feature.
- FrameTimingManager CPU/GPU timing, main-thread, render-thread и present-wait visibility.
- ProfilerRecorder render, SRP Batcher, BRG/GRD, upload, memory и GPU-memory counters, когда они доступны.
- Bottleneck classification для GPU, CPU main thread, CPU render thread, present/VSync, balanced или unknown frames.
- UI Toolkit runtime overlay с presets, layouts, graphs, metric bars, themes и custom metric rows.
- Session recording с warm-up, scene scope, worst-frame summaries, JSON/CSV export, device metadata и camera metadata.
- Rule alerts со structured logs, callbacks, Editor warning cooldowns и MCP alert commands.
- Opt-in numerical overdraw measurement и visual overdraw heatmap через URP Render Graph.
- Device, camera, Render Graph, status, metrics, alerts, session и custom metric snapshots для кода и MCP automation.

## Быстрый Старт

1. Установите Unity package из этого репозитория с package path `Assets/Scripts/SGG.PerfMeter`.
2. Откройте `SGG/Perfmeter/Setup` в Unity.
3. Запустите recommended setup, войдите в Play Mode и проверьте, что overlay появился.

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Полный setup guide: [Установка](./installation.md) и [Быстрый старт](./quick-start.md).

## Для Кого

- Unity URP developers, которые валидируют performance в Play Mode, development builds и device smoke tests.
- Rendering engineers и technical artists, которым нужны draw calls, SetPass, upload, memory, SRP Batcher, BRG/GRD, overdraw и frame timing visibility.
- Tooling developers, которым нужны стабильные runtime snapshots, а не только visual HUD.
- Команды, использующие Unity MCP или editor agents для profiling automation и regression checks.
- Solo developers, которым нужен более диагностический инструмент, чем обычный FPS overlay.

## Основные Workflow

- **Zero-code overlay**: создайте `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` из setup window и дайте PerfMeter стартовать автоматически.
- **Runtime API**: вызовите `PerformanceMeter.EnsureRunning()`, затем читайте immutable status, metrics, device, camera и session snapshots.
- **Session export**: записывайте bounded profiling windows и экспортируйте JSON/CSV со scene, device, camera, settings, counters, warnings и worst-frame metadata.
- **Overdraw diagnostics**: запускайте bounded numerical measurement или включайте visual heatmap, когда renderer feature установлен.
- **Agent automation**: используйте MCP command metadata, чтобы запускать collection, переключать overlay modes, экспортировать sessions, читать alerts и snapshots.

Подробнее: [Workflow](./workflows.md), [API](./api.md) и [MCP](./mcp.md).

## Скриншоты

Галереи показывают overlay по умолчанию, setup window pages, visual presets и runtime widgets.

Начните с [Визуальные пресеты](./presets.md), [Скриншоты Setup Window](./setup-window-screenshots.md), [Реализованные виджеты](./widgets.md) и [Скриншоты](./screenshots.md).

## Сравнение С FPS Counters

Advanced FPS Counter и Graphy - сильные general-purpose drop-in visual overlays. SGG PerfMeter намеренно фокусируется на modern Unity URP diagnostics: structured timing и render counters, bottleneck classification, reproducible sessions, device/camera snapshots, overdraw diagnostics, Render Graph state и agent-readable automation.

Это product/architecture comparison, а не measured runtime benchmark. См. [Сравнение](./comparison.md).

## Требования

- Unity `6000.4+` для поддерживаемого runtime usage.
- URP `17.4+` с Render Graph path.
- Frame Timing Stats включен перед использованием FrameTimingManager в builds.
- Vulkan предпочтителен на Android, если важен GPU timing.

Unity `2022.3` through `6000.3` может быть import-safe для compile checks, но runtime overlay, Render Graph features, overdraw passes и support expectations требуют Unity `6000.4+` с URP `17.4+`.

## Документация

- [Установка](./installation.md)
- [Быстрый старт](./quick-start.md)
- [Workflow](./workflows.md)
- [Визуальные пресеты](./presets.md)
- [Реализованные виджеты](./widgets.md)
- [API](./api.md)
- [MCP](./mcp.md)
- [Сравнение](./comparison.md)
- [Ограничения](./limitations.md)
- [Troubleshooting](./troubleshooting.md)
- [Скриншоты](./screenshots.md)
- [Скриншоты Setup Window](./setup-window-screenshots.md)
- [Проверки contributor changes](./contributor-checks.md)

Внутренние development, historical roadmap, release-readiness и architecture notes лежат в `_DevelopmentDocs/`.

## Лицензия

Пакет лицензирован по **Stinger Royalty-Free EULA 1.0**.

- Основной текст лицензии: [LICENSE.ru.md](../../LICENSE.ru.md)
- Английский справочный перевод: [LICENSE.md](../../LICENSE.md)
- Notices: [NOTICE.md](../../NOTICE.md) и [NOTICE.ru.md](../../NOTICE.ru.md)
