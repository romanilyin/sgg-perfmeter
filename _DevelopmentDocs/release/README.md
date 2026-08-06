# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Current published release: `2026.8.6-1`
- Previous published release: `2026.8.5-2`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.6-1
- npm publish: `com.sungeargames.perfmeter@2026.8.6-1` published through Trusted Publishing OIDC with verified SLSA provenance
- npm dist-tag: `latest` -> `2026.8.6-1`
- npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31084894892
- npm SHA-1: `0274ab33642b2399d9f4fa8505ec0bb004bf404e`
- Package: `com.sungeargames.perfmeter`
- Unity validation target: `6000.5.6f1`
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-06
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release notes/checklist: `_DevelopmentDocs/release/2026.8.6-1-profilers-release.md`.
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
