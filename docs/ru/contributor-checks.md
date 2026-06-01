# Проверки Contributor Changes

Выбирайте самый легкий check, который соответствует изменению. Unity compile и Test Runner checks дорогие, поэтому они нужны для runtime/editor behavior changes, а не для каждой documentation-only правки.

## Только Документация Или Metadata

```bash
git diff --check
```

Также проверьте затронутые ссылки и синхронизируйте английскую/русскую документацию, если изменение касается обеих версий.

## Runtime Или Editor Code Changes

Запустите Unity compile check для целевого проекта и укажите команду в pull request. Если tests релевантны, запустите EditMode и/или PlayMode Test Runner checks.

Для maintainer-only release gates или device smoke tests используйте актуальный project-maintainer checklist и укажите команду или окружение в pull request.

## Перед Pull Request

- Проверьте `git status` и stage только intended files.
- Не коммитьте generated Unity state: `Library/`, `Logs/`, `Temp/`, `Obj/` или локальные build outputs.
- Не коммитьте secrets, `.env` files, device dumps, private logs или unrelated screenshots.
- Если меняется runtime profiler behavior, обновите tests и user-facing docs в том же PR.
