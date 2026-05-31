# План для агента: подготовить SGG PerfMeter к релизу

Аудитория: coding/documentation agent, работающий внутри `romanilyin/sgg-perfmeter`.

Целевая версия: `2026.5.20-1`.

Release state: private release candidate. Не делать репозиторий публичным, не публиковать в registry, не создавать публичные releases и не пушить tags без явного подтверждения владельца.

## Цель

Подготовить репозиторий к чистому private/public-ready release pass:

1. Держать README, package docs, marketing docs, release plan и release notes синхронными с текущей реализацией.
2. Держать English и Russian документацию одинаковой по смыслу там, где docs bilingual.
3. Проверить package metadata, samples, changelogs, release notes и release plan.
4. Запустить локальные проверки, соответствующие типу изменений.
5. Подготовить final checklist перед tag `2026.5.20-1`.

## Текущие факты

Текущий repository содержит больше, чем описывали старые release-candidate docs:

- project-owned JSON settings и zero-code startup;
- collection modes и runtime reset;
- UI Toolkit overlay presets/modules/tunables и allocation-conscious text refresh;
- device/environment snapshots и camera snapshots;
- session recorder с warm-up, scene scope, worst-frame summaries, JSON/CSV export и MCP session commands;
- rule alerts с callback/log/Editor warning cooldowns и MCP alert commands;
- custom metric providers с API, session JSON, MCP и bounded overlay rows;
- Render Graph analytics snapshot API и `perfmeter.rendergraph.snapshot`;
- Package Manager samples для bootstrap/settings, runtime workflows и editor/MCP automation.

Не оставлять stale claims, что CSV/JSON session export или Render Graph analytics отсутствуют. Корректная формулировка про overlay: refresh allocation-conscious и throttled, но изменившиеся numeric values и graph legend labels все еще могут materialize managed strings на refresh interval.

## Файлы для проверки

- `README.md`
- `Assets/Scripts/SGG.PerfMeter/README.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/README.en.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/README.ru.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/STATUS.en.md`
- `Assets/Scripts/SGG.PerfMeter/.Documentation/STATUS.ru.md`
- `CHANGELOG.md`
- `Assets/Scripts/SGG.PerfMeter/CHANGELOG.md`
- `_DevelopmentDocs/release-readiness/release-2026.5.20-1.md`
- `_DevelopmentDocs/release-readiness/release-notes-2026.5.20-1.md`
- `_DevelopmentDocs/perfmeter_status.md`
- `_DevelopmentDocs/perfmeter_iterations.md`
- `_DevelopmentDocs/marketing/positioning.en.md`
- `_DevelopmentDocs/marketing/positioning.ru.md`
- `_DevelopmentDocs/marketing/competitor-comparison.en.md`
- `_DevelopmentDocs/marketing/competitor-comparison.ru.md`

`_DevelopmentDocs/sgg-perfmeter-competitor-comparison-updated.md` - локальный reference source, его нельзя коммитить.

## Package Metadata

Проверить `Assets/Scripts/SGG.PerfMeter/package.json`:

- `name`: `com.sungeargames.perfmeter`
- `version`: `2026.5.20-1`
- `unity`: `6000.4`
- URP dependency: `17.4.0`
- samples: `Samples~/BootstrapAndSettings`, `Samples~/RuntimeWorkflows`, `Samples~/EditorAutomation`
- URLs ведут на canonical repository docs/license/changelog.

## Messaging Rules

- Позиционировать SGG PerfMeter как Unity 6000+ URP Render Graph diagnostics layer и agent-readable profiling API.
- Явно писать, что пакет дополняет Unity Profiler, RenderDoc, Profile Analyzer и Frame Debugger, но не заменяет их.
- Сравнивать AFPS и Graphy как visual overlay references, а не как code/assets для копирования.
- Держать audio modules, screenshot/debug-break alert actions и player hotkeys вне текущего scope без явного approval.
- Не утверждать zero-overhead или all-platform coverage.

## Verification

Для docs-only изменений минимум:

```bash
git diff --check
```

Для code, package metadata, JSON, asmdef, shader или Unity asset changes запускать Unity compile и релевантные EditMode/PlayMode checks из `AGENTS.md`.

Перед commit проверить:

```bash
git status --short
git diff --stat
git diff --check
```

Не stage'ить generated Unity state, logs, builds, `.env` files или локальный comparison reference file.
