# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published GitHub/npm release: `2026.11.12-1`; npm `latest` points to this version.
- Previous published release: `2026.10.13-1`.
- [GitHub Release 2026.11.12-1](https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.11.12-1), immutable annotated tag, and package metadata resolve to release commit `eee4ebdff196cf28e7c94965b8c932c3e5d9a374` ([PR #50](https://github.com/romanilyin/sgg-perfmeter/pull/50)).
- npm Trusted Publishing [workflow 43291020817](https://github.com/romanilyin/sgg-perfmeter/actions/runs/43291020817) succeeded. npm signatures/provenance passed; SLSA v1 provenance binds the archive to the exact tag/commit. All 426 published package files match the tag byte-for-byte.
- npm SHA-1: `d6276d730b7f91b6b958d150074cb2913d54087a`.
- Preserved Unity `6000.6.8f1` and `7000.0.0a16` verification: full EditMode 668/665 passed respectively, one skipped each, zero failures; targeted stream tests 16/16 each. User requested no test rerun; no new Unity consumer-import qualification is claimed.
- Current record: [`2026.11.12-session-export-release.md`](2026.11.12-session-export-release.md).
- Previous release record: [`2026.10.13-unfocused-collection-release.md`](2026.10.13-unfocused-collection-release.md).
- Historical full matrix: [`2026.10.7-1-unity-matrix-release.md`](2026.10.7-1-unity-matrix-release.md).
- Trusted publishing setup: [`npm-trusted-publishing.md`](npm-trusted-publishing.md).
- Supported runtime: Unity `6000.4+` with URP/HDRP `17.4+`. Older Unity versions are import-safe only. Optional native bridge stays pinned to `2026.8.19-1`.

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
