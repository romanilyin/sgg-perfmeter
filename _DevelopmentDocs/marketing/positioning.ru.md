# Позиционирование SGG PerfMeter

## Одна фраза

**SGG PerfMeter - это Unity 6000+ URP Render Graph diagnostics layer и agent-readable profiling API, а не просто FPS counter.**

## Короткий pitch

SGG PerfMeter дает Unity URP-командам легкий runtime diagnostics layer для билдов, Play Mode, smoke tests и editor/AI automation. Пакет объединяет FrameTimingManager timings, ProfilerRecorder counters, bottleneck classification, overdraw diagnostics, device/camera snapshots, session export, rule alerts, custom metrics, UI Toolkit overlay и MCP commands.

Большинство FPS counters отвечают: **"Какой FPS сейчас?"**

SGG PerfMeter отвечает: **"Почему кадр медленный, что изменилось, как агент может это прочитать и как воспроизвести capture?"**

## Целевая аудитория

- Unity URP developers для PC, Android, iOS/macOS и internal test builds.
- Technical artists и rendering engineers, которым нужны draw-call, SetPass, upload, memory, SRP Batcher, BRG, overdraw и frame-timing diagnostics.
- Tooling developers, которым нужны стабильные runtime API snapshots, а не только visual HUD.
- Команды, использующие Unity MCP или editor agents для automated profiling, smoke tests и regression checks.
- Solo developers, которым нужен более диагностический инструмент, чем обычный FPS counter.

## Не целевая аудитория

- Пользователи, которым нужен только маленький drop-in FPS label со старой Unity support.
- Пользователи, которым нужен generic uGUI dashboard framework.
- Пользователи, которым нужен audio spectrum monitoring.
- Пользователи, которым нужна first-class поддержка Built-in Render Pipeline или HDRP.
- Пользователи, которым нужна глубокая frame capture диагностика уровня Unity Profiler, RenderDoc, Profile Analyzer или Frame Debugger.

## Главные отличия

- URP-specific diagnostic depth: Unity 6000.4+, URP 17.4+, FrameTimingManager, ProfilerRecorder, Render Graph, SRP Batcher, BRG/GRD, upload counters и Render Graph diagnostics.
- Bottleneck classification: GPU-bound, CPU main-thread-bound, CPU render-thread-bound, present-limited, balanced или unknown.
- Structured snapshots: status, metrics, device info, camera state, Render Graph diagnostics, alerts, session summaries и custom metrics доступны коду и MCP, а не только overlay.
- Reproducible captures: session exports включают scene, settings, device, camera и worst-frame metadata.
- Agent-readable workflow: MCP commands запускают и останавливают runtime collection, переключают overlay, экспортируют sessions, читают alerts, device/camera snapshots и управляют overdraw без screenshots или console scraping.
- Explicit overdraw diagnostics: numerical overdraw measurement и visual heatmap - opt-in, bounded URP Render Graph diagnostic modes.

## Messaging pillars

- Fast diagnosis: показать вероятный bottleneck и подозрительные counters во время игры.
- Reproducible profiling: экспортировать session summaries с scene, device, camera и worst-frame metadata.
- Agent-ready telemetry: дать Unity MCP и editor/AI agents structured JSON вместо screenshots.
- Modern URP focus: сделать фокус на Unity 6000+ URP Render Graph вместо old all-pipeline compatibility.
- Safe diagnostics: дорогие diagnostics вроде overdraw measurement и heatmap включаются явно, ограничены по времени и видны в runtime state.

## README tagline

Runtime performance diagnostics and agent-readable profiling API for Unity 6000+ URP Render Graph projects.

## Короткое описание

SGG PerfMeter - low-overhead Unity URP diagnostics package, который отдает CPU/GPU timing, render counters, bottleneck classification, overdraw diagnostics, session export, rule alerts, device/camera snapshots, custom metrics и MCP commands через runtime overlay и structured APIs.

## Comparison sentence

Compared with general-purpose Unity FPS overlays such as Advanced FPS Counter and Graphy, SGG PerfMeter focuses on Unity 6000+ URP Render Graph diagnostics, reproducible runtime sessions, structured snapshots, and agent-readable automation.

## Чего избегать в тексте

- "Ultimate FPS counter", потому что это занижает diagnostics focus.
- "Profiler replacement", потому что Unity Profiler, RenderDoc, Profile Analyzer и Frame Debugger остаются правильными инструментами для deep captures.
- "Zero-overhead", потому что diagnostics имеют стоимость; используйте "low-overhead" и явно описывайте diagnostic costs.
- "Works everywhere", потому что platform support gated и часть metrics может быть unavailable.
