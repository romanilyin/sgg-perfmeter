# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.15-1`
- Current npm `latest`: `2026.8.15-1`
- Previous published release: `2026.8.13-2`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#35](https://github.com/romanilyin/sgg-perfmeter/pull/35) merged to `main` with an explicitly authorized admin bypass as commit `301230dfe43ed5183918079b4cafd02497ee9427`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.15-1 (published 2026-08-15)
- Annotated Git tag `2026.8.15-1` dereferences to main merge commit `301230dfe43ed5183918079b4cafd02497ee9427`
- Last published npm: `com.sungeargames.perfmeter@2026.8.15-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.15-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31882514644 (completed successfully and published npm)
- Last published npm SHA-1: `5f82a971ec53b14a0253a03e3b6582d287e30ebb`
- Last published npm integrity: `sha512-1ms+2C/fNYHFIGQ+ZMWmJXE8tSh1jRP/iF1D3mwPgpJw/5n9g/ed5He3SBJtH16Qmf5/mG1d3TM4elMr6xdsSA==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.15-1`, commit `301230dfe43ed5183918079b4cafd02497ee9427`, and workflow run `31882514644`.
- Repository-facing public npm and Git UPM install examples point to published `2026.8.15-1`; they were updated only after verified GitHub/npm publication and clean-consumer installs. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.13-2`.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation: `6000.4.12f1`, `6000.5.6f1`, `6000.6.0b7`, and `6000.7.0a4` compile plus full EditMode/PlayMode, with clean npm/Git consumers on `6000.4.12f1`.
- The four-version matrix was explicitly one-time; subsequent releases return to one primary Unity validation version unless broader coverage is requested.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-15
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.15-1-smoothed-peak-backdrop-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.13-2-overlay-corrections-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.13-1-renderdoc-analyzer-release.md`.
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
