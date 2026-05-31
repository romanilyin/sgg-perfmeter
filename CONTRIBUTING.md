# Contributing

Russian localization: `CONTRIBUTING.ru.md`.

Read `AGENTS.md` and `_DevelopmentDocs/perfmeter_theory.md` before changing profiler behavior or architecture.

## Pull Requests

All changes to `main` should go through a pull request once the repository moves out of owner-only private preparation. Keep PRs focused on one behavior or repository-maintenance change.

Use the PR template checklist and include the local verification command you ran. If a change affects runtime profiler behavior, update tests and bilingual package documentation in the same PR.

## Branch Naming

Use lowercase ASCII branch names in this form:

```text
<type>/<short-kebab-topic>
```

Allowed types:

```text
feature
fix
docs
l10n
refactor
chore
security
release
```

Examples:

```text
feature/render-graph-analytics
fix/overdraw-readback-session
docs/private-release-notes
l10n/russian-docs
release/2026-5-18-1
```

Rules:

- Use lowercase letters, digits, and hyphens in the topic.
- Keep exactly one slash between the type and topic.
- Use `l10n` for documentation translation or localization-only changes.
- Do not put secrets, hostnames, private directory names, customer names, device serials, or tokens in branch names.
- Do not use `main`, `master`, `release`, or tag-like names such as `v1.2.3` for PR branches.

## Local Checks

For code changes, run Unity compile and Test Runner checks listed in `docs/manual-checks.md`.

For docs/metadata-only changes, at minimum run:

```bash
git diff --check
```

Keep package user-facing documentation synchronized in English and Russian under `Assets/Scripts/SGG.PerfMeter/.Documentation/`.
