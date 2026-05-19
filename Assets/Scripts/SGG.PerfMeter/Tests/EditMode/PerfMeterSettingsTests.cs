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
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Fps);
			AssertHasModule(settings.OverlayModules, PerfMeterOverlayModule.Overdraw);
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
				string.Empty);

			string json = PerfMeterSettingsStore.ToJson(PerfMeterSettingsStore.CreateFromSnapshot(source));

			Assert.That(json, Does.Contain("schemaVersion"));
			Assert.That(PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot loaded), Is.True);
			Assert.That(loaded.LoadState, Is.EqualTo(PerfMeterSettingsLoadState.Loaded));
			Assert.That(loaded.CollectionMode, Is.EqualTo(PerfMeterCollectionMode.Background));
			Assert.That(loaded.OverlayVisible, Is.False);
			Assert.That(loaded.OverlayCorner, Is.EqualTo(PerfMeterOverlayCorner.BottomLeft));
			Assert.That(loaded.OverlayMode, Is.EqualTo(PerfMeterOverlayMode.Graphs));
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
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Graphs);
			AssertHasModule(loaded.OverlayModules, PerfMeterOverlayModule.Timing);
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
