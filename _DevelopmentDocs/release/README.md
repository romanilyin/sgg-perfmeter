# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.19-1`
- Current npm `latest`: `2026.8.19-1`
- Previous published release: `2026.8.15-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release merge: directly merged to `main` with an explicitly authorized admin bypass as commit `f50b9c548cfd1a8e52254c3b827de9577cc1678c`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.19-1 (published 2026-08-19)
- Annotated Git tag `2026.8.19-1` dereferences to main merge commit `f50b9c548cfd1a8e52254c3b827de9577cc1678c`
- Last published npm: `com.sungeargames.perfmeter@2026.8.19-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.19-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/32263568936 (completed successfully and published npm)
- Last published npm SHA-1: `3ca844425b024c2e075db35ca425ccebd87c4fa8`
- Last published npm integrity: `sha512-OLbypoQL0+y1cQXQ394ZJs6YvTaRxEWzI+jEY+jTUqL0Q2P/kQohNCVKbvfstBCYra1Qch+Bua839SHEH9phxg==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.19-1`, commit `f50b9c548cfd1a8e52254c3b827de9577cc1678c`, and workflow run `32263568936`.
- Repository-facing public npm and Git UPM install examples point to published `2026.8.19-1`; they were updated only after verified GitHub/npm publication, clean-consumer installs, and the online production bridge installer test. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.15-1`.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation: `6000.4.12f1`, `6000.5.9f1`, `6000.6.0b7`, and `6000.7.0a4` compile plus full EditMode/PlayMode, with clean npm/Git consumers and the published bridge installer on `6000.5.9f1`.
- The four-version matrix was explicitly requested for this release; subsequent releases return to one primary Unity validation version unless broader coverage is requested.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-19
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.19-1-renderdoc-annotations-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.15-1-smoothed-peak-backdrop-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.13-2-overlay-corrections-release.md`.
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
