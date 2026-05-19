using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SGG.PerfMeter
{
	internal static class PerfMeterSessionExporter
	{
		private const string PackageName = "com.sungeargames.perfmeter";
		private const string PackageVersion = "2026.5.18-1";
		private const int SchemaVersion = 1;

		internal static bool ExportJson(string path, PerfMeterSessionSummarySnapshot summary, PerfMeterSessionSampleSnapshot[] samples, PerfMeterStatusSnapshot status)
		{
			File.WriteAllText(PreparePath(path), BuildJson(summary, samples, status), Encoding.UTF8);
			return true;
		}

		internal static bool ExportCsv(string path, PerfMeterSessionSummarySnapshot summary, PerfMeterSessionSampleSnapshot[] samples, PerfMeterStatusSnapshot status)
		{
			File.WriteAllText(PreparePath(path), BuildCsv(summary, samples, status), Encoding.UTF8);
			return true;
		}

		internal static string BuildJson(PerfMeterSessionSummarySnapshot summary, PerfMeterSessionSampleSnapshot[] samples, PerfMeterStatusSnapshot status)
		{
			PerfMeterSessionSampleSnapshot[] safeSamples = samples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			StringBuilder builder = new StringBuilder(2048 + safeSamples.Length * 768);
			builder.Append("{\"schema_version\":").Append(SchemaVersion);
			builder.Append(",\"package\":").Append(JsonString(PackageName));
			builder.Append(",\"package_version\":").Append(JsonString(PackageVersion));
			builder.Append(",\"summary\":");
			AppendSummary(builder, summary);
			builder.Append(",\"options\":");
			AppendOptions(builder, summary.Options);
			builder.Append(",\"metadata\":{");
			builder.Append("\"device\":");
			AppendDevice(builder, summary.Device);
			builder.Append(",\"camera\":");
			AppendCamera(builder, summary.Camera);
			builder.Append(",\"settings\":");
			AppendSettings(builder, summary.Settings);
			builder.Append('}');
			builder.Append(",\"status\":{");
			builder.Append("\"warning\":").Append(JsonString(status.Warning));
			builder.Append(",\"available_counters\":").Append(JsonString(status.AvailableCounters.ToString()));
			builder.Append(",\"unavailable_counters\":").Append(JsonString(status.UnavailableCounters.ToString()));
			builder.Append('}');
			builder.Append(",\"samples\":[");
			for (int i = 0; i < safeSamples.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				AppendSample(builder, safeSamples[i]);
			}

			builder.Append("]}");
			return builder.ToString();
		}

		internal static string BuildCsv(PerfMeterSessionSummarySnapshot summary, PerfMeterSessionSampleSnapshot[] samples, PerfMeterStatusSnapshot status)
		{
			PerfMeterSessionSampleSnapshot[] safeSamples = samples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			StringBuilder builder = new StringBuilder(1024 + safeSamples.Length * 512);
			builder.Append("frame,time_seconds,scene,bottleneck,cpu_frame_ms,cpu_main_thread_ms,cpu_render_thread_ms,cpu_present_wait_ms,gpu_frame_ms,gpu_available,frame_budget_ms,average_fps,one_percent_low_fps,point_one_percent_low_fps,frame_spike_count,severe_frame_spike_count,draw_calls,set_pass_calls,batches,vertices,srp_batcher_instances,brg_draw_calls,brg_instances,index_buffer_upload_in_frame_bytes,system_used_memory_bytes,gc_reserved_memory_bytes,gpu_memory_bytes,overdraw_state,overdraw_progress,overdraw_ratio,session_warning,available_counters,unavailable_counters");
			builder.AppendLine();
			for (int i = 0; i < safeSamples.Length; i++)
			{
				AppendCsvSample(builder, safeSamples[i], summary.Warning, status);
				builder.AppendLine();
			}

			return builder.ToString();
		}

		private static string PreparePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException("Session export path must not be empty.", nameof(path));
			}

			string fullPath = Path.GetFullPath(path);
			string directory = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return fullPath;
		}

		private static void AppendSummary(StringBuilder builder, PerfMeterSessionSummarySnapshot summary)
		{
			builder.Append('{');
			builder.Append("\"state\":").Append(JsonString(summary.State.ToString()));
			builder.Append(",\"sample_count\":").Append(summary.SampleCount);
			builder.Append(",\"dropped_sample_count\":").Append(summary.DroppedSampleCount);
			builder.Append(",\"first_frame\":").Append(summary.FirstFrame);
			builder.Append(",\"last_frame\":").Append(summary.LastFrame);
			builder.Append(",\"start_time_seconds\":").Append(JsonNumber(summary.StartTimeSeconds));
			builder.Append(",\"stop_time_seconds\":").Append(JsonNumber(summary.StopTimeSeconds));
			builder.Append(",\"duration_seconds\":").Append(JsonNumber(summary.DurationSeconds));
			builder.Append(",\"average_frame_time_ms\":").Append(JsonNumber(summary.AverageFrameTimeMs));
			builder.Append(",\"min_frame_time_ms\":").Append(JsonNumber(summary.MinFrameTimeMs));
			builder.Append(",\"max_frame_time_ms\":").Append(JsonNumber(summary.MaxFrameTimeMs));
			builder.Append(",\"average_fps\":").Append(JsonNumber(summary.AverageFps));
			builder.Append(",\"min_fps\":").Append(JsonNumber(summary.MinFps));
			builder.Append(",\"max_fps\":").Append(JsonNumber(summary.MaxFps));
			builder.Append(",\"gpu_bound_sample_count\":").Append(summary.GpuBoundSampleCount);
			builder.Append(",\"cpu_main_thread_bound_sample_count\":").Append(summary.CpuMainThreadBoundSampleCount);
			builder.Append(",\"cpu_render_thread_bound_sample_count\":").Append(summary.CpuRenderThreadBoundSampleCount);
			builder.Append(",\"present_limited_sample_count\":").Append(summary.PresentLimitedSampleCount);
			builder.Append(",\"frame_spike_count\":").Append(summary.FrameSpikeCount);
			builder.Append(",\"severe_frame_spike_count\":").Append(summary.SevereFrameSpikeCount);
			builder.Append(",\"warning\":").Append(JsonString(summary.Warning));
			builder.Append(",\"start_scene_name\":").Append(JsonString(summary.StartSceneName));
			builder.Append(",\"last_scene_name\":").Append(JsonString(summary.LastSceneName));
			builder.Append(",\"worst_frame\":");
			AppendWorstFrame(builder, summary.WorstFrame);
			builder.Append(",\"current_scene_worst_frame\":");
			AppendWorstFrame(builder, summary.CurrentSceneWorstFrame);
			builder.Append(",\"whole_run\":");
			AppendScopeSummary(builder, summary.WholeRun);
			builder.Append(",\"current_scene\":");
			AppendScopeSummary(builder, summary.CurrentScene);
			builder.Append('}');
		}

		private static void AppendOptions(StringBuilder builder, PerfMeterSessionOptions options)
		{
			builder.Append('{');
			builder.Append("\"warmup_frames\":").Append(options.WarmupFrames);
			builder.Append(",\"warmup_seconds\":").Append(JsonNumber(options.WarmupSeconds));
			builder.Append(",\"sample_interval_seconds\":").Append(JsonNumber(options.SampleIntervalSeconds));
			builder.Append(",\"max_samples\":").Append(options.MaxSamples);
			builder.Append(",\"reset_on_scene_load\":").Append(JsonBool(options.ResetOnSceneLoad));
			builder.Append(",\"scene_load_ignore_frames\":").Append(options.SceneLoadIgnoreFrames);
			builder.Append(",\"scene_load_ignore_seconds\":").Append(JsonNumber(options.SceneLoadIgnoreSeconds));
			builder.Append('}');
		}

		private static void AppendScopeSummary(StringBuilder builder, PerfMeterSessionScopeSummarySnapshot scope)
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

		private static void AppendDevice(StringBuilder builder, PerfMeterDeviceSnapshot device)
		{
			builder.Append('{');
			builder.Append("\"unity_version\":").Append(JsonString(device.UnityVersion));
			builder.Append(",\"platform\":").Append(JsonString(device.ApplicationPlatform.ToString()));
			builder.Append(",\"is_editor\":").Append(JsonBool(device.IsEditor));
			builder.Append(",\"operating_system\":").Append(JsonString(device.OperatingSystem));
			builder.Append(",\"device_model\":").Append(JsonString(device.DeviceModel));
			builder.Append(",\"graphics_device_type\":").Append(JsonString(device.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(device.GraphicsDeviceName));
			builder.Append(",\"graphics_device_vendor\":").Append(JsonString(device.GraphicsDeviceVendor));
			builder.Append(",\"graphics_memory_size_mb\":").Append(device.GraphicsMemorySizeMb);
			builder.Append(",\"screen_width\":").Append(device.ScreenWidth);
			builder.Append(",\"screen_height\":").Append(device.ScreenHeight);
			builder.Append('}');
		}

		private static void AppendCamera(StringBuilder builder, PerfMeterCameraSnapshot camera)
		{
			builder.Append('{');
			builder.Append("\"is_available\":").Append(JsonBool(camera.IsAvailable));
			builder.Append(",\"warning\":").Append(JsonString(camera.Warning));
			builder.Append(",\"camera_name\":").Append(JsonString(camera.CameraName));
			builder.Append(",\"scene_name\":").Append(JsonString(camera.SceneName));
			builder.Append(",\"scene_path\":").Append(JsonString(camera.ScenePath));
			AppendVector3(builder, "position", camera.Position.x, camera.Position.y, camera.Position.z);
			AppendQuaternion(builder, "rotation", camera.Rotation.x, camera.Rotation.y, camera.Rotation.z, camera.Rotation.w);
			AppendVector3(builder, "euler_angles", camera.EulerAngles.x, camera.EulerAngles.y, camera.EulerAngles.z);
			builder.Append(",\"projection\":").Append(JsonString(camera.Projection.ToString()));
			builder.Append(",\"field_of_view\":").Append(JsonNumber(camera.FieldOfView));
			builder.Append(",\"orthographic_size\":").Append(JsonNumber(camera.OrthographicSize));
			builder.Append(",\"near_clip_plane\":").Append(JsonNumber(camera.NearClipPlane));
			builder.Append(",\"far_clip_plane\":").Append(JsonNumber(camera.FarClipPlane));
			builder.Append('}');
		}

		private static void AppendSettings(StringBuilder builder, PerfMeterSettingsSnapshot settings)
		{
			builder.Append('{');
			builder.Append("\"enabled\":").Append(JsonBool(settings.Enabled));
			builder.Append(",\"auto_start\":").Append(JsonBool(settings.AutoStart));
			builder.Append(",\"collection_mode\":").Append(JsonString(settings.CollectionMode.ToString()));
			builder.Append(",\"overlay_visible\":").Append(JsonBool(settings.OverlayVisible));
			builder.Append(",\"overlay_corner\":").Append(JsonString(settings.OverlayCorner.ToString()));
			builder.Append(",\"overlay_mode\":").Append(JsonString(settings.OverlayMode.ToString()));
			builder.Append(",\"target_fps\":").Append((int)settings.TargetFps);
			builder.Append(",\"active_preset\":").Append(JsonString(settings.ActivePreset));
			builder.Append(",\"overlay_modules\":").Append(JsonString(settings.OverlayModules.ToString()));
			builder.Append(",\"overlay_scale\":").Append(JsonNumber(settings.OverlayScale));
			builder.Append(",\"overlay_opacity\":").Append(JsonNumber(settings.OverlayOpacity));
			builder.Append(",\"overlay_font_size\":").Append(JsonNumber(settings.OverlayFontSize));
			builder.Append(",\"overlay_refresh_interval_seconds\":").Append(JsonNumber(settings.OverlayRefreshIntervalSeconds));
			builder.Append(",\"overlay_graph_history_length\":").Append(settings.OverlayGraphHistoryLength);
			builder.Append(",\"session_warmup_frames\":").Append(settings.SessionWarmupFrames);
			builder.Append(",\"session_warmup_seconds\":").Append(JsonNumber(settings.SessionWarmupSeconds));
			builder.Append(",\"session_sample_interval_seconds\":").Append(JsonNumber(settings.SessionSampleIntervalSeconds));
			builder.Append(",\"session_max_samples\":").Append(settings.SessionMaxSamples);
			builder.Append(",\"session_reset_on_scene_load\":").Append(JsonBool(settings.SessionResetOnSceneLoad));
			builder.Append(",\"session_scene_load_ignore_frames\":").Append(settings.SessionSceneLoadIgnoreFrames);
			builder.Append(",\"session_scene_load_ignore_seconds\":").Append(JsonNumber(settings.SessionSceneLoadIgnoreSeconds));
			builder.Append(",\"overdraw_default_frame_count\":").Append(settings.OverdrawDefaultFrameCount);
			builder.Append(",\"overdraw_max_frame_count\":").Append(settings.OverdrawMaxFrameCount);
			builder.Append(",\"alert_overdraw_ratio_threshold\":").Append(JsonNumber(settings.AlertOverdrawRatioThreshold));
			builder.Append(",\"alert_timing_consecutive_frames\":").Append(settings.AlertTimingConsecutiveFrames);
			builder.Append(",\"alert_fps_consecutive_frames\":").Append(settings.AlertFpsConsecutiveFrames);
			builder.Append(",\"alert_gpu_timing_unavailable_consecutive_frames\":").Append(settings.AlertGpuTimingUnavailableConsecutiveFrames);
			builder.Append(",\"alert_overdraw_consecutive_frames\":").Append(settings.AlertOverdrawConsecutiveFrames);
			builder.Append(",\"load_state\":").Append(JsonString(settings.LoadState.ToString()));
			builder.Append(",\"warning\":").Append(JsonString(settings.Warning));
			builder.Append('}');
		}

		private static void AppendSample(StringBuilder builder, PerfMeterSessionSampleSnapshot sample)
		{
			PerfMeterMetricsSnapshot metrics = sample.Metrics;
			builder.Append('{');
			builder.Append("\"frame\":").Append(sample.CollectionFrame);
			builder.Append(",\"time_seconds\":").Append(JsonNumber(sample.CollectionTimeSeconds));
			builder.Append(",\"scene\":").Append(JsonString(sample.SceneName));
			AppendMetrics(builder, metrics);
			builder.Append('}');
		}

		private static void AppendMetrics(StringBuilder builder, PerfMeterMetricsSnapshot metrics)
		{
			builder.Append(",\"bottleneck\":").Append(JsonString(metrics.Bottleneck.ToString()));
			builder.Append(",\"cpu_frame_ms\":").Append(JsonNumber(metrics.CpuFrameTimeMs));
			builder.Append(",\"cpu_main_thread_ms\":").Append(JsonNumber(metrics.CpuMainThreadFrameTimeMs));
			builder.Append(",\"cpu_render_thread_ms\":").Append(JsonNumber(metrics.CpuRenderThreadFrameTimeMs));
			builder.Append(",\"cpu_present_wait_ms\":").Append(JsonNumber(metrics.CpuMainThreadPresentWaitTimeMs));
			builder.Append(",\"gpu_frame_ms\":").Append(JsonNumber(metrics.GpuFrameTimeMs));
			builder.Append(",\"gpu_available\":").Append(JsonBool(metrics.GpuFrameTimeAvailable));
			builder.Append(",\"frame_budget_ms\":").Append(JsonNumber(metrics.FrameBudgetMs));
			builder.Append(",\"average_fps\":").Append(JsonNumber(metrics.AverageFps));
			builder.Append(",\"one_percent_low_fps\":").Append(JsonNumber(metrics.OnePercentLowFps));
			builder.Append(",\"point_one_percent_low_fps\":").Append(JsonNumber(metrics.PointOnePercentLowFps));
			builder.Append(",\"frame_spike_count\":").Append(metrics.FrameSpikeCount);
			builder.Append(",\"severe_frame_spike_count\":").Append(metrics.SevereFrameSpikeCount);
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
			builder.Append(",\"overdraw_state\":").Append(JsonString(metrics.OverdrawState.ToString()));
			builder.Append(",\"overdraw_progress\":").Append(JsonNumber(metrics.OverdrawProgress));
			builder.Append(",\"overdraw_ratio\":").Append(JsonNumber(metrics.OverdrawRatio));
		}

		private static void AppendCsvSample(StringBuilder builder, PerfMeterSessionSampleSnapshot sample, string warning, PerfMeterStatusSnapshot status)
		{
			PerfMeterMetricsSnapshot metrics = sample.Metrics;
			builder.Append(sample.CollectionFrame).Append(',');
			builder.Append(JsonNumber(sample.CollectionTimeSeconds)).Append(',');
			AppendCsv(builder, sample.SceneName).Append(',');
			AppendCsv(builder, metrics.Bottleneck.ToString()).Append(',');
			builder.Append(JsonNumber(metrics.CpuFrameTimeMs)).Append(',');
			builder.Append(JsonNumber(metrics.CpuMainThreadFrameTimeMs)).Append(',');
			builder.Append(JsonNumber(metrics.CpuRenderThreadFrameTimeMs)).Append(',');
			builder.Append(JsonNumber(metrics.CpuMainThreadPresentWaitTimeMs)).Append(',');
			builder.Append(JsonNumber(metrics.GpuFrameTimeMs)).Append(',');
			builder.Append(metrics.GpuFrameTimeAvailable ? "true" : "false").Append(',');
			builder.Append(JsonNumber(metrics.FrameBudgetMs)).Append(',');
			builder.Append(JsonNumber(metrics.AverageFps)).Append(',');
			builder.Append(JsonNumber(metrics.OnePercentLowFps)).Append(',');
			builder.Append(JsonNumber(metrics.PointOnePercentLowFps)).Append(',');
			builder.Append(metrics.FrameSpikeCount).Append(',');
			builder.Append(metrics.SevereFrameSpikeCount).Append(',');
			builder.Append(metrics.DrawCalls).Append(',');
			builder.Append(metrics.SetPassCalls).Append(',');
			builder.Append(metrics.Batches).Append(',');
			builder.Append(metrics.Vertices).Append(',');
			builder.Append(metrics.SrpBatcherInstances).Append(',');
			builder.Append(metrics.BrgDrawCalls).Append(',');
			builder.Append(metrics.BrgInstances).Append(',');
			builder.Append(metrics.IndexBufferUploadInFrameBytes).Append(',');
			builder.Append(metrics.SystemUsedMemoryBytes).Append(',');
			builder.Append(metrics.GcReservedMemoryBytes).Append(',');
			builder.Append(metrics.GpuMemoryBytes).Append(',');
			AppendCsv(builder, metrics.OverdrawState.ToString()).Append(',');
			builder.Append(JsonNumber(metrics.OverdrawProgress)).Append(',');
			builder.Append(JsonNumber(metrics.OverdrawRatio)).Append(',');
			AppendCsv(builder, warning).Append(',');
			AppendCsv(builder, status.AvailableCounters.ToString()).Append(',');
			AppendCsv(builder, status.UnavailableCounters.ToString());
		}

		private static void AppendVector3(StringBuilder builder, string name, float x, float y, float z)
		{
			builder.Append(",\"").Append(name).Append("\":{");
			builder.Append("\"x\":").Append(JsonNumber(x));
			builder.Append(",\"y\":").Append(JsonNumber(y));
			builder.Append(",\"z\":").Append(JsonNumber(z));
			builder.Append('}');
		}

		private static void AppendQuaternion(StringBuilder builder, string name, float x, float y, float z, float w)
		{
			builder.Append(",\"").Append(name).Append("\":{");
			builder.Append("\"x\":").Append(JsonNumber(x));
			builder.Append(",\"y\":").Append(JsonNumber(y));
			builder.Append(",\"z\":").Append(JsonNumber(z));
			builder.Append(",\"w\":").Append(JsonNumber(w));
			builder.Append('}');
		}

		private static StringBuilder AppendCsv(StringBuilder builder, string value)
		{
			builder.Append('"');
			string safeValue = value ?? string.Empty;
			for (int i = 0; i < safeValue.Length; i++)
			{
				char character = safeValue[i];
				if (character == '"')
				{
					builder.Append("\"\"");
				}
				else
				{
					builder.Append(character);
				}
			}

			builder.Append('"');
			return builder;
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
