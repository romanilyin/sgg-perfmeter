using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
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
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
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
		public void ExternalArtifactObservationPreservesLifecycleAndRejectsMismatchedCapture()
		{
			const string captureId = "external-provenance";
			const string identity = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			coordinator.Start(
				new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1),
				new PerfMeterCaptureBundleOptions(includeScreenshot: false),
				CaptureStatus(captureId, PerfMeterCaptureState.Capturing));
			string bundleId = coordinator.GetStatus(captureId).BundleId;

			PerfMeterExternalArtifactSnapshot observed = new PerfMeterExternalArtifactOptions(
				artifactId: "external-artifact",
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: captureId,
				finalizationState: PerfMeterExternalArtifactFinalizationState.Observed)
				.WithSourceFileIdentitySha256(identity)
				.ToSnapshot();
			coordinator.ObserveExternalArtifact(captureId, bundleId, observed);

			PerfMeterCaptureBundleStatusSnapshot recording = coordinator.GetStatus(captureId);
			Assert.That(recording.State, Is.EqualTo(PerfMeterCaptureBundleState.Recording));
			Assert.That(recording.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
			Assert.That(recording.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));

			PerfMeterExternalArtifactSnapshot mismatched = new PerfMeterExternalArtifactOptions(
				artifactId: "mismatched-artifact",
				requestId: "other-capture",
				finalizationState: PerfMeterExternalArtifactFinalizationState.Finalized,
				sizeBytes: 99L)
				.WithSourceFileIdentitySha256(new string('f', 64))
				.ToSnapshot();
			coordinator.ObserveExternalArtifact("other-capture", bundleId, mismatched);
			Assert.That(coordinator.GetStatus(captureId).ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));

			coordinator.ObserveCapture(
				CaptureStatus(captureId, PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			PerfMeterExternalArtifactSnapshot authoritative = new PerfMeterExternalArtifactOptions(
				artifactId: "authoritative-artifact",
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: captureId,
				associationState: PerfMeterExternalArtifactAssociationState.BridgeAuthenticated,
				finalizationState: PerfMeterExternalArtifactFinalizationState.Finalized,
				authorityState: PerfMeterExternalArtifactAuthorityState.Authenticated,
				sizeBytes: 99L)
				.WithSourceFileIdentitySha256(identity)
				.ToSnapshot();
			coordinator.ObserveExternalArtifact(captureId, bundleId, authoritative);

			PerfMeterCaptureBundleStatusSnapshot completed = coordinator.GetStatus(captureId);
			Assert.That(completed.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(completed.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
			Assert.That(completed.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));
			Assert.That(completed.ExternalArtifact.IsAuthoritative, Is.False);

			PerfMeterExternalArtifactSnapshot late = new PerfMeterExternalArtifactOptions(
				artifactId: "late-artifact",
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: captureId,
				finalizationState: PerfMeterExternalArtifactFinalizationState.Finalized,
				sizeBytes: 101L)
				.WithSourceFileIdentitySha256(new string('f', 64))
				.ToSnapshot();
			Assert.That(coordinator.MarkExported(
				captureId,
				bundleId,
				string.Empty,
				"Temp/PerfMeter/CaptureBundles/exported",
				completed.ExternalArtifactState,
				completed.ExternalArtifact), Is.True);
			coordinator.ObserveExternalArtifact(captureId, bundleId, late);
			Assert.That(coordinator.GetStatus(captureId).ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));
		}

		[Test]
		public void ExternalArtifactProvenanceSurvivesStatusExportAndEnvelope()
		{
			const string captureId = "external-envelope";
			const string identity = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator(captureId, includeScreenshot: false);
			string bundleId = coordinator.GetStatus(captureId).BundleId;
			PerfMeterExternalArtifactSnapshot descriptor = new PerfMeterExternalArtifactOptions(
				artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
				requestId: captureId,
				finalizationState: PerfMeterExternalArtifactFinalizationState.Finalized)
				.WithSourceFileIdentitySha256(identity)
				.ToSnapshot();
			coordinator.ObserveExternalArtifact(captureId, bundleId, descriptor);

			PerfMeterCaptureBundleStatusSnapshot status = coordinator.GetStatus(captureId);
			Assert.That(status.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));
			Assert.That(status.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
			Assert.That(coordinator.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.Status.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));

			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/external-envelope-" + Guid.NewGuid().ToString("N");
			string fullPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relativePath);
			try
			{
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(data, relativePath, null, false);

				Assert.That(result.Success, Is.True, result.Error);
				Assert.That(result.Bundle.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
				Assert.That(result.Bundle.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));
				Assert.That(result.ExternalArtifact.SourceFileIdentitySha256, Is.EqualTo(identity));
				string envelope = File.ReadAllText(Path.Combine(fullPath, "external-artifact.json"));
				Assert.That(envelope, Does.Contain("\"source_file_identity_sha256\":\"" + identity + "\""));
				Assert.That(envelope, Does.Not.Contain("source_path"));
				Assert.That(envelope, Does.Not.Contain(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))));
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
		public void BundleExportFreezesSessionAndCaptureTimelinesDefensively()
		{
			const string captureId = "timeline-bundle";
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			PerfMeterCaptureStatusSnapshot capturing = CaptureStatus(captureId, PerfMeterCaptureState.Capturing);
			PerfMeterCaptureStatusSnapshot completed = CaptureStatus(captureId, PerfMeterCaptureState.Completed);
			coordinator.Start(new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1), new PerfMeterCaptureBundleOptions(false), capturing);
			string bundleId = coordinator.GetStatus(captureId).BundleId;
			coordinator.RecordCaptureBoundary(capturing, PerfMeterSessionTimelineCaptureBoundary.Begin, 10, 1d);
			coordinator.RecordCaptureFrame(
				new PerfMeterSessionSampleSnapshot(10, 1d, "Scene", CreateMetrics(10), Array.Empty<PerfMeterCustomMetricSnapshot>(), CreatePlatformTelemetry()),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				PerformanceMeter.GetStatus());
			coordinator.RecordCaptureBoundary(completed, PerfMeterSessionTimelineCaptureBoundary.End, 11, 1.1d);

			PerfMeterSessionTimelineStore sessionTimelineStore = new PerfMeterSessionTimelineStore();
			sessionTimelineStore.Start(1, 0);
			sessionTimelineStore.AddValidBaseline(9, 0.9d, 0);
			PerfMeterSessionTimelineSnapshot sessionTimeline = sessionTimelineStore.GetSnapshotCopy();
			coordinator.ObserveCapture(
				completed,
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				sessionTimeline,
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				PerfMeterRenderIntegrationSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			Assert.That(coordinator.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.SessionTimeline.Events, Has.Length.EqualTo(1));
			Assert.That(data.SessionTimeline.Events[0].Stream, Is.EqualTo(PerfMeterSessionTimelineStream.Baseline));
			Assert.That(data.CaptureTimeline.Events, Has.Length.EqualTo(3));
			Assert.That(data.CaptureTimeline.Events[0].CaptureId, Is.EqualTo(captureId));
			Assert.That(data.CaptureTimeline.Events[0].BundleId, Is.EqualTo(bundleId));
			Assert.That(data.CaptureTimeline.Events[1].CaptureSampleIndex, Is.EqualTo(0));

			data.CaptureTimeline.Events[0] = default;
			Assert.That(coordinator.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData second), Is.True);
			Assert.That(second.CaptureTimeline.Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.CaptureBoundary));
			string captureJson = PerfMeterSessionExporter.BuildCaptureSamplesJson(captureId, second.CaptureSamples, second.CaptureTimeline);
			Assert.That(captureJson, Does.Contain("\"timeline_schema_version\":1"));
			Assert.That(captureJson, Does.Contain("\"capture_boundary\":\"Begin\""));
		}

		[Test]
		public void BundleCaptureCopiesOnlyReportedCustomMetricCount()
		{
			const string captureId = "custom-buffer";
			PerfMeterCustomMetricSnapshot[] buffer = new PerfMeterCustomMetricSnapshot[2];
			buffer[0] = new PerfMeterCustomMetricSnapshot("reported.metric", "Reported", "tests", "count", 7d);
			buffer[1] = new PerfMeterCustomMetricSnapshot("stale.metric", "Stale", "tests", "count", 99d);
			PerfMeterCustomMetricCollection collection = new PerfMeterCustomMetricCollection(buffer, 1);
			PerfMeterCaptureBundleCoordinator coordinator = new PerfMeterCaptureBundleCoordinator();
			coordinator.Start(
				new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1),
				new PerfMeterCaptureBundleOptions(includeScreenshot: false),
				CaptureStatus(captureId, PerfMeterCaptureState.Capturing));

			coordinator.RecordCaptureFrame(
				10,
				1d,
				"Scene",
				CreateMetrics(10),
				collection,
				CreatePlatformTelemetry(),
				"trace",
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				PerfMeterRenderIntegrationSnapshot.NotObserved,
				PerformanceMeter.GetStatus());
			buffer[0] = new PerfMeterCustomMetricSnapshot("mutated.metric", "Mutated", "tests", "count", 100d);

			coordinator.ObserveCapture(
				CaptureStatus(captureId, PerfMeterCaptureState.Completed),
				PerfMeterSessionSummarySnapshot.Empty,
				Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerformanceMeter.GetDeviceInfo(),
				default,
				PerfMeterRenderGraphSnapshot.NotObserved,
				Array.Empty<PerfMeterAlertSnapshot>(),
				false);

			Assert.That(coordinator.TryGetExportData(captureId, out PerfMeterCaptureBundleExportData data), Is.True);
			Assert.That(data.CaptureSamples, Has.Length.EqualTo(1));
			Assert.That(data.CaptureSamples[0].CustomMetrics, Has.Length.EqualTo(1));
			Assert.That(data.CaptureSamples[0].CustomMetrics[0].Id, Is.EqualTo("reported.metric"));
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
				PerfMeterAvailability.Available,
				true,
				"synthetic GRD activity is sampled",
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				PerfMeterAvailability.Available,
				true,
				new PerfMeterGpuResidentDrawerEffectivenessSnapshot(
					PerfMeterAvailability.Available,
					73,
					11,
					22,
					new PerfMeterProfilerMetricCapabilitySnapshot(
						PerfMeterProfilerMetricSemantic.BrgDrawCalls,
						PerfMeterProfilerMetricSampleState.AvailableSampled,
						PerfMeterProfilerMetricResolution.Exact,
						"Render",
						"BRG Draw Calls Count",
						"Count",
						"Int64",
						1,
						1),
					new PerfMeterProfilerMetricCapabilitySnapshot(
						PerfMeterProfilerMetricSemantic.BrgInstances,
						PerfMeterProfilerMetricSampleState.AvailableSampled,
						PerfMeterProfilerMetricResolution.Alias,
						"Render",
						"BRG Instances Count",
						"Count",
						"Int64",
						1,
						1),
					string.Empty),
				PerfMeterGpuResidentDrawerReason.None);
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
			Assert.That(data.RenderIntegration.GpuResidentDrawer.Effectiveness.BrgDrawCalls, Is.EqualTo(11));
			Assert.That(data.RenderIntegration.GpuResidentDrawer.Effectiveness.BrgInstances, Is.EqualTo(22));
			Assert.That(data.RenderIntegration.GpuResidentDrawer.Effectiveness.BrgDrawCallsCapability.Resolution, Is.EqualTo(PerfMeterProfilerMetricResolution.Exact));
			Assert.That(data.RenderIntegration.GpuResidentDrawer.Effectiveness.BrgInstancesCapability.Resolution, Is.EqualTo(PerfMeterProfilerMetricResolution.Alias));
			Assert.That(data.RenderIntegration.GpuResidentDrawer.ActivitySource, Is.EqualTo(PerfMeterGpuResidentDrawerContextSnapshot.UnityRuntimeActivitySource));
			Assert.That(data.RenderIntegration.GpuResidentDrawer.DegradedReason, Is.EqualTo(PerfMeterGpuResidentDrawerReason.None));

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
				Assert.That(context, Does.Contain("\"gpu_resident_drawer\":{\"availability\":\"Available\",\"configured_mode\":\"Enabled\",\"is_configured\":true,\"support_availability\":\"Available\",\"is_supported\":true,\"activity_availability\":\"Available\""));
				Assert.That(context, Does.Contain("\"activity_source\":\"IGPUResidentRenderPipeline.IsGPUResidentDrawerEnabled\""));
				Assert.That(context, Does.Contain("\"degraded_reason\":\"None\""));
				Assert.That(context, Does.Contain("\"effectiveness\":{\"availability\":\"Available\",\"collection_frame\":73,\"scope\":\"brg_aggregate\",\"brg_draw_calls\":11,\"brg_instances\":22"));
				Assert.That(context, Does.Contain("\"brg_draw_calls_capability\":{\"sample_state\":\"AvailableSampled\",\"resolution\":\"Exact\""));
				Assert.That(context, Does.Contain("\"brg_instances_capability\":{\"sample_state\":\"AvailableSampled\",\"resolution\":\"Alias\""));
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
		public void PublicCaptureApiRejectsNativeBackendModesForPixButKeepsGenericPixValid()
		{
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(
				"pix-preferred",
				PerfMeterCaptureTool.Pix,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativePreferred)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions(
				"pix-required",
				PerfMeterCaptureTool.Pix,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired)), Is.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));

			try
			{
				Assert.That(PerformanceMeter.RequestCapture(new PerfMeterCaptureOptions("pix-generic", PerfMeterCaptureTool.Pix)), Is.Not.EqualTo(PerfMeterCaptureRequestResult.InvalidRequest));
			}
			finally
			{
				PerformanceMeter.Stop();
			}
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
			PerfMeterCaptureBundleExportResult reserved = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/.sgg-perfmeter-staging-user", null, false);
			PerfMeterCaptureBundleExportResult reservedMixedCase = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/.SGG-PERFMETER-STAGING-user", null, false);
			PerfMeterCaptureBundleExportResult authority = PerfMeterCaptureBundleExporter.Export(data, PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/authority-" + Guid.NewGuid().ToString("N"), null, true);

			Assert.That(traversal.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(absolute.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(malformed.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(reserved.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(reserved.Error, Is.EqualTo("path_uses_reserved_staging_name"));
			Assert.That(reservedMixedCase.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
			Assert.That(reservedMixedCase.Error, Is.EqualTo("path_uses_reserved_staging_name"));
			Assert.That(authority.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.AuthorityRequired));
			Assert.That(authority.Error, Does.Contain("authoritative"));
		}

		[Test]
		public void BundleExportRejectsEmptyObservedArtifact()
		{
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("empty-artifact", includeScreenshot: false);
			coordinator.TryGetExportData("empty-artifact", out PerfMeterCaptureBundleExportData data);
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string sourceDirectory = Path.Combine(projectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "empty.rdc");
			string sourceRelativePath = sourcePath.Substring(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				File.WriteAllBytes(sourcePath, Array.Empty<byte>());
				PerfMeterCaptureBundleExportResult result = PerfMeterCaptureBundleExporter.Export(
					data,
					PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/empty-" + Guid.NewGuid().ToString("N"),
					sourceRelativePath,
					false);

				Assert.That(result.Success, Is.False);
				Assert.That(result.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.PathRejected));
				Assert.That(result.Error, Is.EqualTo("external_artifact_is_empty"));
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
		public void PathRedactionHandlesCaseInsensitiveJsonEscapes()
		{
			PerfMeterCaptureBundleExportEnvironment environment = new PerfMeterCaptureBundleExportEnvironment(
				"C:\\Project",
				"C:\\Project\\Temp\\PerfMeter\\CaptureBundles",
				"C:\\Users\\Roman\\AppData",
				"C:\\Users\\Roman",
				true,
				"test");
			string escapedUserPath = environment.UserProfilePath.ToUpperInvariant().Replace("\\", "\\\\");
			string json = "{\"path\":\"" + escapedUserPath + "\\\\secret.txt\"}";

			string redacted = PerfMeterCaptureBundleExporter.RedactSensitivePaths(json, environment);

			Assert.That(redacted, Does.Not.Contain(escapedUserPath));
			Assert.That(redacted, Does.Contain("<user>\\\\secret.txt"));
		}

		[Test]
		public void MemorySnapshotCleanupWarningIsCombinedWithoutDuplication()
		{
			const string existingWarning = "existing-warning";
			string cleanupWarning = PerfMeterRuntime.MemorySnapshotCleanupWarning;
			PerfMeterCaptureBundleCoordinator coordinator = CreateReadyCoordinator("cleanup-warning", includeScreenshot: false);
			string bundleId = coordinator.GetStatus("cleanup-warning").BundleId;

			coordinator.AppendWarning("cleanup-warning", bundleId, existingWarning);
			coordinator.AppendWarning("cleanup-warning", bundleId, cleanupWarning);
			coordinator.AppendWarning("cleanup-warning", bundleId, cleanupWarning);

			PerfMeterCaptureBundleStatusSnapshot status = coordinator.GetStatus("cleanup-warning");
			Assert.That(status.Warning, Is.EqualTo(existingWarning + " " + cleanupWarning));
			Assert.That(status.Warning.IndexOf(cleanupWarning, StringComparison.Ordinal), Is.EqualTo(status.Warning.LastIndexOf(cleanupWarning, StringComparison.Ordinal)));
			Assert.That(PerfMeterCaptureBundleCoordinator.CombineWarnings(existingWarning, existingWarning), Is.EqualTo(existingWarning));
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
				string envelope = File.ReadAllText(Path.Combine(bundlePath, "external-artifact.json"));
				Assert.That(envelope, Does.Contain("\"schema\":\"sgg.perfmeter.external-artifact\""));
				Assert.That(envelope, Does.Contain("\"association_state\":\"Unverified\""));
				Assert.That(envelope, Does.Contain("\"contains_gpu_capture_data\":\"Unknown\""));
				Assert.That(envelope, Does.Contain("\"storage_mode\":\"Embed\""));
				Assert.That(envelope, Does.Contain("\"post_copy_sha256\":\"" + Sha256(artifactBytes) + "\""));
				Assert.That(result.ExternalArtifact.PostCopySha256, Is.EqualTo(Sha256(artifactBytes)));
				Assert.That(result.ExternalArtifact.IsAuthoritative, Is.False);
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
		public void AsyncExportIsSingleFlightAndFailedExportCanBeRetried()
		{
			PerfMeterCaptureBundleCoordinator bundleCoordinator = CreateReadyCoordinator("async-retry", includeScreenshot: false);
			Assert.That(bundleCoordinator.TryGetExportData("async-retry", out PerfMeterCaptureBundleExportData data), Is.True);
			PerfMeterCaptureBundleExportCoordinator coordinator = new PerfMeterCaptureBundleExportCoordinator();
			PerfMeterCaptureBundleExportEnvironment environment = PerfMeterCaptureBundleExporter.CaptureEnvironment();
			string exportId;
			PerfMeterCaptureBundleExportRequestResult request = coordinator.Request(
				data,
				"Temp/PerfMeter/CaptureBundles/../invalid",
				null,
				false,
				environment,
				null,
				out exportId);

			Assert.That(request, Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.Started));
			Assert.That(exportId, Is.Not.Empty);
			Assert.That(coordinator.Request(data, null, null, false, environment, null, out _), Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.AlreadyActive));
			PerfMeterCaptureBundleExportStatusSnapshot failed = WaitForExport(coordinator, exportId);
			Assert.That(failed.Phase, Is.EqualTo(PerfMeterCaptureBundleExportPhase.Failed));
			Assert.That(failed.CanRetry, Is.True);
			Assert.That(coordinator.TryConsumeCompletion(out _), Is.True);

			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/async-retry-" + Guid.NewGuid().ToString("N");
			string fullPath = Path.Combine(environment.ProjectRoot, relativePath);
			try
			{
				Assert.That(coordinator.Request(data, relativePath, null, false, environment, null, out string retryId), Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.Started));
				PerfMeterCaptureBundleExportStatusSnapshot completed = WaitForExport(coordinator, retryId);
				Assert.That(completed.Phase, Is.EqualTo(PerfMeterCaptureBundleExportPhase.Completed), completed.Error + " " + completed.Warning);
				Assert.That(completed.Success, Is.True);
				Assert.That(completed.Progress, Is.EqualTo(1f));
				Assert.That(File.Exists(Path.Combine(fullPath, "manifest.json")), Is.True);
				coordinator.AppendTerminalWarning(retryId, PerfMeterRuntime.MemorySnapshotCleanupWarning);
				coordinator.AppendTerminalWarning(retryId, PerfMeterRuntime.MemorySnapshotCleanupWarning);
				PerfMeterCaptureBundleExportStatusSnapshot warningStatus = coordinator.GetStatus(retryId);
				Assert.That(warningStatus.Error, Is.Empty);
				Assert.That(warningStatus.Warning, Is.EqualTo(PerfMeterRuntime.MemorySnapshotCleanupWarning));
				Assert.That(coordinator.TryConsumeCompletion(out _), Is.True);
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
		public void QueueRejectionFailsAsyncAndBlockingExportsWithoutWaiting()
		{
			PerfMeterCaptureBundleCoordinator bundleCoordinator = CreateReadyCoordinator("queue-rejected", includeScreenshot: false);
			Assert.That(bundleCoordinator.TryGetExportData("queue-rejected", out PerfMeterCaptureBundleExportData data), Is.True);
			PerfMeterCaptureBundleExportCoordinator coordinator = new PerfMeterCaptureBundleExportCoordinator((callback, state) => false);
			PerfMeterCaptureBundleExportEnvironment environment = PerfMeterCaptureBundleExporter.CaptureEnvironment();

			PerfMeterCaptureBundleExportRequestResult request = coordinator.Request(data, null, null, false, environment, null, out string exportId);
			PerfMeterCaptureBundleExportStatusSnapshot status = coordinator.GetStatus(exportId);
			Assert.That(request, Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.Failed));
			Assert.That(exportId, Is.Not.Empty);
			Assert.That(status.Phase, Is.EqualTo(PerfMeterCaptureBundleExportPhase.Failed));
			Assert.That(status.Error, Is.EqualTo("export_queue_rejected"));
			Assert.That(status.Warning, Is.Empty);
			Assert.That(coordinator.TryConsumeCompletion(out _), Is.True);

			PerfMeterCaptureBundleExportResult blocking = coordinator.ExportBlocking(data, null, null, false, environment);
			Assert.That(blocking.Success, Is.False);
			Assert.That(blocking.Status, Is.EqualTo(PerfMeterCaptureBundleExportStatus.IoError));
			Assert.That(blocking.Error, Is.EqualTo("export_queue_rejected"));
		}

		[Test]
		public void QueueExceptionRedactsProjectRootFromTerminalError()
		{
			PerfMeterCaptureBundleCoordinator bundleCoordinator = CreateReadyCoordinator("queue-throws", includeScreenshot: false);
			Assert.That(bundleCoordinator.TryGetExportData("queue-throws", out PerfMeterCaptureBundleExportData data), Is.True);
			PerfMeterCaptureBundleExportEnvironment environment = PerfMeterCaptureBundleExporter.CaptureEnvironment();
			PerfMeterCaptureBundleExportCoordinator coordinator = new PerfMeterCaptureBundleExportCoordinator((callback, state) =>
			{
				throw new IOException(environment.ProjectRoot + "/private/queue-failure");
			});

			PerfMeterCaptureBundleExportRequestResult request = coordinator.Request(data, null, null, false, environment, null, out string exportId);
			PerfMeterCaptureBundleExportStatusSnapshot status = coordinator.GetStatus(exportId);

			Assert.That(request, Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.Failed));
			Assert.That(status.Error, Does.Not.Contain(environment.ProjectRoot));
			Assert.That(status.Error, Does.Contain("<project>"));
			Assert.That(status.Warning, Is.Empty);
		}

		[Test]
		public void LegacyPublicCaptureBundleConstructorsRemainAvailable()
		{
			Type[] statusParameters =
			{
				typeof(PerfMeterAvailability),
				typeof(PerfMeterCaptureBundleState),
				typeof(string),
				typeof(string),
				typeof(PerfMeterCaptureState),
				typeof(PerfMeterCaptureTool),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(bool),
				typeof(PerfMeterCaptureScreenshotState),
				typeof(PerfMeterCaptureExternalArtifactState),
				typeof(string),
				typeof(string),
				typeof(PerfMeterMemorySnapshotState)
			};
			Type[] resultParameters =
			{
				typeof(bool),
				typeof(PerfMeterCaptureBundleExportStatus),
				typeof(string),
				typeof(string),
				typeof(PerfMeterCaptureBundleStatusSnapshot)
			};

			Assert.That(typeof(PerfMeterCaptureBundleStatusSnapshot).GetConstructor(statusParameters), Is.Not.Null);
			Assert.That(typeof(PerfMeterCaptureBundleExportResult).GetConstructor(resultParameters), Is.Not.Null);
		}

		[Test]
		public void AsyncExportCancellationLeavesBundleExportable()
		{
			PerfMeterCaptureBundleCoordinator bundleCoordinator = CreateReadyCoordinator("async-cancel", includeScreenshot: false);
			Assert.That(bundleCoordinator.TryGetExportData("async-cancel", out PerfMeterCaptureBundleExportData data), Is.True);
			PerfMeterCaptureBundleExportCoordinator coordinator = new PerfMeterCaptureBundleExportCoordinator();
			PerfMeterCaptureBundleExportEnvironment environment = PerfMeterCaptureBundleExporter.CaptureEnvironment();
			string relativePath = PerfMeterCaptureBundleExporter.RelativeBundleRoot + "/async-cancel-" + Guid.NewGuid().ToString("N");
			string fullPath = Path.Combine(environment.ProjectRoot, relativePath);
			string sourceDirectory = Path.Combine(environment.ProjectRoot, "Temp", "PerfMeter", "TestArtifacts", Guid.NewGuid().ToString("N"));
			string sourcePath = Path.Combine(sourceDirectory, "cancel.rdc");
			string sourceRelativePath = sourcePath.Substring(environment.ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(sourceDirectory);
				using (FileStream stream = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
				{
					stream.SetLength(8L * 1024L * 1024L);
				}

				Assert.That(coordinator.Request(data, relativePath, sourceRelativePath, false, environment, null, out string exportId), Is.EqualTo(PerfMeterCaptureBundleExportRequestResult.Started));
				Assert.That(coordinator.Cancel(exportId), Is.True);
				PerfMeterCaptureBundleExportStatusSnapshot status = WaitForExport(coordinator, exportId);
				Assert.That(status.Phase, Is.EqualTo(PerfMeterCaptureBundleExportPhase.Canceled));
				Assert.That(status.IsCanceled, Is.True);
				Assert.That(status.CanRetry, Is.True);
				Assert.That(Directory.Exists(fullPath), Is.False);
				Assert.That(coordinator.TryConsumeCompletion(out _), Is.True);
			}
			finally
			{
				if (Directory.Exists(fullPath))
				{
					Directory.Delete(fullPath, true);
				}

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
		public void RuntimeShutdownRetainsBundleWhenCaptureCleanupFails()
		{
			const string captureId = "shutdown-retains-bundle";
			PerformanceMeter.EnsureRunning();
			ShutdownCaptureBackend backend = new ShutdownCaptureBackend { EndSucceeds = false };
			PerfMeterRuntime.Instance.SetCaptureBackendForTests(backend);

			Assert.That(
				PerformanceMeter.RequestCapture(
					new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));

			PerformanceMeter.Stop();

			PerfMeterCaptureBundleStatusSnapshot status = PerformanceMeter.GetCaptureBundleStatus(captureId);
			Assert.That(status.State, Is.EqualTo(PerfMeterCaptureBundleState.Error));
			Assert.That(status.CaptureState, Is.EqualTo(PerfMeterCaptureState.Error));
			Assert.That(status.IsExportReady, Is.True);

			backend.EndSucceeds = true;
			PerformanceMeter.Stop();
		}

		[Test]
		public void RuntimeRetainsBundleAndLeaseUntilV3ArtifactCompletionIsFrozen()
		{
			const string captureId = "v3-runtime-finalization";
			PerformanceMeter.EnsureRunning();
			CompletionNativeCaptureBackend backend = new CompletionNativeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendV2ForTests(backend);

			Assert.That(
				PerformanceMeter.RequestCapture(
					new PerfMeterCaptureOptions(
						captureId,
						PerfMeterCaptureTool.RenderDoc,
						1,
						0,
						0,
						PerfMeterCaptureBackendMode.NativeRequired),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			PerfMeterProfilerLeaseStatusSnapshot lease = PerformanceMeter.GetProfilerLeaseStatus();

			PerfMeterRuntime.Instance.TickCaptureForTests();

			Assert.That(PerformanceMeter.GetCaptureStatus().State, Is.EqualTo(PerfMeterCaptureState.Completed));
			Assert.That(PerformanceMeter.GetCaptureBundleStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Recording));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(lease.LeaseId).IsHeld, Is.True);

			backend.FinalizationCanComplete = true;
			PerfMeterRuntime.Instance.TickCaptureForTests();

			PerfMeterCaptureBundleStatusSnapshot completed = PerformanceMeter.GetCaptureBundleStatus(captureId);
			Assert.That(completed.State, Is.EqualTo(PerfMeterCaptureBundleState.Ready));
			Assert.That(completed.ExternalArtifactState, Is.EqualTo(PerfMeterCaptureExternalArtifactState.FileObserved));
			Assert.That(completed.ExternalArtifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Finalized));
			Assert.That(completed.ExternalArtifact.RequestId, Is.EqualTo(captureId));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(lease.LeaseId).State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
			Assert.That(backend.TryConsumeExternalArtifact(out _), Is.False, "The runtime must consume the completion before freezing the bundle.");
		}

		[Test]
		public void RuntimeFreezesFailedV3ArtifactBeforeReleasingLease()
		{
			const string captureId = "v3-runtime-failed-finalization";
			PerformanceMeter.EnsureRunning();
			CompletionNativeCaptureBackend backend = new CompletionNativeCaptureBackend
			{
				FinalizationSucceeds = false
			};
			PerfMeterRuntime.Instance.SetCaptureBackendV2ForTests(backend);
			Assert.That(
				PerformanceMeter.RequestCapture(
					new PerfMeterCaptureOptions(
						captureId,
						PerfMeterCaptureTool.RenderDoc,
						1,
						0,
						0,
						PerfMeterCaptureBackendMode.NativeRequired),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));
			PerfMeterProfilerLeaseStatusSnapshot lease = PerformanceMeter.GetProfilerLeaseStatus();

			PerfMeterRuntime.Instance.TickCaptureForTests();
			backend.FinalizationCanComplete = true;
			PerfMeterRuntime.Instance.TickCaptureForTests();

			PerfMeterCaptureBundleStatusSnapshot failed = PerformanceMeter.GetCaptureBundleStatus(captureId);
			Assert.That(failed.State, Is.EqualTo(PerfMeterCaptureBundleState.Error));
			Assert.That(failed.ExternalArtifact.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Failed));
			Assert.That(failed.ExternalArtifact.Warning, Is.EqualTo("finalization failed"));
			Assert.That(PerformanceMeter.GetProfilerLeaseStatus(lease.LeaseId).State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
		}

		[Test]
		public void RuntimeShutdownDoesNotFinalizeNativeBundleWithPendingResources()
		{
			const string captureId = "shutdown-native-pending";
			PerformanceMeter.EnsureRunning();
			ShutdownNativeCaptureBackend backend = new ShutdownNativeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendV2ForTests(backend);

			Assert.That(
				PerformanceMeter.RequestCapture(
					new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));

			PerformanceMeter.Stop();

			Assert.That(PerformanceMeter.GetCaptureBundleStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Recording));
			Assert.That(backend.DiscardCount, Is.EqualTo(1));

			backend.CleanupCanComplete = true;
			Assert.That(PerfMeterRuntime.EnsureRunning(), Is.True);

			Assert.That(PerformanceMeter.GetCaptureBundleStatus(captureId).State, Is.EqualTo(PerfMeterCaptureBundleState.Canceled));
			Assert.That(backend.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void PostDestroyCleanupTerminalizesRetainedNativeBundleWithoutRediscard()
		{
			const string captureId = "destroy-native-pending";
			PerformanceMeter.EnsureRunning();
			ShutdownNativeCaptureBackend backend = new ShutdownNativeCaptureBackend();
			PerfMeterRuntime.Instance.SetCaptureBackendV2ForTests(backend);

			Assert.That(
				PerformanceMeter.RequestCapture(
					new PerfMeterCaptureOptions(captureId, PerfMeterCaptureTool.RenderDoc, 1, 0, 0, PerfMeterCaptureBackendMode.NativeRequired),
					new PerfMeterCaptureBundleOptions(includeScreenshot: false)),
				Is.EqualTo(PerfMeterCaptureRequestResult.Started));

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			typeof(PerfMeterRuntime).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
			typeof(PerfMeterRuntime).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
			UnityEngine.Object.DestroyImmediate(runtime.gameObject);

			bool runtimeDestroyed = PerfMeterRuntime.Instance == null;
			PerfMeterCaptureBundleState retainedState = PerformanceMeter.GetCaptureBundleStatus(captureId).State;
			int discardCountAfterDestroy = backend.DiscardCount;

			backend.CleanupCanComplete = true;
			bool restarted = PerfMeterRuntime.EnsureRunning();
			PerfMeterCaptureBundleState finalState = PerformanceMeter.GetCaptureBundleStatus(captureId).State;

			Assert.That(runtimeDestroyed, Is.True);
			Assert.That(retainedState, Is.EqualTo(PerfMeterCaptureBundleState.Recording));
			Assert.That(discardCountAfterDestroy, Is.EqualTo(1));
			Assert.That(restarted, Is.True);
			Assert.That(finalState, Is.EqualTo(PerfMeterCaptureBundleState.Canceled));
			Assert.That(backend.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void CaptureMcpCommandsAreRegisteredAndCapabilitiesDoNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();

			Assert.That(metadata, Does.Contain("perfmeter.capture.request"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.status"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.cancel"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export.request"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export.status"));
			Assert.That(metadata, Does.Contain("perfmeter.capture.export.cancel"));
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
			Assert.That(PerfMeterMcpCommands.CaptureExportStatus("{}"), Does.Contain("\"phase\":\"None\""));
			Assert.That(PerfMeterMcpCommands.CaptureExportStatus("{}"), Does.Contain("\"external_artifact\""));
			string missingExport = PerfMeterMcpCommands.CaptureExportStatus("{\"export_id\":\"missing-export\"}");
			Assert.That(missingExport, Does.Contain("\"requested_export_id\":\"missing-export\""));
			Assert.That(missingExport, Does.Contain("\"export_id\":\"\""));
			Assert.That(PerfMeterMcpCommands.CaptureStatus("{\"capture_id\":\"missing\"}"), Does.Contain("\"result\":\"not_found\""));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void CaptureMcpRequestDefaultsAndReportsBackendModeStatusFields()
		{
			string omitted = PerfMeterMcpCommands.CaptureRequest("{\"capture_id\":\"mcp-default\",\"tool\":\"RenderDoc\"}");
			Assert.That(omitted, Does.Contain("\"requested_backend_mode\":\"GenericUnity\""));
			Assert.That(omitted, Does.Contain("\"effective_backend_kind\":\"GenericUnity\""));
			Assert.That(omitted, Does.Contain("\"native_phase\":"));
			Assert.That(omitted, Does.Contain("\"native_result_code\":"));
			Assert.That(omitted, Does.Contain("\"fallback_reason\":"));

			string omittedStatus = PerfMeterMcpCommands.CaptureStatus("{\"capture_id\":\"mcp-default\"}");
			Assert.That(omittedStatus, Does.Contain("\"requested_backend_mode\":\"GenericUnity\""));
			Assert.That(omittedStatus, Does.Contain("\"effective_backend_kind\":\"GenericUnity\""));

			PerformanceMeter.Stop();

			string explicitMode = PerfMeterMcpCommands.CaptureRequest("{\"capture_id\":\"mcp-explicit\",\"tool\":\"RenderDoc\",\"backend_mode\":\"NativePreferred\"}");
			Assert.That(explicitMode, Does.Contain("\"requested_backend_mode\":\"NativePreferred\""));
			Assert.That(explicitMode, Does.Contain("\"effective_backend_kind\":\"GenericUnity\""));
			Assert.That(explicitMode, Does.Contain("\"native_phase\":"));
			Assert.That(explicitMode, Does.Contain("\"native_result_code\":"));
			Assert.That(explicitMode, Does.Contain("\"fallback_reason\":"));

			string explicitStatus = PerfMeterMcpCommands.CaptureStatus("{\"capture_id\":\"mcp-explicit\"}");
			Assert.That(explicitStatus, Does.Contain("\"requested_backend_mode\":\"NativePreferred\""));
			Assert.That(explicitStatus, Does.Contain("\"effective_backend_kind\":\"GenericUnity\""));
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

		private static PerfMeterCaptureBundleExportStatusSnapshot WaitForExport(
			PerfMeterCaptureBundleExportCoordinator coordinator,
			string exportId)
		{
			for (int i = 0; i < 500; i++)
			{
				PerfMeterCaptureBundleExportStatusSnapshot status = coordinator.GetStatus(exportId);
				if (status.IsTerminal)
				{
					return status;
				}

				Thread.Sleep(10);
			}

			return coordinator.GetStatus(exportId);
		}

		private sealed class ShutdownCaptureBackend : IPerfMeterCaptureBackend
		{
			internal bool EndSucceeds { get; set; }

			public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
			{
				return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
			}

			public bool TryBegin(PerfMeterCaptureTool tool, out string error)
			{
				error = string.Empty;
				return true;
			}

			public bool TryEnd(out string error)
			{
				error = EndSucceeds ? string.Empty : "shutdown cleanup failed";
				return EndSucceeds;
			}
		}

		private sealed class ShutdownNativeCaptureBackend : IPerfMeterCaptureBackendV2
		{
			private PerfMeterCaptureBackendV2Snapshot _snapshot;

			internal bool CleanupCanComplete { get; set; }
			internal int DiscardCount { get; private set; }
			internal int TickCount { get; private set; }

			public PerfMeterCaptureBackendV2Snapshot Snapshot => _snapshot;

			public PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options)
			{
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.None, false);
				return _snapshot;
			}

			public bool TryBegin(PerfMeterCaptureOptions options, out string error)
			{
				error = string.Empty;
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.BeginExecuted, true);
				return true;
			}

			public bool ScheduleEnd(out string error)
			{
				error = string.Empty;
				return false;
			}

			public bool TryDiscard(out string error)
			{
				DiscardCount++;
				error = CleanupCanComplete ? string.Empty : "native cleanup pending";
				_snapshot = NativeSnapshot(
					CleanupCanComplete ? PerfMeterRenderDocCapturePhase.Completed : PerfMeterRenderDocCapturePhase.FinalizingArtifact,
					!CleanupCanComplete);
				return true;
			}

			public void Tick()
			{
				TickCount++;
				if (CleanupCanComplete && _snapshot.HasPendingCompletion)
				{
					_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.Completed, false);
				}
			}

			private static PerfMeterCaptureBackendV2Snapshot NativeSnapshot(
				PerfMeterRenderDocCapturePhase phase,
				bool active)
			{
				return new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterCaptureBackendKind.RenderDocNative,
					phase,
					PerfMeterNativeCaptureResultCodes.Ok,
					string.Empty,
					true,
					active,
					active);
			}
		}

		private sealed class CompletionNativeCaptureBackend : IPerfMeterCaptureBackendV3
		{
			private PerfMeterCaptureBackendV2Snapshot _snapshot = NativeSnapshot(
				PerfMeterRenderDocCapturePhase.None,
				false,
				false);
			private string _captureId = string.Empty;
			private int _generation;
			private bool _completionAvailable;
			private PerfMeterCaptureExternalArtifactCompletion _completion;

			internal bool FinalizationCanComplete { get; set; }
			internal bool FinalizationSucceeds { get; set; } = true;

			public PerfMeterCaptureBackendV2Snapshot Snapshot => _snapshot;

			public PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options)
			{
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.None, false, false);
				return _snapshot;
			}

			public bool TryBegin(PerfMeterCaptureOptions options, out string error)
			{
				return TryBegin(options, 0, out error) == PerfMeterCaptureBackendBeginResult.Started;
			}

			public PerfMeterCaptureBackendBeginResult TryBegin(
				PerfMeterCaptureOptions options,
				int generation,
				out string error)
			{
				error = string.Empty;
				_captureId = options.CaptureId;
				_generation = generation;
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.BeginExecuted, false, true);
				return PerfMeterCaptureBackendBeginResult.Started;
			}

			public bool ScheduleEnd(out string error)
			{
				error = string.Empty;
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.AwaitingArtifact, true, true);
				return true;
			}

			public bool TryDiscard(out string error)
			{
				error = string.Empty;
				_completionAvailable = false;
				_snapshot = NativeSnapshot(PerfMeterRenderDocCapturePhase.Completed, false, false);
				return true;
			}

			public void Tick()
			{
				if (!FinalizationCanComplete || !_snapshot.HasPendingCompletion)
				{
					return;
				}

				PerfMeterExternalArtifactSnapshot artifact = new PerfMeterExternalArtifactOptions(
					artifactId: _captureId + "-renderdoc",
					artifactKind: PerfMeterExternalArtifactKind.GpuCapture,
					toolId: "renderdoc",
					requestId: _captureId,
					associationState: FinalizationSucceeds
						? PerfMeterExternalArtifactAssociationState.BridgeAuthenticated
						: PerfMeterExternalArtifactAssociationState.Unverified,
					finalizationState: FinalizationSucceeds
						? PerfMeterExternalArtifactFinalizationState.Finalized
						: PerfMeterExternalArtifactFinalizationState.Failed,
					authorityState: FinalizationSucceeds
						? PerfMeterExternalArtifactAuthorityState.Observed
						: PerfMeterExternalArtifactAuthorityState.Unknown,
					containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
					privacyFlags: PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
						PerfMeterExternalArtifactPrivacyFlags.Sensitive |
						PerfMeterExternalArtifactPrivacyFlags.RequiresReview,
					storageMode: PerfMeterExternalArtifactStorageMode.MetadataOnly,
					quotaBytes: PerfMeterRenderDocStoragePolicy.MaxPayloadBytes,
					sharePolicy: PerfMeterExternalArtifactSharePolicy.DoNotShare,
					sizeBytes: 4L,
					observedSourceSha256: FinalizationSucceeds ? new string('a', 64) : string.Empty,
					warning: FinalizationSucceeds ? string.Empty : "finalization failed")
					.WithSourceFileIdentitySha256(new string('b', 64))
					.ToSnapshot();
				_completion = new PerfMeterCaptureExternalArtifactCompletion(
					_captureId,
					_generation,
					artifact,
					string.Empty);
				_completionAvailable = true;
				_snapshot = NativeSnapshot(
					FinalizationSucceeds ? PerfMeterRenderDocCapturePhase.Completed : PerfMeterRenderDocCapturePhase.Failed,
					false,
					false);
			}

			public bool TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion)
			{
				completion = default;
				if (!_completionAvailable)
				{
					return false;
				}

				completion = _completion;
				_completion = default;
				_completionAvailable = false;
				return true;
			}

			private static PerfMeterCaptureBackendV2Snapshot NativeSnapshot(
				PerfMeterRenderDocCapturePhase phase,
				bool pending,
				bool active)
			{
				return new PerfMeterCaptureBackendV2Snapshot(
					PerfMeterAvailability.Available,
					string.Empty,
					PerfMeterCaptureBackendKind.RenderDocNative,
					phase,
					PerfMeterNativeCaptureResultCodes.Ok,
					string.Empty,
					false,
					pending,
					active);
			}
		}
	}
}
