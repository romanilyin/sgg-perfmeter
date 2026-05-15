using System;
using System.Globalization;
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

		public static string MetricsLatest()
		{
			return MetricsJson(RuntimePerformanceMeter.GetLatestMetrics());
		}

		public static string OverlaySet(string argsJson)
		{
			bool visible = RequireBool(argsJson, "visible");
			if (TryExtractString(argsJson, "corner", out string corner))
			{
				RuntimePerformanceMeter.SetOverlayCorner(ParseOverlayCorner(corner));
			}

			if (TryExtractString(argsJson, "mode", out string mode))
			{
				RuntimePerformanceMeter.SetOverlayMode(ParseOverlayMode(mode));
			}

			if (TryExtractInt(argsJson, "target_fps", out int targetFps))
			{
				RuntimePerformanceMeter.SetTargetFps(ParseTargetFps(targetFps));
			}

			RuntimePerformanceMeter.SetOverlayVisible(visible);
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string OverdrawStart(string argsJson)
		{
			int frameCount = Mathf.Clamp(ExtractInt(argsJson, "frame_count", 60), 1, 600);
			RuntimePerformanceMeter.RequestOverdrawMeasurement(frameCount);
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		public static string OverdrawCancel()
		{
			RuntimePerformanceMeter.CancelOverdrawMeasurement();
			return StatusJson(RuntimePerformanceMeter.GetStatus());
		}

		private static string StatusJson(PerfMeterStatusSnapshot status)
		{
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"state\":").Append(JsonString(status.State.ToString()));
			builder.Append(",\"availability\":").Append(JsonString(status.Availability.ToString()));
			builder.Append(",\"frame_timing_availability\":").Append(JsonString(status.FrameTimingAvailability.ToString()));
			builder.Append(",\"graphics_device_type\":").Append(JsonString(status.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(status.GraphicsDeviceName));
			builder.Append(",\"warning\":").Append(JsonString(status.Warning));
			builder.Append(",\"collection_frame\":").Append(status.CollectionFrame);
			builder.Append(",\"last_error\":").Append(JsonString(status.LastError));
			builder.Append(",\"bottleneck\":").Append(JsonString(status.Bottleneck.ToString()));
			builder.Append(",\"available_counters\":").Append(JsonString(status.AvailableCounters.ToString()));
			builder.Append(",\"unavailable_counters\":").Append(JsonString(status.UnavailableCounters.ToString()));
			builder.Append(",\"overlay_visible\":").Append(JsonBool(status.OverlayVisible));
			builder.Append(",\"overlay_corner\":").Append(JsonString(status.OverlayCorner.ToString()));
			builder.Append(",\"overlay_mode\":").Append(JsonString(status.OverlayMode.ToString()));
			builder.Append(",\"target_fps\":").Append((int)status.TargetFps);
			builder.Append(",\"target_frame_budget_ms\":").Append(JsonNumber(1000d / (int)status.TargetFps));
			builder.Append(",\"overdraw_state\":").Append(JsonString(status.OverdrawState.ToString()));
			builder.Append(",\"overdraw_progress\":").Append(JsonNumber(status.OverdrawProgress));
			builder.Append(",\"overdraw_ratio\":").Append(JsonNumber(status.OverdrawRatio));
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
		}

		private static string MetricsJson(PerfMeterMetricsSnapshot metrics)
		{
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
			AppendEditorState(builder);
			builder.Append('}');
			return builder.ToString();
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

			StringBuilder builder = new StringBuilder();
			for (index++; index < json.Length; index++)
			{
				char character = json[index];
				if (character == '"')
				{
					value = builder.ToString();
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

		private static PerfMeterOverlayCorner ParseOverlayCorner(string value)
		{
			string normalized = (value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Trim();
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

		private static PerfMeterOverlayMode ParseOverlayMode(string value)
		{
			string normalized = (value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Trim();
			if (string.Equals(normalized, "FpsOnly", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayMode.FpsOnly;
			}

			if (string.Equals(normalized, "TextCompact", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayMode.TextCompact;
			}

			if (string.Equals(normalized, "Graphs", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayMode.Graphs;
			}

			if (string.Equals(normalized, "Full", StringComparison.OrdinalIgnoreCase))
			{
				return PerfMeterOverlayMode.Full;
			}

			throw new InvalidOperationException("schema_validation_failed\nArgument mode must be FpsOnly, TextCompact, Graphs, or Full");
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
