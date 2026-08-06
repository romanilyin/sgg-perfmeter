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
			return runtime != null ? runtime.GetLatestCustomMetrics() : PerfMeterCustomMetricRegistry.Collect();
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
			return runtime != null ? runtime.LatestPlatformTelemetry : PerfMeterPlatformTelemetryRegistry.Collect();
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

		public static PerfMeterSettingsSnapshot GetSettings()
		{
			return PerfMeterSettingsStore.LoadFromResources();
		}

		public static void EnsureRunning()
		{
			PerfMeterRuntime.EnsureRunning();
		}

		public static void Stop()
		{
			PerfMeterRuntime.StopRunning();
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
			if (mode == PerfMeterCollectionMode.Stopped)
			{
				Stop();
				return;
			}

			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetCollectionMode(mode);
			}
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
			StartSession(PerfMeterSessionOptions.FromSettings(GetSettings()));
		}

		public static void StartSession(PerfMeterSessionOptions options)
		{
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.StartSession(options);
			}
		}

		public static void StopSession()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.StopSession();
			}
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

		public static bool ExportSessionJson(string path)
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			PerfMeterSessionSummarySnapshot summary = runtime != null ? runtime.GetSessionSummary() : PerfMeterSessionSummarySnapshot.Empty;
			PerfMeterSessionSampleSnapshot[] samples = runtime != null ? runtime.GetSessionSamples() : System.Array.Empty<PerfMeterSessionSampleSnapshot>();
			return PerfMeterSessionExporter.ExportJson(path, summary, samples, GetStatus());
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
			PerfMeterSettingsSnapshot settings = GetSettings();
			int normalizedFrameCount = frameCount <= 0
				? settings.OverdrawDefaultFrameCount
				: Mathf.Clamp(frameCount, 1, settings.OverdrawMaxFrameCount);
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.RequestOverdrawMeasurement(normalizedFrameCount);
			}
		}

		public static void CancelOverdrawMeasurement()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.CancelOverdrawMeasurement();
			}
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
			if (!PerfMeterRuntime.EnsureRunning())
			{
				return;
			}

			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (runtime != null)
			{
				runtime.SetOverdrawHeatmapVisible(visible);
			}
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
				return runtime != null ? runtime.OverlayModules : PerfMeterSettingsStore.GetPresetModules(PerfMeterOverlayPreset.FullDiagnostics);
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
				return runtime != null ? runtime.StructuredLogsEnabled : true;
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

		internal static void ApplySettings(PerfMeterSettingsSnapshot settings)
		{
			PerfMeterSettingsStore.ApplySnapshotToRuntime(settings);
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

	internal static class PerfMeterCustomMetricRegistry
	{
		private static readonly System.Collections.Generic.List<IPerfMeterCustomMetricProvider> Providers = new System.Collections.Generic.List<IPerfMeterCustomMetricProvider>();
		private static readonly object SyncRoot = new object();
		private static IPerfMeterCustomMetricProvider[] _providerSnapshot = System.Array.Empty<IPerfMeterCustomMetricProvider>();

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
					_providerSnapshot = Providers.ToArray();
					changed = true;
				}
			}

			if (changed)
			{
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
					_providerSnapshot = Providers.ToArray();
					changed = true;
				}
			}

			if (changed)
			{
				PerfMeterProfilerInstrumentation.RecordCustomMetricCount(0);
			}
		}

		internal static void Clear()
		{
			lock (SyncRoot)
			{
				Providers.Clear();
				_providerSnapshot = System.Array.Empty<IPerfMeterCustomMetricProvider>();
			}

			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(0);
		}

		internal static PerfMeterCustomMetricSnapshot[] Collect()
		{
			PerfMeterCustomMetricSnapshot[] metrics;
			using (PerfMeterSelfObservability.Measure(PerfMeterSelfOverheadComponent.CustomMetricProviders))
			using (PerfMeterProfilerInstrumentation.CustomMetricsMarker.Auto())
			{
				metrics = CollectCore();
			}

			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(metrics.Length);
			return metrics;
		}

		private static PerfMeterCustomMetricSnapshot[] CollectCore()
		{
			IPerfMeterCustomMetricProvider[] providers;
			lock (SyncRoot)
			{
				providers = _providerSnapshot;
				if (providers.Length == 0)
				{
					return System.Array.Empty<PerfMeterCustomMetricSnapshot>();
				}
			}

			PerfMeterCustomMetricSnapshot[] metrics = new PerfMeterCustomMetricSnapshot[providers.Length];
			int count = 0;
			for (int i = 0; i < providers.Length; i++)
			{
				IPerfMeterCustomMetricProvider provider = providers[i];
				string providerId = GetProviderId(provider, i);
				try
				{
					if (provider.TryCollect(out PerfMeterCustomMetricSnapshot metric))
					{
						metrics[count] = NormalizeMetric(metric, providerId);
						count++;
					}
				}
				catch (System.Exception exception)
				{
					metrics[count] = new PerfMeterCustomMetricSnapshot(providerId, providerId, "custom", string.Empty, 0d, false, exception.GetType().Name + ": " + exception.Message);
					count++;
				}
			}

			if (count == metrics.Length)
			{
				return metrics;
			}

			PerfMeterCustomMetricSnapshot[] compact = new PerfMeterCustomMetricSnapshot[count];
			System.Array.Copy(metrics, compact, count);
			return compact;
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
	}
}
