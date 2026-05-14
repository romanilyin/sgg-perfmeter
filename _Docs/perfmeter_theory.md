# **Архитектура и техническая спецификация идеального инструмента профилирования графики для Unity 6000.4+ URP**

## **Введение и проблематика существующих решений**

В экосистеме разработки на Unity традиционно существует явный разрыв между высокоуровневыми утилитами мониторинга производительности и низкоуровневыми инструментами отладки графического конвейера. Инструменты вроде Graphy или AFPS Counter предоставляют базовую телеметрию (частота кадров, использование оперативной памяти, базовая температура устройства), однако их архитектура зачастую опирается на устаревшие методы сбора данных.1 В частности, использование Time.deltaTime для вычисления времени кадра включает в себя время простоя в ожидании вертикальной синхронизации (VSync) и не позволяет дифференцировать нагрузку между центральным процессором (CPU) и графическим ускорителем (GPU).3 Кроме того, классические инструменты, построенные на базе Unity UI (uGUI), сами по себе вносят значительный вклад в снижение производительности: обновление текстовых полей провоцирует ресурсоемкий процесс перестроения холста (Canvas.SendWillRenderCanvases), увеличивает количество вызовов отрисовки (Draw Calls) и создает аллокации в управляемой куче (GC Alloc).1 Это нарушает принцип "эффекта наблюдателя" в профилировании, когда сам инструмент искажает измеряемые метрики.

С другой стороны, существуют мощные низкоуровневые инструменты, такие как RenderDoc или встроенный Unity Frame Debugger.6 Они предоставляют исчерпывающую информацию о состоянии графического API (Vulkan, DirectX 12, Metal), содержимом буферов и шейдерных инструкциях. Однако эти инструменты предназначены для статического анализа: они требуют приостановки выполнения приложения (захват кадра), что делает невозможным динамический мониторинг производительности в реальном времени при непрерывном игровом процессе.6 Более того, их использование на мобильных устройствах или в автономных сборках (Standalone Builds) часто затруднено сложной настройкой сетевого подключения и высочайшими накладными расходами при захвате данных.

Для новейших версий движка (Unity 6000.4+) и Universal Render Pipeline (URP), который полностью перешел на архитектуру Render Graph 8, требуется принципиально новый класс инструмента. "Идеальный счетчик производительности" должен удовлетворять следующим строгим критериям:

1. **Глубокая интеграция с Render Graph API:** Инструмент должен понимать новую топологию направленного ациклического графа (DAG) URP и уметь извлекать время выполнения отдельных проходов (passes) без нарушения процессов слияния проходов (Pass Merging) и алиасинга ресурсов (Resource Aliasing).8  
2. **Архитектура нулевого вмешательства (Zero-Interference Architecture):** Инструмент обязан математически и архитектурно изолировать собственный вклад в производительность от метрик игрового процесса.9  
3. **Аппаратная точность таймингов в сборках (Builds):** Отказ от Time.deltaTime в пользу прямых запросов к аппаратным таймерам GPU через FrameTimingManager и извлечение данных конвейера через ProfilerRecorder.3  
4. **Специфичные метрики Unity 6:** Поддержка мониторинга новых механизмов оптимизации геометрии, таких как BatchRendererGroup (BRG), GPU Resident Drawer (GRD) и Scriptable Render Pipeline (SRP) Batcher.12  
5. **Объективная визуальная и численная оценка Overdraw:** Предоставление возможности оценивать перерисовку (Fillrate) не только визуально (через аддитивное смешивание), но и численно (в виде конкретного коэффициента) непосредственно в скомпилированном приложении на целевом устройстве.14

В данном документе представлена исчерпывающая техническая спецификация и архитектурное руководство по созданию такого инструмента профилирования графики, лишенного излишних абстракций и сфокусированного исключительно на поиске узких мест (bottlenecks) в графическом конвейере Unity 6000.4+.

## **Фундаментальный уровень сбора метрик: Отказ от абстракций**

Любой инструмент профилирования начинается с получения базовых метрик времени кадра. Идеальный счетчик производительности должен полностью исключить зависимость от высокоуровневых абстракций игрового цикла Unity и напрямую обращаться к подсистемам диспетчеризации задач и графическому драйверу.

### **Аппаратные тайминги через FrameTimingManager**

Для определения истинной причины падения производительности необходимо точно знать, где именно образуется затор: в логике CPU (Update, физика), в подготовке команд рендеринга на CPU (Render Thread), или непосредственно при выполнении шейдеров на GPU.3 Unity предоставляет API FrameTimingManager, который обеспечивает доступ к аппаратным таймстемпам. Для его функционирования в Standalone-сборках (Release и Development) необходимо активировать параметр Frame Timing Stats в настройках Player Settings (раздел Rendering).17

При вызове метода FrameTimingManager.CaptureFrameTimings() и последующем чтении через FrameTimingManager.GetLatestTimings(), инструмент получает доступ к массиву структур FrameTiming, которые содержат следующие критически важные поля 3:

| Параметр FrameTiming | Описание и технический смысл | Применение для поиска узких мест |
| :---- | :---- | :---- |
| cpuFrameTime | Полное время кадра на CPU. Вычисляется как разница во времени между началом текущего и следующего кадра в главном потоке. | Включает в себя время ожидания VSync. Не используется для чистой оценки производительности логики. |
| cpuMainThreadFrameTime | Время полезной работы главного потока (Main Thread). Измеряется от начала кадра до завершения последней задачи потока. | Если это значение превышает бюджет кадра (например, 16.6 мс), приложение упирается в логику (CPU-bound: скрипты, физика, анимации). |
| cpuRenderThreadFrameTime | Время работы потока рендеринга (Render Thread). Измеряется от первой отправленной команды до вызова графического API Present(). | Высокое значение указывает на перегрузку драйвера графики: слишком много Draw Calls, SetPass Calls или тяжелая каллинг-логика. |
| cpuMainThreadPresentWaitTime | Время, в течение которого CPU простаивает в ожидании завершения работы GPU или синхронизации с VSync. | Ключевая метрика. Если это время велико, а VSync отключен, это означает, что GPU не успевает обработать отправленные команды. |
| gpuFrameTime | Аппаратное время выполнения команд на GPU. Измеряется драйвером графики между отправкой работы и сигналом завершения. | Отражает реальную стоимость пиксельных и вершинных шейдеров, Overdraw и пропускной способности памяти (Fillrate). |

