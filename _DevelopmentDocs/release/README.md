# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published GitHub/npm release: `2026.10.12-1`; npm `latest` points to this version.
- Previous published release: `2026.10.7-6`.
- First public release: `2026.6.5-1`.
- Package: `com.sungeargames.perfmeter`.
- [PR #43](https://github.com/romanilyin/sgg-perfmeter/pull/43) merged to main as `df4d00ce95bdb2a72d2bc20ecc7119b1214f6fb6` using the user's explicitly authorized admin merge; branch protection is unchanged.
- Normal [GitHub Release 2026.10.12-1](https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.10.12-1), published 2026-10-12 UTC. Its annotated tag resolves to the merge commit.
- npm Trusted Publishing [workflow 38266617427](https://github.com/romanilyin/sgg-perfmeter/actions/runs/38266617427) succeeded with verified SLSA v1 provenance. All 414 tarball files match the tag; registry signature and provenance verification passed.
- npm SHA-1: `fe712da09d270a1a764099a91b0580defc82bff2`.
- Unity `6000.6.5f1` / URP `17.6.0`: 621 EditMode passed with one expected opt-in replay skipped, 25/25 PlayMode passed. Windows x64 Mono Development Dedicated Server build/runtime passed with no PerfMeter assemblies, 180 frames and zero runtime errors. Server-target Editor tooling and imported samples passed.
- User-approved validation scope is one Unity version. The prior multi-version matrix remains historical evidence; no new Linux/macOS/cloud native build or optional-package device validation is claimed.
- Fresh Git UPM and npm scoped-registry consumers on the same Unity version passed compile/type loading and installed-source comparison. Public install pins advance only after these checks.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration. Dedicated Server/CloudRendering runtime assemblies are excluded. Optional native bridge remains pinned to `2026.8.19-1`.

Current release record: `_DevelopmentDocs/release/2026.10.12-dedicated-server-release.md`.
Previous release record: `_DevelopmentDocs/release/2026.10.7-mcpreports-release.md`.
Historical full matrix: `_DevelopmentDocs/release/2026.10.7-1-unity-matrix-release.md`.
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
