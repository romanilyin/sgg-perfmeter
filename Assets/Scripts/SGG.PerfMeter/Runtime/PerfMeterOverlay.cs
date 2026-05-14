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
		private const float RefreshIntervalSeconds = 0.25f;
		private const float GraphBlockWidth = 780f;
		private const float TextBlockWidth = 520f;
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

		private static readonly Color BackgroundColor = new Color(0.02f, 0.025f, 0.03f, 0.84f);
		private static readonly Color TextColor = new Color(0.88f, 0.96f, 1f, 1f);
		private static readonly Color MutedTextColor = new Color(0.68f, 0.82f, 0.9f, 1f);
		private static readonly Color FrameColor = new Color(0.36f, 0.78f, 0.86f, 1f);
		private static readonly Color OtherCpuColor = new Color(0.50f, 0.44f, 0.82f, 1f);
		private static readonly Color MainColor = new Color(0.88f, 0.63f, 0.32f, 1f);
		private static readonly Color RenderColor = new Color(0.38f, 0.76f, 0.40f, 1f);
		private static readonly Color GpuColor = new Color(0.62f, 0.24f, 0.34f, 1f);
		private static readonly Color UnavailableColor = new Color(0.34f, 0.38f, 0.42f, 1f);

		private readonly StringBuilder _builder = new StringBuilder(1024);
		private readonly PerfMeterOverlayHistory _history = new PerfMeterOverlayHistory();
		private UIDocument _document;
		private PanelSettings _panelSettings;
		private PanelTextSettings _panelTextSettings;
		private ThemeStyleSheet _themeStyleSheet;
		private UnityEngine.TextCore.Text.FontAsset _fontAsset;
		private VisualElement _container;
		private VisualElement _graphBlock;
		private VisualElement _textBlock;
		private VisualElement _graphs;
		private Label _label;
		private Label _cpuFrameLegend;
		private Label _cpuMainLegend;
		private Label _cpuRenderLegend;
		private Label _cpuOtherLegend;
		private Label _gpuLegend;
		private PerfMeterGraphElement _cpuGraph;
		private PerfMeterGraphElement _gpuGraph;
		private float _nextRefreshTime;
		private float _warningVisibleUntil;
		private string _heldWarning = string.Empty;
		private PerfMeterOverlayCorner _corner = PerfMeterOverlayCorner.TopRight;
		private PerfMeterOverlayMode _mode = PerfMeterOverlayMode.Full;
		private bool _isVisible = true;

		internal bool IsVisible => _isVisible && isActiveAndEnabled;

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
			_container.style.unityFont = GetRuntimeFont();

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
			_label = new Label
			{
				name = "sgg-perfmeter-metrics",
				pickingMode = PickingMode.Ignore
			};
			_label.style.width = Length.Percent(100f);
			_label.style.color = TextColor;
			_label.style.fontSize = 12f;
			_label.style.unityFont = GetRuntimeFont();
			_label.style.unityFontStyleAndWeight = FontStyle.Normal;
			_label.style.unityTextAlign = TextAnchor.UpperLeft;
			_label.style.whiteSpace = WhiteSpace.Normal;
			_textBlock.Add(_label);

			_container.Add(_graphBlock);
			_container.Add(_textBlock);
			root.Add(_container);
			ApplyModeLayout();
			ApplyCorner();
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
			block.style.backgroundColor = BackgroundColor;
			block.style.flexDirection = FlexDirection.Column;
			block.style.unityFont = GetRuntimeFont();
			return block;
		}

		private void BuildGraphRows()
		{
			VisualElement cpuHeader = CreateHeaderRow("CPU", new LegendToken("frame", FrameColor), new LegendToken("other", OtherCpuColor), new LegendToken("main", MainColor), new LegendToken("render", RenderColor));
			_graphs.Add(cpuHeader);

			VisualElement cpuRow = CreateGraphRow(CpuGraphHeight);
			Label cpuMaxScaleLabel;
			Label cpuBudgetLabel;
			VisualElement cpuScale = CreateScaleLabelColumn(CpuGraphHeight, out cpuMaxScaleLabel, out cpuBudgetLabel);
			_cpuGraph = new PerfMeterGraphElement("sgg-perfmeter-cpu-graph", PerfMeterGraphMode.StackedCpu, CpuGraphHeight, cpuMaxScaleLabel, cpuBudgetLabel);
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
			_gpuGraph = new PerfMeterGraphElement("sgg-perfmeter-gpu-graph", PerfMeterGraphMode.Line, GpuGraphHeight, gpuMaxScaleLabel, gpuBudgetLabel);
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

			Label titleLabel = CreateSmallLabel(title + ":", TextColor, TextAnchor.MiddleLeft);
			titleLabel.style.marginRight = 5f;
			row.Add(titleLabel);

			for (int i = 0; i < tokens.Length; i++)
			{
				Label tokenLabel = CreateSmallLabel(tokens[i].Name, tokens[i].Color, TextAnchor.MiddleLeft);
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
			budgetLabel = CreateScaleLabel(new Color(1f, 0.32f, 0.28f, 0.95f));
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
			label.style.unityFont = GetRuntimeFont();
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
			Label label = CreateSmallLabel(text, GetReadableTextColor(color), TextAnchor.MiddleLeft);
			label.style.height = 17f;
			label.style.fontSize = 9.5f;
			label.style.marginTop = 2f;
			label.style.paddingLeft = 4f;
			label.style.paddingRight = 4f;
			label.style.backgroundColor = color;
			return label;
		}

		private static Label CreateSmallLabel(string text, Color color, TextAnchor align)
		{
			Label label = new Label(text)
			{
				pickingMode = PickingMode.Ignore
			};
			label.style.color = color;
			label.style.fontSize = 11f;
			label.style.unityFont = GetRuntimeFont();
			label.style.unityTextAlign = align;
			label.style.whiteSpace = WhiteSpace.NoWrap;
			return label;
		}

		private void ApplyModeLayout()
		{
			if (_container == null)
			{
				return;
			}

			bool showGraphs = _mode == PerfMeterOverlayMode.Graphs || _mode == PerfMeterOverlayMode.Full;
			_graphBlock.style.display = showGraphs ? DisplayStyle.Flex : DisplayStyle.None;

			float containerWidth = showGraphs ? GraphBlockWidth : TextBlockWidth;
			_container.style.width = containerWidth;
			_textBlock.style.width = TextBlockWidth;
			_textBlock.style.height = GetTextBlockHeight();
			if (showGraphs)
			{
				_graphBlock.style.height = StyleKeyword.Auto;
			}
			else
			{
				_graphBlock.style.height = 0f;
			}

			if (_mode == PerfMeterOverlayMode.FpsOnly)
			{
				_label.style.fontSize = 13f;
				_label.style.flexGrow = 1f;
				_label.style.unityTextAlign = TextAnchor.MiddleLeft;
			}
			else
			{
				_label.style.fontSize = _mode == PerfMeterOverlayMode.Full ? 12f : 13f;
				_label.style.flexGrow = 1f;
				_label.style.unityTextAlign = TextAnchor.UpperLeft;
			}

			ApplyBlockAlignment();
		}

		private float GetTextBlockHeight()
		{
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

		private void ApplyBlockAlignment()
		{
			if (_textBlock == null || _graphBlock == null)
			{
				return;
			}

			bool rightAligned = _corner == PerfMeterOverlayCorner.TopRight || _corner == PerfMeterOverlayCorner.BottomRight;
			Align align = rightAligned ? Align.FlexEnd : Align.FlexStart;
			_textBlock.style.alignSelf = align;
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
			if (_label == null)
			{
				return;
			}

			if (!force && Time.unscaledTime < _nextRefreshTime)
			{
				return;
			}

			_nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
			PerfMeterStatusSnapshot status = PerfMeter.GetStatus();
			PerfMeterMetricsSnapshot metrics = PerfMeter.GetLatestMetrics();
			string warning = ResolveDisplayWarning(status.Warning);
			_history.AddSample(metrics, status);

			UpdateGraphs(status, metrics);

			_builder.Length = 0;
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

			_label.text = _builder.ToString();
		}

		private void UpdateGraphs(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics)
		{
			if (_cpuGraph == null || _gpuGraph == null)
			{
				return;
			}

			double frameBudgetMs = PerfMeterRuntime.GetFrameBudgetMs(status.TargetFps);
			_cpuGraph.SetFrameBudgetMs(frameBudgetMs);
			_gpuGraph.SetFrameBudgetMs(frameBudgetMs);

			bool cpuValid = metrics.CpuFrameTimeMs > 0d;
			_cpuGraph.AddSample(
				metrics.CpuFrameTimeMs,
				metrics.CpuMainThreadFrameTimeMs,
				metrics.CpuRenderThreadFrameTimeMs,
				cpuValid);
			if (metrics.GpuFrameTimeAvailable && metrics.GpuFrameTimeMs > 0d)
			{
				_gpuGraph.AddSample(metrics.GpuFrameTimeMs, 0d, 0d, true);
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

		private void BuildFpsOnlyText(PerfMeterMetricsSnapshot metrics, string warning)
		{
			AppendFpsSummary(metrics);
			if (!string.IsNullOrEmpty(warning))
			{
				_builder.Append(" | Warn");
			}
		}

		private void BuildTextCompactText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			_builder.Append("SGG PerfMeter ");
			_builder.Append(status.State.ToString());
			_builder.Append(" / ");
			_builder.Append(metrics.Bottleneck.ToString());
			_builder.Append('\n');
			AppendFpsLine(metrics);
			AppendTimingLineWithRanges(metrics);
			AppendIntPairWithRanges("Draw/SetPass", metrics.DrawCalls, _history.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls), metrics.SetPassCalls, _history.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls));
			AppendIntPairWithRanges("Batches/Verts", metrics.Batches, _history.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches), metrics.Vertices, _history.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices));
			AppendLine("SRP/BRG", FormatIntWithRange(metrics.SrpBatcherInstances, _history.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances)) + " / " + FormatIntWithRange(metrics.BrgDrawCalls, _history.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls)) + ":" + FormatIntWithRange(metrics.BrgInstances, _history.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances)));
			AppendOverdraw(metrics);
			AppendMemoryPairWithRanges("Mem/GPU", metrics.SystemUsedMemoryBytes, _history.SystemMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory), metrics.GpuMemoryBytes, _history.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory));
			AppendGpuValidity(metrics);
			AppendWarning(warning, 120);
		}

		private void BuildGraphsText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			_builder.Append("SGG PerfMeter Graphs ");
			_builder.Append(status.State.ToString());
			_builder.Append('\n');
			AppendFpsLine(metrics);
			AppendLine("Bottleneck", metrics.Bottleneck.ToString());
			AppendGpuValidity(metrics);
			AppendWarning(warning, 140);
		}

		private void BuildFullText(PerfMeterStatusSnapshot status, PerfMeterMetricsSnapshot metrics, string warning)
		{
			_builder.Append("SGG PerfMeter ");
			_builder.Append(status.State.ToString());
			_builder.Append(" / ");
			_builder.Append(metrics.Bottleneck.ToString());
			_builder.Append('\n');
			AppendFpsLine(metrics);
			AppendLine("Spikes", metrics.FrameSpikeCount.ToString(CultureInfo.InvariantCulture) + " / severe " + metrics.SevereFrameSpikeCount.ToString(CultureInfo.InvariantCulture));
			AppendMsWithRange("CPU Frame", metrics.CpuFrameTimeMs, _history.CpuFrameTimeMs);
			AppendMsWithRange("CPU Main", metrics.CpuMainThreadFrameTimeMs, _history.CpuMainThreadFrameTimeMs);
			AppendMsWithRange("CPU Render", metrics.CpuRenderThreadFrameTimeMs, _history.CpuRenderThreadFrameTimeMs);
			AppendMsWithRange("GPU", GetDisplayGpuFrameTime(metrics), _history.GpuFrameTimeMs, true, metrics.GpuFrameTimeAvailable);
			AppendMsWithRange("Present Wait", metrics.CpuMainThreadPresentWaitTimeMs, _history.CpuPresentWaitTimeMs);
			AppendGpuValidity(metrics);
			AppendIntWithRange("Draw Calls", metrics.DrawCalls, _history.DrawCalls, HasCounter(status, PerfMeterCounterAvailability.DrawCalls));
			AppendIntWithRange("SetPass", metrics.SetPassCalls, _history.SetPassCalls, HasCounter(status, PerfMeterCounterAvailability.SetPassCalls));
			AppendIntWithRange("Batches", metrics.Batches, _history.Batches, HasCounter(status, PerfMeterCounterAvailability.Batches));
			AppendIntWithRange("Vertices", metrics.Vertices, _history.Vertices, HasCounter(status, PerfMeterCounterAvailability.Vertices));
			AppendIntWithRange("SRP Instances", metrics.SrpBatcherInstances, _history.SrpBatcherInstances, HasCounter(status, PerfMeterCounterAvailability.SrpBatcherInstances));
			AppendIntPairWithRanges("BRG Draw/Inst", metrics.BrgDrawCalls, _history.BrgDrawCalls, HasCounter(status, PerfMeterCounterAvailability.BrgDrawCalls), metrics.BrgInstances, _history.BrgInstances, HasCounter(status, PerfMeterCounterAvailability.BrgInstances));
			AppendMemoryWithRange("Index Upload", metrics.IndexBufferUploadInFrameBytes, _history.IndexUploadBytes, HasCounter(status, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes));
			AppendOverdraw(metrics);
			AppendMemoryWithRange("Memory", metrics.SystemUsedMemoryBytes, _history.SystemMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.SystemUsedMemory));
			AppendMemoryWithRange("GC Reserved", metrics.GcReservedMemoryBytes, _history.GcReservedMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GcReservedMemory));
			AppendMemoryWithRange("GPU Memory", metrics.GpuMemoryBytes, _history.GpuMemoryBytes, HasCounter(status, PerfMeterCounterAvailability.GpuMemory));
			AppendWarning(warning, 180);
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

			Font font = GetRuntimeFont();
			if (font == null)
			{
				return null;
			}

			_fontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font);
			_fontAsset.name = "SGG PerfMeter Font Asset";
			_fontAsset.hideFlags = HideFlags.DontSave;
			return _fontAsset;
		}

		private static Font GetRuntimeFont()
		{
			return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
			AppendFpsSummary(metrics);
			_builder.Append(" | samples ");
			_builder.Append(metrics.FrameSampleCount.ToString(CultureInfo.InvariantCulture));
			_builder.Append('\n');
		}

		private void AppendFpsSummary(PerfMeterMetricsSnapshot metrics)
		{
			double currentFps = FpsFromFrameMs(metrics.CpuFrameTimeMs);
			double averageFps = metrics.AverageFps > 1d ? metrics.AverageFps : currentFps;
			double onePercentLowFps = metrics.OnePercentLowFps > 1d ? metrics.OnePercentLowFps : averageFps;
			double pointOnePercentLowFps = metrics.PointOnePercentLowFps > 1d ? metrics.PointOnePercentLowFps : onePercentLowFps;

			_builder.Append("FPS ");
			_builder.Append(FormatFpsValue(averageFps));
			_builder.Append(" | 1% ");
			_builder.Append(FormatFpsValue(onePercentLowFps));
			_builder.Append(" | 0.1% ");
			_builder.Append(FormatFpsValue(pointOnePercentLowFps));
		}

		private void AppendTimingLineWithRanges(PerfMeterMetricsSnapshot metrics)
		{
			_builder.Append("CPU/GPU ms: ");
			_builder.Append(FormatMsWithRange(metrics.CpuFrameTimeMs, _history.CpuFrameTimeMs));
			_builder.Append(" / ");
			_builder.Append(FormatMsWithRange(GetDisplayGpuFrameTime(metrics), _history.GpuFrameTimeMs, true, metrics.GpuFrameTimeAvailable));
			_builder.Append('\n');
			_builder.Append("main/render: ");
			_builder.Append(FormatMsWithRange(metrics.CpuMainThreadFrameTimeMs, _history.CpuMainThreadFrameTimeMs));
			_builder.Append(" / ");
			_builder.Append(FormatMsWithRange(metrics.CpuRenderThreadFrameTimeMs, _history.CpuRenderThreadFrameTimeMs));
			_builder.Append('\n');
		}

		private void AppendGpuValidity(PerfMeterMetricsSnapshot metrics)
		{
			_builder.Append("GPU valid: ");
			_builder.Append(metrics.GpuValidSampleCount.ToString(CultureInfo.InvariantCulture));
			_builder.Append("/");
			_builder.Append(metrics.FrameSampleCount.ToString(CultureInfo.InvariantCulture));
			_builder.Append('\n');
		}

		private void AppendMsWithRange(string name, double value, PerfMeterHistorySeries series, bool available = true, bool currentAvailable = true)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(FormatMsWithRange(value, series, available, currentAvailable));
			_builder.Append(" ms\n");
		}

		private void AppendIntWithRange(string name, int value, PerfMeterHistorySeries series, bool available = true)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(FormatIntWithRange(value, series, available));
			_builder.Append('\n');
		}

		private void AppendMemoryWithRange(string name, long bytes, PerfMeterHistorySeries series, bool available = true)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(FormatMemoryWithRange(bytes, series, available));
			_builder.Append(" MB\n");
		}

		private void AppendIntPairWithRanges(string name, int first, PerfMeterHistorySeries firstSeries, bool firstAvailable, int second, PerfMeterHistorySeries secondSeries, bool secondAvailable)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(FormatIntWithRange(first, firstSeries, firstAvailable));
			_builder.Append(" / ");
			_builder.Append(FormatIntWithRange(second, secondSeries, secondAvailable));
			_builder.Append('\n');
		}

		private void AppendMemoryPairWithRanges(string name, long firstBytes, PerfMeterHistorySeries firstSeries, bool firstAvailable, long secondBytes, PerfMeterHistorySeries secondSeries, bool secondAvailable)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(FormatMemoryWithRange(firstBytes, firstSeries, firstAvailable));
			_builder.Append(" / ");
			_builder.Append(FormatMemoryWithRange(secondBytes, secondSeries, secondAvailable));
			_builder.Append(" MB\n");
		}

		private void AppendOverdraw(PerfMeterMetricsSnapshot metrics)
		{
			_builder.Append("Overdraw: ");
			_builder.Append(metrics.OverdrawState.ToString());
			_builder.Append(" ");
			_builder.Append((metrics.OverdrawProgress * 100f).ToString("0", CultureInfo.InvariantCulture));
			_builder.Append("% ratio ");
			_builder.Append(metrics.OverdrawRatio > 0d ? metrics.OverdrawRatio.ToString("0.00", CultureInfo.InvariantCulture) : "unknown");
			_builder.Append('\n');
		}

		private void AppendWarning(string warning, int maxLength)
		{
			if (string.IsNullOrEmpty(warning))
			{
				return;
			}

			_builder.Append("Warn: ");
			if (warning.Length <= maxLength)
			{
				_builder.Append(warning);
			}
			else
			{
				_builder.Append(warning.Substring(0, maxLength - 3));
				_builder.Append("...");
			}
		}

		private void AppendLine(string name, string value)
		{
			_builder.Append(name);
			_builder.Append(": ");
			_builder.Append(value);
			_builder.Append('\n');
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

		private static double Max(params double[] values)
		{
			double result = 0d;
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] > result && !double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
				{
					result = values[i];
				}
			}

			return result;
		}

		private static double FpsFromFrameMs(double frameMs)
		{
			return frameMs > 0d ? 1000d / frameMs : 0d;
		}

		private static Color GetReadableTextColor(Color background)
		{
			float luminance = 0.2126f * background.r + 0.7152f * background.g + 0.0722f * background.b;
			return luminance > 0.58f ? new Color(0.04f, 0.055f, 0.065f, 1f) : new Color(0.92f, 0.96f, 1f, 1f);
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

		private sealed class PerfMeterGraphElement : VisualElement
		{
			private const int Capacity = 120;
			private static readonly Color BudgetColor = new Color(1f, 0.32f, 0.28f, 0.75f);
			private static readonly Color GridColor = new Color(0.32f, 0.42f, 0.5f, 0.32f);
			private static readonly Color GraphBackgroundColor = new Color(0.04f, 0.055f, 0.065f, 0.92f);
			private static readonly Color FrameFillColor = new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.34f);
			private static readonly Color MainFillColor = new Color(MainColor.r, MainColor.g, MainColor.b, 0.55f);
			private static readonly Color RenderFillColor = new Color(RenderColor.r, RenderColor.g, RenderColor.b, 0.55f);
			private static readonly Color OtherFillColor = new Color(OtherCpuColor.r, OtherCpuColor.g, OtherCpuColor.b, 0.45f);

			private readonly double[] _primary = new double[Capacity];
			private readonly double[] _secondary = new double[Capacity];
			private readonly double[] _tertiary = new double[Capacity];
			private readonly bool[] _valid = new bool[Capacity];
			private readonly double[] _scratch = new double[Capacity];
			private readonly PerfMeterGraphMode _mode;
			private readonly float _height;
			private readonly Label _maxScaleLabel;
			private readonly Label _budgetLabel;
			private int _index;
			private int _count;
			private double _frameBudgetMs = PerfMeterCollector.DefaultFrameBudgetMs;

			internal PerfMeterGraphElement(string name, PerfMeterGraphMode mode, float height, Label maxScaleLabel, Label budgetLabel)
			{
				this.name = name;
				_mode = mode;
				_height = height;
				_maxScaleLabel = maxScaleLabel;
				_budgetLabel = budgetLabel;
				pickingMode = PickingMode.Ignore;
				style.width = GraphPlotWidth;
				style.height = height;
				style.backgroundColor = GraphBackgroundColor;
				UpdateScaleLabels();
				generateVisualContent += OnGenerateVisualContent;
			}

			internal double ScaleMs { get; private set; } = PerfMeterCollector.DefaultFrameBudgetMs * 2d;

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
				_index = (_index + 1) % Capacity;

				if (_count < Capacity)
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
				return (_index - _count + sample + Capacity) % Capacity;
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
