# Manual Checks

This repository currently uses local checks and a manual-only GitHub Actions release workflow. The workflow is intentionally not triggered by `push` or `pull_request` while the repository remains private.

## Local Unity Checks

Use the installed Unity editor with Android Build Support for Android validation. Current reliable editor path:

```text
/mnt/c/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe
```

Compile check:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-compile.log"
```

Test Runner checks must be launched without `-quit`:

```bash
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform EditMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-editmode.log"
Unity.exe -batchmode -projectPath "C:\Work\Unity\sgg-perfmeter" -runTests -testPlatform PlayMode -testResults "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode-results.xml" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-playmode.log"
```

Android smoke build:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -executeMethod PerfMeterAndroidBuild.BuildDevelopmentApk -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-android-vulkan-build.log"
```

Android GLES fallback build:

```bash
Unity.exe -batchmode -quit -projectPath "C:\Work\Unity\sgg-perfmeter" -executeMethod PerfMeterAndroidBuild.BuildDevelopmentApk -perfMeterAndroidGraphics gles3 -perfMeterAndroidApk "Builds/Android/SGGPerfMeter-S23-gles-dev.apk" -logFile "C:\Work\Unity\sgg-perfmeter\Logs\opencode-release-android-gles-build.log"
```

## Documentation And Metadata Checks

Before a release commit, verify:

```bash
git diff --check
```

Manually confirm:

- `Assets/Scripts/SGG.PerfMeter/package.json` uses the target version.
- `README.md` fixed-version install example uses the target tag.
- Root and package-local changelogs include the target version.
- `docs/release-<version>.md` and `docs/release-notes-<version>.md` exist.
- Package-local `README.md`, `LICENSE.md`, `LICENSE.ru.md`, `NOTICE.md`, and `NOTICE.ru.md` are present inside `Assets/Scripts/SGG.PerfMeter/` for Git UPM consumers.
- New assets under `Assets/` have matching `.meta` files.

## Manual GitHub Workflow

The manual release-readiness workflow is:

```text
.github/workflows/release.yml
```

It must stay manually triggered by:

```text
workflow_dispatch
```

Do not add automatic `push`, `pull_request`, `schedule`, or `workflow_run` triggers while the repository remains private unless the project policy changes.
