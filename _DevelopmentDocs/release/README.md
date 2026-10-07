# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: `2026.10.7-6` (one consolidated `mcpreports` release; no intermediate tags/publications).
- Current published release: `2026.10.7-1`
- Current npm `latest`: `2026.10.7-1`
- Previous published release: `2026.8.19-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release merge: [PR #38](https://github.com/romanilyin/sgg-perfmeter/pull/38) merged to `main` with an explicitly authorized admin bypass as commit `d4bf372a741db6f6732cb2b851541a984948c26f`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.10.7-1 (published 2026-10-07)
- Annotated Git tag `2026.10.7-1` dereferences to main merge commit `d4bf372a741db6f6732cb2b851541a984948c26f`
- Last published npm: `com.sungeargames.perfmeter@2026.10.7-1` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.10.7-1`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/37606726290 (completed successfully and published npm)
- Last published npm SHA-1: `6185b2df0c965e4ea134b09297080b56899307c6`
- Last published npm integrity: `sha512-G+OA6ZH5tqvbNj/LTTXeez8utlAj2tgntBxtJxj1zqc2cYN7Hcz2g+8d5z7GR0fKZ3i5cFc5O4cJjt2TW7tFsw==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one package's registry signature and provenance attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.10.7-1`, commit `d4bf372a741db6f6732cb2b851541a984948c26f`, and workflow run `37606726290`.
- Repository-facing public npm and Git UPM install examples point to published `2026.10.7-1`; they advance only after verified GitHub/npm publication and clean-consumer installs. The immutable npm tarball and release tag retain their prepublication package README pin to `2026.8.19-1`. The optional native bridge remains pinned to `2026.8.19-1`; no new native-capture matrix is claimed.
- Package: `com.sungeargames.perfmeter`
- Last published Unity import/compile validation: `2021.3.45f2`, `2022.3.62f3`, `6000.1.17f1`, `6000.2.15f1`, and `6000.3.20f1`.
- Last published Unity runtime validation: `6000.4.12f1`, `6000.5.9f1`, `6000.6.4f1`, `6000.7.0b3`, and `7000.0.0a7`: each has 557 EditMode passed, one opt-in RenderDoc replay ignored, and 20/20 PlayMode passed. HDRP adapter compile/type-loading passed on all five editors; clean npm/Git consumers passed on `6000.6.4f1`.
- The ten-version matrix, including Unity 7 alpha, was explicitly requested for this release; subsequent releases return to one primary Unity validation version unless broader coverage is requested. Beta/alpha results apply only to the exact tested versions.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-10-07
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.10.7-1-unity-matrix-release.md`.
Current release candidate record: `_DevelopmentDocs/release/2026.10.7-mcpreports-release.md`.
Previous published release record: `_DevelopmentDocs/release/2026.8.19-1-renderdoc-annotations-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.15-1-smoothed-peak-backdrop-release.md`.
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
