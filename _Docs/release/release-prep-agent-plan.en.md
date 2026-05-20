# Agent Plan: Prepare SGG PerfMeter Repository for Release

Audience: coding/documentation agent working inside `romanilyin/sgg-perfmeter`.

Target version: `2026.5.20-1`.

Release state: private release candidate. Do not make the repository public, publish to registries, create public releases, or push tags unless explicitly approved by the owner.

## Goal

Prepare the repository for a clean private/public-ready release pass:

1. Keep README, package docs, marketing docs, release plan, and release notes consistent with the current implementation.
2. Keep English and Russian documentation aligned where bilingual docs exist.
3. Verify package metadata, samples, changelogs, release notes, and release plan.
4. Run the requested local checks for the change type.
5. Produce a final checklist before tagging `2026.5.20-1`.

## Current Facts

The current repository includes more than the older release-candidate docs originally described:

- project-owned JSON settings and zero-code startup;
- collection modes and runtime reset;
- UI Toolkit overlay presets/modules/tunables and allocation-conscious text refresh;
- device/environment snapshots and camera snapshots;
- session recorder with warm-up, scene scope, worst-frame summaries, JSON/CSV export, and MCP session commands;
- rule alerts with callback/log/Editor warning cooldowns and MCP alert commands;
- custom metric providers with API, session JSON, MCP, and bounded overlay rows;
- Render Graph analytics snapshot API and `perfmeter.rendergraph.snapshot`;
- Package Manager samples for bootstrap/settings, runtime workflows, and editor/MCP automation.

Do not leave stale claims that CSV/JSON session export or Render Graph analytics are missing. The accurate overlay statement is: refresh is allocation-conscious and throttled, but changed numeric values and graph legend labels can still materialize managed strings at the refresh interval.

## Files To Check

- `README.md`
- `Assets/Scripts/SGG.PerfMeter/README.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/README.en.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/README.ru.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/STATUS.en.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/STATUS.ru.md`
- `CHANGELOG.md`
- `Assets/Scripts/SGG.PerfMeter/CHANGELOG.md`
- `docs/release-2026.5.20-1.md`
- `docs/release-notes-2026.5.20-1.md`
- `_Docs/perfmeter_status.md`
- `_Docs/perfmeter_iterations.md`
- `_Docs/marketing/positioning.en.md`
- `_Docs/marketing/positioning.ru.md`
- `_Docs/marketing/competitor-comparison.en.md`
- `_Docs/marketing/competitor-comparison.ru.md`

`_Docs/sgg-perfmeter-competitor-comparison-updated.md` is a local reference source and must not be committed.

## Package Metadata

Confirm `Assets/Scripts/SGG.PerfMeter/package.json`:

- `name`: `com.sungeargames.perfmeter`
- `version`: `2026.5.20-1`
- `unity`: `6000.4`
- URP dependency: `17.4.0`
- samples: `Samples~/BootstrapAndSettings`, `Samples~/RuntimeWorkflows`, `Samples~/EditorAutomation`
- URLs point at canonical repository docs/license/changelog.

## Messaging Rules

- Position SGG PerfMeter as a Unity 6000+ URP Render Graph diagnostics layer and agent-readable profiling API.
- State clearly that it complements Unity Profiler, RenderDoc, Profile Analyzer, and Frame Debugger; it does not replace them.
- Compare AFPS and Graphy as visual overlay references, not as code/assets to copy.
- Keep audio modules, screenshot/debug-break alert actions, and player hotkeys out of current scope unless explicitly approved.
- Do not claim zero-overhead or all-platform coverage.

## Verification

For docs-only changes, run at minimum:

```bash
git diff --check
```

For code, package metadata, JSON, asmdef, shader, or Unity asset changes, run Unity compile and the relevant EditMode/PlayMode checks from `AGENTS.md`.

Before committing, inspect:

```bash
git status --short
git diff --stat
git diff --check
```

Do not stage generated Unity state, logs, builds, `.env` files, or the local comparison reference file.
