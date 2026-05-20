# Release Process

Release publishing automation is not implemented yet. The repository has local Unity verification and a manual release-readiness workflow that validates documentation and metadata gates without publishing packages or opening the repository.

Current release plan: `docs/release-2026.5.20-1.md`.

## Coordinates

- Canonical repository: `https://github.com/romanilyin/sgg-perfmeter`.
- Unity UPM package: `com.sungeargames.perfmeter`.
- Display name: `SGG PerfMeter`.
- License: `LicenseRef-Stinger-Royalty-Free-EULA-1.0`.
- Current private release candidate: `2026.5.20-1`.

The repository remains private until a public switch is explicitly approved.

## Release Scope

The private release candidate includes the nested Unity package under `Assets/Scripts/SGG.PerfMeter/`, the sample Unity project used for validation, Android smoke-build helpers, package documentation, and release-readiness repository metadata.

For the first private candidate, Unity distribution is Git UPM by repository tag and path:

```text
git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.5.20-1
```

Do not publish to Unity Asset Store, OpenUPM, a package registry, or GitHub Releases yet.

## Gates

Before tagging a private release candidate:

- Run Unity batchmode compile.
- Run EditMode and PlayMode Test Runner checks without `-quit`.
- Run `git diff --check`.
- Confirm root and package-local changelogs include the target version.
- Confirm `Assets/Scripts/SGG.PerfMeter/package.json`, README pin examples, release plan, and release notes use the same version.
- Confirm package-local `README.md`, `LICENSE.md`, `LICENSE.ru.md`, `NOTICE.md`, and `NOTICE.ru.md` are included for Git UPM consumers.
- Confirm no generated Unity noise is committed from `Library/`, `Logs/`, `UserSettings/`, `Build*/`, `Temp/`, `Obj/`, `ProjectVersion.txt`, or package dependency upgrades unrelated to the release.

Before a device validation release:

- Rebuild and run the Android Vulkan smoke APK on Galaxy S23 or another target device.
- Rebuild and run the Android OpenGLES3 fallback smoke APK.
- Record `SGG_PERFMETER_SMOKE` logcat markers for runtime status, frame timing availability, GPU timing availability, and overdraw state.

## Tag Policy

Create and push the source tag only after the release commit passes gates:

```bash
git tag 2026.5.20-1
git push origin 2026.5.20-1
```

Do not move an existing pushed tag. If anything changes after a pushed tag, create the next daily suffix such as `2026.5.18-2`.

## Public Switch

Public release is deferred. Before opening the repository:

- Re-run all private release gates.
- Re-review `SECURITY.md`, `NOTICE.md`, `LICENSE.md`, and package metadata URLs.
- Decide whether GitHub Actions should stay manual-only or gain `pull_request` / `push` triggers.
- Enable secret scanning and private vulnerability reporting if available.
- Create a minimal PR after enabling PR-triggered CI, then configure required checks and branch protection only after check names are stable.

## Post-Release Notes

Use `docs/release-notes-2026.5.20-1.md` as the private release notes draft. If a public GitHub Release is later created, review the notes for public wording and mark the release as pre-release if any shipped surface remains experimental.