**Алгоритм детекции узких мест (Bottleneck Detection):**

Идеальный счетчик использует данные FrameTimingManager для автоматической классификации текущего состояния кадра. Логика классификации выстраивается следующим образом:

1. Если gpuFrameTime \> Целевого бюджета (например, \> 16.6 мс для 60 FPS), а cpuMainThreadPresentWaitTime составляет значительную долю от cpuFrameTime, приложение гарантированно находится в состоянии **GPU Bound**.3 В этом случае дальнейший анализ должен фокусироваться на Overdraw, сложности шейдеров и разрешении текстур.  
2. Если cpuMainThreadFrameTime \> Целевого бюджета, состояние маркируется как **CPU Main Thread Bound**. Внимание разработчика направляется на скрипты (Update), физику или системы частиц на CPU.  
3. Если cpuRenderThreadFrameTime \> Целевого бюджета, состояние классифицируется как **CPU Render Thread Bound**. Проблема кроется в количестве вызовов отрисовки (Draw Calls), смене состояний (SetPass) или отсутствии батчинга (SRP Batcher не работает).

*Важное архитектурное ограничение:* При разработке инструмента для платформы Android необходимо учитывать, что на устройствах, использующих графический API OpenGL ES, аппаратные таймстемпы часто не поддерживаются на уровне драйверов производителей (Mali, Adreno). В таких случаях gpuFrameTime может возвращать нуль или искаженные значения, а сам опрос таймеров может вызвать деградацию производительности.20 Идеальный инструмент должен динамически проверять используемый графический API (SystemInfo.graphicsDeviceType) и предупреждать пользователя, если для профилирования GPU требуется переключение проекта на Vulkan API.

### **Извлечение глубокой телеметрии через ProfilerRecorder API**

До появления ProfilerRecorder в Unity 2020.2, получение детальной статистики (количество полигонов, вызовов отрисовки) в Standalone-сборке требовало внедрения небезопасных хаков или парсинга отладочных логов.23 Новое API ProfilerRecorder предоставляет прямой, строго типизированный доступ к внутренним счетчикам производительности движка (Profiler Counters) с околонулевыми накладными расходами (Zero-Allocation).11

Идеальный счетчик производительности инициализирует набор рекордеров в методе OnEnable и освобождает их в OnDisable через Dispose(). Критически важным аспектом является чтение данных: чтобы избежать выделения памяти в куче (GC Alloc) при чтении истории кадров, инструмент обязан использовать unsafe блок кода с выделением памяти на стеке (stackalloc).11

C\#

static double GetRecorderFrameAverage(ProfilerRecorder recorder) {  
    var samplesCount \= recorder.Capacity;  
    if (samplesCount \== 0) return 0;  
    double r \= 0;  
    unsafe {  
        var samples \= stackalloc ProfilerRecorderSample\[samplesCount\];  
        recorder.CopyTo(samples, samplesCount);  
        for (var i \= 0; i \< samplesCount; \++i) r \+= samples\[i\].Value;  
        r /= samplesCount;  
    }  
    return r;  
}

Такой подход позволяет усреднять значения (например, за последние 15-30 кадров), обеспечивая плавность выводимых на экран графиков без микрофризов от работы сборщика мусора. В таблице ниже систематизированы обязательные маркеры и счетчики, которые должен захватывать инструмент для всестороннего профилирования графики в Unity 6000.4+:

| Категория профилировщика | Идентификатор маркера (Name) | Аналитическая ценность для URP |
| :---- | :---- | :---- |
| ProfilerCategory.Render | SetPass Calls Count | Количество смен состояния графического конвейера (привязка новых шейдеров, текстур). Является главным фактором нагрузки на CPU Render Thread.25 Гораздо критичнее, чем простой Draw Call. |
| ProfilerCategory.Render | Draw Calls Count | Общее количество команд DrawIndexed или DrawArrays, отправленных графическому API. Высокое значение при низком SetPass указывает на хорошую работу SRP Batcher или Instancing.25 |
| ProfilerCategory.Render | Vertices Count | Геометрическая сложность кадра, отправленного на растеризацию. Определяет нагрузку на вершинные шейдеры (Vertex Shaders).25 |
| ProfilerCategory.Render | Batches Count | Количество пакетов (батчей), собранных движком. В контексте SRP Batcher — это количество крупных буферов данных, отправленных на GPU.12 |
| ProfilerCategory.Render | Hybrid Renderer (BRG) Draw Calls Count | Специфичный счетчик Unity 6\. Показывает количество вызовов, обработанных через BatchRendererGroup API (основа GPU Resident Drawer). Ключевой показатель эффективности нового конвейера.12 |
| ProfilerCategory.Memory | System Used Memory | Общий объем физической оперативной памяти, занятый процессом операционной системы. Критично для профилирования на консолях и мобильных платформах.11 |
| ProfilerCategory.Memory | GC Reserved Memory | Память, зарезервированная под управляемую кучу сборщика мусора. Постоянный рост указывает на аллокации в игровом цикле (Update), которые в итоге вызовут паузу (GC Spike).11 |

Использование этих счетчиков позволяет инструменту выводить точную численную сводку в реальном времени, не прибегая к вычислениям на стороне C\#-скриптов инструмента, полностью полагаясь на нативные данные C++ ядра Unity.

## **Архитектура нулевого вмешательства (Zero-Interference Architecture)**

