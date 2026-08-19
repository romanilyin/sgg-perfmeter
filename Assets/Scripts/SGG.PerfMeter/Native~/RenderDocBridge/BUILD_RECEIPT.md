# RenderDoc Annotation Bridge Validation Receipt

This receipt records feature-branch validation after rebasing onto
`origin/main` commit `7491008db4009ac9a2d9f7eb442ff7c5f6181455`. It is not a
release-artifact manifest and no native DLL is committed to the binary-free UPM
package.

Feature: `PM-RDANN-002` Windows x64 Editor/D3D12 command annotations

## Inputs

- Source: package `Native~/RenderDocBridge` in `feature/perfmeter-renderdoc-annotations`.
- Compiler: Microsoft Visual C++ from Visual Studio Community 18 (2026), x64 Release.
- CMake: Visual Studio bundled CMake with Ninja, static MSVC runtime, artifact version `2026.8.15-1`.
- Unity PluginAPI headers: Unity `6000.5.6f1`, `Editor/Data/PluginAPI`.
- RenderDoc header: commit `7db2264afa00a5313154022f8c4ae0628a641300`, SHA-256 `b7005e7dc34c3635046868bbd76d81b9b055aede0f56daa0bd39fedee0639ffb`.
- Warnings: `/W4 /WX`.

## Verification

- Rebased production DLL built outside the repository: `131072` bytes, SHA-256 `9B974EDEBADDBE0328F9EA1B2EBC39BA84409CFEE87B54A8899D4BB0F4AAA511`.
- Export/dependency audit: exactly 12 named exports (six capture, four annotation, `UnityPluginLoad`, `UnityPluginUnload`) and only `KERNEL32.dll`.
- CTest passed `1/1`; the fake-table executable passed all `16/16` internal cases.
- Unity `6000.4.12f1` annotation EditMode tests passed `10/10`; the full EditMode suite passed `549`, failed `0`, and ignored one opt-in real replay smoke (`550` total).
- Original pre-rebase D3D12 Editor smokes passed on Unity `6000.4.12f1` and `6000.5.6f1`: two packets, eight RenderDoc calls, zero errors, and XML-confirmed set → red clear → delete → blue clear ordering. A real-tool smoke of the rebased separately installed artifact remains a publication gate.
- The package does not contain `renderdoc.dll` or RenderDoc replay binaries.
- Distribution is a future separately verified Windows x64 bridge artifact installed project-locally by FTUE.
- At the time of this feature-branch validation, the published `2026.8.11-1` bridge exposed the capture ABI only and annotation calls mapped to `BridgeTooOld`.

This receipt proves the rebased source build, fixed export/dependency surface, fake/native contract suite, and managed Unity suite. It retains the two original real-capture rows only as historical evidence; a clean release artifact, rebased real-tool smoke, and clean external consumer are still required before publication. It does not prove Vulkan, D3D11, Player, or object annotations.
