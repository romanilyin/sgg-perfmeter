# Установка

SGG PerfMeter сейчас распространяется как Unity package с именем `com.sungeargames.perfmeter`. NPM-дистрибуция пока не документируется как текущий способ установки.

## Требования

- Unity `6000.4+` для поддерживаемого runtime usage.
- URP `17.4+` с Render Graph path.
- Runtime support для UI Toolkit.
- Frame Timing Stats включен перед использованием FrameTimingManager в билдах.

## Установка Через Git UPM

Пакет находится внутри репозитория:

```text
Assets/Scripts/SGG.PerfMeter
```

Добавьте зависимость в `Packages/manifest.json` Unity-проекта:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Для приватного репозитория через SSH:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Для повторяемой установки закрепите tag или commit:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.20-1"
  }
}
```

## Локальная Установка

Скопируйте папку в Unity-проект:

```text
Assets/Scripts/SGG.PerfMeter
```

Это удобно для локальной разработки пакета или если Git dependencies не подходят.

## Первичная Настройка Проекта

Откройте:

```text
SGG/Perfmeter/Setup
```

Затем выполните recommended setup:

1. Включите Frame Timing Stats.
2. Установите `PerfMeterRenderGraphFeature` в editable active URP renderer assets.
3. Сохраните JSON settings в `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` для zero-code setup или скопируйте initialization snippet.
4. Войдите в Play Mode и проверьте overlay.

## Samples

Импортируйте package samples из Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
