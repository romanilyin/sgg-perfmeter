# PerfMeter MCP Problem Reports

Статус: active internal ledger.
Дата актуализации: 2026-07-19.

Источник raw reports:

```text
%LOCALAPPDATA%\SGG\UnityMcpGateway\audit\problem-reports
```

Raw JSON остается в Gateway audit-каталоге. Этот документ хранит PerfMeter ownership, ожидаемое поведение и порядок будущего исправления пакета.

## PerfMeter-Owned Reports

| ID | Priority | Status | Scope |
| --- | --- | --- | --- |
| `PM-MCP-001` | P1/P2 | resolved, Unity 6000.5.3 | Atomic export and existing-path conflict semantics |
| `PM-MCP-002` | P2 | resolved, Unity 6000.5.3 | Owning package version and provenance |
| `PM-MCP-003` | P1/P2 | resolved, Unity 6000.5.3 | Configured and effective session settings snapshots |
| `PM-MCP-004` | P1/P2 | resolved, Unity 6000.5.3 | Requested, attached and rendered overlay state semantics |
| `PM-MCP-005` | P1/P2 | resolved, Unity 6000.5.3 | MCP overlay visibility across Play Mode transitions |
| `PM-MCP-006` | P2 | resolved, Unity 6000.5.4 | Lifecycle/capture alert classification and fired-history provenance |
| `PM-MCP-007` | P1 | resolved, Unity 6000.5.4 | Serialized UI Toolkit panel settings for Development Player |

### PM-MCP-001 P1/P2: existing export path silently retains stale data

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

Resolution: MCP export атомарно отказывается с `file_exists` и сохраняет существующие bytes; runtime API делает атомарный overwrite. Оба пути возвращают success только после commit.

### PM-MCP-002 P2: exported package_version is stale

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

Resolution: export schema v2 берет identity из runtime assembly metadata, проверяемой против owning `package.json`, и пишет `package_version_source`.

### PM-MCP-003 P1/P2: exported settings contradict effective runtime state

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

Resolution: session хранит отдельные configured/effective snapshots на момент старта; последующие runtime changes не меняют capture metadata.

### PM-MCP-004 P1/P2: overlay command result did not describe rendered state

Reports: `problem_20260715T161353_ab2548fe3110`, `problem_20260715T162251_7c4fc831953a`, `problem_20260715T180234_2f4e68d022ad`, `problem_20260715T180718_cb6e84ef09e3`, `problem_20260715T180741_f5056a4a7488`, `problem_20260715T180800_674821d7d83d`.

Observed:

- EditMode мог вернуть `collection_mode: Overlay` вместе с `overlay_visible: false` без объяснения deferred состояния;
- `overlay_visible` описывал active component state, но воспринимался как подтверждение пикселей следующего GameView capture;
- hide оставлял подключенный UI Toolkit document с `display: none` и не запрашивал Editor repaint.

Resolution: MCP status отдельно возвращает requested visibility, persisted request и apply state. `active_component` подтверждает только активный overlay component, а `rendered_visibility` остается `unknown`, пока внешний capture не подтвердит кадр. Hide деактивирует и отсоединяет overlay object, UI Toolkit помечается dirty, Editor windows получают repaint request.

### PM-MCP-005 P1/P2: EditMode visibility override was lost on Play Mode transition

Observed: MCP hide в EditMode сохранялся только в transient runtime instance; Play Mode bootstrap повторно применял configured default и показывал overlay.

Resolution: MCP visibility хранится в editor `SessionState` и повторно применяется к уже запущенному/autostarted runtime после Play Mode/domain reload. Override не запускает отключенный runtime и применяется при явном `runtime.ensure`. Добавлен transition test `hide -> EnterPlayMode -> runtime ensure -> detached overlay`.

Remaining boundary: screenshot pixels принадлежат GameView/capture lifecycle и не подтверждаются синхронным PerfMeter command result. Alert toast report требует отдельного определения владельца visual notification; PerfMeter overlay не содержит alert-toast component.

