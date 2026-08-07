using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterCaptureBundleTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[Test]
		public void BundleCoordinatorFreezesCorrelatedCaptureData()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("bundle-data", includeScreenshot: false);

			PerfMeterCaptureBundleStatusSnapshot status = coordinator.GetStatus("bundle-data");
			Assert.That(status.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(status.CaptureSampleCount, Is.EqualTo(1));
			Assert.That(status.BaselineSampleCount, Is.EqualTo(1));
			Assert.That(status.AlertEventCount, Is.EqualTo(1));
			Assert.That(status.AlertEventsTruncated, Is.True);
			Assert.That(status.ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.NotRequested));
			Assert.That(coordinator.TryGetExportData("bundle-data", out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.CaptureSamples, Has.Length.EqualTo(1));
			Assert.That(data.BaselineSamples, Has.Length.EqualTo(1));
			Assert.That(data.CaptureSamples[0].PlatformTelemetry.ProviderId, Is.EqualTo("capture.provider"));
			Assert.That(data.CaptureSamples[0].PlatformTelemetry.TemperatureLevel, Is.EqualTo(0.7f));
			Assert.That(data.AlertEvents[0].CaptureId, Is.EqualTo("bundle-data"));
		}

		[Test]
		public void BundleExportFreezesNeutralRenderContextAdditivelyBesideLegacyContext()
		{
			const string captureId = "render-context";
			PerfMeterRenderPipelineSnapshot firstPipeline = new PerfMeterRenderPipelineSnapshot(
				PerfMeterRenderPipelineKind.Universal,
				"Synthetic Universal Asset",
				"SyntheticUniversalPipelineAsset",
				"SyntheticUniversalRuntime");
			PerfMeterGpuResidentDrawerContextSnapshot firstGrd = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Available,
				"Enabled",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Unknown,
				false,
				"synthetic GRD activity is unknown");
			PerfMeterVariableRateShadingContextSnapshot firstVrs = new PerfMeterVariableRateShadingContextSnapshot(
				PerfMeterAvailability.Available,
				true,
				true,
				true,
				16,
				16,
				"R8G8B8A8_UNorm",
				PerfMeterAvailability.Unknown,
				false,
				PerfMeterAvailability.Unknown,
				false,
				"synthetic VRS configuration and activity are unknown");
			PerfMeterRenderGraphSnapshot firstLegacyRender = new PerfMeterRenderGraphSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderGraphState.Observed,
				72,
				"Legacy Camera First",
				"Game",
				2,
				4,
				3,
				5,
				6,
				7,
				"legacy-first-warning",
				PerfMeterRenderPipelineKind.Universal,
				"Legacy First Integration",
				"BeforeRendering");
			PerfMeterRenderIntegrationSnapshot firstNeutralRender = new PerfMeterRenderIntegrationSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderIntegrationState.Observed,
				firstPipeline,
				PerfMeterRenderPipelineAssetSource.GraphicsSettings,
				73,
				0,
				true,
				9007199254740993UL,
				"Neutral Camera First",
				"Game",
				"neutral.integration.first",
				"Neutral Integration First",
				"1.0.0",
				PerfMeterRenderPassKind.RenderGraphRaster,
				"Neutral Pass First",
				"AfterRendering",
				3,
				"Deferred+",
				firstGrd,
				firstVrs,
				firstLegacyRender,
				"neutral-first-warning");

			PerfMeterRenderPipelineSnapshot laterPipeline = new PerfMeterRenderPipelineSnapshot(
				PerfMeterRenderPipelineKind.HighDefinition,
				"Later High Definition Asset",
				"LaterHighDefinitionPipelineAsset",
				"LaterHighDefinitionRuntime");
			PerfMeterGpuResidentDrawerContextSnapshot laterGrd = new PerfMeterGpuResidentDrawerContextSnapshot(
				PerfMeterAvailability.Unavailable,
				"Disabled",
				PerfMeterAvailability.Unavailable,
				false,
				PerfMeterAvailability.Unknown,
				false,
				"later GRD context");
			PerfMeterVariableRateShadingContextSnapshot laterVrs = new PerfMeterVariableRateShadingContextSnapshot(
				PerfMeterAvailability.Unavailable,
				false,
				false,
				false,
				0,
				0,
				string.Empty,
				PerfMeterAvailability.Unavailable,
				false,
				PerfMeterAvailability.Unavailable,
				false,
				"later VRS context");
			PerfMeterRenderGraphSnapshot laterLegacyRender = new PerfMeterRenderGraphSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderGraphState.Observed,
				99,
				"Legacy Camera Later",
				"Preview",
				9,
				10,
				8,
				11,
				12,
				13,
				"later-legacy-warning",
				PerfMeterRenderPipelineKind.HighDefinition,
				"Legacy Later Integration",
				"AfterRendering");
			PerfMeterRenderIntegrationSnapshot laterNeutralRender = new PerfMeterRenderIntegrationSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderIntegrationState.Observed,
				laterPipeline,
				PerfMeterRenderPipelineAssetSource.QualitySettings,
				100,
				0,
				true,
				42UL,
				"Neutral Camera Later",
				"Preview",
				"neutral.integration.later",
				"Neutral Integration Later",
				"9.9.9",
				PerfMeterRenderPassKind.CustomPass,
				"Neutral Pass Later",
				"BeforeRendering",
				8,
				"Forward",
				laterGrd,
				laterVrs,
				laterLegacyRender,
				"neutral-later-warning");

			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			coordinator.Start(
				new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1),
				new PerfMeterCaptureBundleOptions(includeScreenshot: false),
				CaptureStatus(captureId, PerfMeterCaptureState.Capturing));
			coordinator.RecordCaptureFrame(
				new PerfMeterSessionSampleSnapshot(10, 1d, "Scene", CreateMetrics(10), Array.Empty<PerfMeterCustomMetricSnapshot>(), CreatePlatformTelemetry()),
				PerformanceMeter.GetDeviceInfo(),
				default,
				firstLegacyRender,
				firstNeutralRender,
				PerformanceMeter.GetStatus());
			coordinator.ObserveCapture(
				CaptureStatus(captureId, PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				laterLegacyRender,
				laterNeutralRender,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			Assert.That(coordinator.GetStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(coordinator.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.Render.IntegrationName, Is.EqualTo("Legacy First Integration"));
			Assert.That(data.RenderIntegration.IntegrationId, Is.EqualTo("neutral.integration.first"));
			Assert.That(data.RenderIntegration.ObservedCameraEntityId, Is.EqualTo(9007199254740993UL));

			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/render-context-" + Guid.NewGuid().ToString("N");
			string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
			try
			{
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);
				Assert.That(result.Success, Is.True, result.Error);
				string context = File.ReadAllText(Path.Combine(fullPath, "context.json"));

				Assert.That(context, Does.Contain("\"render\":{\"availability\":\"Available\",\"state\":\"Observed\",\"pipeline\":\"Universal\",\"integration_name\":\"Legacy First Integration\",\"warning\":\"legacy-first-warning\"}"));
				Assert.That(context, Does.Contain("\"render_integration\":{\"is_available\":true,\"availability\":\"Available\",\"state\":\"Observed\""));
				Assert.That(context, Does.Contain("\"render_pipeline\":{\"kind\":\"Universal\",\"asset_name\":\"Synthetic Universal Asset\",\"asset_type_name\":\"SyntheticUniversalPipelineAsset\",\"runtime_type_name\":\"SyntheticUniversalRuntime\"}"));
				Assert.That(context, Does.Contain("\"render_pipeline_asset_source\":\"GraphicsSettings\",\"last_observed_frame\":73"));
				Assert.That(context, Does.Contain("\"observed_camera_entity_id\":\"9007199254740993\",\"observed_camera_name\":\"Neutral Camera First\",\"observed_camera_type\":\"Game\""));
				Assert.That(context, Does.Contain("\"integration_id\":\"neutral.integration.first\""));
				Assert.That(context, Does.Contain("\"pass_kind\":\"RenderGraphRaster\""));
				Assert.That(context, Does.Contain("\"perfmeter_pass_count\":3,\"effective_rendering_mode\":\"Deferred+\""));
				Assert.That(context, Does.Contain("\"gpu_resident_drawer\":{\"availability\":\"Available\",\"configured_mode\":\"Enabled\",\"is_configured\":true,\"support_availability\":\"Available\",\"is_supported\":true,\"activity_availability\":\"Unknown\""));
				Assert.That(context, Does.Contain("\"variable_rate_shading\":{\"availability\":\"Available\",\"supports_variable_rate_shading\":true,\"supports_per_draw_call\":true,\"supports_per_image_tile\":true,\"image_tile_width\":16,\"image_tile_height\":16,\"graphics_format\":\"R8G8B8A8_UNorm\",\"configuration_availability\":\"Unknown\",\"is_configured\":false,\"activity_availability\":\"Unknown\""));
				Assert.That(context, Does.Contain("\"legacy_render_graph\":{\"is_available\":true,\"availability\":\"Available\",\"state\":\"Observed\",\"last_frame\":72,\"observed_camera_name\":\"Legacy Camera First\",\"observed_camera_type\":\"Game\",\"render_pipeline\":\"Universal\",\"integration_name\":\"Legacy First Integration\",\"observed_injection_point\":\"BeforeRendering\",\"perfmeter_pass_count\":2,\"registered_pass_count\":4,\"merged_pass_count\":3"));
				Assert.That(context, Does.Not.Contain("neutral.integration.later"));
				Assert.That(context, Does.Not.Contain("Legacy Later Integration"));
				Assert.That(context, Does.Not.Contain("later-legacy-warning"));
			}
			finally
			{
				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}
			}
		}

		[Test]
		public void MemorySnapshotBundleStreamsOwnedArtifactAndRecordsProvenance()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string snapshotRoot = Path.Combine(projectRoot, PerfMeterMemorySnapshotStorage.RelativeSnapshotRoot);
			string sourcePath = Path.Combine(snapshotRoot, ".sgg-perfmeter-memory-" + Guid.NewGuid().ToString("N") + ".snap");
			string externalPath = Path.Combine(snapshotRoot, "unexpected-" + Guid.NewGuid().ToString("N") + ".rdc");
			string externalRelativePath = externalPath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/memory-" + Guid.NewGuid().ToString("N");
			string fullPath = Path.Combine(projectRoot, relativePath);
			byte[] snapshotBytes = { 1, 2, 3, 5, 8, 13 };

			try
			{
				Directory.CreateDirectory(snapshotRoot);
				File.WriteAllBytes(sourcePath, snapshotBytes);
				File.WriteAllBytes(externalPath, new byte[] { 1 });
				PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
				PerfMeterMemorySnapshotOptions options = new PerfMeterMemorySnapshotOptions("memory-bundle");
				PerfMeterMemorySnapshotStatusSnapshot completed = MemoryStatus(options, PerfMeterMemorySnapshotState.Completed, snapshotBytes.LongLength);
				coordinator.StartMemorySnapshot(options, completed, default, default);
				coordinator.ObserveMemorySnapshot(
					completed,
					new PerfMeterMemorySnapshotArtifact(completed, sourcePath),
					PerfMeterSessionSummarySnapshot.Empty,
					Array.Empty<PerfMeterSessionSampleSnapshot>(),
					PerformanceMeter.GetStatus(),
					PerformanceMeter.GetDeviceInfo(),
					default,
					PerfMeterRenderGraphSnapshot.NotObserved);

				Assert.That(coordinator.GetStatus("memory-bundle").State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
				Assert.That(coordinator.GetStatus("memory-bundle").MemorySnapshotState, Is.EqualTo(PerfMeterMemorySnapshotState.Completed));
				Assert.That(coordinator.TryGetExportData("memory-bundle", out PerfMeterCaptureBundleExportData data), Is.True);
				PerfMeterCaptureBundleExportResult rejectedExternal = PerfMeterCaptureBundleExporter.Export(data, relativePath, externalRelativePath, false);
				Assert.That(rejectedExternal.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
				Assert.That(rejectedExternal.Error, Is.EqualTo("external_artifact_not_supported_for_memory_snapshot"));
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);

				Assert.That(result.Success, Is.True, result.Error);
				Assert.That(File.ReadAllBytes(Path.Combine(fullPath, "memory-snapshot.snap")), Is.EqualTo(snapshotBytes));
				string manifest = File.ReadAllText(Path.Combine(fullPath, "manifest.json"));
				Assert.That(manifest, Does.Contain("\"requested_tool\":\"MemoryProfiler\""));
				Assert.That(manifest, Does.Contain("\"memory_snapshot_state\":\"Completed\""));
				Assert.That(manifest, Does.Contain("\"memory_snapshot_requested_flags\":\"ManagedObjects, NativeObjects\""));
				Assert.That(manifest, Does.Contain("\"contains_sensitive_memory\":true"));
				Assert.That(manifest, Does.Contain("Memory snapshot contains sensitive process memory."));
				Assert.That(manifest, Does.Contain(Sha256(snapshotBytes)));
				string metadata = File.ReadAllText(Path.Combine(fullPath, "memory-snapshot.json"));
				Assert.That(metadata, Does.Contain("\"backend_id\":\"fake.memory\""));
				Assert.That(metadata, Does.Contain("\"capture_flags_confirmed\":false"));
				Assert.That(metadata, Does.Contain("Memory snapshot contains sensitive process memory."));
				Assert.That(File.Exists(Path.Combine(fullPath, "external-capture.json")), Is.False);
				coordinator.MarkExported("memory-bundle", result.RelativePath, result.Bundle.ExternalArtifactState);
				Assert.That(coordinator.TryGetExportData("memory-bundle", out _), Is.False);
			}
			finally
			{
				if (File.Exists(sourcePath))
				{
					File.Delete(sourcePath);
				}

				if (File.Exists(externalPath))
				{
					File.Delete(externalPath);
				}

				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}
			}
		}

		[Test]
		public void ScreenshotRequestStaysPendingUntilExplicitCompletion()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("screenshot", includeScreenshot: true, completeScreenshot: false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.PendingScreenshot));
			Assert.That(coordinator.TryGetExportData("screenshot", out _), Is.False);
			Assert.That(coordinator.TryStartScreenshot(out string captureId, out string bundleId), Is.True);
			coordinator.CompleteScreenshot(captureId, bundleId, null, "batch mode", true);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
			Assert.That(coordinator.TryGetExportData("screenshot", out _), Is.True);
		}

		[Test]
		public void PendingScreenshotBecomesExportableWhenRuntimeStops()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("interrupted-screenshot", includeScreenshot: true, completeScreenshot: false);

			coordinator.CompletePendingScreenshotAsUnavailable(coordinator.GetStatus().BundleId, "runtime stopped");

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
			Assert.That(coordinator.GetStatus().Warning, Does.Contain("runtime stopped"));
			Assert.That(coordinator.TryGetExportData("interrupted-screenshot", out _), Is.True);
		}

		[Test]
		public void StaleScreenshotCompletionCannotMutateReplacementBundle()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("reused", includeScreenshot: true, completeScreenshot: false);
			Assert.That(coordinator.TryStartScreenshot(out string captureId, out string oldBundleId), Is.True);
			PerfMeterCaptureOptions replacement = new PerfMeterCaptureOptions("reused", PerfMeterCaptureTool.RenderDoc);
			coordinator.Start(replacement, new PerfMeterCaptureBundleOptions(includeScreenshot: true), CaptureStatus("reused", PerfMeterCaptureState.Capturing));
			coordinator.ObserveCapture(
				CaptureStatus("reused", PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			coordinator.CompleteScreenshot(captureId, oldBundleId, new byte[] { 1 }, string.Empty, false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.PendingScreenshot));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Pending));
		}

		[Test]
		public void PublicCaptureApiRejectsAdvertisedLimitViolationsBeforeStartingRuntime()
		{
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(" ", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(new string('x', 129), PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("bad\u0001id", PerfMeterCaptureTool.RenderDoc)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("too-many", PerfMeterCaptureTool.RenderDoc, 121, 0, 0)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("too-much-roll", PerfMeterCaptureTool.RenderDoc, 1, 601, 0)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void BundleExportCommitsAtomicallyAndRefusesExistingDestination()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("atomic", includeScreenshot: false);
			Assert.That(coordinator.TryGetExportData("atomic", out PerfMeterCaptureBundleExportData data), Is.True);
			string directoryName = "test-atomic-" + Guid.NewGuid().ToString("N");
			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/" + directoryName;
			string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

			try
			{
				PerfMeterCaptureBundleExportResult first = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);
				Assert.That(first.Success, Is.True, first.Error);
				Assert.That(first.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.Exported));
				Assert.That(File.Exists(Path.Combine(fullPath, "manifest.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, ".sgg-perfmeter-bundle")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "session.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "capture-samples.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "alerts.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "context.json")), Is.True);
				Assert.That(File.Exists(Path.Combine(fullPath, "external-capture.json")), Is.True);
				string manifest = File.ReadAllText(Path.Combine(fullPath, "manifest.json"));
				Assert.That(manifest, Does.Contain("\"schema\":\"sgg.perfmeter.capture-bundle\""));
				Assert.That(manifest, Does.Contain(Sha256(File.ReadAllBytes(Path.Combine(fullPath, "session.json")))));
				string captureSamples = File.ReadAllText(Path.Combine(fullPath, "capture-samples.json"));
				Assert.That(captureSamples, Does.Contain("\"provider_id\":\"capture.provider\""));
				Assert.That(captureSamples, Does.Contain("\"temperature_level\":0.7"));
				string context = File.ReadAllText(Path.Combine(fullPath, "context.json"));
				Assert.That(context, Does.Contain("\"configured_settings\""));
				Assert.That(context, Does.Contain("\"effective_settings\""));
				byte[] originalManifest = File.ReadAllBytes(Path.Combine(fullPath, "manifest.json"));

				PerfMeterCaptureBundleExportResult second = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);
				Assert.That(second.Success, Is.False);
				Assert.That(second.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.Conflict));
				Assert.That(File.ReadAllBytes(Path.Combine(fullPath, "manifest.json")), Is.EqualTo(originalManifest));
			}
			finally
			{
				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}
			}
		}

		[Test]
		public void BundleExportRejectsTraversalAndUnavailableAuthority()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("policy", includeScreenshot: false);
			coordinator.TryGetExportData("policy", out PerfMeterCaptureBundleExportData data);

			PerfMeterCaptureBundleExportResult traversal = PerfMeterCaptureBundleExporter.Export(data, "Temp/PerfMeter/CaptureBundles/../outside", null, false);
			PerfMeterCaptureBundleExportResult absolute = PerfMeterCaptureBundleExporter.Export(data, Path.GetFullPath(Path.Combine(Application.dataPath, "..", PerfMeterCaptureBundleExporter.RelativeBundleRoot, "absolute")), null, false);
			PerfMeterCaptureBundleExportResult malformed = PerfMeterCaptureBundleExporter.Export(data, "Temp/PerfMeter/CaptureBundles/bad\0path", null, false);
			PerfMeterCaptureBundleExportResult authority = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/authority-" + Guid.NewGuid().ToString("N"), null, true);

			Assert.That(traversal.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(absolute.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(malformed.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(authority.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.AuthorityRequired));
			Assert.That(authority.Error, Does.Contain("authoritative"));
		}

		[Test]
		public void BundleExportUsesSafeDefaultAndCopiesObservedArtifactWithoutClaimingAuthority()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("unsafe/name", includeScreenshot: false);
			coordinator.TryGetExportData("unsafe/name", out PerfMeterCaptureBundleExportData data);
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string sourceDirectory = Path.Combine(projectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "observed.rdc");
			string sourceRelativePath = sourcePath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
			string unknownDirectory = Path.Combine(projectRoot, PerfMeterCaptureBundleExporter.RelativeBundleRoot, "unknown-" + Guid.NewGuid().ToString("N"));
			byte[] artifactBytes = { 1, 3, 3, 7 };
			string bundlePath = string.Empty;

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				File.WriteAllBytes(sourcePath, artifactBytes);
				Directory.CreateDirectory(unknownDirectory);
				File.WriteAllText(Path.Combine(unknownDirectory, "manifest.json"), "{\"schema\":\"sgg.perfmeter.capture-bundle\",\"schema_version\":1,\"bundle_id\":\"fake\",\"files\":[]}");
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, null, sourceRelativePath, false);
				Assert.That(result.Success, Is.True, result.Error);
				Assert.That(result.RelativePath, Does.StartWith(PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/capture-"));
				Assert.That(result.RelativePath, Does.Not.Contain("unsafe"));
				Assert.That(result.Bundle.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
				bundlePath = Path.Combine(projectRoot, result.RelativePath);
				Assert.That(File.ReadAllBytes(Path.Combine(bundlePath, "external-capture.rdc")), Is.EqualTo(artifactBytes));
				string metadata = File.ReadAllText(Path.Combine(bundlePath, "external-capture.json"));
				Assert.That(metadata, Does.Contain("\"artifact_file\":\"external-capture.rdc\""));
				Assert.That(metadata, Does.Contain("\"association_verified\":false"));
				Assert.That(metadata, Does.Not.Contain(sourceDirectory));
				Assert.That(File.ReadAllText(Path.Combine(bundlePath, "manifest.json")), Does.Contain(Sha256(artifactBytes)));
				Assert.That(Directory.Exists(unknownDirectory), Is.True);
			}
			finally
			{
				if (!string.IsNullOrEmpty(bundlePath) && Directory.Exists(bundlePath))
				{
					Directory.Delete(bundlePath, true);
				}

				if (Directory.Exists(sourceDirectory))
				{
					Directory.Delete(sourceDirectory, true);
				}

				if (Directory.Exists(unknownDirectory))
				{
					Directory.Delete(unknownDirectory, true);
				}
			}
		}

		[Test]
		public void BundleExportRejectsOversizedArtifactBeforeAllocatingIt()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("oversized-artifact", includeScreenshot: false);
			coordinator.TryGetExportData("oversized-artifact", out PerfMeterCaptureBundleExportData data);
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string sourceDirectory = Path.Combine(projectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "oversized.rdc");
			string sourceRelativePath = sourcePath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				using (FileStream stream = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					stream.SetLength(PerfMeterCaptureBundleExporter.MaxBundleBytes + 1L);
				}

				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/oversized-" + Guid.NewGuid().ToString("N"), sourceRelativePath, false);
				Assert.That(result.Success, Is.False);
				Assert.That(result.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.QuotaExceeded));
			}
			finally
			{
				if (Directory.Exists(sourceDirectory))
				{
					Directory.Delete(sourceDirectory, true);
				}
			}
		}

		[Test]
		public void TerminalCaptureWithoutScreenshotDoesNotRemainPending()
		{
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			PerfMeterCaptureOptions options = new PerfMeterCaptureOptions("canceled", PerfMeterCaptureTool.RenderDoc);
			coordinator.Start(options, new PerfMeterCaptureBundleOptions(includeScreenshot: true), CaptureStatus("canceled", PerfMeterCaptureState.Capturing));

			coordinator.ObserveCapture(
				CaptureStatus("canceled", PerfMeterCaptureState.Canceled),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterCaptureBundleState.Canceled));
			Assert.That(coordinator.GetStatus().ScreenshotState, Is.EqualTo(PerfMeterCaptureScreenshotState.Unavailable));
		}

		[Test]
		public void CaptureMcpCommandsAreRegisteredAndCapabilitiesDoNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("perfmeter.capture.request"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.status"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.cancel"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.capabilities"));
			Assert.That(metadata, Does.Contain("require_authoritative_external_artifact"));
			Assert.That(metadata, Does.Not.Contain("require_authoritative_external_metadata"));
			Assert.That(metadata, Does.Contain("PerfMeterMcpCommands.CaptureRequest"));
			string capabilities = PerfMeterMcpCommands.CaptureCapabilities();
			string status = PerfMeterMcpCommands.CaptureStatus("{}");
			Assert.That(capabilities, Does.Contain("\"bundle_root\":\"Temp/PerfMeter/CaptureBundles\""));
			Assert.That(capabilities, Does.Contain("\"tool_identity\":\"unknown\""));
			Assert.That(capabilities, Does.Contain("\"tool_version\":\"unknown\""));
			Assert.That(status, Does.Contain("\"bundle\""));
			Assert.That(PerfMeterMcpCommands.CaptureStatus("{\"capture_id\":\"missing\"}"), Does.Contain("\"result\":\"not_found\""));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void AlertEngineKeepsBoundedCaptureEventHistory()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 1, 0f, PerfMeterAlertAction.Callback)
			});
			Action<PerfMeterAlertSnapshot> handler = _ => { };
			PerformanceMeter.AlertFired += handler;
			try
			{
				for (int i = 0; i < 300; i++)
				{
					engine.Evaluate(CreateMetrics(i), i, PerfMeterAlertClassification.Capture, "bounded");
				}

				PerfMeterAlertSnapshot[] events = engine.GetFiredCaptureEvents("bounded", out bool truncated);
				Assert.That(events, Has.Length.EqualTo(256));
				Assert.That(truncated, Is.True);
				Assert.That(events[0].CollectionFrame, Is.EqualTo(44));
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		[Test]
		public void ReusedCaptureIdDoesNotReusePriorAlertEvents()
		{
			PerfMeterAlertEngine engine = new PerfMeterAlertEngine(new[]
			{
				new PerfMeterRule("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 1, 0f, PerfMeterAlertAction.Callback)
			});
			Action<PerfMeterAlertSnapshot> handler = _ => { };
			PerformanceMeter.AlertFired += handler;
			try
			{
				engine.Evaluate(CreateMetrics(1), 1d, PerfMeterAlertClassification.Capture, "reused");
				engine.BeginCaptureEventCollection();
				engine.Evaluate(CreateMetrics(2), 2d, PerfMeterAlertClassification.Capture, "reused");

				PerfMeterAlertSnapshot[] events = engine.GetFiredCaptureEvents("reused", out bool truncated);
				Assert.That(events, Has.Length.EqualTo(1));
				Assert.That(events[0].CollectionFrame, Is.EqualTo(2));
				Assert.That(truncated, Is.False);
			}
			finally
			{
				PerformanceMeter.AlertFired -= handler;
			}
		}

		private static PerfMeterCaptureBundleCoordinator CreateReadyCoordinator(string captureId, bool includeScreenshot, bool completeScreenshot = true)
		{
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			PerfMeterCaptureOptions options = new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1);
			coordinator.Start(options, new PerfMeterCaptureBundleOptions(includeScreenshot), CaptureStatus(captureId, PerfMeterCaptureState.Capturing));
			PerfMeterSessionSampleSnapshot captureSample = new PerfMeterSessionSampleSnapshot(10, 1d, "Scene", CreateMetrics(10), Array.Empty<PerfMeterCustomMetricSnapshot>(), CreatePlatformTelemetry());
			coordinator.RecordCaptureFrame(captureSample, PerformanceMeter.GetDeviceInfo(), default, PerfMeterRenderGraphSnapshot.NotObserved, PerformanceMeter.GetStatus());
			PerfMeterSessionSampleSnapshot baseline = new PerfMeterSessionSampleSnapshot(9, 0.9d, "Scene", CreateMetrics(9));
			PerfMeterAlertSnapshot alert = new PerfMeterAlertSnapshot("capture.alert", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, 0d, 16d, 10, 1d, 1, true, "capture alert", PerfMeterAlertClassification.Capture, captureId);
			coordinator.ObserveCapture(
				CaptureStatus(captureId, PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				new[] { baseline },
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				new[] { alert },
				true);
			if (includeScreenshot && completeScreenshot)
			{
				coordinator.TryStartScreenshot(out string screenshotCaptureId, out string screenshotBundleId);
				coordinator.CompleteScreenshot(screenshotCaptureId, screenshotBundleId, null, "unavailable", true);
			}

			return coordinator;
		}

		private static PerfMeterPlatformTelemetrySnapshot CreatePlatformTelemetry()
		{
			return new PerfMeterPlatformTelemetrySnapshot(
				PerfMeterAvailability.Available,
				"capture.provider",
				"1.0",
				1d,
				1d,
				true,
				PerfMeterThermalWarningLevel.ThrottlingImminent,
				true,
				0.7f,
				true,
				0.2f,
				true,
				2,
				true,
				3,
				true,
				PerfMeterAdaptiveBottleneck.Gpu);
		}

		private static PerfMeterCaptureStatusSnapshot CaptureStatus(string captureId, PerfMeterCaptureState state)
		{
			return new PerfMeterCaptureStatusSnapshot(
				state == PerfMeterCaptureState.Error || state == PerfMeterCaptureState.Unavailable ? PerfMeterAvailability.Unavailable : PerfMeterAvailability.Available,
				state,
				captureId,
				PerfMeterCaptureTool.RenderDoc,
				0,
				1,
				0,
				0,
				state == PerfMeterCaptureState.Completed ? 1 : 0,
				0,
				string.Empty);
		}

		private static PerfMeterMemorySnapshotStatusSnapshot MemoryStatus(PerfMeterMemorySnapshotOptions options, PerfMeterMemorySnapshotState state, long sizeBytes)
		{
			return new PerfMeterMemorySnapshotStatusSnapshot(
				PerfMeterAvailability.Available,
				state,
				options.CaptureId,
				options.Trigger,
				options.CaptureFlags,
				"fake.memory",
				"1.0",
				1d,
				2d,
				sizeBytes,
				0d,
				string.Empty);
		}

		private static PerfMeterMetricsSnapshot CreateMetrics(int frame)
		{
			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				frame,
				PerfMeterBottleneck.Balanced,
				16.666d,
				false,
				16d,
				8d,
				4d,
				1d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				0L,
				0L,
				0L,
				0L,
				0d,
				PerfMeterOverdrawMeasurementState.Off,
				0f);
		}

		private static string Sha256(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(bytes);
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}
}
