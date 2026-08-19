# RenderDoc Annotation Bridge Validation Receipt

This receipt records the original feature-branch validation inputs. It is not a
release-artifact manifest and no native DLL is committed to the binary-free UPM
package.

Feature: `PM-RDANN-002` Windows x64 Editor/D3D12 command annotations

## Inputs

- Source: package `Native~/RenderDocBridge` in `feature/perfmeter-renderdoc-annotations`.
- Compiler: Microsoft Visual C++ from Visual Studio Community 18 (2026), x64 Release.
- CMake: Visual Studio bundled CMake.
- Unity PluginAPI headers: Unity `6000.5.6f1`, `Editor/Data/PluginAPI`.
- RenderDoc header: commit `7db2264afa00a5313154022f8c4ae0628a641300`, SHA-256 `b7005e7dc34c3635046868bbd76d81b9b055aede0f56daa0bd39fedee0639ffb`.
- Warnings: `/W4 /WX`.

## Verification

- CTest: passed `1/1`; the fake-table executable passed all `16/16` internal cases.
- Unity annotation EditMode tests passed `10/10`; the final Unity `6000.4.12f1` D3D12 EditMode suite passed `433/433`.
- Plain production resolver probe reported `SGG_RD_NOT_LOADED`; the same probe launched by portable RenderDoc v1.46 reported module/export present, App API `1.7.0`, and annotations available.
- Real D3D12 Editor smokes passed on Unity `6000.4.12f1` and `6000.5.6f1`: two packets, eight RenderDoc calls, zero errors, and XML-confirmed set → red clear → delete → blue clear ordering.
- The package does not contain `renderdoc.dll` or RenderDoc replay binaries.
- Distribution is a future separately verified Windows x64 bridge artifact installed project-locally by FTUE.
- The current published `2026.8.11-1` bridge exposes the capture ABI only; annotation calls map to `BridgeTooOld`.

This historical receipt proves the original source build, fake/native contract suite, and the two recorded Windows x64 Editor/D3D12 matrix rows. Rebased source requires a fresh artifact receipt before publication. It does not prove Vulkan, D3D11, Player, object annotations, or external-consumer acceptance.
