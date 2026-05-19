using System;
using UnityEngine;

namespace SGG.PerfMeter
{
	public enum PerfMeterSettingsLoadState
	{
		NotLoaded = 0,
		Missing = 1,
		Loaded = 2,
		Invalid = 3,
		UnsupportedVersion = 4
	}

	public readonly struct PerfMeterSettingsSnapshot
	{
		internal PerfMeterSettingsSnapshot(
			bool enabled,
			bool autoStart,
			bool overlayVisible,
			PerfMeterOverlayCorner overlayCorner,
			PerfMeterOverlayMode overlayMode,
			PerfMeterTargetFps targetFps,
			string activePreset,
			PerfMeterOverlayModule overlayModules,
			int sessionWarmupFrames,
			float sessionSampleIntervalSeconds,
			int sessionMaxSamples,
			PerfMeterSettingsLoadState loadState,
			string warning)
		{
			Enabled = enabled;
			AutoStart = autoStart;
			OverlayVisible = overlayVisible;
			OverlayCorner = overlayCorner;
			OverlayMode = overlayMode;
			TargetFps = targetFps;
			ActivePreset = string.IsNullOrEmpty(activePreset) ? PerfMeterSettingsStore.DefaultPresetId : activePreset;
			OverlayModules = overlayModules == PerfMeterOverlayModule.None ? PerfMeterSettingsStore.GetPresetModules(PerfMeterSettingsStore.ParseOverlayPreset(ActivePreset)) : overlayModules;
			SessionWarmupFrames = Mathf.Max(0, sessionWarmupFrames);
			SessionSampleIntervalSeconds = sessionSampleIntervalSeconds > 0f ? sessionSampleIntervalSeconds : PerfMeterSessionOptions.DefaultSampleIntervalSeconds;
			SessionMaxSamples = Mathf.Max(1, sessionMaxSamples);
			LoadState = loadState;
			Warning = warning ?? string.Empty;
		}

		public bool Enabled { get; }
		public bool AutoStart { get; }
		public bool OverlayVisible { get; }
		public PerfMeterOverlayCorner OverlayCorner { get; }
		public PerfMeterOverlayMode OverlayMode { get; }
		public PerfMeterTargetFps TargetFps { get; }
		public string ActivePreset { get; }
		public PerfMeterOverlayModule OverlayModules { get; }
		public int SessionWarmupFrames { get; }
		public float SessionSampleIntervalSeconds { get; }
		public int SessionMaxSamples { get; }
		public PerfMeterSettingsLoadState LoadState { get; }
		public string Warning { get; }
	}

	[Serializable]
	internal sealed class PerfMeterSettingsJson
	{
		public int schemaVersion = PerfMeterSettingsStore.CurrentSchemaVersion;
		public bool enabled = true;
		public bool autoStart = true;
		public bool overlayVisible = true;
		public string overlayCorner = nameof(PerfMeterOverlayCorner.TopRight);
		public string overlayMode = nameof(PerfMeterOverlayMode.Full);
		public int targetFps = (int)PerfMeterTargetFps.Fps60;
		public string activePreset = PerfMeterSettingsStore.DefaultPresetId;
		public PerfMeterPresetSettingsJson[] presets = Array.Empty<PerfMeterPresetSettingsJson>();
		public PerfMeterRuleDefaultsJson ruleDefaults = new PerfMeterRuleDefaultsJson();
		public PerfMeterSessionSettingsJson session = new PerfMeterSessionSettingsJson();
		public PerfMeterOverdrawSettingsJson overdraw = new PerfMeterOverdrawSettingsJson();
	}

	[Serializable]
	internal sealed class PerfMeterPresetSettingsJson
	{
		public string id = string.Empty;
		public bool overlayVisible = true;
		public string overlayMode = nameof(PerfMeterOverlayMode.Full);
		public int targetFps = (int)PerfMeterTargetFps.Fps60;
		public string[] modules = Array.Empty<string>();
	}

	[Serializable]
	internal sealed class PerfMeterRuleDefaultsJson
	{
		public float editorWarningCooldownSeconds = 8f;
		public float structuredLogCooldownSeconds = 2f;
		public float callbackCooldownSeconds = 0.5f;
	}

	[Serializable]
	internal sealed class PerfMeterSessionSettingsJson
	{
		public int warmupFrames = 0;
		public float sampleIntervalSeconds = 0.25f;
		public int maxSamples = 4096;
	}

	[Serializable]
	internal sealed class PerfMeterOverdrawSettingsJson
	{
		public int defaultFrameCount = 60;
		public int maxFrameCount = 600;
	}

