# Скриншоты

Скриншоты готовятся во время обновления документации. Храните source images в:

```text
docs/assets/screenshots/
```

## План Галереи

| File | Purpose |
| --- | --- |
| `hero-overlay-full-diagnostics.png` | Главный hero image для GitHub README с overlay в реальной сцене. |
| `setup-project-settings.png` | Frame Timing Stats и support-target state в setup window. |
| `setup-renderer-features.png` | URP renderer feature detection и install controls. |
| `setup-presets.png` | JSON settings, zero-code setup, work mode и visual preset authoring. |
| `setup-runtime.png` | Play Mode runtime controls. |
| `overlay-fps-only.png` | Minimal FPS/lows overlay. |
| `overlay-graphs.png` | CPU/GPU graphs layout. |
| `overlay-metric-bars.png` | Default metric bars layout. |
| `overlay-overdraw-heatmap.png` | Visual overdraw heatmap. |
| `session-export-summary.png` | Session summary/export result или JSON summary preview. |

## Guidelines

- Сначала сделайте readable desktop screenshots, затем добавьте mobile/device examples.
- По возможности используйте реальные Unity scenes вместо пустых test scenes.
- Не используйте local `.ScreenReferences/` images напрямую; это design references, а не product screenshots.
- Держите screenshots достаточно маленькими для GitHub browsing и package documentation.
