using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSettingsTests
	{
		[Test]
		public void DefaultSettingsUseJsonZeroCodeValues()
		{
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;

			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Missing));
			Assert.That(settings.Enabled, Is.True);
			Assert.That(settings.AutoStart, Is.True);
			Assert.That(settings.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Overlay));
			Assert.That(settings.OverlayVisible, Is.True);
			Assert.That(settings.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.TopRight));
			Assert.That(settings.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(settings.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.ClassicDark));
			Assert.That(settings.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.DiagnosticsWide));
			Assert.That(settings.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.Manrope));
			Assert.That(settings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(settings.ActivePreset, Is.EqualTo(nameof(PerfMeterOverlayPreset.Custom)));
			Assert.That(settings.ActiveOverlayPresetId, Is.EqualTo(PerfMeterOverlayPresetDefaults.DefaultId));
			Assert.That(settings.ActiveOverlayPreset, Is.Not.Null);
			Assert.That(settings.SessionWarmupFrames, Is.EqualTo(0));
			Assert.That(settings.SessionWarmupSeconds, Is.EqualTo(0f).Within(0.001f));
			Assert.That(settings.SessionSampleIntervalSeconds, Is.EqualTo(0.25f).Within(0.001f));
			Assert.That(settings.SessionMaxSamples, Is.EqualTo(4096));
			Assert.That(settings.SessionResetOnSceneLoad, Is.False);
			Assert.That(settings.SessionSceneLoadIgnoreFrames, Is.EqualTo(0));
			Assert.That(settings.SessionSceneLoadIgnoreSeconds, Is.EqualTo(0f).Within(0.001f));
			Assert.That(settings.EditorWarningCooldownSeconds, Is.EqualTo(8f).Within(0.001f));
			Assert.That(settings.StructuredLogCooldownSeconds, Is.EqualTo(2f).Within(0.001f));
			Assert.That(settings.CallbackCooldownSeconds, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(settings.EditorWarningsEnabled, Is.True);
			Assert.That(settings.OverlayScale, Is.EqualTo(1f).Within(0.001f));
			Assert.That(settings.OverlayOpacity, Is.EqualTo(0.84f).Within(0.001f));
			Assert.That(settings.OverlayFontSize, Is.EqualTo(12f).Within(0.001f));
			Assert.That(settings.OverlayRefreshIntervalSeconds, Is.EqualTo(0.25f).Within(0.001f));
			Assert.That(settings.OverlayGraphHistoryLength, Is.EqualTo(120));
			Assert.That(settings.OverdrawDefaultFrameCount, Is.EqualTo(60));
			Assert.That(settings.OverdrawMaxFrameCount, Is.EqualTo(600));
			Assert.That(settings.AlertOverdrawRatioThreshold, Is.EqualTo(3d).Within(0.001d));
			Assert.That(settings.AlertTimingConsecutiveFrames, Is.EqualTo(5));
			Assert.That(settings.AlertFpsConsecutiveFrames, Is.EqualTo(60));
			Assert.That(settings.AlertGpuTimingUnavailableConsecutiveFrames, Is.EqualTo(120));
			Assert.That(settings.AlertOverdrawConsecutiveFrames, Is.EqualTo(3));
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Overdraw);
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
			AssertDoesNotHaveModule(settings.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(settings.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
		}

		[Test]
		public void SettingsJsonRoundTripsSnapshotValues()
		{
			PerfMeterSettingsSnapshot source = new PerfMeterSettingsSnapshot(
				true,
				true,
				PerfMeterCollectionMode.Background,
				false,
				PerfMeterOverlayCorner.BottomLeft,
				PerfMeterOverlayMode.Full,
				PerfMeterTargetFps.Fps120,
				"Timing",
				PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Graphs,
				3,
				1.25f,
				0.5f,
				128,
				true,
				2,
				0.75f,
				9f,
				3f,
				1f,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty,
				overlayScale: 1.25f,
				overlayOpacity: 0.66f,
				overlayFontSize: 14f,
				overlayRefreshIntervalSeconds: 0.5f,
				overlayGraphHistoryLength: 240,
				overdrawDefaultFrameCount: 24,
				overdrawMaxFrameCount: 180,
				alertOverdrawRatioThreshold: 2.5d,
				alertTimingConsecutiveFrames: 7,
				alertFpsConsecutiveFrames: 30,
				alertGpuTimingUnavailableConsecutiveFrames: 90,
				alertOverdrawConsecutiveFrames: 4,
				overlayTheme: PerfMeterOverlayTheme.Cyber,
				overlayLayout: PerfMeterOverlayLayout.DiagnosticsWide,
				overlayFontFamily: PerfMeterOverlayFontFamily.JetBrainsMono,
				editorWarningsEnabled: false);

			string json = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(source));

			Assert.That(json, Does.Contain("schemaVersion"));
			Assert.That(json, Does.Contain("\"theme\": \"Cyber\""));
			Assert.That(json, Does.Contain("\"layout\": \"DiagnosticsWide\""));
			Assert.That(json, Does.Contain("\"fontFamily\": \"JetBrainsMono\""));
			Assert.That(json, Does.Contain("\"editorWarningsEnabled\": false"));
			Assert.That(json, Does.Not.Contain("disableEditorWarnings"));
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot loaded), Is.True);
			Assert.That(loaded.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Loaded));
			Assert.That(loaded.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(loaded.OverlayVisible, Is.False);
			Assert.That(loaded.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(loaded.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(loaded.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.Cyber));
			Assert.That(loaded.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.DiagnosticsWide));
			Assert.That(loaded.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.JetBrainsMono));
			Assert.That(loaded.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(loaded.ActivePreset, Is.EqualTo(nameof(PerfMeterOverlayPreset.Custom)));
			Assert.That(loaded.SessionWarmupFrames, Is.EqualTo(3));
			Assert.That(loaded.SessionWarmupSeconds, Is.EqualTo(1.25f).Within(0.001f));
			Assert.That(loaded.SessionSampleIntervalSeconds, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(loaded.SessionMaxSamples, Is.EqualTo(128));
			Assert.That(loaded.SessionResetOnSceneLoad, Is.True);
			Assert.That(loaded.SessionSceneLoadIgnoreFrames, Is.EqualTo(2));
			Assert.That(loaded.SessionSceneLoadIgnoreSeconds, Is.EqualTo(0.75f).Within(0.001f));
			Assert.That(loaded.EditorWarningCooldownSeconds, Is.EqualTo(9f).Within(0.001f));
			Assert.That(loaded.StructuredLogCooldownSeconds, Is.EqualTo(3f).Within(0.001f));
			Assert.That(loaded.CallbackCooldownSeconds, Is.EqualTo(1f).Within(0.001f));
			Assert.That(loaded.EditorWarningsEnabled, Is.False);
			Assert.That(loaded.OverlayScale, Is.EqualTo(1.25f).Within(0.001f));
			Assert.That(loaded.OverlayOpacity, Is.EqualTo(0.66f).Within(0.001f));
			Assert.That(loaded.OverlayFontSize, Is.EqualTo(14f).Within(0.001f));
			Assert.That(loaded.OverlayRefreshIntervalSeconds, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(loaded.OverlayGraphHistoryLength, Is.EqualTo(240));
			Assert.That(loaded.OverdrawDefaultFrameCount, Is.EqualTo(24));
			Assert.That(loaded.OverdrawMaxFrameCount, Is.EqualTo(180));
			Assert.That(loaded.AlertOverdrawRatioThreshold, Is.EqualTo(2.5d).Within(0.001d));
			Assert.That(loaded.AlertTimingConsecutiveFrames, Is.EqualTo(7));
			Assert.That(loaded.AlertFpsConsecutiveFrames, Is.EqualTo(30));
			Assert.That(loaded.AlertGpuTimingUnavailableConsecutiveFrames, Is.EqualTo(90));
			Assert.That(loaded.AlertOverdrawConsecutiveFrames, Is.EqualTo(4));
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Graphs);
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Timing);
		}

		[Test]
		public void SnapshotUsesCustomLayoutForLegacyModeLayoutMismatch()
		{
			PerfMeterSettingsSnapshot snapshot = new PerfMeterSettingsSnapshot(
				true,
				true,
				PerfMeterCollectionMode.Overlay,
				true,
				PerfMeterOverlayCorner.TopRight,
				PerfMeterOverlayMode.Graphs,
				PerfMeterTargetFps.Fps60,
				PerfMeterSettingsStore.DefaultPresetId,
				PerfMeterSettingsStore.GetPresetModules(PerfMeterOverlayPreset.FullDiagnostics),
				0,
				0f,
				0.25f,
				4096,
				false,
				0,
				0f,
				8f,
				2f,
				0.5f,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty,
				overlayLayout: PerfMeterOverlayLayout.DiagnosticsWide);

			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Custom));
			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
		}

		[Test]
		public void ExistingRuleDefaultsWithoutEditorWarningToggleKeepWarningsEnabled()
		{
			string json = "{\"schemaVersion\":1,\"ruleDefaults\":{\"editorWarningCooldownSeconds\":9.0,\"structuredLogCooldownSeconds\":3.0,\"callbackCooldownSeconds\":1.0}}";

			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot snapshot), Is.True);
			Assert.That(snapshot.EditorWarningsEnabled, Is.True);
			Assert.That(snapshot.EditorWarningCooldownSeconds, Is.EqualTo(9f).Within(0.001f));
		}

		[Test]
		public void SettingsJsonClampsTunables()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.overlay.scale = 10f;
			settings.overlay.opacity = -1f;
			settings.overlay.fontSize = 100f;
			settings.overlay.refreshIntervalSeconds = 0f;
			settings.overlay.graphHistoryLength = 1;
			settings.overlay.theme = "bad-theme";
			settings.overlay.layout = "bad-layout";
			settings.overlay.fontFamily = "bad-font";
			settings.ruleDefaults.overdrawRatioThreshold = -1d;
			settings.ruleDefaults.timingConsecutiveFrames = 0;
			settings.ruleDefaults.fpsConsecutiveFrames = 0;
			settings.ruleDefaults.gpuTimingUnavailableConsecutiveFrames = 0;
			settings.ruleDefaults.overdrawConsecutiveFrames = 0;
			settings.overdraw.maxFrameCount = 12;
			settings.overdraw.defaultFrameCount = 120;

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.OverlayScale, Is.EqualTo(PerfMeterSettingsStore.MaxOverlayScale).Within(0.001f));
			Assert.That(snapshot.OverlayOpacity, Is.EqualTo(PerfMeterSettingsStore.MinOverlayOpacity).Within(0.001f));
			Assert.That(snapshot.OverlayFontSize, Is.EqualTo(PerfMeterSettingsStore.MaxOverlayFontSize).Within(0.001f));
			Assert.That(snapshot.OverlayRefreshIntervalSeconds, Is.EqualTo(PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds).Within(0.001f));
			Assert.That(snapshot.OverlayGraphHistoryLength, Is.EqualTo(PerfMeterSettingsStore.MinOverlayGraphHistoryLength));
			Assert.That(snapshot.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.ClassicDark));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			Assert.That(snapshot.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.Manrope));
			Assert.That(snapshot.Warning, Does.Contain("Invalid overlay theme"));
			Assert.That(snapshot.Warning, Does.Contain("Invalid overlay layout"));
			Assert.That(snapshot.Warning, Does.Contain("Invalid overlay fontFamily"));
			Assert.That(snapshot.AlertOverdrawRatioThreshold, Is.EqualTo(0.1d).Within(0.001d));
			Assert.That(snapshot.AlertTimingConsecutiveFrames, Is.EqualTo(1));
			Assert.That(snapshot.AlertFpsConsecutiveFrames, Is.EqualTo(1));
			Assert.That(snapshot.AlertGpuTimingUnavailableConsecutiveFrames, Is.EqualTo(1));
			Assert.That(snapshot.AlertOverdrawConsecutiveFrames, Is.EqualTo(1));
			Assert.That(snapshot.OverdrawMaxFrameCount, Is.EqualTo(12));
			Assert.That(snapshot.OverdrawDefaultFrameCount, Is.EqualTo(12));
		}

		[Test]
		public void ActivePresetControlsOverlayModules()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "Memory";

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Memory);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Gc);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Warnings);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.Overdraw);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
		}

		[Test]
		public void VisualOverlayPresetControlsOnlyVisualOverlaySettings()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.targetFps = (int)PerfMeterTargetFps.Fps120;
			settings.overlay.refreshIntervalSeconds = 0.5f;
			settings.activeOverlayPresetId = PerfMeterOverlayPresetDefaults.CompactTimingId;
			settings.overlayPresets = new[] { PerfMeterOverlayPresetDefaults.CreateCompactTiming() };

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.ActiveOverlayPresetId, Is.EqualTo(PerfMeterOverlayPresetDefaults.CompactTimingId));
			Assert.That(snapshot.ActivePreset, Is.EqualTo(nameof(PerfMeterOverlayPreset.Custom)));
			Assert.That(snapshot.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(snapshot.OverlayRefreshIntervalSeconds, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.CompactCards));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Timing);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.Rendering);
		}

		[Test]
		public void DefaultOverlayPresetIsFirstSelectableVisualPreset()
		{
			PerfMeterOverlayPresetJson[] presets = PerfMeterOverlayPresetDefaults.CreateDefaultPresets();

			Assert.That(presets.Length, Is.GreaterThan(0));
			Assert.That(presets[0].id, Is.EqualTo(PerfMeterOverlayPresetDefaults.DefaultId));
			Assert.That(presets[0].displayName, Is.EqualTo("Default"));
			Assert.That(PerfMeterOverlayPresetUtility.Validate(presets[0]).IsValid, Is.True);
		}

		[Test]
		public void FpsOnlyVisualPresetUsesCompactOneLineComposition()
		{
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateFpsOnly();
			PerfMeterOverlayModule modules = PerfMeterOverlayPresetUtility.GetEnabledModules(preset, out string warning);

			Assert.That(warning, Is.Empty);
			Assert.That(preset.style.layout, Is.EqualTo(nameof(PerfMeterOverlayLayout.FpsOnly)));
			Assert.That(preset.style.maxWidth, Is.LessThanOrEqualTo(360));
			Assert.That(preset.widgets.Length, Is.EqualTo(2));
			AssertHasWidget(preset, "fps.summary-card");
			AssertHasWidget(preset, "timing.cpu-card");
			AssertHasModule(modules, PerfMeterOverlayModule.Fps);
			AssertHasModule(modules, PerfMeterOverlayModule.Timing);
			AssertDoesNotHaveModule(modules, PerfMeterOverlayModule.Rendering);
			AssertDoesNotHaveModule(modules, PerfMeterOverlayModule.Graphs);
		}

		[Test]
		public void OverlayPresetJsonValidationIgnoresUnknownWidgetsWithoutThrowing()
		{
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateFpsOnly();
			preset.widgets = new[]
			{
				new PerfMeterOverlayPresetWidgetJson { id = "fps.summary-card", enabled = true, order = 10 },
				new PerfMeterOverlayPresetWidgetJson { id = "unknown.widget", enabled = true, order = 20 }
			};

			PerfMeterOverlayPresetValidationResult validation = PerfMeterOverlayPresetUtility.Validate(preset);
			PerfMeterOverlayModule modules = PerfMeterOverlayPresetUtility.GetEnabledModules(preset, out string warning);

			Assert.That(validation.IsValid, Is.True);
			Assert.That(validation.Warning, Does.Contain("Unknown overlay widget"));
			Assert.That(warning, Does.Contain("Unknown overlay widget"));
			AssertHasModule(modules, PerfMeterOverlayModule.Fps);
		}

		[Test]
		public void OverlayPresetJsonValidationRejectsDuplicateWidgets()
		{
			PerfMeterOverlayPresetJson preset = PerfMeterOverlayPresetDefaults.CreateFpsOnly();
			preset.widgets = new[]
			{
				new PerfMeterOverlayPresetWidgetJson { id = "fps.summary-card", enabled = true, order = 10 },
				new PerfMeterOverlayPresetWidgetJson { id = "fps.summary-card", enabled = true, order = 20 }
			};

			PerfMeterOverlayPresetValidationResult validation = PerfMeterOverlayPresetUtility.Validate(preset);

			Assert.That(validation.IsValid, Is.False);
			Assert.That(validation.Warning, Does.Contain("duplicate widget id"));
		}

		[Test]
		public void LayoutMismatchMarksActivePresetCustom()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "Timing";
			settings.overlay.layout = nameof(PerfMeterOverlayLayout.MetricBars);

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.ActivePreset, Is.EqualTo(nameof(PerfMeterOverlayPreset.Custom)));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Timing);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Graphs);
		}

		[Test]
		public void AgentDebugPresetDoesNotEnableCpuCoreModulesByDefault()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "AgentDebug";
			settings.overlay.layout = nameof(PerfMeterOverlayLayout.TextCompact);

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.TextCompact));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.TextCompact));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
		}

		[Test]
		public void SettingsJsonParsesOptionalOverlayModules()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "Custom";
			settings.presets = new[]
			{
				new PerfMeterPresetSettingsJson
				{
					id = "Custom",
					overlayVisible = true,
					targetFps = (int)PerfMeterTargetFps.Fps60,
					modules = new[] { "Fps", "CustomMetrics", "CpuCoreBars", "CpuCoreGraphs" }
				}
			};

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.Memory);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCores);
		}

		[Test]
		public void EmptyPresetModulesUsePresetDefaultsWithDefaultCpuCoreBars()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "Custom";
			settings.presets = new[]
			{
				new PerfMeterPresetSettingsJson
				{
					id = "Custom",
					overlayVisible = true,
					targetFps = (int)PerfMeterTargetFps.Fps60,
					modules = Array.Empty<string>()
				}
			};

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Timing);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
		}

		[Test]
		public void InvalidPresetModulesUsePresetDefaultsWithDefaultCpuCoreBars()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activeOverlayPresetId = string.Empty;
			settings.overlayPresets = Array.Empty<PerfMeterOverlayPresetJson>();
			settings.activePreset = "Custom";
			settings.presets = new[]
			{
				new PerfMeterPresetSettingsJson
				{
					id = "Custom",
					overlayVisible = true,
					targetFps = (int)PerfMeterTargetFps.Fps60,
					modules = new[] { "NotAModule" }
				}
			};

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCores);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreBars);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.CpuCoreGraphs);
			Assert.That(snapshot.Warning, Does.Contain("Unknown overlay module"));
		}

		[Test]
		public void SettingsJsonMissingThemeAndLayoutUsesDefaultsWithoutWarning()
		{
			string json = "{\"schemaVersion\":1,\"enabled\":true,\"autoStart\":true,\"overlay\":{\"scale\":1.0}}";

			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot snapshot), Is.True);
			Assert.That(snapshot.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.ClassicDark));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.MetricBars));
			Assert.That(snapshot.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.Manrope));
			Assert.That(snapshot.Warning, Does.Not.Contain("Invalid overlay theme"));
			Assert.That(snapshot.Warning, Does.Not.Contain("Invalid overlay layout"));
			Assert.That(snapshot.Warning, Does.Not.Contain("Invalid overlay fontFamily"));
		}

		[Test]
		public void UnsupportedSettingsSchemaFallsBackSafely()
		{
			bool loaded = PerfMeterSettingsStore.TryReadSnapshot("{\"schemaVersion\":999,\"enabled\":true}", out PerfMeterSettingsSnapshot settings);

			Assert.That(loaded, Is.False);
			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.UnsupportedVersion));
			Assert.That(settings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(settings.Warning, Does.Contain("newer than supported"));
		}

		[Test]
		public void InvalidSettingsJsonFallsBackSafely()
		{
			bool loaded = PerfMeterSettingsStore.TryReadSnapshot(string.Empty, out PerfMeterSettingsSnapshot settings);

			Assert.That(loaded, Is.False);
			Assert.That(settings.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Invalid));
			Assert.That(settings.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Full));
			Assert.That(settings.Warning, Does.Contain("empty"));
		}

		[Test]
		public void PackageJsonDeclaresImportableSamples()
		{
			string packageRoot = GetPackageRoot();
			string packageJsonPath = Path.Combine(packageRoot, "package.json");
			string packageJson = File.ReadAllText(packageJsonPath);

			AssertSample(packageRoot, packageJson, "Samples~/BootstrapAndSettings", "README.md");
			AssertSample(packageRoot, packageJson, "Samples~/RuntimeWorkflows", "README.md");
			AssertSample(packageRoot, packageJson, "Samples~/EditorAutomation", "README.md");
			Assert.That(File.Exists(Path.Combine(packageRoot, "Samples~/BootstrapAndSettings/Resources/SGG.PerfMeter/perfmeter-settings.json")), Is.True);
		}

		[Test]
		public void SetupWindowLocalizationFilesCoverSameSources()
		{
			string localizationRoot = Path.Combine(GetPackageRoot(), "Editor/UI/Localization");
			HashSet<string> english = ReadXlfSources(Path.Combine(localizationRoot, "perfmeter-window.en.xlf"));
			HashSet<string> russian = ReadXlfSources(Path.Combine(localizationRoot, "perfmeter-window.ru.xlf"));

			List<string> missingRussian = MissingSources(english, russian);
			List<string> missingEnglish = MissingSources(russian, english);

			Assert.That(missingRussian, Is.Empty, "Russian localization is missing source entries.");
			Assert.That(missingEnglish, Is.Empty, "English localization is missing source entries.");
		}

		[Test]
		public void SetupWindowUssKeepsAccessibleContrast()
		{
			string uss = File.ReadAllText(Path.Combine(GetPackageRoot(), "Editor/UI/PerfMeterSetupWindow.uss"));
			Dictionary<string, string> variables = ExtractCssVariables(uss);
			Color panel = ResolveCssColor("var(--pm-panel)", variables, Color.black);

			AssertContrast("section caption", GetCssProperty(uss, ".pm-section-caption", "color"), GetCssProperty(uss, ".pm-section-caption", "background-color"), variables, panel);
			AssertContrast("debug table header", GetCssProperty(uss, ".pm-debug-row--header .pm-debug-cell", "color"), GetCssProperty(uss, ".pm-debug-row--header", "background-color"), variables, panel);
			AssertContrast("active button", GetCssProperty(uss, ".pm-button--active", "color"), GetCssProperty(uss, ".pm-button--active", "background-color"), variables, panel);
			AssertContrast("active work mode checkbox text", GetCssProperty(uss, ".pm-workmode-toggle.pm-button--active .unity-toggle__text", "color"), GetCssProperty(uss, ".pm-button--active", "background-color"), variables, panel);
			AssertContrast("inactive work mode checkbox text", GetCssProperty(uss, ".pm-workmode-toggle--off", "color"), GetCssProperty(uss, ".pm-workmode-toggle", "background-color"), variables, panel);
			AssertContrast("info/value row text", "var(--pm-text-muted)", "#141414", variables, panel);
			AssertContrast("copy button icon", GetCssProperty(uss, ".pm-copy-button", "color"), "var(--pm-panel)", variables, panel);
			AssertContrast("checklist success icon", "var(--pm-ok)", "#141414", variables, panel);
			AssertContrast("checklist warning icon", "var(--pm-warn)", "#141414", variables, panel);
			AssertContrast("checklist optional icon", "var(--pm-optional)", "#141414", variables, panel);
			AssertContrast("checklist error icon", "var(--pm-error)", "#141414", variables, panel);
		}

		private static void AssertHasModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == expected, Is.True);
		}

		private static void AssertHasWidget(PerfMeterOverlayPresetJson preset, string widgetId)
		{
			for (int i = 0; i < preset.widgets.Length; i++)
			{
				if (preset.widgets[i] != null && string.Equals(preset.widgets[i].id, widgetId, StringComparison.Ordinal))
				{
					Assert.That(preset.widgets[i].enabled, Is.True);
					return;
				}
			}

			Assert.Fail("Missing overlay preset widget " + widgetId);
		}

		private static HashSet<string> ReadXlfSources(string path)
		{
			HashSet<string> sources = new HashSet<string>(StringComparer.Ordinal);
			string content = File.ReadAllText(path);
			foreach (Match match in Regex.Matches(content, "<source[^>]*>(.*?)</source>", RegexOptions.Singleline))
			{
				string source = WebUtility.HtmlDecode(match.Groups[1].Value);
				if (!string.IsNullOrEmpty(source))
				{
					sources.Add(source);
				}
			}

			return sources;
		}

		private static List<string> MissingSources(HashSet<string> expected, HashSet<string> actual)
		{
			List<string> missing = new List<string>();
			foreach (string source in expected)
			{
				if (!actual.Contains(source))
				{
					missing.Add(source);
				}
			}

			missing.Sort(StringComparer.Ordinal);
			return missing;
		}

		private static Dictionary<string, string> ExtractCssVariables(string uss)
		{
			Dictionary<string, string> variables = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (Match match in Regex.Matches(uss, @"(?m)^\s*(--pm-[\w-]+)\s*:\s*([^;]+);"))
			{
				variables[match.Groups[1].Value] = match.Groups[2].Value.Trim();
			}

			return variables;
		}

		private static string GetCssProperty(string uss, string selector, string property)
		{
			Match rule = Regex.Match(uss, Regex.Escape(selector) + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
			Assert.That(rule.Success, Is.True, "Missing USS selector " + selector);

			Match value = Regex.Match(rule.Groups["body"].Value, @"(?m)^\s*" + Regex.Escape(property) + @"\s*:\s*(?<value>[^;]+);");
			Assert.That(value.Success, Is.True, "Missing USS property " + selector + " / " + property);
			return value.Groups["value"].Value.Trim();
		}

		private static void AssertContrast(string label, string foregroundValue, string backgroundValue, Dictionary<string, string> variables, Color compositeBase)
		{
			Color foreground = ResolveCssColor(foregroundValue, variables, compositeBase);
			Color background = ResolveCssColor(backgroundValue, variables, compositeBase);
			double ratio = ContrastRatio(foreground, background);
			Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5d), label + " contrast ratio");
		}

		private static Color ResolveCssColor(string value, Dictionary<string, string> variables, Color compositeBase)
		{
			string trimmed = value.Trim();
			Match variable = Regex.Match(trimmed, @"^var\((--pm-[^)]+)\)$");
			if (variable.Success)
			{
				Assert.That(variables.TryGetValue(variable.Groups[1].Value, out string variableValue), Is.True, "Missing USS variable " + variable.Groups[1].Value);
				return ResolveCssColor(variableValue, variables, compositeBase);
			}

			if (trimmed.StartsWith("#", StringComparison.Ordinal))
			{
				Assert.That(ColorUtility.TryParseHtmlString(trimmed, out Color color), Is.True, "Invalid USS color " + trimmed);
				return CompositeOver(color, compositeBase);
			}

			Match rgb = Regex.Match(trimmed, @"^rgba?\(([^)]+)\)$");
			Assert.That(rgb.Success, Is.True, "Unsupported USS color " + trimmed);

			string[] parts = rgb.Groups[1].Value.Split(',');
			Assert.That(parts.Length == 3 || parts.Length == 4, Is.True, "Unsupported USS color " + trimmed);
			Color parsed = new Color(
				ParseCssColorChannel(parts[0]),
				ParseCssColorChannel(parts[1]),
				ParseCssColorChannel(parts[2]),
				parts.Length == 4 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1f);
			return CompositeOver(parsed, compositeBase);
		}

		private static float ParseCssColorChannel(string value)
		{
			return float.Parse(value.Trim(), CultureInfo.InvariantCulture) / 255f;
		}

		private static Color CompositeOver(Color color, Color background)
		{
			if (color.a >= 0.999f)
			{
				return new Color(color.r, color.g, color.b, 1f);
			}

			float alpha = Mathf.Clamp01(color.a);
			return new Color(
				color.r * alpha + background.r * (1f - alpha),
				color.g * alpha + background.g * (1f - alpha),
				color.b * alpha + background.b * (1f - alpha),
				1f);
		}

		private static double ContrastRatio(Color foreground, Color background)
		{
			double foregroundLuminance = RelativeLuminance(foreground);
			double backgroundLuminance = RelativeLuminance(background);
			double lighter = Math.Max(foregroundLuminance, backgroundLuminance);
			double darker = Math.Min(foregroundLuminance, backgroundLuminance);
			return (lighter + 0.05d) / (darker + 0.05d);
		}

		private static double RelativeLuminance(Color color)
		{
			return 0.2126d * LinearRgb(color.r) + 0.7152d * LinearRgb(color.g) + 0.0722d * LinearRgb(color.b);
		}

		private static double LinearRgb(float channel)
		{
			return channel <= 0.03928f ? channel / 12.92d : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
		}

		private static void AssertSample(string packageRoot, string packageJson, string samplePath, string requiredFile)
		{
			Assert.That(packageJson, Does.Contain(samplePath));
			Assert.That(File.Exists(Path.Combine(packageRoot, samplePath, requiredFile)), Is.True);
		}

		private static string GetPackageRoot()
		{
			PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(PerformanceMeter).Assembly);
			return packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath)
				? packageInfo.resolvedPath
				: Path.Combine(Application.dataPath, "Scripts/SGG.PerfMeter");
		}

		private static void AssertDoesNotHaveModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == 0, Is.True);
		}
	}
}
