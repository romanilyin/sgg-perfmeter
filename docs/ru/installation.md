# Установка

SGG PerfMeter распространяется как пакет Unity с именем `com.sungeargames.perfmeter`. Текущая публичная npm-версия: `2026.6.11-1`; установка через Git UPM и локальную копию остается доступной.

## Требования

- Unity `6000.4+` для поддерживаемого использования во время выполнения.
- URP `17.4+` with Render Graph path or HDRP `17.4+` with Custom Pass integration.
- Поддержка UI Toolkit во время выполнения.
- Frame Timing Stats включен перед использованием FrameTimingManager в билдах.

Метаданные пакета используют Unity `2022.3` как import-safety floor для проверок импорта и компиляции. Актуальная поддерживаемая версия Unity для использования во время выполнения начинается с Unity `6000.4+` с URP `17.4+` Render Graph.

## Установка через npm scoped registry

Добавьте npm registry как Unity Package Manager scoped registry в `Packages/manifest.json` Unity-проекта:

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
    "com.sungeargames.perfmeter": "2026.6.11-1"
  }
}
```

Если в manifest уже есть `scopedRegistries`, добавьте запись `npmjs` в существующий массив.

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
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.11-1"
  }
}
```

## Локальная установка

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
2. Установите `PerfMeterRenderGraphFeature` в активные ассеты URP Renderer, доступные для редактирования.
3. Сохраните JSON-настройки в `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` для запуска без кода или скопируйте фрагмент инициализации.
4. Войдите в Play Mode и проверьте оверлей.

## Примеры

Импортируйте примеры пакета из Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
