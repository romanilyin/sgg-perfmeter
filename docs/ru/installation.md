# Установка

SGG PerfMeter сейчас распространяется как пакет Unity с именем `com.sungeargames.perfmeter`. Для релиза `2026.6.5-1` установка через npm недоступна.

## Требования

- Unity `6000.4+` для поддерживаемого использования во время выполнения.
- URP `17.4+` с Render Graph.
- Поддержка UI Toolkit в runtime.
- Frame Timing Stats включен перед использованием FrameTimingManager в билдах.

## Установка через Git UPM

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

Если в вашем окружении Git-зависимости используют SSH:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Для повторяемой установки закрепите тег или коммит:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-1"
  }
}
```

## Локальная Установка

Скопируйте папку в Unity-проект:

```text
Assets/Scripts/SGG.PerfMeter
```

Этот вариант удобен для локальной разработки пакета или проектов, где Git-зависимости не подходят.

## Первичная настройка проекта

Откройте окно:

```text
SGG/Perfmeter/Setup
```

Затем выполните рекомендованную настройку:

1. Включите Frame Timing Stats.
2. Установите `PerfMeterRenderGraphFeature` в доступные для редактирования активные ассеты URP Renderer.
3. Сохраните JSON-настройки в `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` для запуска без кода или скопируйте фрагмент инициализации.
4. Войдите в Play Mode и проверьте оверлей.

## Примеры

Импортируйте примеры пакета из Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
