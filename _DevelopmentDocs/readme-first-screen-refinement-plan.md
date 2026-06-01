# README First-Screen Refinement Plan

Status: planning only. Do not apply these wording changes without a separate implementation pass.

## Goal

Make the first screen easier for a regular Unity URP developer to understand before introducing MCP, agents, and detailed counter terminology.

## Proposed Tagline Direction

Current direction is technically accurate but starts with `agent-readable profiling API`, which can feel niche on first contact.

Candidate user-first pitch:

> SGG PerfMeter helps Unity URP teams understand why a frame is slow - CPU, GPU, render thread, VSync/present, counters, overdraw - and export the same data as structured JSON/CSV for tests and tools.

## README First-Screen Changes To Consider

1. Keep the `not just an FPS counter` positioning.
2. Move `agent-readable` and `MCP` from the first line into a separate `Automation-ready` block.
3. Make the first paragraph explain the user problem: slow frames, bottleneck reason, reproducible capture, export.
4. Keep Unity `6000.4+`, URP `17.4+`, and Render Graph visible in the first screen.
5. Keep the hero screenshot above the fold.

## Highlights Restructure

Replace the current dense technical bullet list with benefit-oriented bullets first:

- Diagnose slow frames while the game is running.
- See CPU, GPU, render-thread, present/VSync, memory, render counters, and overdraw signals.
- Record sessions and export JSON/CSV with device/camera metadata.
- Use overlay presets for quick visual reads.
- Feed the same data to tools, tests, and agents.

Then add a separate `What It Measures` section for technical names:

- `FrameTimingManager` timings.
- `ProfilerRecorder` counters.
- SRP Batcher, BRG/GRD, uploads, memory.
- URP Render Graph diagnostics.
- MCP command metadata.

## Non-Goals

- Do not remove MCP/agent positioning; it is an important differentiator.
- Do not overpromise `zero-overhead`, profiler replacement, or all-pipeline support.
- Do not hide Unity/URP version requirements.
