using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SGG.PerfMeter.Editor.Setup;
using UnityEditor;
using UnityEngine;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.Mcp
{
	public static class PerfMeterMcpCommands
	{
		public static string SetupStatus()
		{
			StringBuilder builder = new StringBuilder(1536);
			builder.Append("{\"compatibility\":");
			AppendCompatibilityStatus(builder, PerfMeterSetupActions.GetCompatibilityStatus());
			builder.Append(",\"status_report\":").Append(JsonString(PerfMeterSetupActions.GetStatusReport()));
			builder.Append('}');
			return builder.ToString();
		}

		public static string CompatibilityStatus()
		{
			StringBuilder builder = new StringBuilder(1024);
			AppendCompatibilityStatus(builder, PerfMeterSetupActions.GetCompatibilityStatus());
			return builder.ToString();
		}

		public static string SetupRun()
		{
			PerfMeterSetupActionResult result = PerfMeterSetupActions.RunRecommendedSetup();
			return "{\"success\":" + JsonBool(result.Success)
				+ ",\"message\":" + JsonString(result.Message)
				+ ",\"status_report\":" + JsonString(PerfMeterSetupActions.GetStatusReport())
				+ "}";
		}

		public static string RuntimeStatus()
		{
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string RuntimeEnsure()
		{
			RuntimePerformanceMeter.EnsureRunning();
			bool repaintRequested = PerfMeterMcpOverlaySession.ApplyStoredVisibilityIfPlaying();
			return StatusJson(RuntimePerformanceMeter.GetStatus(), repaintRequested);
		}

		public static string RuntimeStop()
		{
			RuntimePerformanceMeter.Stop();
			return StatusJson(RuntimePerformanceMeter.GetStatus(), PerfMeterMcpOverlaySession.RequestRepaint());
		}

		public static string RuntimeResetStats()
		{
			RuntimePerformanceMeter.ResetStats();
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string RuntimeModeSet(string argsJson)
		{
			PerfMeterCollectionMode mode = ParseCollectionMode(RequireString(argsJson, "mode"));
			if (mode == PerfMeterCollectionMode.Background)
			{
				PerfMeterMcpOverlaySession.StoreRequestedVisibility(false);
			}
			else if (mode == PerfMeterCollectionMode.Overlay || mode == PerfMeterCollectionMode.OverdrawDiagnostic)
			{
				PerfMeterMcpOverlaySession.StoreRequestedVisibility(true);
			}

			RuntimePerformanceMeter.SetCollectionMode(mode);
			if (mode == PerfMeterCollectionMode.OverdrawDiagnostic && TryExtractInt(argsJson, "frame_count", out int frameCount))
			{
				PerfMeterSettingsSnapshot settings = RuntimePerformanceMeter.GetSettings();
				RuntimePerformanceMeter.RequestOverdrawMeasurement(Mathf.Clamp(frameCount, 1, settings.OverdrawMaxFrameCount));
			}

			return StatusJson(RuntimePerformanceMeter.GetStatus(), PerfMeterMcpOverlaySession.RequestRepaint());
		}

		public static string MetricsLatest()
		{
			return MetricsJson(RuntimePerformanceMeter.GetLatestMetrics());
		}

		public static string PlatformTelemetry()
		{
			StringBuilder builder = new StringBuilder(768);
			AppendPlatformTelemetry(builder, RuntimePerformanceMeter.GetPlatformTelemetry());
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string ProfilerCapabilities()
		{
			return ProfilerCapabilitiesJson(RuntimePerformanceMeter.GetProfilerMetricCatalog());
		}

		public static string GraphicsDiagnostics()
		{
			PerfMeterGraphicsDiagnosticsSnapshot snapshot = RuntimePerformanceMeter.GetGraphicsDiagnostics();
			StringBuilder builder = new StringBuilder(1024);
			builder.Append("{\"availability\":").Append(JsonString(snapshot.Availability.ToString()));
			builder.Append(",\"collection_frame\":").Append(snapshot.CollectionFrame);
			builder.Append(",\"profiler_metric_catalog_revision\":").Append(snapshot.ProfilerMetricCatalogRevision);
			builder.Append(",\"graphics_device_type\":").Append(JsonString(snapshot.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(snapshot.GraphicsDeviceName));
			builder.Append(",\"graphics_device_vendor\":").Append(JsonString(snapshot.GraphicsDeviceVendor));
			builder.Append(",\"graphics_device_version\":").Append(JsonString(snapshot.GraphicsDeviceVersion));
			builder.Append(",\"parallel_pso_creation_availability\":").Append(JsonString(snapshot.ParallelPsoCreationAvailability.ToString()));
			builder.Append(",\"supports_parallel_pso_creation\":").Append(JsonBool(snapshot.SupportsParallelPsoCreation));
			builder.Append(",\"shader_gpu_program_creation_value\":").Append(snapshot.ShaderGpuProgramCreationValue);
			builder.Append(",\"graphics_pipeline_creation_value\":").Append(snapshot.GraphicsPipelineCreationValue);
			builder.Append(",\"shader_gpu_program_creation_capability\":");
			AppendProfilerMetricCapability(builder, snapshot.ShaderGpuProgramCreationCapability);
			builder.Append(",\"graphics_pipeline_creation_capability\":");
			AppendProfilerMetricCapability(builder, snapshot.GraphicsPipelineCreationCapability);
			builder.Append(",\"warning\":").Append(JsonString(snapshot.Warning));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string AlertsLatest()
		{
			return AlertsJson(false);
		}

		public static string AlertsClear()
		{
			RuntimePerformanceMeter.ClearAlerts();
			return AlertsJson(true);
		}

		public static string AlertsCaptureBegin(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			bool started = RuntimePerformanceMeter.BeginAlertCapture(captureId);
			return AlertCaptureScopeJson(captureId, started, false);
		}

		public static string AlertsCaptureEnd(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			bool ended = RuntimePerformanceMeter.EndAlertCapture(captureId);
			return AlertCaptureScopeJson(captureId, false, ended);
		}

		private static string AlertCaptureScopeJson(string requestedCaptureId, bool started, bool ended)
		{
			string activeCaptureId = RuntimePerformanceMeter.ActiveAlertCaptureId;
			return "{\"capture_scope_active\":" + JsonBool(!string.IsNullOrEmpty(activeCaptureId))
				+ ",\"capture_id\":" + JsonString(activeCaptureId)
				+ ",\"requested_capture_id\":" + JsonString(requestedCaptureId)
				+ ",\"started\":" + JsonBool(started)
				+ ",\"ended\":" + JsonBool(ended)
				+ "}";
		}

		public static string CaptureRequest(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			PerfMeterCaptureTool tool = ParseCaptureTool(RequireString(argsJson, "tool"));
			int captureFrames = RequireRange(ExtractInt(argsJson, "capture_frames", 1), 1, 120, "capture_frames");
			int preRollFrames = RequireRange(ExtractInt(argsJson, "pre_roll_frames", 0), 0, 600, "pre_roll_frames");
			int postRollFrames = RequireRange(ExtractInt(argsJson, "post_roll_frames", 0), 0, 600, "post_roll_frames");
			bool includeScreenshot = TryExtractBool(argsJson, "include_screenshot", out bool screenshot) && screenshot;
			PerfMeterCaptureRequestResult result = RuntimePerformanceMeter.RequestCapture(
				new PerfMeterCaptureOptions(captureId, tool, captureFrames, preRollFrames, postRollFrames),
				new PerfMeterCaptureBundleOptions(includeScreenshot));
			return CaptureCommandJson(result.ToString(), RuntimePerformanceMeter.GetCaptureStatus(), RuntimePerformanceMeter.GetCaptureBundleStatus(captureId));
		}

		public static string CaptureStatus(string argsJson)
		{
			TryExtractString(argsJson, "capture_id", out string captureId);
			PerfMeterCaptureStatusSnapshot capture = RuntimePerformanceMeter.GetCaptureStatus();
			PerfMeterCaptureBundleStatusSnapshot bundle = RuntimePerformanceMeter.GetCaptureBundleStatus(captureId);
			if (!string.IsNullOrEmpty(captureId) && !string.Equals(capture.CaptureId, captureId, StringComparison.Ordinal))
			{
				return CaptureCommandJson(bundle.State == PerfMeterCaptureBundleState.None ? "not_found" : "status", PerfMeterCaptureStatusSnapshot.NotRunning, bundle);
			}

			return CaptureCommandJson("status", capture, bundle);
		}

		public static string CaptureCancel(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			bool canceled = RuntimePerformanceMeter.CancelCapture(captureId);
			PerfMeterCaptureStatusSnapshot capture = RuntimePerformanceMeter.GetCaptureStatus();
			if (!string.Equals(capture.CaptureId, captureId, StringComparison.Ordinal))
			{
				capture = PerfMeterCaptureStatusSnapshot.NotRunning;
			}

			return CaptureCommandJson(canceled ? "canceled" : "not_canceled", capture, RuntimePerformanceMeter.GetCaptureBundleStatus(captureId));
		}

		public static string CaptureExport(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			TryExtractString(argsJson, "path", out string path);
			TryExtractString(argsJson, "external_artifact_path", out string externalArtifactPath);
			bool requireAuthority = TryExtractBool(argsJson, "require_authoritative_external_artifact", out bool required) && required;
			PerfMeterCaptureBundleExportResult result = RuntimePerformanceMeter.ExportCaptureBundle(captureId, path, externalArtifactPath, requireAuthority);
			return CaptureExportJson(result);
		}

		public static string CaptureCapabilities()
		{
			PerfMeterCaptureCapabilitiesSnapshot capabilities = RuntimePerformanceMeter.GetCaptureCapabilities();
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"renderdoc_supported\":").Append(JsonBool(capabilities.RenderDocSupported));
			builder.Append(",\"pix_supported\":").Append(JsonBool(capabilities.PixSupported));
			builder.Append(",\"screenshot_supported\":").Append(JsonBool(capabilities.ScreenshotSupported));
			builder.Append(",\"max_capture_frames\":").Append(capabilities.MaxCaptureFrames);
			builder.Append(",\"max_roll_frames\":").Append(capabilities.MaxRollFrames);
			builder.Append(",\"max_bundle_bytes\":").Append(capabilities.MaxBundleBytes);
			builder.Append(",\"max_screenshot_bytes\":").Append(capabilities.MaxScreenshotBytes);
			builder.Append(",\"max_memory_snapshot_bytes\":").Append(capabilities.MaxMemorySnapshotBytes);
			builder.Append(",\"total_quota_bytes\":").Append(capabilities.TotalQuotaBytes);
			builder.Append(",\"max_committed_bundles\":").Append(capabilities.MaxCommittedBundles);
			builder.Append(",\"retention_days\":").Append(capabilities.RetentionDays);
			builder.Append(",\"bundle_root\":").Append(JsonString(capabilities.BundleRoot));
			builder.Append(",\"tool_identity\":\"unknown\"");
			builder.Append(",\"tool_version\":\"unknown\"");
			builder.Append(",\"external_artifact_authority\":\"unavailable_without_native_provider\"");
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string MemorySnapshotRequest(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			PerfMeterMemoryCaptureFlags flags = ParseMemoryCaptureFlags(argsJson);
			int minimumFreeDiskMb = RequireRange(ExtractInt(argsJson, "minimum_free_disk_mb", 1024), 0, 1048576, "minimum_free_disk_mb");
			int cooldownSeconds = RequireRange(ExtractInt(argsJson, "cooldown_seconds", 300), 0, 86400, "cooldown_seconds");
			PerfMeterMemorySnapshotRequestResult result = RuntimePerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions(
				captureId,
				flags,
				minimumFreeDiskMb * 1024L * 1024L,
				cooldownSeconds));
			return MemorySnapshotCommandJson(result.ToString(), RuntimePerformanceMeter.GetMemorySnapshotStatus(), RuntimePerformanceMeter.GetCaptureBundleStatus(captureId));
		}

		public static string MemorySnapshotStatus()
		{
			PerfMeterMemorySnapshotStatusSnapshot status = RuntimePerformanceMeter.GetMemorySnapshotStatus();
			return MemorySnapshotCommandJson("status", status, RuntimePerformanceMeter.GetCaptureBundleStatus(status.CaptureId));
		}

		public static string MemorySnapshotCapabilities()
		{
			PerfMeterMemorySnapshotCapabilitiesSnapshot capabilities = RuntimePerformanceMeter.GetMemorySnapshotCapabilities();
			StringBuilder builder = new StringBuilder(640);
			builder.Append("{\"availability\":").Append(JsonString(capabilities.Availability.ToString()));
			builder.Append(",\"backend_id\":").Append(JsonString(capabilities.BackendId));
			builder.Append(",\"backend_version\":").Append(JsonString(capabilities.BackendVersion));
			builder.Append(",\"supported_capture_flags\":").Append(JsonString(capabilities.SupportedCaptureFlags.ToString()));
			builder.Append(",\"max_snapshot_bytes\":").Append(capabilities.MaxSnapshotBytes);
			builder.Append(",\"snapshot_root\":").Append(JsonString(capabilities.SnapshotRoot));
			builder.Append(",\"warning\":").Append(JsonString(capabilities.Warning));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string MemorySnapshotTriggersConfigure(string argsJson)
		{
			bool enabled = RequireBool(argsJson, "enabled");
			int thresholdMb = RequireRange(ExtractInt(argsJson, "system_memory_threshold_mb", 0), 0, 1048576, "system_memory_threshold_mb");
			int growthMb = RequireRange(ExtractInt(argsJson, "leak_growth_threshold_mb", 0), 0, 1048576, "leak_growth_threshold_mb");
			int windowFrames = RequireRange(ExtractInt(argsJson, "leak_window_frames", 300), 30, 36000, "leak_window_frames");
			int minimumFreeDiskMb = RequireRange(ExtractInt(argsJson, "minimum_free_disk_mb", 1024), 0, 1048576, "minimum_free_disk_mb");
			int cooldownSeconds = RequireRange(ExtractInt(argsJson, "cooldown_seconds", 300), 0, 86400, "cooldown_seconds");
			PerfMeterMemorySnapshotTriggerOptions options = new PerfMeterMemorySnapshotTriggerOptions(
				enabled,
				thresholdMb * 1024L * 1024L,
				growthMb * 1024L * 1024L,
				windowFrames,
				ParseMemoryCaptureFlags(argsJson),
				minimumFreeDiskMb * 1024L * 1024L,
				cooldownSeconds);
			bool configured = RuntimePerformanceMeter.ConfigureMemorySnapshotTriggers(options);
			StringBuilder builder = new StringBuilder(512);
			builder.Append("{\"configured\":").Append(JsonBool(configured));
			builder.Append(",\"triggers\":");
			AppendMemorySnapshotTriggers(builder, RuntimePerformanceMeter.GetMemorySnapshotTriggers());
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string GraphicsStateTraceRequest(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			int traceFrames = RequireRange(ExtractInt(argsJson, "trace_frames", PerfMeterGraphicsStateTraceOptions.DefaultTraceFrames), 1, PerfMeterGraphicsStateCollectionCoordinator.MaxTraceFrames, "trace_frames");
			int minimumFreeDiskMb = RequireRange(ExtractInt(argsJson, "minimum_free_disk_mb", 1024), 0, 1048576, "minimum_free_disk_mb");
			PerfMeterGraphicsStateCollectionRequestResult result = RuntimePerformanceMeter.RequestGraphicsStateTrace(
				new PerfMeterGraphicsStateTraceOptions(captureId, traceFrames, minimumFreeDiskMb * 1024L * 1024L));
			return GraphicsStateCollectionCommandJson(result.ToString(), RuntimePerformanceMeter.GetGraphicsStateCollectionStatus());
		}

		public static string GraphicsStateCollectionStatus()
		{
			return GraphicsStateCollectionCommandJson("status", RuntimePerformanceMeter.GetGraphicsStateCollectionStatus());
		}

		public static string GraphicsStateCollectionCapabilities()
		{
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities = RuntimePerformanceMeter.GetGraphicsStateCollectionCapabilities();
			StringBuilder builder = new StringBuilder(640);
			builder.Append("{\"availability\":").Append(JsonString(capabilities.Availability.ToString()));
			builder.Append(",\"backend_id\":").Append(JsonString(capabilities.BackendId));
			builder.Append(",\"backend_version\":").Append(JsonString(capabilities.BackendVersion));
			builder.Append(",\"supports_trace\":").Append(JsonBool(capabilities.SupportsTrace));
			builder.Append(",\"supports_prewarm\":").Append(JsonBool(capabilities.SupportsPrewarm));
			builder.Append(",\"supports_cache_miss_tracing\":").Append(JsonBool(capabilities.SupportsCacheMissTracing));
			builder.Append(",\"supports_parallel_pso_creation\":").Append(JsonBool(capabilities.SupportsParallelPsoCreation));
			builder.Append(",\"requires_session_recording\":").Append(JsonBool(capabilities.RequiresSessionRecording));
			builder.Append(",\"max_trace_frames\":").Append(capabilities.MaxTraceFrames);
			builder.Append(",\"max_artifact_bytes\":").Append(capabilities.MaxArtifactBytes);
			builder.Append(",\"artifact_root\":").Append(JsonString(capabilities.ArtifactRoot));
			builder.Append(",\"warning\":").Append(JsonString(capabilities.Warning));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string GraphicsStateTraceCancel(string argsJson)
		{
			string captureId = RequireString(argsJson, "capture_id");
			bool canceled = RuntimePerformanceMeter.CancelGraphicsStateTrace(captureId);
			return GraphicsStateCollectionCommandJson(canceled ? "canceled" : "not_canceled", RuntimePerformanceMeter.GetGraphicsStateCollectionStatus());
		}

		public static string GraphicsStateCollectionPrewarm(string argsJson)
		{
			string relativePath = RequireString(argsJson, "relative_path");
			int maxStateCount = RequireRange(ExtractInt(argsJson, "max_state_count", 0), 0, PerfMeterGraphicsStateCollectionCoordinator.MaxPrewarmStateCount, "max_state_count");
			PerfMeterGraphicsStateCollectionRequestResult result = RuntimePerformanceMeter.PrewarmGraphicsStateCollection(
				new PerfMeterGraphicsStatePrewarmOptions(relativePath, maxStateCount));
			return GraphicsStateCollectionCommandJson(result.ToString(), RuntimePerformanceMeter.GetGraphicsStateCollectionStatus());
		}

		public static string DeviceInfo()
		{
			return DeviceInfoJson(RuntimePerformanceMeter.GetDeviceInfo());
		}

		public static string CameraSnapshot(string argsJson)
		{
			PerfMeterCameraSource source = PerfMeterCameraSource.Auto;
			if (TryExtractString(argsJson, "source", out string sourceValue))
			{
				source = ParseCameraSource(sourceValue);
			}

			TryExtractString(argsJson, "camera_name_filter", out string cameraNameFilter);
			return CameraSnapshotJson(RuntimePerformanceMeter.GetCameraSnapshot(source, cameraNameFilter));
		}

		public static string RenderGraphSnapshot()
		{
			return RenderGraphSnapshotJson(RuntimePerformanceMeter.GetRenderGraphSnapshot());
		}

		public static string RenderIntegrationSnapshot()
		{
			StringBuilder builder = new StringBuilder(2048);
			builder.Append("{\"schema_version\":1,\"render_integration\":");
			PerfMeterSessionExporter.AppendRenderIntegration(builder, RuntimePerformanceMeter.GetRenderIntegrationSnapshot());
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		public static string OverlaySet(string argsJson)
		{
			bool visible = RequireBool(argsJson, "visible");
			if (TryExtractString(argsJson, "preset", out string preset))
			{
				RuntimePerformanceMeter.SetOverlayPreset(ParseOverlayPreset(preset));
			}

			if (TryExtractString(argsJson, "corner", out string corner))
			{
				RuntimePerformanceMeter.SetOverlayCorner(ParseOverlayCorner(corner));
			}

			if (TryExtractString(argsJson, "theme", out string theme))
			{
				RuntimePerformanceMeter.SetOverlayTheme(ParseOverlayTheme(theme));
			}

			if (TryExtractString(argsJson, "layout", out string layout))
			{
				RuntimePerformanceMeter.SetOverlayLayout(ParseOverlayLayout(layout));
			}

			if (TryExtractString(argsJson, "font_family", out string fontFamily))
			{
				RuntimePerformanceMeter.SetOverlayFontFamily(ParseOverlayFontFamily(fontFamily));
			}

			if (TryExtractInt(argsJson, "target_fps", out int targetFps))
			{
				RuntimePerformanceMeter.SetTargetFps(ParseTargetFps(targetFps));
			}

			if (TryExtractStringArray(argsJson, "modules", out string[] modules))
			{
				RuntimePerformanceMeter.SetOverlayModules(ParseOverlayModules(modules));
			}

			PerfMeterMcpOverlaySession.StoreRequestedVisibility(visible);
			RuntimePerformanceMeter.SetOverlayVisible(visible);
			return StatusJson(RuntimePerformanceMeter.GetStatus(), PerfMeterMcpOverlaySession.RequestRepaint());
		}

		public static string OverdrawStart(string argsJson)
		{
			PerfMeterSettingsSnapshot settings = RuntimePerformanceMeter.GetSettings();
			int frameCount = Mathf.Clamp(ExtractInt(argsJson, "frame_count", settings.OverdrawDefaultFrameCount), 1, settings.OverdrawMaxFrameCount);
			RuntimePerformanceMeter.RequestOverdrawMeasurement(frameCount);
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string OverdrawCancel()
		{
			RuntimePerformanceMeter.CancelOverdrawMeasurement();
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string OverdrawHeatmapSet(string argsJson)
		{
			RuntimePerformanceMeter.SetOverdrawHeatmapVisible(RequireBool(argsJson, "visible"));
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string SessionStart(string argsJson)
		{
			PerfMeterSessionOptions settingsOptions = PerfMeterSessionOptions.FromSettings(RuntimePerformanceMeter.GetSettings());
			int warmupFrames = ExtractInt(argsJson, "warmup_frames", settingsOptions.WarmupFrames);
			float warmupSeconds = ExtractFloat(argsJson, "warmup_seconds", settingsOptions.WarmupSeconds);
			float sampleIntervalSeconds = ExtractFloat(argsJson, "sample_interval_seconds", settingsOptions.SampleIntervalSeconds);
			int maxSamples = ExtractInt(argsJson, "max_samples", settingsOptions.MaxSamples);
			bool resetOnSceneLoad = TryExtractBool(argsJson, "reset_on_scene_load", out bool resetOnSceneLoadValue) ? resetOnSceneLoadValue : settingsOptions.ResetOnSceneLoad;
			int sceneLoadIgnoreFrames = ExtractInt(argsJson, "scene_load_ignore_frames", settingsOptions.SceneLoadIgnoreFrames);
			float sceneLoadIgnoreSeconds = ExtractFloat(argsJson, "scene_load_ignore_seconds", settingsOptions.SceneLoadIgnoreSeconds);
			RuntimePerformanceMeter.StartSession(new PerfMeterSessionOptions(warmupFrames, warmupSeconds, sampleIntervalSeconds, maxSamples, resetOnSceneLoad, sceneLoadIgnoreFrames, sceneLoadIgnoreSeconds));
			return SessionCommandJson(true, string.Empty, string.Empty, "recording", RuntimePerformanceMeter.GetSessionSummary());
		}

		public static string SessionStop()
		{
			RuntimePerformanceMeter.StopSession();
			return SessionCommandJson(true, string.Empty, string.Empty, "stopped", RuntimePerformanceMeter.GetSessionSummary());
		}

		public static string SessionSummary()
		{
			return SessionCommandJson(true, string.Empty, string.Empty, RuntimePerformanceMeter.GetSessionSummary().State.ToString(), RuntimePerformanceMeter.GetSessionSummary());
		}

		public static string SessionExport(string argsJson)
		{
			string path = RequireString(argsJson, "path");
			string format = RequireString(argsJson, "format");
			string safePath = ResolveProjectLocalPath(path);
			string normalizedFormat = NormalizeEnumToken(format);
			PerfMeterSessionSummarySnapshot summary = RuntimePerformanceMeter.GetSessionSummary();
			PerfMeterSessionSampleSnapshot[] samples = RuntimePerformanceMeter.GetSessionSamples();
			PerfMeterStatusSnapshot status = RuntimePerformanceMeter.GetStatus();
			PerfMeterSessionExportResult result;
			if (string.Equals(normalizedFormat, "json", StringComparison.OrdinalIgnoreCase))
			{
				PackageManagerInfo packageInfo = PackageManagerInfo.FindForAssembly(typeof(RuntimePerformanceMeter).Assembly);
				PerfMeterPackageIdentity packageIdentity = packageInfo != null
					? new PerfMeterPackageIdentity(packageInfo.name, packageInfo.version, "unity_package_manager")
					: PerfMeterSessionExporter.RuntimePackageIdentity;
				result = PerfMeterSessionExporter.ExportJson(safePath, summary, samples, status, false, packageIdentity);
			}
			else if (string.Equals(normalizedFormat, "csv", StringComparison.OrdinalIgnoreCase))
			{
				result = PerfMeterSessionExporter.ExportCsv(safePath, summary, samples, status, false);
			}
			else
			{
				throw new InvalidOperationException("schema_validation_failed\nArgument format must be json or csv");
			}

			return SessionCommandJson(result.Success, result.Path, result.Error, result.Status, summary);
		}

		private static string StatusJson(PerfMeterStatusSnapshot status, bool repaintRequested = false)
		{
			bool requestedVisible = PerfMeterMcpOverlaySession.GetRequestedVisibility(status);
			StringBuilder builder = new StringBuilder(2048);
			builder.Append("{\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"collection_mode\":").Append(JsonString(status.CollectionMode.ToString()));
			builder.Append(",\"frame_timing_availability\":").Append(JsonString(status.FrameTimingAvailability.ToString()));
			builder.Append(",\"graphics_device_type\":").Append(JsonString(status.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(status.GraphicsDeviceName));
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			builder.Append(",\"collection_frame\":").Append(status.CollectionFrame);
			builder.Append(",\"last_error\":").Append(JsonString(status.LastError));
			builder.Append(",\"application_focused\":").Append(JsonBool(status.ApplicationFocused));
			builder.Append(",\"application_paused\":").Append(JsonBool(status.ApplicationPaused));
			builder.Append(",\"bottleneck\":").Append(JsonString(status.Bottleneck.ToString()));
			AppendSelfOverhead(builder, status.SelfOverhead);
			builder.Append(",\"available_counters\":").Append(JsonString(status.AvailableCounters.ToString()));
			builder.Append(",\"unavailable_counters\":").Append(JsonString(status.UnavailableCounters.ToString()));
			builder.Append(",\"overlay_visible\":").Append(JsonBool(status.OverlayVisible));
			builder.Append(",\"overlay_requested_visible\":").Append(JsonBool(requestedVisible));
			builder.Append(",\"overlay_request_persisted\":").Append(JsonBool(PerfMeterMcpOverlaySession.HasStoredVisibility));
			builder.Append(",\"overlay_apply_state\":").Append(JsonString(PerfMeterMcpOverlaySession.GetApplyState(status, requestedVisible)));
			builder.Append(",\"repaint_requested\":").Append(JsonBool(repaintRequested));
			builder.Append(",\"rendered_visibility\":\"unknown\"");
			builder.Append(",\"overlay_corner\":").Append(JsonString(status.OverlayCorner.ToString()));
			builder.Append(",\"overlay_mode\":").Append(JsonString(status.OverlayMode.ToString()));
			builder.Append(",\"overlay_theme\":").Append(JsonString(status.OverlayTheme.ToString()));
			builder.Append(",\"overlay_layout\":").Append(JsonString(status.OverlayLayout.ToString()));
			builder.Append(",\"overlay_font_family\":").Append(JsonString(status.OverlayFontFamily.ToString()));
			builder.Append(",\"overlay_preset\":").Append(JsonString(status.OverlayPreset.ToString()));
			builder.Append(",\"overlay_modules\":");
			AppendOverlayModules(builder, status.OverlayModules);
			builder.Append(",\"target_fps\":").Append((int)status.TargetFps);
			builder.Append(",\"target_frame_budget_ms\":").Append(JsonNumber(1000d / (int)status.TargetFps));
			builder.Append(",\"overdraw_state\":").Append(JsonString(status.OverdrawState.ToString()));
			builder.Append(",\"overdraw_progress\":").Append(JsonNumber(status.OverdrawProgress));
			builder.Append(",\"overdraw_ratio\":").Append(JsonNumber(status.OverdrawRatio));
			builder.Append(",\"overdraw_heatmap_visible\":").Append(JsonBool(status.OverdrawHeatmapVisible));
			builder.Append(",\"session_state\":").Append(JsonString(status.SessionState.ToString()));
			builder.Append(",\"session_recording\":").Append(JsonBool(status.IsSessionRecording));
			builder.Append(",\"session_sample_count\":").Append(status.SessionSampleCount);
			builder.Append(",\"session_dropped_sample_count\":").Append(status.SessionDroppedSampleCount);
			builder.Append(",\"active_alert_count\":").Append(status.ActiveAlertCount);
			builder.Append(",\"fired_alert_count\":").Append(status.FiredAlertCount);
			builder.Append(",\"latest_alert_rule_id\":").Append(JsonString(status.LatestAlertRuleId));
			builder.Append(",\"latest_alert_message\":").Append(JsonString(status.LatestAlertMessage));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static string AlertsJson(bool cleared)
		{
			PerfMeterStatusSnapshot status = RuntimePerformanceMeter.GetStatus();
			PerfMeterAlertSnapshot[] alerts = RuntimePerformanceMeter.GetLatestAlerts();
			PerfMeterAlertHistorySnapshot history = RuntimePerformanceMeter.GetAlertHistory();
			StringBuilder builder = new StringBuilder(1024);
			builder.Append("{\"cleared\":").Append(JsonBool(cleared));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"collection_frame\":").Append(status.CollectionFrame);
			builder.Append(",\"active_alert_count\":").Append(status.ActiveAlertCount);
			builder.Append(",\"fired_alert_count\":").Append(status.FiredAlertCount);
			builder.Append(",\"latest_alert_rule_id\":").Append(JsonString(status.LatestAlertRuleId));
			builder.Append(",\"latest_alert_message\":").Append(JsonString(status.LatestAlertMessage));
			builder.Append(",\"history\":{");
			builder.Append("\"interval_id\":").Append(JsonString(history.IntervalId));
			builder.Append(",\"start_collection_frame\":").Append(history.StartCollectionFrame);
			builder.Append(",\"start_time_seconds\":").Append(JsonNumber(history.StartTimeSeconds));
			builder.Append(",\"started_utc\":").Append(JsonString(history.StartedUtc));
			builder.Append(",\"reset_reason\":").Append(JsonString(history.ResetReason.ToString()));
			builder.Append(",\"fired_count\":").Append(history.FiredCount);
			builder.Append(",\"steady_state_fired_count\":").Append(history.SteadyStateFiredCount);
			builder.Append(",\"lifecycle_fired_count\":").Append(history.LifecycleFiredCount);
			builder.Append(",\"capture_fired_count\":").Append(history.CaptureFiredCount);
			builder.Append('}');
			builder.Append(",\"latest_fired_alert\":");
			if (string.IsNullOrEmpty(history.LatestFiredAlert.RuleId))
			{
				builder.Append("null");
			}
			else
			{
				AppendAlert(builder, history.LatestFiredAlert);
			}
			builder.Append(",\"alerts\":[");
			for (int i = 0; i < alerts.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				AppendAlert(builder, alerts[i]);
			}

			builder.Append(']');
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendAlert(StringBuilder builder, PerfMeterAlertSnapshot alert)
		{
			builder.Append('{');
			builder.Append("\"rule_id\":").Append(JsonString(alert.RuleId));
			builder.Append(",\"metric\":").Append(JsonString(alert.Metric.ToString()));
			builder.Append(",\"comparison\":").Append(JsonString(alert.Comparison.ToString()));
			builder.Append(",\"threshold\":").Append(JsonNumber(alert.Threshold));
			builder.Append(",\"value\":").Append(JsonNumber(alert.Value));
			builder.Append(",\"collection_frame\":").Append(alert.CollectionFrame);
			builder.Append(",\"time_seconds\":").Append(JsonNumber(alert.TimeSeconds));
			builder.Append(",\"consecutive_frames\":").Append(alert.ConsecutiveFrames);
			builder.Append(",\"active\":").Append(JsonBool(alert.Active));
			builder.Append(",\"message\":").Append(JsonString(alert.Message));
			builder.Append(",\"classification\":").Append(JsonString(alert.Classification.ToString()));
			builder.Append(",\"capture_id\":").Append(JsonString(alert.CaptureId));
			builder.Append('}');
		}

		private static string CaptureCommandJson(string result, PerfMeterCaptureStatusSnapshot capture, PerfMeterCaptureBundleStatusSnapshot bundle)
		{
			StringBuilder builder = new StringBuilder(1024);
			builder.Append("{\"result\":").Append(JsonString(result));
			builder.Append(",\"capture\":");
			AppendCaptureStatus(builder, capture);
			builder.Append(",\"bundle\":");
			AppendCaptureBundleStatus(builder, bundle);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static string MemorySnapshotCommandJson(string result, PerfMeterMemorySnapshotStatusSnapshot status, PerfMeterCaptureBundleStatusSnapshot bundle)
		{
			StringBuilder builder = new StringBuilder(1024);
			builder.Append("{\"result\":").Append(JsonString(result));
			builder.Append(",\"memory_snapshot\":");
			AppendMemorySnapshotStatus(builder, status);
			builder.Append(",\"bundle\":");
			AppendCaptureBundleStatus(builder, bundle);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendCompatibilityStatus(StringBuilder builder, PerfMeterCompatibilityStatus status)
		{
			builder.Append("{\"import_compatible\":").Append(JsonBool(status.ImportCompatible));
			builder.Append(",\"core_runtime_compatible\":").Append(JsonBool(status.CoreRuntimeCompatible));
			builder.Append(",\"render_integration_compatible\":").Append(JsonBool(status.RenderIntegrationCompatible));
			builder.Append(",\"current_unity_version\":").Append(JsonString(status.CurrentUnityVersion));
			builder.Append(",\"current_pipeline_kind\":").Append(JsonString(status.CurrentPipelineKind.ToString()));
			builder.Append(",\"current_pipeline_package_name\":").Append(JsonString(status.CurrentPipelinePackageName));
			builder.Append(",\"current_pipeline_package_version\":").Append(JsonString(status.CurrentPipelinePackageVersion));
			builder.Append(",\"import_unity_version_floor\":").Append(JsonString(status.ImportUnityVersionFloor));
			builder.Append(",\"core_runtime_unity_version_floor\":").Append(JsonString(status.CoreRuntimeUnityVersionFloor));
			builder.Append(",\"render_integration_pipeline_package_version_floor\":").Append(JsonString(status.RenderIntegrationPipelinePackageVersionFloor));
			builder.Append(",\"import_reason\":").Append(JsonString(status.ImportReason));
			builder.Append(",\"core_runtime_reason\":").Append(JsonString(status.CoreRuntimeReason));
			builder.Append(",\"render_integration_reason\":").Append(JsonString(status.RenderIntegrationReason));
			builder.Append('}');
		}

		private static void AppendPlatformTelemetry(StringBuilder builder, PerfMeterPlatformTelemetrySnapshot telemetry)
		{
			builder.Append("{\"availability\":").Append(JsonString(telemetry.Availability.ToString()));
			builder.Append(",\"provider_id\":").Append(JsonString(telemetry.ProviderId));
			builder.Append(",\"provider_version\":").Append(JsonString(telemetry.ProviderVersion));
			builder.Append(",\"sample_time_seconds\":").Append(JsonNumber(telemetry.SampleTimeSeconds));
			builder.Append(",\"last_change_time_seconds\":").Append(JsonNumber(telemetry.LastChangeTimeSeconds));
			builder.Append(",\"thermal_warning_level_available\":").Append(JsonBool(telemetry.ThermalWarningLevelAvailable));
			builder.Append(",\"thermal_warning_level\":").Append(telemetry.ThermalWarningLevelAvailable ? JsonString(telemetry.ThermalWarningLevel.ToString()) : "null");
			builder.Append(",\"temperature_level_available\":").Append(JsonBool(telemetry.TemperatureLevelAvailable));
			builder.Append(",\"temperature_level\":").Append(telemetry.TemperatureLevelAvailable ? JsonNumber(telemetry.TemperatureLevel) : "null");
			builder.Append(",\"temperature_trend_available\":").Append(JsonBool(telemetry.TemperatureTrendAvailable));
			builder.Append(",\"temperature_trend\":").Append(telemetry.TemperatureTrendAvailable ? JsonNumber(telemetry.TemperatureTrend) : "null");
			builder.Append(",\"cpu_performance_level_available\":").Append(JsonBool(telemetry.CpuPerformanceLevelAvailable));
			builder.Append(",\"cpu_performance_level\":").Append(telemetry.CpuPerformanceLevelAvailable ? telemetry.CpuPerformanceLevel.ToString(CultureInfo.InvariantCulture) : "null");
			builder.Append(",\"gpu_performance_level_available\":").Append(JsonBool(telemetry.GpuPerformanceLevelAvailable));
			builder.Append(",\"gpu_performance_level\":").Append(telemetry.GpuPerformanceLevelAvailable ? telemetry.GpuPerformanceLevel.ToString(CultureInfo.InvariantCulture) : "null");
			builder.Append(",\"performance_bottleneck_available\":").Append(JsonBool(telemetry.PerformanceBottleneckAvailable));
			builder.Append(",\"performance_bottleneck\":").Append(telemetry.PerformanceBottleneckAvailable ? JsonString(telemetry.PerformanceBottleneck.ToString()) : "null");
			builder.Append(",\"warning\":").Append(JsonString(telemetry.Warning));
		}

		private static string CaptureExportJson(PerfMeterCaptureBundleExportResult result)
		{
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"success\":").Append(JsonBool(result.Success));
			builder.Append(",\"status\":").Append(JsonString(result.Status.ToString()));
			builder.Append(",\"relative_path\":").Append(JsonString(result.RelativePath));
			builder.Append(",\"error\":").Append(JsonString(result.Error));
			builder.Append(",\"bundle\":");
			AppendCaptureBundleStatus(builder, result.Bundle);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendCaptureStatus(StringBuilder builder, PerfMeterCaptureStatusSnapshot status)
		{
			builder.Append("{\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"capture_id\":").Append(JsonString(status.CaptureId));
			builder.Append(",\"requested_tool\":").Append(JsonString(status.Tool.ToString()));
			builder.Append(",\"requested_pre_roll_frames\":").Append(status.RequestedPreRollFrames);
			builder.Append(",\"requested_capture_frames\":").Append(status.RequestedCaptureFrames);
			builder.Append(",\"requested_post_roll_frames\":").Append(status.RequestedPostRollFrames);
			builder.Append(",\"completed_pre_roll_frames\":").Append(status.CompletedPreRollFrames);
			builder.Append(",\"completed_capture_frames\":").Append(status.CompletedCaptureFrames);
			builder.Append(",\"completed_post_roll_frames\":").Append(status.CompletedPostRollFrames);
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			builder.Append('}');
		}

		private static void AppendCaptureBundleStatus(StringBuilder builder, PerfMeterCaptureBundleStatusSnapshot status)
		{
			builder.Append("{\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"bundle_id\":").Append(JsonString(status.BundleId));
			builder.Append(",\"capture_id\":").Append(JsonString(status.CaptureId));
			builder.Append(",\"capture_state\":").Append(JsonString(status.CaptureState.ToString()));
			builder.Append(",\"requested_tool\":").Append(JsonString(status.RequestedTool.ToString()));
			builder.Append(",\"baseline_sample_count\":").Append(status.BaselineSampleCount);
			builder.Append(",\"capture_sample_count\":").Append(status.CaptureSampleCount);
			builder.Append(",\"dropped_capture_sample_count\":").Append(status.DroppedCaptureSampleCount);
			builder.Append(",\"alert_event_count\":").Append(status.AlertEventCount);
			builder.Append(",\"alert_events_truncated\":").Append(JsonBool(status.AlertEventsTruncated));
			builder.Append(",\"screenshot_state\":").Append(JsonString(status.ScreenshotState.ToString()));
			builder.Append(",\"external_artifact_state\":").Append(JsonString(status.ExternalArtifactState.ToString()));
			builder.Append(",\"memory_snapshot_state\":").Append(JsonString(status.MemorySnapshotState.ToString()));
			builder.Append(",\"committed_relative_path\":").Append(JsonString(status.CommittedRelativePath));
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			builder.Append('}');
		}

		private static void AppendMemorySnapshotStatus(StringBuilder builder, PerfMeterMemorySnapshotStatusSnapshot status)
		{
			builder.Append("{\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"capture_id\":").Append(JsonString(status.CaptureId));
			builder.Append(",\"trigger\":").Append(JsonString(status.Trigger.ToString()));
			builder.Append(",\"requested_capture_flags\":").Append(JsonString(status.RequestedCaptureFlags.ToString()));
			builder.Append(",\"backend_id\":").Append(JsonString(status.BackendId));
			builder.Append(",\"backend_version\":").Append(JsonString(status.BackendVersion));
			builder.Append(",\"started_time_seconds\":").Append(JsonNumber(status.StartedTimeSeconds));
			builder.Append(",\"completed_time_seconds\":").Append(JsonNumber(status.CompletedTimeSeconds));
			builder.Append(",\"artifact_size_bytes\":").Append(status.ArtifactSizeBytes);
			builder.Append(",\"cooldown_remaining_seconds\":").Append(JsonNumber(status.CooldownRemainingSeconds));
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			builder.Append('}');
		}

		private static void AppendMemorySnapshotTriggers(StringBuilder builder, PerfMeterMemorySnapshotTriggerOptions options)
		{
			builder.Append("{\"enabled\":").Append(JsonBool(options.Enabled));
			builder.Append(",\"system_memory_threshold_bytes\":").Append(options.SystemMemoryThresholdBytes);
			builder.Append(",\"leak_growth_threshold_bytes\":").Append(options.LeakGrowthThresholdBytes);
			builder.Append(",\"leak_window_frames\":").Append(options.LeakWindowFrames);
			builder.Append(",\"capture_flags\":").Append(JsonString(options.CaptureFlags.ToString()));
			builder.Append(",\"minimum_free_disk_bytes\":").Append(options.MinimumFreeDiskBytes);
			builder.Append(",\"cooldown_seconds\":").Append(JsonNumber(options.CooldownSeconds));
			builder.Append('}');
		}

		private static string SessionCommandJson(bool success, string path, string error, string status, PerfMeterSessionSummarySnapshot summary)
		{
			StringBuilder builder = new StringBuilder(1024);
			builder.Append("{\"success\":").Append(JsonBool(success));
			builder.Append(",\"path\":").Append(JsonString(path));
			builder.Append(",\"error\":").Append(JsonString(error));
			builder.Append(",\"status\":").Append(JsonString(status));
			builder.Append(",\"summary\":");
			AppendSessionSummary(builder, summary);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendSessionSummary(StringBuilder builder, PerfMeterSessionSummarySnapshot summary)
		{
			builder.Append('{');
			builder.Append("\"session_id\":").Append(JsonString(summary.SessionId));
			builder.Append(",\"state\":").Append(JsonString(summary.State.ToString()));
			builder.Append(",\"sample_count\":").Append(summary.SampleCount);
			builder.Append(",\"dropped_sample_count\":").Append(summary.DroppedSampleCount);
			builder.Append(",\"first_frame\":").Append(summary.FirstFrame);
			builder.Append(",\"last_frame\":").Append(summary.LastFrame);
			builder.Append(",\"duration_seconds\":").Append(JsonNumber(summary.DurationSeconds));
			builder.Append(",\"average_frame_time_ms\":").Append(JsonNumber(summary.AverageFrameTimeMs));
			builder.Append(",\"min_frame_time_ms\":").Append(JsonNumber(summary.MinFrameTimeMs));
			builder.Append(",\"max_frame_time_ms\":").Append(JsonNumber(summary.MaxFrameTimeMs));
			builder.Append(",\"average_fps\":").Append(JsonNumber(summary.AverageFps));
			builder.Append(",\"min_fps\":").Append(JsonNumber(summary.MinFps));
			builder.Append(",\"max_fps\":").Append(JsonNumber(summary.MaxFps));
			builder.Append(",\"frame_spike_count\":").Append(summary.FrameSpikeCount);
			builder.Append(",\"severe_frame_spike_count\":").Append(summary.SevereFrameSpikeCount);
			builder.Append(",\"focus_loss_count\":").Append(summary.FocusLossCount);
			builder.Append(",\"pause_count\":").Append(summary.PauseCount);
			builder.Append(",\"focus_paused_duration_seconds\":").Append(JsonNumber(summary.FocusPausedDurationSeconds));
			builder.Append(",\"warning\":").Append(JsonString(summary.Warning));
			builder.Append(",\"start_scene_name\":").Append(JsonString(summary.StartSceneName));
			builder.Append(",\"last_scene_name\":").Append(JsonString(summary.LastSceneName));
			builder.Append(",\"worst_frame\":");
			AppendWorstFrame(builder, summary.WorstFrame);
			builder.Append(",\"current_scene_worst_frame\":");
			AppendWorstFrame(builder, summary.CurrentSceneWorstFrame);
			builder.Append(",\"whole_run\":");
			AppendSessionScopeSummary(builder, summary.WholeRun);
			builder.Append(",\"current_scene\":");
			AppendSessionScopeSummary(builder, summary.CurrentScene);
			builder.Append(",\"options\":{");
			builder.Append("\"warmup_frames\":").Append(summary.Options.WarmupFrames);
			builder.Append(",\"warmup_seconds\":").Append(JsonNumber(summary.Options.WarmupSeconds));
			builder.Append(",\"sample_interval_seconds\":").Append(JsonNumber(summary.Options.SampleIntervalSeconds));
			builder.Append(",\"max_samples\":").Append(summary.Options.MaxSamples);
			builder.Append(",\"reset_on_scene_load\":").Append(JsonBool(summary.Options.ResetOnSceneLoad));
			builder.Append(",\"scene_load_ignore_frames\":").Append(summary.Options.SceneLoadIgnoreFrames);
			builder.Append(",\"scene_load_ignore_seconds\":").Append(JsonNumber(summary.Options.SceneLoadIgnoreSeconds));
			builder.Append("}}");
		}

		private static void AppendSessionScopeSummary(StringBuilder builder, PerfMeterSessionScopeSummarySnapshot scope)
		{
			builder.Append('{');
			builder.Append("\"scene_name\":").Append(JsonString(scope.SceneName));
			builder.Append(",\"sample_count\":").Append(scope.SampleCount);
			builder.Append(",\"first_frame\":").Append(scope.FirstFrame);
			builder.Append(",\"last_frame\":").Append(scope.LastFrame);
			builder.Append(",\"start_time_seconds\":").Append(JsonNumber(scope.StartTimeSeconds));
			builder.Append(",\"last_sample_time_seconds\":").Append(JsonNumber(scope.LastSampleTimeSeconds));
			builder.Append(",\"duration_seconds\":").Append(JsonNumber(scope.DurationSeconds));
			builder.Append(",\"average_frame_time_ms\":").Append(JsonNumber(scope.AverageFrameTimeMs));
			builder.Append(",\"min_frame_time_ms\":").Append(JsonNumber(scope.MinFrameTimeMs));
			builder.Append(",\"max_frame_time_ms\":").Append(JsonNumber(scope.MaxFrameTimeMs));
			builder.Append(",\"average_fps\":").Append(JsonNumber(scope.AverageFps));
			builder.Append(",\"min_fps\":").Append(JsonNumber(scope.MinFps));
			builder.Append(",\"max_fps\":").Append(JsonNumber(scope.MaxFps));
			builder.Append(",\"gpu_bound_sample_count\":").Append(scope.GpuBoundSampleCount);
			builder.Append(",\"cpu_main_thread_bound_sample_count\":").Append(scope.CpuMainThreadBoundSampleCount);
			builder.Append(",\"cpu_render_thread_bound_sample_count\":").Append(scope.CpuRenderThreadBoundSampleCount);
			builder.Append(",\"present_limited_sample_count\":").Append(scope.PresentLimitedSampleCount);
			builder.Append(",\"frame_spike_count\":").Append(scope.FrameSpikeCount);
			builder.Append(",\"severe_frame_spike_count\":").Append(scope.SevereFrameSpikeCount);
			builder.Append(",\"worst_frame\":");
			AppendWorstFrame(builder, scope.WorstFrame);
			builder.Append('}');
		}

		private static void AppendWorstFrame(StringBuilder builder, PerfMeterSessionWorstFrameSnapshot worstFrame)
		{
			builder.Append('{');
			builder.Append("\"available\":").Append(JsonBool(worstFrame.IsAvailable));
			builder.Append(",\"collection_frame\":").Append(worstFrame.CollectionFrame);
			builder.Append(",\"time_seconds\":").Append(JsonNumber(worstFrame.CollectionTimeSeconds));
			builder.Append(",\"scene_name\":").Append(JsonString(worstFrame.SceneName));
			builder.Append(",\"frame_time_ms\":").Append(JsonNumber(worstFrame.FrameTimeMs));
			builder.Append(",\"fps\":").Append(JsonNumber(worstFrame.Fps));
			builder.Append(",\"bottleneck\":").Append(JsonString(worstFrame.Bottleneck.ToString()));
			builder.Append('}');
		}

		private static string MetricsJson(PerfMeterMetricsSnapshot metrics)
		{
			PerfMeterCustomMetricSnapshot[] customMetrics = RuntimePerformanceMeter.GetCustomMetrics();
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"state\":").Append(JsonString(metrics.State.ToString()));
			builder.Append(",\"availability\":").Append(JsonString(metrics.Availability.ToString()));
			builder.Append(",\"collection_frame\":").Append(metrics.CollectionFrame);
			builder.Append(",\"bottleneck\":").Append(JsonString(metrics.Bottleneck.ToString()));
			builder.Append(",\"frame_budget_ms\":").Append(JsonNumber(metrics.FrameBudgetMs));
			builder.Append(",\"gpu_frame_time_available\":").Append(JsonBool(metrics.GpuFrameTimeAvailable));
			builder.Append(",\"frame_sample_count\":").Append(metrics.FrameSampleCount);
			builder.Append(",\"gpu_valid_sample_count\":").Append(metrics.GpuValidSampleCount);
			builder.Append(",\"average_fps\":").Append(JsonNumber(metrics.AverageFps));
			builder.Append(",\"one_percent_low_fps\":").Append(JsonNumber(metrics.OnePercentLowFps));
			builder.Append(",\"point_one_percent_low_fps\":").Append(JsonNumber(metrics.PointOnePercentLowFps));
			builder.Append(",\"frame_spike_count\":").Append(metrics.FrameSpikeCount);
			builder.Append(",\"severe_frame_spike_count\":").Append(metrics.SevereFrameSpikeCount);
			builder.Append(",\"cpu_frame_time_ms\":").Append(JsonNumber(metrics.CpuFrameTimeMs));
			builder.Append(",\"cpu_main_thread_frame_time_ms\":").Append(JsonNumber(metrics.CpuMainThreadFrameTimeMs));
			builder.Append(",\"cpu_render_thread_frame_time_ms\":").Append(JsonNumber(metrics.CpuRenderThreadFrameTimeMs));
			builder.Append(",\"cpu_main_thread_present_wait_time_ms\":").Append(JsonNumber(metrics.CpuMainThreadPresentWaitTimeMs));
			builder.Append(",\"gpu_frame_time_ms\":").Append(JsonNumber(metrics.GpuFrameTimeMs));
			builder.Append(",\"draw_calls\":").Append(metrics.DrawCalls);
			builder.Append(",\"set_pass_calls\":").Append(metrics.SetPassCalls);
			builder.Append(",\"batches\":").Append(metrics.Batches);
			builder.Append(",\"vertices\":").Append(metrics.Vertices);
			builder.Append(",\"srp_batcher_instances\":").Append(metrics.SrpBatcherInstances);
			builder.Append(",\"brg_draw_calls\":").Append(metrics.BrgDrawCalls);
			builder.Append(",\"brg_instances\":").Append(metrics.BrgInstances);
			builder.Append(",\"index_buffer_upload_in_frame_bytes\":").Append(metrics.IndexBufferUploadInFrameBytes);
			builder.Append(",\"system_used_memory_bytes\":").Append(metrics.SystemUsedMemoryBytes);
			builder.Append(",\"gc_reserved_memory_bytes\":").Append(metrics.GcReservedMemoryBytes);
			builder.Append(",\"gpu_memory_bytes\":").Append(metrics.GpuMemoryBytes);
			builder.Append(",\"shader_gpu_program_creation_value\":").Append(metrics.ShaderGpuProgramCreationValue);
			builder.Append(",\"graphics_pipeline_creation_value\":").Append(metrics.GraphicsPipelineCreationValue);
			builder.Append(",\"graphics_profiler_catalog_revision\":").Append(metrics.ProfilerMetricCatalogRevision);
			builder.Append(",\"shader_gpu_program_creation_capability\":");
			AppendProfilerMetricCapability(builder, metrics.ShaderGpuProgramCreationCapability);
			builder.Append(",\"graphics_pipeline_creation_capability\":");
			AppendProfilerMetricCapability(builder, metrics.GraphicsPipelineCreationCapability);
			builder.Append(",\"overdraw_ratio\":").Append(JsonNumber(metrics.OverdrawRatio));
			builder.Append(",\"overdraw_state\":").Append(JsonString(metrics.OverdrawState.ToString()));
			builder.Append(",\"overdraw_progress\":").Append(JsonNumber(metrics.OverdrawProgress));
			AppendCustomMetrics(builder, customMetrics);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static string ProfilerCapabilitiesJson(PerfMeterProfilerMetricCatalogSnapshot catalog)
		{
			PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = catalog.Capabilities;
			StringBuilder builder = new StringBuilder(2048);
			builder.Append("{\"state\":").Append(JsonString(catalog.State.ToString()));
			builder.Append(",\"revision\":").Append(catalog.Revision);
			builder.Append(",\"discovery_count\":").Append(catalog.DiscoveryCount);
			builder.Append(",\"last_error\":").Append(JsonString(catalog.LastError));
			builder.Append(",\"metrics\":[");
			for (int i = 0; i < capabilities.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				AppendProfilerMetricCapability(builder, capabilities[i]);
			}

			builder.Append("]}");
			return builder.ToString();
		}

		private static void AppendProfilerMetricCapability(StringBuilder builder, PerfMeterProfilerMetricCapabilitySnapshot capability)
		{
			builder.Append("{\"semantic\":").Append(JsonString(capability.Semantic.ToString()));
			builder.Append(",\"sample_state\":").Append(JsonString(capability.SampleState.ToString()));
			builder.Append(",\"resolution\":").Append(JsonString(capability.Resolution.ToString()));
			builder.Append(",\"category\":").Append(JsonString(capability.Category));
			builder.Append(",\"resolved_recorder_names\":").Append(JsonString(capability.ResolvedRecorderNames));
			builder.Append(",\"unit\":").Append(JsonString(capability.Unit));
			builder.Append(",\"data_type\":").Append(JsonString(capability.DataType));
			builder.Append(",\"resolved_component_count\":").Append(capability.ResolvedComponentCount);
			builder.Append(",\"sampled_component_count\":").Append(capability.SampledComponentCount);
			builder.Append('}');
		}

		private static string GraphicsStateCollectionCommandJson(string result, PerfMeterGraphicsStateCollectionStatusSnapshot status)
		{
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"result\":").Append(JsonString(result));
			builder.Append(",\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"capture_id\":").Append(JsonString(status.CaptureId));
			builder.Append(",\"requested_trace_frames\":").Append(status.RequestedTraceFrames);
			builder.Append(",\"completed_trace_frames\":").Append(status.CompletedTraceFrames);
			builder.Append(",\"backend_id\":").Append(JsonString(status.BackendId));
			builder.Append(",\"backend_version\":").Append(JsonString(status.BackendVersion));
			builder.Append(",\"artifact_relative_path\":").Append(JsonString(status.ArtifactRelativePath));
			builder.Append(",\"artifact_size_bytes\":").Append(status.ArtifactSizeBytes);
			builder.Append(",\"total_graphics_state_count\":").Append(status.TotalGraphicsStateCount);
			builder.Append(",\"variant_count\":").Append(status.VariantCount);
			builder.Append(",\"completed_warmup_count\":").Append(status.CompletedWarmupCount);
			builder.Append(",\"is_warmed_up\":").Append(JsonBool(status.IsWarmedUp));
			builder.Append(",\"is_busy\":").Append(JsonBool(status.IsBusy));
			builder.Append(",\"has_pending_cleanup\":").Append(JsonBool(status.HasPendingCleanup));
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendCustomMetrics(StringBuilder builder, PerfMeterCustomMetricSnapshot[] customMetrics)
		{
			builder.Append(",\"custom_metrics\":[");
			for (int i = 0; i < customMetrics.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				PerfMeterCustomMetricSnapshot metric = customMetrics[i];
				builder.Append("{\"id\":").Append(JsonString(metric.Id));
				builder.Append(",\"name\":").Append(JsonString(metric.Name));
				builder.Append(",\"category\":").Append(JsonString(metric.Category));
				builder.Append(",\"unit\":").Append(JsonString(metric.Unit));
				builder.Append(",\"value\":").Append(JsonNumber(metric.Value));
				builder.Append(",\"available\":").Append(JsonBool(metric.Available));
				builder.Append(",\"warning\":").Append(JsonString(metric.Warning));
				builder.Append('}');
			}

			builder.Append(']');
		}

		private static void AppendSelfOverhead(StringBuilder builder, PerfMeterSelfOverheadSnapshot selfOverhead)
		{
			builder.Append(",\"self_overhead\":{");
			builder.Append("\"state\":").Append(JsonString(selfOverhead.State.ToString()));
			builder.Append(",\"cpu_timing_available\":").Append(JsonBool(selfOverhead.CpuTimingAvailable));
			builder.Append(",\"gpu_timing_availability\":").Append(JsonString(selfOverhead.GpuTimingAvailability.ToString()));
			builder.Append(",\"has_budget_violation\":").Append(JsonBool(selfOverhead.HasBudgetViolation));
			builder.Append(",\"collector\":");
			AppendSelfOverheadComponent(builder, selfOverhead.Collector);
			builder.Append(",\"custom_metric_providers\":");
			AppendSelfOverheadComponent(builder, selfOverhead.CustomMetricProviders);
			builder.Append(",\"cpu_core_provider\":");
			AppendSelfOverheadComponent(builder, selfOverhead.CpuCoreProvider);
			builder.Append(",\"overlay\":");
			AppendSelfOverheadComponent(builder, selfOverhead.Overlay);
			builder.Append(",\"urp_render_integration\":");
			AppendSelfOverheadComponent(builder, selfOverhead.UrpRenderIntegration);
			builder.Append(",\"hdrp_render_integration\":");
			AppendSelfOverheadComponent(builder, selfOverhead.HdrpRenderIntegration);
			builder.Append('}');
		}

		private static void AppendSelfOverheadComponent(StringBuilder builder, PerfMeterSelfOverheadComponentSnapshot component)
		{
			builder.Append("{\"component\":").Append(JsonString(component.Component.ToString()));
			builder.Append(",\"state\":").Append(JsonString(component.State.ToString()));
			builder.Append(",\"window_frame_count\":").Append(component.WindowFrameCount);
			builder.Append(",\"invocation_count\":").Append(component.InvocationCount);
			builder.Append(",\"average_cpu_time_ms\":").Append(JsonNumber(component.AverageCpuTimeMs));
			builder.Append(",\"max_cpu_time_ms\":").Append(JsonNumber(component.MaxCpuTimeMs));
			builder.Append(",\"allocated_bytes\":").Append(component.AllocatedBytes);
			builder.Append(",\"average_allocated_bytes\":").Append(JsonNumber(component.AverageAllocatedBytes));
			builder.Append(",\"cpu_budget_ms\":").Append(JsonNumber(component.CpuBudgetMs));
			builder.Append(",\"allocation_budget_bytes\":").Append(component.AllocationBudgetBytes);
			builder.Append(",\"cpu_budget_state\":").Append(JsonString(component.CpuBudgetState.ToString()));
			builder.Append(",\"allocation_budget_state\":").Append(JsonString(component.AllocationBudgetState.ToString()));
			builder.Append('}');
		}

		private static void AppendOverlayModules(StringBuilder builder, PerfMeterOverlayModule modules)
		{
			PerfMeterOverlayModule normalized = modules == PerfMeterOverlayModule.None ? PerfMeterOverlayModule.All : modules;
			PerfMeterOverlayModule[] values =
			{
				PerfMeterOverlayModule.Fps,
				PerfMeterOverlayModule.Timing,
				PerfMeterOverlayModule.Graphs,
				PerfMeterOverlayModule.Rendering,
				PerfMeterOverlayModule.SrpBatcher,
				PerfMeterOverlayModule.Brg,
				PerfMeterOverlayModule.Uploads,
				PerfMeterOverlayModule.Memory,
				PerfMeterOverlayModule.Gc,
				PerfMeterOverlayModule.GpuMemory,
				PerfMeterOverlayModule.Overdraw,
				PerfMeterOverlayModule.Heatmap,
				PerfMeterOverlayModule.Warnings,
				PerfMeterOverlayModule.CustomMetrics,
				PerfMeterOverlayModule.CpuCores,
				PerfMeterOverlayModule.CpuCoreBars,
				PerfMeterOverlayModule.CpuCoreGraphs
			};
			builder.Append('[');
			bool needsComma = false;
			for (int i = 0; i < values.Length; i++)
			{
				if ((normalized & values[i]) == 0)
				{
					continue;
				}

				if (needsComma)
				{
					builder.Append(',');
				}

				builder.Append(JsonString(values[i].ToString()));
				needsComma = true;
			}

			builder.Append(']');
		}

		private static string DeviceInfoJson(PerfMeterDeviceSnapshot device)
		{
			StringBuilder builder = new StringBuilder(1536);
			builder.Append("{\"schema_version\":1");
			builder.Append(",\"unity_version\":").Append(JsonString(device.UnityVersion));
			builder.Append(",\"application_platform\":").Append(JsonString(device.ApplicationPlatform.ToString()));
			builder.Append(",\"is_editor\":").Append(JsonBool(device.IsEditor));
			builder.Append(",\"operating_system\":").Append(JsonString(device.OperatingSystem));
			builder.Append(",\"device_model\":").Append(JsonString(device.DeviceModel));
			builder.Append(",\"device_type\":").Append(JsonString(device.DeviceType.ToString()));
			builder.Append(",\"processor_type\":").Append(JsonString(device.ProcessorType));
			builder.Append(",\"processor_count\":").Append(device.ProcessorCount);
			builder.Append(",\"processor_frequency_mhz\":").Append(device.ProcessorFrequencyMhz);
			builder.Append(",\"system_memory_size_mb\":").Append(device.SystemMemorySizeMb);
			builder.Append(",\"graphics_device_type\":").Append(JsonString(device.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(device.GraphicsDeviceName));
			builder.Append(",\"graphics_device_vendor\":").Append(JsonString(device.GraphicsDeviceVendor));
			builder.Append(",\"graphics_device_version\":").Append(JsonString(device.GraphicsDeviceVersion));
			builder.Append(",\"graphics_memory_size_mb\":").Append(device.GraphicsMemorySizeMb);
			builder.Append(",\"graphics_shader_level\":").Append(device.GraphicsShaderLevel);
			builder.Append(",\"graphics_multi_threaded\":").Append(JsonBool(device.GraphicsMultiThreaded));
			builder.Append(",\"max_texture_size\":").Append(device.MaxTextureSize);
			builder.Append(",\"supports_compute_shaders\":").Append(JsonBool(device.SupportsComputeShaders));
			builder.Append(",\"supports_async_gpu_readback\":").Append(JsonBool(device.SupportsAsyncGpuReadback));
			builder.Append(",\"supports_instancing\":").Append(JsonBool(device.SupportsInstancing));
			builder.Append(",\"supports_graphics_fence\":").Append(JsonBool(device.SupportsGraphicsFence));
			builder.Append(",\"screen_width\":").Append(device.ScreenWidth);
			builder.Append(",\"screen_height\":").Append(device.ScreenHeight);
			builder.Append(",\"current_resolution_width\":").Append(device.CurrentResolutionWidth);
			builder.Append(",\"current_resolution_height\":").Append(device.CurrentResolutionHeight);
			builder.Append(",\"current_refresh_rate_numerator\":").Append(device.CurrentRefreshRateNumerator);
			builder.Append(",\"current_refresh_rate_denominator\":").Append(device.CurrentRefreshRateDenominator);
			builder.Append(",\"current_refresh_rate_hz\":").Append(JsonNumber(device.CurrentRefreshRateHz));
			builder.Append(",\"dpi\":").Append(JsonNumber(device.Dpi));
			builder.Append(",\"full_screen\":").Append(JsonBool(device.FullScreen));
			builder.Append(",\"full_screen_mode\":").Append(JsonString(device.FullScreenMode.ToString()));
			builder.Append(",\"main_window_position_available\":").Append(JsonBool(device.MainWindowPositionAvailable));
			builder.Append(",\"main_window_position_x\":").Append(device.MainWindowPositionX);
			builder.Append(",\"main_window_position_y\":").Append(device.MainWindowPositionY);
			builder.Append(",\"display_layout_available\":").Append(JsonBool(device.DisplayLayoutAvailable));
			builder.Append(",\"display_layout_warning\":").Append(JsonString(device.DisplayLayoutWarning));
			AppendRenderPipelineInfo(builder, device);
			builder.Append(",\"frame_timing_stats_enabled\":").Append(JsonBool(PlayerSettings.enableFrameTimingStats));
			builder.Append(",\"active_build_target\":").Append(JsonString(EditorUserBuildSettings.activeBuildTarget.ToString()));
			builder.Append(",\"active_build_target_group\":").Append(JsonString(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString()));
			builder.Append(",\"target_frame_rate\":").Append(Application.targetFrameRate);
			builder.Append(",\"v_sync_count\":").Append(QualitySettings.vSyncCount);
			AppendDisplays(builder, device.Displays);
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendDisplays(StringBuilder builder, PerfMeterDisplaySnapshot[] displays)
		{
			builder.Append(",\"displays\":[");
			for (int i = 0; i < displays.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				PerfMeterDisplaySnapshot display = displays[i];
				builder.Append("{\"index\":").Append(display.Index);
				builder.Append(",\"name\":").Append(JsonString(display.Name));
				builder.Append(",\"width\":").Append(display.Width);
				builder.Append(",\"height\":").Append(display.Height);
				builder.Append(",\"work_area_x\":").Append(display.WorkAreaX);
				builder.Append(",\"work_area_y\":").Append(display.WorkAreaY);
				builder.Append(",\"work_area_width\":").Append(display.WorkAreaWidth);
				builder.Append(",\"work_area_height\":").Append(display.WorkAreaHeight);
				builder.Append(",\"refresh_rate_numerator\":").Append(display.RefreshRateNumerator);
				builder.Append(",\"refresh_rate_denominator\":").Append(display.RefreshRateDenominator);
				builder.Append(",\"refresh_rate_hz\":").Append(JsonNumber(display.RefreshRateHz));
				builder.Append(",\"is_main_window_display\":").Append(JsonBool(display.IsMainWindowDisplay));
				builder.Append(",\"is_fallback\":").Append(JsonBool(display.IsFallback));
				builder.Append('}');
			}

			builder.Append(']');
		}

		private static void AppendRenderPipelineInfo(StringBuilder builder, PerfMeterDeviceSnapshot device)
		{
			UnityEngine.Rendering.RenderPipelineAsset graphicsAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
			UnityEngine.Rendering.RenderPipelineAsset qualityAsset = QualitySettings.renderPipeline;
			builder.Append(",\"render_pipeline\":").Append(JsonString(device.RenderPipeline.ToString()));
			builder.Append(",\"render_pipeline_asset_name\":").Append(JsonString(device.RenderPipelineAssetName));
			builder.Append(",\"render_pipeline_asset_type\":").Append(JsonString(device.RenderPipelineAssetType));
			builder.Append(",\"render_pipeline_runtime_type\":").Append(JsonString(device.RenderPipelineRuntimeType));
			builder.Append(",\"render_pipeline_asset\":").Append(JsonString(graphicsAsset != null ? graphicsAsset.name : string.Empty));
			builder.Append(",\"render_pipeline_type\":").Append(JsonString(graphicsAsset != null ? graphicsAsset.GetType().FullName : string.Empty));
			builder.Append(",\"quality_level\":").Append(QualitySettings.GetQualityLevel());
			builder.Append(",\"quality_name\":").Append(JsonString(QualitySettings.names.Length > QualitySettings.GetQualityLevel() ? QualitySettings.names[QualitySettings.GetQualityLevel()] : string.Empty));
			builder.Append(",\"quality_render_pipeline_asset\":").Append(JsonString(qualityAsset != null ? qualityAsset.name : string.Empty));
			builder.Append(",\"quality_render_pipeline_type\":").Append(JsonString(qualityAsset != null ? qualityAsset.GetType().FullName : string.Empty));
		}

		private static string CameraSnapshotJson(PerfMeterCameraSnapshot camera)
		{
			StringBuilder builder = new StringBuilder(1536);
		#if UNITY_6000_4_OR_NEWER
			builder.Append("{\"schema_version\":2");
		#else
			builder.Append("{\"schema_version\":1");
		#endif
			builder.Append(",\"is_available\":").Append(JsonBool(camera.IsAvailable));
			builder.Append(",\"warning\":").Append(JsonString(camera.Warning));
			builder.Append(",\"source\":").Append(JsonString(camera.Source.ToString()));
			builder.Append(",\"detected_game_camera_count\":").Append(camera.DetectedGameCameraCount);
			builder.Append(",\"camera_name\":").Append(JsonString(camera.CameraName));
		#if UNITY_6000_4_OR_NEWER
			builder.Append(",\"camera_entity_id\":").Append(JsonString(camera.CameraEntityId.ToString(CultureInfo.InvariantCulture)));
		#else
			builder.Append(",\"camera_instance_id\":").Append(camera.CameraInstanceId);
		#endif
			builder.Append(",\"scene_name\":").Append(JsonString(camera.SceneName));
			builder.Append(",\"scene_path\":").Append(JsonString(camera.ScenePath));
			builder.Append(",\"enabled\":").Append(JsonBool(camera.Enabled));
			builder.Append(",\"is_active_and_enabled\":").Append(JsonBool(camera.IsActiveAndEnabled));
			builder.Append(",\"camera_type\":").Append(JsonString(camera.CameraType.ToString()));
			builder.Append(",\"projection\":").Append(JsonString(camera.Projection.ToString()));
			AppendVector3(builder, "position", camera.Position);
			AppendQuaternion(builder, "rotation", camera.Rotation);
			AppendVector3(builder, "euler_angles", camera.EulerAngles);
			AppendVector3(builder, "forward", camera.Forward);
			AppendVector3(builder, "up", camera.Up);
			builder.Append(",\"field_of_view\":").Append(JsonNumber(camera.FieldOfView));
			builder.Append(",\"orthographic_size\":").Append(JsonNumber(camera.OrthographicSize));
			builder.Append(",\"near_clip_plane\":").Append(JsonNumber(camera.NearClipPlane));
			builder.Append(",\"far_clip_plane\":").Append(JsonNumber(camera.FarClipPlane));
			builder.Append(",\"aspect\":").Append(JsonNumber(camera.Aspect));
			AppendRect(builder, "pixel_rect", camera.PixelRect);
			builder.Append(",\"target_display\":").Append(camera.TargetDisplay);
			builder.Append(",\"depth\":").Append(JsonNumber(camera.Depth));
			builder.Append(",\"clear_flags\":").Append(JsonString(camera.ClearFlags.ToString()));
			builder.Append(",\"culling_mask\":").Append(camera.CullingMask);
			builder.Append(",\"allow_hdr\":").Append(JsonBool(camera.AllowHdr));
			builder.Append(",\"allow_msaa\":").Append(JsonBool(camera.AllowMsaa));
			builder.Append(",\"actual_rendering_path\":").Append(JsonString(camera.ActualRenderingPath.ToString()));
			builder.Append(",\"has_urp_additional_camera_data\":").Append(JsonBool(camera.HasUniversalAdditionalCameraData));
			builder.Append(",\"urp_render_type\":").Append(JsonString(camera.UrpRenderType));
			builder.Append(",\"urp_render_post_processing\":").Append(JsonBool(camera.UrpRenderPostProcessing));
			builder.Append(",\"urp_antialiasing\":").Append(JsonString(camera.UrpAntialiasing));
			builder.Append(",\"urp_antialiasing_quality\":").Append(JsonString(camera.UrpAntialiasingQuality));
			builder.Append(",\"urp_stop_nan\":").Append(JsonBool(camera.UrpStopNaN));
			builder.Append(",\"urp_render_shadows\":").Append(JsonBool(camera.UrpRenderShadows));
			builder.Append(",\"urp_clear_depth\":").Append(JsonBool(camera.UrpClearDepth));
			builder.Append(",\"urp_requires_depth_option\":").Append(JsonString(camera.UrpRequiresDepthOption));
			builder.Append(",\"urp_requires_color_option\":").Append(JsonString(camera.UrpRequiresColorOption));
			builder.Append(",\"urp_requires_depth_texture\":").Append(JsonBool(camera.UrpRequiresDepthTexture));
			builder.Append(",\"urp_requires_color_texture\":").Append(JsonBool(camera.UrpRequiresColorTexture));
			builder.Append(",\"has_hdrp_additional_camera_data\":").Append(JsonBool(camera.HasHighDefinitionAdditionalCameraData));
			builder.Append(",\"hdrp_clear_color_mode\":").Append(JsonString(camera.HdrpClearColorMode));
			builder.Append(",\"hdrp_clear_depth\":").Append(JsonBool(camera.HdrpClearDepth));
			builder.Append(",\"hdrp_antialiasing\":").Append(JsonString(camera.HdrpAntialiasing));
			builder.Append(",\"hdrp_smaa_quality\":").Append(JsonString(camera.HdrpSmaaQuality));
			builder.Append(",\"hdrp_stop_nan\":").Append(JsonBool(camera.HdrpStopNaN));
			builder.Append(",\"hdrp_dithering\":").Append(JsonBool(camera.HdrpDithering));
			builder.Append(",\"hdrp_allow_dynamic_resolution\":").Append(JsonBool(camera.HdrpAllowDynamicResolution));
			builder.Append(",\"hdrp_custom_rendering_settings\":").Append(JsonBool(camera.HdrpCustomRenderingSettings));
			builder.Append(",\"hdrp_volume_layer_mask\":").Append(camera.HdrpVolumeLayerMask);
			builder.Append(",\"hdrp_has_volume_anchor_override\":").Append(JsonBool(camera.HdrpHasVolumeAnchorOverride));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static string RenderGraphSnapshotJson(PerfMeterRenderGraphSnapshot snapshot)
		{
			StringBuilder builder = new StringBuilder(512);
			builder.Append("{\"schema_version\":1");
			builder.Append(",\"is_available\":").Append(JsonBool(snapshot.IsAvailable));
			builder.Append(",\"availability\":").Append(JsonString(snapshot.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(snapshot.State.ToString()));
			builder.Append(",\"last_frame\":").Append(snapshot.LastFrame);
			builder.Append(",\"observed_camera_name\":").Append(JsonString(snapshot.ObservedCameraName));
			builder.Append(",\"observed_camera_type\":").Append(JsonString(snapshot.ObservedCameraType));
			builder.Append(",\"render_pipeline\":").Append(JsonString(snapshot.RenderPipeline.ToString()));
			builder.Append(",\"integration_name\":").Append(JsonString(snapshot.IntegrationName));
			builder.Append(",\"observed_injection_point\":").Append(JsonString(snapshot.ObservedInjectionPoint));
			builder.Append(",\"perfmeter_pass_count\":").Append(snapshot.PerfMeterPassCount);
			builder.Append(",\"registered_pass_count\":").Append(snapshot.RegisteredPassCount);
			builder.Append(",\"merged_pass_count\":").Append(snapshot.MergedPassCount);
			builder.Append(",\"transient_resource_count\":").Append(snapshot.TransientResourceCount);
			builder.Append(",\"imported_resource_count\":").Append(snapshot.ImportedResourceCount);
			builder.Append(",\"aliased_resource_count\":").Append(snapshot.AliasedResourceCount);
			builder.Append(",\"warning\":").Append(JsonString(snapshot.Warning));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static void AppendVector3(StringBuilder builder, string name, Vector3 value)
		{
			builder.Append(",\"").Append(name).Append("\":{");
			builder.Append("\"x\":").Append(JsonNumber(value.x));
			builder.Append(",\"y\":").Append(JsonNumber(value.y));
			builder.Append(",\"z\":").Append(JsonNumber(value.z));
			builder.Append('}');
		}

		private static void AppendQuaternion(StringBuilder builder, string name, Quaternion value)
		{
			builder.Append(",\"").Append(name).Append("\":{");
			builder.Append("\"x\":").Append(JsonNumber(value.x));
			builder.Append(",\"y\":").Append(JsonNumber(value.y));
			builder.Append(",\"z\":").Append(JsonNumber(value.z));
			builder.Append(",\"w\":").Append(JsonNumber(value.w));
			builder.Append('}');
		}

		private static void AppendRect(StringBuilder builder, string name, Rect value)
		{
			builder.Append(",\"").Append(name).Append("\":{");
			builder.Append("\"x\":").Append(JsonNumber(value.x));
			builder.Append(",\"y\":").Append(JsonNumber(value.y));
			builder.Append(",\"width\":").Append(JsonNumber(value.width));
			builder.Append(",\"height\":").Append(JsonNumber(value.height));
			builder.Append('}');
		}

		private static void AppendEditorState(StringBuilder builder)
		{
			builder.Append(",\"is_playing\":").Append(JsonBool(EditorApplication.isPlaying));
			builder.Append(",\"is_paused\":").Append(JsonBool(EditorApplication.isPaused));
			builder.Append(",\"is_compiling\":").Append(JsonBool(EditorApplication.isCompiling));
		}

		private static bool RequireBool(string json, string property)
		{
			if (TryExtractBool(json, property, out bool value))
			{
				return value;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument " + property + " is required");
		}

		private static string RequireString(string json, string property)
		{
			if (TryExtractString(json, property, out string value) && !string.IsNullOrWhiteSpace(value))
			{
				return value;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument " + property + " is required");
		}

		private static bool TryExtractBool(string json, string property, out bool value)
		{
			value = false;
			int colon = FindPropertyColon(json, property);
			if (colon < 0)
			{
				return false;
			}

			int index = IndexOfNextNonWhitespace(json, colon + 1);
			if (index < 0)
			{
				return false;
			}

			if (json.IndexOf("true", index, StringComparison.OrdinalIgnoreCase) == index)
			{
				value = true;
				return true;
			}

			if (json.IndexOf("false", index, StringComparison.OrdinalIgnoreCase) == index)
			{
				value = false;
				return true;
			}

			return false;
		}

		private static int ExtractInt(string json, string property, int defaultValue)
		{
			return TryExtractInt(json, property, out int value) ? value : defaultValue;
		}

		private static float ExtractFloat(string json, string property, float defaultValue)
		{
			return TryExtractFloat(json, property, out float value) ? value : defaultValue;
		}

		private static bool TryExtractFloat(string json, string property, out float value)
		{
			value = 0f;
			int colon = FindPropertyColon(json, property);
			if (colon < 0)
			{
				return false;
			}

			int index = IndexOfNextNonWhitespace(json, colon + 1);
			if (index < 0)
			{
				return false;
			}

			int start = index;
			if (json[index] == '-')
			{
				index++;
			}

			while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.'))
			{
				index++;
			}

			return index > start && float.TryParse(json.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		private static bool TryExtractInt(string json, string property, out int value)
		{
			value = 0;
			int colon = FindPropertyColon(json, property);
			if (colon < 0)
			{
				return false;
			}

			int index = IndexOfNextNonWhitespace(json, colon + 1);
			if (index < 0)
			{
				return false;
			}

			int start = index;
			if (json[index] == '-')
			{
				index++;
			}

			while (index < json.Length && char.IsDigit(json[index]))
			{
				index++;
			}

			if (index == start || !int.TryParse(json.Substring(start, index - start), out value))
			{
				return false;
			}

			return true;
		}

		private static bool TryExtractString(string json, string property, out string value)
		{
			value = string.Empty;
			int colon = FindPropertyColon(json, property);
			if (colon < 0)
			{
				return false;
			}

			int index = IndexOfNextNonWhitespace(json, colon + 1);
			if (index < 0 || json[index] != '"')
			{
				return false;
			}

			int nextIndex;
			return TryReadJsonString(json, index, out value, out nextIndex);
		}

		private static bool TryExtractStringArray(string json, string property, out string[] values)
		{
			values = Array.Empty<string>();
			int colon = FindPropertyColon(json, property);
			if (colon < 0)
			{
				return false;
			}

			int index = IndexOfNextNonWhitespace(json, colon + 1);
			if (index < 0 || json[index] != '[')
			{
				return false;
			}

			List<string> result = new List<string>();
			index++;
			while (index < json.Length)
			{
				index = IndexOfNextNonWhitespace(json, index);
				if (index < 0)
				{
					return false;
				}

				if (json[index] == ']')
				{
					values = result.ToArray();
					return true;
				}

				if (json[index] != '"' || !TryReadJsonString(json, index, out string value, out int nextIndex))
				{
					return false;
				}

				result.Add(value);
				index = IndexOfNextNonWhitespace(json, nextIndex);
				if (index < 0)
				{
					return false;
				}

				if (json[index] == ',')
				{
					index++;
					continue;
				}

				if (json[index] == ']')
				{
					values = result.ToArray();
					return true;
				}

				return false;
			}

			return false;
		}

		private static bool TryReadJsonString(string json, int quoteIndex, out string value, out int nextIndex)
		{
			value = string.Empty;
			nextIndex = quoteIndex;
			if (string.IsNullOrEmpty(json) || quoteIndex < 0 || quoteIndex >= json.Length || json[quoteIndex] != '"')
			{
				return false;
			}

			StringBuilder builder = new StringBuilder();
			for (int index = quoteIndex + 1; index < json.Length; index++)
			{
				char character = json[index];
				if (character == '"')
				{
					value = builder.ToString();
					nextIndex = index + 1;
					return true;
				}

				if (character == '\\' && index + 1 < json.Length)
				{
					index++;
					character = json[index];
				}

				builder.Append(character);
			}

			return false;
		}

		private static PerfMeterOverlayPreset ParseOverlayPreset(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterOverlayPreset preset) && Enum.IsDefined(typeof(PerfMeterOverlayPreset), preset))
			{
				return preset;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument preset must be Custom, Minimal, Timing, Rendering, Memory, Overdraw, FullDiagnostics, or AgentDebug");
		}

		private static PerfMeterCollectionMode ParseCollectionMode(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterCollectionMode mode) && Enum.IsDefined(typeof(PerfMeterCollectionMode), mode))
			{
				return mode;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument mode must be Stopped, Background, Overlay, or OverdrawDiagnostic");
		}

		private static PerfMeterCaptureTool ParseCaptureTool(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (string.Equals(normalized, "RenderDoc", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCaptureTool.RenderDoc;
			}

			if (string.Equals(normalized, "Pix", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCaptureTool.Pix;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument tool must be RenderDoc or Pix");
		}

		private static PerfMeterMemoryCaptureFlags ParseMemoryCaptureFlags(string argsJson)
		{
			PerfMeterMemoryCaptureFlags flags = PerfMeterMemoryCaptureFlags.None;
			bool managedObjects = !TryExtractBool(argsJson, "managed_objects", out bool managed) || managed;
			bool nativeObjects = !TryExtractBool(argsJson, "native_objects", out bool native) || native;
			if (managedObjects)
			{
				flags |= PerfMeterMemoryCaptureFlags.ManagedObjects;
			}

			if (nativeObjects)
			{
				flags |= PerfMeterMemoryCaptureFlags.NativeObjects;
			}

			if (TryExtractBool(argsJson, "native_allocations", out bool allocations) && allocations)
			{
				flags |= PerfMeterMemoryCaptureFlags.NativeAllocations;
			}

			if (TryExtractBool(argsJson, "native_allocation_sites", out bool sites) && sites)
			{
				flags |= PerfMeterMemoryCaptureFlags.NativeAllocationSites;
			}

			if (TryExtractBool(argsJson, "native_stack_traces", out bool stacks) && stacks)
			{
				flags |= PerfMeterMemoryCaptureFlags.NativeStackTraces;
			}

			return flags;
		}

		private static int RequireRange(int value, int minimum, int maximum, string property)
		{
			if (value < minimum || value > maximum)
			{
				throw new InvalidOperationException("schema_validation_failed\nArgument " + property + " must be between " + minimum + " and " + maximum);
			}

			return value;
		}

		private static PerfMeterOverlayModule ParseOverlayModules(string[] values)
		{
			if (values == null || values.Length == 0)
			{
				return PerfMeterOverlayModule.None;
			}

			PerfMeterOverlayModule modules = PerfMeterOverlayModule.None;
			for (int i = 0; i < values.Length; i++)
			{
				modules |= ParseOverlayModule(values[i]);
			}

			return modules;
		}

		private static PerfMeterOverlayModule ParseOverlayModule(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterOverlayModule module) && (module & ~PerfMeterOverlayModule.All) == 0)
			{
				return module;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument modules must contain only None, All, Fps, Timing, Graphs, Rendering, SrpBatcher, Brg, Uploads, Memory, Gc, GpuMemory, Overdraw, Heatmap, Warnings, CustomMetrics, CpuCores, CpuCoreBars, or CpuCoreGraphs");
		}

		private static PerfMeterOverlayTheme ParseOverlayTheme(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterOverlayTheme theme) && Enum.IsDefined(typeof(PerfMeterOverlayTheme), theme))
			{
				return theme;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument theme must be ClassicDark, Glass, Cyber, or HighContrast");
		}

		private static PerfMeterOverlayLayout ParseOverlayLayout(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterOverlayLayout layout) && Enum.IsDefined(typeof(PerfMeterOverlayLayout), layout))
			{
				return layout;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument layout must be FpsOnly, TextCompact, Graphs, Classic, CompactCards, DiagnosticsWide, OverdrawFocus, MetricBars, or Custom");
		}

		private static PerfMeterOverlayFontFamily ParseOverlayFontFamily(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (Enum.TryParse(normalized, true, out PerfMeterOverlayFontFamily fontFamily) && Enum.IsDefined(typeof(PerfMeterOverlayFontFamily), fontFamily))
			{
				return fontFamily;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument font_family must be Manrope, JetBrainsMono, or LegacyRuntime");
		}

		private static string NormalizeEnumToken(string value)
		{
			return (value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
		}

		private static PerfMeterOverlayCorner ParseOverlayCorner(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (string.Equals(normalized, "TopLeft", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayCorner.TopLeft;
			}

			if (string.Equals(normalized, "TopRight", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayCorner.TopRight;
			}

			if (string.Equals(normalized, "BottomLeft", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayCorner.BottomLeft;
			}

			if (string.Equals(normalized, "BottomRight", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayCorner.BottomRight;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument corner must be TopLeft, TopRight, BottomLeft, or BottomRight");
		}

		private static PerfMeterTargetFps ParseTargetFps(int value)
		{
			switch (value)
			{
				case 15:
					return PerfMeterTargetFps.Fps15;
				case 30:
					return PerfMeterTargetFps.Fps30;
				case 60:
					return PerfMeterTargetFps.Fps60;
				case 90:
					return PerfMeterTargetFps.Fps90;
				case 120:
					return PerfMeterTargetFps.Fps120;
				case 144:
					return PerfMeterTargetFps.Fps144;
				case 240:
					return PerfMeterTargetFps.Fps240;
				default:
					throw new InvalidOperationException("schema_validation_failed\nArgument target_fps must be 15, 30, 60, 90, 120, 144, or 240");
			}
		}

		private static PerfMeterCameraSource ParseCameraSource(string value)
		{
			string normalized = NormalizeEnumToken(value);
			if (string.Equals(normalized, "Auto", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCameraSource.Auto;
			}

			if (string.Equals(normalized, "MainCamera", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCameraSource.MainCamera;
			}

			if (string.Equals(normalized, "NameFilter", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCameraSource.NameFilter;
			}

			if (string.Equals(normalized, "FirstGameCamera", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterCameraSource.FirstGameCamera;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument source must be Auto, MainCamera, NameFilter, or FirstGameCamera");
		}

		private static string ResolveProjectLocalPath(string path)
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string combinedPath = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path);
			string fullPath = Path.GetFullPath(combinedPath);
			string normalizedRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("schema_validation_failed\nArgument path must stay inside the Unity project directory");
			}

			return fullPath;
		}

		private static int FindPropertyColon(string json, string property)
		{
			if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(property))
			{
				return -1;
			}

			string pattern = "\"" + property + "\"";
			int propertyIndex = json.IndexOf(pattern, StringComparison.Ordinal);
			if (propertyIndex < 0)
			{
				return -1;
			}

			return json.IndexOf(':', propertyIndex + pattern.Length);
		}

		private static int IndexOfNextNonWhitespace(string value, int start)
		{
			if (string.IsNullOrEmpty(value))
			{
				return -1;
			}

			for (int index = start; index < value.Length; index++)
			{
				if (!char.IsWhiteSpace(value[index]))
				{
					return index;
				}
			}

			return -1;
		}

		private static string JsonBool(bool value)
		{
			return value ? "true" : "false";
		}

		private static string JsonNumber(double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				return JsonString(value.ToString(CultureInfo.InvariantCulture));
			}

			return value.ToString("R", CultureInfo.InvariantCulture);
		}

		private static string JsonNumber(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				return JsonString(value.ToString(CultureInfo.InvariantCulture));
			}

			return value.ToString("R", CultureInfo.InvariantCulture);
		}

		private static string JsonString(string value)
		{
			if (value == null)
			{
				return "\"\"";
			}

			StringBuilder builder = new StringBuilder(value.Length + 2);
			builder.Append('"');
			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				switch (character)
				{
					case '\\':
						builder.Append("\\\\");
						break;
					case '"':
						builder.Append("\\\"");
						break;
					case '\n':
						builder.Append("\\n");
						break;
					case '\r':
						builder.Append("\\r");
						break;
					case '\t':
						builder.Append("\\t");
						break;
					default:
						if (char.IsControl(character))
						{
							builder.Append("\\u").Append(((int)character).ToString("x4"));
						}
						else
						{
							builder.Append(character);
						}
						break;
				}
			}

			builder.Append('"');
			return builder.ToString();
		}
	}

	[InitializeOnLoad]
	internal static class PerfMeterMcpOverlaySession
	{
		private const string HasVisibilityKey = "SGG.PerfMeter.Mcp.OverlayVisibility.HasValue";
		private const string VisibilityKey = "SGG.PerfMeter.Mcp.OverlayVisibility.Value";
		private static int _updatesUntilApply;

		static PerfMeterMcpOverlaySession()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				ScheduleApply();
			}
		}

		internal static bool HasStoredVisibility => SessionState.GetBool(HasVisibilityKey, false);

		internal static void StoreRequestedVisibility(bool visible)
		{
			SessionState.SetBool(HasVisibilityKey, true);
			SessionState.SetBool(VisibilityKey, visible);
		}

		internal static bool GetRequestedVisibility(PerfMeterStatusSnapshot status)
		{
			if (HasStoredVisibility)
			{
				return SessionState.GetBool(VisibilityKey, false);
			}

			return status.CollectionMode == PerfMeterCollectionMode.Overlay || status.CollectionMode == PerfMeterCollectionMode.OverdrawDiagnostic;
		}

		internal static string GetApplyState(PerfMeterStatusSnapshot status, bool requestedVisible)
		{
			if (status.State == PerfMeterRuntimeState.Stopped)
			{
				return "stopped";
			}

			if (!Application.isPlaying)
			{
				return "edit_mode_deferred";
			}

			if (requestedVisible == status.OverlayVisible)
			{
				return requestedVisible ? "active_component" : "detached";
			}

			return "pending";
		}

		internal static bool ApplyStoredVisibilityIfPlaying()
		{
			if (!Application.isPlaying || !HasStoredVisibility || RuntimePerformanceMeter.GetStatus().State == PerfMeterRuntimeState.Stopped)
			{
				return false;
			}

			RuntimePerformanceMeter.SetOverlayVisible(SessionState.GetBool(VisibilityKey, false));
			return RequestRepaint();
		}

		internal static bool RequestRepaint()
		{
			EditorApplication.QueuePlayerLoopUpdate();
			EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			for (int i = 0; i < windows.Length; i++)
			{
				windows[i].Repaint();
			}

			return true;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				ScheduleApply();
			}
			else if (state == PlayModeStateChange.ExitingPlayMode)
			{
				EditorApplication.update -= ApplyAfterPlayModeLoad;
			}
		}

		private static void ScheduleApply()
		{
			_updatesUntilApply = 2;
			EditorApplication.update -= ApplyAfterPlayModeLoad;
			EditorApplication.update += ApplyAfterPlayModeLoad;
		}

		private static void ApplyAfterPlayModeLoad()
		{
			if (!EditorApplication.isPlaying)
			{
				if (!EditorApplication.isPlayingOrWillChangePlaymode)
				{
					EditorApplication.update -= ApplyAfterPlayModeLoad;
				}

				return;
			}

			if (EditorApplication.isCompiling || --_updatesUntilApply > 0)
			{
				return;
			}

			EditorApplication.update -= ApplyAfterPlayModeLoad;
			ApplyStoredVisibilityIfPlaying();
		}
	}
}
