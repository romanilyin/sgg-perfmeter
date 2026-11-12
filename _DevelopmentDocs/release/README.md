# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: `2026.11.12-1`, including all changes merged through `9408eef`.
- Current published GitHub/npm release: `2026.10.13-1`; npm `latest` points to this version.
- Candidate record: [`2026.11.12-session-export-release.md`](2026.11.12-session-export-release.md).
- Candidate verification is reused from completed Unity `6000.6.8f1` and `7000.0.0a16` runs: full EditMode 668/665 passed respectively, one skipped each, zero failures; targeted stream tests 16/16 each. User requested no test rerun.
- Publication uses a normal GitHub Release and the existing npm Trusted Publishing workflow. Public install pins advance after registry/provenance/payload verification.
- Previous release record: [`2026.10.13-unfocused-collection-release.md`](2026.10.13-unfocused-collection-release.md).
- Historical full matrix: [`2026.10.7-1-unity-matrix-release.md`](2026.10.7-1-unity-matrix-release.md).
- Trusted publishing setup: [`npm-trusted-publishing.md`](npm-trusted-publishing.md).
- Supported runtime: Unity `6000.4+` with URP/HDRP `17.4+`. Older Unity versions are import-safe only. Optional native bridge stays pinned to `2026.8.19-1`.

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

The npm publish workflow runs when a normal GitHub Release is published or through its guarded manual recovery from `main`. It uses the GitHub-hosted runner, owner-approved `npm` environment and npm Trusted Publishing OIDC; it must not use an npm write token or an automatic `push`, `pull_request`, `schedule` or `workflow_run` trigger.
