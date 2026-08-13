# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.13-1`
- Current npm `latest`: `2026.8.13-1`
- Previous published release: `2026.8.12-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#31](https://github.com/romanilyin/sgg-perfmeter/pull/31) merged to `main` with an explicitly authorized admin bypass as commit `eb633aa359d3008715d380a12358632212c72086`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.13-1 (published 2026-08-13)
- Annotated Git tag `2026.8.13-1` dereferences to main merge commit `eb633aa359d3008715d380a12358632212c72086`
- Last published npm: `com.sungeargames.perfmeter@2026.8.13-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.13-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31675576794 (completed successfully and published npm)
- Last published npm SHA-1: `de457b4b0aaca2e63c565b9e979600ec945f23d6`
- Last published npm integrity: `sha512-5E8h9aoWy1DFqUexNEtRtZzyYOzqm6LSFTz2hI3iNWRAGExXZxQk0qxA1GMbwaDalOtuPbPl3D0MDYGafHgQPw==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.13-1`, commit `eb633aa359d3008715d380a12358632212c72086`, and workflow run `31675576794`.
- Repository-facing public npm and Git UPM install examples point to published `2026.8.13-1`; they were updated only after verified GitHub/npm publication and clean-consumer installs. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.12-1`.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation: `6000.4.12f1` compile, targeted analyzer suites, full EditMode/PlayMode, pinned D3D11/D3D12 replay smoke, and clean npm/Git consumers.
- Published release Unity validation evidence records Vulkan analyzer replay as unverified because the pinned capture fails closed at `open_capture`.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-13
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.13-1-renderdoc-analyzer-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.12-1-raw-cpu-colors-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.11-2-diagnostics-overlay-release.md`.
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
