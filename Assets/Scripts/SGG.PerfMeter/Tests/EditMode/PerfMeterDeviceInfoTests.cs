using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterDeviceInfoTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
		}

		[Test]
		public void GetDeviceInfoReturnsSafeSnapshotBeforeRuntimeStart()
		{
			Assert.That(PerformanceMeter.TryGetDeviceInfo(out PerfMeterDeviceSnapshot device), Is.True);

			Assert.That(device.UnityVersion, Is.Not.Empty);
			Assert.That(device.OperatingSystem, Is.Not.Null);
			Assert.That(device.GraphicsDeviceName, Is.Not.Null);
			Assert.That(device.ProcessorCount, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.SystemMemorySizeMb, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.ScreenWidth, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.ScreenHeight, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.CurrentResolutionWidth, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.CurrentResolutionHeight, Is.GreaterThanOrEqualTo(0));
			Assert.That(device.Displays, Is.Not.Null);
			Assert.That(device.Displays.Length, Is.GreaterThanOrEqualTo(1));
		}

		[Test]
		public void DeviceInfoMcpCommandDoesNotStartRuntime()
		{
			string json = PerfMeterMcpCommands.DeviceInfo();

			Assert.That(json, Does.StartWith("{"));
			Assert.That(json, Does.EndWith("}"));
			Assert.That(json, Does.Contain("\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"unity_version\":"));
			Assert.That(json, Does.Contain("\"graphics_device_type\":"));
			Assert.That(json, Does.Contain("\"display_layout_available\":"));
			Assert.That(json, Does.Contain("\"displays\":"));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void DeviceInfoMcpCommandMetadataIsRegistered()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("\"id\": \"perfmeter.device.info\""));
			Assert.That(metadata, Does.Contain("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands.DeviceInfo"));
			Assert.That(metadata, Does.Contain("\"risk\": \"read\""));
			Assert.That(metadata, Does.Contain("\"idempotency\": \"safe\""));
		}

		[Test]
		public void CameraSnapshotMcpCommandDoesNotStartRuntime()
		{
			string json = PerfMeterMcpCommands.CameraSnapshot("{}");

			Assert.That(json, Does.StartWith("{"));
		#if UNITY_6000_4_OR_NEWER
			Assert.That(json, Does.Contain("\"schema_version\":2"));
			Assert.That(json, Does.Contain("\"camera_entity_id\":"));
			Assert.That(json, Does.Not.Contain("\"camera_instance_id\":"));
		#else
			Assert.That(json, Does.Contain("\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"camera_instance_id\":"));
		#endif
			Assert.That(json, Does.Contain("\"is_available\":"));
			Assert.That(json, Does.Contain("\"position\":"));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void CameraSnapshotMcpCommandMetadataIsRegistered()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("\"id\": \"perfmeter.camera.snapshot\""));
			Assert.That(metadata, Does.Contain("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands.CameraSnapshot"));
			Assert.That(metadata, Does.Contain("\"camera_name_filter\""));
		}

		[Test]
		public void OverlaySetMcpCommandAppliesPresetAndModules()
		{
			string json = PerfMeterMcpCommands.OverlaySet("{\"visible\":true,\"preset\":\"Timing\",\"modules\":[\"Fps\",\"Timing\",\"Graphs\",\"Warnings\",\"CustomMetrics\"],\"theme\":\"Glass\",\"layout\":\"CompactCards\",\"font_family\":\"JetBrainsMono\",\"target_fps\":30}");

			Assert.That(json, Does.Contain("\"overlay_preset\":\"Timing\""));
			Assert.That(json, Does.Contain("\"overlay_theme\":\"Glass\""));
			Assert.That(json, Does.Contain("\"overlay_layout\":\"CompactCards\""));
			Assert.That(json, Does.Contain("\"overlay_font_family\":\"JetBrainsMono\""));
			Assert.That(json, Does.Contain("\"overlay_modules\":[\"Fps\",\"Timing\",\"Graphs\",\"Warnings\",\"CustomMetrics\"]"));
			Assert.That(json, Does.Contain("\"target_fps\":30"));
			Assert.That(PerformanceMeter.GetStatus().OverlayPreset, Is.EqualTo(PerfMeterOverlayPreset.Timing));
			Assert.That(PerformanceMeter.GetStatus().OverlayTheme, Is.EqualTo(PerfMeterOverlayTheme.Glass));
			Assert.That(PerformanceMeter.GetStatus().OverlayLayout, Is.EqualTo(PerfMeterOverlayLayout.CompactCards));
			Assert.That(PerformanceMeter.GetStatus().OverlayFontFamily, Is.EqualTo(PerfMeterOverlayFontFamily.JetBrainsMono));
			Assert.That((PerformanceMeter.GetStatus().OverlayModules & PerfMeterOverlayModule.Graphs) == PerfMeterOverlayModule.Graphs, Is.True);
			Assert.That((PerformanceMeter.GetStatus().OverlayModules & PerfMeterOverlayModule.CustomMetrics) == PerfMeterOverlayModule.CustomMetrics, Is.True);
		}

		[Test]
		public void OverlaySetMcpCommandMetadataIncludesPresetAndModules()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("\"id\": \"perfmeter.overlay.set\""));
			Assert.That(metadata, Does.Contain("\"preset\""));
			Assert.That(metadata, Does.Contain("\"modules\""));
			Assert.That(metadata, Does.Contain("\"theme\""));
			Assert.That(metadata, Does.Contain("\"layout\""));
			Assert.That(metadata, Does.Contain("\"font_family\""));
			Assert.That(metadata, Does.Contain("\"Cyber\""));
			Assert.That(metadata, Does.Contain("\"CompactCards\""));
			Assert.That(metadata, Does.Contain("\"JetBrainsMono\""));
			Assert.That(metadata, Does.Contain("\"AgentDebug\""));
			Assert.That(metadata, Does.Contain("\"CustomMetrics\""));
		}
	}
}
