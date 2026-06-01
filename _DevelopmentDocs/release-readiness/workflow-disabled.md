# Release Workflow Disabled

Date: 2026-06-01

The previous manual-only GitHub Actions release-readiness workflow was removed while the documentation tree is being reorganized.

Reason:

- user-facing documentation now lives under `docs/en/` and `docs/ru/`;
- internal release-readiness files live under `_DevelopmentDocs/release-readiness/`;
- the old workflow still expected release files under root `docs/`, which is no longer the intended structure;
- Unity/package release is not being performed yet.

Before re-enabling a release workflow:

- decide which release gates are public and which remain internal;
- point release-readiness checks at `_DevelopmentDocs/release-readiness/` or introduce dedicated public release notes under `docs/en` / `docs/ru`;
- remove assumptions that package-local `.Documentation/STATUS.*` files exist;
- keep the workflow manual-only unless an explicit project policy changes.
