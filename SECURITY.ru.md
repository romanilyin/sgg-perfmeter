# Политика Безопасности

SGG PerfMeter - локальный Unity profiling package. Он не является network service и не предоставляет filesystem/process sandbox.

## Сообщение Об Уязвимостях

Сообщайте о предполагаемых уязвимостях приватно. Не открывайте публичные issues или discussions с exploit details, private project paths, device logs, tokens, package credentials или customer/project information.

Используйте GitHub private vulnerability reporting или private GitHub security advisory для `romanilyin/sgg-perfmeter`, когда это доступно. Если этот способ недоступен, пока репозиторий приватный, свяжитесь с владельцем `@romanilyin` приватно и приложите достаточно деталей для воспроизведения.

Укажите:

- Затронутую поверхность: runtime API, overlay, URP renderer feature, overdraw measurement, Editor setup actions, MCP command metadata/handlers, Android smoke helper, package metadata или release automation.
- Минимальные шаги воспроизведения и relevant Unity/URP/platform details.
- Ожидаемое влияние: unsafe project mutation, unintended renderer/settings changes, command authorization bypass через host gateway, secret/log exposure, denial of service или release supply-chain issue.
- Стала ли проблема уже публичной.

Ожидаемые сроки реакции:

- Первичное подтверждение в течение 7 дней.
- Triage update в течение 14 дней после подтверждения, если report воспроизводим.
- Coordinated disclosure timing согласуется по severity и availability fix'а.

## Текущие Гарантии

- Runtime profiling API читает Unity performance data и не должен выполнять filesystem, network, process или shell operations.
- Runtime overlay использует UI Toolkit и не использует uGUI/IMGUI runtime overlays.
- Editor setup actions являются явными Editor operations для Frame Timing Stats и установки URP renderer feature.
- Overdraw measurement и heatmap являются opt-in diagnostic rendering paths.
- MCP command metadata содержит risk/idempotency information для enforcement policy в host gateway.

## Не-Гарантии

- PerfMeter не является sandbox и не защищает от unsafe Unity Editor code, запущенного в том же проекте.
- MCP safety зависит от policy host gateway/editor bridge. PerfMeter command definitions не должны использоваться для обхода user approval или project permissions.
- Metrics и bottleneck classification являются diagnostic heuristics, а не authoritative security/correctness decisions.
- Overdraw measurement и heatmap добавляют дополнительную rendering work, пока активны/видимы, и могут влиять на frame timing.
- Logs, screenshots, device dumps и build artifacts могут содержать private project information и должны проверяться перед отправкой.

## Scope

Security reports особенно важны для unexpected project mutation, command policy bypass, secret/log exposure, unsafe release artifacts, unsafe package metadata или renderer/setup behavior, который может неожиданно менять unrelated project assets.