Самая сложная инженерная задача при разработке счетчика производительности — сделать так, чтобы сам счетчик не влиял на те метрики, которые он измеряет. Если счетчик использует тяжеловесную графику для вывода графиков нагрузки GPU, он сам увеличит gpuFrameTime и Draw Calls. Если он часто обновляет текстовые метрики в классическом Unity UI (uGUI), он вызовет колоссальную нагрузку на Canvas.SendWillRenderCanvases и увеличит cpuMainThreadFrameTime.1 В идеальном инструменте эта проблема решается с помощью трехкомпонентной стратегии изоляции.

### **1\. Математическая изоляция оверхеда (Mathematical Overhead Subtraction)**

Современная архитектура Render Graph и Profiler API позволяет инструменту измерять собственную стоимость и математически вычитать ее из общих показателей кадра.10 Это достигается путем оборачивания всех процессов отрисовки интерфейса инструмента в уникальный контекст профилирования.

Инструмент определяет собственный ProfilingSampler (например, new ProfilingSampler("ProfilerOverlayTool")).9 Весь рендеринг пользовательского интерфейса счетчика выделяется в отдельный проход (Custom Render Feature), который выполняется в самом конце кадра (событие RenderPassEvent.AfterRendering или AfterRenderingPostProcessing). Внутри функции ExecutePass этого прохода используется ProfilingScope:

C\#

static void ExecutePass(PassData data, RasterGraphContext context) {  
    using (new ProfilingScope(context.cmd, ProfilingSampler.Get(CustomProfilerIds.ProfilerOverlayTool))) {  
        // Команды отрисовки графиков, текста и UI инструмента  
    }  
}

Параллельно, система сбора данных инициализирует ProfilerRecorder для захвата времени выполнения именно маркера ProfilerOverlayTool. На этапе агрегации данных перед выводом на экран, инструмент корректирует глобальные метрики:

TrueGameGpuTime \= TotalGpuFrameTime \- RecorderToolGpuTime

TrueGameDrawCalls \= TotalDrawCalls \- RecorderToolDrawCalls

Такой подход гарантирует, что пользователь видит абсолютно очищенную статистику игрового кадра (True Metrics), а любые просадки производительности, вызванные рендерингом сложных графиков самого профилировщика, не искажают восприятие оптимизации сцены.9 Разработчик получает уверенность, что цифра 14.2 ms GPU Time относится исключительно к ресурсам игры.

### **2\. UI Toolkit как основа представления данных**

Выбор фреймворка для построения интерфейса профилировщика критически важен. Классический uGUI (Unity UI) или старый IMGUI категорически не подходят для этой задачи. В uGUI любое изменение текста (например, обновление счетчика FPS 60 раз в секунду) вызывает инвалидацию всего Canvas, что приводит к перестроению мешей всех элементов (Canvas Rebuild CPU Spikes) и генерации мусора в куче.1 IMGUI (через OnGUI) работает по принципу немедленного режима, выполняя код компоновки каждый кадр, что порождает тысячи аллокаций и разрушает производительность CPU.29

Для Unity 6000.4+ единственным правильным решением является UI Toolkit (UITK), основанный на парадигме Retained Mode.30 Однако простого перехода на UITK недостаточно; необходимо применять строгие архитектурные практики для обеспечения нулевого оверхеда 28:

* **Исключение Layout Recalculation (Пересчета макета):** Элементы интерфейса профилировщика должны использовать абсолютное позиционирование (position: absolute) и фиксированные размеры.28 Изменение содержимого Label (текста с метриками) не должно влиять на размеры родительских контейнеров. Пересчет макета в UITK (Layout Engine) — это ресурсоемкая операция, и абсолютное позиционирование полностью исключает ее из цикла обновления.  
* **Использование Data Binding с триггером WhenDirty:** В Unity 6 внедрена полноценная система привязки данных (Data Binding) для UI Toolkit.31 Инструмент не должен обновлять значения UI каждый кадр в методе Update(). Вместо этого данные привязываются через BindingUpdateTrigger.WhenDirty. Метод MarkDirty() вызывается агрегатором данных только с определенной частотой (например, 4-5 раз в секунду), или когда значение изменяется более чем на заданную дельту (Threshold). Это радикально снижает затраты времени на Style resolution и Vertex buffer updates.28  
* **Батчинг геометрии UITK:** Все текстуры и иконки профилировщика объединяются в единый динамический атлас (Dynamic Texture Atlas), а элементы стилизуются так, чтобы минимизировать использование сложных шейдеров с масками (Masking) или уникальных материалов, что гарантирует упаковку всего интерфейса профилировщика в 1-2 Draw Calls.28

### **3\. Физическая изоляция через Camera Stacking и Render Graph**

Для предотвращения влияния настроек рендеринга игры на визуализацию профилировщика (например, если игра использует низкий Render Scale или агрессивный Bloom-эффект, текст профилировщика не должен размываться или пикселизироваться), применяется техника Camera Stacking.10

В URP настраивается Overlay Camera, которая отвечает исключительно за отрисовку слоя (Culling Mask), на котором находится UI Toolkit Document профилировщика. Эта камера добавляется в стек основной камеры (Base Camera). В Unity 6 использование Render Graph для Camera Stacking значительно оптимизировано: новая функция сохранения альфа-канала (Alpha Preservation) и передача данных через ContextContainer позволяет накладывать прозрачный интерфейс профилировщика поверх игрового кадра без потери данных HDR-буфера и без дополнительных затрат на промежуточные копии Color Buffer (Blit).10 Это обеспечивает кристально четкое отображение графиков поверх любого, даже сильно модифицированного игрового кадра.

## **Профилирование новейших механизмов батчинга (GRD и SRP Batcher)**

Оценка количества Draw Calls долгое время была главной метрикой оптимизации графики. Однако с развитием Universal Render Pipeline и архитектуры Unity этот показатель уступил место другим, более релевантным метрикам.32 Идеальный профилировщик должен смещать акцент пользователя на правильные данные, чтобы стимулировать корректные архитектурные решения.

### **Аналитика SRP Batcher**

