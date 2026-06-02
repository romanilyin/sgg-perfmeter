# Contributing

Russian localization: `CONTRIBUTING.ru.md`.

Read `AGENTS.md` before making repository changes. For profiler behavior or architecture changes, also check `_DevelopmentDocs/decisions/` and the relevant `_DevelopmentDocs/backlog/` notes.

## Pull Requests

All changes to `main` should go through a pull request. Keep PRs focused on one behavior, documentation, or repository-maintenance change.

Use the PR template checklist and include the local verification command you ran. If a change affects runtime profiler behavior, update tests and user-facing documentation in the same PR.

## Branch Naming

Use lowercase ASCII branch names in this form:

```text
<type>/<short-kebab-topic>
```

Allowed types:

```text
feature
fix
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
docs/readme-screenshots
l10n/russian-docs
release/2026-6-5-1
```

Rules:

- Use lowercase letters, digits, and hyphens in the topic.
- Keep exactly one slash between the type and topic.
- Use `l10n` for documentation translation or localization-only changes.
- Do not put secrets, hostnames, private directory names, customer names, device serials, or tokens in branch names.
- Do not use `main`, `master`, `release`, or tag-like names such as `v1.2.3` for PR branches.

## Local Checks

For public contributor guidance, see [Contributor Checks](./docs/en/contributor-checks.md).

For docs/metadata-only changes, at minimum run:

```bash
git diff --check
```

Keep affected localized user-facing documentation synchronized when a change affects multiple audiences.
