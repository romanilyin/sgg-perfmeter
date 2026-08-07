using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterGraphicsStateCollectionTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterGraphicsStateCollectionBackendRegistry.ClearForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterGraphicsStateCollectionBackendRegistry.ClearForTests();
		}

		[Test]
		public void CapabilitiesReflectRegisteredBackendWithoutStartingRuntime()
		{
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot unavailable = PerfMeterGraphicsStateCollectionBackendRegistry.GetCapabilities();
			Assert.That(unavailable.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(unavailable.SupportsTrace, Is.False);
			Assert.That(unavailable.SupportsPrewarm, Is.False);
			Assert.That(unavailable.MaxTraceFrames, Is.EqualTo(600));
			Assert.That(unavailable.MaxArtifactBytes, Is.EqualTo(64L * 1024L * 1024L));
			Assert.That(unavailable.ArtifactRoot, Is.EqualTo("Temp/PerfMeter/GraphicsStateCollections"));

			FakeBackend backend = new FakeBackend { SupportsCacheMissTracing = false };
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot available = PerfMeterGraphicsStateCollectionBackendRegistry.GetCapabilities();
			Assert.That(available.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(available.SupportsTrace, Is.True);
			Assert.That(available.SupportsPrewarm, Is.True);
			Assert.That(available.SupportsCacheMissTracing, Is.False);
			Assert.That(available.BackendId, Is.EqualTo("fake.graphics"));
		}

		[Test]
		public void PublicReadsAndMcpMetadataDoNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			PerfMeterGraphicsDiagnosticsSnapshot diagnostics = PerformanceMeter.GetGraphicsDiagnostics();
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities = PerformanceMeter.GetGraphicsStateCollectionCapabilities();

			Assert.That(metadata, Does.Contain("perfmeter.graphics.diagnostics"));
			Assert.That(metadata, Does.Contain("perfmeter.graphics.state_collection.request"));
			Assert.That(metadata, Does.Contain("perfmeter.graphics.state_collection.status"));
			Assert.That(metadata, Does.Contain("perfmeter.graphics.state_collection.capabilities"));
			Assert.That(metadata, Does.Contain("perfmeter.graphics.state_collection.cancel"));
			Assert.That(metadata, Does.Contain("perfmeter.graphics.state_collection.prewarm"));
			Assert.That(diagnostics.Availability, Is.EqualTo(PerfMeterAvailability.Unknown));
			Assert.That(capabilities.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(PerfMeterMcpCommands.GraphicsDiagnostics(), Does.Contain("\"availability\":\"Unknown\""));
			string graphicsStateStatus = PerfMeterMcpCommands.GraphicsStateCollectionStatus();
			Assert.That(graphicsStateStatus, Does.Contain("\"state\":\"Idle\""));
			Assert.That(graphicsStateStatus, Does.Contain("\"is_busy\":false"));
			Assert.That(graphicsStateStatus, Does.Contain("\"has_pending_cleanup\":false"));
			Assert.That(PerfMeterMcpCommands.GraphicsStateCollectionCapabilities(), Does.Contain("\"max_trace_frames\":600"));
			Assert.That(PerfMeterMcpCommands.MetricsLatest(), Does.Contain("\"graphics_pipeline_creation_value\":0"));
			Assert.That(PerformanceMeter.RequestGraphicsStateTrace(default), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void SessionJsonPreservesGraphicsCreationValues()
		{
			PerfMeterProfilerMetricCapabilitySnapshot shaderCapability = new PerfMeterProfilerMetricCapabilitySnapshot(
				PerfMeterProfilerMetricSemantic.ShaderGpuProgramCreation,
				PerfMeterProfilerMetricSampleState.AvailableSampled,
				PerfMeterProfilerMetricResolution.Alias,
				"Render",
				"Shader.CompileGPUProgram",
				"Nanoseconds",
				"Int64",
				1,
				1);
			PerfMeterProfilerMetricCapabilitySnapshot pipelineCapability = new PerfMeterProfilerMetricCapabilitySnapshot(
				PerfMeterProfilerMetricSemantic.GraphicsPipelineCreation,
				PerfMeterProfilerMetricSampleState.AvailableSampled,
				PerfMeterProfilerMetricResolution.Exact,
				"Render",
				"CreatePSO.Job",
				"Nanoseconds",
				"Int64",
				1,
				1);
			PerfMeterMetricsSnapshot metrics = new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				10,
				PerfMeterBottleneck.Balanced,
				16.666d,
				false,
				0d,
				0d,
				0d,
				0d,
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
				overdrawState: PerfMeterOverdrawMeasurementState.Off,
				overdrawProgress: 0f,
				srpBatcherInstances: 0,
				frameSampleCount: 0,
				gpuValidSampleCount: 0,
				averageFps: 0d,
				onePercentLowFps: 0d,
				pointOnePercentLowFps: 0d,
				frameSpikeCount: 0,
				severeFrameSpikeCount: 0,
				shaderGpuProgramCreationValue: 123L,
				graphicsPipelineCreationValue: 456L,
				profilerMetricCatalogRevision: 3,
				shaderGpuProgramCreationCapability: shaderCapability,
				graphicsPipelineCreationCapability: pipelineCapability);
			PerfMeterSessionSampleSnapshot sample = new PerfMeterSessionSampleSnapshot(
				10,
				1d,
				"Graphics",
				metrics,
				Array.Empty<PerfMeterCustomMetricSnapshot>(),
				PerfMeterPlatformTelemetrySnapshot.Unavailable(),
				"trace-01");

			string json = PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, new[] { sample }, PerformanceMeter.GetStatus());
			string captureSamplesJson = PerfMeterSessionExporter.BuildCaptureSamplesJson("trace-01", new[] { sample });
			string csv = PerfMeterSessionExporter.BuildCsv(PerfMeterSessionSummarySnapshot.Empty, new[] { sample }, PerformanceMeter.GetStatus());

			Assert.That(json, Does.Contain("\"shader_gpu_program_creation_value\":123"));
			Assert.That(json, Does.Contain("\"graphics_pipeline_creation_value\":456"));
			Assert.That(json, Does.Contain("\"graphics_profiler_catalog_revision\":3"));
			Assert.That(json, Does.Contain("\"resolved_recorder_names\":\"Shader.CompileGPUProgram\""));
			Assert.That(json, Does.Contain("\"unit\":\"Nanoseconds\""));
			Assert.That(json, Does.Contain("\"resolved_component_count\":1"));
			Assert.That(json, Does.Contain("\"sampled_component_count\":1"));
			Assert.That(json, Does.Contain("\"graphics_state_trace_id\":\"trace-01\""));
			Assert.That(captureSamplesJson, Does.Contain("\"resolved_component_count\":1"));
			Assert.That(captureSamplesJson, Does.Contain("\"sampled_component_count\":1"));
			Assert.That(captureSamplesJson, Does.Contain("\"graphics_state_trace_id\":\"trace-01\""));
			Assert.That(csv, Does.Contain("graphics_profiler_catalog_revision"));
			Assert.That(csv, Does.Contain("\"trace-01\",3,123,\"AvailableSampled\",\"Alias\",\"Render\",\"Shader.CompileGPUProgram\",\"Nanoseconds\",\"Int64\",1,1,456,\"AvailableSampled\",\"Exact\""));
		}

		[Test]
		public void RealStorageRejectsTraversalAndAbsolutePrewarmPaths()
		{
			PerfMeterGraphicsStateCollectionStorage storage = new PerfMeterGraphicsStateCollectionStorage(Directory.GetCurrentDirectory());
			Assert.That(storage.TryResolvePrewarmInput("../outside.graphicsstate", 64L * 1024L * 1024L, out _, out _, out _), Is.False);
			Assert.That(storage.TryResolvePrewarmInput(Path.GetFullPath("outside.graphicsstate"), 64L * 1024L * 1024L, out _, out _, out _), Is.False);
			Assert.That(storage.TryResolvePrewarmInput("Temp/Other/outside.graphicsstate", 64L * 1024L * 1024L, out _, out _, out _), Is.False);
		}

		[Test]
		public void RealStoragePersistsPendingCleanupAcrossInstances()
		{
			string projectRoot = Path.Combine(Path.GetTempPath(), "sgg-perfmeter-gsc-" + Guid.NewGuid().ToString("N"));
			try
			{
				PerfMeterGraphicsStateCollectionStorage storage = new PerfMeterGraphicsStateCollectionStorage(projectRoot);
				Assert.That(storage.TryPrepare(0L, out string artifactPath, out _, out string error), Is.True, error);
				Assert.That(storage.TryMarkPendingCleanup(artifactPath), Is.True);

				PerfMeterGraphicsStateCollectionStorage replacement = new PerfMeterGraphicsStateCollectionStorage(projectRoot);
				Assert.That(replacement.GetPendingCleanupPaths(), Does.Contain(artifactPath));
				Assert.That(replacement.TryClearPendingCleanup(artifactPath), Is.True);
				Assert.That(replacement.GetPendingCleanupPaths(), Is.Empty);
			}
			finally
			{
				if (Directory.Exists(projectRoot))
				{
					Directory.Delete(projectRoot, true);
				}
			}
		}

		[Test]
		public void OptionsRejectInvalidTraceRequestsAndBoundPrewarmCount()
		{
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(default), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(new PerfMeterGraphicsStateTraceOptions("capture", 0)), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(new PerfMeterGraphicsStateTraceOptions("capture", 601)), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(new PerfMeterGraphicsStateTraceOptions("capture\n")), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(new PerfMeterGraphicsStateTraceOptions("capture", 1)), Is.True);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidPrewarmOptions(new PerfMeterGraphicsStatePrewarmOptions("artifact", -1)), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidPrewarmOptions(new PerfMeterGraphicsStatePrewarmOptions("artifact", 1000001)), Is.False);
			Assert.That(PerfMeterGraphicsStateCollectionCoordinator.IsValidPrewarmOptions(new PerfMeterGraphicsStatePrewarmOptions("../artifact")), Is.True);
		}

		[Test]
		public void RegistryIsIdempotentRejectsSecondBackendAndContainsIdentityExceptions()
		{
			FakeBackend backend = new FakeBackend();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			Assert.DoesNotThrow(() => PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend));
			Assert.Throws<InvalidOperationException>(() => PerfMeterGraphicsStateCollectionBackendRegistry.Register(new FakeBackend()));

			PerfMeterGraphicsStateCollectionBackendRegistry.ClearForTests();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(new FakeBackend { ThrowOnIdentity = true });
			Assert.That(PerfMeterGraphicsStateCollectionBackendRegistry.TryGet(out _, out _, out _, out _, out _, out string error), Is.False);
			Assert.That(error, Does.Contain("identity failed"));
		}

		[Test]
		public void TraceLifecycleCountsFramesAndPersistsRelativeArtifact()
		{
			FakeBackend backend = new FakeBackend
			{
				TraceResult = new PerfMeterGraphicsStateTraceBackendResult(true, 12, 4)
			};
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("capture", 2)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Tracing));
			coordinator.Tick();
			Assert.That(coordinator.GetStatus().CompletedTraceFrames, Is.EqualTo(1));
			coordinator.Tick();

			PerfMeterGraphicsStateCollectionStatusSnapshot status = coordinator.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Completed));
			Assert.That(status.CompletedTraceFrames, Is.EqualTo(2));
			Assert.That(status.ArtifactRelativePath, Is.EqualTo("artifact-1"));
			Assert.That(status.ArtifactSizeBytes, Is.EqualTo(42L));
			Assert.That(status.TotalGraphicsStateCount, Is.EqualTo(12));
			Assert.That(status.VariantCount, Is.EqualTo(4));
			Assert.That(backend.BeginCount, Is.EqualTo(1));
			Assert.That(backend.EndCount, Is.EqualTo(1));
		}

		[Test]
		public void TraceRejectsDuplicateAndOverlapDuringSingleFlight()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("one", 3)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("one", 3)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.AlreadyActive));
			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("two", 3)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap));
		}

		[Test]
		public void StaleGenerationCannotTickReplacementWithSameCaptureId()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("same-id", 2), out int firstGeneration), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.CancelTrace("same-id"), Is.True);
			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("same-id", 2), out int replacementGeneration), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));

			Assert.That(coordinator.IsActiveTrace("same-id", firstGeneration), Is.False);
			Assert.That(coordinator.IsActiveTrace("same-id", replacementGeneration), Is.True);
			coordinator.Tick(firstGeneration);
			Assert.That(coordinator.GetStatus().CompletedTraceFrames, Is.Zero);
			coordinator.Tick(replacementGeneration);
			Assert.That(coordinator.GetStatus().CompletedTraceFrames, Is.EqualTo(1));
		}

		[Test]
		public void IsBusyCoversPreparationAndPrewarmFlights()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			storage.OnPrepare = () => Assert.That(coordinator.IsBusy, Is.True);
			backend.OnPrewarm = () => Assert.That(coordinator.IsBusy, Is.True);
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("busy", 1)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.IsBusy, Is.True);
			coordinator.Tick();
			Assert.That(coordinator.IsBusy, Is.False);
			Assert.That(coordinator.Prewarm(new PerfMeterGraphicsStatePrewarmOptions(coordinator.GetStatus().ArtifactRelativePath)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Completed));
			Assert.That(coordinator.IsBusy, Is.False);
		}

		[Test]
		public void ShutdownDuringPreparationCannotResurrectTrace()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			storage.OnPrepare = coordinator.Shutdown;
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("prepare-shutdown")), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Failed));
			Assert.That(backend.BeginCount, Is.Zero);
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(coordinator.IsBusy, Is.False);
		}

		[Test]
		public void StaleEndAfterShutdownCannotCommitCompletedArtifact()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			backend.OnEnd = coordinator.Shutdown;
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("end-shutdown", 1));

			coordinator.Tick();

			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(coordinator.GetStatus().ArtifactRelativePath, Is.Empty);
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(backend.EndCount, Is.EqualTo(1));
		}

		[Test]
		public void StaleEndCannotCommitOverReplacementTrace()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			PerfMeterGraphicsStateCollectionRequestResult replacementResult = PerfMeterGraphicsStateCollectionRequestResult.Failed;
			backend.OnEnd = () =>
			{
				Assert.That(coordinator.IsBusy, Is.True);
				coordinator.Shutdown();
				replacementResult = coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("replacement", 1));
			};
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("original", 1));

			coordinator.Tick();

			Assert.That(replacementResult, Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Tracing));
			Assert.That(coordinator.GetStatus().CaptureId, Is.EqualTo("replacement"));
			Assert.That(storage.DeletedPaths, Does.Contain("artifact-1"));
			Assert.That(backend.EndCount, Is.EqualTo(1));
		}

		[Test]
		public void CancelTraceMatchesOnlyActiveOrPreparingCaptureAndCleansArtifact()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);

			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("cancel-me", 10));
			Assert.That(coordinator.CancelTrace("other"), Is.False);
			Assert.That(coordinator.CancelTrace("cancel-me"), Is.True);
			Assert.That(backend.CancelCount, Is.EqualTo(1));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Canceled));
			Assert.That(coordinator.IsBusy, Is.False);
			coordinator.Tick();
			Assert.That(backend.EndCount, Is.Zero);
		}

		[Test]
		public void CancelTraceDuringPreparationInvalidatesBegin()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			bool cancelResult = false;
			storage.OnPrepare = () => cancelResult = coordinator.CancelTrace("cancel-during-prepare");
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("cancel-during-prepare")), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Failed));
			Assert.That(cancelResult, Is.True);
			Assert.That(backend.BeginCount, Is.Zero);
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Canceled));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
		}

		[Test]
		public void CancelTraceCleanupFailureReportsErrorAndCanRetryOnNextRequest()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage { DeleteSucceeds = false };
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("cancel-failure", 2));

			Assert.That(coordinator.CancelTrace("cancel-failure"), Is.True);
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(coordinator.GetStatus().Warning, Does.Contain("could not be deleted"));
			Assert.That(coordinator.HasPendingCleanup, Is.True);
			Assert.That(coordinator.IsBusy, Is.True);
			Assert.That(coordinator.GetStatus().IsBusy, Is.True);
			Assert.That(coordinator.GetStatus().HasPendingCleanup, Is.True);
			Assert.That(storage.PendingCleanupPaths, Does.Contain(storage.PreparedPath));

			storage.DeleteSucceeds = true;
			Assert.That(coordinator.RetryPendingCleanup(), Is.True);
			Assert.That(coordinator.HasPendingCleanup, Is.False);
			Assert.That(coordinator.IsBusy, Is.False);
			Assert.That(coordinator.GetStatus().IsBusy, Is.False);
			Assert.That(coordinator.GetStatus().HasPendingCleanup, Is.False);
			Assert.That(storage.PendingCleanupPaths, Is.Empty);
			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("retry", 1)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Started));
			Assert.That(coordinator.IsBusy, Is.True);
		}

		[Test]
		public void PendingCleanupIsRestoredAfterCoordinatorReplacement()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage { DeleteSucceeds = false };
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("reload-cleanup", 2));
			coordinator.CancelTrace("reload-cleanup");

			PerfMeterGraphicsStateCollectionCoordinator replacement = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			Assert.That(replacement.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(replacement.GetStatus().HasPendingCleanup, Is.True);
			Assert.That(replacement.GetStatus().Warning, Does.Contain("restored after reload"));

			storage.DeleteSucceeds = true;
			Assert.That(replacement.RetryPendingCleanup(), Is.True);
			Assert.That(replacement.GetStatus().HasPendingCleanup, Is.False);
		}

		[Test]
		public void CancelTraceBackendFailureReportsErrorWarning()
		{
			FakeBackend backend = new FakeBackend { ThrowOnCancel = true };
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("cancel-backend", 2));

			Assert.That(coordinator.CancelTrace("cancel-backend"), Is.True);
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(coordinator.GetStatus().Warning, Does.Contain("cancellation failed"));
		}

		[Test]
		public void BeginFailureIsContainedAndCleansPendingArtifact()
		{
			FakeBackend backend = new FakeBackend { BeginSucceeds = false };
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("failed")), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Failed));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(backend.CancelCount, Is.EqualTo(1));
		}

		[Test]
		public void PrewarmUsesCapabilityAndPreservesArtifact()
		{
			FakeBackend backend = new FakeBackend
			{
				TraceResult = new PerfMeterGraphicsStateTraceBackendResult(true, 8, 2),
				PrewarmResult = new PerfMeterGraphicsStatePrewarmBackendResult(true, 8, 8, true)
			};
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("capture", 1));
			coordinator.Tick();

			PerfMeterGraphicsStateCollectionStatusSnapshot completed = coordinator.GetStatus();
			Assert.That(coordinator.Prewarm(new PerfMeterGraphicsStatePrewarmOptions(completed.ArtifactRelativePath)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Completed));
			PerfMeterGraphicsStateCollectionStatusSnapshot status = coordinator.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Prewarmed));
			Assert.That(status.CompletedWarmupCount, Is.EqualTo(8));
			Assert.That(status.TotalGraphicsStateCount, Is.EqualTo(8));
			Assert.That(status.ArtifactRelativePath, Is.EqualTo(completed.ArtifactRelativePath));
			Assert.That(status.ArtifactSizeBytes, Is.EqualTo(42L));
			Assert.That(status.IsWarmedUp, Is.True);
			Assert.That(storage.DeletedPaths, Does.Not.Contain(storage.PreparedPath));
		}

		[Test]
		public void PartialPrewarmReportsIncompleteEvidenceAndPreservesArtifact()
		{
			FakeBackend backend = new FakeBackend
			{
				PrewarmResult = new PerfMeterGraphicsStatePrewarmBackendResult(true, 3, 8, false)
			};
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("capture", 1));
			coordinator.Tick();
			string relativePath = coordinator.GetStatus().ArtifactRelativePath;

			Assert.That(coordinator.Prewarm(new PerfMeterGraphicsStatePrewarmOptions(relativePath, maxStateCount: 3)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Completed));
			PerfMeterGraphicsStateCollectionStatusSnapshot status = coordinator.GetStatus();
			Assert.That(status.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Prewarmed));
			Assert.That(status.IsWarmedUp, Is.False);
			Assert.That(status.Warning, Does.Contain("incomplete"));
			Assert.That(status.ArtifactRelativePath, Is.EqualTo(relativePath));
			Assert.That(storage.DeletedPaths, Does.Not.Contain(storage.PreparedPath));
		}

		[Test]
		public void UnsupportedCacheMissTracingDoesNotCallBackend()
		{
			FakeBackend backend = new FakeBackend { SupportsCacheMissTracing = false };
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);

			Assert.That(coordinator.Prewarm(new PerfMeterGraphicsStatePrewarmOptions("artifact", traceCacheMisses: true)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.Unavailable));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Unavailable));
			Assert.That(backend.PrewarmCount, Is.Zero);
		}

		[Test]
		public void ShutdownCancelsTraceAndCleansPendingArtifact()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("shutdown", 10));

			coordinator.Shutdown();

			Assert.That(backend.CancelCount, Is.EqualTo(1));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(coordinator.GetStatus().State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Error));
		}

		[Test]
		public void FailedSupersessionCleanupPreservesCompletedArtifactAndWarning()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
			PerfMeterGraphicsStateCollectionCoordinator coordinator = new PerfMeterGraphicsStateCollectionCoordinator(storage);
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("first", 1));
			coordinator.Tick();
			PerfMeterGraphicsStateCollectionStatusSnapshot before = coordinator.GetStatus();
			storage.DeleteSucceeds = false;

			Assert.That(coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("second", 1)), Is.EqualTo(PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap));
			PerfMeterGraphicsStateCollectionStatusSnapshot after = coordinator.GetStatus();
			Assert.That(after.State, Is.EqualTo(PerfMeterGraphicsStateCollectionState.Completed));
			Assert.That(after.CaptureId, Is.EqualTo(before.CaptureId));
			Assert.That(after.ArtifactRelativePath, Is.EqualTo(before.ArtifactRelativePath));
			Assert.That(after.Warning, Does.Contain("could not be deleted"));
			Assert.That(after.IsBusy, Is.True);
			Assert.That(after.HasPendingCleanup, Is.True);
			string warning = after.Warning;
			coordinator.RequestTrace(new PerfMeterGraphicsStateTraceOptions("second", 1));
			Assert.That(coordinator.GetStatus().Warning, Is.EqualTo(warning));
		}

		private sealed class FakeBackend : IPerfMeterGraphicsStateCollectionBackend
		{
			internal bool BeginSucceeds { get; set; } = true;
			internal bool EndSucceeds { get; set; } = true;
			internal bool PrewarmSucceeds { get; set; } = true;
			internal bool ThrowOnCancel { get; set; }
			public bool SupportsCacheMissTracing { get; internal set; } = true;
			internal bool ThrowOnIdentity { get; set; }
			internal PerfMeterGraphicsStateTraceBackendResult TraceResult { get; set; } = new PerfMeterGraphicsStateTraceBackendResult(true, 1, 1);
			internal PerfMeterGraphicsStatePrewarmBackendResult PrewarmResult { get; set; } = new PerfMeterGraphicsStatePrewarmBackendResult(true, 1, 1, true);
			internal int BeginCount { get; private set; }
			internal int EndCount { get; private set; }
			internal int CancelCount { get; private set; }
			internal int PrewarmCount { get; private set; }
			internal Action OnEnd { get; set; }
			internal Action OnPrewarm { get; set; }

			public string Id
			{
				get
				{
					if (ThrowOnIdentity)
					{
						throw new InvalidOperationException("identity getter failed");
					}

					return "fake.graphics";
				}
			}

			public string Version => "1.0";
			public bool SupportsParallelPsoCreation => true;

			public bool TryBeginTrace(out string error)
			{
				BeginCount++;
				error = BeginSucceeds ? string.Empty : "begin failed";
				return BeginSucceeds;
			}

			public bool TryEndTrace(string outputPath, out PerfMeterGraphicsStateTraceBackendResult result, out string error)
			{
				EndCount++;
				Action callback = OnEnd;
				OnEnd = null;
				callback?.Invoke();
				result = TraceResult;
				error = EndSucceeds ? string.Empty : "end failed";
				return EndSucceeds;
			}

			public void CancelTrace()
			{
				CancelCount++;
				if (ThrowOnCancel)
				{
					throw new InvalidOperationException("cancel failed");
				}
			}

			public bool TryPrewarm(string inputPath, int maxStateCount, bool traceCacheMisses, out PerfMeterGraphicsStatePrewarmBackendResult result, out string error)
			{
				PrewarmCount++;
				Action callback = OnPrewarm;
				OnPrewarm = null;
				callback?.Invoke();
				result = PrewarmResult;
				error = PrewarmSucceeds ? string.Empty : "prewarm failed";
				return PrewarmSucceeds;
			}
		}

		private sealed class FakeStorage : IPerfMeterGraphicsStateCollectionStorage
		{
			private int _preparedCount;
			internal bool DeleteSucceeds { get; set; } = true;
			internal bool ResolveSucceeds { get; set; } = true;
			internal Action OnPrepare { get; set; }
			internal List<string> DeletedPaths { get; } = new List<string>();
			internal List<string> PendingCleanupPaths { get; } = new List<string>();
			internal string PreparedPath { get; private set; } = string.Empty;
			public string RelativeRoot => "Temp/TestGraphicsStateCollections";

			public bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error)
			{
				PreparedPath = "artifact-" + (++_preparedCount);
				Action callback = OnPrepare;
				OnPrepare = null;
				callback?.Invoke();
				path = PreparedPath;
				availableFreeDiskBytes = long.MaxValue;
				error = string.Empty;
				return true;
			}

			public bool TryValidateCompleted(string path, string expectedPath, long maxBytes, out long sizeBytes, out string error)
			{
				sizeBytes = 42L;
				error = string.Empty;
				return string.Equals(path, expectedPath, StringComparison.Ordinal);
			}

			public bool TryResolvePrewarmInput(string relativePath, long maxBytes, out string path, out long sizeBytes, out string error)
			{
				path = relativePath;
				sizeBytes = 42L;
				error = ResolveSucceeds ? string.Empty : "input rejected";
				return ResolveSucceeds;
			}

			public bool TryDelete(string path)
			{
				DeletedPaths.Add(path);
				return DeleteSucceeds;
			}

			public string[] GetPendingCleanupPaths()
			{
				return PendingCleanupPaths.ToArray();
			}

			public bool TryMarkPendingCleanup(string path)
			{
				if (!PendingCleanupPaths.Contains(path))
				{
					PendingCleanupPaths.Add(path);
				}
				return true;
			}

			public bool TryClearPendingCleanup(string path)
			{
				PendingCleanupPaths.Remove(path);
				return true;
			}

			public string GetRelativePath(string path)
			{
				return path ?? string.Empty;
			}
		}
	}
}