Сценарий, при котором 1000 объектов порождают 1000 Draw Calls, больше не является катастрофическим, если эти объекты обрабатываются Scriptable Render Pipeline (SRP) Batcher.32 Главным источником узкого места на CPU (Render Thread) является смена состояний контекста графического API (привязка новых текстур, компиляция шейдеров, загрузка констант) — так называемые SetPass Calls.

SRP Batcher радикально снижает стоимость вызовов отрисовки за счет объединения объектов, использующих один и тот же вариант шейдера (Shader Variant), даже если они используют разные материалы.32 Данные материалов сохраняются в больших константных буферах (Constant Buffers) в памяти GPU (Per Object large buffer).34 В результате, вместо тысячи SetPass Calls выполняется один, а вызовы отрисовки пролетают конвейер с минимальными накладными расходами CPU.

Следовательно, инструмент должен визуализировать эффективность SRP Batcher, отображая:

1. SetPass Calls — первичная метрика для минимизации.  
2. SRP Batcher Instances Count — количество объектов, успешно прошедших через оптимизированный путь.12  
3. Batches Count (Количество SRP-батчей) — количество уникальных буферов.12 Если это число растет пропорционально Draw Calls, это сигнализирует разработчику о фрагментации (использование множества разных шейдеров или разрушение батчинга из\-за ключевых слов (Shader Keywords)).35

### **Декодирование GPU Resident Drawer и BRG**

В Unity 6 была представлена прорывная технология GPU Resident Drawer (GRD), которая автоматизирует инстансинг объектов без необходимости писать сложный код.36 GRD работает поверх низкоуровневого API BatchRendererGroup (BRG) и переносит колоссальный объем работы по каллингу (Culling) и сортировке объектов с CPU на GPU с использованием вычислительных шейдеров (Compute Shaders).36

При активации GRD в настройках URP Asset (с обязательным использованием Forward+ Rendering Path), классические показатели Draw Calls и метрики SRP Batcher могут стать неинформативными, так как движок упаковывает вызовы в гигантские диспетчерские пулы Hybrid Batch Group.37

Идеальный счетчик для Unity 6000.4+ обязан предоставлять специализированную секцию для мониторинга GRD, считывая данные из следующих ProfilerRecorder счетчиков 12:

* Hybrid Renderer (BRG) Draw Calls Count — количество вызовов, сформированных BatchRendererGroup.  
* Hybrid Renderer (BRG) Instances Count — фактическое количество геометрии, отрисованной через GPU-инстансинг.  
* Index Buffer Upload In Frame Bytes — метрика пропускной способности шины PCIe. Показывает, сколько мегабайт индексов CPU был вынужден отправить на GPU в текущем кадре.12 Для идеально настроенного GRD этот показатель должен стремиться к нулю после первой загрузки сцены, так как данные "резидентно" хранятся в VRAM.

Только предоставляя эти узкоспециализированные метрики, инструмент позволит разработчику оценить, насколько эффективно применяется технология GPU Resident Drawer в конкретной сцене.37

## **Объективная оценка Overdraw (Перерисовки) и Fillrate**

Исчерпание пропускной способности памяти графического процессора (Memory Bandwidth) и перегрузка блоков растеризации (ROP) из\-за избыточной перерисовки пикселей (Overdraw) — основная причина падения gpuFrameTime на мобильных устройствах и в VR-проектах.15 Классические прозрачные частицы, слоистые UI-интерфейсы или плотная растительность заставляют GPU вычислять цвет фрагмента и смешивать его (Alpha Blending) несколько раз для одного и того же экранного пикселя.15

Встроенный в редактор Unity режим Scene View Overdraw Mode показывает перерисовку лишь визуально (теплыми цветами на основе аддитивного блендинга) и недоступен в релизных сборках (Standalone) без активации Development-режима или Rendering Debugger.39 Визуальная оценка субъективна и не позволяет построить график для поиска математических корреляций с падением FPS.

Идеальный счетчик должен внедрять двойной подход к измерению Overdraw: Визуальный (для поиска проблемных зон на экране) и Численный (для графиков и объективной телеметрии).14

### **1\. Вычисление числового коэффициента Overdraw через Compute Shaders**

Для получения точного числа перерисовок (например, значения 3.4, означающего, что каждый пиксель в среднем был закрашен три с половиной раза) применяется инновационный метод с использованием Compute Buffers и атомарных счетчиков во фрагментных шейдерах (Fragment Execution Counts).14

**Архитектурный конвейер программного измерителя Overdraw:**

1. **Выделение памяти (Compute Buffer):** Создается RWStructuredBuffer\<uint\> размером в 1 элемент (глобальный аккумулятор). Буфер очищается (заполняется нулями) в начале каждого кадра.  
2. **Инъекция Custom Render Feature в Render Graph:** Инструмент динамически регистрирует новый проход рендеринга AddRasterRenderPass. В этом проходе сцене отдается команда на перерисовку (Material Replacement) всех объектов.14  
3. **Конфигурация замещающего шейдера (Replacement Shader):** Шейдер предельно упрощается. Отключается запись в буфер глубины (ZWrite Off) и отключается отсечение по глубине (ZTest Always). Цветовой вывод полностью блокируется (ColorMask 0), чтобы минимизировать нагрузку на растеризатор и исключить влияние этого прохода на визуальную картинку игры.41  
4. **Атомарный инкремент:** Вся логика шейдера сводится к одной HLSL-команде во фрагментном шейдере: InterlockedAdd(GlobalCounterBuffer, 1).14 Каждый раз, когда GPU определяет, что фрагмент принадлежит треугольнику на экране, он увеличивает значение глобального счетчика на единицу.  
5. **Асинхронное чтение данных (AsyncGPUReadback):** После завершения растеризации данные из GPU-буфера необходимо вернуть на CPU для отображения в интерфейсе инструмента. Классический вызов ComputeBuffer.GetData() вызовет жесткую блокировку конвейера (CPU Stall), так как CPU будет ждать завершения всех команд GPU.42 Вместо этого используется AsyncGPUReadback.Request(GlobalCounterBuffer,... ). Этот API планирует неблокирующее чтение, и результат будет доступен через 1-2 кадра. Задержка вывода метрики в 30 миллисекунд абсолютно приемлема для профилировщика и не искажает cpuMainThreadFrameTime.  
6. **Нормализация метрики:** В функции обратного вызова (Callback) полученное суммарное количество выполненных фрагментных шейдеров делится на общее количество пикселей экрана: Average Overdraw \= TotalFragments / (Screen.width \* Screen.height).

