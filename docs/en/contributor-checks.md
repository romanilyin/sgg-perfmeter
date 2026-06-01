# Contributor Checks

Use the lightest check that matches the change. Unity compile and Test Runner checks are expensive, so they are expected for runtime/editor behavior changes, not for every documentation-only edit.

## Documentation Or Metadata Only

```bash
git diff --check
```

Also verify affected links and keep English/Russian docs in sync when both languages are affected.

## Runtime Or Editor Code Changes

Run a Unity compile check for the target project and include the command in the pull request. When tests are relevant, run EditMode and/or PlayMode Test Runner checks.

Detailed internal command lines, device smoke-test notes, and release-readiness gates live in `_DevelopmentDocs/release-readiness/`.

## Before Opening A Pull Request

- Check `git status` and stage only intended files.
- Do not commit generated Unity state such as `Library/`, `Logs/`, `Temp/`, `Obj/`, or local build outputs.
- Do not commit secrets, `.env` files, device dumps, private logs, or unrelated screenshots.
- If runtime profiler behavior changes, update tests and user-facing docs in the same PR.
