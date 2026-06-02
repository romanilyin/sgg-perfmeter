# Localization Backlog

Статус: current localized docs are present for the first public release.

Текущий пользовательский documentation surface:

- root `README.md` as the English entry point;
- localized docs under `docs/en/`, `docs/ru/`, `docs/de/`, `docs/es/`, `docs/fr/`, `docs/it/`, `docs/ja/`, `docs/ko/`, `docs/pt-br/`, and `docs/zh-cn/`;
- package-local `.Documentation/README.en.md` and `.Documentation/README.ru.md` as minimal GitHub link entry points.

`docs/` предназначен только для конечных пользователей. `_DevelopmentDocs` хранит только внутренний backlog и решения.

## Future Work

- Keep public README language links limited to languages that have complete user docs.
- When runtime behavior changes, update affected localized docs or explicitly record why a language is intentionally deferred.
