# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.9-1`
- Current npm `latest`: `2026.8.9-1`
- Previous published release: `2026.8.8-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#22](https://github.com/romanilyin/sgg-perfmeter/pull/22) merged to `main` as commit `b3894a0ce87c423f200f46e3d9caf64ce5dde2fb`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.9-1 (published 2026-08-09)
- Git tag `2026.8.9-1` points to main merge commit `b3894a0ce87c423f200f46e3d9caf64ce5dde2fb`
- Last published npm: `com.sungeargames.perfmeter@2026.8.9-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.9-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31300896015 (completed successfully and published npm)
- Last published npm SHA-1: `bccd958a68c99de0bf2af3beb56b51307436ac5a`
- Last published npm integrity: `sha512-GZ50UNnkMAhT7swIhOPSl03zsMi9r9O2/xuKMVy9zlH3AnESGkd5CeyDohX8zurfJtMswjRzq8zyh7tfl5YK8Q==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.9-1`, commit `b3894a0ce87c423f200f46e3d9caf64ce5dde2fb`, and workflow run `31300896015`.
- Public npm and Git UPM install pins point to published `2026.8.9-1`; they were updated only after verified GitHub and npm publication.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation matrix: `6000.4.12f1`
- Published release Unity validation evidence: `6000.4.12f1`; see the current release record for targeted and full-suite results.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-09
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.9-1-core-hardening-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.8-1-ftue-continuation-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.7-2-setup-ftue-release.md`.
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
