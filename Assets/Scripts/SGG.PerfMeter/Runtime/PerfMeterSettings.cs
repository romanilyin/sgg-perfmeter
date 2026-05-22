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
			PerfMeterCollectionMode collectionMode,
			bool overlayVisible,
			PerfMeterOverlayCorner overlayCorner,
			PerfMeterOverlayMode overlayMode,
			PerfMeterTargetFps targetFps,
			string activePreset,
			PerfMeterOverlayModule overlayModules,
			int sessionWarmupFrames,
			float sessionWarmupSeconds,
			float sessionSampleIntervalSeconds,
			int sessionMaxSamples,
			bool sessionResetOnSceneLoad,
			int sessionSceneLoadIgnoreFrames,
			float sessionSceneLoadIgnoreSeconds,
			float editorWarningCooldownSeconds,
			float structuredLogCooldownSeconds,
			float callbackCooldownSeconds,
			PerfMeterSettingsLoadState loadState,
			string warning,
			float overlayScale = 1f,
			float overlayOpacity = 0.84f,
			float overlayFontSize = 12f,
			float overlayRefreshIntervalSeconds = 0.25f,
			int overlayGraphHistoryLength = 120,
			int overdrawDefaultFrameCount = 60,
			int overdrawMaxFrameCount = 600,
			double alertOverdrawRatioThreshold = 3d,
			int alertTimingConsecutiveFrames = 5,
			int alertFpsConsecutiveFrames = 60,
			int alertGpuTimingUnavailableConsecutiveFrames = 120,
			int alertOverdrawConsecutiveFrames = 3,
			PerfMeterOverlayTheme overlayTheme = PerfMeterOverlayTheme.ClassicDark,
			PerfMeterOverlayLayout overlayLayout = PerfMeterOverlayLayout.Classic,
			PerfMeterOverlayFontFamily overlayFontFamily = PerfMeterOverlayFontFamily.Manrope)
		{
			Enabled = enabled;
			AutoStart = autoStart;
			CollectionMode = NormalizeCollectionMode(collectionMode);
			OverlayVisible = overlayVisible;
			OverlayCorner = overlayCorner;
			OverlayMode = overlayMode;
			OverlayTheme = PerfMeterSettingsStore.NormalizeOverlayTheme(overlayTheme);
			OverlayLayout = PerfMeterSettingsStore.NormalizeOverlayLayout(overlayLayout);
			OverlayFontFamily = PerfMeterSettingsStore.NormalizeOverlayFontFamily(overlayFontFamily);
			TargetFps = targetFps;
			ActivePreset = string.IsNullOrEmpty(activePreset) ? PerfMeterSettingsStore.DefaultPresetId : activePreset;
			OverlayModules = overlayModules == PerfMeterOverlayModule.None ? PerfMeterSettingsStore.GetPresetModules(PerfMeterSettingsStore.ParseOverlayPreset(ActivePreset)) : overlayModules;
			SessionWarmupFrames = Mathf.Max(0, sessionWarmupFrames);
			SessionWarmupSeconds = Mathf.Max(0f, sessionWarmupSeconds);
			SessionSampleIntervalSeconds = sessionSampleIntervalSeconds > 0f ? sessionSampleIntervalSeconds : PerfMeterSessionOptions.DefaultSampleIntervalSeconds;
			SessionMaxSamples = Mathf.Max(1, sessionMaxSamples);
			SessionResetOnSceneLoad = sessionResetOnSceneLoad;
			SessionSceneLoadIgnoreFrames = Mathf.Max(0, sessionSceneLoadIgnoreFrames);
			SessionSceneLoadIgnoreSeconds = Mathf.Max(0f, sessionSceneLoadIgnoreSeconds);
			EditorWarningCooldownSeconds = Mathf.Max(1f, editorWarningCooldownSeconds);
			StructuredLogCooldownSeconds = Mathf.Max(0f, structuredLogCooldownSeconds);
			CallbackCooldownSeconds = Mathf.Max(0f, callbackCooldownSeconds);
			OverlayScale = Mathf.Clamp(overlayScale, PerfMeterSettingsStore.MinOverlayScale, PerfMeterSettingsStore.MaxOverlayScale);
			OverlayOpacity = Mathf.Clamp(overlayOpacity, PerfMeterSettingsStore.MinOverlayOpacity, PerfMeterSettingsStore.MaxOverlayOpacity);
			OverlayFontSize = Mathf.Clamp(overlayFontSize, PerfMeterSettingsStore.MinOverlayFontSize, PerfMeterSettingsStore.MaxOverlayFontSize);
			OverlayRefreshIntervalSeconds = Mathf.Clamp(overlayRefreshIntervalSeconds, PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds, PerfMeterSettingsStore.MaxOverlayRefreshIntervalSeconds);
			OverlayGraphHistoryLength = Mathf.Clamp(overlayGraphHistoryLength, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength);
			OverdrawMaxFrameCount = Mathf.Clamp(overdrawMaxFrameCount, 1, PerfMeterSettingsStore.MaxOverdrawFrameCountLimit);
			OverdrawDefaultFrameCount = Mathf.Clamp(overdrawDefaultFrameCount, 1, OverdrawMaxFrameCount);
			AlertOverdrawRatioThreshold = Math.Max(0.1d, alertOverdrawRatioThreshold);
			AlertTimingConsecutiveFrames = Mathf.Max(1, alertTimingConsecutiveFrames);
			AlertFpsConsecutiveFrames = Mathf.Max(1, alertFpsConsecutiveFrames);
			AlertGpuTimingUnavailableConsecutiveFrames = Mathf.Max(1, alertGpuTimingUnavailableConsecutiveFrames);
			AlertOverdrawConsecutiveFrames = Mathf.Max(1, alertOverdrawConsecutiveFrames);
			LoadState = loadState;
			Warning = warning ?? string.Empty;
		}

		public bool Enabled { get; }
		public bool AutoStart { get; }
		public PerfMeterCollectionMode CollectionMode { get; }
		public bool OverlayVisible { get; }
		public PerfMeterOverlayCorner OverlayCorner { get; }
		public PerfMeterOverlayMode OverlayMode { get; }
		public PerfMeterOverlayTheme OverlayTheme { get; }
		public PerfMeterOverlayLayout OverlayLayout { get; }
		public PerfMeterOverlayFontFamily OverlayFontFamily { get; }
		public PerfMeterTargetFps TargetFps { get; }
		public string ActivePreset { get; }
		public PerfMeterOverlayModule OverlayModules { get; }
		public int SessionWarmupFrames { get; }
		public float SessionWarmupSeconds { get; }
		public float SessionSampleIntervalSeconds { get; }
		public int SessionMaxSamples { get; }
		public bool SessionResetOnSceneLoad { get; }
		public int SessionSceneLoadIgnoreFrames { get; }
		public float SessionSceneLoadIgnoreSeconds { get; }
		public float EditorWarningCooldownSeconds { get; }
		public float StructuredLogCooldownSeconds { get; }
		public float CallbackCooldownSeconds { get; }
		public float OverlayScale { get; }
		public float OverlayOpacity { get; }
		public float OverlayFontSize { get; }
		public float OverlayRefreshIntervalSeconds { get; }
		public int OverlayGraphHistoryLength { get; }
		public int OverdrawDefaultFrameCount { get; }
		public int OverdrawMaxFrameCount { get; }
		public double AlertOverdrawRatioThreshold { get; }
		public int AlertTimingConsecutiveFrames { get; }
		public int AlertFpsConsecutiveFrames { get; }
		public int AlertGpuTimingUnavailableConsecutiveFrames { get; }
		public int AlertOverdrawConsecutiveFrames { get; }
		public PerfMeterSettingsLoadState LoadState { get; }
		public string Warning { get; }

		private static PerfMeterCollectionMode NormalizeCollectionMode(PerfMeterCollectionMode mode)
		{
			switch (mode)
			{
				case PerfMeterCollectionMode.Stopped:
				case PerfMeterCollectionMode.Background:
				case PerfMeterCollectionMode.Overlay:
				case PerfMeterCollectionMode.OverdrawDiagnostic:
					return mode;
				default:
					return PerfMeterCollectionMode.Overlay;
			}
		}
	}

	[Serializable]
	internal sealed class PerfMeterSettingsJson
	{
		public int schemaVersion = PerfMeterSettingsStore.CurrentSchemaVersion;
		public bool enabled = true;
		public bool autoStart = true;
		public string collectionMode = nameof(PerfMeterCollectionMode.Overlay);
		public bool overlayVisible = true;
		public string overlayCorner = nameof(PerfMeterOverlayCorner.TopRight);
		public string overlayMode = nameof(PerfMeterOverlayMode.Full);
		public int targetFps = (int)PerfMeterTargetFps.Fps60;
		public string activePreset = PerfMeterSettingsStore.DefaultPresetId;
		public PerfMeterPresetSettingsJson[] presets = Array.Empty<PerfMeterPresetSettingsJson>();
		public PerfMeterOverlaySettingsJson overlay = new PerfMeterOverlaySettingsJson();
		public PerfMeterRuleDefaultsJson ruleDefaults = new PerfMeterRuleDefaultsJson();
		public PerfMeterSessionSettingsJson session = new PerfMeterSessionSettingsJson();
		public PerfMeterOverdrawSettingsJson overdraw = new PerfMeterOverdrawSettingsJson();
	}

	[Serializable]
	internal sealed class PerfMeterOverlaySettingsJson
	{
		public string theme = nameof(PerfMeterOverlayTheme.ClassicDark);
		public string layout = nameof(PerfMeterOverlayLayout.Classic);
		public string fontFamily = nameof(PerfMeterOverlayFontFamily.Manrope);
		public float scale = 1f;
		public float opacity = 0.84f;
		public float fontSize = 12f;
		public float refreshIntervalSeconds = 0.25f;
		public int graphHistoryLength = 120;
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
		public double overdrawRatioThreshold = 3d;
		public int timingConsecutiveFrames = 5;
		public int fpsConsecutiveFrames = 60;
		public int gpuTimingUnavailableConsecutiveFrames = 120;
		public int overdrawConsecutiveFrames = 3;
	}

	[Serializable]
	internal sealed class PerfMeterSessionSettingsJson
	{
		public int warmupFrames = 0;
		public float warmupSeconds = 0f;
		public float sampleIntervalSeconds = 0.25f;
		public int maxSamples = 4096;
		public bool resetOnSceneLoad = false;
		public int sceneLoadIgnoreFrames = 0;
		public float sceneLoadIgnoreSeconds = 0f;
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
		internal const float MinOverlayScale = 0.5f;
		internal const float MaxOverlayScale = 2f;
		internal const float MinOverlayOpacity = 0.1f;
		internal const float MaxOverlayOpacity = 1f;
		internal const float MinOverlayFontSize = 9f;
		internal const float MaxOverlayFontSize = 24f;
		internal const float MinOverlayRefreshIntervalSeconds = 0.05f;
		internal const float MaxOverlayRefreshIntervalSeconds = 2f;
		internal const int MinOverlayGraphHistoryLength = 16;
		internal const int MaxOverlayGraphHistoryLength = 600;
		internal const int MaxOverdrawFrameCountLimit = 600;

		internal static PerfMeterSettingsSnapshot Defaults => ToSnapshot(CreateDefault(), PerfMeterSettingsLoadState.Missing, string.Empty);

		internal static PerfMeterSettingsJson CreateDefault()
		{
			return new PerfMeterSettingsJson
			{
				schemaVersion = CurrentSchemaVersion,
				enabled = true,
				autoStart = true,
				collectionMode = nameof(PerfMeterCollectionMode.Overlay),
				overlayVisible = true,
				overlayCorner = nameof(PerfMeterOverlayCorner.TopRight),
				overlayMode = nameof(PerfMeterOverlayMode.Full),
				targetFps = (int)PerfMeterTargetFps.Fps60,
				activePreset = DefaultPresetId,
				presets = CreateDefaultPresets(),
				overlay = new PerfMeterOverlaySettingsJson(),
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
			settings.collectionMode = snapshot.CollectionMode.ToString();
			settings.overlayVisible = snapshot.OverlayVisible;
			settings.overlayCorner = snapshot.OverlayCorner.ToString();
			settings.overlayMode = snapshot.OverlayMode.ToString();
			settings.targetFps = (int)snapshot.TargetFps;
			settings.activePreset = string.IsNullOrEmpty(snapshot.ActivePreset) ? DefaultPresetId : snapshot.ActivePreset;
			settings.overlay.theme = snapshot.OverlayTheme.ToString();
			settings.overlay.layout = snapshot.OverlayLayout.ToString();
			settings.overlay.fontFamily = snapshot.OverlayFontFamily.ToString();
			settings.overlay.scale = snapshot.OverlayScale;
			settings.overlay.opacity = snapshot.OverlayOpacity;
			settings.overlay.fontSize = snapshot.OverlayFontSize;
			settings.overlay.refreshIntervalSeconds = snapshot.OverlayRefreshIntervalSeconds;
			settings.overlay.graphHistoryLength = snapshot.OverlayGraphHistoryLength;
			settings.session.warmupFrames = snapshot.SessionWarmupFrames;
			settings.session.warmupSeconds = snapshot.SessionWarmupSeconds;
			settings.session.sampleIntervalSeconds = snapshot.SessionSampleIntervalSeconds;
			settings.session.maxSamples = snapshot.SessionMaxSamples;
			settings.session.resetOnSceneLoad = snapshot.SessionResetOnSceneLoad;
			settings.session.sceneLoadIgnoreFrames = snapshot.SessionSceneLoadIgnoreFrames;
			settings.session.sceneLoadIgnoreSeconds = snapshot.SessionSceneLoadIgnoreSeconds;
			settings.ruleDefaults.editorWarningCooldownSeconds = snapshot.EditorWarningCooldownSeconds;
			settings.ruleDefaults.structuredLogCooldownSeconds = snapshot.StructuredLogCooldownSeconds;
			settings.ruleDefaults.callbackCooldownSeconds = snapshot.CallbackCooldownSeconds;
			settings.ruleDefaults.overdrawRatioThreshold = snapshot.AlertOverdrawRatioThreshold;
			settings.ruleDefaults.timingConsecutiveFrames = snapshot.AlertTimingConsecutiveFrames;
			settings.ruleDefaults.fpsConsecutiveFrames = snapshot.AlertFpsConsecutiveFrames;
			settings.ruleDefaults.gpuTimingUnavailableConsecutiveFrames = snapshot.AlertGpuTimingUnavailableConsecutiveFrames;
			settings.ruleDefaults.overdrawConsecutiveFrames = snapshot.AlertOverdrawConsecutiveFrames;
			settings.overdraw.defaultFrameCount = snapshot.OverdrawDefaultFrameCount;
			settings.overdraw.maxFrameCount = snapshot.OverdrawMaxFrameCount;
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
			PerfMeterCollectionMode collectionMode = ParseCollectionMode(settings.collectionMode, settings.overlayVisible);
			bool overlayVisible = settings.overlayVisible;
			PerfMeterOverlayModule overlayModules = GetPresetModules(ParseOverlayPreset(settings.activePreset));
			PerfMeterOverlayTheme overlayTheme = settings.overlay != null ? ParseOverlayTheme(settings.overlay.theme) : PerfMeterOverlayTheme.ClassicDark;
			PerfMeterOverlayLayout overlayLayout = settings.overlay != null ? ParseOverlayLayout(settings.overlay.layout) : PerfMeterOverlayLayout.Classic;
			PerfMeterOverlayFontFamily overlayFontFamily = settings.overlay != null ? ParseOverlayFontFamily(settings.overlay.fontFamily) : PerfMeterOverlayFontFamily.Manrope;

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
				collectionMode,
				overlayVisible,
				ParseOverlayCorner(settings.overlayCorner),
				overlayMode,
				targetFps,
				settings.activePreset,
				overlayModules,
				settings.session != null ? settings.session.warmupFrames : 0,
				settings.session != null ? settings.session.warmupSeconds : PerfMeterSessionOptions.DefaultWarmupSeconds,
				settings.session != null ? settings.session.sampleIntervalSeconds : PerfMeterSessionOptions.DefaultSampleIntervalSeconds,
				settings.session != null ? settings.session.maxSamples : PerfMeterSessionOptions.DefaultMaxSamples,
				settings.session != null && settings.session.resetOnSceneLoad,
				settings.session != null ? settings.session.sceneLoadIgnoreFrames : PerfMeterSessionOptions.DefaultSceneLoadIgnoreFrames,
				settings.session != null ? settings.session.sceneLoadIgnoreSeconds : PerfMeterSessionOptions.DefaultSceneLoadIgnoreSeconds,
				settings.ruleDefaults != null ? settings.ruleDefaults.editorWarningCooldownSeconds : 8f,
				settings.ruleDefaults != null ? settings.ruleDefaults.structuredLogCooldownSeconds : 2f,
				settings.ruleDefaults != null ? settings.ruleDefaults.callbackCooldownSeconds : 0.5f,
				loadState,
				CombineWarnings(CombineWarnings(warning, normalizeWarning), moduleWarning),
				overlayScale: settings.overlay != null ? settings.overlay.scale : 1f,
				overlayOpacity: settings.overlay != null ? settings.overlay.opacity : 0.84f,
				overlayFontSize: settings.overlay != null ? settings.overlay.fontSize : 12f,
				overlayRefreshIntervalSeconds: settings.overlay != null ? settings.overlay.refreshIntervalSeconds : 0.25f,
				overlayGraphHistoryLength: settings.overlay != null ? settings.overlay.graphHistoryLength : 120,
				overdrawDefaultFrameCount: settings.overdraw != null ? settings.overdraw.defaultFrameCount : 60,
				overdrawMaxFrameCount: settings.overdraw != null ? settings.overdraw.maxFrameCount : 600,
				alertOverdrawRatioThreshold: settings.ruleDefaults != null ? settings.ruleDefaults.overdrawRatioThreshold : 3d,
				alertTimingConsecutiveFrames: settings.ruleDefaults != null ? settings.ruleDefaults.timingConsecutiveFrames : 5,
				alertFpsConsecutiveFrames: settings.ruleDefaults != null ? settings.ruleDefaults.fpsConsecutiveFrames : 60,
				alertGpuTimingUnavailableConsecutiveFrames: settings.ruleDefaults != null ? settings.ruleDefaults.gpuTimingUnavailableConsecutiveFrames : 120,
				alertOverdrawConsecutiveFrames: settings.ruleDefaults != null ? settings.ruleDefaults.overdrawConsecutiveFrames : 3,
				overlayTheme: overlayTheme,
				overlayLayout: overlayLayout,
				overlayFontFamily: overlayFontFamily);
		}

		internal static void ApplySnapshotToRuntime(PerfMeterSettingsSnapshot settings)
		{
			if (settings.CollectionMode == PerfMeterCollectionMode.Stopped)
			{
				PerformanceMeter.Stop();
				return;
			}

			PerformanceMeter.EnsureRunning();
			PerformanceMeter.ApplyOverlayTuning(settings);
			PerformanceMeter.SetOverlayTheme(settings.OverlayTheme);
			PerformanceMeter.SetOverlayLayout(settings.OverlayLayout);
			PerformanceMeter.SetOverlayFontFamily(settings.OverlayFontFamily);
			PerformanceMeter.SetOverlayPreset(ParseOverlayPreset(settings.ActivePreset));
			PerformanceMeter.SetOverlayModules(settings.OverlayModules);
			PerformanceMeter.SetTargetFps(settings.TargetFps);
			PerformanceMeter.SetOverlayCorner(settings.OverlayCorner);
			PerformanceMeter.SetOverlayMode(settings.OverlayMode);
			PerformanceMeter.SetCollectionMode(settings.CollectionMode);
			if (settings.CollectionMode == PerfMeterCollectionMode.Overlay)
			{
				PerformanceMeter.SetOverlayVisible(settings.OverlayVisible);
			}
		}

		internal static PerfMeterOverlayPreset ParseOverlayPreset(string value)
		{
			return Enum.TryParse(value, true, out PerfMeterOverlayPreset preset) && Enum.IsDefined(typeof(PerfMeterOverlayPreset), preset)
				? preset
				: PerfMeterOverlayPreset.FullDiagnostics;
		}

		internal static PerfMeterOverlayTheme NormalizeOverlayTheme(PerfMeterOverlayTheme theme)
		{
			switch (theme)
			{
				case PerfMeterOverlayTheme.ClassicDark:
				case PerfMeterOverlayTheme.Glass:
				case PerfMeterOverlayTheme.Cyber:
				case PerfMeterOverlayTheme.HighContrast:
					return theme;
				default:
					return PerfMeterOverlayTheme.ClassicDark;
			}
		}

		internal static PerfMeterOverlayLayout NormalizeOverlayLayout(PerfMeterOverlayLayout layout)
		{
			switch (layout)
			{
				case PerfMeterOverlayLayout.Classic:
				case PerfMeterOverlayLayout.CompactCards:
				case PerfMeterOverlayLayout.DiagnosticsWide:
				case PerfMeterOverlayLayout.OverdrawFocus:
					return layout;
				default:
					return PerfMeterOverlayLayout.Classic;
			}
		}

		internal static PerfMeterOverlayFontFamily NormalizeOverlayFontFamily(PerfMeterOverlayFontFamily fontFamily)
		{
			switch (fontFamily)
			{
				case PerfMeterOverlayFontFamily.Manrope:
				case PerfMeterOverlayFontFamily.JetBrainsMono:
				case PerfMeterOverlayFontFamily.LegacyRuntime:
					return fontFamily;
				default:
					return PerfMeterOverlayFontFamily.Manrope;
			}
		}

		internal static PerfMeterOverlayTheme ParseOverlayTheme(string value)
		{
			return TryParseOverlayTheme(value, out PerfMeterOverlayTheme theme) ? theme : PerfMeterOverlayTheme.ClassicDark;
		}

		internal static PerfMeterOverlayLayout ParseOverlayLayout(string value)
		{
			return TryParseOverlayLayout(value, out PerfMeterOverlayLayout layout) ? layout : PerfMeterOverlayLayout.Classic;
		}

		internal static PerfMeterOverlayFontFamily ParseOverlayFontFamily(string value)
		{
			return TryParseOverlayFontFamily(value, out PerfMeterOverlayFontFamily fontFamily) ? fontFamily : PerfMeterOverlayFontFamily.Manrope;
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
					return PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Rendering | PerfMeterOverlayModule.SrpBatcher | PerfMeterOverlayModule.Brg | PerfMeterOverlayModule.Uploads | PerfMeterOverlayModule.Memory | PerfMeterOverlayModule.Overdraw | PerfMeterOverlayModule.Heatmap | PerfMeterOverlayModule.Warnings | PerfMeterOverlayModule.CustomMetrics;
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
				PerfMeterOverlayModule.Warnings,
				PerfMeterOverlayModule.CustomMetrics
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

			if (string.IsNullOrEmpty(settings.collectionMode) || !TryParseCollectionMode(settings.collectionMode, out PerfMeterCollectionMode collectionMode))
			{
				settings.collectionMode = settings.overlayVisible ? nameof(PerfMeterCollectionMode.Overlay) : nameof(PerfMeterCollectionMode.Background);
				warning = CombineWarnings(warning, "Invalid collectionMode; overlayVisible is used as fallback.");
			}
			else
			{
				settings.collectionMode = NormalizeCollectionMode(collectionMode).ToString();
			}

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
			settings.ruleDefaults.overdrawRatioThreshold = Math.Max(0.1d, settings.ruleDefaults.overdrawRatioThreshold);
			settings.ruleDefaults.timingConsecutiveFrames = Mathf.Max(1, settings.ruleDefaults.timingConsecutiveFrames);
			settings.ruleDefaults.fpsConsecutiveFrames = Mathf.Max(1, settings.ruleDefaults.fpsConsecutiveFrames);
			settings.ruleDefaults.gpuTimingUnavailableConsecutiveFrames = Mathf.Max(1, settings.ruleDefaults.gpuTimingUnavailableConsecutiveFrames);
			settings.ruleDefaults.overdrawConsecutiveFrames = Mathf.Max(1, settings.ruleDefaults.overdrawConsecutiveFrames);

			if (settings.overlay == null)
			{
				settings.overlay = new PerfMeterOverlaySettingsJson();
			}

			settings.overlay.scale = Mathf.Clamp(settings.overlay.scale, MinOverlayScale, MaxOverlayScale);
			if (string.IsNullOrEmpty(settings.overlay.theme))
			{
				settings.overlay.theme = nameof(PerfMeterOverlayTheme.ClassicDark);
			}
			else if (!TryParseOverlayTheme(settings.overlay.theme, out PerfMeterOverlayTheme theme))
			{
				settings.overlay.theme = nameof(PerfMeterOverlayTheme.ClassicDark);
				warning = CombineWarnings(warning, "Invalid overlay theme; ClassicDark is used.");
			}
			else
			{
				settings.overlay.theme = theme.ToString();
			}

			if (string.IsNullOrEmpty(settings.overlay.layout))
			{
				settings.overlay.layout = nameof(PerfMeterOverlayLayout.Classic);
			}
			else if (!TryParseOverlayLayout(settings.overlay.layout, out PerfMeterOverlayLayout layout))
			{
				settings.overlay.layout = nameof(PerfMeterOverlayLayout.Classic);
				warning = CombineWarnings(warning, "Invalid overlay layout; Classic is used.");
			}
			else
			{
				settings.overlay.layout = layout.ToString();
			}

			if (string.IsNullOrEmpty(settings.overlay.fontFamily))
			{
				settings.overlay.fontFamily = nameof(PerfMeterOverlayFontFamily.Manrope);
			}
			else if (!TryParseOverlayFontFamily(settings.overlay.fontFamily, out PerfMeterOverlayFontFamily fontFamily))
			{
				settings.overlay.fontFamily = nameof(PerfMeterOverlayFontFamily.Manrope);
				warning = CombineWarnings(warning, "Invalid overlay fontFamily; Manrope is used.");
			}
			else
			{
				settings.overlay.fontFamily = fontFamily.ToString();
			}

			settings.overlay.opacity = Mathf.Clamp(settings.overlay.opacity, MinOverlayOpacity, MaxOverlayOpacity);
			settings.overlay.fontSize = Mathf.Clamp(settings.overlay.fontSize, MinOverlayFontSize, MaxOverlayFontSize);
			settings.overlay.refreshIntervalSeconds = Mathf.Clamp(settings.overlay.refreshIntervalSeconds, MinOverlayRefreshIntervalSeconds, MaxOverlayRefreshIntervalSeconds);
			settings.overlay.graphHistoryLength = Mathf.Clamp(settings.overlay.graphHistoryLength, MinOverlayGraphHistoryLength, MaxOverlayGraphHistoryLength);

			if (settings.session == null)
			{
				settings.session = new PerfMeterSessionSettingsJson();
			}

			settings.session.warmupFrames = Mathf.Max(0, settings.session.warmupFrames);
			settings.session.warmupSeconds = Mathf.Max(0f, settings.session.warmupSeconds);
			settings.session.sampleIntervalSeconds = Mathf.Max(0.02f, settings.session.sampleIntervalSeconds);
			settings.session.maxSamples = Mathf.Clamp(settings.session.maxSamples, 1, 100000);
			settings.session.sceneLoadIgnoreFrames = Mathf.Max(0, settings.session.sceneLoadIgnoreFrames);
			settings.session.sceneLoadIgnoreSeconds = Mathf.Max(0f, settings.session.sceneLoadIgnoreSeconds);

			if (settings.overdraw == null)
			{
				settings.overdraw = new PerfMeterOverdrawSettingsJson();
			}

			settings.overdraw.maxFrameCount = Mathf.Clamp(settings.overdraw.maxFrameCount, 1, MaxOverdrawFrameCountLimit);
			settings.overdraw.defaultFrameCount = Mathf.Clamp(settings.overdraw.defaultFrameCount, 1, settings.overdraw.maxFrameCount);
		}

		private static PerfMeterOverlayCorner ParseOverlayCorner(string value)
		{
			return TryParseOverlayCorner(value, out PerfMeterOverlayCorner corner) ? corner : PerfMeterOverlayCorner.TopRight;
		}

		private static PerfMeterCollectionMode ParseCollectionMode(string value, bool overlayVisibleFallback)
		{
			return TryParseCollectionMode(value, out PerfMeterCollectionMode mode) ? NormalizeCollectionMode(mode) : (overlayVisibleFallback ? PerfMeterCollectionMode.Overlay : PerfMeterCollectionMode.Background);
		}

		private static PerfMeterCollectionMode NormalizeCollectionMode(PerfMeterCollectionMode mode)
		{
			switch (mode)
			{
				case PerfMeterCollectionMode.Stopped:
				case PerfMeterCollectionMode.Background:
				case PerfMeterCollectionMode.Overlay:
				case PerfMeterCollectionMode.OverdrawDiagnostic:
					return mode;
				default:
					return PerfMeterCollectionMode.Overlay;
			}
		}

		private static PerfMeterOverlayMode ParseOverlayMode(string value)
		{
			return TryParseOverlayMode(value, out PerfMeterOverlayMode mode) ? mode : PerfMeterOverlayMode.Full;
		}

		private static bool TryParseOverlayTheme(string value, out PerfMeterOverlayTheme theme)
		{
			return Enum.TryParse(value, true, out theme) && Enum.IsDefined(typeof(PerfMeterOverlayTheme), theme);
		}

		private static bool TryParseOverlayLayout(string value, out PerfMeterOverlayLayout layout)
		{
			return Enum.TryParse(value, true, out layout) && Enum.IsDefined(typeof(PerfMeterOverlayLayout), layout);
		}

		private static bool TryParseOverlayFontFamily(string value, out PerfMeterOverlayFontFamily fontFamily)
		{
			return Enum.TryParse(value, true, out fontFamily) && Enum.IsDefined(typeof(PerfMeterOverlayFontFamily), fontFamily);
		}

		private static bool TryParseOverlayCorner(string value, out PerfMeterOverlayCorner corner)
		{
			return Enum.TryParse(value, true, out corner) && Enum.IsDefined(typeof(PerfMeterOverlayCorner), corner);
		}

		private static bool TryParseOverlayMode(string value, out PerfMeterOverlayMode mode)
		{
			return Enum.TryParse(value, true, out mode) && Enum.IsDefined(typeof(PerfMeterOverlayMode), mode);
		}

		private static bool TryParseCollectionMode(string value, out PerfMeterCollectionMode mode)
		{
			return Enum.TryParse(value, true, out mode) && Enum.IsDefined(typeof(PerfMeterCollectionMode), mode);
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
