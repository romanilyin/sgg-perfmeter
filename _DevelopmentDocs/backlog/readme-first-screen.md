# README First-Screen Backlog

Статус: planning only.

Цель: сделать первый экран README понятнее обычному Unity URP-разработчику до подробностей про MCP и agents.

## Direction

Текущий technical positioning корректен, но первый экран должен быстрее отвечать на вопрос пользователя: почему кадр медленный и как это проверить в билде.

Candidate pitch:

> SGG PerfMeter helps Unity URP teams understand why a frame is slow - CPU, GPU, render thread, VSync/present, counters, overdraw - and export the same data as structured JSON/CSV for tests and tools.

## Что поменять в отдельном README pass

1. Оставить `not just an FPS counter` positioning.
2. Перенести `agent-readable` и `MCP` из первой строки в отдельный блок `Automation-ready`.
3. В первом абзаце объяснить user problem: slow frames, bottleneck reason, reproducible capture, export.
4. Оставить Unity `6000.4+`, URP `17.4+` и Render Graph заметными above the fold.
5. Не обещать all-pipeline support, zero-overhead или profiler replacement.

## Benefit-First Highlights

- Diagnose slow frames while the game is running.
- See CPU, GPU, render-thread, present/VSync, memory, render counters, and overdraw signals.
- Record sessions and export JSON/CSV with device/camera metadata.
- Use overlay presets for quick visual reads.
- Feed the same data to tools, tests, and agents.
