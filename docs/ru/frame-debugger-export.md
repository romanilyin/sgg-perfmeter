# Экспорт Frame Debugger

Ручная выгрузка текущего локального кадра доступна в **SGG > Perfmeter > Frame Debugger Export**. Она создаёт отдельный snapshot событий; существующие форматы session JSON/CSV сохраняются.

## Доступная конфигурация

Адаптер включён для Unity **6000.6.8f1**, Windows x64 Editor, **Direct3D 12**. Первая сцена проверки использует URP **17.6.0**. Для других версий Editor и graphics APIs возвращается явный unavailable. Remote Player в эту версию не входит.

## Порядок работы

1. Открыть **Window > Analysis > Frame Debugger**, выбрать локальный Editor target и включить отладчик. Оставить окно открытым с выбранным событием.
2. Открыть **SGG > Perfmeter > Frame Debugger Export**. Открытие окна не включает отладчик и не запускает сборщик PerfMeter.
3. Задать **Maximum events**: по умолчанию 2048, максимум 8192. **Include shader properties** по умолчанию выключен.
4. Нажать **Export current capture**. Для отмены использовать **Cancel export**.
5. Дождаться конечного статуса и открыть **Show exported files**. Важные выгрузки скопировать из временного каталога.

Во время обхода exporter выбирает события и проверяет свежесть данных. При ручной смене selection/capture обход прекращается, новый выбор сохраняется. Исходный выбор восстанавливается только при сохранённой принадлежности последней selection тому же capture.

Пересоздание native списка/hash также останавливает обход с `Partial`, даже если изображение визуально не изменилось. Базовый полный экспорт проверен на неизменном command buffer с clear и mesh draws в URP-проекте; полный набор URP Render Graph workloads пока не квалифицирован.

## Результат и ограничения

Каталог: `Temp/PerfMeter/FrameDebuggerExports/<export-id>/`.

- `events.json`: schema `sgg.perfmeter.frame-debugger`, version 1; контекст, completeness, raw event types и typed fields с availability/reasons.
- `events.csv`: основные event/shader/pass/geometry/render-target столбцы с безопасным представлением текста для spreadsheet import. Исходный текст сохраняется в JSON.
- `properties.csv`: необязательная длинная форма shader properties и native flags.
- `manifest.json`: размеры, SHA-256 и сведения о выгрузке.

Готовый каталог публикуется атомарно, существующий export ID не перезаписывается. Обход ограничен 60 секундами по умолчанию; неподготовленное событие становится unavailable по своему deadline. Ограничения events/fields/characters дают явно частичную выгрузку. Запись имеет 30-секундный cancellation deadline и общий лимит **64 MiB**; при превышении уменьшить число событий или отключить properties. Хранятся до **8 выгрузок / 256 MiB**. Чужие файлы не удаляются; если места нельзя освободить среди собственных результатов, возвращается ошибка.

Статус **Partial** означает ограниченное покрытие. Проверять missing/truncated counts и availability полей. Event count, draw-call count, Render Graph passes и GPU time имеют разную семантику; exporter не измеряет длительность GPU-события и не восстанавливает ранее замеченный worst frame.

Clear/resolve events не получают сохранённые Unity shader values предыдущего события. Неизвестные типы событий сохраняются. Неподдержанные matrix/buffer layouts обозначаются unavailable; pixels текстур и содержимое buffers не копируются.

Если PerfMeter уже собирает сессию, обход отмечается gaps с причиной `EditorDiagnostic`; такие кадры не попадают в обычные timing/session/alert измерения. Сам exporter сборщик не запускает. Интервал начинается с запроса выгрузки; предыдущее время работы отладчика восстановить нельзя.

В файлах могут находиться названия событий/объектов, keywords и shader properties. Перед передачей другим людям проверить содержимое; автоматическая загрузка отсутствует.
