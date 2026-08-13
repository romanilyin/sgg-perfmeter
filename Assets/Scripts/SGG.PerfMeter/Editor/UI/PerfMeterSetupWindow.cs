using System;
using System.Collections.Generic;
using System.IO;
using SGG.PerfMeter.Editor;
using SGG.PerfMeter.Editor.Setup;
using SGG.PerfMeter.Editor.UI.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimePerformanceMeter = SGG.PerfMeter.PerformanceMeter;

namespace SGG.PerfMeter.Editor.UI
{
	public sealed class PerfMeterSetupWindow : EditorWindow
	{
		private readonly List<Button> _runtimeButtons = new List<Button>();
		private readonly List<RuntimeButtonBinding> _runtimeButtonBindings = new List<RuntimeButtonBinding>();
		private readonly List<Button> _settingsButtons = new List<Button>();
		private readonly List<SettingsButtonBinding> _settingsButtonBindings = new List<SettingsButtonBinding>();
		private readonly List<OverlayPresetWidgetBinding> _presetWidgetBindings = new List<OverlayPresetWidgetBinding>();
		private readonly HashSet<string> _selectedRendererPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly List<string> _overlayPresetChoiceLabels = new List<string>();
		private List<PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset> _overlayPresetAssets = new List<PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset>();
		private VisualElement _ftuePanel;
		private VisualElement _setupPanel;
		private VisualElement _presetsPanel;
		private VisualElement _runtimePanel;
		private VisualElement _debugPanel;
		private ToolbarToggle _ftueTab;
		private ToolbarToggle _setupTab;
		private ToolbarToggle _presetsTab;
		private ToolbarToggle _runtimeTab;
		private ToolbarToggle _debugTab;
		private string _currentTab = "FTUE";
		private PerfMeterFtuePage _ftuePage;
		private Label _checklistFrameTiming;
		private Label _checklistPackagePath;
		private Label _checklistRendererFeature;
		private Label _checklistSettingsJson;
		private Label _checklistOverlayPresets;

		private Label _projectStatus;
		private Label _frameTimingStats;
		private Label _packagePath;
		private VisualElement _projectIndicator;

		private Label _rendererStatus;
		private VisualElement _rendererList;
		private VisualElement _rendererIndicator;
		private Button _installSelectedRendererButton;
		private Button _installAllMissingRendererButton;
		private Button _selectMissingRendererButton;

		private TextField _initCode;
		private Label _settingsStatus;
		private Label _settingsSchemaVersion;
		private Label _settingsPath;
		private Label _settingsResourcesPath;
		private Toggle _settingsEnabled;
		private Toggle _settingsAutoStart;
		private Toggle _settingsCollectMetrics;
		private Toggle _settingsOverlayVisible;
		private Toggle _settingsOverdrawDiagnostics;
		private EnumField _settingsTargetFps;
		private EnumField _settingsOverlayCorner;
		private EnumField _settingsOverlayTheme;
		private EnumField _settingsOverlayLayout;
		private EnumField _settingsOverlayFontFamily;
		private Label _settingsCollectionModeStatus;
		private Label _settingsLegacyActivePreset;
		private Label _settingsLegacyOverlayModules;
		private FloatField _settingsOverlayScale;
		private FloatField _settingsOverlayOpacity;
		private FloatField _settingsOverlayFontSize;
		private FloatField _settingsOverlayRefreshInterval;
		private IntegerField _settingsOverlayGraphHistory;
		private IntegerField _settingsPresetMaxWidth;
		private IntegerField _settingsPresetGap;
		private Toggle _settingsEditorWarningsEnabled;
		private Toggle _settingsStructuredLogsEnabled;
		private FloatField _settingsEditorWarningCooldown;
		private FloatField _settingsStructuredLogCooldown;
		private FloatField _settingsCallbackCooldown;
		private FloatField _settingsAlertOverdrawThreshold;
		private IntegerField _settingsAlertTimingFrames;
		private IntegerField _settingsAlertFpsFrames;
		private IntegerField _settingsAlertGpuTimingUnavailableFrames;
		private IntegerField _settingsAlertOverdrawFrames;
		private IntegerField _settingsSessionWarmupFrames;
		private FloatField _settingsSessionWarmupSeconds;
		private FloatField _settingsSessionSampleInterval;
		private IntegerField _settingsSessionMaxSamples;
		private Toggle _settingsSessionResetOnSceneLoad;
		private IntegerField _settingsSessionSceneLoadIgnoreFrames;
		private FloatField _settingsSessionSceneLoadIgnoreSeconds;
		private IntegerField _settingsOverdrawDefaultFrameCount;
		private IntegerField _settingsOverdrawMaxFrameCount;
		private PopupField<string> _settingsVisualPresetField;
		private Label _visualPresetDescription;
		private Label _visualPresetTags;
		private Label _visualPresetSchemaVersion;
		private Label _visualPresetReservedWidgetMetadata;
		private Label _visualPresetSource;
		private Label _visualPresetSummary;
		private Label _visualPresetStatus;
		private Button _visualPresetSaveButton;
		private Button _visualPresetReloadButton;
		private Button _visualPresetRevealButton;
		private VisualElement _settingsWidgetCompositionRows;
		private PerfMeterOverlayPresetJson _editingOverlayPreset;
		private string _editingOverlayPresetPath = string.Empty;
		private bool _editingOverlayPresetModified;
		private bool _suppressOverlayPresetCallbacks;
		private Label _lastActionLabel;
		private Label _runtimePlayModeInfo;
		private Label _runtimeStatus;
		private Label _runtimeCollectionMode;
		private Label _runtimeOverlayVisible;
		private Label _runtimeOverlayPreset;
		private Label _runtimeOverlayModules;
		private Label _runtimeTargetFps;
		private Label _runtimeCurrentFps;
		private Label _runtimeCpuFrame;
		private Label _runtimeGpuFrame;
		private Label _runtimeStyleSummary;
		private Label _runtimeEditorWarnings;
		private Label _runtimeStructuredLogs;
		private Label _runtimeOverdraw;
		private Label _runtimeP3SessionState;
		private Label _runtimeP3SessionId;
		private Label _runtimeP3SessionSamples;
		private Label _runtimeP3MemoryCapabilities;
		private Label _runtimeP3MemoryStatus;
		private Label _runtimeP3GraphicsStateCapabilities;
		private Label _runtimeP3GraphicsStateStatus;
		private Label _runtimeP3GraphicsMarkers;
		private Label _runtimeP3RenderIntegration;
		private Label _runtimeP3Grd;
		private Label _runtimeP3Warnings;
		private PopupField<string> _runtimeVisualPresetField;
		private FloatField _runtimeUpdateInterval;
		private IntegerField _runtimeGraphHistory;
		private Label _debugWidgetSummary;
		private VisualElement _debugWidgetRows;
		private PopupField<string> _languageField;
		private Label _settingsLanguageCurrent;

		internal const string SettingsStatusElementName = "settings-status";
		internal const string SettingsSchemaVersionElementName = "settings-schema-version";
		internal const string SettingsJsonPathElementName = "settings-json-path";
		internal const string SettingsResourcesPathElementName = "settings-resources-path";
		internal const string SettingsPresetDescriptionElementName = "settings-preset-description";
		internal const string SettingsPresetTagsElementName = "settings-preset-tags";
		internal const string SettingsPresetSchemaVersionElementName = "settings-preset-schema-version";
		internal const string SettingsPresetReservedWidgetMetadataElementName = "settings-preset-reserved-widget-metadata";
		internal const string SettingsPresetMaxWidthElementName = "settings-preset-max-width";
		internal const string SettingsPresetGapElementName = "settings-preset-gap";
		internal const string SettingsWidgetCompositionElementName = "settings-widget-composition";
		internal const string SettingsEnabledElementName = "settings-enabled";
		internal const string SettingsAutoStartElementName = "settings-auto-start";
		internal const string SettingsCollectMetricsElementName = "settings-collect-metrics";
		internal const string SettingsCollectionModeElementName = "settings-collection-mode";
		internal const string SettingsOverlayVisibleElementName = "settings-overlay-visible";
		internal const string SettingsOverdrawDiagnosticsElementName = "settings-overdraw-diagnostics";
		internal const string SettingsTargetFpsElementName = "settings-target-fps";
		internal const string SettingsOverlayCornerElementName = "settings-overlay-corner";
		internal const string SettingsOverlayThemeElementName = "settings-overlay-theme";
		internal const string SettingsOverlayLayoutElementName = "settings-overlay-layout";
		internal const string SettingsOverlayFontFamilyElementName = "settings-overlay-font-family";
		internal const string SettingsOverlayScaleElementName = "settings-overlay-scale";
		internal const string SettingsOverlayOpacityElementName = "settings-overlay-opacity";
		internal const string SettingsOverlayFontSizeElementName = "settings-overlay-font-size";
		internal const string SettingsOverlayRefreshIntervalElementName = "settings-overlay-refresh-interval";
		internal const string SettingsOverlayGraphHistoryElementName = "settings-overlay-graph-history";
		internal const string SettingsEditorWarningsEnabledElementName = "settings-editor-warnings-enabled";
		internal const string SettingsStructuredLogsEnabledElementName = "settings-structured-logs-enabled";
		internal const string SettingsEditorWarningCooldownElementName = "settings-editor-warning-cooldown";
		internal const string SettingsStructuredLogCooldownElementName = "settings-structured-log-cooldown";
		internal const string SettingsCallbackCooldownElementName = "settings-callback-cooldown";
		internal const string SettingsAlertOverdrawThresholdElementName = "settings-alert-overdraw-threshold";
		internal const string SettingsAlertTimingFramesElementName = "settings-alert-timing-frames";
		internal const string SettingsAlertFpsFramesElementName = "settings-alert-fps-frames";
		internal const string SettingsAlertGpuTimingUnavailableFramesElementName = "settings-alert-gpu-timing-unavailable-frames";
		internal const string SettingsAlertOverdrawFramesElementName = "settings-alert-overdraw-frames";
		internal const string SettingsSessionWarmupFramesElementName = "settings-session-warmup-frames";
		internal const string SettingsSessionWarmupSecondsElementName = "settings-session-warmup-seconds";
		internal const string SettingsSessionSampleIntervalElementName = "settings-session-sample-interval";
		internal const string SettingsSessionMaxSamplesElementName = "settings-session-max-samples";
		internal const string SettingsSessionResetOnSceneLoadElementName = "settings-session-reset-on-scene-load";
		internal const string SettingsSessionSceneLoadIgnoreFramesElementName = "settings-session-scene-load-ignore-frames";
		internal const string SettingsSessionSceneLoadIgnoreSecondsElementName = "settings-session-scene-load-ignore-seconds";
		internal const string SettingsOverdrawDefaultFrameCountElementName = "settings-overdraw-default-frame-count";
		internal const string SettingsOverdrawMaxFrameCountElementName = "settings-overdraw-max-frame-count";
		internal const string SettingsActiveOverlayPresetElementName = "settings-active-overlay-preset";
		internal const string VisualPresetSaveButtonElementName = "settings-save-overlay-preset";
		internal const string SettingsLegacyActivePresetElementName = "settings-legacy-active-preset";
		internal const string SettingsLegacyOverlayModulesElementName = "settings-legacy-overlay-modules";
		internal const string RuntimeCollectionModeElementName = "runtime-collection-mode";
		internal const string RuntimeCurrentFpsElementName = "runtime-current-fps";
		internal const string RuntimeCpuFrameElementName = "runtime-cpu-frame";
		internal const string RuntimeGpuFrameElementName = "runtime-gpu-frame";
		internal const string RuntimeStyleSummaryElementName = "runtime-style-summary";
		internal const string RuntimeUpdateIntervalElementName = "runtime-update-interval";
		internal const string RuntimeGraphHistoryElementName = "runtime-graph-history";
		internal const string RuntimeP3SessionAnalysisButtonName = "runtime-p3-session-analysis";
		internal const string RuntimeP3ProfileAnalyzerButtonName = "runtime-p3-profile-analyzer";
		internal const string RuntimeP3RefreshButtonName = "runtime-p3-refresh";
		internal const string RuntimeP3StartSessionButtonName = "runtime-p3-start-session";
		internal const string RuntimeP3StopSessionButtonName = "runtime-p3-stop-session";
		internal const string FtueTabElementName = "ftue-tab";
		internal const string ReviewFtueButtonElementName = "review-ftue";
		internal const string RefreshInitializationCodeButtonElementName = "refresh-initialization-code";
		internal const string CopyInitializationCodeButtonElementName = "copy-initialization-code";
		internal const int ProjectDefaultOverdrawRequestFrameCount = 0;
		internal const float MinPersistedSessionSampleIntervalSeconds = 0.02f;
		internal const int MaxPersistedSessionSamples = 100000;

		[MenuItem("SGG/Perfmeter/Setup")]
		public static void Open()
		{
			PerfMeterSetupWindow window = GetWindow<PerfMeterSetupWindow>(BuildWindowTitle());
			window.minSize = new Vector2(480f, 360f);
			window.Show();
		}

