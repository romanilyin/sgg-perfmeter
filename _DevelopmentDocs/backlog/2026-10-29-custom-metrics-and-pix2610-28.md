# PerfMeter: custom metrics в Player и поддержка PIX 2610.28

Дата: 2026-10-29. Source baseline: `4df3a38d58d63fc4d13bda3423a1a6a60700c967`, package `2026.10.13-1`.

Этот backlog описывает две независимые будущие задачи. Изменение исходников, Unity validation, запуск Player/PIX, установка SDK и выпуск пакета в этот docs-only этап не входят. Центральный порядок находится в [roadmap](roadmap.md).

| ID | Приоритет | Следующий результат |
| --- | --- | --- |
| `PM-OBS-006` | P2, диагностика и проверка контракта | Привязать provider registration, collection/cache, публичный snapshot, overlay и retained/export rows к одной реальной Player session; исправлять только установленный разрыв. |
| `PM-PIX-002` | P2, version compatibility и SDK re-evaluation | Определить и проверить поддержку установленного пользователем PIX 2610.28 в Windows x64/D3D12, отдельно для generic capture и новых optional API/CLI workflows. |

## PM-OBS-006 — custom metrics в Development Player

Четыре reports относятся к одной уточняющейся цепочке PerfMeter/Sky/RTGI наблюдений. Их primary lane — package-owned availability/registration/collection/cache/export. MCP transport и `MCP-PR-240` orphan teardown учитываются отдельно.

| Report ID | Содержание и граница evidence |
| --- | --- |
| `problem_20261028T210652_bedae5d5f565` | Overlay показывает custom GPU stages как n/a без различения отсутствующего provider snapshot и `Available=false`; overall GPU timing есть. Runtime provider presence/warning/sample count не получены. |
| `problem_20261029T064927_0c95911e6614` | Автор сообщает 8835 и 8873 samples двух сессий, drops 0, у каждого `custom_metrics=[]`; compiled integration assemblies и GPU recorder support присутствуют. Foreground screenshot gate не прошёл, runs provisional. |
| `problem_20261029T102819_67d229fc879d` | Source/build audit автора: providers должны сохранять unavailable rows, build metadata содержит initializer entries, recorder/export сохраняют массивы. Runtime исполнение initializer и registry state не наблюдались. |
| `problem_20261029T132438_4fa6f7c804fa` | Новый bounded probe сообщает `GetCustomMetrics()` rows 0 до Reset и после Stop, error пустой; 8731 записанных epochs, export также пуст. API result может быть cached collection; это не registry count. Screenshot/focus отсутствовали, результат диагностический. |

У всех четырёх reports `snapshot=null`. Заявлены PerfMeter `2026.10.13-1`, Unity `6000.6.7f1`, Windows/DX12; версия пакета сама по себе не подтверждает installed/runtime binary parity. Сохранённые reports содержат statements и ссылки автора, а не вложенные Player exports/logs/native receipts. Эти bytes в данном проходе повторно не исследовались.

Присутствие assembly/initializer metadata не доказывает успешную runtime регистрацию. Metadata audit `102819` относится к GameAssembly SHA256 `5e3c6ce93b13608ae564188eca394b81a0a4fc7214777ed90c826fca3717dfa8`; поздний probe `132438` — к другому build, `f17beb02bce8bde4cd68a66bdd3776c31c052812df117ab640e180573063b85b`. Нельзя переносить metadata acceptance между ними. Общий FrameTimingManager GPU timing не подтверждает per-pass recorder availability. Отсутствующий sample сохраняется как unavailable, а не числовой ноль.

### Scope и acceptance

