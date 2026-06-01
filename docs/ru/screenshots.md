# Скриншоты

Скриншоты документации лежат в:

```text
docs/assets/screenshots/
```

## Текущие Assets

| Раздел | Файлы | Документ |
| --- | --- | --- |
| Лэндинг и overlay по умолчанию | `presets/preset-default.png` | [Визуальные пресеты](./presets.md#default) |
| Визуальные пресеты | `presets/preset-*.png` | [Визуальные пресеты](./presets.md) |
| Окно настройки, английский UI | `setup-window/setup-window-en-*.png` | [English setup screenshots](../en/setup-window-screenshots.md) |
| Окно настройки, русский UI | `setup-window/setup-window-ru-*.png` | [Скриншоты окна настройки](./setup-window-screenshots.md) |
| Runtime-виджеты | `widgets/*.png` | [Реализованные виджеты](./widgets.md) |

## Галереи

- [Визуальные пресеты](./presets.md)
- [Скриншоты окна настройки](./setup-window-screenshots.md)
- [Реализованные виджеты](./widgets.md)

## Рекомендации

- Сначала делайте читаемые desktop screenshots, а mobile/device examples добавляйте после проверки на устройствах.
- По возможности используйте реальные Unity-сцены вместо пустых тестовых сцен.
- Не используйте local `.ScreenReferences/` images напрямую; это design references, а не product screenshots.
- Держите screenshots достаточно компактными для GitHub и package documentation.
