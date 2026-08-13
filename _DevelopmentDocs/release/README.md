# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: `2026.8.13-1`.
- Current published release: `2026.8.12-1`
- Current npm `latest`: `2026.8.12-1`
- Previous published release: `2026.8.11-2`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#29](https://github.com/romanilyin/sgg-perfmeter/pull/29) merged to `main` as commit `726ea1e192cc4f1b064865520993e0ecf3ad8cb0`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.12-1 (published 2026-08-12)
- Git tag `2026.8.12-1` points to main merge commit `726ea1e192cc4f1b064865520993e0ecf3ad8cb0`
- Last published npm: `com.sungeargames.perfmeter@2026.8.12-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.12-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31623627489 (completed successfully and published npm)
- Last published npm SHA-1: `7fc3a779dc63a87f2e8d56ab091d96ff94b0fbb4`
- Last published npm integrity: `sha512-tcY9skyONwaHb/TwkCDLyvVLgLv2kUPDjwry4+6Ux9ZqRKMYP85aIUODC76tiB3jWpvug9CXJWjsaBmlLsgeQA==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.12-1`, commit `726ea1e192cc4f1b064865520993e0ecf3ad8cb0`, and workflow run `31623627489`.
- Repository-facing public npm and Git UPM install examples point to published `2026.8.12-1`; they were updated only after verified GitHub/npm publication and clean-consumer installs. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.11-2`.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation: `6000.4.12f1`; full multi-version matrix explicitly waived for this color-classifier-only patch.
- Published release Unity validation evidence covers compile, targeted boundary tests, full EditMode, and clean npm/Git consumers.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-12
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.12-1-raw-cpu-colors-release.md`.
Current release candidate record: `_DevelopmentDocs/release/2026.8.13-1-renderdoc-analyzer-release.md`.
Previous published release record: `_DevelopmentDocs/release/2026.8.11-2-diagnostics-overlay-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.11-1-renderdoc-bridge-release.md`.
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
