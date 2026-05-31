# Участие В Разработке

Английская версия: `CONTRIBUTING.md`.

Перед изменением поведения или архитектуры profiler прочитайте `AGENTS.md` и `_DevelopmentDocs/perfmeter_theory.md`.

## Pull Requests

Все изменения в `main` должны проходить через pull request после выхода репозитория из owner-only private preparation. Держите PR сфокусированным на одном изменении поведения или одной задаче по обслуживанию репозитория.

Используйте checklist из PR template и указывайте локальную команду проверки, которую вы запускали. Если изменение влияет на runtime profiler behavior, обновляйте tests и bilingual package documentation в том же PR.

## Именование Веток

Используйте lowercase ASCII branch names в таком формате:

```text
<type>/<short-kebab-topic>
```

Разрешенные типы:

```text
feature
fix
docs
l10n
test
refactor
chore
security
release
```

Примеры:

```text
feature/render-graph-analytics
fix/overdraw-readback-session
docs/private-release-notes
l10n/russian-docs
release/2026-5-18-1
```

Правила:

- Используйте lowercase letters, digits и hyphens в topic.
- Держите ровно один slash между type и topic.
- Используйте `l10n` для translation или localization-only изменений документации.
- Не добавляйте secrets, hostnames, private directory names, customer names, device serials или tokens в branch names.
- Не используйте `main`, `master`, `release` или tag-like names вроде `v1.2.3` для PR branches.

## Локальные Проверки

Для code changes запускайте Unity compile и Test Runner checks из `docs/manual-checks.md`.

Для docs/metadata-only changes минимум:

```bash
git diff --check
```

User-facing package documentation нужно синхронизировать на английском и русском в `Assets/Scripts/SGG.PerfMeter/.Documentation/`.
