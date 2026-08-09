# Contributor Checks

Use the lightest check that matches the change. Unity compile and Test Runner checks are expensive, so they are expected for runtime/editor behavior changes, not for every documentation-only edit.

## Documentation Or Metadata Only

```bash
git diff --check
```

Also verify affected links and keep affected localized docs in sync when multiple languages are affected.

## Runtime Or Editor Code Changes

Run a Unity compile check for the target project and include the command in the pull request. When tests are relevant, run EditMode and/or PlayMode Test Runner checks.

For maintainer-only release gates or device smoke tests, use the current project-maintainer checklist and mention the command or environment in the pull request.

## Before Opening A Pull Request

- Check `git status` and stage only intended files.
- Do not commit generated Unity state such as `Library/`, `Logs/`, `Temp/`, `Obj/`, or local build outputs.
- Do not commit secrets, `.env` files, device dumps, private logs, or unrelated screenshots.
- If runtime profiler behavior changes, update tests and user-facing docs in the same PR.

## Performance CI

`.github/workflows/performance-ci.yml` runs the full EditMode correctness suite and isolated performance tests on Unity `6000.4.12f1` and `6000.5.6f1` for same-repository pull requests, pushes to `main`, and manual runs. Fork pull requests are skipped because GitHub does not expose Unity license secrets. CI injects `com.unity.test-framework.performance` `3.5.0` only into its ephemeral checkout; the package keeps no hard dependency. Versioned thresholds live in `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`, and CI uploads raw NUnit XML, converted JUnit XML, performance JSON, and logs.

The same workflow runs a separate full PlayMode lifecycle job on both Unity versions when repository variable `PERFMETER_UNITY_CI_ENABLED` is `true` and GameCI-compatible Unity credentials are configured. Changes under `Tests/PlayMode/**` trigger the workflow; disabled credentials and fork pull requests produce an explicit skipped job rather than a false test result.