Этот метод предоставляет эталонно точную цифру нагрузки на Fillrate.14 Однако выполнение дополнительного прохода всей геометрии сцены с отключенным Z-Test удваивает вершинную нагрузку на GPU (удваивает gpuFrameTime). В идеальном инструменте этот модуль профилирования находится в спящем состоянии (Off by default) и активируется разработчиком только по нажатию кнопки "Measure Overdraw" на ограниченное время (например, на 60 кадров для сбора статистики).

### **2\. Визуализация Overdraw в Standalone-сборке**

Для визуальной инспекции проблемных зон интерфейс инструмента должен позволять включать визуальный режим Overdraw непосредственно в скомпилированном билде (на устройстве).43

Реализация осуществляется через еще один настраиваемый Render Pass в Render Graph. В отличие от численного метода, здесь не нужны атомарные буферы. Сцена рендерится с использованием шейдера аддитивного смешивания (Blend One One), ZWrite Off, и ZTest Always.41 Каждому фрагменту задается базовый цвет с низкой интенсивностью, например, float4(0.1, 0.02, 0.0, 1.0). Если в одной точке экрана отрисовывается 10 перекрывающихся прозрачных плоскостей, их значения сложатся (![][image1]), и пиксель станет ярко-красным. В Unity 6 этот проход направляет вывод не на главный экран, а во временную текстуру (RenderTexture) UniversalResourceData.activeColorTexture.45 Затем эта текстура передается в элемент Image интерфейса UI Toolkit профилировщика, обеспечивая наглядную тепловую карту перерисовки, которая может быть активирована в любой момент на целевом устройстве без необходимости подключения редактора.44

## **Аналитика и профилирование самого Render Graph**

Уникальной особенностью Unity 6000.4+ является то, что URP больше не выполняет шейдерные проходы в жестко заданном порядке. Вместо этого он анализирует декларации входов и выходов (inputs/outputs) ресурсов каждого прохода и выстраивает граф выполнения, автоматически оптимизируя выделение памяти (VRAM) и объединяя (Merging) совместимые проходы для устройств с тайловым рендерингом (TBDR \- Tile-Based Deferred Rendering), популярных в мобильном сегменте.8

Классические инструменты не могут предоставить метрики этого процесса. Идеальный счетчик должен уметь анализировать состояние Render Graph в реальном времени.

### **Мониторинг Pass Merging и Resource Aliasing**

Архитектура Render Graph опирается на две фундаментальные оптимизации:

1. **Resource Aliasing (Переиспользование памяти):** Если текстура глубины (Depth Buffer) больше не нужна после этапа Opaque, граф может переиспользовать эту же область VRAM для хранения временных данных Post-Processing.8 Это радикально снижает потребление видеопамяти.  
2. **Pass Merging (Слияние проходов):** На мобильных GPU (TBDR) чтение и запись в глобальную видеопамять (DDR) — самая дорогая операция. Render Graph пытается объединить последовательные проходы так, чтобы они выполнялись в сверхбыстрой кэш-памяти тайла (Tile Memory) без выгрузки в основную память. Для этого используются методы вроде AddRasterRenderPass вместо устаревшего AddUnsafePass.45

Идеальный инструмент профилирования подключается к внутренним структурам RenderGraph (используя reflection или публичные API для расширения Rendering Debugger) и собирает статистику кадра:

* Количество зарегистрированных проходов (Registered Passes).  
* Количество успешно объединенных проходов (Merged Passes). Если процент Merged Passes на мобильном устройстве падает, инструмент может сигнализировать красным цветом о том, что какой-то кастомный ScriptableRendererFeature разработчика использует несовместимые API (например, форсирует чтение из \_CameraColorTexture с использованием SetRenderTarget вместо SetInputAttachment и макроса LOAD\_FRAMEBUFFER\_X\_INPUT).45  
* Пиковый объем выделенной VRAM для временных текстур Render Graph.

Предоставление этих метрик в реальном времени позволяет графическому программисту мгновенно видеть последствия включения тяжелых эффектов (например, SSAO или Volumetric Fog) на структуру графа рендеринга и понимать, почему мобильное устройство перегревается из\-за потери Pass Merging.45

## **Интеграция с встроенным Rendering Debugger (Гибридный подход)**

Вместо того чтобы изобретать колесо и рисовать полностью автономный интерфейс с нуля, идеальный профилировщик может применять гибридный подход, используя инфраструктуру, уже заложенную инженерами Unity.

В URP существует встроенный Rendering Debugger, окно которого можно открыть как в Editor, так и в Development-билдах (например, долгим нажатием на экран устройства или комбинацией L3+R3 на геймпаде).16 Этот инструмент предоставляет базовые вкладки (Display Stats, Material, Lighting), но его архитектура расширяема через API DebugManager.

Идеальный инструмент профилирования выступает в роли расширения (Extension) для DebugManager. В скрипте OnEnable он инжектирует собственные панели виджетов:

C\#

void OnEnable() {  
    var panel \= DebugManager.instance.GetPanel("Advanced Profiler", true);  
    var widgetList \= new List\<DebugUI.Widget\>();  
      
    // Добавление кастомных графиков для GPU Frame Time, Overdraw, SRP Batches  
    widgetList.Add(new DebugUI.Value { displayName \= "GPU Time (ms)", getter \= () \=\> currentGpuTime });  
    widgetList.Add(new DebugUI.Value { displayName \= "BRG Draw Calls", getter \= () \=\> brgDrawCalls });  
      
    panel.children.Add(widgetList.ToArray());  
}

