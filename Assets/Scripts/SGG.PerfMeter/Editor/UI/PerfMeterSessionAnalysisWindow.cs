using System;
using System.Collections;
using System.Collections.Generic;
using SGG.PerfMeter.Editor.Setup;
using SGG.PerfMeter.Editor.UI.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Editor.UI
{
	public sealed class PerfMeterSessionAnalysisWindow : EditorWindow
	{
		private readonly PerfMeterSessionAnalysisModel _model = new PerfMeterSessionAnalysisModel();
		private PerfMeterSessionSampleSnapshot[] _cachedSamples = Array.Empty<PerfMeterSessionSampleSnapshot>();
		private string _lastSessionId = string.Empty;
		private PerfMeterSessionState _lastSessionState = PerfMeterSessionState.Idle;
		private int _lastSampleCount = -1;
		private double _lastStartTimeSeconds = double.NaN;
		private int _lastFirstFrame = int.MinValue;
		private bool _samplesRead;
		private bool _refreshInProgress;
		private IVisualElementScheduledItem _refreshSchedule;

		private string _currentTab = "Timeline";
		private ToolbarToggle _timelineTab;
		private ToolbarToggle _worstFrameTab;
		private ToolbarToggle _budgetTab;
		private ToolbarToggle _scopesTab;
		private VisualElement _timelinePanel;
		private VisualElement _worstFramePanel;
		private VisualElement _budgetPanel;
		private VisualElement _scopesPanel;

		private Label _summaryInfo;
		private Label _summaryState;
		private Label _summarySessionId;
		private Label _summarySamples;
		private Label _summaryDroppedSamples;
		private Label _summaryDuration;
		private Label _summaryScenes;
		private Label _summaryFocus;
		private Label _summaryPause;
		private Label _summaryWarning;

		private Label _timelineEmpty;
		private Label _budgetEmpty;
		private Label _scopeEmpty;
		private Label _worstInfo;
		private VisualElement _worstDetails;
		private ListView _timelineList;
		private ListView _budgetList;
		private ListView _scopeList;

		[MenuItem("SGG/Perfmeter/Session Analysis")]
		public static void Open()
		{
			PerfMeterSessionAnalysisWindow window = GetWindow<PerfMeterSessionAnalysisWindow>("SGG PerfMeter Session Analysis");
			window.minSize = new Vector2(760f, 460f);
			window.Show();
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			_refreshSchedule?.Pause();
			_refreshSchedule = null;
			_cachedSamples = Array.Empty<PerfMeterSessionSampleSnapshot>();
			_samplesRead = false;
		}

		private void OnFocus()
		{
			ForceRefresh();
		}

		public void CreateGUI()
		{
			_refreshSchedule?.Pause();
			_refreshSchedule = null;
			_samplesRead = false;
			_cachedSamples = Array.Empty<PerfMeterSessionSampleSnapshot>();
			rootVisualElement.Clear();
			rootVisualElement.AddToClassList("pm-window");

			LoadStyleSheets();

			Label title = new Label(Localize("SGG PerfMeter Session Analysis"));
			title.AddToClassList("pm-title");
			rootVisualElement.Add(title);

			BuildTabs();

			ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.flexGrow = 1f;
			rootVisualElement.Add(scroll);

			BuildSummaryPanel(scroll);
			BuildTimelinePanel(scroll);
			BuildWorstFramePanel(scroll);
			BuildBudgetPanel(scroll);
			BuildScopesPanel(scroll);

			SelectCurrentTab();
			RefreshFromRuntime(true);
			_refreshSchedule = rootVisualElement.schedule.Execute(() => RefreshFromRuntime()).Every(750);
		}

		private void LoadStyleSheets()
		{
			string packagePath = PerfMeterSetupUtility.PackageAssetPath;
			AddStyleSheet(packagePath + "/Editor/UI/PerfMeterSetupWindow.uss");
			AddStyleSheet(packagePath + "/Editor/UI/PerfMeterSessionAnalysisWindow.uss");
		}

		private void AddStyleSheet(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
			if (style != null)
			{
				rootVisualElement.styleSheets.Add(style);
			}
		}

		private void BuildTabs()
		{
			Toolbar toolbar = new Toolbar();
			toolbar.AddToClassList("pm-tabs");
			_timelineTab = CreateTab("Timeline", SelectTimelineTab);
			_worstFrameTab = CreateTab("Worst Frame", SelectWorstFrameTab);
			_budgetTab = CreateTab("Budget Violations", SelectBudgetTab);
			_scopesTab = CreateTab("Scene Scopes", SelectScopesTab);
			toolbar.Add(_timelineTab);
			toolbar.Add(_worstFrameTab);
			toolbar.Add(_budgetTab);
			toolbar.Add(_scopesTab);
			rootVisualElement.Add(toolbar);
		}

		private static ToolbarToggle CreateTab(string text, Action selectAction)
		{
			ToolbarToggle tab = new ToolbarToggle { text = Localize(text) };
			tab.AddToClassList("pm-tab");
			tab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					selectAction();
				}
			});
			return tab;
		}

		private void BuildSummaryPanel(VisualElement parent)
		{
			VisualElement section = AddSection(parent, "Session Summary");
			_summaryInfo = AddInfo(section, "No session is available. Session Analysis is read-only.");
			_summaryState = AddRow(section, "State");
			_summarySessionId = AddCopyRow(section, "Session ID");
			_summarySamples = AddRow(section, "Samples");
			_summaryDroppedSamples = AddRow(section, "Dropped samples");
			_summaryDuration = AddRow(section, "Duration");
			_summaryScenes = AddRow(section, "Scenes");
			_summaryFocus = AddRow(section, "Focus losses");
			_summaryPause = AddRow(section, "Pauses");
			_summaryWarning = AddRow(section, "Warning");

			VisualElement actions = AddActions(section);
			AddButton(actions, "Refresh", ForceRefresh);
		}

		private void BuildTimelinePanel(VisualElement parent)
		{
			_timelinePanel = new VisualElement();
			VisualElement section = AddSection(_timelinePanel, "Timeline");
			AddInfo(section, "Recorded samples are shown in collection order. GPU values require explicit GPU timing availability.");
			_timelineEmpty = AddEmptyLabel(section);
			_timelineList = AddVirtualizedTable(
				section,
				"timeline",
				new[] { "Frame", "Time", "Scene", "CPU", "Main", "Render", "Present", "GPU", "Budget", "Bottleneck", "Spikes", "Trace" },
				_model.TimelineRows,
				BindTimelineRow);
			parent.Add(_timelinePanel);
		}

		private void BuildWorstFramePanel(VisualElement parent)
		{
			_worstFramePanel = new VisualElement();
			VisualElement section = AddSection(_worstFramePanel, "Worst Frame");
			_worstInfo = AddInfo(section, "No worst frame: no recorded samples.");
			_worstDetails = new VisualElement();
			_worstDetails.AddToClassList("pm-analysis-inspector");
			section.Add(_worstDetails);
			parent.Add(_worstFramePanel);
		}

		private void BuildBudgetPanel(VisualElement parent)
		{
			_budgetPanel = new VisualElement();
			VisualElement section = AddSection(_budgetPanel, "Budget Violations");
			AddInfo(section, "Violations use each sample's recorded frame budget. CPU main time excludes present wait.");
			_budgetEmpty = AddEmptyLabel(section);
			_budgetList = AddVirtualizedTable(
				section,
				"budget",
				new[] { "Frame", "Time", "Scene", "Kind", "Value", "Budget", "Overage", "Bottleneck" },
				_model.BudgetViolationRows,
				BindBudgetRow);
			parent.Add(_budgetPanel);
		}

		private void BuildScopesPanel(VisualElement parent)
		{
			_scopesPanel = new VisualElement();
			VisualElement section = AddSection(_scopesPanel, "Scene Scopes");
			AddInfo(section, "Only the authoritative whole-run and current-scene snapshots are displayed.");
			_scopeEmpty = AddEmptyLabel(section);
			_scopeList = AddVirtualizedTable(
				section,
				"scope",
				new[] { "Scope", "Scene", "Samples", "Frames", "Times", "Duration", "Avg ms", "Min ms", "Max ms", "Avg FPS", "Min FPS", "Max FPS", "Bottlenecks", "Spikes", "Worst" },
				_model.ScopeRows,
				BindScopeRow);
			parent.Add(_scopesPanel);
		}

		private ListView AddVirtualizedTable(
			VisualElement parent,
			string prefix,
			string[] headers,
			IList items,
			Action<VisualElement, int> bindItem)
		{
			VisualElement table = new VisualElement();
			table.AddToClassList("pm-debug-table");
			table.AddToClassList("pm-analysis-table--" + prefix);
			table.Add(CreateTableRow(prefix, headers, true));

			ListView list = new ListView
			{
				itemsSource = items,
				fixedItemHeight = 25f,
				virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
				selectionType = SelectionType.None,
				makeItem = () => CreateTableRow(prefix, headers, false),
				bindItem = bindItem
			};
			list.AddToClassList("pm-analysis-list");
			table.Add(list);
			ScrollView horizontalScroll = new ScrollView(ScrollViewMode.Horizontal);
			horizontalScroll.AddToClassList("pm-analysis-horizontal-scroll");
			horizontalScroll.Add(table);
			parent.Add(horizontalScroll);
			return list;
		}

		private static VisualElement CreateTableRow(string prefix, string[] headers, bool header)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-debug-row");
			if (header)
			{
				row.AddToClassList("pm-debug-row--header");
			}

			Label[] cells = new Label[headers.Length];
			for (int index = 0; index < headers.Length; index++)
			{
				Label cell = new Label(header ? Localize(headers[index]) : string.Empty);
				cell.AddToClassList("pm-debug-cell");
				cell.AddToClassList("pm-analysis-cell--" + prefix + "-" + index);
				row.Add(cell);
				cells[index] = cell;
			}

			row.userData = cells;
			return row;
		}

		private static void SetTableRow(VisualElement row, string[] values)
		{
			Label[] cells = row.userData as Label[];
			if (cells == null)
			{
				return;
			}

			int count = Math.Min(cells.Length, values.Length);
			for (int index = 0; index < count; index++)
			{
				string value = values[index] ?? string.Empty;
				cells[index].text = value;
				cells[index].tooltip = value;
			}
		}

		private void BindTimelineRow(VisualElement element, int index)
		{
			PerfMeterSessionTimelineRow row = _model.TimelineRows[index];
			SetTableRow(element, new[]
			{
				PerfMeterSessionAnalysisModel.FormatFrame(true, row.Frame),
				PerfMeterSessionAnalysisModel.FormatSeconds(row.TimeAvailable, row.RelativeTimeSeconds),
				row.SceneChanged ? "Scene change: " + PerfMeterSessionAnalysisModel.FormatScene(row.SceneName) : PerfMeterSessionAnalysisModel.FormatScene(row.SceneName),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.CpuTimingAvailable, row.CpuFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.CpuTimingAvailable, row.CpuMainThreadFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.CpuTimingAvailable, row.CpuRenderThreadFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.CpuTimingAvailable, row.CpuMainThreadPresentWaitTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.GpuTimingAvailable, row.GpuFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(row.BudgetAvailable, row.FrameBudgetMs),
				row.Bottleneck.ToString(),
				PerfMeterSessionAnalysisModel.FormatInteger(row.CpuTimingAvailable, row.FrameSpikeCount) + " / " + PerfMeterSessionAnalysisModel.FormatInteger(row.CpuTimingAvailable, row.SevereFrameSpikeCount),
				PerfMeterSessionAnalysisModel.FormatText(row.GraphicsStateTraceId, "None")
			});
		}

		private void BindBudgetRow(VisualElement element, int index)
		{
			PerfMeterSessionBudgetViolationRow row = _model.BudgetViolationRows[index];
			SetTableRow(element, new[]
			{
				PerfMeterSessionAnalysisModel.FormatFrame(true, row.Frame),
				PerfMeterSessionAnalysisModel.FormatSeconds(row.TimeAvailable, row.RelativeTimeSeconds),
				PerfMeterSessionAnalysisModel.FormatScene(row.SceneName),
				FormatViolationKind(row.Kind),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(true, row.ValueMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(true, row.BudgetMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(true, row.OverageMs),
				row.Bottleneck.ToString()
			});
		}

		private void BindScopeRow(VisualElement element, int index)
		{
			PerfMeterSessionScopeRow row = _model.ScopeRows[index];
			PerfMeterSessionScopeSummarySnapshot scope = row.Snapshot;
			bool hasSamples = row.HasSamples;
			string times = hasSamples
				? PerfMeterSessionAnalysisModel.FormatSeconds(true, scope.StartTimeSeconds) + " / " + PerfMeterSessionAnalysisModel.FormatSeconds(true, scope.LastSampleTimeSeconds)
				: PerfMeterSessionAnalysisModel.NoSamplesText;
			string frames = hasSamples
				? PerfMeterSessionAnalysisModel.FormatFrame(scope.FirstFrame >= 0 && scope.LastFrame >= 0, scope.FirstFrame) + " / " + PerfMeterSessionAnalysisModel.FormatFrame(scope.LastFrame >= 0, scope.LastFrame)
				: PerfMeterSessionAnalysisModel.NoSamplesText;

			SetTableRow(element, new[]
			{
				Localize(row.Label),
				PerfMeterSessionAnalysisModel.FormatScene(scope.SceneName),
				PerfMeterSessionAnalysisModel.FormatInteger(hasSamples, scope.SampleCount),
				frames,
				times,
				PerfMeterSessionAnalysisModel.FormatSeconds(hasSamples, scope.DurationSeconds),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(hasSamples, scope.AverageFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(hasSamples, scope.MinFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatMilliseconds(hasSamples, scope.MaxFrameTimeMs),
				PerfMeterSessionAnalysisModel.FormatFps(hasSamples, scope.AverageFps),
				PerfMeterSessionAnalysisModel.FormatFps(hasSamples, scope.MinFps),
				PerfMeterSessionAnalysisModel.FormatFps(hasSamples, scope.MaxFps),
				FormatBottlenecks(scope, hasSamples),
				FormatSpikes(scope, hasSamples),
				FormatWorstFrame(scope.WorstFrame)
			});
		}

		private void RefreshFromRuntime()
		{
			RefreshFromRuntime(false);
		}

		private void RefreshFromRuntime(bool forceSampleRead)
		{
			if (_refreshInProgress || _summaryState == null)
			{
				return;
			}

			_refreshInProgress = true;
			try
			{
				PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
				bool sessionChanged = RequiresSampleRefresh(
					_samplesRead,
					forceSampleRead,
					_lastSessionId,
					_lastSessionState,
					_lastSampleCount,
					_lastStartTimeSeconds,
					_lastFirstFrame,
					summary);
				if (sessionChanged)
				{
					_cachedSamples = PerformanceMeter.GetSessionSamples() ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
					_lastSessionId = summary.SessionId ?? string.Empty;
					_lastSessionState = summary.State;
					_lastSampleCount = summary.SampleCount;
					_lastStartTimeSeconds = summary.StartTimeSeconds;
					_lastFirstFrame = summary.FirstFrame;
					_samplesRead = true;
				}

				if (sessionChanged)
				{
					_model.Rebuild(summary, _cachedSamples);
				}
				else
				{
					_model.RefreshSummary(summary);
				}

				RefreshSummaryPanel();
				RefreshListViews(sessionChanged);
				if (sessionChanged)
				{
					RefreshWorstFramePanel();
				}
			}
			finally
			{
				_refreshInProgress = false;
			}
		}

		private void ForceRefresh()
		{
			RefreshFromRuntime(true);
		}

		internal static bool RequiresSampleRefresh(
			bool samplesRead,
			bool forceSampleRead,
			string lastSessionId,
			PerfMeterSessionState lastSessionState,
			int lastSampleCount,
			double lastStartTimeSeconds,
			int lastFirstFrame,
			PerfMeterSessionSummarySnapshot summary)
		{
			return !samplesRead || forceSampleRead ||
				!string.Equals(lastSessionId, summary.SessionId, StringComparison.Ordinal) ||
				lastSessionState != summary.State ||
				lastSampleCount != summary.SampleCount ||
				!lastStartTimeSeconds.Equals(summary.StartTimeSeconds) ||
				lastFirstFrame != summary.FirstFrame;
		}

		private void RefreshSummaryPanel()
		{
			PerfMeterSessionSummarySnapshot summary = _model.Summary;
			bool hasSession = _model.HasSession;
			_summaryInfo.text = Localize(hasSession
				? "Session data is read-only and refreshes without starting runtime."
				: "No session is available. Start a runtime session to populate analysis.");
			_summaryState.text = hasSession ? summary.State.ToString() : Localize("Idle / No session");
			_summarySessionId.text = hasSession ? PerfMeterSessionAnalysisModel.FormatText(summary.SessionId, PerfMeterSessionAnalysisModel.NoSessionText) : PerfMeterSessionAnalysisModel.NoSessionText;
			_summarySamples.text = PerfMeterSessionAnalysisModel.FormatInteger(hasSession, summary.SampleCount);
			_summaryDroppedSamples.text = PerfMeterSessionAnalysisModel.FormatInteger(hasSession, summary.DroppedSampleCount);
			_summaryDuration.text = PerfMeterSessionAnalysisModel.FormatSeconds(hasSession && _model.HasSamples, summary.DurationSeconds);
			_summaryScenes.text = hasSession
				? PerfMeterSessionAnalysisModel.FormatScene(summary.StartSceneName) + " -> " + PerfMeterSessionAnalysisModel.FormatScene(summary.LastSceneName)
				: PerfMeterSessionAnalysisModel.NoSessionText;
			_summaryFocus.text = PerfMeterSessionAnalysisModel.FormatInteger(hasSession, summary.FocusLossCount);
			_summaryPause.text = hasSession
				? summary.PauseCount.ToString() + " / " + PerfMeterSessionAnalysisModel.FormatSeconds(true, summary.FocusPausedDurationSeconds)
				: PerfMeterSessionAnalysisModel.NoSessionText;
			_summaryWarning.text = hasSession ? PerfMeterSessionAnalysisModel.FormatText(summary.Warning, Localize("None")) : Localize(PerfMeterSessionAnalysisModel.NoSessionText);
		}

		private void RefreshListViews(bool rebuildSampleLists)
		{
			SetEmptyLabel(_timelineEmpty, _model.TimelineRows.Count == 0 ? (_model.HasSession ? "No retained samples." : "No session samples.") : string.Empty);
			SetEmptyLabel(_budgetEmpty, _model.TimelineRows.Count == 0 ? "No samples." : _model.BudgetViolationRows.Count == 0 ? "No budget violations." : string.Empty);
			SetEmptyLabel(_scopeEmpty, _model.ScopeRows.Count == 0 ? "No authoritative scope rows." : string.Empty);
			if (rebuildSampleLists)
			{
				_timelineList?.Rebuild();
				_budgetList?.Rebuild();
			}
			_scopeList?.Rebuild();
		}

		private void RefreshWorstFramePanel()
		{
			PerfMeterSessionWorstFrameDetails details = _model.WorstFrame;
			_worstDetails.Clear();
			if (!details.IsAvailable)
			{
				_worstInfo.text = Localize("No worst frame: no recorded samples.");
				return;
			}

			PerfMeterSessionWorstFrameSnapshot snapshot = details.Snapshot;
			_worstInfo.text = Localize(details.SampleMatched
				? "Worst frame details are available from the retained sample."
				: "Worst frame is in the summary, but its sample is not retained.");
			AddRow(_worstDetails, "Frame", PerfMeterSessionAnalysisModel.FormatFrame(true, snapshot.CollectionFrame));
			AddRow(_worstDetails, "Time", PerfMeterSessionAnalysisModel.FormatSeconds(IsFinite(snapshot.CollectionTimeSeconds), snapshot.CollectionTimeSeconds));
			AddRow(_worstDetails, "Scene", PerfMeterSessionAnalysisModel.FormatScene(snapshot.SceneName));
			AddRow(_worstDetails, "Frame time", PerfMeterSessionAnalysisModel.FormatPositiveMilliseconds(snapshot.FrameTimeMs));
			AddRow(_worstDetails, "FPS", PerfMeterSessionAnalysisModel.FormatPositiveFps(snapshot.Fps));
			AddRow(_worstDetails, "Bottleneck", snapshot.Bottleneck.ToString());

			if (!details.SampleMatched)
			{
				return;
			}

			PerfMeterMetricsSnapshot metrics = details.Metrics;
			AddInfo(_worstDetails, "Matched sample timing");
			AddRow(_worstDetails, "CPU frame", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.CpuTimingAvailable, metrics.CpuFrameTimeMs));
			AddRow(_worstDetails, "CPU main", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.CpuTimingAvailable, metrics.CpuMainThreadFrameTimeMs));
			AddRow(_worstDetails, "CPU render", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.CpuTimingAvailable, metrics.CpuRenderThreadFrameTimeMs));
			AddRow(_worstDetails, "Present wait", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.CpuTimingAvailable, metrics.CpuMainThreadPresentWaitTimeMs));
			AddRow(_worstDetails, "GPU frame", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.GpuTimingAvailable, metrics.GpuFrameTimeMs));
			AddRow(_worstDetails, "Frame budget", PerfMeterSessionAnalysisModel.FormatMilliseconds(details.BudgetAvailable, metrics.FrameBudgetMs));

			AddInfo(_worstDetails, "Matched sample statistics");
			AddRow(_worstDetails, "Frame samples", PerfMeterSessionAnalysisModel.FormatInteger(details.FrameStatsAvailable, metrics.FrameSampleCount));
			AddRow(_worstDetails, "GPU valid samples", PerfMeterSessionAnalysisModel.FormatInteger(details.FrameStatsAvailable, metrics.GpuValidSampleCount));
			AddRow(_worstDetails, "Average FPS", PerfMeterSessionAnalysisModel.FormatFps(details.FrameStatsAvailable, metrics.AverageFps));
			AddRow(_worstDetails, "1% low FPS", PerfMeterSessionAnalysisModel.FormatFps(details.FrameStatsAvailable, metrics.OnePercentLowFps));
			AddRow(_worstDetails, "0.1% low FPS", PerfMeterSessionAnalysisModel.FormatFps(details.FrameStatsAvailable, metrics.PointOnePercentLowFps));
			AddRow(_worstDetails, "Spikes", details.FrameStatsAvailable ? metrics.FrameSpikeCount + " / " + metrics.SevereFrameSpikeCount : PerfMeterSessionAnalysisModel.UnavailableText);

			AddCustomMetricRows(_worstDetails, details.CustomMetrics);
			AddPlatformTelemetryRows(_worstDetails, details.PlatformTelemetry);
			if (!string.IsNullOrEmpty(details.GraphicsStateTraceId))
			{
				AddRow(_worstDetails, "Graphics trace", details.GraphicsStateTraceId);
			}
		}

		private static void AddCustomMetricRows(VisualElement parent, PerfMeterCustomMetricSnapshot[] metrics)
		{
			AddInfo(parent, "Custom metrics");
			if (metrics == null || metrics.Length == 0)
			{
				AddRow(parent, "Values", "None");
				return;
			}

			for (int index = 0; index < metrics.Length; index++)
			{
				PerfMeterCustomMetricSnapshot metric = metrics[index];
				string name = string.IsNullOrEmpty(metric.Name) ? metric.Id : metric.Name;
				string key = Localize("Custom") + ": " + PerfMeterSessionAnalysisModel.FormatText(name, Localize("Metric"));
				AddRawKeyRow(parent, key, PerfMeterSessionAnalysisModel.FormatCustomMetric(metric));
			}
		}

		private static void AddPlatformTelemetryRows(VisualElement parent, PerfMeterPlatformTelemetrySnapshot telemetry)
		{
			AddInfo(parent, "Platform telemetry");
			if (!telemetry.IsAvailable)
			{
				AddRow(parent, "Status", PerfMeterSessionAnalysisModel.UnavailableText);
				return;
			}

			AddRow(parent, "Provider", PerfMeterSessionAnalysisModel.FormatText(telemetry.ProviderId));
			bool added = false;
			if (telemetry.ThermalWarningLevelAvailable)
			{
				AddRow(parent, "Thermal warning", telemetry.ThermalWarningLevel.ToString());
				added = true;
			}
			if (telemetry.TemperatureLevelAvailable)
			{
				AddRow(parent, "Temperature", PerfMeterSessionAnalysisModel.FormatTemperature(true, telemetry.TemperatureLevel));
				added = true;
			}
			if (telemetry.TemperatureTrendAvailable)
			{
				AddRow(parent, "Temperature trend", PerfMeterSessionAnalysisModel.FormatTemperature(true, telemetry.TemperatureTrend));
				added = true;
			}
			if (telemetry.CpuPerformanceLevelAvailable)
			{
				AddRow(parent, "CPU performance level", telemetry.CpuPerformanceLevel.ToString());
				added = true;
			}
			if (telemetry.GpuPerformanceLevelAvailable)
			{
				AddRow(parent, "GPU performance level", telemetry.GpuPerformanceLevel.ToString());
				added = true;
			}
			if (telemetry.PerformanceBottleneckAvailable)
			{
				AddRow(parent, "Adaptive bottleneck", telemetry.PerformanceBottleneck.ToString());
				added = true;
			}
			if (!added)
			{
				AddRow(parent, "Fields", PerfMeterSessionAnalysisModel.UnavailableText);
			}
		}

		private void SelectCurrentTab()
		{
			switch (_currentTab)
			{
				case "Worst Frame":
					SelectWorstFrameTab();
					break;
				case "Budget Violations":
					SelectBudgetTab();
					break;
				case "Scene Scopes":
					SelectScopesTab();
					break;
				default:
					SelectTimelineTab();
					break;
			}
		}

		private void SelectTimelineTab()
		{
			SelectTab("Timeline", _timelineTab, _timelinePanel);
		}

		private void SelectWorstFrameTab()
		{
			SelectTab("Worst Frame", _worstFrameTab, _worstFramePanel);
		}

		private void SelectBudgetTab()
		{
			SelectTab("Budget Violations", _budgetTab, _budgetPanel);
		}

		private void SelectScopesTab()
		{
			SelectTab("Scene Scopes", _scopesTab, _scopesPanel);
		}

		private void SelectTab(string tabName, ToolbarToggle selectedTab, VisualElement selectedPanel)
		{
			_currentTab = tabName;
			SetTabState(_timelineTab, _timelinePanel, selectedTab == _timelineTab, selectedPanel == _timelinePanel);
			SetTabState(_worstFrameTab, _worstFramePanel, selectedTab == _worstFrameTab, selectedPanel == _worstFramePanel);
			SetTabState(_budgetTab, _budgetPanel, selectedTab == _budgetTab, selectedPanel == _budgetPanel);
			SetTabState(_scopesTab, _scopesPanel, selectedTab == _scopesTab, selectedPanel == _scopesPanel);
		}

		private static void SetTabState(ToolbarToggle tab, VisualElement panel, bool selected, bool visible)
		{
			tab?.SetValueWithoutNotify(selected);
			if (panel != null)
			{
				panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			ForceRefresh();
		}

		private static string FormatViolationKind(PerfMeterSessionBudgetViolationKind kind)
		{
			switch (kind)
			{
				case PerfMeterSessionBudgetViolationKind.CpuMainThread:
					return Localize("CPU main");
				case PerfMeterSessionBudgetViolationKind.CpuRenderThread:
					return Localize("CPU render");
				default:
					return Localize("GPU");
			}
		}

		private static string FormatBottlenecks(PerfMeterSessionScopeSummarySnapshot scope, bool available)
		{
			if (!available)
			{
				return PerfMeterSessionAnalysisModel.UnavailableText;
			}

			return "GPU " + scope.GpuBoundSampleCount + ", main " + scope.CpuMainThreadBoundSampleCount + ", render " + scope.CpuRenderThreadBoundSampleCount + ", present " + scope.PresentLimitedSampleCount;
		}

		private static string FormatSpikes(PerfMeterSessionScopeSummarySnapshot scope, bool available)
		{
			return available ? scope.FrameSpikeCount + " / " + scope.SevereFrameSpikeCount : PerfMeterSessionAnalysisModel.UnavailableText;
		}

		private static string FormatWorstFrame(PerfMeterSessionWorstFrameSnapshot worstFrame)
		{
			return worstFrame.IsAvailable
				? PerfMeterSessionAnalysisModel.FormatFrame(true, worstFrame.CollectionFrame) + " / " + PerfMeterSessionAnalysisModel.FormatPositiveMilliseconds(worstFrame.FrameTimeMs)
				: PerfMeterSessionAnalysisModel.UnavailableText;
		}

		private static Label AddEmptyLabel(VisualElement parent)
		{
			Label label = new Label();
			label.AddToClassList("pm-info");
			label.AddToClassList("pm-analysis-empty");
			parent.Add(label);
			return label;
		}

		private static void SetEmptyLabel(Label label, string text)
		{
			if (label == null)
			{
				return;
			}

			label.text = Localize(text ?? string.Empty);
			label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private static VisualElement AddSection(VisualElement parent, string caption)
		{
			VisualElement section = new VisualElement();
			section.AddToClassList("pm-section");
			Label header = new Label(Localize(caption));
			header.AddToClassList("pm-section-caption");
			VisualElement content = new VisualElement();
			content.AddToClassList("pm-section-content");
			section.Add(header);
			section.Add(content);
			parent.Add(section);
			return content;
		}

		private static Label AddRow(VisualElement parent, string key, string value = "")
		{
			return AddInfoRow(parent, Localize(key), value);
		}

		private static Label AddRawKeyRow(VisualElement parent, string key, string value = "")
		{
			return AddInfoRow(parent, key, value);
		}

		private static Label AddInfoRow(VisualElement parent, string key, string value)
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

		private static Label AddCopyRow(VisualElement parent, string key, string value = "")
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("pm-row");
			Label keyLabel = new Label(Localize(key));
			keyLabel.AddToClassList("pm-key");
			Label valueLabel = new Label(value);
			valueLabel.AddToClassList("pm-value");
			row.Add(keyLabel);
			row.Add(valueLabel);
			Button copyButton = new Button(() => CopyValueToClipboard(valueLabel.text))
			{
				text = Localize("Copy"),
				tooltip = Localize("Copy " + key)
			};
			copyButton.AddToClassList("pm-button");
			copyButton.AddToClassList("pm-analysis-copy-button");
			row.Add(copyButton);
			parent.Add(row);
			return valueLabel;
		}

		internal static void CopyValueToClipboard(string value)
		{
			EditorGUIUtility.systemCopyBuffer = value ?? string.Empty;
		}

		private static Label AddInfo(VisualElement parent, string text)
		{
			Label label = new Label(Localize(text));
			label.AddToClassList("pm-info");
			parent.Add(label);
			return label;
		}

		private static VisualElement AddActions(VisualElement parent)
		{
			VisualElement actions = new VisualElement();
			actions.AddToClassList("pm-actions");
			parent.Add(actions);
			return actions;
		}

		private static Button AddButton(VisualElement parent, string text, Action action)
		{
			Button button = new Button(action) { text = Localize(text) };
			button.AddToClassList("pm-button");
			parent.Add(button);
			return button;
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static string Localize(string source)
		{
			return PerfMeterWindowLocalization.Text(source);
		}
	}
}
