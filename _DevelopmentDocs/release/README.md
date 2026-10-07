# Release Notes For Developers

Этот раздел хранит только минимум внутренней release-prep информации. Публичная публикация и GitHub Release выполняются только в запланированный release pass.

## Current Release

- Release candidate: none.
- Current published release: `2026.10.7-6`
- Current npm `latest`: `2026.10.7-6`
- Previous published release: `2026.10.7-1`
- First public release: `2026.6.5-1`
- GitHub Release type: normal release
- Release merge: [PR #40](https://github.com/romanilyin/sgg-perfmeter/pull/40) merged to `main` with an explicitly authorized admin bypass as commit `f4a2411bd560b996fc736c0c83bd7d0a8c497535`
- Last published GitHub Release: https://github.com/romanilyin/sgg-perfmeter/releases/tag/2026.10.7-6 (published 2026-10-07 UTC)
- Annotated Git tag `2026.10.7-6` dereferences to main merge commit `f4a2411bd560b996fc736c0c83bd7d0a8c497535`
- Last published npm: `com.sungeargames.perfmeter@2026.10.7-6` through Trusted Publishing OIDC with verified SLSA provenance v1
- npm dist-tag: `latest` -> `2026.10.7-6`
- Last published npm workflow run: https://github.com/romanilyin/sgg-perfmeter/actions/runs/37687137447 (completed successfully and published npm)
- Last published npm SHA-1: `2f035e1b9cb35f6cf95e9e40354fea6efaafb6fa`
- Last published npm integrity: `sha512-8yyfgnz2tLr7OYMAqZ8inBLiIavAaBEEsncJPgfW/uDt2pvzkVzx96yLt86VpjnF8GsCU0o1zxJmfCUZsvtFYg==`
- Registry signature key ID: `SHA256:DhQ8wR5APBvFHLF/+Tc+AYvPOdTpcIDqOhxsBHRwC7U`
- npm audit signatures: one package's registry signature and provenance attestation verified.
- SLSA provenance v1 resolves `refs/tags/2026.10.7-6`, commit `f4a2411bd560b996fc736c0c83bd7d0a8c497535`, and workflow run `37687137447`. All 408 tarball files match immutable Git blobs byte-for-byte.
- Repository-facing public npm and Git UPM install examples point to verified `2026.10.7-6` after both clean consumers passed. Pins advance through one authorized docs-only PR without another release. The immutable release tag retains prepublication install examples; neither tag nor tarball is rewritten. The optional native bridge remains pinned to `2026.8.19-1`; no new native-capture matrix is claimed.
- Package: `com.sungeargames.perfmeter`
- Last published import-only validation: `2022.3.62f3` / URP `14.0.12`, core/Editor compile/type loading. Older versions are not supported runtime targets.
- Last published primary validation: `6000.6.4f1` / URP `17.6.0`: 148 targeted EditMode, 618 full EditMode passed with one expected opt-in replay ignored, 25/25 full PlayMode. Local exact-sampler D3D12 GPU smoke passed. HDRP `17.6.0` compile/type loading passed; no new HDRP GPU/PlayMode result claimed.
- `6000.5.9f1` / URP `17.5.0`: imported sample compile plus 25/25 adapter cases. `6000.7.0b3` and `7000.0.0a7`: URP compile/type loading only. Fresh true `6000.4` / `17.4` validation is deferred; two IL2CPP builds succeeded but actual Player runtime timed out and remains unverified.
- The prior `2026.10.7-1` ten-version matrix remains historical evidence. This release uses one primary full-suite version; beta/alpha compile results apply only to the exact tested versions and do not imply stable-release support.
- Fresh Git UPM and npm scoped-registry consumers on `6000.6.4f1` passed compile/core/Editor/URP type loading and installed-source/authored-metadata comparison. Git resolved the exact annotated-tag merge commit; npm resolved `2026.10.7-6` from the public registry.
- Runtime target: Unity `6000.4+`, URP `17.4+` Render Graph or HDRP `17.4+` Custom Pass integration
- Release work date: 2026-10-07
- GitHub Actions npm workflow: `.github/workflows/publish-npm.yml`, npm Trusted Publishing with OIDC

Current release record: `_DevelopmentDocs/release/2026.10.7-mcpreports-release.md`.
Current release candidate record: none.
Previous published release record: `_DevelopmentDocs/release/2026.10.7-1-unity-matrix-release.md`.
Earlier published release record: `_DevelopmentDocs/release/2026.8.19-1-renderdoc-annotations-release.md`.
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