**Преимущества такой интеграции:**

1. **Нулевой оверхед на UI:** Rendering Debugger написан и оптимизирован самими инженерами Unity для минимального влияния на конвейер.49  
2. **Кроссплатформенный ввод:** Не нужно писать логику обработки касаний для мобилок, мыши для ПК и геймпадов для консолей — DebugManager уже обрабатывает эту логику.49

**Недостатки и их решение:** Главный недостаток встроенного Rendering Debugger заключается в том, что он недоступен в релизных сборках (Release Builds). При компиляции релиза Unity применяет флаг Strip Debug Variants, который вырезает код отладчика для экономии размера бинарного файла и шейдеров.16 Поэтому идеальный счетчик использует *гибридную модель*. Если он компилируется с директивой DEVELOPMENT\_BUILD, он встраивается в DebugManager. Если компилируется Release, он разворачивает собственный сверхлегкий UI Toolkit Document с привязкой данных (Data Binding), обеспечивая вывод графиков для финального тестирования "в полевых условиях", когда влияние отладочного кода движка полностью отсутствует.16

## **Заключение**

Инжиниринг счетчика производительности графики для Unity 6000.4+ и Universal Render Pipeline — это задача, требующая глубокого понимания низкоуровневых архитектурных изменений движка. Переход на Render Graph, внедрение GPU Resident Drawer (GRD) и развитие SRP Batcher сделали традиционные метрики, такие как количество Draw Calls, недостаточными и даже обманчивыми для оценки производительности.32

Спецификация "идеального инструмента", изложенная в данном документе, базируется на строгом отказе от абстракций игрового цикла. Инструмент должен опираться исключительно на FrameTimingManager для получения точных аппаратных таймингов GPU и CPU, а также на ProfilerRecorder для извлечения метрик с нулевой аллокацией памяти.3

Важнейшей инновацией архитектуры является математическая и физическая изоляция собственного оверхеда инструмента (Zero-Interference). За счет использования UI Toolkit с абсолютным позиционированием и системой Data Binding (WhenDirty) исключается перестроение макета интерфейса (Layout Recalculation).28 Выделение отрисовки профилировщика в отдельный ProfilingSampler внутри Render Graph позволяет измерять время, затраченное на работу самого инструмента, и математически вычитать его из общих метрик gpuFrameTime, предоставляя разработчику абсолютно чистые ("True") метрики игрового процесса.9

Для решения критической проблемы Fillrate и перерисовки (Overdraw) на мобильных и VR-платформах инструмент не ограничивается субъективной визуальной индикацией (additive blending). Он внедряет архитектуру численного измерения Overdraw на базе Compute Buffers, атомарных операций InterlockedAdd и асинхронного извлечения данных AsyncGPUReadback.14 Это дает возможность получать точный коэффициент перерисовки пикселей непосредственно на целевом устройстве в Standalone-сборке.

Внедрение описанных архитектурных шаблонов позволит создать не просто информационный оверлей, а мощный, детерминированный телеметрический комплекс. Он сможет без погрешностей выявлять бутылочные горлышки (Bottlenecks) в конвейере рендеринга, анализировать эффективность новых технологий инстансинга (BatchRendererGroup) и корректность слияния проходов (Pass Merging) в Render Graph, являясь незаменимым компаньоном графического инженера в реалиях Unity 6000.4+.

#### **Источники**