1. На существующих warmup/session boundaries получить для одного exact build/session provider IDs/count и факт исполнения installers, registration/unregistration/reset lifecycle, collection/cache generation/age, публичные rows с ID/Available/Warning. Различать отсутствие регистрации, отсутствие опубликованного snapshot, stale cache, provider exception и unavailable sample. Использовать существующий receipt/probe, без нового capture framework и слепого benchmark loop.
2. Сопоставить эти rows с теми же retained samples и JSON/CSV export, включая registered-but-unavailable metric. Пустота public API выше exporter сужает поиск, но не устанавливает единственного владельца причины. UI должен объяснять n/a с bounded reason, сохраняя missing/unavailable distinctions.
3. Проверить точные Sky/RTGI integration versions и FTUE/provider enablement, затем package-owned registration/cache/recorder/export участок. Исправление определяет доказанная причина; отдельные assembly installers остаются у owning packages.
4. Перед закрытием сохранить exact build hashes, session identity, before/after rows, sample/export counts и availability reasons. Проверить Reset/Stop/scene-load lifecycle и старые public/export contracts; warmed allocation/low-overhead guards сохраняются. Foreground/capture acceptance отмечается отдельно от диагностических receipts.

P2 отражает неизвестную причину и неполную runtime provenance. Повысить до P1 при подтверждённой потере уже retained rows либо воспроизводимом отказе registration/collection на поддержанной pinned matrix. Три последующих reports не образуют три независимых дефекта и не закрывают первый report.

### Сохранённые источники reports

- MCP docs: `%UNITY_WORKSPACE%/sgg-mcp/sgg-unity-mcp-docs\roadmap\specialized\problem-reports-2026-10-29-status.ru.md` и `problem-reports-2026-10-29-follow-up.ru.md`.
- Structured facts: `%UNITY_WORKSPACE%/sgg-mcp/temp\mcp-reports-followup-2026-10-29\intake-facts.json`; старший report сохранён в `temp\mcp-safe-mode-2026-10-29\intake-facts.json`.

| Raw report ID | SHA256 |
| --- | --- |
| `problem_20261028T210652_bedae5d5f565` | `5802bbd70d1dcaa04e9307b107a746d5de3d505f203b10eb9da1ec6b7c70f3bf` |
| `problem_20261029T064927_0c95911e6614` | `880e23f785b0e929773776af2318a5b600a582fa8dc20f713d4f60c203eac8db` |
| `problem_20261029T102819_67d229fc879d` | `71ba1b8e7d02da317ec0cfede65eff0fcf66f476d803fd70f17d9b63882cb476` |
| `problem_20261029T132438_4fa6f7c804fa` | `bfe28180c49f56840d8420ef0cb6705de01dff09c041b5a256976d28b301775f` |

## PM-PIX-002 — Windows/D3D12 и PIX 2610.28

