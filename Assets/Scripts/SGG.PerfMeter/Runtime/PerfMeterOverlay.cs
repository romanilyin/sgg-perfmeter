using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter
{
	[DisallowMultipleComponent]
	internal sealed class PerfMeterOverlay : MonoBehaviour
	{
		private const float DefaultRefreshIntervalSeconds = 0.25f;
		private const int TextFieldCapacity = 32;
		private const float GraphBlockWidth = 780f;
		private const float TextBlockWidth = 520f;
		private const float TextFieldNameWidth = 118f;
		private const float TextFieldNameGap = 8f;
		private const float ScaleLabelWidth = 64f;
		private const float ScaleLabelGap = 6f;
		private const float ScaleLabelHeight = 13f;
		private const float ScaleLabelSeparation = 3f;
		private const float GraphPlotWidth = 448f;
		private const float LegendWidth = 242f;
		private const float CpuGraphHeight = 86f;
		private const float GpuGraphHeight = 52f;
		private const float FpsOnlyHeight = 36f;
		private const float TextCompactHeight = 260f;
		private const float GraphsTextHeight = 110f;
		private const float FullTextHeight = 430f;
		private const float OverlayMargin = 12f;
		private const float BlockGap = 6f;
		private const float WarningHoldSeconds = 1.25f;
		private const int MaxCustomMetricRows = 8;
		private const int MaxCustomMetricNameLength = 22;
		private const int MaxCustomMetricWarningLength = 52;
		private const float WidgetCardWidth = 144f;
		private const float WidgetCardHeight = 78f;
		private const float WidgetGap = 8f;
		private const float BudgetBarWidth = 372f;
		private const float DiagnosticsWideTextWidth = 780f;
		private const float CompactCardsTextHeight = 260f;
		private const float OverdrawFocusTextHeight = 220f;
		private const float MetricBarsTextHeight = 430f;
		private const float MetricBarsCompactHeight = 230f;
		private const float MetricBarsGraphsHeight = 170f;
		private const int MetricBarFieldCapacity = 32;
		private const float MetricBarRowStride = 18f;
		private const float MetricBarContentVerticalPadding = 16f;
		private const float MetricBarNameWidth = 118f;
		private const float MetricBarRangeWidth = 54f;
		private const float MetricBarTrackWidth = 148f;
		private const float MetricBarCurrentWidth = 78f;

		private static PerfMeterOverlayThemeTokens _activeTheme = PerfMeterOverlayThemeTokens.ClassicDark;
		private static Color BackgroundColor => _activeTheme.Background;
		private static Color TextColor => _activeTheme.Text;
		private static Color MutedTextColor => _activeTheme.MutedText;
		private static Color FrameColor => _activeTheme.Frame;
		private static Color OtherCpuColor => _activeTheme.CpuOther;
		private static Color MainColor => _activeTheme.CpuMain;
		private static Color RenderColor => _activeTheme.CpuRender;
		private static Color GpuColor => _activeTheme.Gpu;
		private static Color WarningColor => _activeTheme.Warning;
		private static Color AccentColor => _activeTheme.Accent;
		private static Color UnavailableColor => _activeTheme.Unavailable;
		private static Color GraphBackgroundColor => _activeTheme.GraphBackground;
		private static Color GridColor => _activeTheme.Grid;
		private static Color BudgetColor => _activeTheme.Budget;
		private static PerfMeterOverlayFontFamily _activeFontFamily = PerfMeterOverlayFontFamily.Manrope;
		private static PerfMeterOverlayFontResources _fontResources;
		private static Font _legacyRuntimeFont;

		private readonly StringBuilder _valueBuilder = new StringBuilder(256);
		private readonly StringBuilder _barStatsBuilder = new StringBuilder(192);
		private readonly StringBuilder _barNameBuilder = new StringBuilder(64);
		private readonly StringBuilder _barMinBuilder = new StringBuilder(32);
		private readonly StringBuilder _barMaxBuilder = new StringBuilder(32);
		private readonly char[] _numberBuffer = new char[64];
		private readonly PerfMeterOverlayTextField[] _textFields = new PerfMeterOverlayTextField[TextFieldCapacity];
		private readonly PerfMeterStatBarField[] _barFields = new PerfMeterStatBarField[MetricBarFieldCapacity];
		private readonly PerfMeterOverlayHistory _history = new PerfMeterOverlayHistory();
		private UIDocument _document;
		private PanelSettings _panelSettings;
		private PanelTextSettings _panelTextSettings;
		private ThemeStyleSheet _themeStyleSheet;
		private UnityEngine.TextCore.Text.FontAsset _fontAsset;
		private VisualElement _container;
		private VisualElement _widgetBlock;
		private VisualElement _graphBlock;
		private VisualElement _textBlock;
		private VisualElement _textRows;
		private VisualElement _barRows;
		private VisualElement _graphs;
		private Label _cpuFrameLegend;
		private Label _cpuMainLegend;
		private Label _cpuRenderLegend;
		private Label _cpuOtherLegend;
		private Label _gpuLegend;
		private PerfMeterMetricCard _fpsCard;
		private PerfMeterMetricCard _cpuCard;
		private PerfMeterMetricCard _gpuCard;
		private PerfMeterMetricCard _spikeCard;
		private PerfMeterMetricCard _overdrawCard;
		private PerfMeterBudgetBar _cpuBudgetBar;
		private PerfMeterBudgetBar _gpuBudgetBar;
		private PerfMeterGraphElement _cpuGraph;
		private PerfMeterGraphElement _gpuGraph;
		private float _nextRefreshTime;
		private float _warningVisibleUntil;
		private string _heldWarning = string.Empty;
		private string _pendingTextFieldName = string.Empty;
		private PerfMeterOverlayCorner _corner = PerfMeterOverlayCorner.TopRight;
		private PerfMeterOverlayMode _mode = PerfMeterOverlayMode.Full;
		private PerfMeterOverlayTheme _theme = PerfMeterOverlayTheme.ClassicDark;
		private PerfMeterOverlayLayout _layout = PerfMeterOverlayLayout.Classic;
		private PerfMeterOverlayFontFamily _fontFamily = PerfMeterOverlayFontFamily.Manrope;
		private PerfMeterOverlayModule _modules = PerfMeterOverlayModule.All;
		private float _overlayScale = 1f;
		private float _overlayOpacity = 0.84f;
		private float _overlayFontSize = 12f;
		private float _refreshIntervalSeconds = DefaultRefreshIntervalSeconds;
		private int _graphHistoryLength = 120;
		private int _textFieldCount;
		private int _lastVisibleTextFieldCount;
		private int _barFieldCount;
		private int _barFieldLimit = MetricBarFieldCapacity;
		private int _lastVisibleBarFieldCount;
		private int _lastRecordedCollectionFrame = -1;
		private bool _isVisible = true;

		internal bool IsVisible => _isVisible && isActiveAndEnabled;
		internal int ActiveTextFieldCount => _textFieldCount + _barFieldCount;

		private void Awake()
		{
			EnsureDocument();
		}

		private void OnEnable()
		{
			EnsureDocument();
			BuildVisualTree();
			ApplyVisibility();
			RefreshText(force: true);
		}

		private void Update()
		{
			if (!_isVisible || Time.unscaledTime < _nextRefreshTime)
			{
				return;
			}

			RefreshText(force: false);
		}

		private void OnDestroy()
		{
			if (_themeStyleSheet != null)
			{
				Destroy(_themeStyleSheet);
			}

			if (_fontAsset != null)
			{
				Destroy(_fontAsset);
			}

			if (_panelTextSettings != null)
			{
				Destroy(_panelTextSettings);
			}

			if (_panelSettings != null)
			{
				Destroy(_panelSettings);
			}
		}

		internal void SetVisible(bool visible)
		{
			_isVisible = visible;
			ApplyVisibility();

			if (visible)
			{
				RefreshText(force: true);
			}
		}

		internal void SetCorner(PerfMeterOverlayCorner corner)
		{
			_corner = corner;
			ApplyCorner();
			ApplyBlockAlignment();
		}

		internal void SetMode(PerfMeterOverlayMode mode)
		{
			if (_mode == mode)
			{
				return;
			}

			_mode = mode;
			ApplyModeLayout();

			if (_isVisible)
			{
				RefreshText(force: true);
			}
		}

		internal void SetTheme(PerfMeterOverlayTheme theme)
		{
			PerfMeterOverlayTheme normalized = PerfMeterSettingsStore.NormalizeOverlayTheme(theme);
			if (_theme == normalized)
			{
				return;
			}

			_theme = normalized;
			SetActiveTheme(_theme);
			RebuildVisualTree();
		}

		internal void SetLayout(PerfMeterOverlayLayout layout)
		{
			PerfMeterOverlayLayout normalized = PerfMeterSettingsStore.NormalizeOverlayLayout(layout);
			if (_layout == normalized)
			{
				return;
			}

			_layout = normalized;
			ApplyModeLayout();

			if (_isVisible)
			{
				RefreshText(force: true);
			}
		}

		internal void SetFontFamily(PerfMeterOverlayFontFamily fontFamily)
		{
			PerfMeterOverlayFontFamily normalized = PerfMeterSettingsStore.NormalizeOverlayFontFamily(fontFamily);
			if (_fontFamily == normalized)
			{
				return;
			}

			_fontFamily = normalized;
			SetActiveFontFamily(_fontFamily);
			RebuildVisualTree();
		}

		internal void SetModules(PerfMeterOverlayModule modules)
		{
			PerfMeterOverlayModule normalized = modules == PerfMeterOverlayModule.None ? PerfMeterOverlayModule.All : modules;
			if (_modules == normalized)
			{
				return;
			}

			_modules = normalized;
			ApplyModeLayout();

			if (_isVisible)
			{
				RefreshText(force: true);
			}
		}

		internal void SetTargetFps(PerfMeterTargetFps targetFps)
		{
			double frameBudgetMs = PerfMeterRuntime.GetFrameBudgetMs(targetFps);
			if (_cpuGraph != null)
			{
				_cpuGraph.SetFrameBudgetMs(frameBudgetMs);
			}

			if (_gpuGraph != null)
			{
				_gpuGraph.SetFrameBudgetMs(frameBudgetMs);
			}
		}

		internal void SetTuning(float scale, float opacity, float fontSize, float refreshIntervalSeconds, int graphHistoryLength)
		{
			_overlayScale = Mathf.Clamp(scale, PerfMeterSettingsStore.MinOverlayScale, PerfMeterSettingsStore.MaxOverlayScale);
			_overlayOpacity = Mathf.Clamp(opacity, PerfMeterSettingsStore.MinOverlayOpacity, PerfMeterSettingsStore.MaxOverlayOpacity);
			_overlayFontSize = Mathf.Clamp(fontSize, PerfMeterSettingsStore.MinOverlayFontSize, PerfMeterSettingsStore.MaxOverlayFontSize);
			_refreshIntervalSeconds = Mathf.Clamp(refreshIntervalSeconds, PerfMeterSettingsStore.MinOverlayRefreshIntervalSeconds, PerfMeterSettingsStore.MaxOverlayRefreshIntervalSeconds);
			_graphHistoryLength = Mathf.Clamp(graphHistoryLength, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength);

			ApplyTuningToVisuals();
			_cpuGraph?.SetHistoryCapacity(_graphHistoryLength);
			_gpuGraph?.SetHistoryCapacity(_graphHistoryLength);
		}

		private void EnsureDocument()
		{
			if (_document == null)
			{
				_document = GetComponent<UIDocument>();
				if (_document == null)
				{
					_document = gameObject.AddComponent<UIDocument>();
				}
			}

			if (_panelSettings == null)
			{
				_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
				_panelSettings.name = "SGG PerfMeter Panel Settings";
				_panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
				_panelSettings.sortingOrder = short.MaxValue;
				_panelSettings.hideFlags = HideFlags.DontSave;
			}

			if (_panelSettings.textSettings == null)
			{
				_panelSettings.textSettings = CreatePanelTextSettings();
			}

			if (_panelSettings.themeStyleSheet == null)
			{
				_panelSettings.themeStyleSheet = ResolveRuntimeThemeStyleSheet() ?? CreateGeneratedThemeStyleSheet();
			}

			_document.panelSettings = _panelSettings;
		}

		private void BuildVisualTree()
		{
			VisualElement root = _document.rootVisualElement;
			if (root == null || _container != null)
			{
				return;
			}

			root.pickingMode = PickingMode.Ignore;
			root.style.position = Position.Absolute;
			root.style.left = 0f;
			root.style.top = 0f;
			root.style.right = 0f;
			root.style.bottom = 0f;

			_container = new VisualElement
			{
				name = "sgg-perfmeter-overlay",
				pickingMode = PickingMode.Ignore
			};
			_container.style.position = Position.Absolute;
			_container.style.width = GraphBlockWidth;
			_container.style.flexDirection = FlexDirection.Column;
			SetActiveTheme(_theme);
			SetActiveFontFamily(_fontFamily);
			_container.style.unityFont = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);

			_widgetBlock = CreateBlock("sgg-perfmeter-widget-block", GraphBlockWidth);
			_widgetBlock.style.marginBottom = BlockGap;
			BuildWidgetRows();

			_graphBlock = CreateBlock("sgg-perfmeter-graph-block", GraphBlockWidth);
			_graphBlock.style.marginBottom = BlockGap;
			_graphs = new VisualElement
			{
				name = "sgg-perfmeter-graphs",
				pickingMode = PickingMode.Ignore
			};
			_graphs.style.flexDirection = FlexDirection.Column;
			BuildGraphRows();
			_graphBlock.Add(_graphs);

			_textBlock = CreateBlock("sgg-perfmeter-text-block", TextBlockWidth);
			_textRows = new VisualElement
			{
				name = "sgg-perfmeter-metrics",
				pickingMode = PickingMode.Ignore
			};
			_textRows.style.width = Length.Percent(100f);
			_textRows.style.flexGrow = 1f;
			_textRows.style.flexDirection = FlexDirection.Column;
			_textRows.style.unityFont = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);
			_textRows.style.unityFontStyleAndWeight = FontStyle.Normal;
			_textBlock.Add(_textRows);

			_barRows = new VisualElement
			{
				name = "sgg-perfmeter-metric-bars",
				pickingMode = PickingMode.Ignore
			};
			_barRows.style.width = Length.Percent(100f);
			_barRows.style.flexGrow = 1f;
			_barRows.style.flexDirection = FlexDirection.Column;
			_barRows.style.unityFont = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);
			_barRows.style.display = DisplayStyle.None;
			_textBlock.Add(_barRows);

			_container.Add(_widgetBlock);
			_container.Add(_graphBlock);
			_container.Add(_textBlock);
			root.Add(_container);
			ApplyModeLayout();
			ApplyCorner();
		}

		private void RebuildVisualTree()
		{
			if (_document == null || _document.rootVisualElement == null)
			{
				return;
			}

			_document.rootVisualElement.Clear();
			_container = null;
			_widgetBlock = null;
			_graphBlock = null;
			_textBlock = null;
			_textRows = null;
			_barRows = null;
			_graphs = null;
			_fpsCard = null;
			_cpuCard = null;
			_gpuCard = null;
			_spikeCard = null;
			_overdrawCard = null;
			_cpuBudgetBar = null;
			_gpuBudgetBar = null;
			_cpuGraph = null;
			_gpuGraph = null;
			_cpuFrameLegend = null;
			_cpuMainLegend = null;
			_cpuRenderLegend = null;
			_cpuOtherLegend = null;
			_gpuLegend = null;
			_textFieldCount = 0;
			_lastVisibleTextFieldCount = 0;
			_barFieldCount = 0;
			_lastVisibleBarFieldCount = 0;
			Array.Clear(_textFields, 0, _textFields.Length);
			Array.Clear(_barFields, 0, _barFields.Length);

			BuildVisualTree();
			ApplyVisibility();
			if (_isVisible)
			{
				RefreshText(force: true);
			}
		}

		private VisualElement CreateBlock(string name, float width)
		{
			VisualElement block = new VisualElement
			{
				name = name,
				pickingMode = PickingMode.Ignore
			};
			block.style.width = width;
			block.style.paddingLeft = 10f;
			block.style.paddingRight = 10f;
			block.style.paddingTop = 8f;
			block.style.paddingBottom = 8f;
			block.style.backgroundColor = GetBackgroundColor();
			block.style.flexDirection = FlexDirection.Column;
			block.style.unityFont = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);
			return block;
		}

		private void BuildWidgetRows()
		{
			VisualElement cardRow = CreateWidgetRow("sgg-perfmeter-widget-cards");
			_fpsCard = CreateMetricCard("FPS", AccentColor);
			_cpuCard = CreateMetricCard("CPU", FrameColor);
			_gpuCard = CreateMetricCard("GPU", GpuColor);
			_spikeCard = CreateMetricCard("Spikes", WarningColor);
			_overdrawCard = CreateMetricCard("Overdraw", MainColor);
			cardRow.Add(_fpsCard);
			cardRow.Add(_cpuCard);
			cardRow.Add(_gpuCard);
			cardRow.Add(_spikeCard);
			cardRow.Add(_overdrawCard);
			_widgetBlock.Add(cardRow);

			VisualElement budgetRow = CreateWidgetRow("sgg-perfmeter-widget-bars");
			budgetRow.style.marginBottom = 0f;
			_cpuBudgetBar = CreateBudgetBar("CPU budget", FrameColor);
			_gpuBudgetBar = CreateBudgetBar("GPU budget", GpuColor);
			budgetRow.Add(_cpuBudgetBar);
			budgetRow.Add(_gpuBudgetBar);
			_widgetBlock.Add(budgetRow);
		}

		private static VisualElement CreateWidgetRow(string name)
		{
			VisualElement row = new VisualElement
			{
				name = name,
				pickingMode = PickingMode.Ignore
			};
			row.style.flexDirection = FlexDirection.Row;
			row.style.alignItems = Align.Stretch;
			row.style.marginBottom = WidgetGap;
			return row;
		}

		private static PerfMeterMetricCard CreateMetricCard(string title, Color accent)
		{
			return new PerfMeterMetricCard(title, accent, GetRuntimeFont(PerfMeterOverlayFontRole.Medium), GetRuntimeFont(PerfMeterOverlayFontRole.Bold), GetRuntimeFont(PerfMeterOverlayFontRole.Regular));
		}

		private static PerfMeterBudgetBar CreateBudgetBar(string title, Color accent)
		{
			return new PerfMeterBudgetBar(title, accent, GetRuntimeFont(PerfMeterOverlayFontRole.Medium), GetRuntimeFont(PerfMeterOverlayFontRole.SemiBold));
		}

		private void BuildGraphRows()
		{
			VisualElement cpuHeader = CreateHeaderRow("CPU", new LegendToken("frame", FrameColor), new LegendToken("other", OtherCpuColor), new LegendToken("main", MainColor), new LegendToken("render", RenderColor));
			_graphs.Add(cpuHeader);

			VisualElement cpuRow = CreateGraphRow(CpuGraphHeight);
			Label cpuMaxScaleLabel;
			Label cpuBudgetLabel;
			VisualElement cpuScale = CreateScaleLabelColumn(CpuGraphHeight, out cpuMaxScaleLabel, out cpuBudgetLabel);
			_cpuGraph = new PerfMeterGraphElement("sgg-perfmeter-cpu-graph", PerfMeterGraphMode.StackedCpu, CpuGraphHeight, cpuMaxScaleLabel, cpuBudgetLabel, _graphHistoryLength);
			VisualElement cpuLegend = CreateLegendColumn(CpuGraphHeight);
			_cpuFrameLegend = CreateLegendLine("frame --", FrameColor);
			_cpuOtherLegend = CreateLegendLine("other --", OtherCpuColor);
			_cpuMainLegend = CreateLegendLine("main --", MainColor);
			_cpuRenderLegend = CreateLegendLine("render --", RenderColor);
			cpuLegend.Add(_cpuFrameLegend);
			cpuLegend.Add(_cpuOtherLegend);
			cpuLegend.Add(_cpuMainLegend);
			cpuLegend.Add(_cpuRenderLegend);
			cpuRow.Add(cpuScale);
			cpuRow.Add(_cpuGraph);
			cpuRow.Add(cpuLegend);
			_graphs.Add(cpuRow);

			VisualElement gpuHeader = CreateHeaderRow("GPU", new LegendToken("gpu", GpuColor));
			_graphs.Add(gpuHeader);

			VisualElement gpuRow = CreateGraphRow(GpuGraphHeight);
			Label gpuMaxScaleLabel;
			Label gpuBudgetLabel;
			VisualElement gpuScale = CreateScaleLabelColumn(GpuGraphHeight, out gpuMaxScaleLabel, out gpuBudgetLabel);
			_gpuGraph = new PerfMeterGraphElement("sgg-perfmeter-gpu-graph", PerfMeterGraphMode.Line, GpuGraphHeight, gpuMaxScaleLabel, gpuBudgetLabel, _graphHistoryLength);
			VisualElement gpuLegend = CreateLegendColumn(GpuGraphHeight);
			_gpuLegend = CreateLegendLine("gpu --", GpuColor);
			gpuLegend.Add(_gpuLegend);
			gpuRow.Add(gpuScale);
			gpuRow.Add(_gpuGraph);
			gpuRow.Add(gpuLegend);
			_graphs.Add(gpuRow);
		}

		private static VisualElement CreateHeaderRow(string title, params LegendToken[] tokens)
		{
			VisualElement row = new VisualElement
			{
				pickingMode = PickingMode.Ignore
			};
			row.style.flexDirection = FlexDirection.Row;
			row.style.height = 18f;
			row.style.alignItems = Align.Center;

			Label titleLabel = CreateSmallLabel(title + ":", TextColor, TextAnchor.MiddleLeft, PerfMeterOverlayFontRole.Medium);
			titleLabel.style.marginRight = 5f;
			row.Add(titleLabel);

			for (int i = 0; i < tokens.Length; i++)
			{
				Label tokenLabel = CreateSmallLabel(tokens[i].Name, tokens[i].Color, TextAnchor.MiddleLeft, PerfMeterOverlayFontRole.Medium);
				tokenLabel.style.marginRight = 6f;
				row.Add(tokenLabel);
			}

			return row;
		}

		private static VisualElement CreateGraphRow(float height)
		{
			VisualElement row = new VisualElement
			{
				pickingMode = PickingMode.Ignore
			};
			row.style.flexDirection = FlexDirection.Row;
			row.style.height = height;
			row.style.alignItems = Align.FlexEnd;
			row.style.marginBottom = 4f;
			return row;
		}

		private static VisualElement CreateScaleLabelColumn(float height, out Label maxScaleLabel, out Label budgetLabel)
		{
			VisualElement column = new VisualElement
			{
				pickingMode = PickingMode.Ignore
			};
			column.style.position = Position.Relative;
			column.style.width = ScaleLabelWidth;
			column.style.height = height;
			column.style.marginRight = ScaleLabelGap;

			maxScaleLabel = CreateScaleLabel(MutedTextColor);
			budgetLabel = CreateScaleLabel(WithAlpha(BudgetColor, 0.95f));
			column.Add(maxScaleLabel);
			column.Add(budgetLabel);
			return column;
		}

		private static Label CreateScaleLabel(Color color)
		{
			Label label = new Label
			{
				pickingMode = PickingMode.Ignore
			};
			label.style.position = Position.Absolute;
			label.style.left = 0f;
			label.style.width = ScaleLabelWidth;
			label.style.height = ScaleLabelHeight;
			label.style.fontSize = 10f;
			label.style.unityFont = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);
			label.style.color = color;
			label.style.unityTextAlign = TextAnchor.MiddleRight;
			label.style.whiteSpace = WhiteSpace.NoWrap;
			return label;
		}

		private static VisualElement CreateLegendColumn(float height)
		{
			VisualElement legend = new VisualElement
			{
				pickingMode = PickingMode.Ignore
			};
			legend.style.width = LegendWidth;
			legend.style.height = height;
			legend.style.paddingLeft = 8f;
			legend.style.flexDirection = FlexDirection.Column;
			legend.style.justifyContent = Justify.FlexEnd;
			return legend;
		}

		private static Label CreateLegendLine(string text, Color color)
		{
			Label label = CreateSmallLabel(text, GetReadableTextColor(color), TextAnchor.MiddleLeft, PerfMeterOverlayFontRole.Medium);
			label.style.height = 17f;
			label.style.fontSize = 9.5f;
			label.style.marginTop = 2f;
			label.style.paddingLeft = 4f;
			label.style.paddingRight = 4f;
			label.style.backgroundColor = color;
			return label;
		}

		private static Label CreateSmallLabel(string text, Color color, TextAnchor align, PerfMeterOverlayFontRole role = PerfMeterOverlayFontRole.Regular)
		{
			Label label = new Label(text)
			{
				pickingMode = PickingMode.Ignore
			};
			label.style.color = color;
			label.style.fontSize = 11f;
			label.style.unityFont = GetRuntimeFont(role);
			label.style.unityTextAlign = align;
			label.style.whiteSpace = WhiteSpace.NoWrap;
			return label;
		}

		private PerfMeterOverlayTextField CreateTextField(int index)
		{
			VisualElement row = new VisualElement
			{
				name = "sgg-perfmeter-field-" + index.ToString(CultureInfo.InvariantCulture),
				pickingMode = PickingMode.Ignore
			};
			row.style.flexDirection = FlexDirection.Row;
			row.style.width = Length.Percent(100f);
			row.style.minHeight = 16f;

			Label nameLabel = CreateSmallLabel(string.Empty, MutedTextColor, TextAnchor.UpperLeft, PerfMeterOverlayFontRole.Medium);
			nameLabel.style.width = TextFieldNameWidth;
			nameLabel.style.marginRight = TextFieldNameGap;
			nameLabel.style.flexShrink = 0f;

			Label valueLabel = CreateSmallLabel(string.Empty, TextColor, TextAnchor.UpperLeft, PerfMeterOverlayFontRole.SemiBold);
			valueLabel.style.flexGrow = 1f;
			valueLabel.style.whiteSpace = WhiteSpace.Normal;

			row.Add(nameLabel);
			row.Add(valueLabel);
			_textRows.Add(row);

			PerfMeterOverlayTextField field = new PerfMeterOverlayTextField(row, nameLabel, valueLabel);
			field.SetFontSize(GetTextFontSize());
			field.SetFonts(GetRuntimeFont(PerfMeterOverlayFontRole.Medium), GetRuntimeFont(PerfMeterOverlayFontRole.SemiBold));
			return field;
		}

		private PerfMeterStatBarField CreateBarField(int index)
		{
			PerfMeterStatBarField field = new PerfMeterStatBarField(
				"sgg-perfmeter-bar-" + index.ToString(CultureInfo.InvariantCulture),
				GetRuntimeFont(PerfMeterOverlayFontRole.Medium),
				GetRuntimeFont(PerfMeterOverlayFontRole.SemiBold),
				GetRuntimeFont(PerfMeterOverlayFontRole.Regular));
			field.SetFontSize(GetBarFontSize());
			_barRows.Add(field.Root);
			return field;
		}

		private void ApplyModeLayout()
		{
			if (_container == null)
			{
				return;
			}

			bool showWidgets = ShouldShowWidgetBlock();
			bool showGraphs = (_mode == PerfMeterOverlayMode.Graphs || _mode == PerfMeterOverlayMode.Full) && HasModule(PerfMeterOverlayModule.Graphs);
			if (_widgetBlock != null)
			{
				_widgetBlock.style.display = showWidgets ? DisplayStyle.Flex : DisplayStyle.None;
				_widgetBlock.style.height = showWidgets ? StyleKeyword.Auto : 0f;
				_widgetBlock.style.width = GraphBlockWidth;
			}

			_graphBlock.style.display = showGraphs ? DisplayStyle.Flex : DisplayStyle.None;

			float textWidth = GetTextBlockWidth();
			float containerWidth = showGraphs || showWidgets ? GraphBlockWidth : textWidth;
			_container.style.width = containerWidth;
			_textBlock.style.width = textWidth;
			_textBlock.style.height = GetTextBlockHeight();
			if (showGraphs)
			{
				_graphBlock.style.height = StyleKeyword.Auto;
			}
			else
			{
				_graphBlock.style.height = 0f;
			}


			if (_textRows != null)
			{
				_textRows.style.justifyContent = _mode == PerfMeterOverlayMode.FpsOnly ? Justify.Center : Justify.FlexStart;
				SetTextFieldFontSize(GetTextFontSize());
			}

			ApplyWidgetLayout();

			ApplyBlockAlignment();
			ApplyTuningToVisuals();
		}

		private bool ShouldShowWidgetBlock()
		{
			return _layout != PerfMeterOverlayLayout.Classic && (HasModule(PerfMeterOverlayModule.Fps) || HasModule(PerfMeterOverlayModule.Timing) || HasModule(PerfMeterOverlayModule.Overdraw) || HasModule(PerfMeterOverlayModule.Heatmap));
		}

		private bool ShouldUseMetricBarTextBlock()
		{
			return _layout == PerfMeterOverlayLayout.MetricBars && _mode != PerfMeterOverlayMode.FpsOnly;
		}

		private float GetTextBlockWidth()
		{
			return _layout == PerfMeterOverlayLayout.DiagnosticsWide || _layout == PerfMeterOverlayLayout.MetricBars ? DiagnosticsWideTextWidth : TextBlockWidth;
		}

		private void ApplyWidgetLayout()
		{
			if (_widgetBlock == null)
			{
				return;
			}

			bool timing = HasModule(PerfMeterOverlayModule.Timing);
			bool overdraw = HasModule(PerfMeterOverlayModule.Overdraw) || HasModule(PerfMeterOverlayModule.Heatmap);
			SetWidgetVisible(_fpsCard, HasModule(PerfMeterOverlayModule.Fps));
			SetWidgetVisible(_cpuCard, timing && _layout != PerfMeterOverlayLayout.OverdrawFocus);
			SetWidgetVisible(_gpuCard, timing && _layout != PerfMeterOverlayLayout.OverdrawFocus);
			SetWidgetVisible(_spikeCard, HasModule(PerfMeterOverlayModule.Fps) && _layout != PerfMeterOverlayLayout.OverdrawFocus);
			SetWidgetVisible(_overdrawCard, overdraw || _layout == PerfMeterOverlayLayout.OverdrawFocus);
			SetWidgetVisible(_cpuBudgetBar, timing);
			SetWidgetVisible(_gpuBudgetBar, timing);
		}

		private static void SetWidgetVisible(VisualElement element, bool visible)
		{
			if (element != null)
			{
				element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		private float GetTextFontSize()
		{
			return _mode == PerfMeterOverlayMode.Full ? _overlayFontSize : _overlayFontSize + 1f;
		}

		private void ApplyTuningToVisuals()
		{
			if (_container != null)
			{
				_container.style.scale = new StyleScale(new Vector2(_overlayScale, _overlayScale));
			}

			Color background = GetBackgroundColor();
			if (_widgetBlock != null)
			{
				_widgetBlock.style.backgroundColor = background;
			}

			if (_graphBlock != null)
			{
				_graphBlock.style.backgroundColor = background;
			}

			if (_textBlock != null)
			{
				_textBlock.style.backgroundColor = background;
			}

			SetTextFieldFontSize(GetTextFontSize());
			SetBarFieldFontSize(GetBarFontSize());
			SetWidgetFontSizes();
		}

		private Color GetBackgroundColor()
		{
			return new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, _overlayOpacity);
		}

		private void SetTextFieldFontSize(float fontSize)
		{
			for (int i = 0; i < _textFields.Length; i++)
			{
				_textFields[i]?.SetFontSize(fontSize);
			}
		}

		private void SetWidgetFontSizes()
		{
			float small = Mathf.Max(9f, _overlayFontSize - 1f);
			float value = Mathf.Max(18f, _overlayFontSize + 13f);
			float caption = Mathf.Max(9f, _overlayFontSize - 2f);
			_fpsCard?.SetFontSizes(small, value + 5f, caption);
			_cpuCard?.SetFontSizes(small, value, caption);
			_gpuCard?.SetFontSizes(small, value, caption);
			_spikeCard?.SetFontSizes(small, value, caption);
			_overdrawCard?.SetFontSizes(small, value, caption);
			_cpuBudgetBar?.SetFontSize(small);
			_gpuBudgetBar?.SetFontSize(small);
		}

		private void SetBarFieldFontSize(float fontSize)
		{
			for (int i = 0; i < _barFields.Length; i++)
			{
				_barFields[i]?.SetFontSize(fontSize);
			}
		}

		private float GetBarFontSize()
		{
			return Mathf.Max(9f, _overlayFontSize - 1f);
		}

		private float GetTextBlockHeight()
		{
			if (_layout == PerfMeterOverlayLayout.MetricBars)
			{
				switch (_mode)
				{
					case PerfMeterOverlayMode.TextCompact:
						return MetricBarsCompactHeight;
					case PerfMeterOverlayMode.Graphs:
						return MetricBarsGraphsHeight;
					case PerfMeterOverlayMode.FpsOnly:
						return FpsOnlyHeight;
					default:
						return MetricBarsTextHeight;
				}
			}

			if (_layout == PerfMeterOverlayLayout.CompactCards && _mode == PerfMeterOverlayMode.Full)
			{
				return CompactCardsTextHeight;
			}

			if (_layout == PerfMeterOverlayLayout.OverdrawFocus && _mode == PerfMeterOverlayMode.Full)
			{
				return OverdrawFocusTextHeight;
			}

			switch (_mode)
			{
				case PerfMeterOverlayMode.FpsOnly:
					return FpsOnlyHeight;
				case PerfMeterOverlayMode.TextCompact:
					return TextCompactHeight;
				case PerfMeterOverlayMode.Graphs:
					return GraphsTextHeight;
				default:
					return FullTextHeight;
			}
		}

		private int GetMetricBarFieldLimit()
		{
			float contentHeight = Mathf.Max(0f, GetTextBlockHeight() - MetricBarContentVerticalPadding);
			int visibleRows = Mathf.FloorToInt(contentHeight / MetricBarRowStride);
			return Mathf.Clamp(visibleRows, 1, MetricBarFieldCapacity);
		}

		private bool HasAvailableBarField()
		{
			return _barFieldCount < _barFields.Length && _barFieldCount < _barFieldLimit;
		}

		private void ApplyBlockAlignment()
		{
			if (_textBlock == null || _graphBlock == null || _widgetBlock == null)
			{
				return;
			}

			bool rightAligned = _corner == PerfMeterOverlayCorner.TopRight || _corner == PerfMeterOverlayCorner.BottomRight;
			Align align = rightAligned ? Align.FlexEnd : Align.FlexStart;
			_textBlock.style.alignSelf = align;
			_widgetBlock.style.alignSelf = Align.FlexStart;
			_graphBlock.style.alignSelf = Align.FlexStart;
		}

		private void ApplyCorner()
		{
			if (_container == null)
			{
				return;
			}

			_container.style.left = StyleKeyword.Auto;
			_container.style.right = StyleKeyword.Auto;
			_container.style.top = StyleKeyword.Auto;
			_container.style.bottom = StyleKeyword.Auto;

			switch (_corner)
			{
				case PerfMeterOverlayCorner.TopLeft:
					_container.style.left = OverlayMargin;
					_container.style.top = OverlayMargin;
					break;
				case PerfMeterOverlayCorner.BottomLeft:
					_container.style.left = OverlayMargin;
					_container.style.bottom = OverlayMargin;
					break;
				case PerfMeterOverlayCorner.BottomRight:
					_container.style.right = OverlayMargin;
					_container.style.bottom = OverlayMargin;
					break;
				default:
					_container.style.right = OverlayMargin;
					_container.style.top = OverlayMargin;
					break;
			}
		}

		private void ApplyVisibility()
		{
			if (_container != null)
			{
				_container.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		private void RefreshText(bool force)
		{
			if (_textRows == null)
			{
				return;
			}

			if (!force && Time.unscaledTime < _nextRefreshTime)
			{
				return;
			}

			_nextRefreshTime = Time.unscaledTime + _refreshIntervalSeconds;
			PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
			string warning = ResolveDisplayWarning(status.Warning);
			bool recordSample = ShouldRecordOverlaySample(status, metrics);
			if (recordSample)
			{
				_history.AddSample(metrics, status);
			}

			if (HasModule(PerfMeterOverlayModule.Graphs))
			{
				UpdateGraphs(status, metrics, recordSample);
			}

			UpdateMetricWidgets(status, metrics, warning);

			if (ShouldUseMetricBarTextBlock())
			{
				_textRows.style.display = DisplayStyle.None;
				_barRows.style.display = DisplayStyle.Flex;
				_textFieldCount = 0;
				HideUnusedTextFields();
				_barFieldCount = 0;
				_barFieldLimit = GetMetricBarFieldLimit();
				BuildMetricBarText(status, metrics, warning);
				HideUnusedBarFields();
				return;
			}

			_textRows.style.display = DisplayStyle.Flex;
			_barRows.style.display = DisplayStyle.None;
			_barFieldCount = 0;
			_barFieldLimit = MetricBarFieldCapacity;
			HideUnusedBarFields();
			_textFieldCount = 0;
			switch (_mode)
			{
				case PerfMeterOverlayMode.FpsOnly:
					BuildFpsOnlyText(metrics, warning);
					break;
				case PerfMeterOverlayMode.TextCompact:
					BuildTextCompactText(status, metrics, warning);
					break;
				case PerfMeterOverlayMode.Graphs:
					BuildGraphsText(status, metrics, warning);
					break;
				default:
					BuildFullText(status, metrics, warning);
					break;
			}

			HideUnusedTextFields();
		}

		private bool ShouldRecordOverlaySample(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics)
		{
			if (metrics.CollectionFrame < 0 || metrics.CollectionFrame == _lastRecordedCollectionFrame)
			{
				return false;
			}

			if (status.FrameTimingAvailability != PerfMeterFrameTimingAvailability.Available || !PerfMeterCollector.IsValidFrameTimingSampleMs(metrics.CpuFrameTimeMs))
			{
				return false;
			}

			_lastRecordedCollectionFrame = metrics.CollectionFrame;
			return true;
		}

		private void UpdateGraphs(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, bool recordSample)
		{
			if (_cpuGraph == null || _gpuGraph == null)
			{
				return;
			}

			double frameBudgetMs = PerfMeterRuntime.GetFrameBudgetMs(status.TargetFps);
			_cpuGraph.SetFrameBudgetMs(frameBudgetMs);
			_gpuGraph.SetFrameBudgetMs(frameBudgetMs);

			if (recordSample)
			{
				_cpuGraph.AddSample(
					metrics.CpuFrameTimeMs,
					metrics.CpuMainThreadFrameTimeMs,
					metrics.CpuRenderThreadFrameTimeMs,
					true);
				if (metrics.GpuFrameTimeAvailable && PerfMeterCollector.IsValidFrameTimingSampleMs(metrics.GpuFrameTimeMs))
				{
					_gpuGraph.AddSample(metrics.GpuFrameTimeMs, 0d, 0d, true);
				}
			}

			PerfMeterSeriesStats frameStats = _cpuGraph.GetStats(0);
			PerfMeterSeriesStats mainStats = _cpuGraph.GetStats(1);
			PerfMeterSeriesStats renderStats = _cpuGraph.GetStats(2);
			PerfMeterSeriesStats otherStats = _cpuGraph.GetCpuOtherStats();
			PerfMeterSeriesStats gpuStats = _gpuGraph.GetStats(0);

			double cpuLegendReferenceMs = Max(_cpuGraph.ScaleMs, frameStats.Max, mainStats.Max, renderStats.Max, otherStats.Max);
			double gpuLegendReferenceMs = Max(_gpuGraph.ScaleMs, gpuStats.Max);
			SetLegendLine(_cpuFrameLegend, FormatLegend("frame", frameStats, cpuLegendReferenceMs), FrameColor);
			SetLegendLine(_cpuMainLegend, FormatLegend("main", mainStats, cpuLegendReferenceMs), MainColor);
			SetLegendLine(_cpuRenderLegend, FormatLegend("render", renderStats, cpuLegendReferenceMs), RenderColor);
			SetLegendLine(_cpuOtherLegend, FormatLegend("other", otherStats, cpuLegendReferenceMs), OtherCpuColor);

			SetLegendLine(
				_gpuLegend,
				FormatLegend("gpu", gpuStats, gpuLegendReferenceMs, metrics.GpuFrameTimeAvailable),
				metrics.GpuFrameTimeAvailable ? GpuColor : UnavailableColor);
		}

		private void UpdateMetricWidgets(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			if (_widgetBlock == null || !ShouldShowWidgetBlock())
			{
				return;
			}

			double currentFps = FpsFromFrameMs(metrics.CpuFrameTimeMs);
			double averageFps = metrics.AverageFps > 1d ? metrics.AverageFps : currentFps;
			double onePercentLowFps = metrics.OnePercentLowFps > 1d ? metrics.OnePercentLowFps : averageFps;
			double pointOnePercentLowFps = metrics.PointOnePercentLowFps > 1d ? metrics.PointOnePercentLowFps : onePercentLowFps;
			double frameBudgetMs = PerfMeterRuntime.GetFrameBudgetMs(status.TargetFps);
			bool cpuAvailable = metrics.CpuFrameTimeMs > 0d;
			bool gpuAvailable = metrics.GpuFrameTimeAvailable && metrics.GpuFrameTimeMs > 0d;

			_fpsCard?.SetValue(
				FormatFpsValue(averageFps),
				"1% " + FormatFpsValue(onePercentLowFps) + " / .1% " + FormatFpsValue(pointOnePercentLowFps),
				averageFps > 0d && currentFps < 1000d / frameBudgetMs * 0.9d ? WarningColor : AccentColor);

			Color cpuAccent = cpuAvailable && metrics.CpuFrameTimeMs > frameBudgetMs ? WarningColor : FrameColor;
			_cpuCard?.SetValue(
				FormatMsValue(metrics.CpuFrameTimeMs),
				"m " + FormatMsValue(metrics.CpuMainThreadFrameTimeMs) + " / r " + FormatMsValue(metrics.CpuRenderThreadFrameTimeMs),
				cpuAvailable ? cpuAccent : UnavailableColor);

			Color gpuAccent = !gpuAvailable ? UnavailableColor : metrics.GpuFrameTimeMs > frameBudgetMs ? WarningColor : GpuColor;
			_gpuCard?.SetValue(
				gpuAvailable ? FormatMsValue(metrics.GpuFrameTimeMs) : "--",
				"valid " + metrics.GpuValidSampleCount.ToString(CultureInfo.InvariantCulture) + "/" + metrics.FrameSampleCount.ToString(CultureInfo.InvariantCulture),
				gpuAccent);

			_spikeCard?.SetValue(
				metrics.FrameSpikeCount.ToString(CultureInfo.InvariantCulture) + " / " + metrics.SevereFrameSpikeCount.ToString(CultureInfo.InvariantCulture),
				!string.IsNullOrEmpty(warning) ? "warning active" : GetBottleneckText(metrics.Bottleneck),
				metrics.SevereFrameSpikeCount > 0 || !string.IsNullOrEmpty(warning) ? WarningColor : AccentColor);

			Color overdrawAccent = metrics.OverdrawState == PerfMeterOverdrawMeasurementState.Error || metrics.OverdrawState == PerfMeterOverdrawMeasurementState.Unsupported ? WarningColor : MainColor;
			_overdrawCard?.SetValue(
				FormatOverdrawRatio(metrics.OverdrawRatio),
				GetOverdrawStateText(metrics.OverdrawState) + " " + FormatPercent(metrics.OverdrawProgress),
				overdrawAccent);

			_cpuBudgetBar?.SetValue(metrics.CpuFrameTimeMs, frameBudgetMs, cpuAvailable, cpuAccent);
			_gpuBudgetBar?.SetValue(metrics.GpuFrameTimeMs, frameBudgetMs, gpuAvailable, gpuAccent);
		}

		private void BuildMetricBarText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			_valueBuilder.Length = 0;
			AppendRuntimeState(_valueBuilder, status.State);
			_valueBuilder.Append(" / ");
			AppendBottleneck(_valueBuilder, metrics.Bottleneck);
			AddBarMessage("SGG PerfMeter", _valueBuilder, AccentColor);

			if (HasModule(PerfMeterOverlayModule.Fps))
			{
				AddFpsBar(metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Timing))
			{
				AddStatBar("CPU Frame", metrics.CpuFrameTimeMs, _history.CpuFrameTimeMs, metrics.CpuFrameTimeMs > 0d, FrameColor, PerfMeterStatBarFormat.Milliseconds);
				AddStatBar("CPU Main", metrics.CpuMainThreadFrameTimeMs, _history.CpuMainThreadFrameTimeMs, metrics.CpuMainThreadFrameTimeMs > 0d, MainColor, PerfMeterStatBarFormat.Milliseconds);
				AddStatBar("CPU Render", metrics.CpuRenderThreadFrameTimeMs, _history.CpuRenderThreadFrameTimeMs, metrics.CpuRenderThreadFrameTimeMs > 0d, RenderColor, PerfMeterStatBarFormat.Milliseconds);
				AddStatBar("GPU", GetDisplayGpuFrameTime(metrics), _history.GpuFrameTimeMs, metrics.GpuFrameTimeAvailable && metrics.GpuFrameTimeMs > 0d, GpuColor, PerfMeterStatBarFormat.Milliseconds);
				AddStatBar("Present Wait", metrics.CpuMainThreadPresentWaitTimeMs, _history.CpuPresentWaitTimeMs, metrics.CpuMainThreadPresentWaitTimeMs >= 0d, OtherCpuColor, PerfMeterStatBarFormat.Milliseconds);
				_valueBuilder.Length = 0;
				AppendInt(_valueBuilder, metrics.GpuValidSampleCount);
				_valueBuilder.Append('/');
				AppendInt(_valueBuilder, metrics.FrameSampleCount);
				AddBarMessage("GPU valid", _valueBuilder, metrics.GpuFrameTimeAvailable ? GpuColor : UnavailableColor);
			}

			if (HasModule(PerfMeterOverlayModule.Rendering))
			{
				AddStatBar("Draw Calls", metrics.DrawCalls, _history.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls), FrameColor, PerfMeterStatBarFormat.Whole);
				AddStatBar("SetPass", metrics.SetPassCalls, _history.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls), MainColor, PerfMeterStatBarFormat.Whole);
				AddStatBar("Batches", metrics.Batches, _history.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches), RenderColor, PerfMeterStatBarFormat.Whole);
				AddStatBar("Vertices", metrics.Vertices, _history.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices), OtherCpuColor, PerfMeterStatBarFormat.Whole);
			}

			if (HasModule(PerfMeterOverlayModule.SrpBatcher))
			{
				AddStatBar("SRP Instances", metrics.SrpBatcherInstances, _history.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances), AccentColor, PerfMeterStatBarFormat.Whole);
			}

			if (HasModule(PerfMeterOverlayModule.Brg))
			{
				AddStatBar("BRG Draw", metrics.BrgDrawCalls, _history.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls), FrameColor, PerfMeterStatBarFormat.Whole);
				AddStatBar("BRG Inst", metrics.BrgInstances, _history.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances), RenderColor, PerfMeterStatBarFormat.Whole);
			}

			if (HasModule(PerfMeterOverlayModule.Uploads))
			{
				AddStatBar("Index Upload", metrics.IndexBufferUploadInFrameBytes, _history.IndexUploadBytes, HasCounter(status, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes), WarningColor, PerfMeterStatBarFormat.Megabytes);
			}

			if (HasModule(PerfMeterOverlayModule.Overdraw) || HasModule(PerfMeterOverlayModule.Heatmap))
			{
				_valueBuilder.Length = 0;
				AppendOverdrawState(_valueBuilder, metrics.OverdrawState);
				_valueBuilder.Append(' ');
				AppendDouble(_valueBuilder, metrics.OverdrawProgress * 100f, "0");
				_valueBuilder.Append("% ratio ");
				if (metrics.OverdrawRatio > 0d)
				{
					AppendDouble(_valueBuilder, metrics.OverdrawRatio, "0.00");
					_valueBuilder.Append('x');
				}
				else
				{
					_valueBuilder.Append("--");
				}

				_valueBuilder.Append(" heatmap ");
				_valueBuilder.Append(status.OverdrawHeatmapVisible ? "on" : "off");
				AddBarMessage("Overdraw", _valueBuilder, MainColor);
			}

			if (HasModule(PerfMeterOverlayModule.Memory))
			{
				AddStatBar("Memory", metrics.SystemUsedMemoryBytes, _history.SystemMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory), AccentColor, PerfMeterStatBarFormat.Megabytes);
			}

			if (HasModule(PerfMeterOverlayModule.Gc))
			{
				AddStatBar("GC Reserved", metrics.GcReservedMemoryBytes, _history.GcReservedMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GcReservedMemory), MainColor, PerfMeterStatBarFormat.Megabytes);
			}

			if (HasModule(PerfMeterOverlayModule.GpuMemory))
			{
				AddStatBar("GPU Memory", metrics.GpuMemoryBytes, _history.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory), GpuColor, PerfMeterStatBarFormat.Megabytes);
			}

			AppendCustomMetricBars();

			if (HasModule(PerfMeterOverlayModule.Warnings) && !string.IsNullOrEmpty(warning))
			{
				_valueBuilder.Length = 0;
				AppendLimited(_valueBuilder, warning, 150);
				AddBarMessage("Warn", _valueBuilder, WarningColor);
			}
		}

		private void AddFpsBar(PerfMeterMetricsSnapshot metrics)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			double currentFps = FpsFromFrameMs(metrics.CpuFrameTimeMs);
			PerfMeterHistoryStats cpuStats = _history.CpuFrameTimeMs.GetStats();
			if (cpuStats.Count <= 0 && metrics.CpuFrameTimeMs > 0d)
			{
				cpuStats = PerfMeterHistoryStats.FromCurrent(metrics.CpuFrameTimeMs);
			}

			double minFps = cpuStats.Count > 0 ? FpsFromFrameMs(cpuStats.Max) : currentFps;
			double maxFps = cpuStats.Count > 0 ? FpsFromFrameMs(cpuStats.Min) : currentFps;
			double averageFps = metrics.AverageFps > 1d ? metrics.AverageFps : (cpuStats.Count > 0 ? FpsFromFrameMs(cpuStats.Average) : currentFps);
			double onePercentLowFps = metrics.OnePercentLowFps > 1d ? metrics.OnePercentLowFps : (cpuStats.Count > 0 ? FpsFromFrameMs(cpuStats.OnePercent) : averageFps);
			double pointOnePercentLowFps = metrics.PointOnePercentLowFps > 1d ? metrics.PointOnePercentLowFps : (cpuStats.Count > 0 ? FpsFromFrameMs(cpuStats.PointOnePercent) : onePercentLowFps);
			PerfMeterHistoryStats fpsStats = new PerfMeterHistoryStats(
				Math.Max(1, cpuStats.Count),
				currentFps,
				minFps,
				maxFps,
				averageFps,
				onePercentLowFps,
				pointOnePercentLowFps);
			AddStatBar("FPS", currentFps, fpsStats, currentFps > 0d, AccentColor, PerfMeterStatBarFormat.Fps);
		}

		private void AddStatBar(string name, double current, PerfMeterHistorySeries series, bool available, Color accent, PerfMeterStatBarFormat format)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			PerfMeterHistoryStats stats = series.GetStats();
			if (stats.Count <= 0 && available && IsFinite(current))
			{
				stats = PerfMeterHistoryStats.FromCurrent(current);
			}

			AddStatBar(name, current, stats, available, accent, format);
		}

		private void AddStatBar(string name, double current, PerfMeterHistoryStats stats, bool available, Color accent, PerfMeterStatBarFormat format)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			PerfMeterStatBarField field = _barFields[_barFieldCount];
			if (field == null)
			{
				field = CreateBarField(_barFieldCount);
				_barFields[_barFieldCount] = field;
			}

			double scaleMax = Max(stats.Max, stats.Current, stats.Average, stats.OnePercent, stats.PointOnePercent);
			_valueBuilder.Length = 0;
			_barMinBuilder.Length = 0;
			_barMaxBuilder.Length = 0;
			_barStatsBuilder.Length = 0;
			if (available)
			{
				AppendStatBarCurrent(_valueBuilder, current, format);
			}
			else
			{
				_valueBuilder.Append("n/a");
			}

			if (stats.Count > 0)
			{
				AppendStatBarValue(_barMinBuilder, stats.Min, format);
				AppendStatBarValue(_barMaxBuilder, stats.Max, format);
				AppendStatBarValue(_barStatsBuilder, stats.Average, format);
				_barStatsBuilder.Append(" | ");
				AppendStatBarValue(_barStatsBuilder, stats.OnePercent, format);
				_barStatsBuilder.Append(" | ");
				AppendStatBarValue(_barStatsBuilder, stats.PointOnePercent, format);
			}
			else
			{
				_barMinBuilder.Append("--");
				_barMaxBuilder.Append("--");
				_barStatsBuilder.Append("-- | -- | --");
			}

			field.SetValue(
				name,
				_barMinBuilder,
				_barMaxBuilder,
				_valueBuilder,
				_barStatsBuilder,
				available ? GetBarPercent(current, scaleMax) : 0f,
				GetBarPercent(stats.Average, scaleMax),
				GetBarPercent(stats.OnePercent, scaleMax),
				GetBarPercent(stats.PointOnePercent, scaleMax),
				available ? accent : UnavailableColor);
			field.SetVisible(true);
			_barFieldCount++;
		}

		private void AddBarMessage(string name, string value, Color accent)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			PerfMeterStatBarField field = _barFields[_barFieldCount];
			if (field == null)
			{
				field = CreateBarField(_barFieldCount);
				_barFields[_barFieldCount] = field;
			}

			field.SetMessage(name, value, accent);
			field.SetVisible(true);
			_barFieldCount++;
		}

		private void AddBarMessage(string name, StringBuilder value, Color accent)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			PerfMeterStatBarField field = _barFields[_barFieldCount];
			if (field == null)
			{
				field = CreateBarField(_barFieldCount);
				_barFields[_barFieldCount] = field;
			}

			field.SetMessage(name, value, accent);
			field.SetVisible(true);
			_barFieldCount++;
		}

		private void AddBarMessage(StringBuilder name, StringBuilder value, Color accent)
		{
			if (!HasAvailableBarField())
			{
				return;
			}

			PerfMeterStatBarField field = _barFields[_barFieldCount];
			if (field == null)
			{
				field = CreateBarField(_barFieldCount);
				_barFields[_barFieldCount] = field;
			}

			field.SetMessage(name, value, accent);
			field.SetVisible(true);
			_barFieldCount++;
		}

		private void AppendCustomMetricBars()
		{
			if (!HasModule(PerfMeterOverlayModule.CustomMetrics) || !HasAvailableBarField())
			{
				return;
			}

			PerfMeterCustomMetricSnapshot[] customMetrics = GetOverlayCustomMetrics();
			int count = Math.Min(customMetrics.Length, MaxCustomMetricRows);
			for (int i = 0; i < count && HasAvailableBarField(); i++)
			{
				PerfMeterCustomMetricSnapshot metric = customMetrics[i];
				_barNameBuilder.Length = 0;
				_barNameBuilder.Append("Custom ");
				AppendCustomMetricDisplayName(_barNameBuilder, metric);
				_valueBuilder.Length = 0;
				AppendCustomMetricValue(_valueBuilder, metric);
				AddBarMessage(_barNameBuilder, _valueBuilder, metric.Available ? AccentColor : UnavailableColor);
			}
		}

		private void BuildFpsOnlyText(PerfMeterMetricsSnapshot metrics, string warning)
		{
			if (HasModule(PerfMeterOverlayModule.Fps))
			{
				BeginTextField("FPS");
				AppendFpsSummary(_valueBuilder, metrics);
				EndTextField();
			}

			if (!string.IsNullOrEmpty(warning) && HasModule(PerfMeterOverlayModule.Warnings))
			{
				BeginTextField("Warn");
				_valueBuilder.Append("active");
				EndTextField();
			}
		}

		private void BuildTextCompactText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			BeginTextField("SGG PerfMeter");
			AppendRuntimeState(_valueBuilder, status.State);
			_valueBuilder.Append(" / ");
			AppendBottleneck(_valueBuilder, metrics.Bottleneck);
			EndTextField();

			if (HasModule(PerfMeterOverlayModule.Fps))
			{
				AppendFpsLine(metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Timing))
			{
				BeginTextField("CPU/GPU ms");
				AppendMsWithRange(_valueBuilder, metrics.CpuFrameTimeMs, _history.CpuFrameTimeMs);
				_valueBuilder.Append(" / ");
				AppendMsWithRange(_valueBuilder, GetDisplayGpuFrameTime(metrics), _history.GpuFrameTimeMs, true, metrics.GpuFrameTimeAvailable);
				EndTextField();

				BeginTextField("main/render");
				AppendMsWithRange(_valueBuilder, metrics.CpuMainThreadFrameTimeMs, _history.CpuMainThreadFrameTimeMs);
				_valueBuilder.Append(" / ");
				AppendMsWithRange(_valueBuilder, metrics.CpuRenderThreadFrameTimeMs, _history.CpuRenderThreadFrameTimeMs);
				EndTextField();

				AppendGpuValidity(metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Rendering))
			{
				AppendIntPairWithRanges("Draw/SetPass", metrics.DrawCalls, _history.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls), metrics.SetPassCalls, _history.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls));
				AppendIntPairWithRanges("Batches/Verts", metrics.Batches, _history.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches), metrics.Vertices, _history.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices));
			}

			if (HasModule(PerfMeterOverlayModule.SrpBatcher) || HasModule(PerfMeterOverlayModule.Brg))
			{
				BeginTextField("SRP/BRG");
				AppendIntWithRange(_valueBuilder, metrics.SrpBatcherInstances, _history.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances));
				_valueBuilder.Append(" / ");
				AppendIntWithRange(_valueBuilder, metrics.BrgDrawCalls, _history.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls));
				_valueBuilder.Append(':');
				AppendIntWithRange(_valueBuilder, metrics.BrgInstances, _history.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances));
				EndTextField();
			}

			if (HasModule(PerfMeterOverlayModule.Overdraw) || HasModule(PerfMeterOverlayModule.Heatmap))
			{
				AppendOverdraw(status, metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Memory) || HasModule(PerfMeterOverlayModule.GpuMemory))
			{
				AppendMemoryPairWithRanges("Mem/GPU", metrics.SystemUsedMemoryBytes, _history.SystemMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory), metrics.GpuMemoryBytes, _history.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory));
			}

			AppendCustomMetrics();

			if (HasModule(PerfMeterOverlayModule.Warnings))
			{
				AppendWarning(warning, 120);
			}
		}

		private void BuildGraphsText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			BeginTextField("SGG PerfMeter Graphs");
			AppendRuntimeState(_valueBuilder, status.State);
			EndTextField();

			if (HasModule(PerfMeterOverlayModule.Fps))
			{
				AppendFpsLine(metrics);
			}

			BeginTextField("Bottleneck");
			AppendBottleneck(_valueBuilder, metrics.Bottleneck);
			EndTextField();

			if (HasModule(PerfMeterOverlayModule.Timing))
			{
				AppendGpuValidity(metrics);
			}

			AppendCustomMetrics();

			if (HasModule(PerfMeterOverlayModule.Warnings))
			{
				AppendWarning(warning, 140);
			}
		}

		private void BuildFullText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			BeginTextField("SGG PerfMeter");
			AppendRuntimeState(_valueBuilder, status.State);
			_valueBuilder.Append(" / ");
			AppendBottleneck(_valueBuilder, metrics.Bottleneck);
			EndTextField();

			if (HasModule(PerfMeterOverlayModule.Fps))
			{
				AppendFpsLine(metrics);
				BeginTextField("Spikes");
				AppendInt(_valueBuilder, metrics.FrameSpikeCount);
				_valueBuilder.Append(" / severe ");
				AppendInt(_valueBuilder, metrics.SevereFrameSpikeCount);
				EndTextField();
			}

			if (HasModule(PerfMeterOverlayModule.Timing))
			{
				AppendMsWithRange("CPU Frame", metrics.CpuFrameTimeMs, _history.CpuFrameTimeMs);
				AppendMsWithRange("CPU Main", metrics.CpuMainThreadFrameTimeMs, _history.CpuMainThreadFrameTimeMs);
				AppendMsWithRange("CPU Render", metrics.CpuRenderThreadFrameTimeMs, _history.CpuRenderThreadFrameTimeMs);
				AppendMsWithRange("GPU", GetDisplayGpuFrameTime(metrics), _history.GpuFrameTimeMs, true, metrics.GpuFrameTimeAvailable);
				AppendMsWithRange("Present Wait", metrics.CpuMainThreadPresentWaitTimeMs, _history.CpuPresentWaitTimeMs);
				AppendGpuValidity(metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Rendering))
			{
				AppendIntWithRange("Draw Calls", metrics.DrawCalls, _history.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls));
				AppendIntWithRange("SetPass", metrics.SetPassCalls, _history.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls));
				AppendIntWithRange("Batches", metrics.Batches, _history.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches));
				AppendIntWithRange("Vertices", metrics.Vertices, _history.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices));
			}

			if (HasModule(PerfMeterOverlayModule.SrpBatcher))
			{
				AppendIntWithRange("SRP Instances", metrics.SrpBatcherInstances, _history.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances));
			}

			if (HasModule(PerfMeterOverlayModule.Brg))
			{
				AppendIntPairWithRanges("BRG Draw/Inst", metrics.BrgDrawCalls, _history.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls), metrics.BrgInstances, _history.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances));
			}

			if (HasModule(PerfMeterOverlayModule.Uploads))
			{
				AppendMemoryWithRange("Index Upload", metrics.IndexBufferUploadInFrameBytes, _history.IndexUploadBytes, HasCounter(status, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes));
			}

			if (HasModule(PerfMeterOverlayModule.Overdraw) || HasModule(PerfMeterOverlayModule.Heatmap))
			{
				AppendOverdraw(status, metrics);
			}

			if (HasModule(PerfMeterOverlayModule.Memory))
			{
				AppendMemoryWithRange("Memory", metrics.SystemUsedMemoryBytes, _history.SystemMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory));
			}

			if (HasModule(PerfMeterOverlayModule.Gc))
			{
				AppendMemoryWithRange("GC Reserved", metrics.GcReservedMemoryBytes, _history.GcReservedMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GcReservedMemory));
			}

			if (HasModule(PerfMeterOverlayModule.GpuMemory))
			{
				AppendMemoryWithRange("GPU Memory", metrics.GpuMemoryBytes, _history.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory));
			}

			AppendCustomMetrics();

			if (HasModule(PerfMeterOverlayModule.Warnings))
			{
				AppendWarning(warning, 180);
			}
		}

		private string ResolveDisplayWarning(string warning)
		{
			if (!string.IsNullOrEmpty(warning))
			{
				_heldWarning = warning;
				_warningVisibleUntil = Time.unscaledTime + WarningHoldSeconds;
				return warning;
			}

			return Time.unscaledTime <= _warningVisibleUntil ? _heldWarning : string.Empty;
		}

		private PanelTextSettings CreatePanelTextSettings()
		{
			if (_panelTextSettings != null)
			{
				return _panelTextSettings;
			}

			_panelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
			_panelTextSettings.name = "SGG PerfMeter Text Settings";
			_panelTextSettings.hideFlags = HideFlags.DontSave;
			_panelTextSettings.displayWarnings = false;
			_panelTextSettings.defaultFontAsset = CreateRuntimeFontAsset();
			return _panelTextSettings;
		}

		private UnityEngine.TextCore.Text.FontAsset CreateRuntimeFontAsset()
		{
			if (_fontAsset != null)
			{
				return _fontAsset;
			}

			Font font = GetRuntimeFont(PerfMeterOverlayFontRole.Regular);
			if (font == null)
			{
				return null;
			}

			_fontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font);
			_fontAsset.name = "SGG PerfMeter Font Asset";
			_fontAsset.hideFlags = HideFlags.DontSave;
			return _fontAsset;
		}


		private static Font GetRuntimeFont(PerfMeterOverlayFontRole role)
		{
			PerfMeterOverlayFontResources resources = LoadFontResources();
			if (resources != null)
			{
				switch (_activeFontFamily)
				{
					case PerfMeterOverlayFontFamily.JetBrainsMono:
						return role == PerfMeterOverlayFontRole.Regular ? resources.JetBrainsMonoRegular ?? GetLegacyRuntimeFont() : resources.JetBrainsMonoMedium ?? resources.JetBrainsMonoRegular ?? GetLegacyRuntimeFont();
					case PerfMeterOverlayFontFamily.Manrope:
						return GetManropeFont(resources, role) ?? GetLegacyRuntimeFont();
				}
			}

			return GetLegacyRuntimeFont();
		}

		private static Font GetManropeFont(PerfMeterOverlayFontResources resources, PerfMeterOverlayFontRole role)
		{
			switch (role)
			{
				case PerfMeterOverlayFontRole.Bold:
					return resources.ManropeBold;
				case PerfMeterOverlayFontRole.SemiBold:
					return resources.ManropeSemiBold;
				case PerfMeterOverlayFontRole.Medium:
					return resources.ManropeMedium;
				default:
					return resources.ManropeRegular;
			}
		}

		private static PerfMeterOverlayFontResources LoadFontResources()
		{
			if (_fontResources == null)
			{
				_fontResources = Resources.Load<PerfMeterOverlayFontResources>("PerfMeterOverlayFonts");
			}

			return _fontResources;
		}

		private static Font GetLegacyRuntimeFont()
		{
			if (_legacyRuntimeFont == null)
			{
				_legacyRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			}

			return _legacyRuntimeFont;
		}

		private static void SetActiveFontFamily(PerfMeterOverlayFontFamily fontFamily)
		{
			_activeFontFamily = PerfMeterSettingsStore.NormalizeOverlayFontFamily(fontFamily);
		}

		private static void SetActiveTheme(PerfMeterOverlayTheme theme)
		{
			_activeTheme = PerfMeterOverlayThemeTokens.Resolve(PerfMeterSettingsStore.NormalizeOverlayTheme(theme));
		}

		private ThemeStyleSheet CreateGeneratedThemeStyleSheet()
		{
			if (_themeStyleSheet != null)
			{
				return _themeStyleSheet;
			}

			_themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			_themeStyleSheet.name = "SGG PerfMeter Runtime Theme";
			_themeStyleSheet.hideFlags = HideFlags.DontSave;
			return _themeStyleSheet;
		}

		private static ThemeStyleSheet ResolveRuntimeThemeStyleSheet()
		{
			ThemeStyleSheet theme = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
			if (theme != null)
			{
				return theme;
			}

			ThemeStyleSheet[] themes = Resources.FindObjectsOfTypeAll<ThemeStyleSheet>();
			for (int i = 0; i < themes.Length; i++)
			{
				if (themes[i] != null && themes[i].name.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return themes[i];
				}
			}

			return themes.Length > 0 ? themes[0] : null;
		}

		private void AppendFpsLine(PerfMeterMetricsSnapshot metrics)
		{
			BeginTextField("FPS");
			AppendFpsSummary(_valueBuilder, metrics);
			_valueBuilder.Append(" | samples ");
			AppendInt(_valueBuilder, metrics.FrameSampleCount);
			EndTextField();
		}

		private void AppendFpsSummary(StringBuilder builder, PerfMeterMetricsSnapshot metrics)
		{
			double currentFps = FpsFromFrameMs(metrics.CpuFrameTimeMs);
			double averageFps = metrics.AverageFps > 1d ? metrics.AverageFps : currentFps;
			double onePercentLowFps = metrics.OnePercentLowFps > 1d ? metrics.OnePercentLowFps : averageFps;
			double pointOnePercentLowFps = metrics.PointOnePercentLowFps > 1d ? metrics.PointOnePercentLowFps : onePercentLowFps;

			AppendFpsValue(builder, averageFps);
			builder.Append(" | 1% ");
			AppendFpsValue(builder, onePercentLowFps);
			builder.Append(" | 0.1% ");
			AppendFpsValue(builder, pointOnePercentLowFps);
		}

		private void AppendGpuValidity(PerfMeterMetricsSnapshot metrics)
		{
			BeginTextField("GPU valid");
			AppendInt(_valueBuilder, metrics.GpuValidSampleCount);
			_valueBuilder.Append('/');
			AppendInt(_valueBuilder, metrics.FrameSampleCount);
			EndTextField();
		}

		private void AppendMsWithRange(string name, double value, PerfMeterHistorySeries series, bool available = true, bool currentAvailable = true)
		{
			BeginTextField(name);
			AppendMsWithRange(_valueBuilder, value, series, available, currentAvailable);
			_valueBuilder.Append(" ms");
			EndTextField();
		}

		private void AppendIntWithRange(string name, int value, PerfMeterHistorySeries series, bool available = true)
		{
			BeginTextField(name);
			AppendIntWithRange(_valueBuilder, value, series, available);
			EndTextField();
		}

		private void AppendMemoryWithRange(string name, long bytes, PerfMeterHistorySeries series, bool available = true)
		{
			BeginTextField(name);
			AppendMemoryWithRange(_valueBuilder, bytes, series, available);
			_valueBuilder.Append(" MB");
			EndTextField();
		}

		private void AppendIntPairWithRanges(string name, int first, PerfMeterHistorySeries firstSeries, bool firstAvailable, int second, PerfMeterHistorySeries secondSeries, bool secondAvailable)
		{
			BeginTextField(name);
			AppendIntWithRange(_valueBuilder, first, firstSeries, firstAvailable);
			_valueBuilder.Append(" / ");
			AppendIntWithRange(_valueBuilder, second, secondSeries, secondAvailable);
			EndTextField();
		}

		private void AppendMemoryPairWithRanges(string name, long firstBytes, PerfMeterHistorySeries firstSeries, bool firstAvailable, long secondBytes, PerfMeterHistorySeries secondSeries, bool secondAvailable)
		{
			BeginTextField(name);
			AppendMemoryWithRange(_valueBuilder, firstBytes, firstSeries, firstAvailable);
			_valueBuilder.Append(" / ");
			AppendMemoryWithRange(_valueBuilder, secondBytes, secondSeries, secondAvailable);
			_valueBuilder.Append(" MB");
			EndTextField();
		}

		private void AppendOverdraw(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics)
		{
			BeginTextField("Overdraw");
			AppendOverdrawState(_valueBuilder, metrics.OverdrawState);
			_valueBuilder.Append(' ');
			AppendDouble(_valueBuilder, metrics.OverdrawProgress * 100f, "0");
			_valueBuilder.Append("% ratio ");
			if (metrics.OverdrawRatio > 0d)
			{
				AppendDouble(_valueBuilder, metrics.OverdrawRatio, "0.00");
			}
			else
			{
				_valueBuilder.Append("unknown");
			}

			_valueBuilder.Append(" heatmap ");
			_valueBuilder.Append(status.OverdrawHeatmapVisible ? "on" : "off");
			EndTextField();
		}

		private void AppendCustomMetrics()
		{
			if (!HasModule(PerfMeterOverlayModule.CustomMetrics))
			{
				return;
			}

			PerfMeterCustomMetricSnapshot[] customMetrics = GetOverlayCustomMetrics();
			int count = Math.Min(customMetrics.Length, MaxCustomMetricRows);
			for (int i = 0; i < count; i++)
			{
				PerfMeterCustomMetricSnapshot metric = customMetrics[i];
				BeginTextField("Custom " + GetCustomMetricDisplayName(metric));
				AppendCustomMetricValue(_valueBuilder, metric);
				EndTextField();
			}
		}

		private static string GetCustomMetricDisplayName(PerfMeterCustomMetricSnapshot metric)
		{
			string name = !string.IsNullOrEmpty(metric.Name) ? metric.Name : metric.Id;
			if (string.IsNullOrEmpty(name))
			{
				return "metric";
			}

			return name.Length <= MaxCustomMetricNameLength ? name : name.Substring(0, MaxCustomMetricNameLength - 3) + "...";
		}

		private static PerfMeterCustomMetricSnapshot[] GetOverlayCustomMetrics()
		{
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			return runtime != null ? runtime.PeekLatestCustomMetrics() : PerformanceMeter.GetCustomMetrics();
		}

		private static void AppendCustomMetricDisplayName(StringBuilder builder, PerfMeterCustomMetricSnapshot metric)
		{
			string name = !string.IsNullOrEmpty(metric.Name) ? metric.Name : metric.Id;
			if (string.IsNullOrEmpty(name))
			{
				builder.Append("metric");
				return;
			}

			AppendLimited(builder, name, MaxCustomMetricNameLength);
		}

		private void AppendCustomMetricValue(StringBuilder builder, PerfMeterCustomMetricSnapshot metric)
		{
			if (metric.Available && !double.IsNaN(metric.Value) && !double.IsInfinity(metric.Value))
			{
				AppendDouble(builder, metric.Value, "0.###");
				if (!string.IsNullOrEmpty(metric.Unit))
				{
					builder.Append(' ');
					builder.Append(metric.Unit);
				}
			}
			else
			{
				builder.Append("n/a");
			}

			if (!string.IsNullOrEmpty(metric.Warning))
			{
				builder.Append(" ! ");
				AppendLimited(builder, metric.Warning, MaxCustomMetricWarningLength);
			}
		}

		private static void AppendLimited(StringBuilder builder, string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || maxLength <= 0)
			{
				return;
			}

			if (value.Length <= maxLength)
			{
				builder.Append(value);
				return;
			}

			builder.Append(value, 0, Math.Max(0, maxLength - 3));
			builder.Append("...");
		}

		private void AppendWarning(string warning, int maxLength)
		{
			if (string.IsNullOrEmpty(warning))
			{
				return;
			}

			BeginTextField("Warn");
			if (warning.Length <= maxLength)
			{
				_valueBuilder.Append(warning);
			}
			else
			{
				_valueBuilder.Append(warning, 0, maxLength - 3);
				_valueBuilder.Append("...");
			}

			EndTextField();
		}

		private void BeginTextField(string name)
		{
			_pendingTextFieldName = name ?? string.Empty;
			_valueBuilder.Length = 0;
		}

		private void EndTextField()
		{
			if (_textFieldCount >= _textFields.Length)
			{
				return;
			}

			PerfMeterOverlayTextField field = _textFields[_textFieldCount];
			if (field == null)
			{
				field = CreateTextField(_textFieldCount);
				_textFields[_textFieldCount] = field;
			}

			field.SetName(_pendingTextFieldName);
			field.SetValue(_valueBuilder);
			field.SetVisible(true);
			_textFieldCount++;
		}

		private void HideUnusedTextFields()
		{
			for (int i = _textFieldCount; i < _lastVisibleTextFieldCount; i++)
			{
				_textFields[i]?.SetVisible(false);
			}

			_lastVisibleTextFieldCount = _textFieldCount;
		}

		private void HideUnusedBarFields()
		{
			for (int i = _barFieldCount; i < _lastVisibleBarFieldCount; i++)
			{
				_barFields[i]?.SetVisible(false);
			}

			_lastVisibleBarFieldCount = _barFieldCount;
		}

		private void AppendMsWithRange(StringBuilder builder, double value, PerfMeterHistorySeries series, bool available = true, bool currentAvailable = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				AppendMsPlaceholder(builder, range.Max);
				if (range.Count > 0)
				{
					builder.Append(" (");
					AppendMsPlaceholder(builder, range.Min);
					builder.Append('-');
					AppendMsPlaceholder(builder, range.Max);
					builder.Append(')');
				}

				return;
			}

			if (range.Count <= 0)
			{
				if (currentAvailable)
				{
					AppendMsValue(builder, value);
				}
				else
				{
					AppendMsPlaceholder(builder, value);
				}

				return;
			}

			if (currentAvailable)
			{
				AppendMsValue(builder, value);
			}
			else
			{
				AppendMsPlaceholder(builder, range.Max);
			}

			builder.Append(" (");
			AppendMsValue(builder, range.Min);
			builder.Append('-');
			AppendMsValue(builder, range.Max);
			builder.Append(')');
		}

		private void AppendIntWithRange(StringBuilder builder, int value, PerfMeterHistorySeries series, bool available = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				AppendIntPlaceholder(builder, range.Max);
				if (range.Count > 0)
				{
					builder.Append(" (");
					AppendIntPlaceholder(builder, range.Min);
					builder.Append('-');
					AppendIntPlaceholder(builder, range.Max);
					builder.Append(')');
				}

				return;
			}

			AppendInt(builder, value);
			if (range.Count <= 0)
			{
				return;
			}

			builder.Append(" (");
			AppendWholeValue(builder, range.Min);
			builder.Append('-');
			AppendWholeValue(builder, range.Max);
			builder.Append(')');
		}

		private void AppendMemoryWithRange(StringBuilder builder, long bytes, PerfMeterHistorySeries series, bool available = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				AppendMemoryPlaceholder(builder, range.Max);
				if (range.Count > 0)
				{
					builder.Append(" (");
					AppendMemoryPlaceholder(builder, range.Min);
					builder.Append('-');
					AppendMemoryPlaceholder(builder, range.Max);
					builder.Append(')');
				}

				return;
			}

			AppendMemoryValue(builder, bytes);
			if (range.Count <= 0)
			{
				return;
			}

			builder.Append(" (");
			AppendMemoryValue(builder, range.Min);
			builder.Append('-');
			AppendMemoryValue(builder, range.Max);
			builder.Append(')');
		}

		private void AppendFpsValue(StringBuilder builder, double value)
		{
			if (value > 0d)
			{
				AppendDouble(builder, value, "0.0");
			}
			else
			{
				builder.Append("--");
			}
		}

		private void AppendMsValue(StringBuilder builder, double value)
		{
			if (value > 0d)
			{
				AppendDouble(builder, value, "0.00");
			}
			else
			{
				builder.Append("--");
			}
		}

		private void AppendMsPlaceholder(StringBuilder builder, double referenceMaxMs)
		{
			int digits = GetIntegralDigitCount(Math.Max(1d, referenceMaxMs));
			AppendRepeated(builder, '_', digits);
			builder.Append(".__");
		}

		private static void AppendIntPlaceholder(StringBuilder builder, double referenceValue)
		{
			AppendRepeated(builder, '_', GetIntegralDigitCount(Math.Max(1d, referenceValue)));
		}

		private void AppendMemoryPlaceholder(StringBuilder builder, double referenceBytes)
		{
			double megabytes = Math.Max(0d, referenceBytes / 1048576d);
			AppendRepeated(builder, '_', GetIntegralDigitCount(Math.Max(1d, megabytes)));
			builder.Append("._");
		}

		private void AppendMemoryValue(StringBuilder builder, double bytes)
		{
			AppendDouble(builder, bytes / 1048576d, "0.0");
		}

		private void AppendWholeValue(StringBuilder builder, double value)
		{
			AppendDouble(builder, Math.Round(value), "0");
		}

		private void AppendInt(StringBuilder builder, int value)
		{
			if (value.TryFormat(_numberBuffer, out int written, default, CultureInfo.InvariantCulture))
			{
				builder.Append(_numberBuffer, 0, written);
				return;
			}

			builder.Append(value.ToString(CultureInfo.InvariantCulture));
		}

		private void AppendDouble(StringBuilder builder, double value, string format)
		{
			if (value.TryFormat(_numberBuffer, out int written, format, CultureInfo.InvariantCulture))
			{
				builder.Append(_numberBuffer, 0, written);
				return;
			}

			builder.Append(value.ToString(format, CultureInfo.InvariantCulture));
		}

		private void AppendRuntimeState(StringBuilder builder, PerfMeterRuntimeState state)
		{
			builder.Append(GetRuntimeStateText(state));
		}

		private void AppendBottleneck(StringBuilder builder, PerfMeterBottleneck bottleneck)
		{
			builder.Append(GetBottleneckText(bottleneck));
		}

		private void AppendOverdrawState(StringBuilder builder, PerfMeterOverdrawMeasurementState state)
		{
			builder.Append(GetOverdrawStateText(state));
		}

		internal static string GetRuntimeStateText(PerfMeterRuntimeState state)
		{
			switch (state)
			{
				case PerfMeterRuntimeState.Stopped:
					return "Stopped";
				case PerfMeterRuntimeState.Starting:
					return "Starting";
				case PerfMeterRuntimeState.Running:
					return "Running";
				case PerfMeterRuntimeState.Error:
					return "Error";
				default:
					return "Unknown";
			}
		}

		internal static string GetBottleneckText(PerfMeterBottleneck bottleneck)
		{
			switch (bottleneck)
			{
				case PerfMeterBottleneck.Balanced:
					return "Balanced";
				case PerfMeterBottleneck.GpuBound:
					return "GpuBound";
				case PerfMeterBottleneck.CpuMainThreadBound:
					return "CpuMainThreadBound";
				case PerfMeterBottleneck.CpuRenderThreadBound:
					return "CpuRenderThreadBound";
				case PerfMeterBottleneck.PresentLimited:
					return "PresentLimited";
				default:
					return "Unknown";
			}
		}

		internal static string GetOverdrawStateText(PerfMeterOverdrawMeasurementState state)
		{
			switch (state)
			{
				case PerfMeterOverdrawMeasurementState.Off:
					return "Off";
				case PerfMeterOverdrawMeasurementState.Measuring:
					return "Measuring";
				case PerfMeterOverdrawMeasurementState.Completed:
					return "Completed";
				case PerfMeterOverdrawMeasurementState.Canceled:
					return "Canceled";
				case PerfMeterOverdrawMeasurementState.Error:
					return "Error";
				case PerfMeterOverdrawMeasurementState.Unsupported:
					return "Unsupported";
				default:
					return "Unknown";
			}
		}

		private static int GetIntegralDigitCount(double value)
		{
			return Math.Max(1, (int)Math.Floor(Math.Log10(Math.Max(1d, value))) + 1);
		}

		private static void AppendRepeated(StringBuilder builder, char character, int count)
		{
			for (int i = 0; i < count; i++)
			{
				builder.Append(character);
			}
		}

		private static void SetLegendLine(Label label, string text, Color background)
		{
			label.text = text;
			label.style.backgroundColor = background;
			label.style.color = GetReadableTextColor(background);
		}

		private double GetDisplayGpuFrameTime(PerfMeterMetricsSnapshot metrics)
		{
			if (metrics.GpuFrameTimeAvailable && metrics.GpuFrameTimeMs > 0d)
			{
				return metrics.GpuFrameTimeMs;
			}

			return _history.GpuFrameTimeMs.TryGetLast(out double value) ? value : 0d;
		}

		private static bool HasCounter(PerfMeterStatusSnapshot status, PerfMeterCounterAvailability counter)
		{
			return (status.AvailableCounters & counter) != 0;
		}

		private bool HasModule(PerfMeterOverlayModule module)
		{
			return (_modules & module) != 0;
		}

		private static string FormatLegend(string name, PerfMeterSeriesStats stats, double scaleMs, bool currentAvailable = true)
		{
			if (stats.Count <= 0)
			{
				return name + " " + FormatLegendValues(default, scaleMs, false);
			}

			return name + " " + FormatLegendValues(stats, scaleMs, currentAvailable);
		}

		private static string FormatLegendValues(PerfMeterSeriesStats stats, double scaleMs, bool currentAvailable)
		{
			return (currentAvailable ? FormatMsFixedValue(stats.Current, scaleMs) : FormatMsPlaceholder(scaleMs))
				+ " avg " + FormatMsFixedValue(stats.Average, scaleMs)
				+ " 1% " + FormatMsFixedValue(stats.OnePercentHigh, scaleMs)
				+ " .1% " + FormatMsFixedValue(stats.PointOnePercentHigh, scaleMs);
		}

		private static string FormatMsWithRange(double value, PerfMeterHistorySeries series, bool available = true, bool currentAvailable = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				return FormatMsPlaceholder(range.Max) + (range.Count > 0 ? " (" + FormatMsPlaceholder(range.Min) + "-" + FormatMsPlaceholder(range.Max) + ")" : string.Empty);
			}

			if (range.Count <= 0)
			{
				return currentAvailable ? FormatMsValue(value) : FormatMsPlaceholder(value);
			}

			string current = currentAvailable ? FormatMsValue(value) : FormatMsPlaceholder(range.Max);
			return current + " (" + FormatMsValue(range.Min) + "-" + FormatMsValue(range.Max) + ")";
		}

		private static string FormatIntWithRange(int value, PerfMeterHistorySeries series, bool available = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				return FormatIntPlaceholder(range.Max) + (range.Count > 0 ? " (" + FormatIntPlaceholder(range.Min) + "-" + FormatIntPlaceholder(range.Max) + ")" : string.Empty);
			}

			if (range.Count <= 0)
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}

			return value.ToString(CultureInfo.InvariantCulture) + " (" + FormatWholeValue(range.Min) + "-" + FormatWholeValue(range.Max) + ")";
		}

		private static string FormatMemoryWithRange(long bytes, PerfMeterHistorySeries series, bool available = true)
		{
			PerfMeterHistoryRange range = series.GetRange();
			if (!available)
			{
				return FormatMemoryPlaceholder(range.Max) + (range.Count > 0 ? " (" + FormatMemoryPlaceholder(range.Min) + "-" + FormatMemoryPlaceholder(range.Max) + ")" : string.Empty);
			}

			if (range.Count <= 0)
			{
				return FormatMemoryValue(bytes);
			}

			return FormatMemoryValue(bytes) + " (" + FormatMemoryValue(range.Min) + "-" + FormatMemoryValue(range.Max) + ")";
		}

		private static string FormatFpsValue(double value)
		{
			return value > 0d ? value.ToString("0.0", CultureInfo.InvariantCulture) : "--";
		}

		private static string FormatOverdrawRatio(double value)
		{
			return value > 0d ? value.ToString("0.00", CultureInfo.InvariantCulture) + "x" : "--";
		}

		private static string FormatPercent(float value)
		{
			return (Mathf.Clamp01(value) * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
		}

		private void AppendStatBarCurrent(StringBuilder builder, double value, PerfMeterStatBarFormat format)
		{
			if (format == PerfMeterStatBarFormat.Ratio)
			{
				AppendPositiveFixed(builder, value, "0.00");
				if (value > 0d)
				{
					builder.Append('x');
				}

				return;
			}

			AppendStatBarValue(builder, value, format);
			switch (format)
			{
				case PerfMeterStatBarFormat.Milliseconds:
					builder.Append(" ms");
					break;
				case PerfMeterStatBarFormat.Megabytes:
					builder.Append(" MB");
					break;
			}
		}

		private void AppendStatBarValue(StringBuilder builder, double value, PerfMeterStatBarFormat format)
		{
			switch (format)
			{
				case PerfMeterStatBarFormat.Fps:
					AppendFpsValue(builder, value);
					break;
				case PerfMeterStatBarFormat.Milliseconds:
					AppendNonNegativeFixed(builder, value, "0.00");
					break;
				case PerfMeterStatBarFormat.Megabytes:
					AppendNonNegativeFixed(builder, value / 1048576d, "0.0");
					break;
				case PerfMeterStatBarFormat.Ratio:
					AppendPositiveFixed(builder, value, "0.00");
					break;
				default:
					if (IsFinite(value))
					{
						AppendWholeValue(builder, value);
					}
					else
					{
						builder.Append("--");
					}
					break;
			}
		}

		private void AppendNonNegativeFixed(StringBuilder builder, double value, string format)
		{
			if (IsFinite(value) && value >= 0d)
			{
				AppendDouble(builder, value, format);
				return;
			}

			builder.Append("--");
		}

		private void AppendPositiveFixed(StringBuilder builder, double value, string format)
		{
			if (IsFinite(value) && value > 0d)
			{
				AppendDouble(builder, value, format);
				return;
			}

			builder.Append("--");
		}

		private static string FormatMsValue(double value)
		{
			return value > 0d ? value.ToString("0.00", CultureInfo.InvariantCulture) : "--";
		}

		private static string FormatMsFixedValue(double value, double referenceMaxMs)
		{
			double safeReference = Math.Max(1d, referenceMaxMs);
			int digits = Math.Max(2, (int)Math.Floor(Math.Log10(safeReference)) + 1);
			string text = Math.Max(0d, value).ToString("F2", CultureInfo.InvariantCulture);
			int decimalIndex = text.IndexOf('.');
			int integerDigits = decimalIndex >= 0 ? decimalIndex : text.Length;
			return integerDigits < digits ? new string('0', digits - integerDigits) + text : text;
		}

		private static string FormatMsPlaceholder(double referenceMaxMs)
		{
			return MaskDigits(FormatMsFixedValue(0d, referenceMaxMs));
		}

		private static string FormatIntPlaceholder(double referenceValue)
		{
			int digits = Math.Max(1, (int)Math.Floor(Math.Log10(Math.Max(1d, referenceValue))) + 1);
			return new string('_', digits);
		}

		private static string FormatMemoryPlaceholder(double referenceBytes)
		{
			return MaskDigits(FormatMemoryValue(referenceBytes));
		}

		private static string MaskDigits(string value)
		{
			char[] characters = value.ToCharArray();
			for (int i = 0; i < characters.Length; i++)
			{
				if (char.IsDigit(characters[i]))
				{
					characters[i] = '_';
				}
			}

			return new string(characters);
		}

		private static string FormatMemoryValue(double bytes)
		{
			return (bytes / 1048576d).ToString("0.0", CultureInfo.InvariantCulture);
		}

		private static string FormatWholeValue(double value)
		{
			return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
		}

		private static double Max(double first, double second)
		{
			double result = 0d;
			result = MaxFinite(result, first);
			return MaxFinite(result, second);
		}

		private static double Max(double first, double second, double third, double fourth, double fifth)
		{
			double result = 0d;
			result = MaxFinite(result, first);
			result = MaxFinite(result, second);
			result = MaxFinite(result, third);
			result = MaxFinite(result, fourth);
			return MaxFinite(result, fifth);
		}

		private static double MaxFinite(double current, double value)
		{
			return value > current && IsFinite(value) ? value : current;
		}

		private static double FpsFromFrameMs(double frameMs)
		{
			return frameMs > 0d ? 1000d / frameMs : 0d;
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static float GetBarPercent(double value, double scaleMax)
		{
			return IsFinite(value) && IsFinite(scaleMax) && scaleMax > 0d ? Mathf.Clamp((float)(value / scaleMax * 100d), 0f, 100f) : 0f;
		}

		private static Color GetReadableTextColor(Color background)
		{
			float luminance = 0.2126f * background.r + 0.7152f * background.g + 0.0722f * background.b;
			return luminance > 0.58f ? new Color(0.04f, 0.055f, 0.065f, 1f) : new Color(0.92f, 0.96f, 1f, 1f);
		}

		private static Color WithAlpha(Color color, float alpha)
		{
			return new Color(color.r, color.g, color.b, alpha);
		}

		private sealed class PerfMeterOverlayTextField
		{
			private readonly VisualElement _row;
			private readonly Label _nameLabel;
			private readonly Label _valueLabel;
			private readonly PerfMeterOverlayCachedText _valueCache = new PerfMeterOverlayCachedText();
			private string _name = string.Empty;

			internal PerfMeterOverlayTextField(VisualElement row, Label nameLabel, Label valueLabel)
			{
				_row = row;
				_nameLabel = nameLabel;
				_valueLabel = valueLabel;
			}

			internal void SetName(string name)
			{
				string safeName = name ?? string.Empty;
				if (_name == safeName)
				{
					return;
				}

				_name = safeName;
				_nameLabel.text = safeName;
			}

			internal void SetValue(StringBuilder builder)
			{
				if (_valueCache.TryUpdate(builder, out string text))
				{
					_valueLabel.text = text;
				}
			}

			internal void SetFontSize(float fontSize)
			{
				_nameLabel.style.fontSize = fontSize;
				_valueLabel.style.fontSize = fontSize;
			}

			internal void SetFonts(Font nameFont, Font valueFont)
			{
				_nameLabel.style.unityFont = nameFont;
				_valueLabel.style.unityFont = valueFont;
			}

			internal void SetVisible(bool visible)
			{
				_row.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		private sealed class PerfMeterStatBarField
		{
			private readonly VisualElement _root;
			private readonly VisualElement _track;
			private readonly VisualElement _fill;
			private readonly VisualElement _averageMarker;
			private readonly VisualElement _onePercentMarker;
			private readonly VisualElement _pointOnePercentMarker;
			private readonly Label _nameLabel;
			private readonly Label _minLabel;
			private readonly Label _maxLabel;
			private readonly Label _currentLabel;
			private readonly Label _statsLabel;
			private readonly PerfMeterOverlayCachedText _nameCache = new PerfMeterOverlayCachedText();
			private readonly PerfMeterOverlayCachedText _minCache = new PerfMeterOverlayCachedText();
			private readonly PerfMeterOverlayCachedText _maxCache = new PerfMeterOverlayCachedText();
			private readonly PerfMeterOverlayCachedText _currentCache = new PerfMeterOverlayCachedText();
			private readonly PerfMeterOverlayCachedText _statsCache = new PerfMeterOverlayCachedText();

			internal PerfMeterStatBarField(string name, Font nameFont, Font valueFont, Font statsFont)
			{
				_root = new VisualElement
				{
					name = name,
					pickingMode = PickingMode.Ignore
				};
				_root.style.flexDirection = FlexDirection.Row;
				_root.style.alignItems = Align.Center;
				_root.style.height = 17f;
				_root.style.marginBottom = 1f;
				_root.style.overflow = Overflow.Hidden;

				_nameLabel = CreateLabel(MutedTextColor, TextAnchor.MiddleLeft, nameFont);
				_nameLabel.style.width = MetricBarNameWidth;
				_nameLabel.style.marginRight = 8f;
				_nameLabel.style.flexShrink = 0f;
				_minLabel = CreateLabel(MutedTextColor, TextAnchor.MiddleRight, statsFont);
				_minLabel.style.width = MetricBarRangeWidth;
				_minLabel.style.marginRight = 5f;
				_minLabel.style.flexShrink = 0f;
				_maxLabel = CreateLabel(MutedTextColor, TextAnchor.MiddleLeft, statsFont);
				_maxLabel.style.width = MetricBarRangeWidth;
				_maxLabel.style.marginLeft = 5f;
				_maxLabel.style.marginRight = 8f;
				_maxLabel.style.flexShrink = 0f;
				_currentLabel = CreateLabel(TextColor, TextAnchor.MiddleRight, valueFont);
				_currentLabel.style.width = MetricBarCurrentWidth;
				_currentLabel.style.marginRight = 10f;
				_currentLabel.style.flexShrink = 0f;
				_statsLabel = CreateLabel(MutedTextColor, TextAnchor.MiddleLeft, statsFont);
				_statsLabel.style.flexGrow = 1f;

				_track = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};
				_track.style.position = Position.Relative;
				_track.style.width = MetricBarTrackWidth;
				_track.style.flexShrink = 0f;
				_track.style.height = 3f;
				_track.style.backgroundColor = WithAlpha(TextColor, 0.16f);
				_fill = CreateMarkerElement(3f, 0f);
				_fill.style.left = 0f;
				_fill.style.width = Length.Percent(0f);
				_averageMarker = CreateMarkerElement(7f, -2f);
				_onePercentMarker = CreateMarkerElement(7f, -2f);
				_pointOnePercentMarker = CreateMarkerElement(7f, -2f);
				_track.Add(_fill);
				_track.Add(_averageMarker);
				_track.Add(_onePercentMarker);
				_track.Add(_pointOnePercentMarker);

				_root.Add(_nameLabel);
				_root.Add(_minLabel);
				_root.Add(_track);
				_root.Add(_maxLabel);
				_root.Add(_currentLabel);
				_root.Add(_statsLabel);
			}

			internal VisualElement Root => _root;

			internal void SetValue(string name, StringBuilder min, StringBuilder max, StringBuilder current, StringBuilder stats, float currentPercent, float averagePercent, float onePercent, float pointOnePercent, Color accent)
			{
				SetName(name);
				SetCachedText(_minLabel, _minCache, min);
				SetCachedText(_maxLabel, _maxCache, max);
				SetCachedText(_currentLabel, _currentCache, current);
				SetCachedText(_statsLabel, _statsCache, stats);
				_minLabel.style.display = DisplayStyle.Flex;
				_track.style.display = DisplayStyle.Flex;
				_maxLabel.style.display = DisplayStyle.Flex;
				_currentLabel.style.display = DisplayStyle.Flex;
				_fill.style.width = Length.Percent(currentPercent);
				_fill.style.backgroundColor = accent;
				_track.style.backgroundColor = WithAlpha(accent, 0.18f);
				SetMarker(_averageMarker, averagePercent, WithAlpha(TextColor, 0.95f));
				SetMarker(_onePercentMarker, onePercent, WithAlpha(WarningColor, 0.95f));
				SetMarker(_pointOnePercentMarker, pointOnePercent, WithAlpha(accent, 0.95f));
				_nameLabel.style.color = MutedTextColor;
				_minLabel.style.color = MutedTextColor;
				_maxLabel.style.color = MutedTextColor;
				_currentLabel.style.color = accent;
				_statsLabel.style.color = TextColor;
			}

			internal void SetMessage(string name, string value, Color accent)
			{
				SetName(name);
				SetCachedText(_statsLabel, _statsCache, value);
				ApplyMessageStyle(accent);
			}

			internal void SetMessage(string name, StringBuilder value, Color accent)
			{
				SetName(name);
				SetCachedText(_statsLabel, _statsCache, value);
				ApplyMessageStyle(accent);
			}

			internal void SetMessage(StringBuilder name, StringBuilder value, Color accent)
			{
				SetName(name);
				SetCachedText(_statsLabel, _statsCache, value);
				ApplyMessageStyle(accent);
			}

			private void ApplyMessageStyle(Color accent)
			{
				_minLabel.style.display = DisplayStyle.None;
				_track.style.display = DisplayStyle.None;
				_maxLabel.style.display = DisplayStyle.None;
				_currentLabel.style.display = DisplayStyle.None;
				_nameLabel.style.color = accent;
				_statsLabel.style.color = MutedTextColor;
			}

			internal void SetFontSize(float fontSize)
			{
				_nameLabel.style.fontSize = fontSize;
				_minLabel.style.fontSize = Mathf.Max(8.5f, fontSize - 1f);
				_maxLabel.style.fontSize = Mathf.Max(8.5f, fontSize - 1f);
				_currentLabel.style.fontSize = fontSize + 1.5f;
				_statsLabel.style.fontSize = Mathf.Max(8.5f, fontSize - 1f);
			}

			internal void SetVisible(bool visible)
			{
				_root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}

			private void SetName(string name)
			{
				SetCachedText(_nameLabel, _nameCache, name);
			}

			private void SetName(StringBuilder name)
			{
				if (name == null)
				{
					SetCachedText(_nameLabel, _nameCache, string.Empty);
					return;
				}

				SetCachedText(_nameLabel, _nameCache, name);
			}

			private static void SetCachedText(Label label, PerfMeterOverlayCachedText cache, StringBuilder builder)
			{
				if (cache.TryUpdate(builder, out string text))
				{
					label.text = text;
				}
			}

			private static void SetCachedText(Label label, PerfMeterOverlayCachedText cache, string value)
			{
				if (cache.TryUpdate(value, out string text))
				{
					label.text = text;
				}
			}

			private static Label CreateLabel(Color color, TextAnchor align, Font font)
			{
				Label label = new Label
				{
					pickingMode = PickingMode.Ignore
				};
				label.style.color = color;
				label.style.unityTextAlign = align;
				label.style.unityFont = font;
				label.style.whiteSpace = WhiteSpace.NoWrap;
				label.style.overflow = Overflow.Hidden;
				return label;
			}

			private static VisualElement CreateMarkerElement(float height, float top)
			{
				VisualElement element = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};
				element.style.position = Position.Absolute;
				element.style.top = top;
				element.style.width = 1f;
				element.style.height = height;
				return element;
			}

			private static void SetMarker(VisualElement marker, float percent, Color color)
			{
				marker.style.left = Length.Percent(percent);
				marker.style.backgroundColor = color;
			}
		}

		private sealed class PerfMeterMetricCard : VisualElement
		{
			private readonly Label _titleLabel;
			private readonly Label _valueLabel;
			private readonly Label _captionLabel;
			private string _value = string.Empty;
			private string _caption = string.Empty;

			internal PerfMeterMetricCard(string title, Color accent, Font titleFont, Font valueFont, Font captionFont)
			{
				pickingMode = PickingMode.Ignore;
				style.width = WidgetCardWidth;
				style.height = WidgetCardHeight;
				style.marginRight = WidgetGap;
				style.paddingLeft = 10f;
				style.paddingRight = 10f;
				style.paddingTop = 8f;
				style.paddingBottom = 7f;
				style.flexDirection = FlexDirection.Column;
				style.justifyContent = Justify.SpaceBetween;
				style.overflow = Overflow.Hidden;

				_titleLabel = CreateCardLabel(title, MutedTextColor, TextAnchor.MiddleLeft, titleFont);
				_valueLabel = CreateCardLabel("--", TextColor, TextAnchor.MiddleLeft, valueFont);
				_captionLabel = CreateCardLabel(string.Empty, MutedTextColor, TextAnchor.MiddleLeft, captionFont);
				_valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
				Add(_titleLabel);
				Add(_valueLabel);
				Add(_captionLabel);
				SetAccent(accent);
			}

			internal void SetValue(string value, string caption, Color accent)
			{
				string safeValue = value ?? string.Empty;
				if (_value != safeValue)
				{
					_value = safeValue;
					_valueLabel.text = safeValue;
				}

				string safeCaption = caption ?? string.Empty;
				if (_caption != safeCaption)
				{
					_caption = safeCaption;
					_captionLabel.text = safeCaption;
				}

				SetAccent(accent);
			}

			internal void SetFontSizes(float title, float value, float caption)
			{
				_titleLabel.style.fontSize = title;
				_valueLabel.style.fontSize = value;
				_captionLabel.style.fontSize = caption;
			}

			private void SetAccent(Color accent)
			{
				Color border = WithAlpha(accent, 0.58f);
				style.backgroundColor = WithAlpha(accent, 0.10f);
				style.borderTopWidth = 1f;
				style.borderBottomWidth = 1f;
				style.borderLeftWidth = 2f;
				style.borderRightWidth = 1f;
				style.borderTopColor = border;
				style.borderBottomColor = WithAlpha(accent, 0.28f);
				style.borderLeftColor = accent;
				style.borderRightColor = WithAlpha(accent, 0.28f);
				_valueLabel.style.color = accent;
			}

			private static Label CreateCardLabel(string text, Color color, TextAnchor align, Font font)
			{
				Label label = new Label(text)
				{
					pickingMode = PickingMode.Ignore
				};
				label.style.color = color;
				label.style.unityTextAlign = align;
				label.style.unityFont = font;
				label.style.whiteSpace = WhiteSpace.NoWrap;
				label.style.overflow = Overflow.Hidden;
				return label;
			}
		}

		private sealed class PerfMeterBudgetBar : VisualElement
		{
			private readonly Label _titleLabel;
			private readonly Label _valueLabel;
			private readonly VisualElement _fill;
			private string _value = string.Empty;

			internal PerfMeterBudgetBar(string title, Color accent, Font titleFont, Font valueFont)
			{
				pickingMode = PickingMode.Ignore;
				style.width = BudgetBarWidth;
				style.height = 44f;
				style.marginRight = WidgetGap;
				style.paddingLeft = 10f;
				style.paddingRight = 10f;
				style.paddingTop = 6f;
				style.paddingBottom = 7f;
				style.backgroundColor = WithAlpha(accent, 0.08f);
				style.borderLeftWidth = 2f;
				style.borderTopWidth = 1f;
				style.borderRightWidth = 1f;
				style.borderBottomWidth = 1f;
				style.borderLeftColor = accent;
				style.borderTopColor = WithAlpha(accent, 0.36f);
				style.borderRightColor = WithAlpha(accent, 0.20f);
				style.borderBottomColor = WithAlpha(accent, 0.20f);

				VisualElement header = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};
				header.style.flexDirection = FlexDirection.Row;
				header.style.justifyContent = Justify.SpaceBetween;
				header.style.marginBottom = 5f;
				_titleLabel = CreateBarLabel(title, MutedTextColor, TextAnchor.MiddleLeft, titleFont);
				_valueLabel = CreateBarLabel("--", TextColor, TextAnchor.MiddleRight, valueFont);
				header.Add(_titleLabel);
				header.Add(_valueLabel);
				Add(header);

				VisualElement track = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};
				track.style.position = Position.Relative;
				track.style.height = 8f;
				track.style.width = Length.Percent(100f);
				track.style.backgroundColor = WithAlpha(TextColor, 0.12f);
				_fill = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};
				_fill.style.position = Position.Absolute;
				_fill.style.left = 0f;
				_fill.style.top = 0f;
				_fill.style.bottom = 0f;
				_fill.style.width = Length.Percent(0f);
				_fill.style.backgroundColor = accent;
				track.Add(_fill);
				Add(track);
			}

			internal void SetValue(double currentMs, double budgetMs, bool available, Color accent)
			{
				string text = available ? FormatMsValue(currentMs) + " / " + FormatMsValue(budgetMs) + " ms" : "-- / " + FormatMsValue(budgetMs) + " ms";
				if (_value != text)
				{
					_value = text;
					_valueLabel.text = text;
				}

				float percent = available && budgetMs > 0d ? Mathf.Clamp((float)(currentMs / budgetMs * 100d), 0f, 100f) : 0f;
				_fill.style.width = Length.Percent(percent);
				_fill.style.backgroundColor = accent;
				_valueLabel.style.color = available ? TextColor : UnavailableColor;
				style.backgroundColor = WithAlpha(accent, available ? 0.08f : 0.05f);
				style.borderLeftColor = accent;
				style.borderTopColor = WithAlpha(accent, 0.36f);
				style.borderRightColor = WithAlpha(accent, 0.20f);
				style.borderBottomColor = WithAlpha(accent, 0.20f);
			}

			internal void SetFontSize(float fontSize)
			{
				_titleLabel.style.fontSize = fontSize;
				_valueLabel.style.fontSize = fontSize;
			}

			private static Label CreateBarLabel(string text, Color color, TextAnchor align, Font font)
			{
				Label label = new Label(text)
				{
					pickingMode = PickingMode.Ignore
				};
				label.style.flexGrow = 1f;
				label.style.color = color;
				label.style.unityTextAlign = align;
				label.style.unityFont = font;
				label.style.whiteSpace = WhiteSpace.NoWrap;
				return label;
			}
		}

		internal sealed class PerfMeterOverlayCachedText
		{
			private string _text = string.Empty;

			internal string Text => _text;

			internal bool TryUpdate(StringBuilder builder, out string text)
			{
				if (Matches(builder))
				{
					text = _text;
					return false;
				}

				_text = builder.ToString();
				text = _text;
				return true;
			}

			internal bool TryUpdate(string value, out string text)
			{
				string safeValue = value ?? string.Empty;
				if (_text == safeValue)
				{
					text = _text;
					return false;
				}

				_text = safeValue;
				text = _text;
				return true;
			}

			private bool Matches(StringBuilder builder)
			{
				if (builder.Length != _text.Length)
				{
					return false;
				}

				for (int i = 0; i < builder.Length; i++)
				{
					if (builder[i] != _text[i])
					{
						return false;
					}
				}

				return true;
			}
		}

		private readonly struct LegendToken
		{
			internal LegendToken(string name, Color color)
			{
				Name = name;
				Color = color;
			}

			internal string Name { get; }
			internal Color Color { get; }
		}

		private enum PerfMeterGraphMode
		{
			Line,
			StackedCpu
		}

		private enum PerfMeterOverlayFontRole
		{
			Regular,
			Medium,
			SemiBold,
			Bold
		}

		private enum PerfMeterStatBarFormat
		{
			Fps,
			Milliseconds,
			Whole,
			Megabytes,
			Ratio
		}

		private readonly struct PerfMeterOverlayThemeTokens
		{
			private PerfMeterOverlayThemeTokens(Color background, Color graphBackground, Color text, Color mutedText, Color frame, Color cpuMain, Color cpuRender, Color cpuOther, Color gpu, Color warning, Color accent, Color unavailable, Color grid, Color budget)
			{
				Background = background;
				GraphBackground = graphBackground;
				Text = text;
				MutedText = mutedText;
				Frame = frame;
				CpuMain = cpuMain;
				CpuRender = cpuRender;
				CpuOther = cpuOther;
				Gpu = gpu;
				Warning = warning;
				Accent = accent;
				Unavailable = unavailable;
				Grid = grid;
				Budget = budget;
			}

			internal static PerfMeterOverlayThemeTokens ClassicDark => new PerfMeterOverlayThemeTokens(
				new Color(0.02f, 0.025f, 0.03f, 0.84f),
				new Color(0.04f, 0.055f, 0.065f, 0.92f),
				new Color(0.88f, 0.96f, 1f, 1f),
				new Color(0.68f, 0.82f, 0.9f, 1f),
				new Color(0.36f, 0.78f, 0.86f, 1f),
				new Color(0.88f, 0.63f, 0.32f, 1f),
				new Color(0.38f, 0.76f, 0.40f, 1f),
				new Color(0.50f, 0.44f, 0.82f, 1f),
				new Color(0.62f, 0.24f, 0.34f, 1f),
				new Color(1f, 0.34f, 0.30f, 1f),
				new Color(0.30f, 0.86f, 0.96f, 1f),
				new Color(0.34f, 0.38f, 0.42f, 1f),
				new Color(0.32f, 0.42f, 0.5f, 0.32f),
				new Color(1f, 0.32f, 0.28f, 0.75f));

			internal static PerfMeterOverlayThemeTokens Resolve(PerfMeterOverlayTheme theme)
			{
				switch (theme)
				{
					case PerfMeterOverlayTheme.Glass:
						return new PerfMeterOverlayThemeTokens(
							new Color(0.045f, 0.075f, 0.095f, 0.72f),
							new Color(0.03f, 0.07f, 0.075f, 0.82f),
							new Color(0.91f, 1f, 0.96f, 1f),
							new Color(0.68f, 0.86f, 0.84f, 1f),
							new Color(0.43f, 0.94f, 0.60f, 1f),
							new Color(0.95f, 0.70f, 0.38f, 1f),
							new Color(0.36f, 0.80f, 0.52f, 1f),
							new Color(0.42f, 0.68f, 0.90f, 1f),
							new Color(0.96f, 0.33f, 0.52f, 1f),
							new Color(1f, 0.34f, 0.38f, 1f),
							new Color(0.46f, 0.92f, 1f, 1f),
							new Color(0.38f, 0.46f, 0.48f, 1f),
							new Color(0.72f, 0.95f, 0.90f, 0.22f),
							new Color(1f, 0.42f, 0.35f, 0.72f));
					case PerfMeterOverlayTheme.Cyber:
						return new PerfMeterOverlayThemeTokens(
							new Color(0.005f, 0.018f, 0.035f, 0.90f),
							new Color(0.005f, 0.025f, 0.045f, 0.94f),
							new Color(0.86f, 0.98f, 1f, 1f),
							new Color(0.48f, 0.68f, 0.80f, 1f),
							new Color(0.13f, 0.92f, 1f, 1f),
							new Color(1f, 0.58f, 0.12f, 1f),
							new Color(0.24f, 0.92f, 0.45f, 1f),
							new Color(0.70f, 0.38f, 1f, 1f),
							new Color(0.64f, 0.32f, 1f, 1f),
							new Color(1f, 0.22f, 0.36f, 1f),
							new Color(1f, 0.82f, 0.16f, 1f),
							new Color(0.22f, 0.30f, 0.42f, 1f),
							new Color(0.13f, 0.92f, 1f, 0.24f),
							new Color(1f, 0.22f, 0.36f, 0.82f));
					case PerfMeterOverlayTheme.HighContrast:
						return new PerfMeterOverlayThemeTokens(
							new Color(0.014f, 0.018f, 0.018f, 0.94f),
							new Color(0.02f, 0.045f, 0.045f, 0.96f),
							new Color(0.88f, 1f, 0.95f, 1f),
							new Color(0.58f, 0.82f, 0.80f, 1f),
							new Color(0.18f, 0.93f, 0.96f, 1f),
							new Color(0.95f, 0.72f, 0.28f, 1f),
							new Color(0.36f, 0.90f, 0.50f, 1f),
							new Color(0.32f, 0.64f, 0.90f, 1f),
							new Color(1f, 0.30f, 0.58f, 1f),
							new Color(1f, 0.36f, 0.34f, 1f),
							new Color(0.36f, 0.98f, 0.92f, 1f),
							new Color(0.38f, 0.44f, 0.46f, 1f),
							new Color(0.22f, 0.95f, 0.90f, 0.24f),
							new Color(1f, 0.30f, 0.24f, 0.90f));
					default:
						return ClassicDark;
				}
			}

			internal Color Background { get; }
			internal Color GraphBackground { get; }
			internal Color Text { get; }
			internal Color MutedText { get; }
			internal Color Frame { get; }
			internal Color CpuMain { get; }
			internal Color CpuRender { get; }
			internal Color CpuOther { get; }
			internal Color Gpu { get; }
			internal Color Warning { get; }
			internal Color Accent { get; }
			internal Color Unavailable { get; }
			internal Color Grid { get; }
			internal Color Budget { get; }
		}

		private readonly struct PerfMeterSeriesStats
		{
			internal PerfMeterSeriesStats(int count, double current, double average, double onePercentHigh, double pointOnePercentHigh, double min, double max)
			{
				Count = count;
				Current = current;
				Average = average;
				OnePercentHigh = onePercentHigh;
				PointOnePercentHigh = pointOnePercentHigh;
				Min = min;
				Max = max;
			}

			internal int Count { get; }
			internal double Current { get; }
			internal double Average { get; }
			internal double OnePercentHigh { get; }
			internal double PointOnePercentHigh { get; }
			internal double Min { get; }
			internal double Max { get; }
		}

		private readonly struct PerfMeterHistoryStats
		{
			internal PerfMeterHistoryStats(int count, double current, double min, double max, double average, double onePercent, double pointOnePercent)
			{
				Count = count;
				Current = current;
				Min = min;
				Max = max;
				Average = average;
				OnePercent = onePercent;
				PointOnePercent = pointOnePercent;
			}

			internal static PerfMeterHistoryStats FromCurrent(double value)
			{
				return new PerfMeterHistoryStats(1, value, value, value, value, value, value);
			}

			internal int Count { get; }
			internal double Current { get; }
			internal double Min { get; }
			internal double Max { get; }
			internal double Average { get; }
			internal double OnePercent { get; }
			internal double PointOnePercent { get; }
		}

		private sealed class PerfMeterGraphElement : VisualElement
		{
			private static Color FrameFillColor => WithAlpha(FrameColor, 0.34f);
			private static Color MainFillColor => WithAlpha(MainColor, 0.55f);
			private static Color RenderFillColor => WithAlpha(RenderColor, 0.55f);
			private static Color OtherFillColor => WithAlpha(OtherCpuColor, 0.45f);

			private double[] _primary;
			private double[] _secondary;
			private double[] _tertiary;
			private bool[] _valid;
			private double[] _scratch;
			private readonly PerfMeterGraphMode _mode;
			private readonly float _height;
			private readonly Label _maxScaleLabel;
			private readonly Label _budgetLabel;
			private int _capacity;
			private int _index;
			private int _count;
			private double _frameBudgetMs = PerfMeterCollector.DefaultFrameBudgetMs;

			internal PerfMeterGraphElement(string name, PerfMeterGraphMode mode, float height, Label maxScaleLabel, Label budgetLabel, int historyCapacity)
			{
				this.name = name;
				_mode = mode;
				_height = height;
				_maxScaleLabel = maxScaleLabel;
				_budgetLabel = budgetLabel;
				SetHistoryCapacity(historyCapacity);
				pickingMode = PickingMode.Ignore;
				style.width = GraphPlotWidth;
				style.height = height;
				style.backgroundColor = GraphBackgroundColor;
				UpdateScaleLabels();
				generateVisualContent += OnGenerateVisualContent;
			}

			internal double ScaleMs { get; private set; } = PerfMeterCollector.DefaultFrameBudgetMs * 2d;

			internal void SetHistoryCapacity(int historyCapacity)
			{
				int normalized = Mathf.Clamp(historyCapacity, PerfMeterSettingsStore.MinOverlayGraphHistoryLength, PerfMeterSettingsStore.MaxOverlayGraphHistoryLength);
				if (_capacity == normalized && _primary != null)
				{
					return;
				}

				_capacity = normalized;
				_primary = new double[_capacity];
				_secondary = new double[_capacity];
				_tertiary = new double[_capacity];
				_valid = new bool[_capacity];
				_scratch = new double[_capacity];
				_index = 0;
				_count = 0;
				ScaleMs = CalculateScaleMs();
				UpdateScaleLabels();
				MarkDirtyRepaint();
			}

			internal void SetFrameBudgetMs(double frameBudgetMs)
			{
				_frameBudgetMs = frameBudgetMs > 0d && !double.IsNaN(frameBudgetMs) && !double.IsInfinity(frameBudgetMs)
					? frameBudgetMs
					: PerfMeterCollector.DefaultFrameBudgetMs;
				ScaleMs = CalculateScaleMs();
				UpdateScaleLabels();
				MarkDirtyRepaint();
			}

			internal void AddSample(double primary, double secondary, double tertiary, bool valid)
			{
				_primary[_index] = Sanitize(primary);
				_secondary[_index] = Sanitize(secondary);
				_tertiary[_index] = Sanitize(tertiary);
				_valid[_index] = valid;
				_index = (_index + 1) % _capacity;

				if (_count < _capacity)
				{
					_count++;
				}

				ScaleMs = CalculateScaleMs();
				UpdateScaleLabels();
				MarkDirtyRepaint();
			}

			private void UpdateScaleLabels()
			{
				_maxScaleLabel.text = FormatMsFixedValue(ScaleMs, ScaleMs) + " ms";
				_maxScaleLabel.style.top = 0f;

				_budgetLabel.text = FormatMsFixedValue(_frameBudgetMs, ScaleMs) + " ms";
				float budgetTop = ValueToY(new Rect(0f, 0f, GraphPlotWidth, _height), _frameBudgetMs, ScaleMs) - ScaleLabelHeight * 0.5f;
				float minBudgetTop = ScaleLabelHeight + ScaleLabelSeparation;
				float maxBudgetTop = Mathf.Max(minBudgetTop, _height - ScaleLabelHeight);
				_budgetLabel.style.top = Mathf.Clamp(budgetTop, minBudgetTop, maxBudgetTop);
			}

			internal PerfMeterSeriesStats GetStats(int series)
			{
				return CalculateStats(GetSeriesValues(series));
			}

			internal PerfMeterSeriesStats GetCpuOtherStats()
			{
				int count = 0;
				double total = 0d;
				double min = double.MaxValue;
				double max = 0d;
				double current = 0d;

				for (int sample = 0; sample < _count; sample++)
				{
					int index = BufferIndex(sample);
					if (!_valid[index])
					{
						continue;
					}

					double value = CalculateCpuOther(_primary[index], _secondary[index], _tertiary[index]);
					_scratch[count++] = value;
					total += value;
					min = Math.Min(min, value);
					max = Math.Max(max, value);
					current = value;
				}

				if (count == 0)
				{
					return default;
				}

				Array.Sort(_scratch, 0, count);
				return new PerfMeterSeriesStats(
					count,
					current,
					total / count,
					CalculateHighAverage(count, 0.01d),
					CalculateHighAverage(count, 0.001d),
					min,
					max);
			}

			private void OnGenerateVisualContent(MeshGenerationContext context)
			{
				Rect rect = contentRect;
				if (rect.width <= 1f || rect.height <= 1f)
				{
					return;
				}

				Painter2D painter = context.painter2D;
				DrawGrid(painter, rect, ScaleMs);

				if (_mode == PerfMeterGraphMode.StackedCpu)
				{
					DrawStackedCpu(painter, rect, ScaleMs);
				}
				else
				{
					DrawSeries(painter, rect, _primary, GpuColor, ScaleMs, 1.6f);
				}
			}

			private double CalculateScaleMs()
			{
				PerfMeterSeriesStats primaryStats = GetStats(0);
				double average = primaryStats.Count > 0 ? primaryStats.Average : 0d;
				double scale = Math.Max(average * 1.1d, _frameBudgetMs * 1.2d);
				return Math.Max(1d, Math.Ceiling(scale));
			}

			private void DrawGrid(Painter2D painter, Rect rect, double scaleMs)
			{
				painter.lineWidth = 1f;
				painter.strokeColor = GridColor;
				painter.BeginPath();
				painter.MoveTo(new Vector2(rect.xMin, rect.center.y));
				painter.LineTo(new Vector2(rect.xMax, rect.center.y));
				painter.Stroke();

				float budgetY = ValueToY(rect, _frameBudgetMs, scaleMs);
				painter.strokeColor = BudgetColor;
				painter.BeginPath();
				painter.MoveTo(new Vector2(rect.xMin, budgetY));
				painter.LineTo(new Vector2(rect.xMax, budgetY));
				painter.Stroke();
			}

			private void DrawStackedCpu(Painter2D painter, Rect rect, double scaleMs)
			{
				if (_count <= 1)
				{
					return;
				}

				for (int sample = 0; sample < _count - 1; sample++)
				{
					int firstIndex = BufferIndex(sample);
					int secondIndex = BufferIndex(sample + 1);
					if (!_valid[firstIndex] || !_valid[secondIndex])
					{
						continue;
					}

					float x1 = rect.xMin + rect.width * sample / (_count - 1);
					float x2 = rect.xMin + rect.width * (sample + 1) / (_count - 1);
					CpuStack firstStack = CalculateCpuStack(_primary[firstIndex], _secondary[firstIndex], _tertiary[firstIndex]);
					CpuStack secondStack = CalculateCpuStack(_primary[secondIndex], _secondary[secondIndex], _tertiary[secondIndex]);

					DrawBand(painter, rect, x1, x2, 0d, firstStack.Render, 0d, secondStack.Render, RenderFillColor, scaleMs);
					DrawBand(painter, rect, x1, x2, firstStack.Render, firstStack.Render + firstStack.Main, secondStack.Render, secondStack.Render + secondStack.Main, MainFillColor, scaleMs);
					DrawBand(painter, rect, x1, x2, firstStack.Render + firstStack.Main, firstStack.Frame, secondStack.Render + secondStack.Main, secondStack.Frame, OtherFillColor, scaleMs);
				}

				DrawSeries(painter, rect, _primary, FrameColor, scaleMs, 1.6f);
			}

			private void DrawBand(Painter2D painter, Rect rect, float x1, float x2, double bottom1, double top1, double bottom2, double top2, Color color, double scaleMs)
			{
				if (top1 <= bottom1 && top2 <= bottom2)
				{
					return;
				}

				painter.fillColor = color;
				painter.BeginPath();
				painter.MoveTo(new Vector2(x1, ValueToY(rect, bottom1, scaleMs)));
				painter.LineTo(new Vector2(x1, ValueToY(rect, top1, scaleMs)));
				painter.LineTo(new Vector2(x2, ValueToY(rect, top2, scaleMs)));
				painter.LineTo(new Vector2(x2, ValueToY(rect, bottom2, scaleMs)));
				painter.ClosePath();
				painter.Fill();
			}

			private void DrawSeries(Painter2D painter, Rect rect, double[] values, Color color, double scaleMs, float lineWidth)
			{
				if (_count <= 0)
				{
					return;
				}

				painter.lineWidth = lineWidth;
				painter.strokeColor = color;

				bool pathOpen = false;
				for (int sample = 0; sample < _count; sample++)
				{
					int index = BufferIndex(sample);
					double value = values[index];
					if (!_valid[index] || value <= 0d)
					{
						if (pathOpen)
						{
							painter.Stroke();
							pathOpen = false;
						}

						continue;
					}

					float x = rect.xMin;
					if (_count > 1)
					{
						x += rect.width * sample / (_count - 1);
					}

					float y = ValueToY(rect, value, scaleMs);
					if (!pathOpen)
					{
						painter.BeginPath();
						painter.MoveTo(new Vector2(x, y));
						pathOpen = true;
					}
					else
					{
						painter.LineTo(new Vector2(x, y));
					}
				}

				if (pathOpen)
				{
					painter.Stroke();
				}
			}

			private PerfMeterSeriesStats CalculateStats(double[] values)
			{
				int count = 0;
				double total = 0d;
				double min = double.MaxValue;
				double max = 0d;
				double current = 0d;

				for (int sample = 0; sample < _count; sample++)
				{
					int index = BufferIndex(sample);
					if (!_valid[index] || values[index] <= 0d)
					{
						continue;
					}

					double value = values[index];
					_scratch[count++] = value;
					total += value;
					min = Math.Min(min, value);
					max = Math.Max(max, value);
					current = value;
				}

				if (count == 0)
				{
					return default;
				}

				Array.Sort(_scratch, 0, count);
				return new PerfMeterSeriesStats(
					count,
					current,
					total / count,
					CalculateHighAverage(count, 0.01d),
					CalculateHighAverage(count, 0.001d),
					min,
					max);
			}

			private double CalculateHighAverage(int count, double percentile)
			{
				int highCount = Mathf.Clamp(Mathf.CeilToInt((float)(count * percentile)), 1, count);
				double total = 0d;
				for (int i = count - highCount; i < count; i++)
				{
					total += _scratch[i];
				}

				return total / highCount;
			}

			private double[] GetSeriesValues(int series)
			{
				switch (series)
				{
					case 1:
						return _secondary;
					case 2:
						return _tertiary;
					default:
						return _primary;
				}
			}

			private int BufferIndex(int sample)
			{
				return (_index - _count + sample + _capacity) % _capacity;
			}

			private static CpuStack CalculateCpuStack(double frame, double main, double render)
			{
				frame = Sanitize(frame);
				main = Sanitize(main);
				render = Sanitize(render);
				double renderPart = Math.Min(render, frame);
				double mainPart = Math.Min(main, Math.Max(0d, frame - renderPart));
				return new CpuStack(frame, mainPart, renderPart);
			}

			private static double CalculateCpuOther(double frame, double main, double render)
			{
				CpuStack stack = CalculateCpuStack(frame, main, render);
				return Math.Max(0d, stack.Frame - stack.Main - stack.Render);
			}

			private static float ValueToY(Rect rect, double value, double scaleMs)
			{
				float normalized = Mathf.Clamp01((float)(value / scaleMs));
				return rect.yMax - normalized * rect.height;
			}

			private static double Sanitize(double value)
			{
				return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0d;
			}

			private readonly struct CpuStack
			{
				internal CpuStack(double frame, double main, double render)
				{
					Frame = frame;
					Main = main;
					Render = render;
				}

				internal double Frame { get; }
				internal double Main { get; }
				internal double Render { get; }
			}
		}

		private readonly struct PerfMeterHistoryRange
		{
			internal PerfMeterHistoryRange(int count, double min, double max)
			{
				Count = count;
				Min = min;
				Max = max;
			}

			internal int Count { get; }
			internal double Min { get; }
			internal double Max { get; }
		}

		private sealed class PerfMeterHistorySeries
		{
			private const int Capacity = 600;
			private readonly double[] _values = new double[Capacity];
			private readonly double[] _scratch = new double[Capacity];
			private int _index;
			private int _count;

			internal void Add(double value, bool valid = true)
			{
				if (!valid || double.IsNaN(value) || double.IsInfinity(value))
				{
					return;
				}

				_values[_index] = value;
				_index = (_index + 1) % Capacity;
				_count = Mathf.Min(_count + 1, Capacity);
			}

			internal PerfMeterHistoryRange GetRange()
			{
				if (_count == 0)
				{
					return default;
				}

				double min = double.MaxValue;
				double max = double.MinValue;
				int validCount = 0;
				for (int i = 0; i < _count; i++)
				{
					double value = _values[i];
					min = Math.Min(min, value);
					max = Math.Max(max, value);
					validCount++;
				}

				return validCount > 0 ? new PerfMeterHistoryRange(validCount, min, max) : default;
			}

			internal bool TryGetLast(out double value)
			{
				value = 0d;
				if (_count == 0)
				{
					return false;
				}

				int lastIndex = (_index - 1 + Capacity) % Capacity;
				value = _values[lastIndex];
				return true;
			}

			internal PerfMeterHistoryStats GetStats()
			{
				if (_count == 0)
				{
					return default;
				}

				double total = 0d;
				double min = double.MaxValue;
				double max = double.MinValue;
				int validCount = 0;
				for (int i = 0; i < _count; i++)
				{
					double value = _values[i];
					if (!IsFinite(value))
					{
						continue;
					}

					_scratch[validCount++] = value;
					total += value;
					min = Math.Min(min, value);
					max = Math.Max(max, value);
				}

				if (validCount == 0)
				{
					return default;
				}

				Array.Sort(_scratch, 0, validCount);
				TryGetLast(out double current);
				return new PerfMeterHistoryStats(
					validCount,
					current,
					min,
					max,
					total / validCount,
					CalculateHighAverage(validCount, 0.01d),
					CalculateHighAverage(validCount, 0.001d));
			}

			private double CalculateHighAverage(int count, double percentile)
			{
				int highCount = Mathf.Clamp(Mathf.CeilToInt((float)(count * percentile)), 1, count);
				double total = 0d;
				for (int i = count - highCount; i < count; i++)
				{
					total += _scratch[i];
				}

				return total / highCount;
			}
		}

		private sealed class PerfMeterOverlayHistory
		{
			internal readonly PerfMeterHistorySeries CpuFrameTimeMs = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries CpuMainThreadFrameTimeMs = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries CpuRenderThreadFrameTimeMs = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries CpuPresentWaitTimeMs = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries GpuFrameTimeMs = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries DrawCalls = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries SetPassCalls = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries Batches = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries Vertices = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries SrpBatcherInstances = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries BrgDrawCalls = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries BrgInstances = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries IndexUploadBytes = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries SystemMemoryBytes = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries GcReservedMemoryBytes = new PerfMeterHistorySeries();
			internal readonly PerfMeterHistorySeries GpuMemoryBytes = new PerfMeterHistorySeries();

			internal void AddSample(PerfMeterMetricsSnapshot metrics, PerfMeterStatusSnapshot status)
			{
				CpuFrameTimeMs.Add(metrics.CpuFrameTimeMs, metrics.CpuFrameTimeMs > 0d);
				CpuMainThreadFrameTimeMs.Add(metrics.CpuMainThreadFrameTimeMs, metrics.CpuMainThreadFrameTimeMs > 0d);
				CpuRenderThreadFrameTimeMs.Add(metrics.CpuRenderThreadFrameTimeMs, metrics.CpuRenderThreadFrameTimeMs > 0d);
				CpuPresentWaitTimeMs.Add(metrics.CpuMainThreadPresentWaitTimeMs, metrics.CpuMainThreadPresentWaitTimeMs > 0d);
				if (metrics.GpuFrameTimeAvailable)
				{
					GpuFrameTimeMs.Add(metrics.GpuFrameTimeMs, metrics.GpuFrameTimeMs > 0d);
				}

				DrawCalls.Add(metrics.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls));
				SetPassCalls.Add(metrics.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls));
				Batches.Add(metrics.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches));
				Vertices.Add(metrics.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices));
				SrpBatcherInstances.Add(metrics.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances));
				BrgDrawCalls.Add(metrics.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls));
				BrgInstances.Add(metrics.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances));
				IndexUploadBytes.Add(metrics.IndexBufferUploadInFrameBytes, HasCounter(status, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes));
				SystemMemoryBytes.Add(metrics.SystemUsedMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory));
				GcReservedMemoryBytes.Add(metrics.GcReservedMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GcReservedMemory));
				GpuMemoryBytes.Add(metrics.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory));
			}
		}
	}
}
