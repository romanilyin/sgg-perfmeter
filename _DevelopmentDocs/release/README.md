# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: `2026.8.8-1`
- Current published release: `2026.8.7-2`
- Current npm `latest`: `2026.8.7-2`
- Previous published release: `2026.8.7-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.7-2
- Git tag `2026.8.7-2` points to main merge commit `3bddc31699bb95ef43bf7df292771f476c99080c`
- Last published npm: `com.sungeargames.perfmeter@2026.8.7-2` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.7-2`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31222122073 (completed successfully and published npm)
- Last published npm SHA-1: `d8dec280b669a049140c65896e75a7fe3ba64afd`
- Last published npm integrity: `sha512-rooQCnGEIbwm7ST3+3Z31ag/4517gfFat2tYt/lpryE7CwBUDNX4J+rkvDf6Mxeh/ngbzEfKYGYQosBSOJvvqw==`
- Public npm and Git UPM install pins point to published `2026.8.7-2`; they were updated only after verified GitHub and npm publication.
- Package: `com.sungeargames.perfmeter`
- Unity validation matrix: `6000.4.12f1`, `6000.5.6f1`, `6000.6.0b7`, and `6000.7.0a4`
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-08
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release-candidate record: `_DevelopmentDocs/release/2026.8.8-1-ftue-continuation-release.md`.
Last published release record: `_DevelopmentDocs/release/2026.8.7-2-setup-ftue-release.md`.
Trusted publishing setup: `_DevelopmentDocs/release/npm-trusted-publishing.md`.

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
