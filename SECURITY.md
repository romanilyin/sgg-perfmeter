# Security Policy

SGG PerfMeter is a local Unity profiling package. It is not a network service and does not provide a filesystem or process sandbox.

## Reporting Vulnerabilities

Report suspected vulnerabilities privately. Do not open a public issue or discussion with exploit details, private project paths, device logs, tokens, package credentials, or customer/project information.

Use GitHub private vulnerability reporting or a private GitHub security advisory for `romanilyin/sgg-perfmeter` when available. If that route is unavailable while the repository is private, contact the repository owner `@romanilyin` privately and include enough detail to reproduce the issue.

Please include:

- Affected surface: runtime API, overlay, URP renderer feature, overdraw measurement, Editor setup actions, MCP command metadata/handlers, Android smoke helper, package metadata, or release automation.
- Minimal reproduction steps and relevant Unity/URP/platform details.
- Expected impact: unsafe project mutation, unintended renderer/settings changes, command authorization bypass through a host gateway, secret/log exposure, denial of service, or release supply-chain issue.
- Whether the issue is already public.

Expected response targets:

- Initial acknowledgement within 7 days.
- Triage update within 14 days after acknowledgement when the report is reproducible.
- Coordinated disclosure timing agreed per issue severity and fix availability.

## Current Guarantees

- The runtime profiling API reads Unity performance data and does not intentionally perform filesystem, network, process, or shell operations.
- Runtime overlay uses UI Toolkit and does not use uGUI/IMGUI runtime overlays.
- Editor setup actions are explicit Editor operations for Frame Timing Stats and URP renderer feature installation.
- Overdraw measurement and heatmap are opt-in diagnostic rendering paths.
- MCP command metadata exposes risk/idempotency information for a host gateway to enforce policy.

## Non-Guarantees

- PerfMeter is not a sandbox and does not protect against unsafe Unity Editor code running in the same project.
- MCP safety depends on the host gateway/editor bridge policy. PerfMeter command definitions must not be used to bypass user approval or project permissions.
- Metrics and bottleneck classification are diagnostic heuristics, not authoritative security or correctness decisions.
- Overdraw measurement and heatmap add extra rendering work while active/visible and can affect frame timing.
- Logs, screenshots, device dumps, and build artifacts can contain private project information and must be reviewed before sharing.

## Scope

Security reports are especially relevant for unexpected project mutation, command policy bypass, secret/log exposure, unsafe release artifacts, unsafe package metadata, or renderer/setup behavior that can unexpectedly alter unrelated project assets.
