using System;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using SGG.PerfMeter.Editor.Setup;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterCompatibilityTests
	{
		private const string UniversalPackageName = "com.unity.render-pipelines.universal";
		private const string HighDefinitionPackageName = "com.unity.render-pipelines.high-definition";

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
		public void Unity2022_3IsImportCompatibleOnly()
		{
			PerfMeterCompatibilityStatus status = Evaluate("2022.3.0f1", PerfMeterRenderPipelineKind.Unknown, string.Empty, string.Empty, false);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.False);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
		}

		[Test]
		public void Unity6000_3IsImportCompatibleOnly()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.3.8f1", PerfMeterRenderPipelineKind.Universal, UniversalPackageName, "17.4.0", true);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.False);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
			Assert.That(status.RenderIntegrationReason, Does.Contain("core runtime"));
		}

		[Test]
		public void Unity6000_4Urp17_4WithAdapterIsFullyCompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.Universal, UniversalPackageName, "17.4.0", true);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.True);
		}

		[Test]
		public void Unity6000_4Hdrp17_4WithAdapterIsFullyCompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.HighDefinition, HighDefinitionPackageName, "17.4.0", true);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.True);
		}

		[Test]
		public void OldSrpPackageIsNotRenderCompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.Universal, UniversalPackageName, "17.3.9", true);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
			Assert.That(status.RenderIntegrationReason, Does.Contain("below"));
		}

		[Test]
		public void MissingAdapterIsNotRenderCompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.Universal, UniversalPackageName, "17.4.0", false);

			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
			Assert.That(status.RenderIntegrationReason, Does.Contain("adapter assembly"));
		}

		[TestCase("", "")]
		[TestCase("com.example.wrong-pipeline", "17.4.0")]
		public void MissingOrWrongPipelinePackageIsNotRenderCompatible(string packageName, string packageVersion)
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.Universal, packageName, packageVersion, true);

			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
			Assert.That(status.RenderIntegrationReason, Does.Contain(UniversalPackageName));
		}

		[Test]
		public void BuiltInPipelineRetainsCoreRuntimeCompatibilityOnly()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.BuiltIn, string.Empty, string.Empty, false);

			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
		}

		[Test]
		public void UnknownPipelineIsNotRenderCompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("6000.4.7f1", PerfMeterRenderPipelineKind.Unknown, string.Empty, string.Empty, true);

			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
		}

		[Test]
		public void MalformedUnityVersionIsExplicitlyIncompatible()
		{
			PerfMeterCompatibilityStatus status = Evaluate("not-a-version", PerfMeterRenderPipelineKind.Universal, UniversalPackageName, "17.4.0", true);

			Assert.That(status.ImportCompatible, Is.False);
			Assert.That(status.CoreRuntimeCompatible, Is.False);
			Assert.That(status.RenderIntegrationCompatible, Is.False);
			Assert.That(status.ImportReason, Does.Contain("malformed"));
			Assert.That(status.CoreRuntimeReason, Does.Contain("malformed"));
		}

		[Test]
		public void VersionParserHandlesUnitySuffixAndSemver()
		{
			Assert.That(PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.TryParseMajorMinor("6000.5.6f1", out int unityMajor, out int unityMinor), Is.True);
			Assert.That(unityMajor, Is.EqualTo(6000));
			Assert.That(unityMinor, Is.EqualTo(5));
			Assert.That(PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.TryParseMajorMinor("17.4.0-pre.1", out int packageMajor, out int packageMinor), Is.True);
			Assert.That(packageMajor, Is.EqualTo(17));
			Assert.That(packageMinor, Is.EqualTo(4));
			Assert.That(PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.TryParseMajorMinor("malformed", out _, out _), Is.False);
			Assert.That(PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.TryParseMajorMinor("17.4.0.1", out _, out _), Is.False);
			Assert.That(PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.TryParseMajorMinor("6000.5.6.1", out _, out _), Is.False);
		}

		[Test]
		public void CurrentCompatibilityStatusAndReportDoNotStartRuntime()
		{
			PerfMeterCompatibilityStatus status = PerfMeterSetupActions.GetCompatibilityStatus();
			string report = PerfMeterSetupActions.GetStatusReport();

			Assert.That(status.CurrentUnityVersion, Is.Not.Empty);
			Assert.That(status.ImportCompatible, Is.True);
			Assert.That(status.CoreRuntimeCompatible, Is.True);
			Assert.That(status.RenderIntegrationCompatible, Is.True);
			Assert.That(status.ImportUnityVersionFloor, Is.EqualTo("2022.3"));
			Assert.That(status.CoreRuntimeUnityVersionFloor, Is.EqualTo("6000.4"));
			Assert.That(status.RenderIntegrationPipelinePackageVersionFloor, Is.EqualTo("17.4"));
			Assert.That(report, Does.Contain("Import compatibility:"));
			Assert.That(report, Does.Contain(status.CoreRuntimeReason));
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		[Test]
		public void DeclaredImportFloorMatchesPackageMetadata()
		{
			Assert.That(PerfMeterTestAssets.ReadPackageJson(), Does.Contain("\"unity\": \"" + PerfMeterSetupUtility.ImportUnityVersionFloor + "\""));
		}

		[Test]
		public void OfficialUnityVersionSupportedMatchesCoreRuntimeCompatibility()
		{
			PerfMeterCompatibilityStatus status = PerfMeterSetupActions.GetCompatibilityStatus();

			Assert.That(PerfMeterSetupUtility.IsOfficialUnityVersionSupported, Is.EqualTo(status.CoreRuntimeCompatible));
		}

		[Test]
		public void CompatibilityMcpPayloadIsStructuredAndDoesNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			string compatibility = PerfMeterMcpCommands.CompatibilityStatus();
			string setup = PerfMeterMcpCommands.SetupStatus();

			Assert.That(metadata, Does.Contain("perfmeter.compatibility.status"));
			Assert.That(metadata, Does.Contain("PerfMeterMcpCommands.CompatibilityStatus"));
			int commandStart = metadata.IndexOf("\"id\": \"perfmeter.compatibility.status\"", StringComparison.Ordinal);
			int commandEnd = metadata.IndexOf("\"id\":", commandStart + 5, StringComparison.Ordinal);
			string commandMetadata = metadata.Substring(commandStart, commandEnd - commandStart);
			Assert.That(commandMetadata, Does.Contain("\"risk\": \"read\""));
			Assert.That(commandMetadata, Does.Contain("\"idempotency\": \"safe\""));
			Assert.That(commandMetadata, Does.Contain("\"additionalProperties\": false"));
			CompatibilityPayload compatibilityPayload = JsonUtility.FromJson<CompatibilityPayload>(compatibility);
			SetupPayload setupPayload = JsonUtility.FromJson<SetupPayload>(setup);
			Assert.That(compatibilityPayload, Is.Not.Null);
			Assert.That(compatibilityPayload.import_compatible, Is.True);
			Assert.That(compatibilityPayload.core_runtime_compatible, Is.True);
			Assert.That(compatibilityPayload.render_integration_compatible, Is.True);
			Assert.That(compatibilityPayload.import_unity_version_floor, Is.EqualTo("2022.3"));
			Assert.That(compatibilityPayload.core_runtime_unity_version_floor, Is.EqualTo("6000.4"));
			Assert.That(compatibilityPayload.render_integration_pipeline_package_version_floor, Is.EqualTo("17.4"));
			Assert.That(setupPayload, Is.Not.Null);
			Assert.That(setupPayload.compatibility, Is.Not.Null);
			Assert.That(setupPayload.compatibility.render_integration_compatible, Is.True);
			Assert.That(setupPayload.status_report, Is.Not.Empty);
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));
		}

		private static PerfMeterCompatibilityStatus Evaluate(
			string unityVersion,
			PerfMeterRenderPipelineKind pipelineKind,
			string packageName,
			string packageVersion,
			bool adapterAvailable)
		{
			return PerfMeterSetupUtility.PerfMeterCompatibilityEvaluator.Evaluate(
				unityVersion,
				pipelineKind,
				packageName,
				packageVersion,
				adapterAvailable);
		}

		[Serializable]
		private sealed class CompatibilityPayload
		{
			public bool import_compatible;
			public bool core_runtime_compatible;
			public bool render_integration_compatible;
			public string import_unity_version_floor;
			public string core_runtime_unity_version_floor;
			public string render_integration_pipeline_package_version_floor;
		}

		[Serializable]
		private sealed class SetupPayload
		{
			public CompatibilityPayload compatibility;
			public string status_report;
		}
	}
}
