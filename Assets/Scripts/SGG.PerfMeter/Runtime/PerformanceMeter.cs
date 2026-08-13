using UnityEngine;

namespace SGG.PerfMeter
{
	/// <summary>
	/// Public entry point for agent-readable performance meter state and latest metric snapshots.
	/// </summary>
	public static class PerformanceMeter
	{
		public static event System.Action<PerfMeterAlertSnapshot> AlertFired;

		public static void RegisterCustomMetricProvider(IPerfMeterCustomMetricProvider provider)
		{
			PerfMeterCustomMetricRegistry.Register(provider);
		}

		public static void UnregisterCustomMetricProvider(IPerfMeterCustomMetricProvider provider)
		{
			PerfMeterCustomMetricRegistry.Unregister(provider);
		}

		public static void ClearCustomMetricProviders()
		{
			PerfMeterCustomMetricRegistry.Clear();
		}

		public static PerfMeterCustomMetricSnapshot[] GetCustomMetrics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetLatestCustomMetrics() : PerfMeterCustomMetricRegistry.Copy(PerfMeterCustomMetricRegistry.Collect());
		}

		public static void RegisterPlatformTelemetryProvider(IPerfMeterPlatformTelemetryProvider provider)
		{
			PerfMeterPlatformTelemetryRegistry.Register(provider);
		}

		public static void UnregisterPlatformTelemetryProvider(IPerfMeterPlatformTelemetryProvider provider)
		{
			PerfMeterPlatformTelemetryRegistry.Unregister(provider);
		}

