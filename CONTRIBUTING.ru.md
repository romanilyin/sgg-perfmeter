# Участие В Разработке

Английская версия: `CONTRIBUTING.md`.

Перед изменением поведения или архитектуры profiler прочитайте `AGENTS.md` и `_DevelopmentDocs/perfmeter_theory.md`.

## Pull Requests

Все изменения в `main` должны проходить через pull request. Держите PR сфокусированным на одном изменении поведения, документации или обслуживания репозитория.

Используйте checklist из PR template и указывайте локальную команду проверки, которую запускали. Если изменение влияет на runtime profiler behavior, обновляйте tests и пользовательскую документацию в том же PR.

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
docs/readme-screenshots
l10n/russian-docs
release/2026-5-20-1
```

Правила:

- Используйте lowercase letters, digits и hyphens в topic.
- Держите ровно один slash между type и topic.
- Используйте `l10n` для translation или localization-only изменений документации.
- Не добавляйте secrets, hostnames, private directory names, customer names, device serials или tokens в branch names.
- Не используйте `main`, `master`, `release` или tag-like names вроде `v1.2.3` для PR branches.

## Локальные Проверки

Публичные рекомендации для contributor checks: [Проверки contributor changes](./docs/ru/contributor-checks.md).

Для docs/metadata-only changes минимум:

```bash
git diff --check
```

Синхронизируйте пользовательскую документацию в `docs/en` и `docs/ru`, если изменение влияет на обе аудитории.