Версия подтверждена официально: [PIX 2610.28 выпущен 28 октября 2026](https://devblogs.microsoft.com/pix/pix-2610-28/), [download page](https://devblogs.microsoft.com/pix/download/) перечисляет её как main release. Проверка источников выполнена 29 октября 2026; локальная установка/version probing и captures не проводились.

### Текущий package boundary и новые официальные сведения

Read-only baseline `Runtime/PerfMeterCapture.cs`, `PerfMeterExternalGpuProfilerBackend.EvaluateCapability`, ограничивает generic PIX Windows Editor/Development Player, D3D12 и уже attached external profiler. `BeginGPUCapture`/`EndGPUCapture` проверяют Unity lifecycle. `Runtime/PerformanceMeter.cs`, `IsValidCaptureOptions`, допускает PIX только в `GenericUnity`; native modes для PIX отклоняются. Unity attachment не аутентифицирует tool/version или exact `.wpix` path. FTUE содержит внешнюю download link; package не поставляет PIX binaries.

[Прежний accepted boundary PM-PIX-001](../decisions/pix-native-timing-boundary.md) зафиксировал отсутствие документированного bounded Windows circular/discard контракта в исследованных тогда API. Новые официальные источники дают основание пересмотреть этот blocker:

- [Programmatic capture](https://learn.microsoft.com/en-us/windows/pix/get-started/programmatic-capture) описывает Windows timing `FileCircular`, `MaximumToolingMemorySizeMb` и discard через `PIXEndCapture(TRUE)`; `MemoryCircular` остаётся unsupported. Timing collection требует performance logging permissions; GPU и timing capturer lifecycle различаются.
- [pixtool reference](https://learn.microsoft.com/en-us/windows/pix/gpu-captures/pixtool) документирует timing circular mode, ring limit и exact output filename; нельзя смешивать circular с raw ETL. Эти CLI параметры не являются настройками текущего generic Unity backend.
- [PIX API overview](https://learn.microsoft.com/en-us/windows/pix/pix-api/pix-api-overview) описывает установленный SDK с `PixApi.h`/`PixApi.lib`, COM interfaces и `pixapi.dll`. Для C# projections заявлен .NET 10+, поэтому совместимость с Unity managed runtime требует отдельного решения; optional sidecar остаётся вариантом для изучения.
- Release notes предупреждают об изменениях preview API types/factory identity и необходимости rebuild со shipping headers; [официальные samples](https://github.com/microsoft/pix-samples) задают версионные prerequisites конкретных примеров.

Старое blanket утверждение «Windows игнорирует storage/size/discard» больше не используется как актуальный внешний API вывод. Реализация PerfMeter остаётся generic-only до отдельного design/validation решения. Этот backlog не меняет ADR автоматически и не объявляет alert-trigger integration готовой.

### Поддерживаемый target и границы задачи

Первый target — **Windows x64, D3D12, поддержанный Unity 6000.4+ Editor и Development Player**, с явными URP/HDRP 17.4+ rows только для заявляемых возможностей пакета. Точные Unity patch, Player scripting backend, GPU/driver, PIX **2610.28**, SDK/header/runtime versions и graphics API записываются в acceptance. Windows ARM64, D3D11On12, Linux/macOS/mobile, non-Development Player и preview-only DirectX features остаются незаявленными, пока для них нет отдельной matrix.

В задаче два последовательно принимаемых результата: version compatibility существующего generic GPU capture; затем feasibility/design для optional documented PIX API/CLI control/analysis и timing. Новый circular API возвращает `PM-PIX-001` на re-evaluation, сохраняя его P4 native alert scope. Конкретные arm/alert-trigger/end/flush/cancel semantics, границы ring и provenance должны быть проверены до реализации этого scope.

### Acceptance и открытые gaps

1. **Version/SDK:** user-installed allowlisted PIX 2610.28 обнаруживается с exact executable/DLL identity/hash/version. Проверить shipping headers, установленный help/capabilities и совместимый toolchain; marker runtime, capturer и PIX API SDK — разные зависимости. EULA/redistribution и возможность in-process loading требуют отдельного решения. Package не устанавливает PIX, не повышает права и не принимает arbitrary paths из MCP/JSON.
2. **Generic capture:** реальные pinned Windows/D3D12 Editor и Development Player smokes проходят begin/end, создают читаемый PIX 2610.28 capture и сохраняют инструмент/версию/API/build/session correlation. Attach failure, unsupported matrix, cancel и tool conflict дают явный unavailable/refusal. Статус Unity begin/end остаётся отдельно от authenticated artifact association.
3. **API/CLI candidate:** выбрать документированный workflow и проверить capture type, exact output path, version errors, bounded timeout/cancel/finalization, unknown outcome и process ownership. Без private exports, UI automation, proprietary parsing, directory guessing или автоматического повторения uncertain begin. Analysis/replay остаётся отдельным optional слоем; новый GPU file format требует проверки совместимости, без изменения исходного capture по умолчанию.
4. **Timing/circular gate:** отдельно доказать размер/ресурсы ring, pre-alert coverage, deterministic end/flush на alert, discard и safe teardown при reload/exit. Проверить минимальные права без их автоматической выдачи, conflict lease и sensitive process-trace policy. Наличие public API ещё не является PerfMeter runtime acceptance.
5. **Artifacts:** exact caller/request/generation binding, finalized stable-file identity/hash, quota/redaction/share policy и официальное открытие/анализ PIX. Generic observed файл остаётся non-authoritative. Fixtures проверяют контракт; только real-tool smoke разрешает конкретную support row.

Открытые blockers: pinned локальный PIX/SDK ещё не проверены, real captures отсутствуют, Unity/SDK interop и optional distribution/licensing решение не приняты, alert-trigger lifecycle и process-trace budgets не подтверждены. Новая задача поддержки версии имеет самостоятельный acceptance; наличие PIX 2610.28 в Microsoft download не закрывает её.
