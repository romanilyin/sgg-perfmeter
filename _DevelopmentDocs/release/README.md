# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Candidate

- Current release candidate: `2026.7.19-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- npm publish: included for `2026.7.19-1`
- Package: `com.sungeargames.perfmeter`
- Unity validation target: `6000.5.4f1`
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-07-19
- GitHub Actions release workflow: disabled

Current release notes/checklist: `_DevelopmentDocs/release/2026.7.19-1-alerts-panel-release.md`.

## Local Gates

Docs-only changes:

```bash
git diff --check
```

Unity compile:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-compile.log"
```

Unity tests must run without `-quit`:

```bash
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform EditMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode.log"
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform PlayMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode.log"
```

Optional Android smoke builds:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -executeMethod PerfMeterAndroidBuild.BuildDevelopmentApk -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-android-vulkan-build.log"
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -executeMethod PerfMeterAndroidBuild.BuildDevelopmentApk -perfMeterAndroidGraphics gles3 -perfMeterAndroidApk "Builds/Android/SGGPerfMeter-S23-gles-dev.apk" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-android-gles-build.log"
```

## Workflow State

The previous manual-only GitHub Actions release workflow was removed while docs and release gates are being reorganized. Do not re-enable automatic `push`, `pull_request`, `schedule`, or `workflow_run` triggers unless project policy changes.
