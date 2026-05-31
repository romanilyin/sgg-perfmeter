using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGG.PerfMeter
{
	[Serializable]
	public sealed class PerfMeterOverlayPresetJson
	{
		public string schema = PerfMeterOverlayPresetUtility.Schema;
		public int version = PerfMeterOverlayPresetUtility.CurrentVersion;
		public string id = string.Empty;
		public string displayName = string.Empty;
		public string description = string.Empty;
		public string[] tags = Array.Empty<string>();
		public PerfMeterOverlayPresetStyleJson style = new PerfMeterOverlayPresetStyleJson();
		public PerfMeterOverlayPresetWidgetJson[] widgets = Array.Empty<PerfMeterOverlayPresetWidgetJson>();
	}

	[Serializable]
	public sealed class PerfMeterOverlayPresetStyleJson
	{
		public string anchor = nameof(PerfMeterOverlayCorner.TopRight);
		public string layout = nameof(PerfMeterOverlayLayout.CompactCards);
		public string theme = nameof(PerfMeterOverlayTheme.ClassicDark);
		public string font = nameof(PerfMeterOverlayFontFamily.Manrope);
		public float scale = 1f;
		public float opacity = 0.84f;
		public int maxWidth = 420;
		public int gap = 4;
	}

	[Serializable]
	public sealed class PerfMeterOverlayPresetWidgetJson
	{
		public string id = string.Empty;
		public bool enabled = true;
		public int order = 10;
		public string variant = string.Empty;
		public int height = 0;
	}

	[Serializable]
	internal sealed class PerfMeterOverlayPresetCollectionJson
	{
		public int schemaVersion = 1;
		public string activePresetId = PerfMeterOverlayPresetDefaults.CompactTimingId;
		public PerfMeterOverlayPresetJson[] presets = Array.Empty<PerfMeterOverlayPresetJson>();
	}

	public sealed class PerfMeterWidgetDescriptor
	{
		internal PerfMeterWidgetDescriptor(
			string id,
			string displayName,
			string category,
			string kind,
			string module,
			string description,
			bool isPresetBlock,
			bool isDebugOnly,
			PerfMeterOverlayModule overlayModules,
			params string[] requiredProviders)
		{
			Id = id ?? string.Empty;
			DisplayName = displayName ?? string.Empty;
			Category = category ?? string.Empty;
			Kind = kind ?? string.Empty;
			Module = module ?? string.Empty;
			Description = description ?? string.Empty;
			IsPresetBlock = isPresetBlock;
			IsDebugOnly = isDebugOnly;
			OverlayModules = overlayModules;
			RequiredProviders = requiredProviders ?? Array.Empty<string>();
		}

		public string Id { get; }
		public string DisplayName { get; }
		public string Category { get; }
		public string Kind { get; }
		public string Module { get; }
		public string Description { get; }
		public bool IsPresetBlock { get; }
		public bool IsDebugOnly { get; }
		public string[] RequiredProviders { get; }
		internal PerfMeterOverlayModule OverlayModules { get; }
	}

	public readonly struct PerfMeterOverlayPresetValidationResult
	{
		internal PerfMeterOverlayPresetValidationResult(bool isValid, string warning)
		{
			IsValid = isValid;
			Warning = warning ?? string.Empty;
		}

		public bool IsValid { get; }
		public string Warning { get; }
	}

	public static class PerfMeterWidgetRegistry
	{
		private static readonly PerfMeterWidgetDescriptor[] BuiltInDescriptors =
		{
			new PerfMeterWidgetDescriptor("fps.summary-card", "FPS summary card", "FPS", "Card", "Fps", "Average FPS, 1% low, 0.1% low, and current budget state.", true, false, PerfMeterOverlayModule.Fps, "Fps"),
			new PerfMeterWidgetDescriptor("timing.cpu-card", "CPU timing card", "Timing", "Card", "Timing", "CPU frame, main thread, render thread, and frame budget state.", true, false, PerfMeterOverlayModule.Timing, "Timing"),
			new PerfMeterWidgetDescriptor("timing.gpu-card", "GPU timing card", "Timing", "Card", "GpuTiming", "GPU frame timing and valid sample count when FrameTimingManager provides GPU data.", true, false, PerfMeterOverlayModule.Timing, "GpuTiming"),
			new PerfMeterWidgetDescriptor("timing.frame-spikes-card", "Frame spikes card", "Timing", "Card", "Fps / Warnings", "Frame spike counters and active warning state.", true, false, PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Warnings, "Fps", "Warnings"),
			new PerfMeterWidgetDescriptor("overdraw.card", "Overdraw card", "Overdraw", "Card", "Overdraw / Heatmap", "Overdraw ratio, measurement progress, and heatmap state.", true, false, PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap, "Overdraw", "Heatmap"),
			new PerfMeterWidgetDescriptor("timing.cpu-budget-bar", "CPU budget bar", "Timing", "BudgetBar", "Timing", "CPU frame time against target FPS budget.", true, false, PerfMeterOverlayModule.Timing, "Timing"),
			new PerfMeterWidgetDescriptor("timing.gpu-budget-bar", "GPU budget bar", "Timing", "BudgetBar", "GpuTiming", "GPU frame time against target FPS budget.", true, false, PerfMeterOverlayModule.Timing, "GpuTiming"),
			new PerfMeterWidgetDescriptor("graphs.cpu-timing", "CPU timing graph", "Graphs", "Graph", "Graphs / Timing", "Stacked CPU frame, main, render, and other timing history.", true, false, PerfMeterOverlayModule.Graphs | PerfMeterOverlayModule.Timing, "Graphs", "Timing"),
			new PerfMeterWidgetDescriptor("graphs.gpu-timing", "GPU timing graph", "Graphs", "Graph", "Graphs / GpuTiming", "GPU frame timing history with budget line.", true, false, PerfMeterOverlayModule.Graphs | PerfMeterOverlayModule.Timing, "Graphs", "GpuTiming"),
			new PerfMeterWidgetDescriptor("cpu.cores-bars", "CPU core bars", "CPU", "Panel", "CpuCoreBars", "Per-logical-core load bars; uses platform CPU load sampling when available.", true, false, PerfMeterOverlayModule.CpuCoreBars, "CpuCoreSampling"),
			new PerfMeterWidgetDescriptor("cpu.cores-graphs", "CPU core graphs", "CPU", "Panel", "CpuCoreGraphs", "Per-logical-core load history graphs.", true, false, PerfMeterOverlayModule.CpuCoreGraphs, "CpuCoreSampling", "Graphs"),
			new PerfMeterWidgetDescriptor("custom-metrics.panel", "Custom metrics panel", "Custom", "Panel", "CustomMetrics", "Renders metrics supplied by project IPerfMeterCustomMetricProvider implementations.", true, false, PerfMeterOverlayModule.CustomMetrics, "CustomMetrics"),
			new PerfMeterWidgetDescriptor("rendering.summary-card", "Rendering summary card", "Rendering", "Card", "Rendering", "Draw calls, SetPass calls, batches, and vertices.", true, false, PerfMeterOverlayModule.Rendering, "Rendering"),
			new PerfMeterWidgetDescriptor("memory.summary-card", "Memory summary card", "Memory", "Card", "Memory / GC / GPU memory", "System memory, GC memory, and GPU memory counters.", true, false, PerfMeterOverlayModule.Memory | PerfMeterOverlayModule.Gc | PerfMeterOverlayModule.GpuMemory, "Memory", "Gc", "GpuMemory"),
			new PerfMeterWidgetDescriptor("batching.summary-card", "Batching summary card", "Rendering", "Card", "SrpBatcher / Brg", "SRP Batcher and BatchRendererGroup counters.", true, false, PerfMeterOverlayModule.SrpBatcher | PerfMeterOverlayModule.Brg, "SrpBatcher", "Brg"),
			new PerfMeterWidgetDescriptor("uploads.summary-card", "Uploads summary card", "Rendering", "Card", "Uploads", "Index/upload counters.", true, false, PerfMeterOverlayModule.Uploads, "Uploads"),

			new PerfMeterWidgetDescriptor("fps.row", "FPS row / bar", "FPS", "Text row / metric bar", "Fps", "FPS summary rendered in text layouts or MetricBars layout.", false, false, PerfMeterOverlayModule.Fps, "Fps"),
			new PerfMeterWidgetDescriptor("timing.rows", "CPU timing rows / bars", "Timing", "Text row / metric bar", "Timing", "CPU frame, main, render, present wait, and range statistics.", false, false, PerfMeterOverlayModule.Timing, "Timing"),
			new PerfMeterWidgetDescriptor("gpu.validity-row", "GPU validity row / bar", "Timing", "Text row / metric bar", "Timing", "GPU valid sample count versus collected frame samples.", false, false, PerfMeterOverlayModule.Timing, "GpuTiming"),
			new PerfMeterWidgetDescriptor("rendering.rows", "Rendering counters rows / bars", "Rendering", "Text row / metric bar", "Rendering", "Draw calls, SetPass calls, batches, and vertices.", false, false, PerfMeterOverlayModule.Rendering, "Rendering"),
			new PerfMeterWidgetDescriptor("srp-batcher.row", "SRP Batcher row / bar", "Rendering", "Text row / metric bar", "SrpBatcher", "SRP Batcher instance counter when Unity exposes it.", false, false, PerfMeterOverlayModule.SrpBatcher, "SrpBatcher"),
			new PerfMeterWidgetDescriptor("brg.rows", "BRG/GRD rows / bars", "Rendering", "Text row / metric bar", "Brg", "Batch Renderer Group / GPU Resident Drawer draw and instance counters.", false, false, PerfMeterOverlayModule.Brg, "Brg"),
			new PerfMeterWidgetDescriptor("uploads.row", "Index upload row / bar", "Rendering", "Text row / metric bar", "Uploads", "Index buffer upload bytes in frame.", false, false, PerfMeterOverlayModule.Uploads, "Uploads"),
			new PerfMeterWidgetDescriptor("overdraw.status-row", "Overdraw status row / bar", "Overdraw", "Text row / metric bar", "Overdraw / Heatmap", "Measurement state, progress, ratio, and heatmap visibility.", false, false, PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap, "Overdraw", "Heatmap"),
			new PerfMeterWidgetDescriptor("memory.rows", "System memory row / bar", "Memory", "Text row / metric bar", "Memory", "System used memory in MB.", false, false, PerfMeterOverlayModule.Memory, "Memory"),
			new PerfMeterWidgetDescriptor("gc.row", "GC memory row / bar", "Memory", "Text row / metric bar", "Gc", "GC reserved memory in MB.", false, false, PerfMeterOverlayModule.Gc, "Gc"),
			new PerfMeterWidgetDescriptor("gpu-memory.row", "GPU memory row / bar", "Memory", "Text row / metric bar", "GpuMemory", "GPU memory counter in MB when Unity exposes it.", false, false, PerfMeterOverlayModule.GpuMemory, "GpuMemory"),
			new PerfMeterWidgetDescriptor("warnings.row", "Warning row", "Warnings", "Text row", "Warnings", "Current overlay warning with short hold time.", false, false, PerfMeterOverlayModule.Warnings, "Warnings"),
			new PerfMeterWidgetDescriptor("custom-metrics.slot", "Custom metric slot", "Custom", "Custom metric row / bar", "CustomMetrics", "Internal custom metric slot rendered by custom metrics panel.", false, false, PerfMeterOverlayModule.CustomMetrics, "CustomMetrics")
		};

		public static PerfMeterWidgetDescriptor[] GetAllDescriptors()
		{
			PerfMeterWidgetDescriptor[] copy = new PerfMeterWidgetDescriptor[BuiltInDescriptors.Length];
			Array.Copy(BuiltInDescriptors, copy, BuiltInDescriptors.Length);
			return copy;
		}

		public static PerfMeterWidgetDescriptor[] GetPresetBlockDescriptors()
		{
			List<PerfMeterWidgetDescriptor> descriptors = new List<PerfMeterWidgetDescriptor>();
			for (int i = 0; i < BuiltInDescriptors.Length; i++)
			{
				if (BuiltInDescriptors[i].IsPresetBlock)
				{
					descriptors.Add(BuiltInDescriptors[i]);
				}
			}

			return descriptors.ToArray();
		}

		public static bool TryGetDescriptor(string id, out PerfMeterWidgetDescriptor descriptor)
		{
			for (int i = 0; i < BuiltInDescriptors.Length; i++)
			{
				if (string.Equals(BuiltInDescriptors[i].Id, id, StringComparison.OrdinalIgnoreCase))
				{
					descriptor = BuiltInDescriptors[i];
					return true;
				}
			}

			descriptor = null;
			return false;
		}
	}

	public static class PerfMeterOverlayPresetUtility
	{
		public const string Schema = "sgg-perfmeter.overlay-preset";
		public const int CurrentVersion = 1;

		public static string ToJson(PerfMeterOverlayPresetJson preset)
		{
			PerfMeterOverlayPresetJson normalized = Clone(preset ?? PerfMeterOverlayPresetDefaults.CreateCompactTiming());
			NormalizeForSave(normalized);
			return JsonUtility.ToJson(normalized, true);
		}

		public static bool TryReadJson(string json, out PerfMeterOverlayPresetJson preset, out string warning)
		{
			preset = null;
			warning = string.Empty;
			if (string.IsNullOrWhiteSpace(json))
			{
				warning = "Overlay preset JSON is empty.";
				return false;
			}

			try
			{
				preset = JsonUtility.FromJson<PerfMeterOverlayPresetJson>(json);
				PerfMeterOverlayPresetValidationResult validation = Validate(preset);
				warning = validation.Warning;
				return validation.IsValid;
			}
			catch (Exception exception)
			{
				warning = "Overlay preset JSON is invalid: " + exception.Message;
				preset = null;
				return false;
			}
		}

		public static PerfMeterOverlayPresetValidationResult Validate(PerfMeterOverlayPresetJson preset)
		{
			string warning = string.Empty;
			if (preset == null)
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset is null.");
			}

			if (!string.Equals(preset.schema, Schema, StringComparison.Ordinal))
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset schema must be '" + Schema + "'.");
			}

			if (preset.version != CurrentVersion)
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset schema version " + preset.version + " is unsupported.");
			}

			if (string.IsNullOrWhiteSpace(preset.id))
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset id is empty.");
			}

			if (preset.style == null)
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset style is missing.");
			}

			if (preset.style.scale <= 0f)
			{
				return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset scale must be greater than zero.");
			}

			if (preset.style.opacity < 0f || preset.style.opacity > 1f)
			{
				warning = CombineWarnings(warning, "Overlay preset opacity was clamped to 0..1.");
			}

			HashSet<string> widgetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			PerfMeterOverlayPresetWidgetJson[] widgets = preset.widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>();
			for (int i = 0; i < widgets.Length; i++)
			{
				PerfMeterOverlayPresetWidgetJson widget = widgets[i];
				if (widget == null || string.IsNullOrWhiteSpace(widget.id))
				{
					warning = CombineWarnings(warning, "Overlay preset contains an empty widget id.");
					continue;
				}

				if (!widgetIds.Add(widget.id))
				{
					return new PerfMeterOverlayPresetValidationResult(false, "Overlay preset contains duplicate widget id '" + widget.id + "'.");
				}

				if (!PerfMeterWidgetRegistry.TryGetDescriptor(widget.id, out PerfMeterWidgetDescriptor descriptor))
				{
					warning = CombineWarnings(warning, "Unknown overlay widget '" + widget.id + "' will be ignored.");
					continue;
				}

				if (widget.enabled && !descriptor.IsPresetBlock)
				{
					warning = CombineWarnings(warning, "Low-level widget '" + widget.id + "' cannot be used in visual presets and will be ignored.");
				}
			}

			return new PerfMeterOverlayPresetValidationResult(true, warning);
		}

		internal static PerfMeterOverlayPresetJson Clone(PerfMeterOverlayPresetJson preset)
		{
			if (preset == null)
			{
				return null;
			}

			return JsonUtility.FromJson<PerfMeterOverlayPresetJson>(JsonUtility.ToJson(preset));
		}

		internal static PerfMeterOverlayPresetJson FindById(PerfMeterOverlayPresetJson[] presets, string id)
		{
			if (presets == null || presets.Length == 0)
			{
				return null;
			}

			for (int i = 0; i < presets.Length; i++)
			{
				PerfMeterOverlayPresetJson preset = presets[i];
				if (preset != null && string.Equals(preset.id, id, StringComparison.OrdinalIgnoreCase))
				{
					return preset;
				}
			}

			return null;
		}

		internal static PerfMeterOverlayModule GetEnabledModules(PerfMeterOverlayPresetJson preset, out string warning)
		{
			warning = string.Empty;
			if (preset == null || preset.widgets == null || preset.widgets.Length == 0)
			{
				warning = "Overlay preset has no widgets; FPS summary fallback is used.";
				return PerfMeterOverlayModule.Fps;
			}

			PerfMeterOverlayModule modules = PerfMeterOverlayModule.None;
			for (int i = 0; i < preset.widgets.Length; i++)
			{
				PerfMeterOverlayPresetWidgetJson widget = preset.widgets[i];
				if (widget == null || !widget.enabled || string.IsNullOrWhiteSpace(widget.id))
				{
					continue;
				}

				if (!PerfMeterWidgetRegistry.TryGetDescriptor(widget.id, out PerfMeterWidgetDescriptor descriptor))
				{
					warning = CombineWarnings(warning, "Unknown overlay widget '" + widget.id + "' was ignored.");
					continue;
				}

				if (!descriptor.IsPresetBlock)
				{
					warning = CombineWarnings(warning, "Low-level widget '" + widget.id + "' was ignored.");
					continue;
				}

				modules |= descriptor.OverlayModules;
			}

			if (modules == PerfMeterOverlayModule.None)
			{
				warning = CombineWarnings(warning, "Overlay preset has no enabled preset blocks; FPS summary fallback is used.");
				modules = PerfMeterOverlayModule.Fps;
			}

			return modules;
		}

		internal static PerfMeterOverlayCorner GetCorner(PerfMeterOverlayPresetJson preset)
		{
			string value = preset?.style != null ? preset.style.anchor : string.Empty;
			return Enum.TryParse(value, true, out PerfMeterOverlayCorner corner) && Enum.IsDefined(typeof(PerfMeterOverlayCorner), corner)
				? corner
				: PerfMeterOverlayCorner.TopRight;
		}

		internal static PerfMeterOverlayLayout GetLayout(PerfMeterOverlayPresetJson preset)
		{
			string value = preset?.style != null ? preset.style.layout : string.Empty;
			if (string.Equals(value, "WideDiagnostics", StringComparison.OrdinalIgnoreCase))
			{
				value = nameof(PerfMeterOverlayLayout.DiagnosticsWide);
			}
			else if (string.Equals(value, "ClassicCards", StringComparison.OrdinalIgnoreCase))
			{
				value = nameof(PerfMeterOverlayLayout.Classic);
			}

			return PerfMeterSettingsStore.ParseOverlayLayout(value);
		}

		internal static PerfMeterOverlayTheme GetTheme(PerfMeterOverlayPresetJson preset)
		{
			return PerfMeterSettingsStore.ParseOverlayTheme(preset?.style != null ? preset.style.theme : string.Empty);
		}

		internal static PerfMeterOverlayFontFamily GetFontFamily(PerfMeterOverlayPresetJson preset)
		{
			return PerfMeterSettingsStore.ParseOverlayFontFamily(preset?.style != null ? preset.style.font : string.Empty);
		}

		internal static float GetScale(PerfMeterOverlayPresetJson preset, float fallback)
		{
			return preset?.style != null && preset.style.scale > 0f ? preset.style.scale : fallback;
		}

		internal static float GetOpacity(PerfMeterOverlayPresetJson preset, float fallback)
		{
			return preset?.style != null ? Mathf.Clamp01(preset.style.opacity) : fallback;
		}

		internal static string BuildSummary(PerfMeterOverlayPresetJson preset)
		{
			if (preset == null)
			{
				return "No preset";
			}

			int enabledCount = 0;
			PerfMeterOverlayPresetWidgetJson[] widgets = preset.widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>();
			for (int i = 0; i < widgets.Length; i++)
			{
				if (widgets[i] != null && widgets[i].enabled)
				{
					enabledCount++;
				}
			}

			string anchor = preset.style != null ? preset.style.anchor : nameof(PerfMeterOverlayCorner.TopRight);
			string layout = preset.style != null ? preset.style.layout : nameof(PerfMeterOverlayLayout.CompactCards);
			string theme = preset.style != null ? preset.style.theme : nameof(PerfMeterOverlayTheme.ClassicDark);
			string font = preset.style != null ? preset.style.font : nameof(PerfMeterOverlayFontFamily.Manrope);
			return anchor + " · " + layout + " · " + theme + " · " + font + " · " + enabledCount + " widgets";
		}

		internal static void NormalizeForSave(PerfMeterOverlayPresetJson preset)
		{
			if (preset == null)
			{
				return;
			}

			preset.schema = Schema;
			preset.version = CurrentVersion;
			preset.id = string.IsNullOrWhiteSpace(preset.id) ? PerfMeterOverlayPresetDefaults.CompactTimingId : preset.id.Trim();
			preset.displayName = string.IsNullOrWhiteSpace(preset.displayName) ? preset.id : preset.displayName.Trim();
			preset.description = preset.description ?? string.Empty;
			preset.tags = preset.tags ?? Array.Empty<string>();
			preset.style = preset.style ?? new PerfMeterOverlayPresetStyleJson();
			preset.style.anchor = GetCorner(preset).ToString();
			preset.style.layout = GetLayout(preset).ToString();
			preset.style.theme = GetTheme(preset).ToString();
			preset.style.font = GetFontFamily(preset).ToString();
			preset.style.scale = Mathf.Clamp(preset.style.scale <= 0f ? 1f : preset.style.scale, PerfMeterSettingsStore.MinOverlayScale, PerfMeterSettingsStore.MaxOverlayScale);
			preset.style.opacity = Mathf.Clamp(preset.style.opacity, 0f, 1f);
			preset.style.maxWidth = Mathf.Max(1, preset.style.maxWidth);
			preset.style.gap = Mathf.Max(0, preset.style.gap);
			preset.widgets = preset.widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>();
			Array.Sort(preset.widgets, CompareWidgetOrder);
		}

		internal static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			if (string.IsNullOrEmpty(second))
			{
				return first;
			}

			return first + " " + second;
		}

		private static int CompareWidgetOrder(PerfMeterOverlayPresetWidgetJson left, PerfMeterOverlayPresetWidgetJson right)
		{
			int leftOrder = left != null ? left.order : int.MaxValue;
			int rightOrder = right != null ? right.order : int.MaxValue;
			int order = leftOrder.CompareTo(rightOrder);
			if (order != 0)
			{
				return order;
			}

			return string.Compare(left?.id, right?.id, StringComparison.OrdinalIgnoreCase);
		}
	}

	public static class PerfMeterOverlayPresetDefaults
	{
		public const string DefaultId = "default";
		public const string FpsOnlyId = "fps-only";
		public const string CompactTimingId = "compact-timing";
		public const string ClassicCardsId = "classic-cards";
		public const string GraphsId = "graphs";
		public const string FullDiagnosticsId = "full-diagnostics";

		public static PerfMeterOverlayPresetJson[] CreateDefaultPresets()
		{
			return new[]
			{
				CreateDefault(),
				CreateFpsOnly(),
				CreateCompactTiming(),
				CreateClassicCards(),
				CreateGraphs(),
				CreateFullDiagnostics()
			};
		}

		public static PerfMeterOverlayPresetJson CreateDefault()
		{
			PerfMeterOverlayPresetJson preset = CreateFullDiagnostics();
			preset.id = DefaultId;
			preset.displayName = "Default";
			preset.description = "Default PerfMeter diagnostics overlay for zero-code setup with compact metric bars.";
			preset.style.layout = nameof(PerfMeterOverlayLayout.MetricBars);
			return preset;
		}

		public static PerfMeterOverlayPresetJson CreateFpsOnly()
		{
			return CreatePreset(
				FpsOnlyId,
				"FPS Only",
				"Super-minimal one-line overlay with current FPS, average FPS, 1% low, 0.1% low, and render-thread time.",
				"FpsOnly",
				360,
				Widget("fps.summary-card", 10),
				Widget("timing.cpu-card", 20));
		}

		public static PerfMeterOverlayPresetJson CreateCompactTiming()
		{
			return CreatePreset(
				CompactTimingId,
				"Compact Timing",
				"Compact overlay with FPS, CPU/GPU timing cards and budget bars.",
				"CompactCards",
				420,
				Widget("fps.summary-card", 10),
				Widget("timing.cpu-card", 20),
				Widget("timing.gpu-card", 30),
				Widget("timing.cpu-budget-bar", 40),
				Widget("timing.gpu-budget-bar", 50));
		}

		public static PerfMeterOverlayPresetJson CreateClassicCards()
		{
			return CreatePreset(
				ClassicCardsId,
				"Classic Cards",
				"Main timing and diagnostic cards without graphs.",
				"Classic",
				520,
				Widget("fps.summary-card", 10),
				Widget("timing.cpu-card", 20),
				Widget("timing.gpu-card", 30),
				Widget("timing.frame-spikes-card", 40),
				Widget("rendering.summary-card", 50),
				Widget("memory.summary-card", 60));
		}

		public static PerfMeterOverlayPresetJson CreateGraphs()
		{
			return CreatePreset(
				GraphsId,
				"Graphs",
				"Timing-focused overlay with CPU and GPU history graphs.",
				"Graphs",
				620,
				Widget("fps.summary-card", 10),
				Widget("timing.cpu-card", 20),
				Widget("timing.gpu-card", 30),
				Widget("graphs.cpu-timing", 40, 48),
				Widget("graphs.gpu-timing", 50, 48));
		}

		public static PerfMeterOverlayPresetJson CreateFullDiagnostics()
		{
			return CreatePreset(
				FullDiagnosticsId,
				"Full Diagnostics",
				"Wide diagnostic overlay with major high-level PerfMeter widgets enabled.",
				"DiagnosticsWide",
				720,
				Widget("fps.summary-card", 10),
				Widget("timing.cpu-card", 20),
				Widget("timing.gpu-card", 30),
				Widget("timing.frame-spikes-card", 40),
				Widget("timing.cpu-budget-bar", 50),
				Widget("timing.gpu-budget-bar", 60),
				Widget("graphs.cpu-timing", 70, 48),
				Widget("graphs.gpu-timing", 80, 48),
				Widget("cpu.cores-bars", 90),
				Widget("cpu.cores-graphs", 100, 48, false),
				Widget("overdraw.card", 110),
				Widget("rendering.summary-card", 120),
				Widget("memory.summary-card", 130),
				Widget("batching.summary-card", 140),
				Widget("uploads.summary-card", 150),
				Widget("custom-metrics.panel", 160));
		}

		private static PerfMeterOverlayPresetJson CreatePreset(string id, string displayName, string description, string layout, int maxWidth, params PerfMeterOverlayPresetWidgetJson[] widgets)
		{
			return new PerfMeterOverlayPresetJson
			{
				schema = PerfMeterOverlayPresetUtility.Schema,
				version = PerfMeterOverlayPresetUtility.CurrentVersion,
				id = id,
				displayName = displayName,
				description = description,
				tags = Array.Empty<string>(),
				style = new PerfMeterOverlayPresetStyleJson
				{
					anchor = nameof(PerfMeterOverlayCorner.TopRight),
					layout = layout,
					theme = nameof(PerfMeterOverlayTheme.ClassicDark),
					font = nameof(PerfMeterOverlayFontFamily.Manrope),
					scale = 1f,
					opacity = 0.84f,
					maxWidth = maxWidth,
					gap = 4
				},
				widgets = widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>()
			};
		}

		private static PerfMeterOverlayPresetWidgetJson Widget(string id, int order, int height = 0, bool enabled = true)
		{
			return new PerfMeterOverlayPresetWidgetJson
			{
				id = id,
				enabled = enabled,
				order = order,
				height = height
			};
		}
	}
}
