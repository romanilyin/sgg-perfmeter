## Summary

-

## Verification

-

## Branch

- [ ] Branch name follows `CONTRIBUTING.md`.

## PerfMeter Checklist

- [ ] Runtime profiler changes follow `AGENTS.md` and relevant `_DevelopmentDocs/decisions/` notes.
- [ ] User-facing package docs are updated in affected localized docs when behavior changes.
- [ ] Unity-generated noise is not included unless intentionally changed.
- [ ] New files under `Assets/` include matching `.meta` files.
- [ ] `FrameTimingManager` / `ProfilerRecorder` collection remains preferred over `Time.deltaTime` for profiler metrics.
