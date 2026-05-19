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
			string metadata = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json").text;

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
			Assert.That(json, Does.Contain("\"schema_version\":1"));
			Assert.That(json, Does.Contain("\"is_available\":"));
			Assert.That(json, Does.Contain("\"position\":"));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void CameraSnapshotMcpCommandMetadataIsRegistered()
		{
			string metadata = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Scripts/SGG.PerfMeter/Editor/Mcp/mcp.commands.json").text;

			Assert.That(metadata, Does.Contain("\"id\": \"perfmeter.camera.snapshot\""));
			Assert.That(metadata, Does.Contain("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands.CameraSnapshot"));
			Assert.That(metadata, Does.Contain("\"camera_name_filter\""));
		}
	}
}
