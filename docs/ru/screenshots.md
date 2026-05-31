# Скриншоты

Скриншоты документации лежат в:

```text
docs/assets/screenshots/
```

## Текущие assets

| File | Назначение |
| --- | --- |
| `presets/preset-default.png` | Default screenshot для лэндинга и capture дефолтного visual preset. |
| `setup-window/setup-window-en-*.png` | Английские страницы setup window. См. English docs. |
| `setup-window/setup-window-ru-*.png` | Русские страницы setup window. См. [Скриншоты Setup Window](./setup-window-screenshots.md). |
| `presets/preset-*.png` | Fullscreen captures визуальных пресетов. См. [Визуальные пресеты](./presets.md). |
| `widgets/*.png` | Зарезервированные paths для runtime widget screenshots. См. [Реализованные виджеты](./widgets.md). |

## Планируемые дополнения

| File | Назначение |
| --- | --- |
| `overlay-overdraw-heatmap.png` | Visual overdraw heatmap. |
| `session-export-summary.png` | Session summary/export result или JSON summary preview. |

## Guidelines

- Сначала сделайте readable desktop screenshots, затем добавьте mobile/device examples.
- По возможности используйте реальные Unity scenes вместо пустых test scenes.
- Не используйте local `.ScreenReferences/` images напрямую; это design references, а не product screenshots.
- Держите screenshots достаточно маленькими для GitHub browsing и package documentation.
