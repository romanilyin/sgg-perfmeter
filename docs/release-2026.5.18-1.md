# Release Plan 2026.5.18-1

This is the working plan for the private SGG PerfMeter release candidate. The repository stays private until a public switch is explicitly approved.

## Scope

Release candidate `2026.5.18-1` includes:

- Source repository and nested Unity Git UPM package `com.sungeargames.perfmeter` under `Assets/Scripts/SGG.PerfMeter`.
- Root README, package-local README, bilingual package documentation, root and package-local changelogs, license and notice files.
- Runtime public API `SGG.PerfMeter.PerformanceMeter` for status, metrics, lifecycle, collection modes, overlay controls, target FPS, session recording/export, alerts, custom metrics, device/camera snapshots, Render Graph snapshots, overdraw measurement, and heatmap visibility.
- Project-owned JSON settings for zero-code setup, overlay presets/modules/tunables, rule defaults, session defaults, and overdraw limits.
- UI Toolkit runtime overlay with compact, graph, full, preset/module-filtered diagnostics, allocation-conscious text refresh, and bounded custom metric rows.
- URP 17 Render Graph feature with opt-in overlay marker, numerical overdraw measurement, visual overdraw heatmap passes, and safe Render Graph analytics snapshot.
- Editor setup/runtime/presets window and MCP command metadata/handlers for setup, runtime, metrics, device, camera, Render Graph, overlay, overdraw, sessions, and alerts.
- Package Manager samples for bootstrap/settings, runtime workflows, and editor/MCP automation.
- EditMode and PlayMode test suites.
- Android Development APK smoke build helper and Android runtime smoke bootstrap.

## Version And Tag

Use calendar version:

```text
2026.5.18-1
```

Planned tag after the final release-prep commit passes gates:

```text
2026.5.18-1
```

Private Git UPM consumers can pin:

```text
git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.18-1
```

Do not push the tag until the final release commit is reviewed and verified. Do not move a pushed tag.

## Prepared State

- Package metadata includes `licensesUrl`, `changelogUrl`, and `documentationUrl` pointing at canonical repository files.
- Root and package-local changelogs track `2026.5.18-1`.
- Package-local README/CHANGELOG are included for Git UPM consumers.
- Security policy, contributing policy, CODEOWNERS, PR template, issue security routing, and manual-only release workflow are present.
- Manual release workflow uses `workflow_dispatch` only and does not publish packages.
- Public switch and registry publication are explicitly deferred.

## Verification Baseline

Latest completed checks before this documentation sync:

- Iteration 43 Render Graph analytics compile: `Logs/opencode-iter43-rendergraph-compile.log`, `ExitCode: 0`.
- Iteration 43 EditMode: `54/54`, `Logs/opencode-iter43-rendergraph-editmode-results.xml`.
- Iteration 43 PlayMode: `5/5`, `Logs/opencode-iter43-rendergraph-playmode-results.xml`.
- Iteration 43 whitespace check: `git diff --check` passed.
- Android Vulkan S23 smoke: `Logs/opencode-android-s23-build-terminal.log`, `overdraw_state=Completed`, no filtered `AndroidRuntime` fatal output.
- Android OpenGLES3 S23 smoke: `Logs/opencode-android-s23-gles-build.log`, expected `overdraw_state=Unsupported`, no filtered `AndroidRuntime` fatal output.

## Pre-Tag Checklist

Run before creating tag `2026.5.18-1`:

```bash
git diff --check
```

Unity compile:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-compile.log"
```

Unity tests without `-quit`:

```bash
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform EditMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode.log"
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform PlayMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode.log"
```

Manual checks:

- Confirm `Assets/Scripts/SGG.PerfMeter/package.json` version is `2026.5.18-1`.
- Confirm README fixed-version examples use `#2026.5.18-1`.
- Confirm release notes and changelogs include the same version.
- Confirm Unity-generated package/version noise from Unity `6000.4.7f1` was not committed unless intentionally changed.
- Confirm no private logs, APKs, `.env` files, or device-specific dumps are staged.

## Deferred Public Work

- Keep the repository private until explicitly approved.
- Do not publish to Unity Asset Store, OpenUPM, npm, or GitHub Releases.
- Do not enable automatic GitHub Actions triggers while the repository is private.
- Before a public switch, review branch protection, secret scanning, vulnerability reporting, public install wording, and CI trigger policy.
