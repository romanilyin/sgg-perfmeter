using System;
using System.Collections.Generic;
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
		private readonly List<OverlayModuleToggle> _settingsModuleToggles = new List<OverlayModuleToggle>();
		private readonly HashSet<string> _selectedRendererPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private VisualElement _setupPanel;
		private VisualElement _presetsPanel;
		private VisualElement _runtimePanel;
		private VisualElement _settingsPanel;
		private ToolbarToggle _setupTab;
		private ToolbarToggle _presetsTab;
		private ToolbarToggle _runtimeTab;
		private ToolbarToggle _settingsTab;
		private string _currentTab = "Setup";

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
		private Toggle _initOverlayVisible;
		private EnumField _initTargetFps;
		private EnumField _initOverlayCorner;
		private EnumField _initOverlayTheme;
		private EnumField _initOverlayLayout;
		private EnumField _initOverlayFontFamily;
		private Label _settingsStatus;
		private Label _settingsPath;
		private Label _settingsResourcesPath;
		private Toggle _settingsEnabled;
		private Toggle _settingsAutoStart;
		private EnumField _settingsCollectionMode;
		private Toggle _settingsOverlayVisible;
		private EnumField _settingsTargetFps;
		private EnumField _settingsOverlayCorner;
		private EnumField _settingsOverlayTheme;
		private EnumField _settingsOverlayLayout;
		private EnumField _settingsOverlayFontFamily;
		private EnumField _settingsActivePreset;
		private FloatField _settingsOverlayScale;
		private FloatField _settingsOverlayOpacity;
		private FloatField _settingsOverlayFontSize;
		private FloatField _settingsOverlayRefreshInterval;
		private IntegerField _settingsOverlayGraphHistory;
		private Toggle _settingsEditorWarningsEnabled;
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
		private Label _lastActionLabel;
		private Label _runtimePlayModeInfo;
		private Label _runtimeStatus;
		private Label _runtimeCollectionMode;
		private Label _runtimeOverlayVisible;
		private Label _runtimeOverlayPreset;
		private Label _runtimeOverlayModules;
		private Label _runtimeTargetFps;
		private Label _runtimeOverlayCorner;
		private Label _runtimeOverlayTheme;
		private Label _runtimeOverlayLayout;
		private Label _runtimeOverlayFontFamily;
		private Label _runtimeEditorWarnings;
		private Label _runtimeOverdraw;
		private PopupField<string> _languageField;
		private Label _settingsLanguageCurrent;

		[MenuItem("SGG/Perfmeter/Setup")]
		public static void Open()
		{
			PerfMeterSetupWindow window = GetWindow<PerfMeterSetupWindow>("SGG PerfMeter");
			window.minSize = new Vector2(480f, 360f);
			window.Show();
		}

		public void CreateGUI()
		{
			rootVisualElement.Clear();
			_runtimeButtons.Clear();
			_runtimeButtonBindings.Clear();
			_settingsButtons.Clear();
			_settingsButtonBindings.Clear();
			_settingsModuleToggles.Clear();
			rootVisualElement.AddToClassList("pm-window");

			string stylePath = PerfMeterSetupUtility.PackageAssetPath + "/Editor/UI/PerfMeterSetupWindow.uss";
			StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
			if (style != null)
			{
				rootVisualElement.styleSheets.Add(style);
			}

			Label title = new Label("SGG PerfMeter Setup");
			title.AddToClassList("pm-title");
			rootVisualElement.Add(title);

			BuildTabs();

			ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.flexGrow = 1f;
			rootVisualElement.Add(scroll);

			_setupPanel = new VisualElement();
			_presetsPanel = new VisualElement();
			_runtimePanel = new VisualElement();
			_settingsPanel = new VisualElement();
			scroll.Add(_setupPanel);
			scroll.Add(_presetsPanel);
			scroll.Add(_runtimePanel);
			scroll.Add(_settingsPanel);

			BuildSetupPanel();
			BuildPresetsPanel();
			BuildRuntimePanel();
			BuildSettingsPanel();

			_lastActionLabel = new Label();
			_lastActionLabel.AddToClassList("pm-log");
			rootVisualElement.Add(_lastActionLabel);

			SelectCurrentTab();
			RefreshAll();
		}

		private void BuildTabs()
		{
			Toolbar toolbar = new Toolbar();
			toolbar.AddToClassList("pm-tabs");
			_setupTab = new ToolbarToggle { text = "Setup" };
			_presetsTab = new ToolbarToggle { text = "Presets" };
			_runtimeTab = new ToolbarToggle { text = "Runtime" };
			_settingsTab = new ToolbarToggle { text = "Settings" };
			_setupTab.AddToClassList("pm-tab");
			_presetsTab.AddToClassList("pm-tab");
			_runtimeTab.AddToClassList("pm-tab");
			_settingsTab.AddToClassList("pm-tab");
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
			_settingsTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectSettingsTab();
				}
			});
			toolbar.Add(_setupTab);
			toolbar.Add(_presetsTab);
			toolbar.Add(_runtimeTab);
			toolbar.Add(_settingsTab);
			rootVisualElement.Add(toolbar);
		}

		private void BuildSetupPanel()
		{
			BuildProjectSection(_setupPanel);
			BuildRendererSection(_setupPanel);
			BuildInitializationSection(_setupPanel);
		}

		private void BuildProjectSection(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Project Settings");
			_projectIndicator = AddStatusLine(section, out _projectStatus);
			_frameTimingStats = AddRow(section, "Frame Timing Stats");
			_packagePath = AddRow(section, "Package Path");

			VisualElement actions = AddActions(section);
			AddButton(actions, "Enable Frame Timing", () => RunAction("Enable Frame Timing", PerfMeterSetupActions.EnableFrameTimingStats));
			AddButton(actions, "Refresh", RefreshAll);
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
			AddInfo(section, "Manual bootstrap code remains available for projects that do not want JSON zero-code setup. Use the Presets tab to save project-owned JSON settings that auto-start PerfMeter without writing code.");

			_initOverlayVisible = new Toggle { value = true };
			_initOverlayVisible.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Visible", _initOverlayVisible);

			_initTargetFps = new EnumField(PerfMeterTargetFps.Fps60);
			_initTargetFps.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Target FPS", _initTargetFps);

			_initOverlayCorner = new EnumField(PerfMeterOverlayCorner.TopRight);
			_initOverlayCorner.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Corner", _initOverlayCorner);

			_initOverlayTheme = new EnumField(PerfMeterOverlayTheme.ClassicDark);
			_initOverlayTheme.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Theme", _initOverlayTheme);

			_initOverlayLayout = new EnumField(PerfMeterOverlayLayout.MetricBars);
			_initOverlayLayout.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Layout", _initOverlayLayout);

			_initOverlayFontFamily = new EnumField(PerfMeterOverlayFontFamily.Manrope);
			_initOverlayFontFamily.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Font", _initOverlayFontFamily);

			_initCode = new TextField
			{
				multiline = true,
				isReadOnly = true,
				value = BuildInitializationSnippetFromOptions()
			};
			_initCode.AddToClassList("pm-code");
			section.Add(_initCode);

			VisualElement actions = AddActions(section);
			AddButton(actions, "Copy Init Code", CopyInitializationCode);
		}

		private void BuildPresetsPanel()
		{
			VisualElement section = AddSection(_presetsPanel, "JSON Settings and Zero-Code Setup");
			AddInfo(section, "Settings are stored as project-owned JSON under Assets/Resources and loaded at runtime through Resources. ScriptableObject settings are intentionally not used.");

			_settingsStatus = AddRow(section, "Status");
			_settingsPath = AddRow(section, "JSON Path");
			_settingsResourcesPath = AddRow(section, "Resources Path");

			_settingsEnabled = new Toggle();
			AddControlRow(section, "Enabled", _settingsEnabled);

			_settingsAutoStart = new Toggle();
			AddControlRow(section, "Auto Start", _settingsAutoStart);

			_settingsOverlayVisible = new Toggle();
			AddControlRow(section, "Overlay Visible", _settingsOverlayVisible);

			_settingsCollectionMode = new EnumField(PerfMeterCollectionMode.Overlay);
			_settingsActivePreset = new EnumField(PerfMeterOverlayPreset.FullDiagnostics);
			_settingsActivePreset.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue is PerfMeterOverlayPreset preset)
				{
					ApplyPresetDefaultsToSettingsControls(preset);
				}
			});
			_settingsTargetFps = new EnumField(PerfMeterTargetFps.Fps60);
			_settingsOverlayCorner = new EnumField(PerfMeterOverlayCorner.TopRight);
			_settingsOverlayTheme = new EnumField(PerfMeterOverlayTheme.ClassicDark);
			_settingsOverlayLayout = new EnumField(PerfMeterOverlayLayout.MetricBars);
			_settingsOverlayFontFamily = new EnumField(PerfMeterOverlayFontFamily.Manrope);

			VisualElement collectionActions = AddChoiceGroup(section, "Collection Mode");
			AddSettingsCollectionModeButton(collectionActions, PerfMeterCollectionMode.Background);
			AddSettingsCollectionModeButton(collectionActions, PerfMeterCollectionMode.Overlay);
			AddSettingsCollectionModeButton(collectionActions, PerfMeterCollectionMode.OverdrawDiagnostic);
			AddSettingsCollectionModeButton(collectionActions, PerfMeterCollectionMode.Stopped);

			VisualElement presetActions = AddChoiceGroup(section, "Active Preset");
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Minimal);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Timing);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Rendering);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Memory);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Overdraw);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.FullDiagnostics);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.AgentDebug);
			AddSettingsPresetButton(presetActions, PerfMeterOverlayPreset.Custom);

			VisualElement targetActions = AddChoiceGroup(section, "Target FPS");
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps15);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps30);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps60);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps90);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps120);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps144);
			AddSettingsTargetFpsButton(targetActions, PerfMeterTargetFps.Fps240);

			VisualElement cornerActions = AddChoiceGroup(section, "Overlay Corner");
			AddSettingsCornerButton(cornerActions, PerfMeterOverlayCorner.TopLeft);
			AddSettingsCornerButton(cornerActions, PerfMeterOverlayCorner.TopRight);
			AddSettingsCornerButton(cornerActions, PerfMeterOverlayCorner.BottomLeft);
			AddSettingsCornerButton(cornerActions, PerfMeterOverlayCorner.BottomRight);

			VisualElement layoutActions = AddChoiceGroup(section, "Overlay Layout");
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.FpsOnly);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.TextCompact);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.Graphs);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.Classic);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.CompactCards);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.DiagnosticsWide);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.OverdrawFocus);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.MetricBars);
			AddSettingsLayoutButton(layoutActions, PerfMeterOverlayLayout.Custom);

			VisualElement themeActions = AddChoiceGroup(section, "Overlay Theme");
			AddSettingsThemeButton(themeActions, PerfMeterOverlayTheme.ClassicDark);
			AddSettingsThemeButton(themeActions, PerfMeterOverlayTheme.Glass);
			AddSettingsThemeButton(themeActions, PerfMeterOverlayTheme.Cyber);
			AddSettingsThemeButton(themeActions, PerfMeterOverlayTheme.HighContrast);

			VisualElement fontActions = AddChoiceGroup(section, "Overlay Font");
			AddSettingsFontButton(fontActions, PerfMeterOverlayFontFamily.Manrope);
			AddSettingsFontButton(fontActions, PerfMeterOverlayFontFamily.JetBrainsMono);
			AddSettingsFontButton(fontActions, PerfMeterOverlayFontFamily.LegacyRuntime);

			VisualElement moduleList = new VisualElement();
			moduleList.AddToClassList("pm-module-list");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Fps, "FPS");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Timing, "CPU/GPU timings");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Graphs, "Graphs");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Rendering, "Rendering counters");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.SrpBatcher, "SRP Batcher");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Brg, "BRG/GRD");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Uploads, "Index uploads");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Memory, "System memory");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Gc, "GC memory");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.GpuMemory, "GPU memory");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Overdraw, "Overdraw ratio");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Heatmap, "Heatmap state");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.Warnings, "Warnings");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.CustomMetrics, "Custom metrics");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.CpuCores, "CPU core rows");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.CpuCoreBars, "CPU core % bars");
			AddModuleToggle(moduleList, PerfMeterOverlayModule.CpuCoreGraphs, "CPU core graphs");
			AddControlRow(section, "Modules", moduleList);

			_settingsOverlayScale = new FloatField();
			AddControlRow(section, "Overlay Scale", _settingsOverlayScale);

			_settingsOverlayOpacity = new FloatField();
			AddControlRow(section, "Overlay Opacity", _settingsOverlayOpacity);

			_settingsOverlayFontSize = new FloatField();
			AddControlRow(section, "Overlay Font Size", _settingsOverlayFontSize);

			_settingsOverlayRefreshInterval = new FloatField();
			AddControlRow(section, "Refresh Interval", _settingsOverlayRefreshInterval);

			_settingsOverlayGraphHistory = new IntegerField();
			AddControlRow(section, "Graph History", _settingsOverlayGraphHistory);


			_settingsEditorWarningsEnabled = new Toggle();
			AddControlRow(section, "Enable Editor Warning Logs", _settingsEditorWarningsEnabled);

			_settingsEditorWarningCooldown = new FloatField();
			AddControlRow(section, "Editor Warning Cooldown", _settingsEditorWarningCooldown);

			_settingsStructuredLogCooldown = new FloatField();
			AddControlRow(section, "Structured Log Cooldown", _settingsStructuredLogCooldown);

			_settingsCallbackCooldown = new FloatField();
			AddControlRow(section, "Callback Cooldown", _settingsCallbackCooldown);

			_settingsAlertOverdrawThreshold = new FloatField();
			AddControlRow(section, "Overdraw Alert Ratio", _settingsAlertOverdrawThreshold);

			_settingsAlertTimingFrames = new IntegerField();
			AddControlRow(section, "Timing Alert Frames", _settingsAlertTimingFrames);

			_settingsAlertFpsFrames = new IntegerField();
			AddControlRow(section, "FPS Alert Frames", _settingsAlertFpsFrames);

			_settingsAlertGpuTimingUnavailableFrames = new IntegerField();
			AddControlRow(section, "GPU Timing Alert Frames", _settingsAlertGpuTimingUnavailableFrames);

			_settingsAlertOverdrawFrames = new IntegerField();
			AddControlRow(section, "Overdraw Alert Frames", _settingsAlertOverdrawFrames);

			_settingsSessionWarmupFrames = new IntegerField();
			AddControlRow(section, "Session Warmup Frames", _settingsSessionWarmupFrames);

			_settingsSessionWarmupSeconds = new FloatField();
			AddControlRow(section, "Session Warmup Seconds", _settingsSessionWarmupSeconds);

			_settingsSessionSampleInterval = new FloatField();
			AddControlRow(section, "Session Sample Interval", _settingsSessionSampleInterval);

			_settingsSessionMaxSamples = new IntegerField();
			AddControlRow(section, "Session Max Samples", _settingsSessionMaxSamples);

			_settingsSessionResetOnSceneLoad = new Toggle();
			AddControlRow(section, "Reset On Scene Load", _settingsSessionResetOnSceneLoad);

			_settingsSessionSceneLoadIgnoreFrames = new IntegerField();
			AddControlRow(section, "Scene Ignore Frames", _settingsSessionSceneLoadIgnoreFrames);

			_settingsSessionSceneLoadIgnoreSeconds = new FloatField();
			AddControlRow(section, "Scene Ignore Seconds", _settingsSessionSceneLoadIgnoreSeconds);

			_settingsOverdrawDefaultFrameCount = new IntegerField();
			AddControlRow(section, "Overdraw Default Frames", _settingsOverdrawDefaultFrameCount);

			_settingsOverdrawMaxFrameCount = new IntegerField();
			AddControlRow(section, "Overdraw Max Frames", _settingsOverdrawMaxFrameCount);

			VisualElement actions = AddActions(section);
			AddButton(actions, "Create Defaults", () => RunAction("Create Defaults", PerfMeterSetupActions.CreateDefaultSettings));
			AddButton(actions, "Load JSON", () =>
			{
				LoadSettingsIntoControls(PerfMeterSetupActions.LoadSettings());
				_lastActionLabel.text = PerfMeterWindowLocalization.Text("Load JSON: settings loaded from project JSON or defaults.");
			});
			AddButton(actions, "Save JSON", () => RunAction("Save JSON", SaveSettingsFromControls));
			AddButton(actions, "Apply In Play Mode", () => RunAction("Apply Settings", PerfMeterSetupActions.ApplySettingsToRuntime));
		}

		private void BuildRuntimePanel()
		{
			VisualElement statusSection = AddSection(_runtimePanel, "Runtime Status");
			_runtimePlayModeInfo = new Label();
			_runtimePlayModeInfo.AddToClassList("pm-info");
			statusSection.Add(_runtimePlayModeInfo);
			_runtimeStatus = AddRow(statusSection, "State");
			_runtimeCollectionMode = AddRow(statusSection, "Collection Mode");
			_runtimeOverlayVisible = AddRow(statusSection, "Overlay Visible");
			_runtimeOverlayPreset = AddRow(statusSection, "Overlay Preset");
			_runtimeOverlayModules = AddRow(statusSection, "Overlay Modules");
			_runtimeTargetFps = AddRow(statusSection, "Target FPS");
			_runtimeOverlayCorner = AddRow(statusSection, "Overlay Corner");
			_runtimeOverlayTheme = AddRow(statusSection, "Overlay Theme");
			_runtimeOverlayLayout = AddRow(statusSection, "Overlay Layout");
			_runtimeOverlayFontFamily = AddRow(statusSection, "Overlay Font");
			_runtimeEditorWarnings = AddRow(statusSection, "Editor Warning Logs");
			_runtimeOverdraw = AddRow(statusSection, "Overdraw");

			VisualElement lifecycleActions = AddActions(statusSection);
			AddRuntimeButton(lifecycleActions, "Ensure Runtime", () => RunRuntimeAction("Ensure Runtime", RuntimePerformanceMeter.EnsureRunning), status => status.State == PerfMeterRuntimeState.Running);

			VisualElement collectionSection = AddSection(_runtimePanel, "Collection");
			VisualElement collectionActions = AddActions(collectionSection);
			AddRuntimeButton(collectionActions, "Background Mode", () => RunRuntimeAction("Background Mode", () => RuntimePerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Background)), status => status.CollectionMode == PerfMeterCollectionMode.Background);
			AddRuntimeButton(collectionActions, "Overlay Collection", () => RunRuntimeAction("Overlay Collection", () => RuntimePerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.Overlay)), status => status.CollectionMode == PerfMeterCollectionMode.Overlay);
			AddRuntimeButton(collectionActions, "Overdraw Diagnostic", () => RunRuntimeAction("Overdraw Diagnostic", () => RuntimePerformanceMeter.SetCollectionMode(PerfMeterCollectionMode.OverdrawDiagnostic)), status => status.CollectionMode == PerfMeterCollectionMode.OverdrawDiagnostic);

			VisualElement visibilitySection = AddSection(_runtimePanel, "Overlay Visibility");
			VisualElement visibilityActions = AddActions(visibilitySection);
			AddRuntimeButton(visibilityActions, "Show Overlay", () => RunRuntimeAction("Show Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(true)), status => status.OverlayVisible);
			AddRuntimeButton(visibilityActions, "Hide Overlay", () => RunRuntimeAction("Hide Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(false)), status => !status.OverlayVisible);

			VisualElement targetSection = AddSection(_runtimePanel, "Target FPS");
			VisualElement targetActions = AddActions(targetSection);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps15);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps30);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps60);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps90);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps120);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps144);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps240);

			VisualElement layoutSection = AddSection(_runtimePanel, "Overlay Layout");
			VisualElement layoutActions = AddActions(layoutSection);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.FpsOnly);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.TextCompact);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.Graphs);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.Classic);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.CompactCards);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.DiagnosticsWide);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.OverdrawFocus);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.MetricBars);
			AddOverlayLayoutButton(layoutActions, PerfMeterOverlayLayout.Custom);

			VisualElement themeSection = AddSection(_runtimePanel, "Overlay Theme");
			VisualElement themeActions = AddActions(themeSection);
			AddOverlayThemeButton(themeActions, PerfMeterOverlayTheme.ClassicDark);
			AddOverlayThemeButton(themeActions, PerfMeterOverlayTheme.Glass);
			AddOverlayThemeButton(themeActions, PerfMeterOverlayTheme.Cyber);
			AddOverlayThemeButton(themeActions, PerfMeterOverlayTheme.HighContrast);

			VisualElement fontSection = AddSection(_runtimePanel, "Overlay Font");
			VisualElement fontActions = AddActions(fontSection);
			AddOverlayFontButton(fontActions, PerfMeterOverlayFontFamily.Manrope);
			AddOverlayFontButton(fontActions, PerfMeterOverlayFontFamily.JetBrainsMono);
			AddOverlayFontButton(fontActions, PerfMeterOverlayFontFamily.LegacyRuntime);

			VisualElement cornerSection = AddSection(_runtimePanel, "Overlay Corner");
			VisualElement cornerActions = AddActions(cornerSection);
			AddRuntimeButton(cornerActions, "Top Left", () => RunRuntimeAction("Top Left", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopLeft)), status => status.OverlayCorner == PerfMeterOverlayCorner.TopLeft);
			AddRuntimeButton(cornerActions, "Top Right", () => RunRuntimeAction("Top Right", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight)), status => status.OverlayCorner == PerfMeterOverlayCorner.TopRight);
			AddRuntimeButton(cornerActions, "Bottom Left", () => RunRuntimeAction("Bottom Left", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomLeft)), status => status.OverlayCorner == PerfMeterOverlayCorner.BottomLeft);
			AddRuntimeButton(cornerActions, "Bottom Right", () => RunRuntimeAction("Bottom Right", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomRight)), status => status.OverlayCorner == PerfMeterOverlayCorner.BottomRight);

			VisualElement cpuCoreSection = AddSection(_runtimePanel, "CPU Core Panels");
			VisualElement cpuCoreActions = AddActions(cpuCoreSection);
			AddRuntimeButton(cpuCoreActions, "CPU Core Rows", () => RunRuntimeAction("CPU Core Rows", () => RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCores, true)), status => StatusHasModule(status, PerfMeterOverlayModule.CpuCores));
			AddRuntimeButton(cpuCoreActions, "CPU Core % Bars", () => RunRuntimeAction("CPU Core % Bars", () => RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCoreBars, true)), status => StatusHasModule(status, PerfMeterOverlayModule.CpuCoreBars));
			AddRuntimeButton(cpuCoreActions, "CPU Core Graphs", () => RunRuntimeAction("CPU Core Graphs", () => RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCoreGraphs, true)), status => StatusHasModule(status, PerfMeterOverlayModule.CpuCoreGraphs));
			AddRuntimeButton(cpuCoreActions, "Hide CPU Cores", () => RunRuntimeAction("Hide CPU Cores", HideCpuCoreModules), status => !StatusHasAnyCpuCoreModule(status));

			VisualElement warningSection = AddSection(_runtimePanel, "Editor Warning Logs");
			VisualElement warningActions = AddActions(warningSection);
			AddRuntimeButton(warningActions, "Enable Editor Warning Logs", () => RunRuntimeAction("Enable Editor Warning Logs", ToggleEditorWarningLogs), status => status.EditorWarningsEnabled);

			VisualElement overdrawSection = AddSection(_runtimePanel, "Overdraw");
			VisualElement overdrawActions = AddActions(overdrawSection);
			AddRuntimeButton(overdrawActions, "Measure Overdraw 30f", () => RunRuntimeAction("Measure Overdraw", () => RuntimePerformanceMeter.RequestOverdrawMeasurement(30)), status => status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring);
			AddRuntimeButton(overdrawActions, "Cancel Overdraw", () => RunRuntimeAction("Cancel Overdraw", RuntimePerformanceMeter.CancelOverdrawMeasurement), status => status.OverdrawState == PerfMeterOverdrawMeasurementState.Measuring);
			AddRuntimeButton(overdrawActions, "Show Heatmap", () => RunRuntimeAction("Show Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(true)), status => status.OverdrawHeatmapVisible);
			AddRuntimeButton(overdrawActions, "Hide Heatmap", () => RunRuntimeAction("Hide Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(false)), status => !status.OverdrawHeatmapVisible);
		}

		private void BuildSettingsPanel()
		{
			VisualElement section = AddSection(_settingsPanel, "Localization");
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
			VisualElement indicator = new VisualElement();
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
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-row");
			Label keyLabel = new Label(key);
			keyLabel.AddToClassList("pm-key");
			Label valueLabel = new Label(value);
			valueLabel.AddToClassList("pm-value");
			row.Add(keyLabel);
			row.Add(valueLabel);
			parent.Add(row);
			return valueLabel;
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

		private static void AddInfo(VisualElement parent, string text)
		{
			Label label = new Label(text);
			label.AddToClassList("pm-info");
			parent.Add(label);
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

		private Button AddSettingsCollectionModeButton(VisualElement parent, PerfMeterCollectionMode mode)
		{
			return AddSettingsButton(parent, FormatEnumLabel(mode.ToString()), () => _settingsCollectionMode?.SetValueWithoutNotify(mode), () => _settingsCollectionMode != null && _settingsCollectionMode.value is PerfMeterCollectionMode value && value == mode);
		}

		private Button AddSettingsPresetButton(VisualElement parent, PerfMeterOverlayPreset preset)
		{
			return AddSettingsButton(parent, FormatEnumLabel(preset.ToString()), () => SetSettingsActivePreset(preset), () => _settingsActivePreset != null && _settingsActivePreset.value is PerfMeterOverlayPreset value && value == preset);
		}

		private Button AddSettingsTargetFpsButton(VisualElement parent, PerfMeterTargetFps targetFps)
		{
			return AddSettingsButton(parent, FormatTargetFps(targetFps), () => _settingsTargetFps?.SetValueWithoutNotify(targetFps), () => _settingsTargetFps != null && _settingsTargetFps.value is PerfMeterTargetFps value && value == targetFps);
		}

		private Button AddSettingsCornerButton(VisualElement parent, PerfMeterOverlayCorner corner)
		{
			return AddSettingsButton(parent, FormatEnumLabel(corner.ToString()), () => _settingsOverlayCorner?.SetValueWithoutNotify(corner), () => _settingsOverlayCorner != null && _settingsOverlayCorner.value is PerfMeterOverlayCorner value && value == corner);
		}

		private Button AddSettingsLayoutButton(VisualElement parent, PerfMeterOverlayLayout layout)
		{
			return AddSettingsButton(parent, FormatEnumLabel(layout.ToString()), () => SetSettingsOverlayLayout(layout), () => _settingsOverlayLayout != null && _settingsOverlayLayout.value is PerfMeterOverlayLayout value && value == layout);
		}

		private Button AddSettingsThemeButton(VisualElement parent, PerfMeterOverlayTheme theme)
		{
			return AddSettingsButton(parent, FormatEnumLabel(theme.ToString()), () => _settingsOverlayTheme?.SetValueWithoutNotify(theme), () => _settingsOverlayTheme != null && _settingsOverlayTheme.value is PerfMeterOverlayTheme value && value == theme);
		}

		private Button AddSettingsFontButton(VisualElement parent, PerfMeterOverlayFontFamily fontFamily)
		{
			return AddSettingsButton(parent, FormatOverlayFontLabel(fontFamily), () => _settingsOverlayFontFamily?.SetValueWithoutNotify(fontFamily), () => _settingsOverlayFontFamily != null && _settingsOverlayFontFamily.value is PerfMeterOverlayFontFamily value && value == fontFamily);
		}

		private Button AddTargetFpsButton(VisualElement parent, PerfMeterTargetFps targetFps)
		{
			return AddRuntimeButton(parent, FormatTargetFps(targetFps), () => RunRuntimeAction("Target " + FormatTargetFps(targetFps), () => RuntimePerformanceMeter.SetTargetFps(targetFps)), status => status.TargetFps == targetFps);
		}

		private Button AddOverlayThemeButton(VisualElement parent, PerfMeterOverlayTheme theme)
		{
			return AddRuntimeButton(parent, FormatEnumLabel(theme.ToString()), () => RunRuntimeAction("Theme " + theme, () => RuntimePerformanceMeter.SetOverlayTheme(theme)), status => status.OverlayTheme == theme);
		}

		private Button AddOverlayLayoutButton(VisualElement parent, PerfMeterOverlayLayout layout)
		{
			return AddRuntimeButton(parent, FormatEnumLabel(layout.ToString()), () => RunRuntimeAction("Layout " + layout, () => RuntimePerformanceMeter.SetOverlayLayout(layout)), status => status.OverlayLayout == layout);
		}

		private Button AddOverlayFontButton(VisualElement parent, PerfMeterOverlayFontFamily fontFamily)
		{
			return AddRuntimeButton(parent, FormatOverlayFontLabel(fontFamily), () => RunRuntimeAction("Font " + fontFamily, () => RuntimePerformanceMeter.SetOverlayFontFamily(fontFamily)), status => status.OverlayFontFamily == fontFamily);
		}

		private static void HideCpuCoreModules()
		{
			RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCores, false);
			RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCoreBars, false);
			RuntimePerformanceMeter.SetOverlayModuleVisible(PerfMeterOverlayModule.CpuCoreGraphs, false);
		}

		private static void ToggleEditorWarningLogs()
		{
			RuntimePerformanceMeter.SetEditorWarningLogsEnabled(!RuntimePerformanceMeter.EditorWarningLogsEnabled);
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

			_rendererStatus.text = status.RendererMessage;
			BuildRendererList(status);
			RefreshRendererButtons(status);
			SetIndicator(_rendererIndicator, status.AllRenderersConfigured, !status.RendererFeatureSetupSupported || (status.Renderers.Count > 0 && !status.AllRenderersConfigured));
			RefreshSettingsPanel(status);
			RefreshInitializationCode();
			RefreshRuntimePanel();
			RefreshWindowSettingsPanel();
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
			_settingsCollectionMode?.SetValueWithoutNotify(settings.CollectionMode);
			_settingsOverlayVisible?.SetValueWithoutNotify(settings.OverlayVisible);
			_settingsTargetFps?.SetValueWithoutNotify(settings.TargetFps);
			_settingsOverlayCorner?.SetValueWithoutNotify(settings.OverlayCorner);
			_settingsOverlayTheme?.SetValueWithoutNotify(settings.OverlayTheme);
			_settingsOverlayLayout?.SetValueWithoutNotify(settings.OverlayLayout);
			_settingsOverlayFontFamily?.SetValueWithoutNotify(settings.OverlayFontFamily);
			if (_settingsActivePreset != null)
			{
				_settingsActivePreset.SetValueWithoutNotify(PerfMeterSettingsStore.ParseOverlayPreset(settings.ActivePreset));
			}

			SetModuleToggles(settings.OverlayModules);
			_settingsOverlayScale?.SetValueWithoutNotify(settings.OverlayScale);
			_settingsOverlayOpacity?.SetValueWithoutNotify(settings.OverlayOpacity);
			_settingsOverlayFontSize?.SetValueWithoutNotify(settings.OverlayFontSize);
			_settingsOverlayRefreshInterval?.SetValueWithoutNotify(settings.OverlayRefreshIntervalSeconds);
			_settingsOverlayGraphHistory?.SetValueWithoutNotify(settings.OverlayGraphHistoryLength);
			_settingsEditorWarningsEnabled?.SetValueWithoutNotify(settings.EditorWarningsEnabled);
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
			RefreshSettingsButtonStates();
		}

		private PerfMeterSetupActionResult SaveSettingsFromControls()
		{
			PerfMeterSettingsSnapshot currentSettings = PerfMeterSetupActions.LoadSettings();
			PerfMeterOverlayPreset activePreset = _settingsActivePreset != null && _settingsActivePreset.value is PerfMeterOverlayPreset preset
				? preset
				: PerfMeterOverlayPreset.FullDiagnostics;
			PerfMeterOverlayLayout overlayLayout = _settingsOverlayLayout != null && _settingsOverlayLayout.value is PerfMeterOverlayLayout layout
				? layout
				: currentSettings.OverlayLayout;
			PerfMeterSettingsSnapshot settings = new PerfMeterSettingsSnapshot(
				_settingsEnabled == null || _settingsEnabled.value,
				_settingsAutoStart == null || _settingsAutoStart.value,
				_settingsCollectionMode != null && _settingsCollectionMode.value is PerfMeterCollectionMode collectionMode ? collectionMode : currentSettings.CollectionMode,
				_settingsOverlayVisible == null || _settingsOverlayVisible.value,
				_settingsOverlayCorner != null && _settingsOverlayCorner.value is PerfMeterOverlayCorner corner ? corner : PerfMeterOverlayCorner.TopRight,
				PerfMeterSettingsStore.GetLayoutMode(overlayLayout, currentSettings.OverlayMode),
				_settingsTargetFps != null && _settingsTargetFps.value is PerfMeterTargetFps targetFps ? targetFps : PerfMeterTargetFps.Fps60,
				activePreset.ToString(),
				GetSelectedOverlayModules(activePreset),
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
				editorWarningsEnabled: _settingsEditorWarningsEnabled == null ? currentSettings.EditorWarningsEnabled : _settingsEditorWarningsEnabled.value);
			return PerfMeterSetupActions.SaveSettings(settings);
		}

		private void AddModuleToggle(VisualElement parent, PerfMeterOverlayModule module, string label)
		{
			Toggle toggle = new Toggle(label);
			toggle.AddToClassList("pm-module-toggle");
			toggle.RegisterValueChangedCallback(_ => MarkSettingsPresetCustom());
			_settingsModuleToggles.Add(new OverlayModuleToggle(module, toggle));
			parent.Add(toggle);
		}

		private void SetSettingsActivePreset(PerfMeterOverlayPreset preset)
		{
			_settingsActivePreset?.SetValueWithoutNotify(preset);
			ApplyPresetDefaultsToSettingsControls(preset);
		}

		private void SetSettingsOverlayLayout(PerfMeterOverlayLayout layout)
		{
			_settingsOverlayLayout?.SetValueWithoutNotify(layout);
			if (_settingsActivePreset != null && _settingsActivePreset.value is PerfMeterOverlayPreset preset && preset != PerfMeterOverlayPreset.Custom && layout != PerfMeterSettingsStore.GetPresetLayout(preset))
			{
				_settingsActivePreset.SetValueWithoutNotify(PerfMeterOverlayPreset.Custom);
			}
		}

		private void MarkSettingsPresetCustom()
		{
			if (_settingsActivePreset != null && _settingsActivePreset.value is PerfMeterOverlayPreset preset && preset != PerfMeterOverlayPreset.Custom)
			{
				_settingsActivePreset.SetValueWithoutNotify(PerfMeterOverlayPreset.Custom);
				RefreshSettingsButtonStates();
			}
		}

		private void ApplyPresetDefaultsToSettingsControls(PerfMeterOverlayPreset preset)
		{
			if (preset == PerfMeterOverlayPreset.Custom)
			{
				return;
			}

			_settingsOverlayLayout?.SetValueWithoutNotify(PerfMeterSettingsStore.GetPresetLayout(preset));
			SetModuleToggles(PerfMeterSettingsStore.GetPresetModules(preset));
			RefreshSettingsButtonStates();
		}

		private void SetModuleToggles(PerfMeterOverlayModule modules)
		{
			PerfMeterOverlayModule normalized = modules == PerfMeterOverlayModule.None ? PerfMeterOverlayModule.All : modules;
			for (int i = 0; i < _settingsModuleToggles.Count; i++)
			{
				OverlayModuleToggle entry = _settingsModuleToggles[i];
				entry.Toggle.SetValueWithoutNotify((normalized & entry.Module) != 0);
			}
		}

		private PerfMeterOverlayModule GetSelectedOverlayModules(PerfMeterOverlayPreset activePreset)
		{
			PerfMeterOverlayModule modules = PerfMeterOverlayModule.None;
			for (int i = 0; i < _settingsModuleToggles.Count; i++)
			{
				OverlayModuleToggle entry = _settingsModuleToggles[i];
				if (entry.Toggle.value)
				{
					modules |= entry.Module;
				}
			}

			return modules == PerfMeterOverlayModule.None ? PerfMeterSettingsStore.GetPresetModules(activePreset) : modules;
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
			Label pathLabel = new Label(assetPath);
			pathLabel.AddToClassList("pm-renderer-path");
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
			bool visible = _initOverlayVisible == null || _initOverlayVisible.value;
			PerfMeterOverlayCorner corner = _initOverlayCorner != null && _initOverlayCorner.value is PerfMeterOverlayCorner cornerValue
				? cornerValue
				: PerfMeterOverlayCorner.TopRight;
			PerfMeterOverlayTheme theme = _initOverlayTheme != null && _initOverlayTheme.value is PerfMeterOverlayTheme themeValue
				? themeValue
				: PerfMeterOverlayTheme.ClassicDark;
			PerfMeterOverlayLayout layout = _initOverlayLayout != null && _initOverlayLayout.value is PerfMeterOverlayLayout layoutValue
				? layoutValue
				: PerfMeterOverlayLayout.MetricBars;
			PerfMeterOverlayFontFamily fontFamily = _initOverlayFontFamily != null && _initOverlayFontFamily.value is PerfMeterOverlayFontFamily fontFamilyValue
				? fontFamilyValue
				: PerfMeterOverlayFontFamily.Manrope;
			PerfMeterTargetFps targetFps = _initTargetFps != null && _initTargetFps.value is PerfMeterTargetFps targetFpsValue
				? targetFpsValue
				: PerfMeterTargetFps.Fps60;
			return PerfMeterSetupUtility.BuildInitializationSnippet(visible, corner, targetFps, theme, layout, fontFamily);
		}

		private void SelectCurrentTab()
		{
			switch (_currentTab)
			{
				case "Presets":
					SelectPresetsTab();
					break;
				case "Runtime":
					SelectRuntimeTab();
					break;
				case "Settings":
					SelectSettingsTab();
					break;
				default:
					SelectSetupTab();
					break;
			}
		}

		private void SelectSetupTab()
		{
			_currentTab = "Setup";
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

			if (_settingsTab != null)
			{
				_settingsTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.Flex;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_settingsPanel != null)
			{
				_settingsPanel.style.display = DisplayStyle.None;
			}
		}

		private void SelectPresetsTab()
		{
			_currentTab = "Presets";
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

			if (_settingsTab != null)
			{
				_settingsTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_presetsPanel != null)
			{
				_presetsPanel.style.display = DisplayStyle.Flex;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}

			if (_settingsPanel != null)
			{
				_settingsPanel.style.display = DisplayStyle.None;
			}
		}

		private void SelectRuntimeTab()
		{
			_currentTab = "Runtime";
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

			if (_settingsTab != null)
			{
				_settingsTab.SetValueWithoutNotify(false);
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
				_runtimePanel.style.display = DisplayStyle.Flex;
			}

			if (_settingsPanel != null)
			{
				_settingsPanel.style.display = DisplayStyle.None;
			}

			RefreshRuntimePanel();
			ApplyLocalization();
		}

		private void SelectSettingsTab()
		{
			_currentTab = "Settings";
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

			if (_settingsTab != null)
			{
				_settingsTab.SetValueWithoutNotify(true);
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

			if (_settingsPanel != null)
			{
				_settingsPanel.style.display = DisplayStyle.Flex;
			}

			RefreshWindowSettingsPanel();
			ApplyLocalization();
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
			SetRuntimeButtonsEnabled(isPlaying);
			RefreshRuntimeButtonStates(status);
			_runtimeStatus.text = status.State + " / " + status.Bottleneck;
			_runtimeCollectionMode.text = status.CollectionMode.ToString();
			_runtimeOverlayVisible.text = status.OverlayVisible ? "Visible" : "Hidden";
			_runtimeOverlayPreset.text = status.OverlayPreset.ToString();
			_runtimeOverlayModules.text = status.OverlayModules.ToString();
			_runtimeTargetFps.text = FormatTargetFps(status.TargetFps) + " / " + (1000d / (int)status.TargetFps).ToString("0.00") + " ms";
			_runtimeOverlayCorner.text = status.OverlayCorner.ToString();
			_runtimeOverlayTheme.text = status.OverlayTheme.ToString();
			_runtimeOverlayLayout.text = status.OverlayLayout.ToString();
			_runtimeOverlayFontFamily.text = status.OverlayFontFamily.ToString();
			_runtimeEditorWarnings.text = status.EditorWarningsEnabled ? "Enabled" : "Disabled";
			_runtimeOverdraw.text = status.OverdrawState + " " + (status.OverdrawProgress * 100f).ToString("0") + "% / heatmap " + (status.OverdrawHeatmapVisible ? "on" : "off");
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

		private static bool StatusHasModule(PerfMeterStatusSnapshot status, PerfMeterOverlayModule module)
		{
			return (status.OverlayModules & module) == module;
		}

		private static bool StatusHasAnyCpuCoreModule(PerfMeterStatusSnapshot status)
		{
			PerfMeterOverlayModule cpuCoreModules = PerfMeterOverlayModule.CpuCores | PerfMeterOverlayModule.CpuCoreBars | PerfMeterOverlayModule.CpuCoreGraphs;
			return (status.OverlayModules & cpuCoreModules) != 0;
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

		private static void SetIndicator(VisualElement indicator, bool ok, bool warn)
		{
			if (indicator == null)
			{
				return;
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

		private readonly struct OverlayModuleToggle
		{
			internal OverlayModuleToggle(PerfMeterOverlayModule module, Toggle toggle)
			{
				Module = module;
				Toggle = toggle;
			}

			internal PerfMeterOverlayModule Module { get; }
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
