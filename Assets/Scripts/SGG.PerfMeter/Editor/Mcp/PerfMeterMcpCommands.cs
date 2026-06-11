using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SGG.PerfMeter.Editor.Setup;
using UnityEditor;
using UnityEngine;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.Mcp
{
	public static class PerfMeterMcpCommands
	{
		public static string SetupStatus()
		{
			return "{\"status_report\":" + JsonString(PerfMeterSetupActions.GetStatusReport()) + "}";
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
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string RuntimeStop()
		{
			RuntimePerformanceMeter.Stop();
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string RuntimeResetStats()
		{
			RuntimePerformanceMeter.ResetStats();
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string RuntimeModeSet(string argsJson)
		{
			PerfMeterCollectionMode mode = ParseCollectionMode(RequireString(argsJson, "mode"));
			RuntimePerformanceMeter.SetCollectionMode(mode);
			if (mode == PerfMeterCollectionMode.OverdrawDiagnostic && TryExtractInt(argsJson, "frame_count", out int frameCount))
			{
				PerfMeterSettingsSnapshot settings = RuntimePerformanceMeter.GetSettings();
				RuntimePerformanceMeter.RequestOverdrawMeasurement(Mathf.Clamp(frameCount, 1, settings.OverdrawMaxFrameCount));
			}

			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string MetricsLatest()
		{
			return MetricsJson(RuntimePerformanceMeter.GetLatestMetrics());
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

			RuntimePerformanceMeter.SetOverlayVisible(visible);
			return StatusJson(RuntimePerformanceMeter.GetStatus());
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
			if (File.Exists(safePath))
			{
				return SessionCommandJson(false, safePath, "file_exists", "not_exported", RuntimePerformanceMeter.GetSessionSummary());
			}

			string normalizedFormat = NormalizeEnumToken(format);
			if (string.Equals(normalizedFormat, "json", StringComparison.OrdinalIgnoreCase))
			{
				RuntimePerformanceMeter.ExportSessionJson(safePath);
			}
			else if (string.Equals(normalizedFormat, "csv", StringComparison.OrdinalIgnoreCase))
			{
				RuntimePerformanceMeter.ExportSessionCsv(safePath);
			}
			else
			{
				throw new InvalidOperationException("schema_validation_failed\nArgument format must be json or csv");
			}

			return SessionCommandJson(true, safePath, string.Empty, "exported", RuntimePerformanceMeter.GetSessionSummary());
		}

		private static string StatusJson(PerfMeterStatusSnapshot status)
		{
			StringBuilder builder = new StringBuilder(768);
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
			builder.Append(",\"available_counters\":").Append(JsonString(status.AvailableCounters.ToString()));
			builder.Append(",\"unavailable_counters\":").Append(JsonString(status.UnavailableCounters.ToString()));
			builder.Append(",\"overlay_visible\":").Append(JsonBool(status.OverlayVisible));
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
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"cleared\":").Append(JsonBool(cleared));
			builder.Append(",\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"collection_frame\":").Append(status.CollectionFrame);
			builder.Append(",\"active_alert_count\":").Append(status.ActiveAlertCount);
			builder.Append(",\"fired_alert_count\":").Append(status.FiredAlertCount);
			builder.Append(",\"latest_alert_rule_id\":").Append(JsonString(status.LatestAlertRuleId));
			builder.Append(",\"latest_alert_message\":").Append(JsonString(status.LatestAlertMessage));
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
			builder.Append("\"state\":").Append(JsonString(summary.State.ToString()));
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
			builder.Append(",\"overdraw_ratio\":").Append(JsonNumber(metrics.OverdrawRatio));
			builder.Append(",\"overdraw_state\":").Append(JsonString(metrics.OverdrawState.ToString()));
			builder.Append(",\"overdraw_progress\":").Append(JsonNumber(metrics.OverdrawProgress));
			AppendCustomMetrics(builder, customMetrics);
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
}