	internal static class PerfMeterSettingsStore
	{
		internal const int CurrentSchemaVersion = 1;
		internal const string DefaultPresetId = "FullDiagnostics";
		internal const string ResourcesLoadPath = "SGG.PerfMeter/perfmeter-settings";
		internal const string ResourcesAssetPath = "Assets/Resources/SGG.PerfMeter/perfmeter-settings.json";

		internal static PerfMeterSettingsSnapshot Defaults => ToSnapshot(CreateDefault(), PerfMeterSettingsLoadState.Missing, string.Empty);

		internal static PerfMeterSettingsJson CreateDefault()
		{
			return new PerfMeterSettingsJson
			{
				schemaVersion = CurrentSchemaVersion,
				enabled = true,
				autoStart = true,
				overlayVisible = true,
				overlayCorner = nameof(PerfMeterOverlayCorner.TopRight),
				overlayMode = nameof(PerfMeterOverlayMode.Full),
				targetFps = (int)PerfMeterTargetFps.Fps60,
				activePreset = DefaultPresetId,
				presets = CreateDefaultPresets(),
				ruleDefaults = new PerfMeterRuleDefaultsJson(),
				session = new PerfMeterSessionSettingsJson(),
				overdraw = new PerfMeterOverdrawSettingsJson()
			};
		}

		internal static PerfMeterSettingsJson CreateFromSnapshot(PerfMeterSettingsSnapshot snapshot)
		{
			PerfMeterSettingsJson settings = CreateDefault();
			settings.enabled = snapshot.Enabled;
			settings.autoStart = snapshot.AutoStart;
			settings.overlayVisible = snapshot.OverlayVisible;
			settings.overlayCorner = snapshot.OverlayCorner.ToString();
			settings.overlayMode = snapshot.OverlayMode.ToString();
			settings.targetFps = (int)snapshot.TargetFps;
			settings.activePreset = string.IsNullOrEmpty(snapshot.ActivePreset) ? DefaultPresetId : snapshot.ActivePreset;
			settings.session.warmupFrames = snapshot.SessionWarmupFrames;
			settings.session.sampleIntervalSeconds = snapshot.SessionSampleIntervalSeconds;
			settings.session.maxSamples = snapshot.SessionMaxSamples;
			ApplySnapshotToPreset(settings, snapshot);
			return settings;
		}

		internal static string ToJson(PerfMeterSettingsJson settings)
		{
			Normalize(settings, out string _);
			return JsonUtility.ToJson(settings, true);
		}

		internal static PerfMeterSettingsSnapshot LoadFromResources()
		{
			TextAsset settingsAsset = Resources.Load<TextAsset>(ResourcesLoadPath);
			if (settingsAsset == null)
			{
				return Defaults;
			}

			return TryReadSnapshot(settingsAsset.text, out PerfMeterSettingsSnapshot snapshot)
				? snapshot
				: snapshot;
		}

		internal static bool TryReadSnapshot(string json, out PerfMeterSettingsSnapshot snapshot)
		{
			if (!TryReadJson(json, out PerfMeterSettingsJson settings, out PerfMeterSettingsLoadState loadState, out string warning))
			{
				snapshot = ToSnapshot(CreateDefault(), loadState, warning);
				return false;
			}

			snapshot = ToSnapshot(settings, loadState, warning);
			return true;
		}

		internal static bool TryReadJson(string json, out PerfMeterSettingsJson settings, out PerfMeterSettingsLoadState loadState, out string warning)
		{
			settings = CreateDefault();
			loadState = PerfMeterSettingsLoadState.Invalid;
			warning = string.Empty;

			if (string.IsNullOrWhiteSpace(json))
			{
				warning = "PerfMeter settings JSON is empty; defaults are used.";
				return false;
			}

			try
			{
				PerfMeterSettingsJson parsed = JsonUtility.FromJson<PerfMeterSettingsJson>(json);
				if (parsed == null)
				{
					warning = "PerfMeter settings JSON could not be parsed; defaults are used.";
					return false;
				}

				if (parsed.schemaVersion > CurrentSchemaVersion)
				{
					loadState = PerfMeterSettingsLoadState.UnsupportedVersion;
					warning = "PerfMeter settings schema " + parsed.schemaVersion + " is newer than supported schema " + CurrentSchemaVersion + "; defaults are used.";
					return false;
				}

				settings = parsed;
				Normalize(settings, out warning);
				loadState = PerfMeterSettingsLoadState.Loaded;
				return true;
			}
			catch (Exception exception)
			{
				warning = "PerfMeter settings JSON is invalid: " + exception.Message;
				return false;
			}
		}

