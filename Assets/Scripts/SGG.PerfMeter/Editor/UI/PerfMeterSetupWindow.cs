using System;
using System.Collections.Generic;
using SGG.PerfMeter.Editor.Setup;
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
		private readonly HashSet<string> _selectedRendererPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private VisualElement _setupPanel;
		private VisualElement _runtimePanel;
		private ToolbarToggle _setupTab;
		private ToolbarToggle _runtimeTab;

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
		private EnumField _initOverlayMode;
		private Label _lastActionLabel;
		private Label _runtimePlayModeInfo;
		private Label _runtimeStatus;
		private Label _runtimeOverlayVisible;
		private Label _runtimeTargetFps;
		private Label _runtimeOverlayCorner;
		private Label _runtimeOverlayMode;
		private Label _runtimeOverdraw;

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
			_runtimePanel = new VisualElement();
			scroll.Add(_setupPanel);
			scroll.Add(_runtimePanel);

			BuildSetupPanel();
			BuildRuntimePanel();

			_lastActionLabel = new Label();
			_lastActionLabel.AddToClassList("pm-log");
			rootVisualElement.Add(_lastActionLabel);

			SelectSetupTab();
			RefreshAll();
		}

		private void BuildTabs()
		{
			Toolbar toolbar = new Toolbar();
			toolbar.AddToClassList("pm-tabs");
			_setupTab = new ToolbarToggle { text = "Setup" };
			_runtimeTab = new ToolbarToggle { text = "Runtime" };
			_setupTab.AddToClassList("pm-tab");
			_runtimeTab.AddToClassList("pm-tab");
			_setupTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectSetupTab();
				}
			});
			_runtimeTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					SelectRuntimeTab();
				}
			});
			toolbar.Add(_setupTab);
			toolbar.Add(_runtimeTab);
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
			AddInfo(section, "Copy this code into your gameplay bootstrap or another project-owned runtime script. The setup window does not create this file automatically.");

			_initOverlayVisible = new Toggle { value = true };
			_initOverlayVisible.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Visible", _initOverlayVisible);

			_initTargetFps = new EnumField(PerfMeterTargetFps.Fps60);
			_initTargetFps.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Target FPS", _initTargetFps);

			_initOverlayCorner = new EnumField(PerfMeterOverlayCorner.TopRight);
			_initOverlayCorner.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Corner", _initOverlayCorner);

			_initOverlayMode = new EnumField(PerfMeterOverlayMode.Full);
			_initOverlayMode.RegisterValueChangedCallback(_ => RefreshInitializationCode());
			AddControlRow(section, "Overlay Mode", _initOverlayMode);

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

		private void BuildRuntimePanel()
		{
			VisualElement runtimeSection = AddSection(_runtimePanel, "Runtime Controls");
			_runtimePlayModeInfo = new Label();
			_runtimePlayModeInfo.AddToClassList("pm-info");
			runtimeSection.Add(_runtimePlayModeInfo);
			_runtimeStatus = AddRow(runtimeSection, "State");
			_runtimeOverlayVisible = AddRow(runtimeSection, "Overlay Visible");
			_runtimeTargetFps = AddRow(runtimeSection, "Target FPS");
			_runtimeOverlayCorner = AddRow(runtimeSection, "Overlay Corner");
			_runtimeOverlayMode = AddRow(runtimeSection, "Overlay Mode");
			_runtimeOverdraw = AddRow(runtimeSection, "Overdraw");

			VisualElement lifecycleActions = AddActions(runtimeSection);
			AddRuntimeButton(lifecycleActions, "Ensure Runtime", () => RunRuntimeAction("Ensure Runtime", RuntimePerformanceMeter.EnsureRunning));
			AddRuntimeButton(lifecycleActions, "Show Overlay", () => RunRuntimeAction("Show Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(true)));
			AddRuntimeButton(lifecycleActions, "Hide Overlay", () => RunRuntimeAction("Hide Overlay", () => RuntimePerformanceMeter.SetOverlayVisible(false)));

			VisualElement targetActions = AddActions(runtimeSection);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps15);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps30);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps60);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps90);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps120);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps144);
			AddTargetFpsButton(targetActions, PerfMeterTargetFps.Fps240);

			VisualElement modeActions = AddActions(runtimeSection);
			AddRuntimeButton(modeActions, "Fps Only", () => RunRuntimeAction("Fps Only", () => RuntimePerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.FpsOnly)));
			AddRuntimeButton(modeActions, "Text Compact", () => RunRuntimeAction("Text Compact", () => RuntimePerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.TextCompact)));
			AddRuntimeButton(modeActions, "Graphs", () => RunRuntimeAction("Graphs", () => RuntimePerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Graphs)));
			AddRuntimeButton(modeActions, "Full", () => RunRuntimeAction("Full", () => RuntimePerformanceMeter.SetOverlayMode(PerfMeterOverlayMode.Full)));

			VisualElement cornerActions = AddActions(runtimeSection);
			AddRuntimeButton(cornerActions, "Top Left", () => RunRuntimeAction("Top Left", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopLeft)));
			AddRuntimeButton(cornerActions, "Top Right", () => RunRuntimeAction("Top Right", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight)));
			AddRuntimeButton(cornerActions, "Bottom Left", () => RunRuntimeAction("Bottom Left", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomLeft)));
			AddRuntimeButton(cornerActions, "Bottom Right", () => RunRuntimeAction("Bottom Right", () => RuntimePerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.BottomRight)));

			VisualElement overdrawActions = AddActions(runtimeSection);
			AddRuntimeButton(overdrawActions, "Measure Overdraw 30f", () => RunRuntimeAction("Measure Overdraw", () => RuntimePerformanceMeter.RequestOverdrawMeasurement(30)));
			AddRuntimeButton(overdrawActions, "Cancel Overdraw", () => RunRuntimeAction("Cancel Overdraw", RuntimePerformanceMeter.CancelOverdrawMeasurement));
			AddRuntimeButton(overdrawActions, "Show Heatmap", () => RunRuntimeAction("Show Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(true)));
			AddRuntimeButton(overdrawActions, "Hide Heatmap", () => RunRuntimeAction("Hide Heatmap", () => RuntimePerformanceMeter.SetOverdrawHeatmapVisible(false)));
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

		private Button AddButton(VisualElement parent, string text, Action action)
		{
			Button button = new Button(action) { text = text };
			button.AddToClassList("pm-button");
			parent.Add(button);
			return button;
		}

		private Button AddRuntimeButton(VisualElement parent, string text, Action action)
		{
			Button button = AddButton(parent, text, action);
			_runtimeButtons.Add(button);
			return button;
		}

		private Button AddTargetFpsButton(VisualElement parent, PerfMeterTargetFps targetFps)
		{
			return AddRuntimeButton(parent, FormatTargetFps(targetFps), () => RunRuntimeAction("Target " + FormatTargetFps(targetFps), () => RuntimePerformanceMeter.SetTargetFps(targetFps)));
		}

		private void RunAction(string title, Func<PerfMeterSetupActionResult> action)
		{
			try
			{
				PerfMeterSetupActionResult result = action();
				_lastActionLabel.text = title + ": " + result.Message;
				if (!result.Success)
				{
					EditorUtility.DisplayDialog(title + " Failed", result.Message, "OK");
				}
			}
			catch (Exception exception)
			{
				_lastActionLabel.text = title + ": " + exception.Message;
				EditorUtility.DisplayDialog(title + " Failed", exception.Message, "OK");
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
			SetIndicator(_rendererIndicator, status.AllRenderersConfigured, status.Renderers.Count > 0 && !status.AllRenderersConfigured);
			RefreshInitializationCode();
			RefreshRuntimePanel();
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
			bool canInstall = !renderer.HasPerfMeterFeature && renderer.IsEditable && !string.IsNullOrEmpty(assetPath);

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
			_installSelectedRendererButton?.SetEnabled(_selectedRendererPaths.Count > 0);
		}

		private void SelectMissingRenderers()
		{
			PerfMeterSetupUtility.PerfMeterSetupStatus status = PerfMeterSetupUtility.GetStatus();
			_selectedRendererPaths.Clear();
			for (int i = 0; i < status.Renderers.Count; i++)
			{
				PerfMeterSetupUtility.RendererSetupStatus renderer = status.Renderers[i];
				if (!renderer.HasPerfMeterFeature && renderer.IsEditable && !string.IsNullOrEmpty(renderer.AssetPath))
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
		}

		private string[] GetSelectedRendererPaths()
		{
			string[] paths = new string[_selectedRendererPaths.Count];
			_selectedRendererPaths.CopyTo(paths);
			return paths;
		}

		private static bool HasMissingRendererFeature(PerfMeterSetupUtility.PerfMeterSetupStatus status)
		{
			for (int i = 0; i < status.Renderers.Count; i++)
			{
				if (!status.Renderers[i].HasPerfMeterFeature && status.Renderers[i].IsEditable)
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

			return details.Count > 0 ? string.Join("; ", details) : string.Empty;
		}

		private void CopyInitializationCode()
		{
			GUIUtility.systemCopyBuffer = _initCode != null ? _initCode.value : BuildInitializationSnippetFromOptions();
			_lastActionLabel.text = "Initialization code copied to clipboard.";
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
			PerfMeterOverlayMode mode = _initOverlayMode != null && _initOverlayMode.value is PerfMeterOverlayMode modeValue
				? modeValue
				: PerfMeterOverlayMode.Full;
			PerfMeterTargetFps targetFps = _initTargetFps != null && _initTargetFps.value is PerfMeterTargetFps targetFpsValue
				? targetFpsValue
				: PerfMeterTargetFps.Fps60;
			return PerfMeterSetupUtility.BuildInitializationSnippet(visible, corner, mode, targetFps);
		}

		private void SelectSetupTab()
		{
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(true);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(false);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.Flex;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.None;
			}
		}

		private void SelectRuntimeTab()
		{
			if (_setupTab != null)
			{
				_setupTab.SetValueWithoutNotify(false);
			}

			if (_runtimeTab != null)
			{
				_runtimeTab.SetValueWithoutNotify(true);
			}

			if (_setupPanel != null)
			{
				_setupPanel.style.display = DisplayStyle.None;
			}

			if (_runtimePanel != null)
			{
				_runtimePanel.style.display = DisplayStyle.Flex;
			}

			RefreshRuntimePanel();
		}

		private void RunRuntimeAction(string title, Action action)
		{
			if (!EditorApplication.isPlaying)
			{
				_lastActionLabel.text = title + ": enter Play Mode to use runtime controls.";
				RefreshRuntimePanel();
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
				EditorUtility.DisplayDialog(title + " Failed", exception.Message, "OK");
			}
			finally
			{
				RefreshRuntimePanel();
			}
		}

		private void RefreshRuntimePanel()
		{
			if (_runtimeStatus == null)
			{
				return;
			}

			bool isPlaying = EditorApplication.isPlaying;
			SetRuntimeButtonsEnabled(isPlaying);
			_runtimePlayModeInfo.text = isPlaying
				? "Runtime controls affect the currently running Play Mode session."
				: "Runtime controls are read-only in Edit Mode. Enter Play Mode to test overlay modes, visibility, and short overdraw capture.";

			PerfMeterStatusSnapshot status = RuntimePerformanceMeter.GetStatus();
			_runtimeStatus.text = status.State + " / " + status.Bottleneck;
			_runtimeOverlayVisible.text = status.OverlayVisible ? "Visible" : "Hidden";
			_runtimeTargetFps.text = FormatTargetFps(status.TargetFps) + " / " + (1000d / (int)status.TargetFps).ToString("0.00") + " ms";
			_runtimeOverlayCorner.text = status.OverlayCorner.ToString();
			_runtimeOverlayMode.text = status.OverlayMode.ToString();
			_runtimeOverdraw.text = status.OverdrawState + " " + (status.OverdrawProgress * 100f).ToString("0") + "% / heatmap " + (status.OverdrawHeatmapVisible ? "on" : "off");
		}

		private void SetRuntimeButtonsEnabled(bool enabled)
		{
			for (int i = 0; i < _runtimeButtons.Count; i++)
			{
				_runtimeButtons[i].SetEnabled(enabled);
			}
		}

		private static string FormatTargetFps(PerfMeterTargetFps targetFps)
		{
			return ((int)targetFps).ToString() + " FPS";
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
	}
}
