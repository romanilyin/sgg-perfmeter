# PerfMeter MCP Problem Reports

Статус: active internal backlog.
Дата актуализации: 2026-07-15.

Источник raw reports:

```text
%LOCALAPPDATA%\SGG\UnityMcpGateway\audit\problem-reports
```

Raw JSON остается в Gateway audit-каталоге. Этот документ хранит PerfMeter ownership, ожидаемое поведение и порядок будущего исправления пакета.

## PerfMeter-Owned Reports

### P1/P2: existing export path silently retains stale data

Report: `problem_20260711T062920_08ae07433ae7`

Command: `perfmeter.session.export`

Observed:

- повторный export в существующий path вернул success/exported;
- файл сохранил старый zero-sample payload вместо текущей 21-sample session;
- вызывающая сторона не получила conflict или reuse result.

Required:

- определить явную overwrite/refuse/reuse семантику;
- при success атомарно записывать текущую session;
- при отказе возвращать typed non-success conflict;
- покрыть existing file, stale file, atomic replacement failure и повторный export.

### P2: exported package_version is stale

Report: `problem_20260715T092056_fa9fde1beac9`

Command: `perfmeter.session.export`

Observed:

- четыре character-population capture содержат `package_version: 2026.5.18-1`;
- установленная версия PerfMeter была `2026.6.28-1`;
- metadata не позволяет надежно связать capture с реально исполнявшейся версией пакета.

Required:

- брать owning-package version из текущего package metadata/runtime source of truth;
- не подменять версию PerfMeter версией Gateway или host project;
- добавить provenance поля для источника версии, если есть несколько допустимых источников;
- не переписывать исторические capture artifacts молча.

### P1/P2: exported settings contradict effective runtime state

Report: `problem_20260715T092636_805123c3a53f`

Command: `perfmeter.session.export`

Observed:

- `perfmeter.runtime.status` до и после matrix показывал `collection_mode: Background` и `overlay_visible: false`;
- exported `metadata.settings` показывал `collection_mode: Overlay` и `overlay_visible: true`;
- export, вероятно, сериализовал persisted/default settings вместо effective runtime state capture session.

Required:

- отдельно хранить configured/persisted settings и effective runtime settings;
- export должен явно указывать, какой state действовал для записанной session;
- snapshot effective state должен быть привязан к session/capture time, а не читаться неоднозначно после завершения;
- добавить tests для Background/Overlay, visible/hidden overlay и runtime override относительно persisted defaults.

## Related Gateway-Owned Reports

Эти reports возникли в PerfMeter validation context, но исправляются в SGG Unity MCP Gateway, а не в PerfMeter package.

### Compile response loss after metric-id correction

Report: `problem_20260710T183154_33ddb5b57646`

Command: `compile.refresh_and_wait`

Observed: после исправления PerfMeter metric ids compile refresh вернул пустой Bridge response; read-only recovery позднее подтвердил ready/no-errors.

Ownership: Gateway compile/Bridge response delivery. PerfMeter не должен добавлять собственный retry или скрывать protocol failure.

### Overlay screenshot remained scheduled while unfocused

Report: `problem_20260714T064922_5d2f6bb85dc4`

Command: `game.view.screenshot.status`

Observed: overlay-capable screenshot оставался `scheduled`, когда Unity был unfocused; status не давал cancel/recovery path.

Ownership: Gateway screenshot lifecycle; закрыто как `MCP-PR-078`. PerfMeter может использовать screenshot artifacts, но не должен управлять Unity focus или дублировать screenshot scheduler.

## Acceptance Boundary

```text
- PerfMeter fixes only exporter-owned file/result/version/settings semantics.
- Gateway fixes transport, lifecycle, compile and screenshot behavior.
- Gateway outer success must not overwrite or reinterpret PerfMeter semantic failure.
- PerfMeter export tests must validate artifact bytes and metadata, not only outer MCP envelope success.
- No automatic rewrite of historical benchmark artifacts.
```
