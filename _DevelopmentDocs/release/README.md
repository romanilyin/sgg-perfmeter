# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.11-2`
- Current npm `latest`: `2026.8.11-2`
- Previous published release: `2026.8.11-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#26](https://github.com/romanilyin/sgg-perfmeter/pull/26) merged to `main` as commit `56dcff41ea5a359d7becdbf7b65e520f90947e1f`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.11-2 (published 2026-08-11)
- Git tag `2026.8.11-2` points to main merge commit `56dcff41ea5a359d7becdbf7b65e520f90947e1f`
- Last published npm: `com.sungeargames.perfmeter@2026.8.11-2` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.11-2`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31533825131 (completed successfully and published npm)
- Last published npm SHA-1: `800f00794b2b983fa25ea49540713dea86f2f65b`
- Last published npm integrity: `sha512-/b9PKTor417+MeXe/hupvoOrp9/dmjxfLgTo0tmhPVZcP/8AsTuknpU65XYFyyCxgNBelMDQS2HxNqBvn3hhTg==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.11-2`, commit `56dcff41ea5a359d7becdbf7b65e520f90947e1f`, and workflow run `31533825131`.
- Public npm and Git UPM install pins point to published `2026.8.11-2`; they were updated only after verified GitHub/npm publication and clean-consumer installs.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation matrix: `6000.4.12f1`, `6000.5.6f1`, `6000.6.0b7`, and `6000.7.0a4`
- Published release Unity validation evidence covers all four rows; see the current release record for targeted and full-suite results.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-11
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.11-2-diagnostics-overlay-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.11-1-renderdoc-bridge-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.9-1-core-hardening-release.md`.
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
