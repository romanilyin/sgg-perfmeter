# Окно настройки

Откройте окно редактора через `SGG/Perfmeter/Setup`.

## Текущее поведение

- Вкладки **Setup** и **Presets** показывают сохранённые настройки проекта PerfMeter и данные пресетов оверлея: явные строки схемы/версии, совместимости с `legacy` и зарезервированных метаданных — все они доступны только для чтения; также отображаются состав виджетов и числовые значения, нормализуемые при потере фокуса.
- **Runtime** показывает только для чтения диагностику сессии, памяти, graphics-state, render integration и GRD/BRG, включая возможности и состояние необязательных интеграций. Состояния `Unavailable`, `unknown` и отсутствие выборки остаются явными. `Measure Overdraw (project default)` использует специальное sentinel-значение проекта по умолчанию.
- Доступны действия `Session Analysis`, `Profile Analyzer` и `Refresh`. `Start Session` и `Stop Session` доступны только в Play Mode. Открытие или обновление Setup не запускает runtime-сбор.
- Параметры запроса memory snapshot и trace/prewarm для graphics-state относятся только к runtime и не являются настройками проекта.

## Справочные скриншоты

> Скриншоты ниже сделаны до P3.5. Они сохранены только как визуальная справка и не являются актуальным свидетельством завершённого UX окна Setup.

### Настройка

![Вкладка Настройка](../assets/screenshots/setup-window/setup-window-ru-setup.png)

### Пресеты

![Вкладка Пресеты](../assets/screenshots/setup-window/setup-window-ru-presets.png)

### Runtime

![Вкладка Runtime](../assets/screenshots/setup-window/setup-window-ru-runtime.png)

### Debug

![Вкладка Debug](../assets/screenshots/setup-window/setup-window-ru-debug.png)