1. Optimizing Unity UI \- Unity Learn, дата последнего обращения: мая 12, 2026, [https://learn.unity.com/course/doozyui-related-tutorials/tutorial/optimizing-unity-ui](https://learn.unity.com/course/doozyui-related-tutorials/tutorial/optimizing-unity-ui)  
2. Unity UI performance optimization tips, дата последнего обращения: мая 12, 2026, [https://unity.com/how-to/unity-ui-optimization-tips](https://unity.com/how-to/unity-ui-optimization-tips)  
3. Scripting API: FrameTiming \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FrameTiming.html](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FrameTiming.html)  
4. Profiler results captured on target hardware unusable because of profiler overhead?, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/profiler-results-captured-on-target-hardware-unusable-because-of-profiler-overhead/804898](https://discussions.unity.com/t/profiler-results-captured-on-target-hardware-unusable-because-of-profiler-overhead/804898)  
5. Unity performance drain of non-visible UI \- Stack Overflow, дата последнего обращения: мая 12, 2026, [https://stackoverflow.com/questions/42376203/unity-performance-drain-of-non-visible-ui](https://stackoverflow.com/questions/42376203/unity-performance-drain-of-non-visible-ui)  
6. Profiling and debugging with Unity and native platform tools, дата последнего обращения: мая 12, 2026, [https://unity.com/how-to/profiling-and-debugging-tools](https://unity.com/how-to/profiling-and-debugging-tools)  
7. There is a runtime render debugger and no one talks about it \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/there-is-a-runtime-render-debugger-and-no-one-talks-about-it/932520](https://discussions.unity.com/t/there-is-a-runtime-render-debugger-and-no-one-talks-about-it/932520)  
8. Adaptive Development: The Render Graph System | by Lem Apperson | May, 2026 | Medium, дата последнего обращения: мая 12, 2026, [https://medium.com/@lemapp09/adaptive-development-the-render-graph-system-a0ff68f4882f](https://medium.com/@lemapp09/adaptive-development-the-render-graph-system-a0ff68f4882f)  
9. How to create a custom profiling sampler and group all passes for a renderer feature?, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/how-to-create-a-custom-profiling-sampler-and-group-all-passes-for-a-renderer-feature/1700831](https://discussions.unity.com/t/how-to-create-a-custom-profiling-sampler-and-group-all-passes-for-a-renderer-feature/1700831)  
10. Fix UI Post-Processing in Unity\! (Finally\!) \- Render Graph URP Camera Stacking Tutorial (2025) \- YouTube, дата последнего обращения: мая 12, 2026, [https://www.youtube.com/watch?v=7\_Vy0jDqjvM](https://www.youtube.com/watch?v=7_Vy0jDqjvM)  
11. Scripting API: ProfilerRecorder \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html)  
12. Profiler counters reference \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-counters-reference.html](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-counters-reference.html)  
13. Manual: New in Unity 6.4, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/WhatsNewUnity64.html](https://docs.unity3d.com/6000.4/Documentation/Manual/WhatsNewUnity64.html)  
14. Overdraw visualization using render feature and compute buffer \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/overdraw-visualization-using-render-feature-and-compute-buffer/1656162](https://discussions.unity.com/t/overdraw-visualization-using-render-feature-and-compute-buffer/1656162)  
15. Unity Overdraw: Improving the GPU Performance of Your Game | TheGamedev.Guru, дата последнего обращения: мая 12, 2026, [https://thegamedev.guru/unity-gpu-performance/overdraw-optimization/](https://thegamedev.guru/unity-gpu-performance/overdraw-optimization/)  
16. Rendering Debugger | Universal RP | 16.0.6 \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/manual/features/rendering-debugger.html](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/manual/features/rendering-debugger.html)  
17. Update for Frame Timing Manager \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/update-for-frame-timing-manager/860718](https://discussions.unity.com/t/update-for-frame-timing-manager/860718)  
18. FrameTimingManager \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/2023.2/Documentation/Manual/frame-timing-manager.html](https://docs.unity3d.com/2023.2/Documentation/Manual/frame-timing-manager.html)  
19. Best practices for profiling game performance \- Unity, дата последнего обращения: мая 12, 2026, [https://unity.com/how-to/best-practices-for-profiling-game-performance](https://unity.com/how-to/best-practices-for-profiling-game-performance)  
20. GPU Usage Profiler module \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/ProfilerGPU.html](https://docs.unity3d.com/6000.4/Documentation/Manual/ProfilerGPU.html)  
21. Frame timing manager introduction \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/frame-timing-manager.html](https://docs.unity3d.com/6000.4/Documentation/Manual/frame-timing-manager.html)  
22. Need help measuring Raw CPU and GPU Time per Frame Excluding VSync Delay on Android \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/need-help-measuring-raw-cpu-and-gpu-time-per-frame-excluding-vsync-delay-on-android/1513389](https://discussions.unity.com/t/need-help-measuring-raw-cpu-and-gpu-time-per-frame-excluding-vsync-delay-on-android/1513389)  
23. Outputting stats at runtime \- Unity Engine, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/outputting-stats-at-runtime/450228](https://discussions.unity.com/t/outputting-stats-at-runtime/450228)  
24. Expose Statistics API \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/expose-statistics-api/744662](https://discussions.unity.com/t/expose-statistics-api/744662)  
25. Rendering Profiler module reference \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/ProfilerRendering.html](https://docs.unity3d.com/6000.4/Documentation/Manual/ProfilerRendering.html)  
26. GPU Instancing not working? Why amount of batches doesn't change? : r/Unity3D \- Reddit, дата последнего обращения: мая 12, 2026, [https://www.reddit.com/r/Unity3D/comments/1i501pn/gpu\_instancing\_not\_working\_why\_amount\_of\_batches/](https://www.reddit.com/r/Unity3D/comments/1i501pn/gpu_instancing_not_working_why_amount_of_batches/)  
27. Understanding URP: Implementing Renderer Features with Render Graph, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/understanding-urp-implementing-renderer-features-with-render-graph/1712280](https://discussions.unity.com/t/understanding-urp-implementing-renderer-features-with-render-graph/1712280)  
28. Optimizing performance \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html)  
29. Why I chose UI Toolkit over IMGUI to make my first Unity tool with | by Alireza Forghani Toosi, дата последнего обращения: мая 12, 2026, [https://alirezaft98.medium.com/why-i-chose-ui-toolkit-over-imgui-to-make-my-first-unity-tool-with-62e70b34e098](https://alirezaft98.medium.com/why-i-chose-ui-toolkit-over-imgui-to-make-my-first-unity-tool-with-62e70b34e098)  
30. Manual: Comparison of UI systems in Unity, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/UI-system-compare.html](https://docs.unity3d.com/6000.4/Documentation/Manual/UI-system-compare.html)  
31. \[UI Toolkit / Editor\] Best practices for optimizing heavy custom Inspector with thousands of bindings? \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/ui-toolkit-editor-best-practices-for-optimizing-heavy-custom-inspector-with-thousands-of-bindings/1698183](https://discussions.unity.com/t/ui-toolkit-editor-best-practices-for-optimizing-heavy-custom-inspector-with-thousands-of-bindings/1698183)  
32. SRP batcher unity 2020 \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/srp-batcher-unity-2020/861198](https://discussions.unity.com/t/srp-batcher-unity-2020/861198)  
33. Unity Draw Call Batching: The Ultimate Guide (2026 Update), дата последнего обращения: мая 12, 2026, [https://thegamedev.guru/unity-performance/draw-call-optimization/](https://thegamedev.guru/unity-performance/draw-call-optimization/)  
34. Scriptable Render Pipeline Batcher in URP \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.4/Documentation/Manual/SRPBatcher.html](https://docs.unity3d.com/6000.4/Documentation/Manual/SRPBatcher.html)  
35. Troubleshoot the SRP Batcher in URP \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher-Profile.html](https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher-Profile.html)  
36. Beginning Game Development: GPU Resident Drawer | by Lem Apperson \- Medium, дата последнего обращения: мая 12, 2026, [https://medium.com/@lemapp09/beginning-game-development-gpu-resident-drawer-e3f0ca6516f3](https://medium.com/@lemapp09/beginning-game-development-gpu-resident-drawer-e3f0ca6516f3)  
37. Enable the GPU Resident Drawer in URP \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-resident-drawer.html](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-resident-drawer.html)  
38. Entities (Graphics) \+ GPU Resident Drawer \+ GPU Instancing? \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/entities-graphics-gpu-resident-drawer-gpu-instancing/1594113](https://discussions.unity.com/t/entities-graphics-gpu-resident-drawer-gpu-instancing/1594113)  
39. How to get fillrate or average overdraw from unity profiler, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/how-to-get-fillrate-or-average-overdraw-from-unity-profiler/676801](https://discussions.unity.com/t/how-to-get-fillrate-or-average-overdraw-from-unity-profiler/676801)  
40. Rendering Debugger window reference for URP \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rendering-debugger-reference.html](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rendering-debugger-reference.html)  
41. Shader drawing over everything \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/shader-drawing-over-everything/825089](https://discussions.unity.com/t/shader-drawing-over-everything/825089)  
42. Compute Shaders \- Catlike Coding, дата последнего обращения: мая 12, 2026, [https://catlikecoding.com/unity/tutorials/basics/compute-shaders/](https://catlikecoding.com/unity/tutorials/basics/compute-shaders/)  
43. ina-amagami/OverdrawForURP: Scene Overdraw in Universal Render Pipeline. \- GitHub, дата последнего обращения: мая 12, 2026, [https://github.com/ina-amagami/OverdrawForURP](https://github.com/ina-amagami/OverdrawForURP)  
44. Objectively measuring overdraw \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/objectively-measuring-overdraw/653009](https://discussions.unity.com/t/objectively-measuring-overdraw/653009)  
45. Introduction to the render graph system in URP \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.1/Documentation/Manual/urp/render-graph-introduction.html](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/render-graph-introduction.html)  
46. How to make Overlays dynamically update along with component? \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/how-to-make-overlays-dynamically-update-along-with-component/1588727](https://discussions.unity.com/t/how-to-make-overlays-dynamically-update-along-with-component/1588727)  
47. Optimize a render graph \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-optimize.html](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-optimize.html)  
48. Manual: What's new in URP 17.1 (Unity 6.1), дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/6000.1/Documentation/Manual/urp/whats-new/urp-whats-new.html](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/whats-new/urp-whats-new.html)  
49. Rendering Debugger | Universal RP | 14.0.12 \- Unity \- Manual, дата последнего обращения: мая 12, 2026, [https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/features/rendering-debugger.html](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/features/rendering-debugger.html)  
50. How to extend the SRP Rendering Debugger? \- Unity Discussions, дата последнего обращения: мая 12, 2026, [https://discussions.unity.com/t/how-to-extend-the-srp-rendering-debugger/890782](https://discussions.unity.com/t/how-to-extend-the-srp-rendering-debugger/890782)  
51. Rendering Debugger | Universal RP | 12.1.12, дата последнего обращения: мая 12, 2026, [https://docs.unity.cn/Packages/com.unity.render-pipelines.universal@12.1/manual/features/rendering-debugger.html](https://docs.unity.cn/Packages/com.unity.render-pipelines.universal@12.1/manual/features/rendering-debugger.html)

[image1]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAHYAAAAXCAYAAADeD7vuAAADmUlEQVR4Xu2YW4hNURjHP6HILY2MazMjkTdymRTyILlEQnIrU3JJSnmglDzIiwe5FCURJZeUB0bKA+JB8cADSskllyiekEvh/z/rrGnttdc3s/c+zT5NrV/9mvHNts+313+ftfZeIpFIJBKJRLqb/nAtPAkPwonJP2diCpzvF0umN1wFm726ZTDcIeY698KRyT/Xha56DtELtsKj8DhcLOY8CYbAW3A/HAgnw2dwhXuQAk++Cz6E/+Du5J9LgTflEngEvoff4dTEEYYm+ARugv3gQvgCznAPKomsPYdgqBzzu7AFNsDzYm7Wvs5xlTAYzFCntg4+h41OLQSDZYOLxDRXr2AZ0ly4T8KD1Aeegleqv1sOwJtizlEmWXrW4HGf4CynNg6+gQtsgWEy1LO2UGU6/AaXenUNfljeYMeKd4d5DBBzN+aBnx8aJF74R0n3t1zCx5eJ1rMGb0aG6C4jg+A9eEbMN1omwS+SDtYGxZNkoUiw6+FhCYc7Al6V/NOkNkjz4F9J98fZhksIZ6h6ofUcgktIu6SD5RJ6R5yZ1waiBevXNYoEyztrOzwmyXCLhkq0QbIB+v1p9TLReg5hA9SC7ajbC/MDLCNY4odbS6hEGyTWQwFmCXY8fATf5nBN5X9mQ+s5BENjeF0GywW8nsESG+5leE2Kh0q0Qdop4QCzBNvdaD2HaIQvJUOwWoBaXaOWYAkfpB7DcxJec7OiDZIWoFYvE63nEKkAtbp9WvQDtEHt8eoatQQ7RswrRytsk/SamwdtkPhq8EfS/dlg+XSswRf/4WIGLKsc6KxoPYfgqxpf2bRg+WTMJ+SOQruYJy4LnyJ/V39auJExSqqP0x5Fg7Wh2umX526T4uFqgzQavhazU+OyRcxbAd8ONPjaxZ2dlTns7Hw+Ws+EN1WzJLPh8X7Pw+BT8a6Prx1c8Fuq/+bgchfqgZgwSYOYXZtfcGa15pL3G04Y6nU4zavXEi4v+oeY93CX0DXx3FzXL0hy06JstJ7JRjEzitsjH+bewdX2IDAbfhYvG17gCXgbLhMzAEyfW4sWfrNviNmCa3LqW+EHMR9u/SpmSuD01Rncq/VDtTAI3vncQ+0KfqMuivlctw/2dcg5joHyGi6JeWg8De+LucHKJmvPXCp+itlCdGdKLh2v4Ga4QcwW8DbvmAosTBAzmHMk/zelp8CpjbMLr5M/UxvnPQjOogye8vdIJBKJRCKRSBH+A9348syjxTWjAAAAAElFTkSuQmCC>