		internal static PerfMeterSettingsSnapshot ToSnapshot(PerfMeterSettingsJson settings, PerfMeterSettingsLoadState loadState, string warning)
		{
			Normalize(settings, out string normalizeWarning);
			PerfMeterPresetSettingsJson activePreset = FindPreset(settings, settings.activePreset);
			string moduleWarning = string.Empty;
			PerfMeterOverlayMode overlayMode = ParseOverlayMode(settings.overlayMode);
			PerfMeterTargetFps targetFps = ParseTargetFps(settings.targetFps);
			bool overlayVisible = settings.overlayVisible;
			PerfMeterOverlayModule overlayModules = GetPresetModules(ParseOverlayPreset(settings.activePreset));

			if (activePreset != null)
			{
				overlayVisible = activePreset.overlayVisible;
				overlayMode = ParseOverlayMode(activePreset.overlayMode);
				targetFps = ParseTargetFps(activePreset.targetFps);
				overlayModules = ParseModules(activePreset.modules, out moduleWarning);
			}
			else
			{
				moduleWarning = "Active overlay preset was not found; top-level overlay settings are used.";
			}

			return new PerfMeterSettingsSnapshot(
				settings.enabled,
				settings.autoStart,
				overlayVisible,
				ParseOverlayCorner(settings.overlayCorner),
				overlayMode,
				targetFps,
				settings.activePreset,
				overlayModules,
				settings.session != null ? settings.session.warmupFrames : 0,
				settings.session != null ? settings.session.sampleIntervalSeconds : PerfMeterSessionOptions.DefaultSampleIntervalSeconds,
				settings.session != null ? settings.session.maxSamples : PerfMeterSessionOptions.DefaultMaxSamples,
				loadState,
				CombineWarnings(CombineWarnings(warning, normalizeWarning), moduleWarning));
		}

		internal static void ApplySnapshotToRuntime(PerfMeterSettingsSnapshot settings)
		{
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.SetOverlayPreset(ParseOverlayPreset(settings.ActivePreset));
			PerformanceMeter.SetOverlayModules(settings.OverlayModules);
			PerformanceMeter.SetTargetFps(settings.TargetFps);
			PerformanceMeter.SetOverlayCorner(settings.OverlayCorner);
			PerformanceMeter.SetOverlayMode(settings.OverlayMode);
			PerformanceMeter.SetOverlayVisible(settings.OverlayVisible);
		}

		internal static PerfMeterOverlayPreset ParseOverlayPreset(string value)
		{
			return Enum.TryParse(value, true, out PerfMeterOverlayPreset preset) && Enum.IsDefined(typeof(PerfMeterOverlayPreset), preset)
				? preset
				: PerfMeterOverlayPreset.FullDiagnostics;
		}

		internal static PerfMeterOverlayModule GetPresetModules(PerfMeterOverlayPreset preset)
		{
			switch (preset)
			{
				case PerfMeterOverlayPreset.Minimal:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Warnings;
				case PerfMeterOverlayPreset.Timing:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Graphs | PerfMeterOverlayModule.Warnings;
				case PerfMeterOverlayPreset.Rendering:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Rendering | PerfMeterOverlayModule.SrpBatcher | PerfMeterOverlayModule.Brg | PerfMeterOverlayModule.Uploads | PerfMeterOverlayModule.Warnings;
				case PerfMeterOverlayPreset.Memory:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Memory | PerfMeterOverlayModule.Gc | PerfMeterOverlayModule.GpuMemory | PerfMeterOverlayModule.Warnings;
				case PerfMeterOverlayPreset.Overdraw:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap | PerfMeterOverlayModule.Warnings;
				case PerfMeterOverlayPreset.AgentDebug:
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Rendering | PerfMeterOverlayModule.SrpBatcher | PerfMeterOverlayModule.Brg | PerfMeterOverlayModule.Uploads | PerfMeterOverlayModule.Memory | PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap | PerfMeterOverlayModule.Warnings;
				default:
					return PerfMeterOverlayModule.All;
			}
		}

		internal static PerfMeterOverlayMode GetPresetMode(PerfMeterOverlayPreset preset)
		{
			switch (preset)
			{
				case PerfMeterOverlayPreset.Minimal:
					return PerfMeterOverlayMode.FpsOnly;
				case PerfMeterOverlayPreset.Timing:
					return PerfMeterOverlayMode.Graphs;
				case PerfMeterOverlayPreset.AgentDebug:
					return PerfMeterOverlayMode.TextCompact;
				default:
					return PerfMeterOverlayMode.Full;
			}
		}