		private void OnEnable()
		{
			UpdateWindowTitle();
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		public void CreateGUI()
		{
			UpdateWindowTitle();
			rootVisualElement.Clear();
			_runtimeButtons.Clear();
			_runtimeButtonBindings.Clear();
			_settingsButtons.Clear();
			_settingsButtonBindings.Clear();
			_presetWidgetBindings.Clear();
			rootVisualElement.AddToClassList("pm-window");

			string stylePath = PerfMeterSetupUtility.PackageAssetPath + "/Editor/UI/PerfMeterSetupWindow.uss";
			StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
			if (style != null)
			{
				rootVisualElement.styleSheets.Add(style);
			}

			string version = PerfMeterFtueState.PackageVersion;
			Label title = new Label(string.IsNullOrEmpty(version) ? "SGG PerfMeter Setup" : "SGG PerfMeter Setup " + version);
			title.AddToClassList("pm-title");
			rootVisualElement.Add(title);

			BuildTabs();

			ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.flexGrow = 1f;
			rootVisualElement.Add(scroll);

			_ftuePanel = new VisualElement();
			_setupPanel = new VisualElement();
			_presetsPanel = new VisualElement();
			_runtimePanel = new VisualElement();
			_debugPanel = new VisualElement();
			scroll.Add(_ftuePanel);
			scroll.Add(_setupPanel);
			scroll.Add(_presetsPanel);
			scroll.Add(_runtimePanel);
			scroll.Add(_debugPanel);

			_lastActionLabel = new Label();
			_lastActionLabel.AddToClassList("pm-log");

			BuildSetupPanel();
			BuildPresetsPanel();
			BuildRuntimePanel();
			BuildDebugPanel();
			_ftuePage = new PerfMeterFtuePage(
				_ftuePanel,
				SelectSetupTab,
				SelectPresetsTab,
				SelectRuntimeTab,
				message => _lastActionLabel.text = message,
				SetFtueVisibility);

			rootVisualElement.Add(_lastActionLabel);

			SelectCurrentTab();
			RefreshAll();
		}

		internal static string BuildWindowTitle()
		{
			string version = PerfMeterFtueState.PackageVersion;
			return string.IsNullOrEmpty(version) ? "SGG PerfMeter" : "SGG PerfMeter " + version;
		}

		private void UpdateWindowTitle()
		{
			titleContent = new GUIContent(BuildWindowTitle());
		}

		private void BuildTabs()
		{
			Toolbar toolbar = new Toolbar();
			toolbar.AddToClassList("pm-tabs");
			_ftueTab = new ToolbarToggle { text = "FTUE", name = FtueTabElementName };
			_setupTab = new ToolbarToggle { text = "Setup" };
			_presetsTab = new ToolbarToggle { text = "Presets" };
			_runtimeTab = new ToolbarToggle { text = "Runtime" };
			_debugTab = new ToolbarToggle { text = "Debug" };
			_ftueTab.AddToClassList("pm-tab");
			_setupTab.AddToClassList("pm-tab");
			_presetsTab.AddToClassList("pm-tab");
			_runtimeTab.AddToClassList("pm-tab");
			_debugTab.AddToClassList("pm-tab");
			_ftueTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectFtueTab();
				}
			});
			_setupTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectSetupTab();
				}
			});
			_presetsTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectPresetsTab();
				}
			});
			_runtimeTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectRuntimeTab();
				}
			});
			_debugTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectDebugTab();
				}
			});
			toolbar.Add(_ftueTab);
			toolbar.Add(_setupTab);
			toolbar.Add(_presetsTab);
			toolbar.Add(_runtimeTab);
			toolbar.Add(_debugTab);
			rootVisualElement.Add(toolbar);
		}

		private void BuildSetupPanel()
		{
			BuildLocalizationSection(_setupPanel);
			BuildSetupChecklistSection(_setupPanel);
			BuildProjectSection(_setupPanel);
			BuildRendererSection(_setupPanel);
			BuildInitializationSection(_setupPanel);
		}

		private void BuildSetupChecklistSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Setup Checklist");
			AddInfo(section, "Required checks connect PerfMeter to Unity/URP runtime data. Recommended and optional checks improve diagnostics but are not needed for every project.");
			_checklistFrameTiming = AddChecklistRow(section, "Frame Timing Stats", "required", "Enable Frame Timing before relying on GPU timing in builds.");
			_checklistPackagePath = AddChecklistRow(section, "Package Path", "required", "PerfMeter package folder must be discoverable by setup tooling.");
			_checklistRendererFeature = AddChecklistRow(section, "URP Renderer Feature", "recommended", "Install PerfMeter Render Graph feature in active URP renderers for markers, overdraw and heatmap.");
			_checklistSettingsJson = AddChecklistRow(section, "Settings JSON", "recommended", "Save project settings JSON for zero-code setup.");
			_checklistOverlayPresets = AddChecklistRow(section, "Visual Overlay Presets", "optional", "Create default overlay preset JSON files when visual preset authoring is needed.");
		}

		private void BuildProjectSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Project Settings");
			_projectIndicator = AddStatusLine(section, out _projectStatus);
			_frameTimingStats = AddRow(section, "Frame Timing Stats");
			_packagePath = AddRawCopyRow(section, "Package Path");

			VisualElement actions = AddActions(section);
			AddButton(actions, "Enable Frame Timing", () => RunAction("Enable Frame Timing", PerfMeterSetupActions.EnableFrameTimingStats));
			AddButton(actions, "Refresh", RefreshAll);
			Button reviewFtueButton = AddButton(actions, "Review FTUE", ShowFtueForReview);
			reviewFtueButton.name = ReviewFtueButtonElementName;
		}

		private void BuildRendererSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "URP Renderer Features");
			_rendererIndicator = AddStatusLine(section, out _rendererStatus);
			_rendererList = new VisualElement();
			_rendererList.AddToClassList("pm-renderer-list");
			AddControlRow(section, "Renderers", _rendererList);

			VisualElement actions = AddActions(section);
			_installSelectedRendererButton = AddButton(actions, "Install Selected", () => RunAction("Install Selected", () => PerfMeterSetupActions.InstallRendererFeatures(GetSelectedRendererPaths())));
			_installAllMissingRendererButton = AddButton(actions, "Install All Missing", () => RunAction("Install All Missing", PerfMeterSetupActions.InstallRendererFeatures));
			_selectMissingRendererButton = AddButton(actions, "Select Missing", SelectMissingRenderers);
			AddButton(actions, "Refresh", RefreshAll);
		}

		private void BuildInitializationSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Initialization Code");
			AddInfo(section, "This self-contained bootstrap embeds the complete normalized Project Settings snapshot, including overlay, logging, alert, session, and overdraw values. Edit settings on Presets, then refresh this code. Capture and profiling requests remain explicit runtime operations.");

			_initCode = new TextField
			{
				multiline = true,
				isReadOnly = true,
				value = BuildInitializationSnippetFromOptions()
			};
			_initCode.AddToClassList("pm-code");
			section.Add(_initCode);

			VisualElement actions = AddActions(section);
			Button refreshButton = AddButton(actions, "Refresh from Project Settings", RefreshInitializationCode);
			refreshButton.name = RefreshInitializationCodeButtonElementName;
			Button copyButton = AddButton(actions, "Copy Init Code", CopyInitializationCode);
			copyButton.name = CopyInitializationCodeButtonElementName;
		}

		private void BuildPresetsPanel()
		{
			PerfMeterOverlayPresetEditorUtility.EnsureDefaultOverlayPresets();

			VisualElement statusSection = AddSection(_presetsPanel, "Project settings JSON");
			AddInfo(statusSection, "Technical/project settings are stored in Resources JSON. Visual overlay presets are project JSON files under Assets/SGG PerfMeter/Presets/Overlay and are baked into Resources JSON for builds.");
			_settingsStatus = NameElement(AddRow(statusSection, "Status"), SettingsStatusElementName);
			_settingsSchemaVersion = NameElement(AddRawRow(statusSection, "Supported schema version"), SettingsSchemaVersionElementName);
			_settingsSchemaVersion.text = PerfMeterSettingsStore.CurrentSchemaVersion.ToString();
			_settingsPath = NameElement(AddRawCopyRow(statusSection, "JSON Path"), SettingsJsonPathElementName);
			_settingsResourcesPath = NameElement(AddRawCopyRow(statusSection, "Resources Path"), SettingsResourcesPathElementName);
			_settingsCollectionModeStatus = NameElement(AddRawRow(statusSection, "Saved collection mode (derived)"), SettingsCollectionModeElementName);
			AddInfo(statusSection, "Legacy enum values below are read-only compatibility data. Visual presets and widget composition are authoritative for the current overlay.");
			_settingsLegacyActivePreset = NameElement(AddRawRow(statusSection, "Legacy active preset (read-only compatibility)"), SettingsLegacyActivePresetElementName);
			_settingsLegacyOverlayModules = NameElement(AddRawRow(statusSection, "Legacy resolved modules (read-only compatibility)"), SettingsLegacyOverlayModulesElementName);

			VisualElement workModeSection = AddSection(_presetsPanel, "Work mode");
			AddInfo(workModeSection, "Work mode controls whether PerfMeter runs and what it collects. Visual presets below only change overlay appearance and composition.");
			VisualElement workModeActions = AddActions(workModeSection);
			_settingsEnabled = NameElement(AddWorkModeToggle(workModeActions, "Enabled"), SettingsEnabledElementName);
			_settingsAutoStart = NameElement(AddWorkModeToggle(workModeActions, "Auto Start"), SettingsAutoStartElementName);
			_settingsCollectMetrics = NameElement(AddWorkModeToggle(workModeActions, "Collect Metrics"), SettingsCollectMetricsElementName);
			_settingsOverlayVisible = NameElement(AddWorkModeToggle(workModeActions, "Show Overlay"), SettingsOverlayVisibleElementName);
			_settingsOverdrawDiagnostics = NameElement(AddWorkModeToggle(workModeActions, "Overdraw diagnostics / heatmap"), SettingsOverdrawDiagnosticsElementName);

			VisualElement technicalSection = AddSection(_presetsPanel, "Technical settings");
			_settingsTargetFps = NameElement(new EnumField(PerfMeterTargetFps.Fps60), SettingsTargetFpsElementName);
			VisualElement targetActions = NameElement(AddChoiceGroup(technicalSection, "Target FPS"), SettingsTargetFpsElementName);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps15);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps30);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps60);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps90);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps120);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps144);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps240);
			_settingsOverlayRefreshInterval = NameElement(new FloatField(), SettingsOverlayRefreshIntervalElementName);
			ConfigureFloatField(_settingsOverlayRefreshInterval, "Normalized on focus loss to the persisted overlay refresh interval range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds, PerfMeterSettingsStore.MaxOverlayRefreshIntervalSeconds));
			AddControlRow(technicalSection, "Update interval", _settingsOverlayRefreshInterval);
			_settingsOverlayGraphHistory = NameElement(new IntegerField(), SettingsOverlayGraphHistoryElementName);
			ConfigureIntegerField(_settingsOverlayGraphHistory, "Normalized on focus loss to the persisted graph history range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength));
			AddControlRow(technicalSection, "Graph history samples", _settingsOverlayGraphHistory);

			Foldout advanced = new Foldout { text = "Advanced technical settings", value = false };
			technicalSection.Add(advanced);
			_settingsEditorWarningsEnabled = NameElement(new Toggle(), SettingsEditorWarningsEnabledElementName);
			AddControlRow(advanced, "Warning logs in Editor", _settingsEditorWarningsEnabled);
			_settingsStructuredLogsEnabled = NameElement(new Toggle(), SettingsStructuredLogsEnabledElementName);
			AddControlRow(advanced, "Structured logs", _settingsStructuredLogsEnabled);
			_settingsEditorWarningCooldown = NameElement(new FloatField(), SettingsEditorWarningCooldownElementName);
			ConfigureFloatField(_settingsEditorWarningCooldown, "Must be at least 1 second; normalized on focus loss.", value => Mathf.Max(1f, value));
			AddControlRow(advanced, "Editor Warning Cooldown", _settingsEditorWarningCooldown);
			_settingsStructuredLogCooldown = NameElement(new FloatField(), SettingsStructuredLogCooldownElementName);
			ConfigureFloatField(_settingsStructuredLogCooldown, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0f, value));
			AddControlRow(advanced, "Structured Log Cooldown", _settingsStructuredLogCooldown);
			_settingsCallbackCooldown = NameElement(new FloatField(), SettingsCallbackCooldownElementName);
			ConfigureFloatField(_settingsCallbackCooldown, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0f, value));
			AddControlRow(advanced, "Callback Cooldown", _settingsCallbackCooldown);
			_settingsAlertOverdrawThreshold = NameElement(new FloatField(), SettingsAlertOverdrawThresholdElementName);
			ConfigureFloatField(_settingsAlertOverdrawThreshold, "Must be at least 0.1; normalized on focus loss.", value => Mathf.Max(0.1f, value));
			AddControlRow(advanced, "Overdraw Alert Ratio", _settingsAlertOverdrawThreshold);
			_settingsAlertTimingFrames = NameElement(new IntegerField(), SettingsAlertTimingFramesElementName);
			ConfigureIntegerField(_settingsAlertTimingFrames, "Must be at least 1 frame; normalized on focus loss.", value => Mathf.Max(1, value));
			AddControlRow(advanced, "Timing Alert Frames", _settingsAlertTimingFrames);
			_settingsAlertFpsFrames = NameElement(new IntegerField(), SettingsAlertFpsFramesElementName);
			ConfigureIntegerField(_settingsAlertFpsFrames, "Must be at least 1 frame; normalized on focus loss.", value => Mathf.Max(1, value));
			AddControlRow(advanced, "FPS Alert Frames", _settingsAlertFpsFrames);
			_settingsAlertGpuTimingUnavailableFrames = NameElement(new IntegerField(), SettingsAlertGpuTimingUnavailableFramesElementName);
			ConfigureIntegerField(_settingsAlertGpuTimingUnavailableFrames, "Must be at least 1 frame; normalized on focus loss.", value => Mathf.Max(1, value));
			AddControlRow(advanced, "GPU Timing Alert Frames", _settingsAlertGpuTimingUnavailableFrames);
			_settingsAlertOverdrawFrames = NameElement(new IntegerField(), SettingsAlertOverdrawFramesElementName);
			ConfigureIntegerField(_settingsAlertOverdrawFrames, "Must be at least 1 frame; normalized on focus loss.", value => Mathf.Max(1, value));
			AddControlRow(advanced, "Overdraw Alert Frames", _settingsAlertOverdrawFrames);
			_settingsSessionWarmupFrames = NameElement(new IntegerField(), SettingsSessionWarmupFramesElementName);
			ConfigureIntegerField(_settingsSessionWarmupFrames, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0, value));
			AddControlRow(advanced, "Session Warmup Frames", _settingsSessionWarmupFrames);
			_settingsSessionWarmupSeconds = NameElement(new FloatField(), SettingsSessionWarmupSecondsElementName);
			ConfigureFloatField(_settingsSessionWarmupSeconds, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0f, value));
			AddControlRow(advanced, "Session Warmup Seconds", _settingsSessionWarmupSeconds);
			_settingsSessionSampleInterval = NameElement(new FloatField(), SettingsSessionSampleIntervalElementName);
			ConfigureFloatField(_settingsSessionSampleInterval, "Normalized on focus loss to the persisted minimum sample interval.", NormalizeSessionSampleInterval);
			AddControlRow(advanced, "Session Sample Interval", _settingsSessionSampleInterval);
			_settingsSessionMaxSamples = NameElement(new IntegerField(), SettingsSessionMaxSamplesElementName);
			ConfigureIntegerField(_settingsSessionMaxSamples, "Normalized on focus loss to the persisted session sample range.", NormalizeSessionMaxSamples);
			AddControlRow(advanced, "Session Max Samples", _settingsSessionMaxSamples);
			_settingsSessionResetOnSceneLoad = NameElement(new Toggle(), SettingsSessionResetOnSceneLoadElementName);
			AddControlRow(advanced, "Reset On Scene Load", _settingsSessionResetOnSceneLoad);
			_settingsSessionSceneLoadIgnoreFrames = NameElement(new IntegerField(), SettingsSessionSceneLoadIgnoreFramesElementName);
			ConfigureIntegerField(_settingsSessionSceneLoadIgnoreFrames, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0, value));
			AddControlRow(advanced, "Scene Ignore Frames", _settingsSessionSceneLoadIgnoreFrames);
			_settingsSessionSceneLoadIgnoreSeconds = NameElement(new FloatField(), SettingsSessionSceneLoadIgnoreSecondsElementName);
			ConfigureFloatField(_settingsSessionSceneLoadIgnoreSeconds, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0f, value));
			AddControlRow(advanced, "Scene Ignore Seconds", _settingsSessionSceneLoadIgnoreSeconds);
			_settingsOverdrawDefaultFrameCount = NameElement(new IntegerField(), SettingsOverdrawDefaultFrameCountElementName);
			ConfigureIntegerField(_settingsOverdrawDefaultFrameCount, "Normalized to 1..the configured overdraw maximum on focus loss.", value => NormalizeOverdrawDefaultFrameCount(value, _settingsOverdrawMaxFrameCount));
			AddControlRow(advanced, "Overdraw Default Frames", _settingsOverdrawDefaultFrameCount);
			_settingsOverdrawMaxFrameCount = NameElement(new IntegerField(), SettingsOverdrawMaxFrameCountElementName);
			ConfigureIntegerField(_settingsOverdrawMaxFrameCount, "Normalized to 1..the persisted overdraw maximum limit on focus loss.", value => NormalizeOverdrawMaxFrameCount(value, _settingsOverdrawDefaultFrameCount));
			AddControlRow(advanced, "Overdraw Max Frames", _settingsOverdrawMaxFrameCount);
			_settingsOverlayFontSize = NameElement(new FloatField(), SettingsOverlayFontSizeElementName);
			ConfigureFloatField(_settingsOverlayFontSize, "Normalized on focus loss to the persisted overlay font size range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayFontSize, PerfMeterSettingsStore.MaxOverlayFontSize));
			AddControlRow(advanced, "Overlay Font Size", _settingsOverlayFontSize);

			VisualElement presetSection = AddSection(_presetsPanel, "Visual preset");
			_settingsVisualPresetField = NameElement(new PopupField<string>(string.Empty, new List<string> { "No presets" }, 0), SettingsActiveOverlayPresetElementName);
			_settingsVisualPresetField.RegisterValueChangedCallback(evt => SelectOverlayPresetByLabel(evt.newValue));
			AddControlRow(presetSection, "Preset", _settingsVisualPresetField);
			VisualElement presetActions = AddActions(presetSection);
			_visualPresetSaveButton = NameElement(AddButton(presetActions, "Save", SaveCurrentOverlayPreset), VisualPresetSaveButtonElementName);
			AddButton(presetActions, "Save as custom", SaveCurrentOverlayPresetAsCustom);
			AddButton(presetActions, "Duplicate", DuplicateCurrentOverlayPreset);
			_visualPresetReloadButton = AddButton(presetActions, "Reload", ReloadCurrentOverlayPreset);
			_visualPresetRevealButton = AddButton(presetActions, "Reveal JSON", RevealCurrentOverlayPreset);
			_visualPresetDescription = NameElement(AddRawRow(presetSection, "Description"), SettingsPresetDescriptionElementName);
			_visualPresetTags = NameElement(AddRawRow(presetSection, "Tags"), SettingsPresetTagsElementName);
			_visualPresetSchemaVersion = NameElement(AddRawRow(presetSection, "Preset schema / version"), SettingsPresetSchemaVersionElementName);
			_visualPresetReservedWidgetMetadata = NameElement(AddRawRow(presetSection, "Reserved widget variant / height (read-only)"), SettingsPresetReservedWidgetMetadataElementName);
			_visualPresetSource = AddRawCopyRow(presetSection, "Source");
			_visualPresetSummary = AddRawRow(presetSection, "Summary");
			_visualPresetStatus = AddRawRow(presetSection, "Status");

			VisualElement styleSection = AddSection(_presetsPanel, "Layout & style");
			_settingsOverlayCorner = NameElement(new EnumField(PerfMeterOverlayCorner.TopRight), SettingsOverlayCornerElementName);
			_settingsOverlayCorner.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Anchor", _settingsOverlayCorner);
			_settingsOverlayLayout = NameElement(new EnumField(PerfMeterOverlayLayout.CompactCards), SettingsOverlayLayoutElementName);
			_settingsOverlayLayout.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Layout", _settingsOverlayLayout);
			_settingsOverlayTheme = NameElement(new EnumField(PerfMeterOverlayTheme.ClassicDark), SettingsOverlayThemeElementName);
			_settingsOverlayTheme.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Theme", _settingsOverlayTheme);
			_settingsOverlayFontFamily = NameElement(new EnumField(PerfMeterOverlayFontFamily.Manrope), SettingsOverlayFontFamilyElementName);
			_settingsOverlayFontFamily.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Font", _settingsOverlayFontFamily);
			_settingsOverlayScale = NameElement(new FloatField(), SettingsOverlayScaleElementName);
			ConfigureFloatField(_settingsOverlayScale, "Normalized on focus loss to the persisted overlay scale range.", NormalizeOverlayScale);
			_settingsOverlayScale.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Scale", _settingsOverlayScale);
			_settingsOverlayOpacity = NameElement(new FloatField(), SettingsOverlayOpacityElementName);
			ConfigureFloatField(_settingsOverlayOpacity, "Normalized on focus loss to the persisted overlay opacity range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayOpacity, PerfMeterSettingsStore.MaxOverlayOpacity));
			_settingsOverlayOpacity.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Opacity", _settingsOverlayOpacity);
			_settingsPresetMaxWidth = NameElement(new IntegerField(), SettingsPresetMaxWidthElementName);
			ConfigureIntegerField(_settingsPresetMaxWidth, "Must be at least 1 pixel; normalized on focus loss.", value => Mathf.Max(1, value));
			_settingsPresetMaxWidth.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Max width", _settingsPresetMaxWidth);
			_settingsPresetGap = NameElement(new IntegerField(), SettingsPresetGapElementName);
			ConfigureIntegerField(_settingsPresetGap, "Must be zero or greater; normalized on focus loss.", value => Mathf.Max(0, value));
			_settingsPresetGap.RegisterValueChangedCallback(_ => OnOverlayPresetStyleChanged());
			AddControlRow(styleSection, "Gap", _settingsPresetGap);

			VisualElement widgetsSection = AddSection(_presetsPanel, "Widget composition");
			AddInfo(widgetsSection, "Only high-level preset blocks are shown here. Low-level rows and metric bars remain visible in Debug.");
			_settingsWidgetCompositionRows = NameElement(new VisualElement(), SettingsWidgetCompositionElementName);
			_settingsWidgetCompositionRows.AddToClassList("pm-debug-table");
			widgetsSection.Add(_settingsWidgetCompositionRows);

			VisualElement actions = AddActions(_presetsPanel);
			AddButton(actions, "Ensure Default Presets", () => RunAction("Ensure Default Presets", PerfMeterSetupActions.EnsureDefaultOverlayPresets));
			AddButton(actions, "Create Defaults", () => RunAction("Create Defaults", PerfMeterSetupActions.CreateDefaultSettings));
			AddButton(actions, "Load JSON", () =>
			{
				LoadSettingsIntoControls(PerfMeterSetupActions.LoadSettings());
				_lastActionLabel.text = PerfMeterWindowLocalization.Text("Load JSON: settings loaded from project JSON or defaults.");
				ApplyLocalization();
			});
			AddButton(actions, "Save JSON", () => RunAction("Save JSON", SaveSettingsFromControls));
			AddButton(actions, "Apply In Play Mode", () => RunAction("Apply Settings", PerfMeterSetupActions.ApplySettingsToRuntime));

			ReloadOverlayPresetList(string.Empty);
		}

		private void BuildRuntimePanel()
		{
			VisualElement statusSection = AddSection(_runtimePanel, "Runtime Status");
			_runtimePlayModeInfo = new Label();
			_runtimePlayModeInfo.AddToClassList("pm-info");
			statusSection.Add(_runtimePlayModeInfo);
			_runtimeStatus = AddRawRow(statusSection, "State");
			_runtimeCollectionMode = NameElement(AddRawRow(statusSection, "Collection"), RuntimeCollectionModeElementName);
			_runtimeOverlayVisible = AddRawRow(statusSection, "Overlay");
			_runtimeOverlayPreset = AddRawRow(statusSection, "Active visual preset");
			_runtimeOverlayModules = AddRawRow(statusSection, "Resolved modules");
			_runtimeTargetFps = AddRawRow(statusSection, "Target FPS");
			_runtimeCurrentFps = NameElement(AddRawRow(statusSection, "Current FPS"), RuntimeCurrentFpsElementName);
			_runtimeCpuFrame = NameElement(AddRawRow(statusSection, "CPU frame"), RuntimeCpuFrameElementName);
			_runtimeGpuFrame = NameElement(AddRawRow(statusSection, "GPU frame"), RuntimeGpuFrameElementName);
			_runtimeStyleSummary = NameElement(AddRawRow(statusSection, "Style summary"), RuntimeStyleSummaryElementName);
			_runtimeEditorWarnings = AddRawRow(statusSection, "Editor Warning Logs");
			_runtimeStructuredLogs = AddRawRow(statusSection, "Structured Logs");
			_runtimeOverdraw = AddRawRow(statusSection, "Overdraw");

			VisualElement controlSection = AddSection(_runtimePanel, "Runtime controls");
			VisualElement controlActions = AddActions(controlSection);
			AddRuntimeButton(controlActions, "Ensure Runtime", () => RunRuntimeAction("Ensure Runtime", RuntimePerformanceMeter.EnsureRunning), status => status.State == PerfMeterRuntimeState.Running);
			AddRuntimeButton(controlActions, "Collect Metrics", () => RunRuntimeAction("Collect Metrics", () => RuntimePerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background)), status => status.CollectionMode != PerfMeterCollectionMode.Stopped);
			AddRuntimeButton(controlActions, "Stop Collection", () => RunRuntimeAction("Stop Collection", () => RuntimePerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Stopped)), status => status.CollectionMode == PerfMeterCollectionMode.Stopped);
			AddRuntimeButton(controlActions, "Show Overlay", () => RunRuntimeAction("Show Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(true)), status => status.OverlayVisible);
			AddRuntimeButton(controlActions, "Hide Overlay", () => RunRuntimeAction("Hide Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(false)), status => !status.OverlayVisible);
			AddRuntimeButton(controlActions, "Apply project defaults", () => RunRuntimeAction("Apply project defaults", ApplyRuntimeProjectSettings));
			AddRuntimeButton(controlActions, "Reset runtime overrides", () => RunRuntimeAction("Reset runtime overrides", ApplyRuntimeProjectSettings));

			VisualElement technicalSection = AddSection(_runtimePanel, "Runtime technical overrides");
			VisualElement targetActions = AddChoiceGroup(technicalSection, "Target FPS");
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps15);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps30);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps60);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps90);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps120);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps144);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps240);
			_runtimeUpdateInterval = NameElement(new FloatField(), RuntimeUpdateIntervalElementName);
			ConfigureFloatField(_runtimeUpdateInterval, "Normalized on focus loss to the persisted overlay refresh interval range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds, PerfMeterSettingsStore.MaxOverlayRefreshIntervalSeconds));
			AddControlRow(technicalSection, "Update interval", _runtimeUpdateInterval);
			_runtimeGraphHistory = NameElement(new IntegerField(), RuntimeGraphHistoryElementName);
			ConfigureIntegerField(_runtimeGraphHistory, "Normalized on focus loss to the persisted graph history range.", value => Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength));
			AddControlRow(technicalSection, "Graph history samples", _runtimeGraphHistory);
			VisualElement technicalActions = AddActions(technicalSection);
			AddRuntimeButton(technicalActions, "Apply technical overrides", () => RunRuntimeAction("Apply technical overrides", ApplyRuntimeTechnicalOverrides));
			AddRuntimeButton(technicalActions, "Save technical settings to project", () => RunRuntimeAction("Save technical settings to project", SaveRuntimeTechnicalSettingsToProject));
			AddRuntimeButton(technicalActions, "Reset to project technical settings", () => RunRuntimeAction("Reset to project technical settings", ApplyProjectTechnicalSettingsToRuntime));

			VisualElement visualPresetSection = AddSection(_runtimePanel, "Runtime visual preset");
			_runtimeVisualPresetField = new PopupField<string>(string.Empty, new List<string> { "No presets" }, 0);
			AddControlRow(visualPresetSection, "Preset", _runtimeVisualPresetField);
			VisualElement visualPresetActions = AddActions(visualPresetSection);
			AddRuntimeButton(visualPresetActions, "Apply", () => RunRuntimeAction("Apply visual preset", ApplyRuntimeSelectedVisualPreset));
			AddRuntimeButton(visualPresetActions, "Reload presets", () => RunRuntimeAction("Reload presets", () => ReloadOverlayPresetList(RuntimePerformanceMeter.VisualOverlayPresetId)));
			AddButton(visualPresetActions, "Edit presets...", SelectPresetsTab);

			VisualElement diagnosticsSection = AddSection(_runtimePanel, "Diagnostics / actions");
			VisualElement diagnosticsActions = AddActions(diagnosticsSection);
			AddRuntimeButton(diagnosticsActions, "Clear graph history", () => RunRuntimeAction("Clear graph history", RuntimePerformanceMeter.ResetStats));
			AddRuntimeButton(diagnosticsActions, "Reset alert counters", () => RunRuntimeAction("Reset alert counters", RuntimePerformanceMeter.ClearAlerts));
			AddRuntimeButton(diagnosticsActions, "Dump runtime snapshot", () => RunRuntimeAction("Dump runtime snapshot", DumpRuntimeSnapshot));
			AddRuntimeButton(diagnosticsActions, "Copy runtime summary", () => RunRuntimeAction("Copy runtime summary", CopyRuntimeSummary));
			AddRuntimeButton(diagnosticsActions, "Measure Overdraw (project default)", () => RunRuntimeAction("Measure Overdraw", () => RuntimePerformanceMeter.RequestOverdrawMeasurement(ProjectDefaultOverdrawRequestFrameCount)), status => status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring);
			AddRuntimeButton(diagnosticsActions, "Cancel Overdraw", () => RunRuntimeAction("Cancel Overdraw", RuntimePerformanceMeter.CancelOverdrawMeasurement), status => status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring);
			AddRuntimeButton(diagnosticsActions, "Show Heatmap", () => RunRuntimeAction("Show Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(true)), status => status.OverdrawHeatmapVisible);
			AddRuntimeButton(diagnosticsActions, "Hide Heatmap", () => RunRuntimeAction("Hide Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(false)), status => !status.OverdrawHeatmapVisible);

			BuildP3IntegrationSection(_runtimePanel);

			ReloadOverlayPresetList(string.Empty);
		}

		private void BuildP3IntegrationSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "P3 integrations & analysis");
			AddInfo(section, "Read-only capability and status discovery for optional P3 integrations. Memory snapshot and graphics-state trace/prewarm request parameters remain runtime-only API/MCP inputs and are not project settings.");
			_runtimeP3SessionState = NameElement(AddRawRow(section, "Session state"), "runtime-p3-session-state");
			_runtimeP3SessionId = NameElement(AddRawCopyRow(section, "Session ID"), "runtime-p3-session-id");
			_runtimeP3SessionSamples = NameElement(AddRawRow(section, "Session samples"), "runtime-p3-session-samples");
			_runtimeP3MemoryCapabilities = NameElement(AddRawRow(section, "Memory snapshot capabilities"), "runtime-p3-memory-capabilities");
			_runtimeP3MemoryStatus = NameElement(AddRawRow(section, "Memory snapshot status"), "runtime-p3-memory-status");
			_runtimeP3GraphicsStateCapabilities = NameElement(AddRawRow(section, "Graphics-state capabilities"), "runtime-p3-graphics-state-capabilities");
			_runtimeP3GraphicsStateStatus = NameElement(AddRawRow(section, "Graphics-state status"), "runtime-p3-graphics-state-status");
			_runtimeP3GraphicsMarkers = NameElement(AddRawRow(section, "Graphics creation markers"), "runtime-p3-graphics-markers");
			_runtimeP3RenderIntegration = NameElement(AddRawRow(section, "Render integration"), "runtime-p3-render-integration");
			_runtimeP3Grd = NameElement(AddRawRow(section, "GRD / BRG telemetry"), "runtime-p3-grd");
			_runtimeP3Warnings = NameElement(AddRawRow(section, "Combined warnings"), "runtime-p3-warnings");

			VisualElement actions = AddActions(section);
			NameElement(AddRuntimeButton(actions, "Start Session", () => RunRuntimeAction("Start Session", RuntimePerformanceMeter.StartSession), status => status.IsSessionRecording), RuntimeP3StartSessionButtonName);
			NameElement(AddRuntimeButton(actions, "Stop Session", () => RunRuntimeAction("Stop Session", RuntimePerformanceMeter.StopSession), status => !status.IsSessionRecording), RuntimeP3StopSessionButtonName);
			NameElement(AddButton(actions, "Session Analysis", OpenSessionAnalysis), RuntimeP3SessionAnalysisButtonName);
			NameElement(AddButton(actions, "Profile Analyzer", OpenProfileAnalyzer), RuntimeP3ProfileAnalyzerButtonName);
			NameElement(AddButton(actions, "Refresh", RefreshRuntimePanelAndLocalization), RuntimeP3RefreshButtonName);
		}

		private void BuildDebugPanel()
		{
			VisualElement section = AddSection(_debugPanel, "Widget Debug");
			AddInfo(section, "Lists overlay widgets available from this package plus project custom metric providers. Source shows whether the widget is implemented inside this package or in the Unity project.");
			_debugWidgetSummary = AddRow(section, "Summary");

			_debugWidgetRows = new VisualElement();
			_debugWidgetRows.AddToClassList("pm-debug-table");
			section.Add(_debugWidgetRows);

			VisualElement actions = AddActions(section);
			AddButton(actions, "Refresh", () =>
			{
				RefreshDebugPanel();
				ApplyLocalization();
			});
		}

		private void RefreshDebugPanel()
		{
			if (_debugWidgetRows == null)
			{
				return;
			}

			List<DebugWidgetInfo> widgets = BuildDebugWidgetList();
			int packageCount = 0;
			int projectCount = 0;
			for (int i = 0; i < widgets.Count; i++)
			{
				if (string.Equals(widgets[i].Source, "Inside this package", StringComparison.Ordinal))
				{
					packageCount++;
				}
				else if (string.Equals(widgets[i].Source, "In project", StringComparison.Ordinal))
				{
					projectCount++;
				}
			}

			if (_debugWidgetSummary != null)
			{
				_debugWidgetSummary.text = widgets.Count + " widgets: " + packageCount + " inside this package, " + projectCount + " in project.";
			}

			_debugWidgetRows.Clear();
			_debugWidgetRows.Add(CreateDebugWidgetRow("Source", "Widget", "Widget type", "Module", "Details", true));
			for (int i = 0; i < widgets.Count; i++)
			{
				DebugWidgetInfo widget = widgets[i];
				_debugWidgetRows.Add(CreateDebugWidgetRow(widget.Source, widget.Name, widget.Type, widget.Module, widget.Details, false, widget.PreserveIdentity));
			}

			if (projectCount == 0)
			{
				_debugWidgetRows.Add(CreateDebugWidgetRow("In project", "No project custom metric providers discovered", "Custom metric provider", "CustomMetrics", "Implement IPerfMeterCustomMetricProvider and register it with PerformanceMeter.RegisterCustomMetricProvider().", false));
			}
		}

		private List<DebugWidgetInfo> BuildDebugWidgetList()
		{
			List<DebugWidgetInfo> widgets = new List<DebugWidgetInfo>();
			AddBuiltInDebugWidgets(widgets);
			AddProjectCustomMetricWidgets(widgets);
			return widgets;
		}

		private static void AddBuiltInDebugWidgets(List<DebugWidgetInfo> widgets)
		{
			PerfMeterWidgetDescriptor[] descriptors = PerfMeterWidgetRegistry.GetAllDescriptors();
			for (int i = 0; i < descriptors.Length; i++)
			{
				PerfMeterWidgetDescriptor descriptor = descriptors[i];
				string details = descriptor.Description + " Preset block: " + (descriptor.IsPresetBlock ? "yes" : "no") + ".";
				AddDebugWidget(widgets, "Inside this package", descriptor.DisplayName, descriptor.Kind, descriptor.Module, details);
			}
		}

		private static void AddProjectCustomMetricWidgets(List<DebugWidgetInfo> widgets)
		{
			List<Type> providerTypes = new List<Type>(TypeCache.GetTypesDerivedFrom<IPerfMeterCustomMetricProvider>());
			providerTypes.Sort((left, right) => string.Compare(left.FullName, right.FullName, StringComparison.OrdinalIgnoreCase));
			for (int i = 0; i < providerTypes.Count; i++)
			{
				Type type = providerTypes[i];
				if (type == null || type.IsAbstract || type.IsInterface)
				{
					continue;
				}

				string assetPath = GetScriptAssetPath(type);
				string source = IsInsideThisPackage(type, assetPath) ? "Inside this package" : "In project";
				string details = string.IsNullOrEmpty(assetPath) ? type.FullName : assetPath;
				AddDebugWidget(widgets, source, type.Name, "Custom metric provider", "CustomMetrics", details, true);
			}
		}

		private static void AddDebugWidget(List<DebugWidgetInfo> widgets, string source, string name, string type, string module, string details, bool preserveIdentity = false)
		{
			widgets.Add(new DebugWidgetInfo(source, name, type, module, details, preserveIdentity));
		}

		internal static VisualElement CreateDebugWidgetRow(string source, string name, string type, string module, string details, bool header, bool preserveIdentity = false)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-debug-row");
			if (header)
			{
				row.AddToClassList("pm-debug-row--header");
			}

			row.Add(CreateDebugCell(source, "pm-debug-cell--source"));
			row.Add(CreateDebugCell(name, "pm-debug-cell--name", preserveIdentity));
			row.Add(CreateDebugCell(type, "pm-debug-cell--type"));
			row.Add(CreateDebugCell(module, "pm-debug-cell--module"));
			row.Add(CreateDebugCell(details, "pm-debug-cell--details", preserveIdentity));
			return row;
		}

		private static Label CreateDebugCell(string text, string className, bool raw = false)
		{
			Label label = new Label(text);
			label.AddToClassList("pm-debug-cell");
			label.AddToClassList(className);
			if (raw)
			{
				label.AddToClassList("pm-no-localize");
			}

			return label;
		}

		private static string GetScriptAssetPath(Type type)
		{
			Type scriptType = type.IsNested && type.DeclaringType != null ? type.DeclaringType : type;
			string[] guids = AssetDatabase.FindAssets(scriptType.Name + " t:MonoScript");
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (script != null && script.GetClass() == scriptType)
				{
					return path;
				}
			}

			return string.Empty;
		}

		private static bool IsInsideThisPackage(Type type, string assetPath)
		{
			string packageAssetPath = PerfMeterSetupUtility.PackageAssetPath;
			string normalizedPath = string.IsNullOrEmpty(assetPath) ? string.Empty : assetPath.Replace('\\', '/');
			if (!string.IsNullOrEmpty(packageAssetPath) && normalizedPath.StartsWith(packageAssetPath, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (normalizedPath.StartsWith("Packages/com.sungeargames.perfmeter/", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			return type.Assembly == typeof(RuntimePerformanceMeter).Assembly;
		}

		private void BuildLocalizationSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Localization");
			AddInfo(section, "Language affects only this setup window. Runtime overlay text, generated C# snippets, JSON keys, paths, and logs stay unchanged.");

			List<string> languageNames = PerfMeterWindowLocalization.LanguageDisplayNames();
			int languageIndex = Math.Min(PerfMeterWindowLocalization.LanguageIndex(PerfMeterWindowLocalization.CurrentLanguage), languageNames.Count - 1);
			_languageField = new PopupField<string>(string.Empty, languageNames, Math.Max(0, languageIndex));
			_languageField.RegisterValueChangedCallback(evt => OnLanguageChanged(evt.newValue));
			AddControlRow(section, "Language", _languageField);
			_settingsLanguageCurrent = AddRow(section, "Current language", PerfMeterWindowLocalization.CurrentLanguageDisplayName());
			AddRow(section, "Scope", "Only SGG PerfMeter Setup window UI text; runtime output and generated snippets stay unchanged.");
		}

		private void OnLanguageChanged(string displayName)
		{
			string language = PerfMeterWindowLocalization.LanguageCodeForDisplayName(displayName);
			if (string.Equals(language, PerfMeterWindowLocalization.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			PerfMeterWindowLocalization.CurrentLanguage = language;
			CreateGUI();
			if (_lastActionLabel != null)
			{
				_lastActionLabel.text = PerfMeterWindowLocalization.Text("Language changed. PerfMeter Setup window UI text has been refreshed.");
			}
		}

		private void ApplyLocalization()
		{
			PerfMeterWindowLocalization.ApplyTo(rootVisualElement);
		}

		private VisualElement AddSection(VisualElement parent, string caption)
		{
			VisualElement section = new VisualElement();
			section.AddToClassList("pm-section");
			Label header = new Label(caption);
			header.AddToClassList("pm-section-caption");
			VisualElement content = new VisualElement();
			content.AddToClassList("pm-section-content");
			section.Add(header);
			section.Add(content);
			parent.Add(section);
			return content;
		}

		private VisualElement AddStatusLine(VisualElement parent, out Label label)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-status-line");
			Label indicator = new Label("●");
			indicator.AddToClassList("pm-indicator");
			label = new Label();
			label.AddToClassList("pm-status-text");
			row.Add(indicator);
			row.Add(label);
			parent.Add(row);
			return indicator;
		}

		private Label AddRow(VisualElement parent, string key, string value = "")
		{
			return AddInfoRow(parent, key, value, false);
		}

		private Label AddRawRow(VisualElement parent, string key, string value = "")
		{
			Label label = AddRow(parent, key, value);
			label.AddToClassList("pm-no-localize");
			return label;
		}

		private Label AddCopyRow(VisualElement parent, string key, string value = "")
		{
			return AddInfoRow(parent, key, value, true);
		}

		private Label AddRawCopyRow(VisualElement parent, string key, string value = "")
		{
			Label label = AddCopyRow(parent, key, value);
			label.AddToClassList("pm-no-localize");
			return label;
		}

		private Label AddInfoRow(VisualElement parent, string key, string value, bool copyable)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-row");
			Label keyLabel = new Label(key);
			keyLabel.AddToClassList("pm-key");
			Label valueLabel = new Label(value);
			valueLabel.AddToClassList("pm-value");
			row.Add(keyLabel);
			row.Add(valueLabel);
			if (copyable)
			{
				row.Add(CopyButton(() => CopyText(key, valueLabel.text), "Copy " + key));
			}

			parent.Add(row);
			return valueLabel;
		}

		private static T NameElement<T>(T element, string name) where T : VisualElement
		{
			element.name = name;
			return element;
		}

		private static void ConfigureFloatField(FloatField field, string tooltip, Func<float, float> normalize)
		{
			field.tooltip = tooltip;
			field.RegisterCallback<FocusOutEvent>(_ =>
			{
				float normalized = normalize(field.value);
				if (!Mathf.Approximately(field.value, normalized))
				{
					field.value = normalized;
				}
			});
		}

		private static void ConfigureIntegerField(IntegerField field, string tooltip, Func<int, int> normalize)
		{
			field.tooltip = tooltip;
			field.RegisterCallback<FocusOutEvent>(_ =>
			{
				int normalized = normalize(field.value);
				if (field.value != normalized)
				{
					field.value = normalized;
				}
			});
		}

		internal static int NormalizeOverdrawDefaultFrameCount(int value, IntegerField maxFrameCountField)
		{
			int maxFrameCount = maxFrameCountField == null
				? PerfMeterSettingsStore.MaxOverdrawFrameCountLimit
				: Mathf.Clamp(maxFrameCountField.value, 1, PerfMeterSettingsStore.MaxOverdrawFrameCountLimit);
			return Mathf.Clamp(value, 1, maxFrameCount);
		}

		internal static float NormalizeOverlayScale(float value)
		{
			return Mathf.Clamp(value, PerfMeterSettingsStore.MinOverlayScale, PerfMeterSettingsStore.MaxOverlayScale);
		}

		internal static float NormalizeSessionSampleInterval(float value)
		{
			return Mathf.Max(MinPersistedSessionSampleIntervalSeconds, value);
		}

		internal static int NormalizeSessionMaxSamples(int value)
		{
			return Mathf.Clamp(value, 1, MaxPersistedSessionSamples);
		}

		internal static int NormalizeOverdrawMaxFrameCount(int value, IntegerField defaultFrameCountField)
		{
			int normalized = Mathf.Clamp(value, 1, PerfMeterSettingsStore.MaxOverdrawFrameCountLimit);
			if (defaultFrameCountField != null && defaultFrameCountField.value > normalized)
			{
				defaultFrameCountField.SetValueWithoutNotify(normalized);
			}

			return normalized;
		}

		private void AddControlRow(VisualElement parent, string key, VisualElement control)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-row");
			Label keyLabel = new Label(key);
			keyLabel.AddToClassList("pm-key");
			control.AddToClassList("pm-control");
			row.Add(keyLabel);
			row.Add(control);
			parent.Add(row);
		}

		private void AddInfo(VisualElement parent, string text)
		{
			Label label = new Label(text);
			label.AddToClassList("pm-info");
			parent.Add(label);
		}

		private Label AddChecklistRow(VisualElement parent, string key, string importance, string nextAction)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-row");
			row.AddToClassList("pm-checklist-row");
			Label keyLabel = new Label(key + " (" + importance + ")");
			keyLabel.AddToClassList("pm-key");
			VisualElement field = new VisualElement();
			field.AddToClassList("pm-checklist-field");
			Label icon = new Label(ChecklistIcon("warn"));
			icon.AddToClassList("pm-checklist-icon");
			Label label = new Label(nextAction);
			label.AddToClassList("pm-checklist-value");
			field.Add(icon);
			field.Add(label);
			row.Add(keyLabel);
			row.Add(field);
			row.Add(CopyButton(() => CopyText(key, label.text), "Copy " + key));
			parent.Add(row);
			SetChecklistState(label, "warn");
			return label;
		}

		private Toggle AddWorkModeToggle(VisualElement parent, string text)
		{
			Toggle toggle = new Toggle { text = text };
			toggle.AddToClassList("pm-workmode-toggle");
			toggle.RegisterValueChangedCallback(_ => RefreshWorkModeButtonStates());
			parent.Add(toggle);
			return toggle;
		}

		private Button CopyButton(Action action, string tooltip)
		{
			Button button = new Button(action) { text = string.Empty, tooltip = tooltip };
			button.AddToClassList("pm-copy-button");
			VisualElement icon = new VisualElement();
			icon.AddToClassList("pm-copy-icon");
			VisualElement back = new VisualElement();
			back.AddToClassList("pm-copy-icon-back");
			VisualElement front = new VisualElement();
			front.AddToClassList("pm-copy-icon-front");
			icon.Add(back);
			icon.Add(front);
			button.Add(icon);
			return button;
		}

		private void CopyText(string key, string value)
		{
			GUIUtility.systemCopyBuffer = value ?? string.Empty;
			if (_lastActionLabel != null)
			{
				_lastActionLabel.text = PerfMeterWindowLocalization.Text(string.IsNullOrEmpty(key) ? "Copied to clipboard." : key + " copied to clipboard.");
			}
		}

		private VisualElement AddActions(VisualElement parent)
		{
			VisualElement actions = new VisualElement();
			actions.AddToClassList("pm-actions");
			parent.Add(actions);
			return actions;
		}

		private VisualElement AddChoiceGroup(VisualElement parent, string caption)
		{
			VisualElement group = new VisualElement();
			group.AddToClassList("pm-choice-group");
			Label label = new Label(caption);
			label.AddToClassList("pm-choice-caption");
			VisualElement actions = new VisualElement();
			actions.AddToClassList("pm-actions");
			group.Add(label);
			group.Add(actions);
			parent.Add(group);
			return actions;
		}

		private Button AddButton(VisualElement parent, string text, Action action)
		{
			Button button = new Button(action) { text = text };
			button.AddToClassList("pm-button");
			parent.Add(button);
			return button;
		}

		private Button AddRuntimeButton(VisualElement parent, string text, Action action)
		{
			return AddRuntimeButton(parent, text, action, null);
		}

		private Button AddRuntimeButton(VisualElement parent, string text, Action action, Func<PerfMeterStatusSnapshot, bool> activeWhen)
		{
			Button button = AddButton(parent, text, action);
			_runtimeButtons.Add(button);
			if (activeWhen != null)
			{
				_runtimeButtonBindings.Add(new RuntimeButtonBinding(button, activeWhen));
			}

			return button;
		}

		private Button AddSettingsButton(VisualElement parent, string text, Action action, Func<bool> activeWhen)
		{
			Button button = AddButton(parent, text, () =>
			{
				action();
				RefreshSettingsButtonStates();
			});
			_settingsButtons.Add(button);
			if (activeWhen != null)
			{
				_settingsButtonBindings.Add(new SettingsButtonBinding(button, activeWhen));
			}

			return button;
		}

		private Button AddSettingsTargetFpsButton(VisualElement parent, PerfMeterTargetFps targetFps)
		{
			return AddSettingsButton(parent, FormatTargetFps(targetFps), () => _settingsTargetFps?.SetValueWithoutNotify(targetFps), () => _settingsTargetFps != null && _settingsTargetFps.value is PerfMeterTargetFps value && value == targetFps);
		}

		private Button AddTargetFpsButton(VisualElement parent, PerfMeterTargetFps targetFps)
		{
			return AddRuntimeButton(parent, FormatTargetFps(targetFps), () => RunRuntimeAction("Target " + FormatTargetFps(targetFps), () => RuntimePerformanceMeter.SetTargetFps(targetFps)), status => status.TargetFps == targetFps);
		}

		private void RunAction(string title, Func<PerfMeterSetupActionResult> action)
		{
			try
			{
				PerfMeterSetupActionResult result = action();
				_lastActionLabel.text = title + ": " + result.Message;
				if (!result.Success)
				{
					EditorUtility.DisplayDialog(PerfMeterWindowLocalization.Text(title + " Failed"), PerfMeterWindowLocalization.Text(result.Message), "OK");
				}
			}
			catch (Exception exception)
			{
				_lastActionLabel.text = title + ": " + exception.Message;
				EditorUtility.DisplayDialog(PerfMeterWindowLocalization.Text(title + " Failed"), exception.Message, "OK");
			}
			finally
			{
				RefreshAll();
			}
		}

		private void RefreshAll()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = PerfMeterSetupUtility.GetStatus();

			_projectStatus.text = status.ProjectSettingsMessage;
			_frameTimingStats.text = status.FrameTimingStatsEnabled ? "Enabled" : "Disabled";
			_packagePath.text = string.IsNullOrEmpty(status.PackageAssetPath) ? "Not found" : status.PackageAssetPath;
			SetIndicator(_projectIndicator, status.FrameTimingStatsEnabled, !status.FrameTimingStatsEnabled);
			RefreshSetupChecklist(status);

			_rendererStatus.text = status.RendererMessage;
			BuildRendererList(status);
			RefreshRendererButtons(status);
			bool rendererChecklistActive = IsRendererChecklistActive(status);
			SetIndicator(_rendererIndicator, rendererChecklistActive, status.HasRendererWarnings || !status.RendererFeatureSetupSupported || (status.Renderers.Count > 0 && !status.AllRenderersConfigured));
			RefreshSettingsPanel(status);
			RefreshInitializationCode();
			RefreshRuntimePanel();
			RefreshDebugPanel();
			RefreshWindowSettingsPanel();
			_ftuePage?.Refresh();
			ApplyLocalization();
		}

		private void RefreshWindowSettingsPanel()
		{
			if (_settingsLanguageCurrent == null)
			{
				return;
			}

			_settingsLanguageCurrent.text = PerfMeterWindowLocalization.CurrentLanguageDisplayName();
			if (_languageField != null)
			{
				List<string> languageNames = PerfMeterWindowLocalization.LanguageDisplayNames();
				int languageIndex = PerfMeterWindowLocalization.LanguageIndex(PerfMeterWindowLocalization.CurrentLanguage);
				if (languageIndex >= 0 && languageIndex < languageNames.Count)
				{
					_languageField.SetValueWithoutNotify(languageNames[languageIndex]);
				}
			}
		}

		private void RefreshSettingsPanel(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			if (_settingsStatus == null)
			{
				return;
			}

			_settingsStatus.text = status.Settings.Message;
			_settingsPath.text = status.Settings.AssetPath;
			_settingsResourcesPath.text = status.Settings.ResourcesLoadPath;
			LoadSettingsIntoControls(status.Settings.Snapshot);
		}

		private void LoadSettingsIntoControls(PerfMeterSettingsSnapshot settings)
		{
			_settingsEnabled?.SetValueWithoutNotify(settings.Enabled);
			_settingsAutoStart?.SetValueWithoutNotify(settings.AutoStart);
			if (_settingsCollectionModeStatus != null)
			{
				_settingsCollectionModeStatus.text = settings.CollectionMode.ToString();
			}

			if (_settingsLegacyActivePreset != null)
			{
				_settingsLegacyActivePreset.text = settings.ActivePreset;
			}

			if (_settingsLegacyOverlayModules != null)
			{
				_settingsLegacyOverlayModules.text = settings.OverlayModules.ToString();
			}
			_settingsCollectMetrics?.SetValueWithoutNotify(settings.CollectionMode != PerfMeterCollectionMode.Stopped && settings.Enabled);
			_settingsOverlayVisible?.SetValueWithoutNotify(settings.OverlayVisible);
			_settingsOverdrawDiagnostics?.SetValueWithoutNotify(settings.CollectionMode == PerfMeterCollectionMode.OverdrawDiagnostic);
			_settingsTargetFps?.SetValueWithoutNotify(settings.TargetFps);
			_settingsOverlayCorner?.SetValueWithoutNotify(settings.OverlayCorner);
			_settingsOverlayTheme?.SetValueWithoutNotify(settings.OverlayTheme);
			_settingsOverlayLayout?.SetValueWithoutNotify(settings.OverlayLayout);
			_settingsOverlayFontFamily?.SetValueWithoutNotify(settings.OverlayFontFamily);
			_settingsOverlayScale?.SetValueWithoutNotify(settings.OverlayScale);
			_settingsOverlayOpacity?.SetValueWithoutNotify(settings.OverlayOpacity);
			_settingsOverlayFontSize?.SetValueWithoutNotify(settings.OverlayFontSize);
			_settingsOverlayRefreshInterval?.SetValueWithoutNotify(settings.OverlayRefreshIntervalSeconds);
			_settingsOverlayGraphHistory?.SetValueWithoutNotify(settings.OverlayGraphHistoryLength);
			_settingsEditorWarningsEnabled?.SetValueWithoutNotify(settings.EditorWarningsEnabled);
			_settingsStructuredLogsEnabled?.SetValueWithoutNotify(settings.StructuredLogsEnabled);
			_settingsEditorWarningCooldown?.SetValueWithoutNotify(settings.EditorWarningCooldownSeconds);
			_settingsStructuredLogCooldown?.SetValueWithoutNotify(settings.StructuredLogCooldownSeconds);
			_settingsCallbackCooldown?.SetValueWithoutNotify(settings.CallbackCooldownSeconds);
			_settingsAlertOverdrawThreshold?.SetValueWithoutNotify((float)settings.AlertOverdrawRatioThreshold);
			_settingsAlertTimingFrames?.SetValueWithoutNotify(settings.AlertTimingConsecutiveFrames);
			_settingsAlertFpsFrames?.SetValueWithoutNotify(settings.AlertFpsConsecutiveFrames);
			_settingsAlertGpuTimingUnavailableFrames?.SetValueWithoutNotify(settings.AlertGpuTimingUnavailableConsecutiveFrames);
			_settingsAlertOverdrawFrames?.SetValueWithoutNotify(settings.AlertOverdrawConsecutiveFrames);
			_settingsSessionWarmupFrames?.SetValueWithoutNotify(settings.SessionWarmupFrames);
			_settingsSessionWarmupSeconds?.SetValueWithoutNotify(settings.SessionWarmupSeconds);
			_settingsSessionSampleInterval?.SetValueWithoutNotify(settings.SessionSampleIntervalSeconds);
			_settingsSessionMaxSamples?.SetValueWithoutNotify(settings.SessionMaxSamples);
			_settingsSessionResetOnSceneLoad?.SetValueWithoutNotify(settings.SessionResetOnSceneLoad);
			_settingsSessionSceneLoadIgnoreFrames?.SetValueWithoutNotify(settings.SessionSceneLoadIgnoreFrames);
			_settingsSessionSceneLoadIgnoreSeconds?.SetValueWithoutNotify(settings.SessionSceneLoadIgnoreSeconds);
			_settingsOverdrawDefaultFrameCount?.SetValueWithoutNotify(settings.OverdrawDefaultFrameCount);
			_settingsOverdrawMaxFrameCount?.SetValueWithoutNotify(settings.OverdrawMaxFrameCount);
			ReloadOverlayPresetList(settings.ActiveOverlayPresetId);
			RefreshWorkModeButtonStates();
			RefreshSettingsButtonStates();
		}

		private void RefreshSetupChecklist(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			SetChecklist(_checklistFrameTiming, status.FrameTimingStatsEnabled ? "ok" : "warn", status.FrameTimingStatsEnabled ? "Active - Frame Timing Stats enabled." : "Next action - Enable Frame Timing before relying on GPU timing in builds.");
			SetChecklist(_checklistPackagePath, string.IsNullOrEmpty(status.PackageAssetPath) ? "error" : "ok", string.IsNullOrEmpty(status.PackageAssetPath) ? "Blocked - PerfMeter package folder was not found." : "Active - package path " + status.PackageAssetPath + ".");

			bool rendererChecklistActive = IsRendererChecklistActive(status);
			string rendererState = rendererChecklistActive ? "ok" : "warn";
			string rendererText = rendererChecklistActive
				? "Active - " + status.InstalledRendererCount + " / " + status.Renderers.Count + " renderers have PerfMeter Render Graph feature."
				: "Warning - " + status.RendererMessage;
			SetChecklist(_checklistRendererFeature, rendererState, rendererText);

			bool settingsJsonActive = IsSettingsJsonActive(status.Settings);
			string settingsJsonText = settingsJsonActive
				? "Active - " + status.Settings.Message
				: status.Settings.FileExists
					? "Warning - " + status.Settings.Message
					: "Next action - Save JSON settings to enable zero-code setup.";
			SetChecklist(_checklistSettingsJson, settingsJsonActive ? "ok" : "warn", settingsJsonText);

			List<PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset> overlayPresets = PerfMeterOverlayPresetEditorUtility.LoadProjectPresets(false);
			int validPresetCount = 0;
			for (int i = 0; i < overlayPresets.Count; i++)
			{
				if (overlayPresets[i].IsValid)
				{
					validPresetCount++;
				}
			}

			SetChecklist(_checklistOverlayPresets, validPresetCount > 0 ? "ok" : "optional", validPresetCount > 0 ? "Active - " + validPresetCount + " visual overlay preset JSON file(s) available." : "Optional - run Ensure Default Overlay Presets when visual preset authoring is needed.");
		}

		internal static bool IsRendererChecklistActive(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			return status != null && status.AllRenderersConfigured && !status.HasRendererWarnings;
		}

		internal static bool IsSettingsJsonActive(PerfMeterSetupUtility.PerfMeterSettingsSetupStatus settings)
		{
			return settings != null && settings.FileExists && settings.Snapshot.LoadState == PerfMeterSettingsLoadState.Loaded;
		}

		private PerfMeterSetupActionResult SaveSettingsFromControls()
		{
			ApplyStyleControlsToEditingPreset(false);
			PerfMeterOverlayPresetJson activeVisualPreset = PerfMeterOverlayPresetUtility.Clone(_editingOverlayPreset);
			return PerfMeterSetupActions.SaveSettings(CreateSettingsSnapshotFromControls(activeVisualPreset));
		}

		private PerfMeterSettingsSnapshot CreateSettingsSnapshotFromControls(PerfMeterOverlayPresetJson activeVisualPreset)
		{
			PerfMeterSettingsSnapshot currentSettings = PerfMeterSetupActions.LoadSettings();
			string activeVisualPresetId = activeVisualPreset != null ? activeVisualPreset.id : currentSettings.ActiveOverlayPresetId;
			PerfMeterOverlayModule overlayModules = activeVisualPreset != null
				? PerfMeterOverlayPresetUtility.GetEnabledModules(activeVisualPreset, out string _)
				: currentSettings.OverlayModules;
			PerfMeterOverlayLayout overlayLayout = _settingsOverlayLayout != null && _settingsOverlayLayout.value is PerfMeterOverlayLayout layout ? layout : currentSettings.OverlayLayout;
			return new PerfMeterSettingsSnapshot(
				_settingsEnabled == null || _settingsEnabled.value,
				_settingsAutoStart == null || _settingsAutoStart.value,
				GetSettingsCollectionModeFromControls(currentSettings.CollectionMode),
				_settingsOverlayVisible == null || _settingsOverlayVisible.value,
				_settingsOverlayCorner != null && _settingsOverlayCorner.value is PerfMeterOverlayCorner corner ? corner : PerfMeterOverlayCorner.TopRight,
				PerfMeterSettingsStore.GetLayoutMode(overlayLayout, currentSettings.OverlayMode),
				_settingsTargetFps != null && _settingsTargetFps.value is PerfMeterTargetFps targetFps ? targetFps : PerfMeterTargetFps.Fps60,
				activeVisualPreset != null ? PerfMeterOverlayPreset.Custom.ToString() : currentSettings.ActivePreset,
				overlayModules,
				_settingsSessionWarmupFrames != null ? _settingsSessionWarmupFrames.value : currentSettings.SessionWarmupFrames,
				_settingsSessionWarmupSeconds != null ? _settingsSessionWarmupSeconds.value : currentSettings.SessionWarmupSeconds,
				_settingsSessionSampleInterval != null ? _settingsSessionSampleInterval.value : currentSettings.SessionSampleIntervalSeconds,
				_settingsSessionMaxSamples != null ? _settingsSessionMaxSamples.value : currentSettings.SessionMaxSamples,
				_settingsSessionResetOnSceneLoad != null ? _settingsSessionResetOnSceneLoad.value : currentSettings.SessionResetOnSceneLoad,
				_settingsSessionSceneLoadIgnoreFrames != null ? _settingsSessionSceneLoadIgnoreFrames.value : currentSettings.SessionSceneLoadIgnoreFrames,
				_settingsSessionSceneLoadIgnoreSeconds != null ? _settingsSessionSceneLoadIgnoreSeconds.value : currentSettings.SessionSceneLoadIgnoreSeconds,
				_settingsEditorWarningCooldown != null ? _settingsEditorWarningCooldown.value : currentSettings.EditorWarningCooldownSeconds,
				_settingsStructuredLogCooldown != null ? _settingsStructuredLogCooldown.value : currentSettings.StructuredLogCooldownSeconds,
				_settingsCallbackCooldown != null ? _settingsCallbackCooldown.value : currentSettings.CallbackCooldownSeconds,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty,
				overlayScale: _settingsOverlayScale != null ? _settingsOverlayScale.value : currentSettings.OverlayScale,
				overlayOpacity: _settingsOverlayOpacity != null ? _settingsOverlayOpacity.value : currentSettings.OverlayOpacity,
				overlayFontSize: _settingsOverlayFontSize != null ? _settingsOverlayFontSize.value : currentSettings.OverlayFontSize,
				overlayRefreshIntervalSeconds: _settingsOverlayRefreshInterval != null ? _settingsOverlayRefreshInterval.value : currentSettings.OverlayRefreshIntervalSeconds,
				overlayGraphHistoryLength: _settingsOverlayGraphHistory != null ? _settingsOverlayGraphHistory.value : currentSettings.OverlayGraphHistoryLength,
				overdrawDefaultFrameCount: _settingsOverdrawDefaultFrameCount != null ? _settingsOverdrawDefaultFrameCount.value : currentSettings.OverdrawDefaultFrameCount,
				overdrawMaxFrameCount: _settingsOverdrawMaxFrameCount != null ? _settingsOverdrawMaxFrameCount.value : currentSettings.OverdrawMaxFrameCount,
				alertOverdrawRatioThreshold: _settingsAlertOverdrawThreshold != null ? _settingsAlertOverdrawThreshold.value : currentSettings.AlertOverdrawRatioThreshold,
				alertTimingConsecutiveFrames: _settingsAlertTimingFrames != null ? _settingsAlertTimingFrames.value : currentSettings.AlertTimingConsecutiveFrames,
				alertFpsConsecutiveFrames: _settingsAlertFpsFrames != null ? _settingsAlertFpsFrames.value : currentSettings.AlertFpsConsecutiveFrames,
				alertGpuTimingUnavailableConsecutiveFrames: _settingsAlertGpuTimingUnavailableFrames != null ? _settingsAlertGpuTimingUnavailableFrames.value : currentSettings.AlertGpuTimingUnavailableConsecutiveFrames,
				alertOverdrawConsecutiveFrames: _settingsAlertOverdrawFrames != null ? _settingsAlertOverdrawFrames.value : currentSettings.AlertOverdrawConsecutiveFrames,
				overlayTheme: _settingsOverlayTheme != null && _settingsOverlayTheme.value is PerfMeterOverlayTheme theme ? theme : currentSettings.OverlayTheme,
				overlayLayout: overlayLayout,
				overlayFontFamily: _settingsOverlayFontFamily != null && _settingsOverlayFontFamily.value is PerfMeterOverlayFontFamily fontFamily ? fontFamily : currentSettings.OverlayFontFamily,
				editorWarningsEnabled: _settingsEditorWarningsEnabled == null ? currentSettings.EditorWarningsEnabled : _settingsEditorWarningsEnabled.value,
				activeOverlayPresetId: activeVisualPresetId,
				activeOverlayPreset: activeVisualPreset,
				structuredLogsEnabled: _settingsStructuredLogsEnabled == null ? currentSettings.StructuredLogsEnabled : _settingsStructuredLogsEnabled.value);
		}

		private PerfMeterCollectionMode GetSettingsCollectionModeFromControls(PerfMeterCollectionMode fallback)
		{
			bool enabled = _settingsEnabled == null || _settingsEnabled.value;
			bool collectMetrics = _settingsCollectMetrics == null || _settingsCollectMetrics.value;
			if (!enabled || !collectMetrics)
			{
				return PerfMeterCollectionMode.Stopped;
			}

			if (_settingsOverdrawDiagnostics != null && _settingsOverdrawDiagnostics.value)
			{
				return PerfMeterCollectionMode.OverdrawDiagnostic;
			}

			if (_settingsOverlayVisible != null)
			{
				return _settingsOverlayVisible.value ? PerfMeterCollectionMode.Overlay : PerfMeterCollectionMode.Background;
			}

			return fallback;
		}

		private void ReloadOverlayPresetList(string preferredPresetId)
		{
			_overlayPresetAssets = PerfMeterOverlayPresetEditorUtility.LoadProjectPresets(true);
			_overlayPresetChoiceLabels.Clear();
			for (int i = 0; i < _overlayPresetAssets.Count; i++)
			{
				_overlayPresetChoiceLabels.Add(GetOverlayPresetChoiceLabel(_overlayPresetAssets[i]));
			}

			if (_overlayPresetChoiceLabels.Count == 0)
			{
				_overlayPresetChoiceLabels.Add("No presets");
			}

			if (_settingsVisualPresetField != null)
			{
				_settingsVisualPresetField.choices = new List<string>(_overlayPresetChoiceLabels);
			}

			if (_runtimeVisualPresetField != null)
			{
				_runtimeVisualPresetField.choices = new List<string>(_overlayPresetChoiceLabels);
			}

			int selectedIndex = FindOverlayPresetIndex(preferredPresetId);
			if (selectedIndex < 0)
			{
				selectedIndex = FindFirstValidOverlayPresetIndex();
			}

			if (selectedIndex < 0)
			{
				selectedIndex = 0;
			}

			string selectedLabel = _overlayPresetChoiceLabels[Mathf.Clamp(selectedIndex, 0, _overlayPresetChoiceLabels.Count - 1)];
			_settingsVisualPresetField?.SetValueWithoutNotify(selectedLabel);
			_runtimeVisualPresetField?.SetValueWithoutNotify(selectedLabel);
			LoadOverlayPresetForEditing(selectedIndex);
		}

		private int FindOverlayPresetIndex(string presetId)
		{
			if (string.IsNullOrEmpty(presetId))
			{
				return -1;
			}

			for (int i = 0; i < _overlayPresetAssets.Count; i++)
			{
				PerfMeterOverlayPresetJson preset = _overlayPresetAssets[i].Preset;
				if (preset != null && string.Equals(preset.id, presetId, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		private int FindFirstValidOverlayPresetIndex()
		{
			for (int i = 0; i < _overlayPresetAssets.Count; i++)
			{
				if (_overlayPresetAssets[i].IsValid)
				{
					return i;
				}
			}

			return -1;
		}

		internal static string GetOverlayPresetChoiceLabel(PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset asset)
		{
			if (asset == null)
			{
				return PerfMeterWindowLocalization.Text("Invalid preset");
			}

			if (!asset.IsValid || asset.Preset == null)
			{
				return PerfMeterWindowLocalization.Format("Invalid: {0}", asset.DisplayName);
			}

			return PerfMeterWindowLocalization.Format("{0} [{1}]", asset.Preset.displayName, asset.Preset.id);
		}

		internal void SelectOverlayPresetByLabel(string label)
		{
			for (int i = 0; i < _overlayPresetChoiceLabels.Count; i++)
			{
				if (string.Equals(_overlayPresetChoiceLabels[i], label, StringComparison.Ordinal))
				{
					LoadOverlayPresetForEditing(i);
					ApplyEditingPresetToRuntimePreview();
					ApplyLocalization();
					return;
				}
			}
		}

		private void LoadOverlayPresetForEditing(int index)
		{
			if (index < 0 || index >= _overlayPresetAssets.Count)
			{
				_editingOverlayPreset = null;
				_editingOverlayPresetPath = string.Empty;
				_editingOverlayPresetModified = false;
				RefreshVisualPresetUi();
				BuildWidgetCompositionRows();
				return;
			}

			PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset asset = _overlayPresetAssets[index];
			_editingOverlayPresetPath = asset.AssetPath;
			_editingOverlayPreset = asset.IsValid ? PerfMeterOverlayPresetUtility.Clone(asset.Preset) : null;
			_editingOverlayPresetModified = false;
			PushEditingPresetToControls();
			BuildWidgetCompositionRows();
			RefreshVisualPresetUi(asset);
		}

		private void PushEditingPresetToControls()
		{
			_suppressOverlayPresetCallbacks = true;
			try
			{
				PerfMeterOverlayPresetJson preset = _editingOverlayPreset;
				if (preset == null)
				{
					return;
				}

				_settingsOverlayCorner?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetCorner(preset));
				_settingsOverlayLayout?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetLayout(preset));
				_settingsOverlayTheme?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetTheme(preset));
				_settingsOverlayFontFamily?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetFontFamily(preset));
				_settingsOverlayScale?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetScale(preset, 1f));
				_settingsOverlayOpacity?.SetValueWithoutNotify(PerfMeterOverlayPresetUtility.GetOpacity(preset, 0.84f));
				_settingsPresetMaxWidth?.SetValueWithoutNotify(preset.style != null ? preset.style.maxWidth : PerfMeterOverlayLayoutLimits.MaxWidth);
				_settingsPresetGap?.SetValueWithoutNotify(preset.style != null ? preset.style.gap : 4);
			}
			finally
			{
				_suppressOverlayPresetCallbacks = false;
			}
		}

		private void OnOverlayPresetStyleChanged()
		{
			ApplyStyleControlsToEditingPreset(true);
		}

		private void ApplyStyleControlsToEditingPreset(bool markModified)
		{
			if (_suppressOverlayPresetCallbacks || _editingOverlayPreset == null)
			{
				return;
			}

			_editingOverlayPreset.style = _editingOverlayPreset.style ?? new PerfMeterOverlayPresetStyleJson();
			if (_settingsOverlayCorner != null && _settingsOverlayCorner.value is PerfMeterOverlayCorner corner)
			{
				_editingOverlayPreset.style.anchor = corner.ToString();
			}

			if (_settingsOverlayLayout != null && _settingsOverlayLayout.value is PerfMeterOverlayLayout layout)
			{
				_editingOverlayPreset.style.layout = layout.ToString();
			}

			if (_settingsOverlayTheme != null && _settingsOverlayTheme.value is PerfMeterOverlayTheme theme)
			{
				_editingOverlayPreset.style.theme = theme.ToString();
			}

			if (_settingsOverlayFontFamily != null && _settingsOverlayFontFamily.value is PerfMeterOverlayFontFamily fontFamily)
			{
				_editingOverlayPreset.style.font = fontFamily.ToString();
			}

			if (_settingsOverlayScale != null)
			{
				_editingOverlayPreset.style.scale = _settingsOverlayScale.value;
			}

			if (_settingsOverlayOpacity != null)
			{
				_editingOverlayPreset.style.opacity = _settingsOverlayOpacity.value;
			}

			if (_settingsPresetMaxWidth != null)
			{
				_editingOverlayPreset.style.maxWidth = _settingsPresetMaxWidth.value;
			}

			if (_settingsPresetGap != null)
			{
				_editingOverlayPreset.style.gap = _settingsPresetGap.value;
			}

			if (markModified)
			{
				MarkOverlayPresetModified();
			}
		}

		private void MarkOverlayPresetModified()
		{
			if (_suppressOverlayPresetCallbacks || _editingOverlayPreset == null)
			{
				return;
			}

			_editingOverlayPresetModified = true;
			RefreshVisualPresetUi();
			ApplyEditingPresetToRuntimePreview();
		}

		private void RefreshVisualPresetUi()
		{
			RefreshVisualPresetUi(GetCurrentOverlayPresetAsset());
		}

		private void RefreshVisualPresetUi(PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset asset)
		{
			if (_visualPresetDescription != null)
			{
				_visualPresetDescription.text = _editingOverlayPreset != null ? _editingOverlayPreset.description : asset != null ? asset.Warning : "No valid preset selected.";
			}

			if (_visualPresetTags != null)
			{
				_visualPresetTags.text = _editingOverlayPreset != null ? FormatPresetTags(_editingOverlayPreset.tags) : "Unavailable";
			}

			if (_visualPresetSchemaVersion != null)
			{
				_visualPresetSchemaVersion.text = _editingOverlayPreset != null
					? FormatOptionalValue(_editingOverlayPreset.schema) + " / " + _editingOverlayPreset.version
					: "Unavailable";
			}

			if (_visualPresetReservedWidgetMetadata != null)
			{
				_visualPresetReservedWidgetMetadata.text = _editingOverlayPreset != null
					? FormatReservedWidgetMetadata(_editingOverlayPreset.widgets)
					: "Unavailable";
			}

			if (_visualPresetSource != null)
			{
				_visualPresetSource.text = string.IsNullOrEmpty(_editingOverlayPresetPath) ? "-" : _editingOverlayPresetPath;
			}

			if (_visualPresetSummary != null)
			{
				_visualPresetSummary.text = _editingOverlayPreset != null ? PerfMeterOverlayPresetUtility.BuildSummary(_editingOverlayPreset) : "Invalid or missing preset";
			}

			if (_visualPresetStatus != null)
			{
				string status = asset == null ? "Missing" : asset.IsValid ? "Project preset" : "Invalid JSON";
				if (asset != null && asset.ReadOnly)
				{
					status += " · Read-only";
				}

				if (_editingOverlayPresetModified)
				{
					status += " · Modified";
				}

				if (asset != null && !string.IsNullOrEmpty(asset.Warning))
				{
					status += " · " + asset.Warning;
				}

				_visualPresetStatus.text = status;
			}

			bool canSave = _editingOverlayPreset != null && asset != null && !asset.ReadOnly;
			_visualPresetSaveButton?.SetEnabled(canSave);
			_visualPresetReloadButton?.SetEnabled(asset != null);
			_visualPresetRevealButton?.SetEnabled(asset != null && !string.IsNullOrEmpty(asset.AssetPath));
		}

		private PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset GetCurrentOverlayPresetAsset()
		{
			if (string.IsNullOrEmpty(_editingOverlayPresetPath))
			{
				return null;
			}

			for (int i = 0; i < _overlayPresetAssets.Count; i++)
			{
				if (string.Equals(_overlayPresetAssets[i].AssetPath, _editingOverlayPresetPath, StringComparison.OrdinalIgnoreCase))
				{
					return _overlayPresetAssets[i];
				}
			}

			return null;
		}

		private void SaveCurrentOverlayPreset()
		{
			PerfMeterSetupActionResult result = SaveCurrentOverlayPresetForProject();
			_lastActionLabel.text = "Save overlay preset: " + result.Message;
			if (!result.Success)
			{
				EditorUtility.DisplayDialog(PerfMeterWindowLocalization.Text("Save overlay preset failed"), PerfMeterWindowLocalization.Text(result.Message), "OK");
			}

			ApplyLocalization();
		}

		internal PerfMeterSetupActionResult SaveCurrentOverlayPresetForProject()
		{
			if (_editingOverlayPreset == null || string.IsNullOrEmpty(_editingOverlayPresetPath))
			{
				return PerfMeterSetupActionResult.Fail("Select a valid visual overlay preset.");
			}

			ApplyStyleControlsToEditingPreset(false);
			PerfMeterOverlayPresetJson activeVisualPreset = PerfMeterOverlayPresetUtility.Clone(_editingOverlayPreset);
			string presetId = activeVisualPreset.id?.Trim() ?? string.Empty;
			PerfMeterSetupUtility.InstallResult presetResult = PerfMeterOverlayPresetEditorUtility.SavePreset(_editingOverlayPresetPath, _editingOverlayPreset);
			if (!presetResult.Success)
			{
				return PerfMeterSetupActionResult.Fail(presetResult.Message);
			}

			_editingOverlayPresetModified = false;
			PerfMeterSetupActionResult settingsResult = PerfMeterSetupActions.SaveActiveOverlayPreset(presetId);
			ReloadOverlayPresetList(presetId);
			if (!settingsResult.Success)
			{
				return PerfMeterSetupActionResult.Fail(presetResult.Message + " Project settings were not saved: " + settingsResult.Message);
			}

			return PerfMeterSetupActionResult.Ok(presetResult.Message + " Set '" + presetId + "' active in project Resources settings for subsequent Play Mode sessions and builds.");
		}

		private void SaveCurrentOverlayPresetAsCustom()
		{
			if (_editingOverlayPreset == null)
			{
				return;
			}

			ApplyStyleControlsToEditingPreset(false);
			string path = PerfMeterOverlayPresetEditorUtility.SaveCustomPresetWithPanel(_editingOverlayPreset, _editingOverlayPreset.displayName);
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			string id = PerfMeterOverlayPresetEditorUtility.CustomPresetIdForPath(path);
			_lastActionLabel.text = "Save as custom overlay preset: " + path;
			ReloadOverlayPresetList(id);
			ApplyLocalization();
		}

		private void DuplicateCurrentOverlayPreset()
		{
			if (_editingOverlayPreset == null)
			{
				return;
			}

			ApplyStyleControlsToEditingPreset(false);
			PerfMeterOverlayPresetJson copy = PerfMeterOverlayPresetUtility.Clone(_editingOverlayPreset);
			copy.displayName = string.IsNullOrEmpty(copy.displayName) ? "Custom Overlay Copy" : copy.displayName + " Copy";
			copy.id = PerfMeterOverlayPresetEditorUtility.Slug(copy.displayName);
			string path = PerfMeterOverlayPresetEditorUtility.SaveCustomPresetWithPanel(copy, copy.displayName);
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			string id = PerfMeterOverlayPresetEditorUtility.CustomPresetIdForPath(path);
			_lastActionLabel.text = "Duplicate overlay preset: " + path;
			ReloadOverlayPresetList(id);
			ApplyLocalization();
		}

		private void ReloadCurrentOverlayPreset()
		{
			string id = _editingOverlayPreset != null ? _editingOverlayPreset.id : string.Empty;
			ReloadOverlayPresetList(id);
			_lastActionLabel.text = "Reload overlay presets: done.";
			ApplyLocalization();
		}

		private void RevealCurrentOverlayPreset()
		{
			PerfMeterOverlayPresetEditorUtility.Reveal(_editingOverlayPresetPath);
		}

		private void ApplyEditingPresetToRuntimePreview()
		{
			if (!EditorApplication.isPlaying || _editingOverlayPreset == null)
			{
				return;
			}

			RuntimePerformanceMeter.ApplyVisualOverlayPreset(_editingOverlayPreset.id, PerfMeterOverlayPresetUtility.Clone(_editingOverlayPreset));
			RefreshRuntimePanel();
		}

		private void BuildWidgetCompositionRows()
		{
			_presetWidgetBindings.Clear();
			if (_settingsWidgetCompositionRows == null)
			{
				return;
			}

			_settingsWidgetCompositionRows.Clear();
			_settingsWidgetCompositionRows.Add(CreatePresetWidgetRow("Order", null, "Widget", "Kind", "Requires", null, null, true));
			if (_editingOverlayPreset == null)
			{
				_settingsWidgetCompositionRows.Add(CreatePresetWidgetRow("-", null, "No valid preset selected", "-", "-", null, null, false));
				return;
			}

			EnsurePresetBlockWidgets(_editingOverlayPreset);
			Array.Sort(_editingOverlayPreset.widgets, (left, right) => (left?.order ?? int.MaxValue).CompareTo(right?.order ?? int.MaxValue));
			for (int i = 0; i < _editingOverlayPreset.widgets.Length; i++)
			{
				PerfMeterOverlayPresetWidgetJson widget = _editingOverlayPreset.widgets[i];
				if (widget == null || !PerfMeterWidgetRegistry.TryGetDescriptor(widget.id, out PerfMeterWidgetDescriptor descriptor) || !descriptor.IsPresetBlock)
				{
					continue;
				}

				Toggle toggle = new Toggle();
				toggle.SetValueWithoutNotify(widget.enabled);
				toggle.RegisterValueChangedCallback(evt =>
				{
					widget.enabled = evt.newValue;
					MarkOverlayPresetModified();
				});

				Button up = new Button(() => MovePresetWidget(widget.id, -1)) { text = "▲" };
				Button down = new Button(() => MovePresetWidget(widget.id, 1)) { text = "▼" };
				_settingsWidgetCompositionRows.Add(CreatePresetWidgetRow(widget.order.ToString(), toggle, descriptor.DisplayName, descriptor.Kind, string.Join(", ", descriptor.RequiredProviders), up, down, false));
				_presetWidgetBindings.Add(new OverlayPresetWidgetBinding(widget.id, toggle));
			}
		}

		private static VisualElement CreatePresetWidgetRow(string order, Toggle enabledToggle, string widgetName, string kind, string requires, Button up, Button down, bool header)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-debug-row");
			if (header)
			{
				row.AddToClassList("pm-debug-row--header");
			}

			row.Add(CreateDebugCell(order, "pm-debug-cell--source"));
			VisualElement enabledCell = new VisualElement();
			enabledCell.AddToClassList("pm-debug-cell");
			enabledCell.AddToClassList("pm-debug-cell--type");
			if (enabledToggle != null)
			{
				enabledCell.Add(enabledToggle);
			}
			else
			{
				enabledCell.Add(new Label(header ? "Enabled" : "-"));
			}

			row.Add(enabledCell);
			row.Add(CreateDebugCell(widgetName, "pm-debug-cell--name"));
			row.Add(CreateDebugCell(kind, "pm-debug-cell--module"));
			row.Add(CreateDebugCell(requires, "pm-debug-cell--details"));
			VisualElement actions = new VisualElement();
			actions.AddToClassList("pm-debug-cell");
			actions.AddToClassList("pm-debug-cell--type");
			if (up != null)
			{
				actions.Add(up);
			}

			if (down != null)
			{
				actions.Add(down);
			}

			row.Add(actions);
			return row;
		}

		private static void EnsurePresetBlockWidgets(PerfMeterOverlayPresetJson preset)
		{
			if (preset == null)
			{
				return;
			}

			List<PerfMeterOverlayPresetWidgetJson> widgets = new List<PerfMeterOverlayPresetWidgetJson>(preset.widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>());
			PerfMeterWidgetDescriptor[] descriptors = PerfMeterWidgetRegistry.GetPresetBlockDescriptors();
			int maxOrder = 0;
			for (int i = 0; i < widgets.Count; i++)
			{
				if (widgets[i] != null)
				{
					maxOrder = Math.Max(maxOrder, widgets[i].order);
				}
			}

			for (int i = 0; i < descriptors.Length; i++)
			{
				if (FindWidget(widgets, descriptors[i].Id) != null)
				{
					continue;
				}

				maxOrder += 10;
				widgets.Add(new PerfMeterOverlayPresetWidgetJson
				{
					id = descriptors[i].Id,
					enabled = false,
					order = maxOrder
				});
			}

			preset.widgets = widgets.ToArray();
		}

		private static PerfMeterOverlayPresetWidgetJson FindWidget(List<PerfMeterOverlayPresetWidgetJson> widgets, string id)
		{
			for (int i = 0; i < widgets.Count; i++)
			{
				if (widgets[i] != null && string.Equals(widgets[i].id, id, StringComparison.OrdinalIgnoreCase))
				{
					return widgets[i];
				}
			}

			return null;
		}

		private void MovePresetWidget(string id, int delta)
		{
			if (_editingOverlayPreset == null || _editingOverlayPreset.widgets == null)
			{
				return;
			}

			Array.Sort(_editingOverlayPreset.widgets, (left, right) => (left?.order ?? int.MaxValue).CompareTo(right?.order ?? int.MaxValue));
			int index = -1;
			for (int i = 0; i < _editingOverlayPreset.widgets.Length; i++)
			{
				if (_editingOverlayPreset.widgets[i] != null && string.Equals(_editingOverlayPreset.widgets[i].id, id, StringComparison.OrdinalIgnoreCase))
				{
					index = i;
					break;
				}
			}

			int target = index + delta;
			if (index < 0 || target < 0 || target >= _editingOverlayPreset.widgets.Length)
			{
				return;
			}

			PerfMeterOverlayPresetWidgetJson temp = _editingOverlayPreset.widgets[index];
			_editingOverlayPreset.widgets[index] = _editingOverlayPreset.widgets[target];
			_editingOverlayPreset.widgets[target] = temp;
			for (int i = 0; i < _editingOverlayPreset.widgets.Length; i++)
			{
				if (_editingOverlayPreset.widgets[i] != null)
				{
					_editingOverlayPreset.widgets[i].order = (i + 1) * 10;
				}
			}

			BuildWidgetCompositionRows();
			MarkOverlayPresetModified();
			ApplyLocalization();
		}

		private void BuildRendererList(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			if (_rendererList == null)
			{
				return;
			}

			_rendererList.Clear();
			if (status.Renderers.Count == 0)
			{
				Label emptyLabel = new Label("No renderer assets discovered.");
				emptyLabel.AddToClassList("pm-renderer-empty");
				_rendererList.Add(emptyLabel);
				_selectedRendererPaths.Clear();
				return;
			}

			HashSet<string> discoveredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < status.Renderers.Count; i++)
			{
				PerfMeterSetupUtility.RendererSetupStatus renderer = status.Renderers[i];
				discoveredPaths.Add(renderer.AssetPath);
				if (renderer.HasPerfMeterFeature)
				{
					_selectedRendererPaths.Remove(renderer.AssetPath);
				}

				_rendererList.Add(CreateRendererRow(renderer));
			}

			string[] selectedPaths = GetSelectedRendererPaths();
			for (int i = 0; i < selectedPaths.Length; i++)
			{
				if (!discoveredPaths.Contains(selectedPaths[i]))
				{
					_selectedRendererPaths.Remove(selectedPaths[i]);
				}
			}
		}

		private VisualElement CreateRendererRow(PerfMeterSetupUtility.RendererSetupStatus renderer)
		{
			string assetPath = renderer.AssetPath ?? string.Empty;
			bool canInstall = renderer.CanInstallFeature && !renderer.HasPerfMeterFeature && renderer.IsEditable && !string.IsNullOrEmpty(assetPath);

			VisualElement row = new VisualElement();
			row.AddToClassList("pm-renderer-row");
			if (renderer.HasPerfMeterFeature)
			{
				row.AddToClassList("pm-renderer-row--installed");
			}
			else if (!renderer.IsEditable)
			{
				row.AddToClassList("pm-renderer-row--readonly");
			}

			Toggle toggle = new Toggle();
			toggle.AddToClassList("pm-renderer-toggle");
			toggle.SetEnabled(canInstall);
			toggle.SetValueWithoutNotify(canInstall && _selectedRendererPaths.Contains(assetPath));
			toggle.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					_selectedRendererPaths.Add(assetPath);
				}
				else
				{
					_selectedRendererPaths.Remove(assetPath);
				}

				RefreshSelectedRendererButton();
			});
			row.Add(toggle);

			Label statusLabel = new Label(GetRendererStatusText(renderer));
			statusLabel.AddToClassList("pm-renderer-status");
			statusLabel.AddToClassList(renderer.HasPerfMeterFeature ? "pm-renderer-status--installed" : renderer.IsEditable ? "pm-renderer-status--missing" : "pm-renderer-status--readonly");
			row.Add(statusLabel);

			VisualElement textColumn = new VisualElement();
			textColumn.AddToClassList("pm-renderer-text");
			Label nameLabel = new Label(string.IsNullOrEmpty(renderer.Name) ? "Renderer" : renderer.Name);
			nameLabel.AddToClassList("pm-renderer-name");
			if (!string.IsNullOrEmpty(renderer.Name))
			{
				nameLabel.AddToClassList("pm-no-localize");
			}

			Label pathLabel = new Label(assetPath);
			pathLabel.AddToClassList("pm-renderer-path");
			pathLabel.AddToClassList("pm-no-localize");
			textColumn.Add(nameLabel);
			textColumn.Add(pathLabel);
			string details = GetRendererDetailsText(renderer);
			if (!string.IsNullOrEmpty(details))
			{
				Label detailsLabel = new Label(details);
				detailsLabel.AddToClassList("pm-renderer-details");
				textColumn.Add(detailsLabel);
			}

			row.Add(textColumn);
			if (!string.IsNullOrEmpty(assetPath))
			{
				row.Add(CopyButton(() => CopyText("Renderer Path", assetPath), "Copy renderer path"));
			}

			return row;
		}

		private void RefreshRendererButtons(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			bool hasMissing = HasMissingRendererFeature(status);
			RefreshSelectedRendererButton();
			_installAllMissingRendererButton?.SetEnabled(hasMissing);
			_selectMissingRendererButton?.SetEnabled(hasMissing);
		}

		private void RefreshSelectedRendererButton()
		{
			_installSelectedRendererButton?.SetEnabled(_selectedRendererPaths.Count > 0 && PerfMeterSetupUtility.IsRendererFeatureSetupSupported);
		}

		private void SelectMissingRenderers()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = PerfMeterSetupUtility.GetStatus();
			_selectedRendererPaths.Clear();
			for (int i = 0; i < status.Renderers.Count; i++)
			{
				PerfMeterSetupUtility.RendererSetupStatus renderer = status.Renderers[i];
				if (!renderer.HasPerfMeterFeature && renderer.CanInstallFeature && !string.IsNullOrEmpty(renderer.AssetPath))
				{
					_selectedRendererPaths.Add(renderer.AssetPath);
				}
			}

			BuildRendererList(status);
			RefreshRendererButtons(status);
			if (_lastActionLabel != null)
			{
				_lastActionLabel.text = "Selected " + _selectedRendererPaths.Count + " renderer asset(s) missing PerfMeter feature.";
			}

			ApplyLocalization();
		}

		private string[] GetSelectedRendererPaths()
		{
			string[] paths = new string[_selectedRendererPaths.Count];
			_selectedRendererPaths.CopyTo(paths);
			return paths;
		}

		private static bool HasMissingRendererFeature(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			if (!status.RendererFeatureSetupSupported)
			{
				return false;
			}

			for (int i = 0; i < status.Renderers.Count; i++)
			{
				if (!status.Renderers[i].HasPerfMeterFeature && status.Renderers[i].CanInstallFeature)
				{
					return true;
				}
			}

			return false;
		}

		private static string GetRendererStatusText(PerfMeterSetupUtility.RendererSetupStatus renderer)
		{
			if (renderer.HasPerfMeterFeature)
			{
				return renderer.HasMissingFeatureReference ? "Installed + broken refs" : "Installed";
			}

			if (!renderer.IsEditable)
			{
				return renderer.HasMissingFeatureReference ? "Not editable + broken refs" : "Not editable";
			}

			if (!renderer.CanInstallFeature)
			{
				return renderer.HasMissingFeatureReference ? "Unsupported + broken refs" : "Unsupported";
			}

			return renderer.HasMissingFeatureReference ? "Missing + broken refs" : "Missing";
		}

		private static string GetRendererDetailsText(PerfMeterSetupUtility.RendererSetupStatus renderer)
		{
			List<string> details = new List<string>();
			if (renderer.IsActive)
			{
				details.Add("Active renderer");
			}

			if (renderer.IsInPackage)
			{
				details.Add("Inside Packages; setup will not modify it");
			}

			if (renderer.HasMissingFeatureReference)
			{
				details.Add("Missing renderer feature reference present");
			}

			if (renderer.IsEditable && !renderer.CanInstallFeature)
			{
				details.Add("Setup requires Unity 6000.4+ with URP 17.4+");
			}

			return details.Count > 0 ? string.Join("; ", details) : string.Empty;
		}

		private void CopyInitializationCode()
		{
			GUIUtility.systemCopyBuffer = _initCode != null ? _initCode.value : BuildInitializationSnippetFromOptions();
			_lastActionLabel.text = PerfMeterWindowLocalization.Text("Initialization code copied to clipboard.");
		}

		private void RefreshInitializationCode()
		{
			if (_initCode != null)
			{
				_initCode.value = BuildInitializationSnippetFromOptions();
			}
		}

		private string BuildInitializationSnippetFromOptions()
		{
			return PerfMeterSetupUtility.BuildInitializationSnippet(PerfMeterSetupUtility.LoadSettingsSnapshot());
		}

		private void SelectCurrentTab()
		{
			switch (_currentTab)
			{
				case "FTUE":
					SelectFtueTab();
					break;
				case "Presets":
					SelectPresetsTab();
					break;
				case "Runtime":
					SelectRuntimeTab();
					break;
				case "Debug":
					SelectDebugTab();
					break;
				default:
					SelectSetupTab();
					break;
			}
		}

		private void SelectFtueTab()
		{
			_currentTab = "FTUE";
			_ftueTab?.SetValueWithoutNotify(true);
			_setupTab?.SetValueWithoutNotify(false);
			_presetsTab?.SetValueWithoutNotify(false);
			_runtimeTab?.SetValueWithoutNotify(false);
			_debugTab?.SetValueWithoutNotify(false);
			if (_ftuePanel != null)
			{
				_ftuePanel.style.display = DisplayStyle.Flex;
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_debugPanel != null)
			{
				_debugPanel.style.display = DisplayStyle.None;
			}

			_ftuePage?.Refresh();
			ApplyLocalization();
		}

		private void SelectSetupTab()
		{
			_currentTab = "Setup";
			_ftueTab?.SetValueWithoutNotify(false);
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(true);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(false);
			}

			if (_presetsTab != null)
			{
				_presetsTab.SetValueWithoutNotify(false);
			}

			if (_debugTab != null)
			{
				_debugTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.Flex;
			}

			if (_ftuePanel != null)
			{
				_ftuePanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_debugPanel != null)
			{
				_debugPanel.style.display = DisplayStyle.None;
			}

		}

		private void SelectPresetsTab()
		{
			_currentTab = "Presets";
			_ftueTab?.SetValueWithoutNotify(false);
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(false);
			}

			if (_presetsTab != null)
			{
				_presetsTab.SetValueWithoutNotify(true);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(false);
			}

			if (_debugTab != null)
			{
				_debugTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_ftuePanel != null)
			{
				_ftuePanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.Flex;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_debugPanel != null)
			{
				_debugPanel.style.display = DisplayStyle.None;
			}

		}

		private void SelectRuntimeTab()
		{
			_currentTab = "Runtime";
			_ftueTab?.SetValueWithoutNotify(false);
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(false);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(true);
			}

			if (_presetsTab != null)
			{
				_presetsTab.SetValueWithoutNotify(false);
			}

			if (_debugTab != null)
			{
				_debugTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_ftuePanel != null)
			{
				_ftuePanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.Flex;
			}

			if (_debugPanel != null)
			{
				_debugPanel.style.display = DisplayStyle.None;
			}

			RefreshRuntimePanel();
			ApplyLocalization();
		}

		private void SelectDebugTab()
		{
			_currentTab = "Debug";
			_ftueTab?.SetValueWithoutNotify(false);
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(false);
			}

			if (_presetsTab != null)
			{
				_presetsTab.SetValueWithoutNotify(false);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(false);
			}

			if (_debugTab != null)
			{
				_debugTab.SetValueWithoutNotify(true);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_ftuePanel != null)
			{
				_ftuePanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_debugPanel != null)
			{
				_debugPanel.style.display = DisplayStyle.Flex;
			}

			RefreshDebugPanel();
			ApplyLocalization();
		}

		private void SetFtueVisibility(bool visible)
		{
			if (_ftueTab != null)
			{
				_ftueTab.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}

			if (!visible && string.Equals(_currentTab, "FTUE", StringComparison.Ordinal))
			{
				SelectSetupTab();
			}
		}

		private void ShowFtueForReview()
		{
			SetFtueVisibility(true);
			SelectFtueTab();
		}

		private void RunRuntimeAction(string title, Action action)
		{
			if (!EditorApplication.isPlaying)
			{
				_lastActionLabel.text = title + ": enter Play Mode to use runtime controls.";
				RefreshRuntimePanel();
				ApplyLocalization();
				return;
			}

			try
			{
				action();
				_lastActionLabel.text = title + ": done.";
			}
			catch (Exception exception)
			{
				_lastActionLabel.text = title + ": " + exception.Message;
				EditorUtility.DisplayDialog(PerfMeterWindowLocalization.Text(title + " Failed"), exception.Message, "OK");
			}
			finally
			{
				RefreshRuntimePanel();
				ApplyLocalization();
			}
		}

		private void ApplyRuntimeTechnicalOverrides()
		{
			float updateInterval = _runtimeUpdateInterval != null ? _runtimeUpdateInterval.value : PerfMeterSetupActions.LoadSettings().OverlayRefreshIntervalSeconds;
			int graphHistory = _runtimeGraphHistory != null ? _runtimeGraphHistory.value : PerfMeterSetupActions.LoadSettings().OverlayGraphHistoryLength;
			RuntimePerformanceMeter.SetOverlayUpdateOptions(updateInterval, graphHistory);
		}

		private static void ApplyRuntimeProjectSettings()
		{
			if (!RuntimePerformanceMeter.ApplySettings(PerfMeterSetupActions.LoadSettings()))
			{
				throw new InvalidOperationException("Project settings are valid, but the runtime could not apply them while cleanup or shutdown is pending.");
			}
		}

		private void SaveRuntimeTechnicalSettingsToProject()
		{
			PerfMeterSettingsSnapshot current = PerfMeterSetupActions.LoadSettings();
			PerfMeterSettingsSnapshot settings = new PerfMeterSettingsSnapshot(
				current.Enabled,
				current.AutoStart,
				current.CollectionMode,
				current.OverlayVisible,
				current.OverlayCorner,
				current.OverlayMode,
				RuntimePerformanceMeter.TargetFps,
				current.ActivePreset,
				current.OverlayModules,
				current.SessionWarmupFrames,
				current.SessionWarmupSeconds,
				current.SessionSampleIntervalSeconds,
				current.SessionMaxSamples,
				current.SessionResetOnSceneLoad,
				current.SessionSceneLoadIgnoreFrames,
				current.SessionSceneLoadIgnoreSeconds,
				current.EditorWarningCooldownSeconds,
				current.StructuredLogCooldownSeconds,
				current.CallbackCooldownSeconds,
				PerfMeterSettingsLoadState.Loaded,
				string.Empty,
				overlayScale: current.OverlayScale,
				overlayOpacity: current.OverlayOpacity,
				overlayFontSize: current.OverlayFontSize,
				overlayRefreshIntervalSeconds: _runtimeUpdateInterval != null ? _runtimeUpdateInterval.value : current.OverlayRefreshIntervalSeconds,
				overlayGraphHistoryLength: _runtimeGraphHistory != null ? _runtimeGraphHistory.value : current.OverlayGraphHistoryLength,
				overdrawDefaultFrameCount: current.OverdrawDefaultFrameCount,
				overdrawMaxFrameCount: current.OverdrawMaxFrameCount,
				alertOverdrawRatioThreshold: current.AlertOverdrawRatioThreshold,
				alertTimingConsecutiveFrames: current.AlertTimingConsecutiveFrames,
				alertFpsConsecutiveFrames: current.AlertFpsConsecutiveFrames,
				alertGpuTimingUnavailableConsecutiveFrames: current.AlertGpuTimingUnavailableConsecutiveFrames,
				alertOverdrawConsecutiveFrames: current.AlertOverdrawConsecutiveFrames,
				overlayTheme: current.OverlayTheme,
				overlayLayout: current.OverlayLayout,
				overlayFontFamily: current.OverlayFontFamily,
				editorWarningsEnabled: current.EditorWarningsEnabled,
				activeOverlayPresetId: current.ActiveOverlayPresetId,
				activeOverlayPreset: current.ActiveOverlayPreset,
				structuredLogsEnabled: current.StructuredLogsEnabled);
			PerfMeterSetupActions.SaveSettings(settings);
		}

		private void ApplyProjectTechnicalSettingsToRuntime()
		{
			PerfMeterSettingsSnapshot settings = PerfMeterSetupActions.LoadSettings();
			RuntimePerformanceMeter.SetTargetFps(settings.TargetFps);
			RuntimePerformanceMeter.SetOverlayUpdateOptions(settings.OverlayRefreshIntervalSeconds, settings.OverlayGraphHistoryLength);
			_runtimeUpdateInterval?.SetValueWithoutNotify(settings.OverlayRefreshIntervalSeconds);
			_runtimeGraphHistory?.SetValueWithoutNotify(settings.OverlayGraphHistoryLength);
		}

		private void ApplyRuntimeSelectedVisualPreset()
		{
			PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset asset = GetRuntimeSelectedOverlayPresetAsset();
			if (asset == null || !asset.IsValid || asset.Preset == null)
			{
				throw new InvalidOperationException("Select a valid visual overlay preset.");
			}

			RuntimePerformanceMeter.ApplyVisualOverlayPreset(asset.Preset.id, PerfMeterOverlayPresetUtility.Clone(asset.Preset));
		}

		private PerfMeterOverlayPresetEditorUtility.OverlayPresetAsset GetRuntimeSelectedOverlayPresetAsset()
		{
			string label = _runtimeVisualPresetField != null ? _runtimeVisualPresetField.value : string.Empty;
			for (int i = 0; i < _overlayPresetChoiceLabels.Count; i++)
			{
				if (string.Equals(_overlayPresetChoiceLabels[i], label, StringComparison.Ordinal) && i < _overlayPresetAssets.Count)
				{
					return _overlayPresetAssets[i];
				}
			}

			return null;
		}

		private void DumpRuntimeSnapshot()
		{
			Debug.Log(BuildRuntimeSummary());
		}

		private void CopyRuntimeSummary()
		{
			GUIUtility.systemCopyBuffer = BuildRuntimeSummary();
		}

		private string BuildRuntimeSummary()
		{
			PerfMeterStatusSnapshot status = RuntimePerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = RuntimePerformanceMeter.GetLatestMetrics();
			return "SGG PerfMeter Runtime: " + status.State + ", collection " + status.CollectionMode + ", overlay " + (status.OverlayVisible ? "visible" : "hidden") + ", preset " + GetRuntimePresetDisplayName(status) + ", fps " + FormatRuntimeFps(metrics) + ", cpu " + FormatRuntimeMs(metrics.CpuFrameTimeMs) + ", gpu " + FormatRuntimeMs(metrics.GpuFrameTimeMs);
		}

		private void RefreshRuntimePanel()
		{
			if (_runtimeStatus == null)
			{
				return;
			}

			bool isPlaying = EditorApplication.isPlaying;
			_runtimePlayModeInfo.text = isPlaying
				? "Runtime controls affect the currently running Play Mode session."
				: "Runtime controls are read-only in Edit Mode. Enter Play Mode to test overlay layouts, appearance, visibility, and short overdraw capture.";

			PerfMeterStatusSnapshot status = RuntimePerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = RuntimePerformanceMeter.GetLatestMetrics();
			SetRuntimeButtonsEnabled(isPlaying);
			RefreshRuntimeButtonStates(status);
			_runtimeStatus.text = status.State + " / " + status.Bottleneck;
			_runtimeCollectionMode.text = status.CollectionMode.ToString();
			_runtimeOverlayVisible.text = status.OverlayVisible ? "Visible" : "Hidden";
			_runtimeOverlayPreset.text = GetRuntimePresetDisplayName(status);
			_runtimeOverlayModules.text = status.OverlayModules.ToString();
			_runtimeTargetFps.text = FormatTargetFps(status.TargetFps) + " / " + (1000d / (int)status.TargetFps).ToString("0.00") + " ms";
			_runtimeCurrentFps.text = FormatRuntimeFps(metrics);
			_runtimeCpuFrame.text = FormatRuntimeMs(metrics.CpuFrameTimeMs);
			_runtimeGpuFrame.text = metrics.GpuFrameTimeAvailable ? FormatRuntimeMs(metrics.GpuFrameTimeMs) : "Unavailable";
			_runtimeStyleSummary.text = status.OverlayCorner + " · " + status.OverlayLayout + " · " + status.OverlayTheme + " · " + status.OverlayFontFamily;
			_runtimeEditorWarnings.text = status.EditorWarningsEnabled ? "Enabled" : "Disabled";
			_runtimeStructuredLogs.text = RuntimePerformanceMeter.StructuredLogsEnabled ? "Enabled" : "Disabled";
			_runtimeOverdraw.text = status.OverdrawState + " " + (status.OverdrawProgress * 100f).ToString("0") + "% / heatmap " + (status.OverdrawHeatmapVisible ? "on" : "off");

			PerfMeterSettingsSnapshot projectSettings = PerfMeterSetupActions.LoadSettings();
			if (_runtimeUpdateInterval != null && _runtimeUpdateInterval.value <= 0f)
			{
				_runtimeUpdateInterval.SetValueWithoutNotify(projectSettings.OverlayRefreshIntervalSeconds);
			}

			if (_runtimeGraphHistory != null && _runtimeGraphHistory.value <= 0)
			{
				_runtimeGraphHistory.SetValueWithoutNotify(projectSettings.OverlayGraphHistoryLength);
			}

			SetRuntimeVisualPresetSelection(status.VisualOverlayPresetId);
			RefreshP3IntegrationStatus(status);
		}

		private void RefreshRuntimePanelAndLocalization()
		{
			RefreshRuntimePanel();
			ApplyLocalization();
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (_runtimeStatus == null)
			{
				return;
			}

			RefreshRuntimePanel();
			ApplyLocalization();
		}

		private void RefreshP3IntegrationStatus(PerfMeterStatusSnapshot runtimeStatus)
		{
			if (_runtimeP3SessionState == null)
			{
				return;
			}

			PerfMeterSessionSummarySnapshot session = RuntimePerformanceMeter.GetSessionSummary();
			PerfMeterMemorySnapshotCapabilitiesSnapshot memoryCapabilities = RuntimePerformanceMeter.GetMemorySnapshotCapabilities();
			PerfMeterMemorySnapshotStatusSnapshot memoryStatus = RuntimePerformanceMeter.GetMemorySnapshotStatus();
			PerfMeterGraphicsStateCollectionCapabilitiesSnapshot graphicsStateCapabilities = RuntimePerformanceMeter.GetGraphicsStateCollectionCapabilities();
			PerfMeterGraphicsStateCollectionStatusSnapshot graphicsStateStatus = RuntimePerformanceMeter.GetGraphicsStateCollectionStatus();
			PerfMeterGraphicsDiagnosticsSnapshot graphicsDiagnostics = RuntimePerformanceMeter.GetGraphicsDiagnostics();
			PerfMeterRenderIntegrationSnapshot renderIntegration = RuntimePerformanceMeter.GetRenderIntegrationSnapshot();
			PerfMeterGpuResidentDrawerContextSnapshot grd = renderIntegration.GpuResidentDrawer;

			_runtimeP3SessionState.text = session.State.ToString();
			_runtimeP3SessionId.text = string.IsNullOrEmpty(session.SessionId) ? "None" : session.SessionId;
			_runtimeP3SessionSamples.text = session.State == PerfMeterSessionState.Idle
				? "No session"
				: session.SampleCount + " retained / " + session.DroppedSampleCount + " dropped";

			_runtimeP3MemoryCapabilities.text = memoryCapabilities.Availability + " / backend " + FormatOptionalValue(memoryCapabilities.BackendId) + " / flags " + memoryCapabilities.SupportedCaptureFlags + " / max " + FormatBytes(memoryCapabilities.MaxSnapshotBytes);
			_runtimeP3MemoryStatus.text = FormatMemorySnapshotStatus(memoryStatus);

			_runtimeP3GraphicsStateCapabilities.text = FormatGraphicsStateCollectionCapabilities(graphicsStateCapabilities);
			_runtimeP3GraphicsStateStatus.text = FormatGraphicsStateCollectionStatus(graphicsStateStatus);

			_runtimeP3GraphicsMarkers.text = FormatGraphicsDiagnostics(graphicsDiagnostics);
			_runtimeP3RenderIntegration.text = FormatRenderIntegration(renderIntegration);
			_runtimeP3Grd.text = FormatGrdTelemetry(grd);

			List<string> warnings = new List<string>();
			AddWarning(warnings, runtimeStatus.Warning);
			AddWarning(warnings, runtimeStatus.LastError);
			AddWarning(warnings, session.Warning);
			AddWarning(warnings, memoryCapabilities.Warning);
			AddWarning(warnings, memoryStatus.Warning);
			AddWarning(warnings, graphicsStateCapabilities.Warning);
			AddWarning(warnings, graphicsStateStatus.Warning);
			AddWarning(warnings, graphicsDiagnostics.Warning);
			AddWarning(warnings, renderIntegration.Warning);
			AddWarning(warnings, grd.Warning);
			AddWarning(warnings, renderIntegration.VariableRateShading.Warning);
			_runtimeP3Warnings.text = warnings.Count == 0 ? "None" : string.Join(" | ", warnings.ToArray());
		}

		private void OpenSessionAnalysis()
		{
			PerfMeterSessionAnalysisWindow.Open();
			_lastActionLabel.text = "Session Analysis: opened.";
			ApplyLocalization();
		}

		private void OpenProfileAnalyzer()
		{
			bool opened = PerfMeterProfileAnalyzerIntegration.TryOpenProfileAnalyzerForCurrentSession();
			_lastActionLabel.text = opened
				? "Profile Analyzer: opened; session ID copied to clipboard."
				: "Profile Analyzer: unavailable; see Console warning.";
			ApplyLocalization();
		}

		private static string FormatOptionalValue(string value)
		{
			return string.IsNullOrEmpty(value) ? "None" : value;
		}

		internal static string FormatMemorySnapshotStatus(PerfMeterMemorySnapshotStatusSnapshot status)
		{
			string text = status.State + " / " + status.Availability;
			if (status.Availability != PerfMeterAvailability.Available)
			{
				return text + " / capture unavailable / cooldown unavailable";
			}

			text += " / capture " + FormatOptionalValue(status.CaptureId);
			text += status.IsActive || status.CooldownRemainingSeconds > 0d
				? " / cooldown " + status.CooldownRemainingSeconds.ToString("0.0") + " s"
				: " / cooldown none";
			return text;
		}

		internal static string FormatGraphicsStateCollectionStatus(PerfMeterGraphicsStateCollectionStatusSnapshot status)
		{
			string text = status.State + " / " + status.Availability;
			if (status.Availability != PerfMeterAvailability.Available)
			{
				return text + " / trace progress unavailable";
			}

			return text + " / " + status.CompletedTraceFrames + "/" + status.RequestedTraceFrames + " trace frames" + (status.IsBusy ? " / busy" : string.Empty);
		}

		internal static string FormatGraphicsStateCollectionCapabilities(PerfMeterGraphicsStateCollectionCapabilitiesSnapshot capabilities)
		{
			string text = capabilities.Availability + " / backend " + FormatOptionalValue(capabilities.BackendId);
			if (capabilities.Availability != PerfMeterAvailability.Available)
			{
				return text + " / trace unavailable / prewarm unavailable";
			}

			return text + " / trace " + (capabilities.SupportsTrace ? "yes" : "no") + " / prewarm " + (capabilities.SupportsPrewarm ? "yes" : "no");
		}

		internal static string FormatGraphicsDiagnostics(PerfMeterGraphicsDiagnosticsSnapshot diagnostics)
		{
			return diagnostics.Availability + " / shader creation " + FormatProfilerMetric(diagnostics.ShaderGpuProgramCreationCapability, diagnostics.ShaderGpuProgramCreationValue) + " / pipeline creation " + FormatProfilerMetric(diagnostics.GraphicsPipelineCreationCapability, diagnostics.GraphicsPipelineCreationValue) + " / device " + FormatOptionalValue(diagnostics.GraphicsDeviceName);
		}

		internal static string FormatRenderIntegration(PerfMeterRenderIntegrationSnapshot integration)
		{
			if (integration.Availability == PerfMeterAvailability.Unavailable)
			{
				return integration.State + " / Unavailable / integration unavailable / passes unavailable / mode unavailable";
			}

			if (integration.Availability != PerfMeterAvailability.Available)
			{
				return integration.State + " / " + integration.Availability + " / integration unknown / passes unavailable / mode unavailable";
			}

			if (integration.State != PerfMeterRenderIntegrationState.Observed)
			{
				return integration.State + " / Available / pipeline " + integration.RenderPipeline.Kind + " / no observation yet / passes unavailable / mode unavailable";
			}

			return integration.State + " / " + integration.Availability + " / " + FormatOptionalValue(integration.IntegrationName) + " / passes " + integration.PerfMeterPassCount + " / mode " + FormatOptionalValue(integration.EffectiveRenderingMode);
		}

		internal static string FormatGrdTelemetry(PerfMeterGpuResidentDrawerContextSnapshot grd)
		{
			string support = FormatAvailabilityBoolean(grd.SupportAvailability, grd.IsSupported, "supported", "unsupported");
			string activity = FormatAvailabilityBoolean(grd.ActivityAvailability, grd.IsObservedActive, "active", "inactive");
			return "support " + support + " / activity " + activity + " / BRG draws " + FormatProfilerMetric(grd.Effectiveness.BrgDrawCallsCapability, grd.Effectiveness.BrgDrawCalls) + " / instances " + FormatProfilerMetric(grd.Effectiveness.BrgInstancesCapability, grd.Effectiveness.BrgInstances) + " / reason " + grd.DegradedReason;
		}

		private static string FormatAvailabilityBoolean(PerfMeterAvailability availability, bool value, string availableText, string unavailableText)
		{
			switch (availability)
			{
				case PerfMeterAvailability.Available:
					return value ? availableText : unavailableText;
				case PerfMeterAvailability.Unavailable:
					return "unavailable";
				default:
					return "unknown";
			}
		}

		private static string FormatPresetTags(string[] tags)
		{
			if (tags == null || tags.Length == 0)
			{
				return "None";
			}

			List<string> values = new List<string>();
			for (int i = 0; i < tags.Length; i++)
			{
				if (!string.IsNullOrWhiteSpace(tags[i]))
				{
					values.Add(tags[i].Trim());
				}
			}

			return values.Count == 0 ? "None" : string.Join(", ", values.ToArray());
		}

		private static string FormatReservedWidgetMetadata(PerfMeterOverlayPresetWidgetJson[] widgets)
		{
			int variantCount = 0;
			int heightCount = 0;
			PerfMeterOverlayPresetWidgetJson[] source = widgets ?? Array.Empty<PerfMeterOverlayPresetWidgetJson>();
			for (int index = 0; index < source.Length; index++)
			{
				PerfMeterOverlayPresetWidgetJson widget = source[index];
				if (widget == null)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(widget.variant))
				{
					variantCount++;
				}

				if (widget.height > 0)
				{
					heightCount++;
				}
			}

			return "Reserved, read-only: variant set on " + variantCount + " widget(s), height set on " + heightCount + " widget(s).";
		}

		private static string FormatBytes(long bytes)
		{
			return bytes > 0L ? bytes.ToString("N0") + " bytes" : "Unavailable";
		}

		internal static string FormatProfilerMetric(PerfMeterProfilerMetricCapabilitySnapshot capability, long value)
		{
			return capability.SampleState == PerfMeterProfilerMetricSampleState.AvailableSampled ? value.ToString() : capability.SampleState.ToString();
		}

		private static void AddWarning(List<string> warnings, string warning)
		{
			if (!string.IsNullOrWhiteSpace(warning) && !warnings.Contains(warning))
			{
				warnings.Add(warning);
			}
		}

		private void SetRuntimeVisualPresetSelection(string presetId)
		{
			if (_runtimeVisualPresetField == null || _overlayPresetAssets.Count == 0)
			{
				return;
			}

			int index = FindOverlayPresetIndex(presetId);
			if (index < 0)
			{
				index = FindFirstValidOverlayPresetIndex();
			}

			if (index >= 0 && index < _overlayPresetChoiceLabels.Count)
			{
				_runtimeVisualPresetField.SetValueWithoutNotify(_overlayPresetChoiceLabels[index]);
			}
		}

		private static string GetRuntimePresetDisplayName(PerfMeterStatusSnapshot status)
		{
			return string.IsNullOrEmpty(status.VisualOverlayPresetId) ? status.OverlayPreset.ToString() : status.VisualOverlayPresetId;
		}

		private static string FormatRuntimeFps(PerfMeterMetricsSnapshot metrics)
		{
			if (metrics.CpuFrameTimeMs <= 0d)
			{
				return "Unavailable";
			}

			return (1000d / metrics.CpuFrameTimeMs).ToString("0.0");
		}

		private static string FormatRuntimeMs(double ms)
		{
			return ms > 0d ? ms.ToString("0.00") + " ms" : "Unavailable";
		}

		private void SetRuntimeButtonsEnabled(bool enabled)
		{
			for (int i = 0; i < _runtimeButtons.Count; i++)
			{
				_runtimeButtons[i].SetEnabled(enabled);
			}
		}

		private void RefreshRuntimeButtonStates(PerfMeterStatusSnapshot status)
		{
			for (int i = 0; i < _runtimeButtonBindings.Count; i++)
			{
				RuntimeButtonBinding binding = _runtimeButtonBindings[i];
				bool active = false;
				try
				{
					active = binding.ActiveWhen(status);
				}
				catch (Exception)
				{
					active = false;
				}

				SetButtonActive(binding.Button, active);
			}
		}

		private void RefreshSettingsButtonStates()
		{
			for (int i = 0; i < _settingsButtonBindings.Count; i++)
			{
				SettingsButtonBinding binding = _settingsButtonBindings[i];
				bool active = false;
				try
				{
					active = binding.ActiveWhen();
				}
				catch (Exception)
				{
					active = false;
				}

				SetButtonActive(binding.Button, active);
			}
		}

		private void RefreshWorkModeButtonStates()
		{
			RefreshWorkModeToggle(_settingsEnabled, "Enabled");
			RefreshWorkModeToggle(_settingsAutoStart, "Auto Start");
			RefreshWorkModeToggle(_settingsCollectMetrics, "Collect Metrics");
			RefreshWorkModeToggle(_settingsOverlayVisible, "Show Overlay");
			RefreshWorkModeToggle(_settingsOverdrawDiagnostics, "Overdraw diagnostics / heatmap");
		}

		private static void RefreshWorkModeToggle(Toggle toggle, string label)
		{
			if (toggle == null)
			{
				return;
			}

			toggle.text = PerfMeterWindowLocalization.Text(label);
			if (toggle.value)
			{
				toggle.AddToClassList("pm-button--active");
				toggle.RemoveFromClassList("pm-workmode-toggle--off");
			}
			else
			{
				toggle.RemoveFromClassList("pm-button--active");
				toggle.AddToClassList("pm-workmode-toggle--off");
			}
		}

		private static void SetButtonActive(Button button, bool active)
		{
			if (button == null)
			{
				return;
			}

			if (active)
			{
				button.AddToClassList("pm-button--active");
			}
			else
			{
				button.RemoveFromClassList("pm-button--active");
			}
		}

		private static string FormatTargetFps(PerfMeterTargetFps targetFps)
		{
			return ((int)targetFps).ToString() + " FPS";
		}

		private static string FormatEnumLabel(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			string result = value[0].ToString();
			for (int i = 1; i < value.Length; i++)
			{
				char character = value[i];
				if (char.IsUpper(character) && !char.IsUpper(value[i - 1]))
				{
					result += " ";
				}

				result += character;
			}

			return result;
		}

		private static string FormatOverlayFontLabel(PerfMeterOverlayFontFamily fontFamily)
		{
			switch (fontFamily)
			{
				case PerfMeterOverlayFontFamily.JetBrainsMono:
					return "JetBrains Mono";
				case PerfMeterOverlayFontFamily.LegacyRuntime:
					return "Legacy Runtime";
				default:
					return fontFamily.ToString();
			}
		}

		private static void SetChecklist(Label label, string state, string text)
		{
			if (label == null)
			{
				return;
			}

			label.text = text ?? string.Empty;
			SetChecklistState(label, state);
		}

		private static void SetChecklistState(Label label, string state)
		{
			VisualElement field = label.parent;
			RemoveChecklistState(label);
			if (field != null)
			{
				RemoveChecklistState(field);
				field.AddToClassList("pm-checklist--" + state);
			}

			Label icon = field == null ? null : field.Q<Label>(className: "pm-checklist-icon");
			if (icon != null)
			{
				icon.text = ChecklistIcon(state);
				icon.tooltip = ChecklistTooltip(state);
				RemoveChecklistState(icon);
				icon.AddToClassList("pm-checklist-icon--" + state);
			}
		}

		private static void RemoveChecklistState(VisualElement element)
		{
			element.RemoveFromClassList("pm-checklist--ok");
			element.RemoveFromClassList("pm-checklist--warn");
			element.RemoveFromClassList("pm-checklist--error");
			element.RemoveFromClassList("pm-checklist--optional");
			element.RemoveFromClassList("pm-checklist-icon--ok");
			element.RemoveFromClassList("pm-checklist-icon--warn");
			element.RemoveFromClassList("pm-checklist-icon--error");
			element.RemoveFromClassList("pm-checklist-icon--optional");
		}

		private static string ChecklistIcon(string state)
		{
			switch (state)
			{
				case "ok":
					return "✔";
				case "warn":
					return "▲";
				case "optional":
					return "◇";
				default:
					return "●";
			}
		}

		private static string ChecklistTooltip(string state)
		{
			switch (state)
			{
				case "ok":
					return "Active / success";
				case "warn":
					return "Warning / attention";
				case "optional":
					return "Optional";
				default:
					return "Error / danger";
			}
		}

		private static void SetIndicator(VisualElement indicator, bool ok, bool warn)
		{
			if (indicator == null)
			{
				return;
			}

			Label indicatorLabel = indicator as Label;
			if (indicatorLabel != null)
			{
				indicatorLabel.text = ok ? "✔" : warn ? "▲" : "●";
			}

			indicator.RemoveFromClassList("pm-indicator--ok");
			indicator.RemoveFromClassList("pm-indicator--warn");
			indicator.RemoveFromClassList("pm-indicator--error");
			if (ok)
			{
				indicator.AddToClassList("pm-indicator--ok");
			}
			else if (warn)
			{
				indicator.AddToClassList("pm-indicator--warn");
			}
			else
			{
				indicator.AddToClassList("pm-indicator--error");
			}
		}

		private readonly struct DebugWidgetInfo
		{
			internal DebugWidgetInfo(string source, string name, string type, string module, string details, bool preserveIdentity)
			{
				Source = source;
				Name = name;
				Type = type;
				Module = module;
				Details = details;
				PreserveIdentity = preserveIdentity;
			}

			internal string Source { get; }
			internal string Name { get; }
			internal string Type { get; }
			internal string Module { get; }
			internal string Details { get; }
			internal bool PreserveIdentity { get; }
		}

		private readonly struct OverlayPresetWidgetBinding
		{
			internal OverlayPresetWidgetBinding(string widgetId, Toggle toggle)
			{
				WidgetId = widgetId ?? string.Empty;
				Toggle = toggle;
			}

			internal string WidgetId { get; }
			internal Toggle Toggle { get; }
		}

		private readonly struct RuntimeButtonBinding
		{
			internal RuntimeButtonBinding(Button button, Func<PerfMeterStatusSnapshot, bool> activeWhen)
			{
				Button = button;
				ActiveWhen = activeWhen;
			}

			internal Button Button { get; }
			internal Func<PerfMeterStatusSnapshot, bool> ActiveWhen { get; }
		}

		private readonly struct SettingsButtonBinding
		{
			internal SettingsButtonBinding(Button button, Func<bool> activeWhen)
			{
				Button = button;
				ActiveWhen = activeWhen;
			}

			internal Button Button { get; }
			internal Func<bool> ActiveWhen { get; }
		}
	}
}
