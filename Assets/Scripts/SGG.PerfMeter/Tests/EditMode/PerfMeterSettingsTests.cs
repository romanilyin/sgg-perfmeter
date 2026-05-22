using System.IO;
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
			Assert.That(settings.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Classic));
			Assert.That(settings.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.Manrope));
			Assert.That(settings.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps60));
			Assert.That(settings.ActivePreset, Is.EqualTo(PerfMeterSettingsStore.DefaultPresetId));
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
				PerfMeterOverlayMode.Graphs,
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
				overlayFontFamily: PerfMeterOverlayFontFamily.JetBrainsMono);

			string json = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(source));

			Assert.That(json, Does.Contain("schemaVersion"));
			Assert.That(json, Does.Contain("\"theme\": \"Cyber\""));
			Assert.That(json, Does.Contain("\"layout\": \"DiagnosticsWide\""));
			Assert.That(json, Does.Contain("\"fontFamily\": \"JetBrainsMono\""));
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot loaded), Is.True);
			Assert.That(loaded.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Loaded));
			Assert.That(loaded.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(loaded.OverlayVisible, Is.False);
			Assert.That(loaded.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(loaded.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
			Assert.That(loaded.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.Cyber));
			Assert.That(loaded.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.DiagnosticsWide));
			Assert.That(loaded.OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.JetBrainsMono));
			Assert.That(loaded.TargetFps, Is.EqualTo(PerfMeterTargetFps.Fps120));
			Assert.That(loaded.ActivePreset, Is.EqualTo("Timing"));
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
		public void SettingsJsonClampsTunables()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
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
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Classic));
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
		public void AgentDebugPresetIncludesCustomMetricsModule()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activePreset = "AgentDebug";

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			Assert.That(snapshot.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.TextCompact));
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
		}

		[Test]
		public void SettingsJsonParsesCustomMetricsModule()
		{
			PerfMeterSettingsJson settings = PerfMeterSettingsStore.CreateDefault();
			settings.activePreset = "Custom";
			settings.presets = new[]
			{
				new PerfMeterPresetSettingsJson
				{
					id = "Custom",
					overlayVisible = true,
					overlayMode = nameof(PerfMeterOverlayMode.Full),
					targetFps = (int)PerfMeterTargetFps.Fps60,
					modules = new[] { "Fps", "CustomMetrics" }
				}
			};

			PerfMeterSettingsSnapshot snapshot = PerfMeterSettingsStore.ToSnapshot(settings, PerfMeterSettingsLoadState.Loaded, string.Empty);

			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(snapshot.OverlayModules, PerfMeterOverlayModule.CustomMetrics);
			AssertDoesNotHaveModule(snapshot.OverlayModules, PerfMeterOverlayModule.Memory);
		}

		[Test]
		public void SettingsJsonMissingThemeAndLayoutUsesDefaultsWithoutWarning()
		{
			string json = "{\"schemaVersion\":1,\"enabled\":true,\"autoStart\":true,\"overlay\":{\"scale\":1.0}}";

			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot snapshot), Is.True);
			Assert.That(snapshot.OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.ClassicDark));
			Assert.That(snapshot.OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.Classic));
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

		private static void AssertHasModule(PerfMeterOverlayModule actual, PerfMeterOverlayModule expected)
		{
			Assert.That((actual & expected) == expected, Is.True);
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