		private static PerfMeterPresetSettingsJson[] CreateDefaultPresets()
		{
			return new[]
			{
				CreatePreset("Minimal", true, GetPresetMode(PerfMeterOverlayPreset.Minimal), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.Minimal))),
				CreatePreset("Timing", true, GetPresetMode(PerfMeterOverlayPreset.Timing), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.Timing))),
				CreatePreset("Rendering", true, GetPresetMode(PerfMeterOverlayPreset.Rendering), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.Rendering))),
				CreatePreset("Memory", true, GetPresetMode(PerfMeterOverlayPreset.Memory), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.Memory))),
				CreatePreset("Overdraw", true, GetPresetMode(PerfMeterOverlayPreset.Overdraw), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.Overdraw))),
				CreatePreset(DefaultPresetId, true, GetPresetMode(PerfMeterOverlayPreset.FullDiagnostics), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.FullDiagnostics))),
				CreatePreset("AgentDebug", true, GetPresetMode(PerfMeterOverlayPreset.AgentDebug), PerfMeterTargetFps.Fps60, ModulesToNames(GetPresetModules(PerfMeterOverlayPreset.AgentDebug)))
			};
		}

		private static PerfMeterPresetSettingsJson CreatePreset(string id, bool overlayVisible, PerfMeterOverlayMode overlayMode, PerfMeterTargetFps targetFps, params string[] modules)
		{
			return new PerfMeterPresetSettingsJson
			{
				id = id,
				overlayVisible = overlayVisible,
				overlayMode = overlayMode.ToString(),
				targetFps = (int)targetFps,
				modules = modules ?? Array.Empty<string>()
			};
		}

		private static void ApplySnapshotToPreset(PerfMeterSettingsJson settings, PerfMeterSettingsSnapshot snapshot)
		{
			if (settings.presets == null || settings.presets.Length == 0)
			{
				settings.presets = CreateDefaultPresets();
			}

			for (int i = 0; i < settings.presets.Length; i++)
			{
				PerfMeterPresetSettingsJson preset = settings.presets[i];
				if (preset != null && string.Equals(preset.id, snapshot.ActivePreset, StringComparison.OrdinalIgnoreCase))
				{
					preset.overlayVisible = snapshot.OverlayVisible;
					preset.overlayMode = snapshot.OverlayMode.ToString();
					preset.targetFps = (int)snapshot.TargetFps;
					preset.modules = ModulesToNames(snapshot.OverlayModules);
					return;
				}
			}
		}

		private static PerfMeterPresetSettingsJson FindPreset(PerfMeterSettingsJson settings, string presetId)
		{
			if (settings.presets == null)
			{
				return null;
			}

			for (int i = 0; i < settings.presets.Length; i++)
			{
				PerfMeterPresetSettingsJson preset = settings.presets[i];
				if (preset != null && string.Equals(preset.id, presetId, StringComparison.OrdinalIgnoreCase))
				{
					return preset;
				}
			}

			return null;
		}

		private static PerfMeterOverlayModule ParseModules(string[] modules, out string warning)
		{
			warning = string.Empty;
			if (modules == null || modules.Length == 0)
			{
				return PerfMeterOverlayModule.All;
			}

			PerfMeterOverlayModule result = PerfMeterOverlayModule.None;
			for (int i = 0; i < modules.Length; i++)
			{
				if (Enum.TryParse(modules[i], true, out PerfMeterOverlayModule module) && module != PerfMeterOverlayModule.None && (module & ~PerfMeterOverlayModule.All) == 0)
				{
					result |= module;
				}
				else
				{
					warning = CombineWarnings(warning, "Unknown overlay module '" + modules[i] + "' was ignored.");
				}
			}

			return result == PerfMeterOverlayModule.None ? PerfMeterOverlayModule.All : result;
		}

		private static string[] ModulesToNames(PerfMeterOverlayModule modules)
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
				PerfMeterOverlayModule.Warnings
			};
			int count = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if ((normalized & values[i]) != 0)
				{
					count++;
				}
			}

			string[] names = new string[count];
			int index = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if ((normalized & values[i]) != 0)
				{
					names[index++] = values[i].ToString();
				}
			}

			return names;
		}

		private static void Normalize(PerfMeterSettingsJson settings, out string warning)
		{
			warning = string.Empty;
			if (settings == null)
			{
				return;
			}

			if (settings.schemaVersion <= 0)
			{
				settings.schemaVersion = CurrentSchemaVersion;
			}

			if (string.IsNullOrEmpty(settings.overlayCorner) || !TryParseOverlayCorner(settings.overlayCorner, out PerfMeterOverlayCorner corner))
			{
				settings.overlayCorner = nameof(PerfMeterOverlayCorner.TopRight);
				warning = CombineWarnings(warning, "Invalid overlayCorner; TopRight is used.");
			}
			else
			{
				settings.overlayCorner = corner.ToString();
			}

			if (string.IsNullOrEmpty(settings.overlayMode) || !TryParseOverlayMode(settings.overlayMode, out PerfMeterOverlayMode mode))
			{
				settings.overlayMode = nameof(PerfMeterOverlayMode.Full);
				warning = CombineWarnings(warning, "Invalid overlayMode; Full is used.");
			}
			else
			{
				settings.overlayMode = mode.ToString();
			}

			PerfMeterTargetFps targetFps = ParseTargetFps(settings.targetFps);
			if ((int)targetFps != settings.targetFps)
			{
				warning = CombineWarnings(warning, "Invalid targetFps; 60 FPS is used.");
			}

			settings.targetFps = (int)targetFps;

			if (string.IsNullOrEmpty(settings.activePreset))
			{
				settings.activePreset = DefaultPresetId;
			}

			if (settings.presets == null || settings.presets.Length == 0)
			{
				settings.presets = CreateDefaultPresets();
			}

			if (settings.ruleDefaults == null)
			{
				settings.ruleDefaults = new PerfMeterRuleDefaultsJson();
			}

			settings.ruleDefaults.editorWarningCooldownSeconds = Mathf.Max(1f, settings.ruleDefaults.editorWarningCooldownSeconds);
			settings.ruleDefaults.structuredLogCooldownSeconds = Mathf.Max(0f, settings.ruleDefaults.structuredLogCooldownSeconds);
			settings.ruleDefaults.callbackCooldownSeconds = Mathf.Max(0f, settings.ruleDefaults.callbackCooldownSeconds);

			if (settings.session == null)
			{
				settings.session = new PerfMeterSessionSettingsJson();
			}

			settings.session.warmupFrames = Mathf.Max(0, settings.session.warmupFrames);
			settings.session.sampleIntervalSeconds = Mathf.Max(0.02f, settings.session.sampleIntervalSeconds);
			settings.session.maxSamples = Mathf.Clamp(settings.session.maxSamples, 1, 100000);

			if (settings.overdraw == null)
			{
				settings.overdraw = new PerfMeterOverdrawSettingsJson();
			}

			settings.overdraw.defaultFrameCount = Mathf.Clamp(settings.overdraw.defaultFrameCount, 1, settings.overdraw.maxFrameCount <= 0 ? 600 : settings.overdraw.maxFrameCount);
			settings.overdraw.maxFrameCount = Mathf.Clamp(settings.overdraw.maxFrameCount, settings.overdraw.defaultFrameCount, 600);
		}

		private static PerfMeterOverlayCorner ParseOverlayCorner(string value)
		{
			return TryParseOverlayCorner(value, out PerfMeterOverlayCorner corner) ? corner : PerfMeterOverlayCorner.TopRight;
		}

		private static PerfMeterOverlayMode ParseOverlayMode(string value)
		{
			return TryParseOverlayMode(value, out PerfMeterOverlayMode mode) ? mode : PerfMeterOverlayMode.Full;
		}

		private static bool TryParseOverlayCorner(string value, out PerfMeterOverlayCorner corner)
		{
			return Enum.TryParse(value, true, out corner) && Enum.IsDefined(typeof(PerfMeterOverlayCorner), corner);
		}

		private static bool TryParseOverlayMode(string value, out PerfMeterOverlayMode mode)
		{
			return Enum.TryParse(value, true, out mode) && Enum.IsDefined(typeof(PerfMeterOverlayMode), mode);
		}

		private static PerfMeterTargetFps ParseTargetFps(int value)
		{
			switch (value)
			{
				case 15:
					return PerfMeterTargetFps.Fps15;
				case 30:
					return PerfMeterTargetFps.Fps30;
				case 90:
					return PerfMeterTargetFps.Fps90;
				case 120:
					return PerfMeterTargetFps.Fps120;
				case 144:
					return PerfMeterTargetFps.Fps144;
				case 240:
					return PerfMeterTargetFps.Fps240;
				default:
					return PerfMeterTargetFps.Fps60;
			}
		}

		private static string CombineWarnings(string first, string second)
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
	}

	internal static class PerfMeterSettingsBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AutoStartFromSettings()
		{
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.LoadFromResources();
			if (settings.LoadState == PerfMeterSettingsLoadState.Loaded && settings.Enabled && settings.AutoStart)
			{
				PerfMeterSettingsStore.ApplySnapshotToRuntime(settings);
			}
		}
	}
}