Live closure, 2026-07-18: PerfMeter `2026.7.16-1` from commit `8304221` was validated in linked-package project `sgg-sky` through Gateway `2026.7.16-9`. `visible:false` returned requested/effective false with `overlay_apply_state:"detached"`, and the next authoritative `1902x915` `screen_capture` frame contained no PerfMeter overlay. `visible:true` returned requested/effective true with `overlay_apply_state:"active_component"`, and the next authoritative frame contained the restored overlay. `alerts.clear` returned zero active/fired counts and empty latest fields; both the next zero-settle authoritative frame and a later two-settle frame contained no alert toast. A later nonzero alert count was a new `cpu.main.over_budget` firing under sustained capture-time CPU spikes, not retained cleared state. All five captures used final-Game-View composition, no fallback, async GPU readback and verified artifact integrity. Evidence: `CaptureArtifacts/mcp-pr-101-overlay-visible.png`, `CaptureArtifacts/mcp-pr-101-overlay-hidden.png`, `CaptureArtifacts/mcp-pr-101-overlay-restored.png`, `CaptureArtifacts/mcp-pr-101-alerts-cleared.png`, and `CaptureArtifacts/mcp-pr-101-alerts-cleared-immediate.png`.

### PM-MCP-006 P2: lifecycle and capture alerts are indistinguishable from steady-state violations

Reports: Q1 authored-weather-map smoke on 2026-07-16, Q0.5 cloud screenshot lifecycle on 2026-07-18 and Q4 fast-orbit capture on 2026-07-18 in `mcp-problem-reports2.md`.

Observed:

- Play Mode entry, domain reload and authoritative screenshot capture produced CPU/FPS alert firings after the measured workload had recovered;
- `active_alert_count` correctly returned to zero, while `fired_alert_count` and latest event retained the transient lifecycle/capture firing;
- the response does not identify the fired-history start/reset point or whether an event belongs to startup, capture or steady-state collection;
- current consecutive-frame thresholds and cooldowns do not provide lifecycle/capture provenance.

Required:

- define whether lifecycle and synchronous capture samples are suppressed, warmed up or retained with explicit classification;
- expose enough provenance to distinguish transient lifecycle/capture events from steady-state budget failures;
- make the fired-history interval or last reset point explicit without discarding useful historical evidence;
- add tests for Play Mode startup/reload, capture-time spikes, steady recovery and explicit alert clear.

Acceptance: MCP and runtime alert snapshots let a caller classify a retained firing without inferring from a later metrics window; sustained steady-state violations remain observable.

Resolution: alert history now has an interval id, start frame/time, UTC timestamp, reset reason, classified counters and the latest fired snapshot. Runtime startup samples are classified as `Lifecycle`, normal samples as `SteadyState`, and bounded external capture scopes as `Capture` through public API and MCP `perfmeter.alerts.capture.begin/end` commands. Capture classification is explicit rather than inferred from frame duration.

### PM-MCP-007 P1: runtime-created PanelSettings lacks ICU data in Development Player

Report: Windows Development Player smoke on 2026-07-19 in `mcp-problem-reports2.md`.

Observed:

- the Player logged 247 `ICU Data not available` errors, 24 main-thread `FindObjectsOfTypeAll` exceptions and a UI Toolkit text-layout `NullReferenceException`;
- `PerfMeterOverlay.EnsureDocument()` still creates `PanelSettings` and `PanelTextSettings` with `ScriptableObject.CreateInstance` at runtime;
- Unity 6000.5 cannot attach the required build-time ICU data to that runtime-created panel configuration;
- the package has no serialized `PanelSettings` asset or Development Player regression coverage for the overlay.

Required:

- ship and reference a serialized `PanelSettings` asset with Unity-assigned ICU data and the required text/theme settings;
- remove the failing runtime-created panel/text-settings path from supported Player builds;
- preserve runtime overlay behavior for supported URP and HDRP projects;
- add a Windows Development Player smoke that enables the overlay and checks Player logs.

Acceptance: the overlay renders in a Unity 6000.5 Development Player with zero ICU, main-thread text fallback and UI Toolkit layout exceptions.

Resolution: the package now ships a serialized `PanelSettings` resource with Unity-assigned ICU data plus serialized `PanelTextSettings` and theme subassets. Runtime overlay setup loads the package resource and no longer creates or discovers panel/text/theme assets at runtime.

Validation, 2026-07-19: Unity `6000.5.4f1` compile passed, EditMode `87/87`, PlayMode `5/5`. Unity `6000.4.10f1` imported the serialized panel resource and compiled cleanly. A Windows x64 Development Player ran the enabled overlay for 15 seconds with zero ICU-data, main-thread text fallback, ATG text-job, or UI Toolkit layout exceptions.

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
