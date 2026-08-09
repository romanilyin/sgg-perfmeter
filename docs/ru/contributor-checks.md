# Проверки изменений контрибьюторов

Выбирайте самую легкую проверку, которая соответствует изменению. Проверки компиляции Unity и Test Runner дорогие, поэтому они нужны для изменений поведения кода во время выполнения или Editor-кода, а не для каждой правки только документации.

## Только документация или метаданные

```bash
git diff --check
```

Также проверьте затронутые ссылки и синхронизируйте затронутые языковые версии, если изменение касается нескольких языков.

## Изменения кода во время выполнения и Editor-кода

Запустите проверку компиляции Unity для целевого проекта и укажите команду в pull request. Если тесты релевантны, запустите EditMode и/или PlayMode через Test Runner.

Для релизных проверок, предназначенных только для мейнтейнеров, или smoke-тестов на устройствах используйте актуальный чеклист мейнтейнеров проекта и укажите команду или окружение в pull request.

## Перед pull request

- Проверьте `git status` и добавьте в stage только нужные файлы.
- Не коммитьте сгенерированное состояние Unity: `Library/`, `Logs/`, `Temp/`, `Obj/` или локальные результаты сборки.
- Не коммитьте секреты, файлы `.env`, дампы устройств, приватные логи или несвязанные скриншоты.
- Если меняется поведение профайлера во время выполнения, обновите тесты и пользовательскую документацию в том же PR.

## Performance CI

`.github/workflows/performance-ci.yml` запускает полный EditMode correctness suite и изолированные performance tests на Unity `6000.4.12f1` и `6000.5.6f1` для pull request из этого репозитория, push в `main` и ручных запусков. Pull request из fork пропускаются, поскольку GitHub не передаёт Unity license secrets. CI добавляет `com.unity.test-framework.performance` `3.5.0` только в ephemeral checkout; у пакета нет hard dependency. Версионируемые пороги находятся в `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`; CI сохраняет raw NUnit XML, преобразованный JUnit XML, performance JSON и логи.

Тот же workflow запускает отдельный полный PlayMode lifecycle job на обеих версиях Unity, когда repository variable `PERFMETER_UNITY_CI_ENABLED` равна `true` и настроены совместимые с GameCI Unity credentials. Изменения в `Tests/PlayMode/**` запускают workflow; отключенные credentials и PR из fork дают явно skipped job.