		public static PerfMeterPlatformTelemetrySnapshot GetPlatformTelemetry()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.LatestPlatformTelemetry : PerfMeterPlatformTelemetryRegistry.Sample(Time.realtimeSinceStartupAsDouble);
		}

		public static PerfMeterProfilerLeaseCapabilitiesSnapshot GetProfilerLeaseCapabilities()
		{
			return PerfMeterRuntime.GetProfilerLeaseCapabilities();
		}

		public static PerfMeterProfilerLeaseStatusSnapshot GetProfilerLeaseStatus(string leaseId = null)
		{
			return PerfMeterRuntime.GetProfilerLeaseStatus(leaseId);
		}

		public static PerfMeterProfilerLeaseAcquireResult TryAcquireProfilerLease(
			PerfMeterProfilerLeaseRequestOptions options,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			return PerfMeterRuntime.TryAcquireProfilerLease(options, out status);
		}

		public static PerfMeterProfilerLeaseReleaseResult ReleaseProfilerLease(
			string leaseId,
			string ownerId,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			return PerfMeterRuntime.ReleaseProfilerLease(leaseId, ownerId, out status);
		}

		public static PerfMeterProfilerLeaseReleaseResult ReleaseProfilerLease(string leaseId, string ownerId)
		{
			return ReleaseProfilerLease(leaseId, ownerId, out _);
		}

		public static void RegisterMemorySnapshotBackend(IPerfMeterMemorySnapshotBackend backend)
		{
			PerfMeterMemorySnapshotBackendRegistry.Register(backend);
		}

		public static void UnregisterMemorySnapshotBackend(IPerfMeterMemorySnapshotBackend backend)
		{
			PerfMeterMemorySnapshotBackendRegistry.Unregister(backend);
		}

		public static PerfMeterMemorySnapshotCapabilitiesSnapshot GetMemorySnapshotCapabilities()
		{
			bool available = PerfMeterMemorySnapshotBackendRegistry.TryGet(
				out _,
				out string backendId,
				out string backendVersion,
				out PerfMeterMemoryCaptureFlags supportedFlags,
				out string error);
			return new PerfMeterMemorySnapshotCapabilitiesSnapshot(
				available ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
				backendId,
				backendVersion,
				supportedFlags,
				PerfMeterMemorySnapshotCoordinator.MaxSnapshotBytes,
				PerfMeterMemorySnapshotStorage.RelativeSnapshotRoot,
				error);
		}

		public static PerfMeterMemorySnapshotStatusSnapshot GetMemorySnapshotStatus()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.MemorySnapshotStatus : PerfMeterMemorySnapshotStatusSnapshot.NotRunning;
		}

		public static PerfMeterMemorySnapshotRequestResult RequestMemorySnapshot(PerfMeterMemorySnapshotOptions options)
		{
			if (!PerfMeterMemorySnapshotCoordinator.IsValidOptions(options))
			{
				return PerfMeterMemorySnapshotRequestResult.InvalidRequest;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return PerfMeterMemorySnapshotRequestResult.Unavailable;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.RequestMemorySnapshot(options) : PerfMeterMemorySnapshotRequestResult.Unavailable;
		}

		public static bool ConfigureMemorySnapshotTriggers(PerfMeterMemorySnapshotTriggerOptions options)
		{
			if (!IsValidMemorySnapshotTriggerOptions(options) || !PerfMeterRuntime.EnsureRunning())
			{
				return false;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.ConfigureMemorySnapshotTriggers(options);
		}

		public static PerfMeterMemorySnapshotTriggerOptions GetMemorySnapshotTriggers()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.MemorySnapshotTriggers : PerfMeterMemorySnapshotTriggerOptions.Disabled;
		}

		public static void RegisterGraphicsStateCollectionBackend(IPerfMeterGraphicsStateCollectionBackend backend)
		{
			PerfMeterGraphicsStateCollectionBackendRegistry.Register(backend);
		}

		public static void UnregisterGraphicsStateCollectionBackend(IPerfMeterGraphicsStateCollectionBackend backend)
		{
			PerfMeterGraphicsStateCollectionBackendRegistry.Unregister(backend);
		}

		public static PerfMeterGraphicsStateCollectionCapabilitiesSnapshot GetGraphicsStateCollectionCapabilities()
		{
			return PerfMeterGraphicsStateCollectionBackendRegistry.GetCapabilities();
		}

		public static PerfMeterGraphicsStateCollectionStatusSnapshot GetGraphicsStateCollectionStatus()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GraphicsStateCollectionStatus : PerfMeterRuntime.PendingGraphicsStateCollectionStatus;
		}

		public static PerfMeterGraphicsStateCollectionRequestResult RequestGraphicsStateTrace(PerfMeterGraphicsStateTraceOptions options)
		{
			if (!PerfMeterGraphicsStateCollectionCoordinator.IsValidTraceOptions(options))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.RequestGraphicsStateTrace(options) : PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
		}

		public static PerfMeterGraphicsStateCollectionRequestResult PrewarmGraphicsStateCollection(PerfMeterGraphicsStatePrewarmOptions options)
		{
			if (!PerfMeterGraphicsStateCollectionCoordinator.IsValidPrewarmOptions(options))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.PrewarmGraphicsStateCollection(options) : PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
		}

		public static bool CancelGraphicsStateTrace(string captureId)
		{
			if (string.IsNullOrEmpty(captureId))
			{
				return false;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.CancelGraphicsStateTrace(captureId);
		}

		private static bool IsValidMemorySnapshotTriggerOptions(PerfMeterMemorySnapshotTriggerOptions options)
		{
			if (!options.Enabled)
			{
				return true;
			}

			return (options.SystemMemoryThresholdBytes > 0L || options.LeakGrowthThresholdBytes > 0L) &&
				options.CaptureFlags != PerfMeterMemoryCaptureFlags.None &&
				(options.CaptureFlags & ~PerfMeterMemorySnapshotCoordinator.AllCaptureFlags) == 0;
		}

		public static PerfMeterCpuCoreLoadSnapshot[] GetCpuCoreLoads()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetCpuCoreLoads() : System.Array.Empty<PerfMeterCpuCoreLoadSnapshot>();
		}

		public static PerfMeterStatusSnapshot GetStatus()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.Status : PerfMeterRuntime.CreateStoppedStatus();
		}

		public static bool TryGetStatus(out PerfMeterStatusSnapshot status)
		{
			status = GetStatus();
			return status.Availability != PerfMeterAvailability.Unknown;
		}

		public static PerfMeterMetricsSnapshot GetLatestMetrics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.LatestMetrics : PerfMeterMetricsSnapshot.Stopped;
		}

		public static PerfMeterDiagnosticsSnapshot GetDiagnostics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.Diagnostics : PerfMeterDiagnosticsSnapshot.NotRunning;
		}

		public static PerfMeterGraphicsDiagnosticsSnapshot GetGraphicsDiagnostics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GraphicsDiagnostics : PerfMeterGraphicsDiagnosticsSnapshot.NotRunning;
		}

		public static PerfMeterProfilerMetricCatalogSnapshot GetProfilerMetricCatalog()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.ProfilerMetricCatalog : PerfMeterProfilerMetricCatalogSnapshot.NotInitialized;
		}

		public static PerfMeterProfilerMetricCapabilitySnapshot[] GetProfilerMetricCapabilities()
		{
			return GetProfilerMetricCatalog().Capabilities;
		}

		public static bool TryRefreshProfilerMetricCatalog()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.RefreshProfilerMetricCatalog();
		}

		public static PerfMeterSelfOverheadSnapshot GetSelfOverhead()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.SelfOverhead : PerfMeterSelfOverheadSnapshot.NotInitialized;
		}

		public static PerfMeterSelfOverheadWindowSnapshot GetSelfOverheadWindow(PerfMeterSelfOverheadWindowKind kind, string identity = null)
		{
			if (kind != PerfMeterSelfOverheadWindowKind.Session && kind != PerfMeterSelfOverheadWindowKind.Capture)
			{
				return PerfMeterSelfOverheadWindowSnapshot.Unavailable;
			}

			return PerfMeterSelfObservability.GetBoundWindowSnapshot(kind, identity, Time.frameCount);
		}

		public static PerfMeterAlertSnapshot[] GetLatestAlerts()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetLatestAlerts() : System.Array.Empty<PerfMeterAlertSnapshot>();
		}

		public static PerfMeterAlertHistorySnapshot GetAlertHistory()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetAlertHistory() : default;
		}

		public static PerfMeterCaptureStatusSnapshot GetCaptureStatus()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.CaptureStatus : PerfMeterRuntime.PendingCaptureStatus;
		}

		public static PerfMeterCaptureRequestResult RequestCapture(PerfMeterCaptureOptions options)
		{
			if (!IsValidCaptureOptions(options))
			{
				return PerfMeterCaptureRequestResult.InvalidRequest;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return PerfMeterCaptureRequestResult.Unavailable;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.RequestCapture(options) : PerfMeterCaptureRequestResult.Unavailable;
		}

		public static PerfMeterCaptureRequestResult RequestCapture(PerfMeterCaptureOptions options, PerfMeterCaptureBundleOptions bundleOptions)
		{
			if (!IsValidCaptureOptions(options))
			{
				return PerfMeterCaptureRequestResult.InvalidRequest;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return PerfMeterCaptureRequestResult.Unavailable;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.RequestCapture(options, bundleOptions) : PerfMeterCaptureRequestResult.Unavailable;
		}

		public static PerfMeterCaptureBundleStatusSnapshot GetCaptureBundleStatus(string captureId = null)
		{
			return PerfMeterRuntime.CaptureBundleStatus(captureId);
		}

		public static PerfMeterCaptureBundleExportRequestResult RequestCaptureBundleExport(
			string captureId,
			out string exportId,
			string path = null,
			string externalArtifactPath = null,
			bool requireAuthoritativeExternalArtifact = false)
		{
			return PerfMeterRuntime.RequestCaptureBundleExport(captureId, path, externalArtifactPath, requireAuthoritativeExternalArtifact, out exportId);
		}

		public static PerfMeterCaptureBundleExportRequestResult RequestCaptureBundleExport(
			string captureId,
			string path,
			string externalArtifactPath,
			bool requireAuthoritativeExternalArtifact,
			out string exportId)
		{
			return PerfMeterRuntime.RequestCaptureBundleExport(captureId, path, externalArtifactPath, requireAuthoritativeExternalArtifact, out exportId);
		}

		public static PerfMeterCaptureBundleExportStatusSnapshot GetCaptureBundleExportStatus(string exportId = null)
		{
			return PerfMeterRuntime.CaptureBundleExportStatus(exportId);
		}

		public static bool CancelCaptureBundleExport(string exportId)
		{
			return PerfMeterRuntime.CancelCaptureBundleExport(exportId);
		}

		public static PerfMeterCaptureCapabilitiesSnapshot GetCaptureCapabilities()
		{
			return PerfMeterRuntime.CaptureCapabilities;
		}

		public static PerfMeterCaptureBundleExportResult ExportCaptureBundle(
			string captureId,
			string path = null,
			string externalArtifactPath = null,
			bool requireAuthoritativeExternalArtifact = false)
		{
			if (string.IsNullOrEmpty(captureId))
			{
				return new PerfMeterCaptureBundleExportResult(false, PerfMeterCaptureBundleExportStatus.NotFound, string.Empty, "capture_id_required", PerfMeterCaptureBundleStatusSnapshot.None);
			}

			return PerfMeterRuntime.ExportCaptureBundle(captureId, path, externalArtifactPath, requireAuthoritativeExternalArtifact);
		}

		private static bool IsValidCaptureOptions(PerfMeterCaptureOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.CaptureId) ||
				options.CaptureId.Length > 128 ||
				(options.Tool != PerfMeterCaptureTool.RenderDoc && options.Tool != PerfMeterCaptureTool.Pix) ||
				!options.IsValidBackendMode ||
				(options.Tool != PerfMeterCaptureTool.RenderDoc && options.BackendMode != PerfMeterCaptureBackendMode.GenericUnity) ||
				options.CaptureFrames > 120 ||
				options.PreRollFrames > 600 ||
				options.PostRollFrames > 600)
			{
				return false;
			}

			for (int i = 0; i < options.CaptureId.Length; i++)
			{
				if (char.IsControl(options.CaptureId[i]))
				{
					return false;
				}
			}

			return true;
		}

		public static bool CancelCapture(string captureId = null)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterCaptureStatusSnapshot status = runtime != null ? runtime.CaptureStatus : PerfMeterRuntime.PendingCaptureStatus;
			string effectiveCaptureId = string.IsNullOrEmpty(captureId) ? status.CaptureId : captureId;
			if (string.IsNullOrEmpty(effectiveCaptureId))
			{
				return false;
			}

			return runtime != null ? runtime.CancelCapture(effectiveCaptureId) : PerfMeterRuntime.CancelPendingCapture(effectiveCaptureId);
		}

		public static string ActiveAlertCaptureId
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.ActiveAlertCaptureId : PerfMeterRuntime.PendingAlertCaptureId;
			}
		}

		public static bool BeginAlertCapture(string captureId)
		{
			if (string.IsNullOrEmpty(captureId))
			{
				throw new System.ArgumentException("Capture id must not be empty.", nameof(captureId));
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return false;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.BeginAlertCapture(captureId);
		}

		public static bool EndAlertCapture(string captureId)
		{
			if (string.IsNullOrEmpty(captureId))
			{
				throw new System.ArgumentException("Capture id must not be empty.", nameof(captureId));
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.EndAlertCapture(captureId);
		}

		public static void ClearAlerts()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.ClearAlerts();
			}
		}

		public static bool TryGetLatestMetrics(out PerfMeterMetricsSnapshot metrics)
		{
			metrics = GetLatestMetrics();
			return metrics.Availability != PerfMeterAvailability.Unknown;
		}

		public static PerfMeterDeviceSnapshot GetDeviceInfo()
		{
			return PerfMeterDeviceInfoProvider.CreateSnapshot();
		}

		public static bool TryGetDeviceInfo(out PerfMeterDeviceSnapshot deviceInfo)
		{
			deviceInfo = GetDeviceInfo();
			return !string.IsNullOrEmpty(deviceInfo.UnityVersion);
		}

		public static PerfMeterCameraSnapshot GetCameraSnapshot(PerfMeterCameraSource source = PerfMeterCameraSource.Auto, string cameraNameFilter = null)
		{
			return PerfMeterCameraSnapshotProvider.CreateSnapshot(source, cameraNameFilter);
		}

		public static bool TryGetCameraSnapshot(out PerfMeterCameraSnapshot cameraSnapshot, PerfMeterCameraSource source = PerfMeterCameraSource.Auto, string cameraNameFilter = null)
		{
			cameraSnapshot = GetCameraSnapshot(source, cameraNameFilter);
			return cameraSnapshot.IsAvailable;
		}

		public static PerfMeterRenderGraphSnapshot GetRenderGraphSnapshot()
		{
			return PerfMeterRenderGraphAnalytics.GetSnapshot();
		}

		public static bool TryGetRenderGraphSnapshot(out PerfMeterRenderGraphSnapshot renderGraphSnapshot)
		{
			renderGraphSnapshot = GetRenderGraphSnapshot();
			return renderGraphSnapshot.IsAvailable;
		}

		public static PerfMeterRenderIntegrationSnapshot GetRenderIntegrationSnapshot()
		{
			return PerfMeterRenderGraphAnalytics.GetRenderIntegrationSnapshot();
		}

		public static bool TryGetRenderIntegrationSnapshot(out PerfMeterRenderIntegrationSnapshot renderIntegrationSnapshot)
		{
			renderIntegrationSnapshot = GetRenderIntegrationSnapshot();
			return renderIntegrationSnapshot.IsAvailable;
		}

		public static PerfMeterSettingsSnapshot GetSettings()
		{
			return PerfMeterSettingsStore.LoadFromResources();
		}

		public static bool TryApplySettingsJson(string json, out string warning)
		{
			if (!PerfMeterSettingsStore.TryReadSnapshot(json, out PerfMeterSettingsSnapshot settings))
			{
				warning = settings.Warning;
				return false;
			}

			warning = settings.Warning;
			if (!PerfMeterSettingsStore.ApplySnapshotToRuntime(settings))
			{
				warning = string.IsNullOrEmpty(warning)
					? "PerfMeter settings were valid but could not be applied to the runtime."
					: warning + " PerfMeter settings could not be applied to the runtime.";
				return false;
			}

			PerfMeterSettingsBootstrap.MarkExplicitSettingsApplied();
			return true;
		}

		public static void EnsureRunning()
		{
			TryEnsureRunning();
		}

		public static void Stop()
		{
			TryStop();
		}

		public static PerfMeterMutationResultSnapshot TryEnsureRunning()
		{
			bool wasRunning = PerfMeterRuntime.Instance != null && PerfMeterRuntime.Instance.AcceptsMutations;
			if (!PerfMeterRuntime.EnsureRunning() || PerfMeterRuntime.Instance == null || !PerfMeterRuntime.Instance.AcceptsMutations)
			{
				return RuntimeUnavailableMutation(PerfMeterRuntimeState.Running, GetStatus().State);
			}

			return MutationResult(
				wasRunning ? PerfMeterMutationStatus.NoChange : PerfMeterMutationStatus.Applied,
				wasRunning ? PerfMeterMutationReason.AlreadyInRequestedState : PerfMeterMutationReason.None,
				PerfMeterRuntimeState.Running,
				GetStatus().State);
		}

		public static PerfMeterMutationResultSnapshot TryStop()
		{
			bool hadRuntime = PerfMeterRuntime.Instance != null;
			PerfMeterRuntime.StopRunning();
			if (PerfMeterRuntime.Instance != null)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.PendingCleanup, PerfMeterRuntimeState.Stopped, GetStatus().State);
			}

			return MutationResult(
				hadRuntime ? PerfMeterMutationStatus.Applied : PerfMeterMutationStatus.NoChange,
				hadRuntime ? PerfMeterMutationReason.None : PerfMeterMutationReason.AlreadyInRequestedState,
				PerfMeterRuntimeState.Stopped,
				PerfMeterRuntimeState.Stopped);
		}

		public static void ResetStats()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.ResetStats();
			}
		}

		public static PerfMeterCollectionMode CollectionMode
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.CollectionMode : PerfMeterCollectionMode.Stopped;
			}
		}

		public static void SetCollectionMode(PerfMeterCollectionMode mode)
		{
			TrySetCollectionMode(mode);
		}

		public static PerfMeterMutationResultSnapshot TrySetCollectionMode(PerfMeterCollectionMode mode)
		{
			if (mode == PerfMeterCollectionMode.Stopped)
			{
				return TryStop();
			}

			PerfMeterCollectionMode normalizedMode = NormalizeCollectionMode(mode);
			if (!TryGetMutableRuntime(out PerfMeterRuntime runtime))
			{
				return RuntimeUnavailableMutation(mode, CollectionMode);
			}

			PerfMeterCollectionMode previousMode = runtime.CollectionMode;
			runtime.SetCollectionMode(normalizedMode);
			PerfMeterCollectionMode effectiveMode = runtime.CollectionMode;
			if (effectiveMode != normalizedMode)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, mode, effectiveMode);
			}
			if (normalizedMode != mode)
			{
				return MutationResult(PerfMeterMutationStatus.Normalized, PerfMeterMutationReason.ValueNormalized, mode, effectiveMode);
			}
			if (normalizedMode == PerfMeterCollectionMode.OverdrawDiagnostic && PerfMeterRuntime.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported)
			{
				return MutationResult(PerfMeterMutationStatus.Unsupported, PerfMeterMutationReason.UnsupportedRenderPipeline, mode, effectiveMode);
			}

			return MutationResult(
				previousMode == effectiveMode ? PerfMeterMutationStatus.NoChange : PerfMeterMutationStatus.Applied,
				previousMode == effectiveMode ? PerfMeterMutationReason.AlreadyInRequestedState : PerfMeterMutationReason.None,
				mode,
				effectiveMode);
		}

		public static bool IsSessionRecording
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && runtime.IsSessionRecording;
			}
		}

		public static void StartSession()
		{
			StartSession(PerfMeterSessionOptions.FromSettings(GetOperationSettings()));
		}

		public static void StartSession(PerfMeterSessionOptions options)
		{
			TryStartSession(options);
		}

		public static PerfMeterMutationResultSnapshot TryStartSession(PerfMeterSessionOptions options)
		{
			if (!TryGetMutableRuntime(out PerfMeterRuntime runtime))
			{
				return RuntimeUnavailableMutation(PerfMeterSessionState.Recording, GetSessionSummary().State);
			}

			runtime.StartSession(options);
			PerfMeterSessionState effectiveState = runtime.GetSessionSummary().State;
			return effectiveState == PerfMeterSessionState.Recording
				? MutationResult(PerfMeterMutationStatus.Applied, PerfMeterMutationReason.None, PerfMeterSessionState.Recording, effectiveState)
				: MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, PerfMeterSessionState.Recording, effectiveState);
		}

		public static void StopSession()
		{
			TryStopSession();
		}

		public static PerfMeterMutationResultSnapshot TryStopSession()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime == null)
			{
				return MutationResult(PerfMeterMutationStatus.NoChange, PerfMeterMutationReason.NoActiveOperation, PerfMeterSessionState.Stopped, PerfMeterSessionState.Idle);
			}
			if (!runtime.AcceptsMutations)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, PerfMeterSessionState.Stopped, runtime.GetSessionSummary().State);
			}

			PerfMeterSessionState previousState = runtime.GetSessionSummary().State;
			runtime.StopSession();
			PerfMeterSessionState effectiveState = runtime.GetSessionSummary().State;
			return MutationResult(
				previousState == PerfMeterSessionState.Recording && effectiveState == PerfMeterSessionState.Stopped ? PerfMeterMutationStatus.Applied : PerfMeterMutationStatus.NoChange,
				previousState == PerfMeterSessionState.Recording ? PerfMeterMutationReason.None : PerfMeterMutationReason.NoActiveOperation,
				PerfMeterSessionState.Stopped,
				effectiveState);
		}

		public static PerfMeterSessionSummarySnapshot GetSessionSummary()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
		}

		public static PerfMeterSessionSampleSnapshot[] GetSessionSamples()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
		}

		public static PerfMeterSessionTimelineSnapshot GetSessionTimeline()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.GetSessionTimeline() : PerfMeterSessionTimelineSnapshot.Empty;
		}

		public static bool ExportSessionJson(string path)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterSessionSummarySnapshot summary = runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
			PerfMeterSessionSampleSnapshot[] samples = runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			PerfMeterSessionTimelineSnapshot timeline = runtime != null ? runtime.GetSessionTimeline() : PerfMeterSessionTimelineSnapshot.Empty;
			PerfMeterStatusSnapshot status = GetStatus();
			PerfMeterSelfOverheadWindowSnapshot selfOverheadWindow = string.IsNullOrEmpty(summary.SessionId)
				? PerfMeterSelfOverheadWindowSnapshot.Unavailable
				: GetSelfOverheadWindow(PerfMeterSelfOverheadWindowKind.Session, summary.SessionId);
			return PerfMeterSessionExporter.ExportJson(
				path,
				summary,
				samples,
				timeline,
				status,
				true,
				PerfMeterSessionExporter.RuntimePackageIdentity,
				selfOverheadWindow).Success;
		}

		public static bool ExportSessionCsv(string path)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterSessionSummarySnapshot summary = runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
			PerfMeterSessionSampleSnapshot[] samples = runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			return PerfMeterSessionExporter.ExportCsv(path, summary, samples, GetStatus());
		}

		public static void RequestOverdrawMeasurement(int frameCount = 0)
		{
			TryRequestOverdrawMeasurement(frameCount);
		}

		public static PerfMeterMutationResultSnapshot TryRequestOverdrawMeasurement(int frameCount = 0)
		{
			PerfMeterSettingsSnapshot settings = GetOperationSettings();
			int normalizedFrameCount = frameCount <= 0
				? settings.OverdrawDefaultFrameCount
				: Mathf.Clamp(frameCount, 1, settings.OverdrawMaxFrameCount);
			if (!TryGetMutableRuntime(out PerfMeterRuntime runtime))
			{
				return RuntimeUnavailableMutation(frameCount, 0);
			}

			runtime.RequestOverdrawMeasurement(normalizedFrameCount);
			PerfMeterOverdrawMeasurementState state = PerfMeterRuntime.OverdrawState;
			if (state == PerfMeterOverdrawMeasurementState.Unsupported)
			{
				return MutationResult(PerfMeterMutationStatus.Unsupported, PerfMeterMutationReason.UnsupportedRenderPipeline, frameCount, normalizedFrameCount);
			}
			if (state != PerfMeterOverdrawMeasurementState.Measuring)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, frameCount, normalizedFrameCount);
			}

			bool normalized = frameCount != normalizedFrameCount;
			return MutationResult(
				normalized ? PerfMeterMutationStatus.Normalized : PerfMeterMutationStatus.Applied,
				normalized ? PerfMeterMutationReason.ValueNormalized : PerfMeterMutationReason.None,
				frameCount,
				normalizedFrameCount);
		}

		public static void CancelOverdrawMeasurement()
		{
			TryCancelOverdrawMeasurement();
		}

		public static PerfMeterMutationResultSnapshot TryCancelOverdrawMeasurement()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime == null)
			{
				return MutationResult(PerfMeterMutationStatus.NoChange, PerfMeterMutationReason.NoActiveOperation, PerfMeterOverdrawMeasurementState.Canceled, PerfMeterOverdrawMeasurementState.Off);
			}
			if (!runtime.AcceptsMutations)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, PerfMeterOverdrawMeasurementState.Canceled, PerfMeterRuntime.OverdrawState);
			}

			PerfMeterOverdrawMeasurementState previousState = PerfMeterRuntime.OverdrawState;
			runtime.CancelOverdrawMeasurement();
			PerfMeterOverdrawMeasurementState effectiveState = PerfMeterRuntime.OverdrawState;
			bool hadOperation = previousState != PerfMeterOverdrawMeasurementState.Off && previousState != PerfMeterOverdrawMeasurementState.Canceled;
			return MutationResult(
				hadOperation && effectiveState == PerfMeterOverdrawMeasurementState.Canceled ? PerfMeterMutationStatus.Applied : PerfMeterMutationStatus.NoChange,
				hadOperation ? PerfMeterMutationReason.None : PerfMeterMutationReason.NoActiveOperation,
				PerfMeterOverdrawMeasurementState.Canceled,
				effectiveState);
		}

		public static bool IsOverdrawHeatmapVisible
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && PerfMeterRuntime.IsOverdrawHeatmapVisible;
			}
		}

		public static void SetOverdrawHeatmapVisible(bool visible)
		{
			TrySetOverdrawHeatmapVisible(visible);
		}

		public static PerfMeterMutationResultSnapshot TrySetOverdrawHeatmapVisible(bool visible)
		{
			if (!TryGetMutableRuntime(out PerfMeterRuntime runtime))
			{
				return RuntimeUnavailableMutation(visible, IsOverdrawHeatmapVisible);
			}

			bool previousValue = PerfMeterRuntime.IsOverdrawHeatmapVisible;
			runtime.SetOverdrawHeatmapVisible(visible);
			bool effectiveValue = PerfMeterRuntime.IsOverdrawHeatmapVisible;
			if (visible && PerfMeterRenderPipelineDetector.GetActiveKind() == PerfMeterRenderPipelineKind.HighDefinition)
			{
				return MutationResult(PerfMeterMutationStatus.Unsupported, PerfMeterMutationReason.UnsupportedRenderPipeline, visible, effectiveValue);
			}
			if (effectiveValue != visible)
			{
				return MutationResult(PerfMeterMutationStatus.Rejected, PerfMeterMutationReason.RuntimeRejected, visible, effectiveValue);
			}

			return MutationResult(
				previousValue == effectiveValue ? PerfMeterMutationStatus.NoChange : PerfMeterMutationStatus.Applied,
				previousValue == effectiveValue ? PerfMeterMutationReason.AlreadyInRequestedState : PerfMeterMutationReason.None,
				visible,
				effectiveValue);
		}

		public static bool IsOverlayVisible
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null && runtime.IsOverlayVisible;
			}
		}

		public static PerfMeterOverlayCorner OverlayCorner
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayCorner : PerfMeterOverlayCorner.TopRight;
			}
		}

		public static PerfMeterOverlayMode OverlayMode
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayMode : PerfMeterOverlayMode.Full;
			}
		}

		public static PerfMeterOverlayPreset OverlayPreset
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayPreset : PerfMeterOverlayPreset.FullDiagnostics;
			}
		}

		public static string VisualOverlayPresetId
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.VisualOverlayPresetId : string.Empty;
			}
		}

		public static PerfMeterOverlayTheme OverlayTheme
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayTheme : PerfMeterOverlayTheme.ClassicDark;
			}
		}

		public static PerfMeterOverlayLayout OverlayLayout
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayLayout : PerfMeterOverlayLayout.MetricBars;
			}
		}

		public static PerfMeterOverlayFontFamily OverlayFontFamily
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayFontFamily : PerfMeterOverlayFontFamily.Manrope;
			}
		}

		public static PerfMeterOverlayModule OverlayModules
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.OverlayModules : PerfMeterSettingsStore.DefaultOverlayModules;
			}
		}

		public static PerfMeterTargetFps TargetFps
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.TargetFps : PerfMeterTargetFps.Fps60;
			}
		}

		public static bool EditorWarningLogsEnabled
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.EditorWarningLogsEnabled : GetSettings().EditorWarningsEnabled;
			}
		}

		public static bool StructuredLogsEnabled
		{
			get
			{
				PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
				return runtime != null ? runtime.StructuredLogsEnabled : GetSettings().StructuredLogsEnabled;
			}
		}

		public static void SetOverlayVisible(bool visible)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayVisible(visible);
			}
		}

		public static PerfMeterMutationResultSnapshot TryApplyOverlayConfiguration(PerfMeterOverlayConfiguration configuration)
		{
			if (!TryGetMutableRuntime(out PerfMeterRuntime runtime))
			{
				return RuntimeUnavailableMutation(
					FormatOverlayConfiguration(configuration),
					FormatOverlayConfiguration(GetEffectiveOverlayConfiguration()));
			}

			PerfMeterOverlayConfiguration normalized = NormalizeOverlayConfiguration(configuration);
			PerfMeterOverlayConfiguration previous = GetEffectiveOverlayConfiguration(runtime);
			if (runtime.OverlayPreset != normalized.Preset)
			{
				runtime.SetOverlayPreset(normalized.Preset);
			}
			if (runtime.OverlayCorner != normalized.Corner)
			{
				runtime.SetOverlayCorner(normalized.Corner);
			}
			if (runtime.OverlayTheme != normalized.Theme)
			{
				runtime.SetOverlayTheme(normalized.Theme);
			}
			if (runtime.OverlayLayout != normalized.Layout)
			{
				runtime.SetOverlayLayout(normalized.Layout);
			}
			if (runtime.OverlayFontFamily != normalized.FontFamily)
			{
				runtime.SetOverlayFontFamily(normalized.FontFamily);
			}
			if (runtime.OverlayModules != normalized.Modules)
			{
				runtime.SetOverlayModules(normalized.Modules);
			}
			if (runtime.TargetFps != normalized.TargetFps)
			{
				runtime.SetTargetFps(normalized.TargetFps);
			}
			if (runtime.OverlayRequestedVisible != normalized.Visible)
			{
				runtime.SetOverlayVisible(normalized.Visible);
			}
			PerfMeterOverlayConfiguration effective = GetEffectiveOverlayConfiguration(runtime);
			if (!OverlayConfigurationsEqual(effective, normalized))
			{
				return MutationResult(
					PerfMeterMutationStatus.Rejected,
					PerfMeterMutationReason.RuntimeRejected,
					FormatOverlayConfiguration(configuration),
					FormatOverlayConfiguration(effective));
			}

			bool wasNormalized = !OverlayConfigurationsEqual(configuration, normalized);
			bool changed = !OverlayConfigurationsEqual(previous, effective);
			return MutationResult(
				wasNormalized ? PerfMeterMutationStatus.Normalized : changed ? PerfMeterMutationStatus.Applied : PerfMeterMutationStatus.NoChange,
				wasNormalized ? PerfMeterMutationReason.ValueNormalized : changed ? PerfMeterMutationReason.None : PerfMeterMutationReason.AlreadyInRequestedState,
				FormatOverlayConfiguration(configuration),
				FormatOverlayConfiguration(effective));
		}

		public static void SetOverlayCorner(PerfMeterOverlayCorner corner)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayCorner(corner);
			}
		}

		public static void SetOverlayMode(PerfMeterOverlayMode mode)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayMode(mode);
			}
		}

		public static void SetOverlayPreset(PerfMeterOverlayPreset preset)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayPreset(preset);
			}
		}

		public static void ApplyVisualOverlayPreset(string presetId, PerfMeterOverlayPresetJson preset)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.ApplyVisualOverlayPreset(presetId, preset);
			}
		}

		public static void SetOverlayTheme(PerfMeterOverlayTheme theme)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayTheme(theme);
			}
		}

		public static void SetOverlayLayout(PerfMeterOverlayLayout layout)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayLayout(layout);
			}
		}

		public static void SetOverlayFontFamily(PerfMeterOverlayFontFamily fontFamily)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayFontFamily(fontFamily);
			}
		}

		public static void SetOverlayModules(PerfMeterOverlayModule modules)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayModules(modules);
			}
		}

		public static void SetOverlayModuleVisible(PerfMeterOverlayModule module, bool visible)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayModuleVisible(module, visible);
			}
		}

		public static void SetTargetFps(PerfMeterTargetFps targetFps)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetTargetFps(targetFps);
			}
		}

		public static void SetOverlayUpdateOptions(float refreshIntervalSeconds, int graphHistoryLength)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayUpdateOptions(refreshIntervalSeconds, graphHistoryLength);
			}
		}

		public static void SetEditorWarningLogsEnabled(bool enabled)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetEditorWarningLogsEnabled(enabled);
			}
		}

		public static void SetStructuredLogsEnabled(bool enabled)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetStructuredLogsEnabled(enabled);
			}
		}

		internal static bool ApplySettings(PerfMeterSettingsSnapshot settings)
		{
			return PerfMeterSettingsStore.ApplySnapshotToRuntime(settings);
		}

		private static PerfMeterSettingsSnapshot GetOperationSettings()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.ConfiguredSettings : GetSettings();
		}

		private static bool TryGetMutableRuntime(out PerfMeterRuntime runtime)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				runtime = PerfMeterRuntime.Instance;
				return false;
			}

			runtime = PerfMeterRuntime.Instance;
			return runtime != null && runtime.AcceptsMutations;
		}

		private static PerfMeterMutationResultSnapshot MutationResult(
			PerfMeterMutationStatus status,
			PerfMeterMutationReason reason,
			object requestedValue,
			object effectiveValue)
		{
			return new PerfMeterMutationResultSnapshot(
				status,
				reason,
				requestedValue?.ToString() ?? string.Empty,
				effectiveValue?.ToString() ?? string.Empty);
		}

		private static PerfMeterMutationResultSnapshot RuntimeUnavailableMutation(object requestedValue, object effectiveValue)
		{
			bool pendingCleanup = PerfMeterRuntime.Instance != null && PerfMeterRuntime.Instance.HasPendingCleanup;
			return MutationResult(
				pendingCleanup ? PerfMeterMutationStatus.Rejected : PerfMeterMutationStatus.Unavailable,
				pendingCleanup ? PerfMeterMutationReason.PendingCleanup : PerfMeterMutationReason.RuntimeUnavailable,
				requestedValue,
				effectiveValue);
		}

		private static PerfMeterCollectionMode NormalizeCollectionMode(PerfMeterCollectionMode mode)
		{
			switch (mode)
			{
				case PerfMeterCollectionMode.Background:
				case PerfMeterCollectionMode.Overlay:
				case PerfMeterCollectionMode.OverdrawDiagnostic:
					return mode;
				default:
					return PerfMeterCollectionMode.Overlay;
			}
		}

		private static PerfMeterOverlayConfiguration GetEffectiveOverlayConfiguration(PerfMeterRuntime runtime = null)
		{
			runtime = runtime ?? PerfMeterRuntime.Instance;
			if (runtime == null)
			{
				return new PerfMeterOverlayConfiguration(
					false,
					PerfMeterOverlayCorner.TopRight,
					PerfMeterOverlayPreset.FullDiagnostics,
					PerfMeterOverlayTheme.ClassicDark,
					PerfMeterOverlayLayout.MetricBars,
					PerfMeterOverlayFontFamily.Manrope,
					PerfMeterSettingsStore.DefaultOverlayModules,
					PerfMeterTargetFps.Fps60);
			}

			return new PerfMeterOverlayConfiguration(
				runtime.OverlayRequestedVisible,
				runtime.OverlayCorner,
				runtime.OverlayPreset,
				runtime.OverlayTheme,
				runtime.OverlayLayout,
				runtime.OverlayFontFamily,
				runtime.OverlayModules,
				runtime.TargetFps);
		}

		private static PerfMeterOverlayConfiguration NormalizeOverlayConfiguration(PerfMeterOverlayConfiguration configuration)
		{
			PerfMeterOverlayPreset preset = NormalizeOverlayPreset(configuration.Preset);
			PerfMeterOverlayLayout layout = PerfMeterSettingsStore.NormalizeOverlayLayout(configuration.Layout);
			PerfMeterOverlayModule modules = configuration.Modules & PerfMeterOverlayModule.All;
			if (modules == PerfMeterOverlayModule.None)
			{
				modules = PerfMeterSettingsStore.GetPresetModules(preset);
			}
			if (preset != PerfMeterOverlayPreset.Custom &&
				(layout != PerfMeterSettingsStore.GetPresetLayout(preset) || modules != PerfMeterSettingsStore.GetPresetModules(preset)))
			{
				preset = PerfMeterOverlayPreset.Custom;
			}

			return new PerfMeterOverlayConfiguration(
				configuration.Visible,
				NormalizeOverlayCorner(configuration.Corner),
				preset,
				PerfMeterSettingsStore.NormalizeOverlayTheme(configuration.Theme),
				layout,
				PerfMeterSettingsStore.NormalizeOverlayFontFamily(configuration.FontFamily),
				modules,
				NormalizeTargetFps(configuration.TargetFps));
		}

		private static PerfMeterOverlayCorner NormalizeOverlayCorner(PerfMeterOverlayCorner corner)
		{
			switch (corner)
			{
				case PerfMeterOverlayCorner.TopLeft:
				case PerfMeterOverlayCorner.TopRight:
				case PerfMeterOverlayCorner.BottomLeft:
				case PerfMeterOverlayCorner.BottomRight:
					return corner;
				default:
					return PerfMeterOverlayCorner.TopRight;
			}
		}

		private static PerfMeterOverlayPreset NormalizeOverlayPreset(PerfMeterOverlayPreset preset)
		{
			return preset >= PerfMeterOverlayPreset.Custom && preset <= PerfMeterOverlayPreset.AgentDebug
				? preset
				: PerfMeterOverlayPreset.FullDiagnostics;
		}

		private static PerfMeterTargetFps NormalizeTargetFps(PerfMeterTargetFps targetFps)
		{
			switch (targetFps)
			{
				case PerfMeterTargetFps.Fps15:
				case PerfMeterTargetFps.Fps30:
				case PerfMeterTargetFps.Fps60:
				case PerfMeterTargetFps.Fps90:
				case PerfMeterTargetFps.Fps120:
				case PerfMeterTargetFps.Fps144:
				case PerfMeterTargetFps.Fps240:
					return targetFps;
				default:
					return PerfMeterTargetFps.Fps60;
			}
		}

		private static bool OverlayConfigurationsEqual(PerfMeterOverlayConfiguration first, PerfMeterOverlayConfiguration second)
		{
			return first.Visible == second.Visible &&
				first.Corner == second.Corner &&
				first.Preset == second.Preset &&
				first.Theme == second.Theme &&
				first.Layout == second.Layout &&
				first.FontFamily == second.FontFamily &&
				first.Modules == second.Modules &&
				first.TargetFps == second.TargetFps;
		}

		private static string FormatOverlayConfiguration(PerfMeterOverlayConfiguration configuration)
		{
			return "visible=" + configuration.Visible +
				";corner=" + configuration.Corner +
				";preset=" + configuration.Preset +
				";theme=" + configuration.Theme +
				";layout=" + configuration.Layout +
				";font_family=" + configuration.FontFamily +
				";modules=" + configuration.Modules +
				";target_fps=" + (int)configuration.TargetFps;
		}

		internal static void ApplyOverlayTuning(PerfMeterSettingsSnapshot settings)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverlayTuning(settings);
			}
		}

		internal static void RaiseAlertFired(PerfMeterAlertSnapshot alert)
		{
			AlertFired?.Invoke(alert);
		}
	}

	internal readonly struct PerfMeterCustomMetricCollection
	{
		internal PerfMeterCustomMetricCollection(PerfMeterCustomMetricSnapshot[] buffer, int count)
		{
			Buffer = buffer ?? System.Array.Empty<PerfMeterCustomMetricSnapshot>();
			Count = count < 0 ? 0 : count > Buffer.Length ? Buffer.Length : count;
		}

		internal PerfMeterCustomMetricSnapshot[] Buffer { get; }
		internal int Count { get; }
	}

	internal static class PerfMeterCustomMetricRegistry
	{
		private static readonly System.Collections.Generic.List<IPerfMeterCustomMetricProvider> Providers = new System.Collections.Generic.List<IPerfMeterCustomMetricProvider>();
		private static readonly object SyncRoot = new object();
		private static ProviderSlot[] _providerSnapshot = System.Array.Empty<ProviderSlot>();
		private static PerfMeterCustomMetricSnapshot[] _collectionBuffer = System.Array.Empty<PerfMeterCustomMetricSnapshot>();

		internal static void Register(IPerfMeterCustomMetricProvider provider)
		{
			if (provider == null)
			{
				throw new System.ArgumentNullException(nameof(provider));
			}

			bool changed = false;
			lock (SyncRoot)
			{
				if (!Providers.Contains(provider))
				{
					Providers.Add(provider);
					PublishProviderSnapshotLocked();
					changed = true;
				}
			}

			if (changed)
			{
				PerfMeterRuntime.InvalidateCustomMetricCache();
				PerfMeterProfilerInstrumentation.RecordCustomMetricCount(0);
			}
		}

		internal static void Unregister(IPerfMeterCustomMetricProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			bool changed = false;
			lock (SyncRoot)
			{
				if (Providers.Remove(provider))
				{
					PublishProviderSnapshotLocked();
					changed = true;
				}
			}

			if (changed)
			{
				PerfMeterRuntime.InvalidateCustomMetricCache();
				PerfMeterProfilerInstrumentation.RecordCustomMetricCount(0);
			}
		}

		internal static void Clear()
		{
			lock (SyncRoot)
			{
				Providers.Clear();
				System.Threading.Volatile.Write(ref _providerSnapshot, System.Array.Empty<ProviderSlot>());
			}

			PerfMeterRuntime.InvalidateCustomMetricCache();
			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(0);
		}

		internal static PerfMeterCustomMetricCollection Collect()
		{
			PerfMeterCustomMetricCollection metrics;
			using (PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.CustomMetricProviders))
			using (PerfMeterProfilerInstrumentation.CustomMetricsMarker.Auto())
			{
				metrics = CollectCore();
			}

			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(metrics.Count);
			return metrics;
		}

		internal static PerfMeterCustomMetricSnapshot[] Copy(PerfMeterCustomMetricCollection metrics)
		{
			if (metrics.Count == 0)
			{
				return System.Array.Empty<PerfMeterCustomMetricSnapshot>();
			}

			PerfMeterCustomMetricSnapshot[] copy = new PerfMeterCustomMetricSnapshot[metrics.Count];
			System.Array.Copy(metrics.Buffer, copy, metrics.Count);
			return copy;
		}

		private static PerfMeterCustomMetricCollection CollectCore()
		{
			ProviderSlot[] providers = System.Threading.Volatile.Read(ref _providerSnapshot);
			if (providers.Length == 0)
			{
				return new PerfMeterCustomMetricCollection(System.Array.Empty<PerfMeterCustomMetricSnapshot>(), 0);
			}

			PerfMeterCustomMetricSnapshot[] metrics = EnsureCollectionBuffer(providers.Length);
			int count = 0;
			for (int i = 0; i < providers.Length; i++)
			{
				ProviderSlot slot = providers[i];
				IPerfMeterCustomMetricProvider provider = slot.Provider;
				try
				{
					if (provider.TryCollect(out PerfMeterCustomMetricSnapshot metric))
					{
						metrics[count] = NormalizeMetric(metric, slot.ProviderId);
						count++;
					}
				}
				catch (System.Exception exception)
				{
					metrics[count] = new PerfMeterCustomMetricSnapshot(slot.ProviderId, slot.ProviderId, "custom", string.Empty, 0d, false, exception.GetType().Name + ": " + exception.Message);
					count++;
				}
			}

			return new PerfMeterCustomMetricCollection(metrics, count);
		}

		private static void PublishProviderSnapshotLocked()
		{
			ProviderSlot[] snapshot = new ProviderSlot[Providers.Count];
			for (int i = 0; i < Providers.Count; i++)
			{
				IPerfMeterCustomMetricProvider provider = Providers[i];
				snapshot[i] = new ProviderSlot(provider, GetProviderId(provider, i));
			}

			System.Threading.Volatile.Write(ref _providerSnapshot, snapshot);
		}

		private static PerfMeterCustomMetricSnapshot[] EnsureCollectionBuffer(int requiredLength)
		{
			if (_collectionBuffer.Length < requiredLength)
			{
				_collectionBuffer = new PerfMeterCustomMetricSnapshot[requiredLength];
			}

			return _collectionBuffer;
		}

		private static string GetProviderId(IPerfMeterCustomMetricProvider provider, int index)
		{
			try
			{
				string id = provider.Id;
				return string.IsNullOrEmpty(id) ? "custom_metric_" + index : id;
			}
			catch (System.Exception)
			{
				return "custom_metric_" + index;
			}
		}

		private static PerfMeterCustomMetricSnapshot NormalizeMetric(PerfMeterCustomMetricSnapshot metric, string providerId)
		{
			string id = string.IsNullOrEmpty(metric.Id) ? providerId : metric.Id;
			string name = string.IsNullOrEmpty(metric.Name) ? id : metric.Name;
			string category = string.IsNullOrEmpty(metric.Category) ? "custom" : metric.Category;
			return new PerfMeterCustomMetricSnapshot(id, name, category, metric.Unit, metric.Value, metric.Available, metric.Warning);
		}

		private readonly struct ProviderSlot
		{
			internal ProviderSlot(IPerfMeterCustomMetricProvider provider, string providerId)
			{
				Provider = provider;
				ProviderId = providerId;
			}

			internal IPerfMeterCustomMetricProvider Provider { get; }
			internal string ProviderId { get; }
		}
	}
}
