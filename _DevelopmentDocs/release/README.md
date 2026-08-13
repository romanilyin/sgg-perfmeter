# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.8.13-2`
- Current npm `latest`: `2026.8.13-2`
- Previous published release: `2026.8.13-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release PR: [#33](https://github.com/romanilyin/sgg-perfmeter/pull/33) merged to `main` with an explicitly authorized admin bypass as commit `af135151fa31215620ea3b3f089211e51b41db00`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.8.13-2 (published 2026-08-13)
- Annotated Git tag `2026.8.13-2` dereferences to main merge commit `af135151fa31215620ea3b3f089211e51b41db00`
- Last published npm: `com.sungeargames.perfmeter@2026.8.13-2` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.8.13-2`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/31716569241 (completed successfully and published npm)
- Last published npm SHA-1: `0e5c210ec9b21a9381bdf3abe67ebf7a822bbcd6`
- Last published npm integrity: `sha512-GHq9/OB9dS52Amp5VmzbG/NpfWTvHpqFzPB8s4GiEJI2JFVHHR4OZAmqqEARHnBpdqll6BV9+pSp9MfMg1rm5w==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one registry signature and one attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.8.13-2`, commit `af135151fa31215620ea3b3f089211e51b41db00`, and workflow run `31716569241`.
- Repository-facing public npm and Git UPM install examples point to published `2026.8.13-2`; they were updated only after verified GitHub/npm publication and clean-consumer installs. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.13-1`.
- Package: `com.sungeargames.perfmeter`
- Last published Unity validation: `6000.4.12f1` compile, targeted overlay/settings suites, full EditMode/PlayMode, and clean npm/Git consumers.
- Published release Unity validation evidence records the Android Vulkan build as waived because AndroidPlayer switching was disabled in the installed environment.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-08-13
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.8.13-2-overlay-corrections-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.8.13-1-renderdoc-analyzer-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.12-1-raw-cpu-colors-release.md`.
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
