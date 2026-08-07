using System;
using System.Collections.Generic;
using System.Globalization;
using SGG.PerfMeter;

namespace SGG.PerfMeter.Editor.UI
{
	internal enum PerfMeterSessionBudgetViolationKind
	{
		CpuMainThread = 0,
		CpuRenderThread = 1,
		Gpu = 2
	}

	internal readonly struct PerfMeterSessionTimelineRow
	{
		internal PerfMeterSessionTimelineRow(
			int frame,
			bool timeAvailable,
			double relativeTimeSeconds,
			string sceneName,
			bool cpuTimingAvailable,
			double cpuFrameTimeMs,
			double cpuMainThreadFrameTimeMs,
			double cpuRenderThreadFrameTimeMs,
			double cpuMainThreadPresentWaitTimeMs,
			bool gpuTimingAvailable,
			double gpuFrameTimeMs,
			bool budgetAvailable,
			double frameBudgetMs,
			PerfMeterBottleneck bottleneck,
			int frameSpikeCount,
			int severeFrameSpikeCount,
			bool sceneChanged,
			string graphicsStateTraceId)
		{
			Frame = frame;
			TimeAvailable = timeAvailable;
			RelativeTimeSeconds = relativeTimeSeconds;
			SceneName = sceneName ?? string.Empty;
			CpuTimingAvailable = cpuTimingAvailable;
			CpuFrameTimeMs = cpuFrameTimeMs;
			CpuMainThreadFrameTimeMs = cpuMainThreadFrameTimeMs;
			CpuRenderThreadFrameTimeMs = cpuRenderThreadFrameTimeMs;
			CpuMainThreadPresentWaitTimeMs = cpuMainThreadPresentWaitTimeMs;
			GpuTimingAvailable = gpuTimingAvailable;
			GpuFrameTimeMs = gpuFrameTimeMs;
			BudgetAvailable = budgetAvailable;
			FrameBudgetMs = frameBudgetMs;
			Bottleneck = bottleneck;
			FrameSpikeCount = Math.Max(0, frameSpikeCount);
			SevereFrameSpikeCount = Math.Max(0, severeFrameSpikeCount);
			SceneChanged = sceneChanged;
			GraphicsStateTraceId = graphicsStateTraceId ?? string.Empty;
		}

		internal int Frame { get; }
		internal bool TimeAvailable { get; }
		internal double RelativeTimeSeconds { get; }
		internal string SceneName { get; }
		internal bool CpuTimingAvailable { get; }
		internal double CpuFrameTimeMs { get; }
		internal double CpuMainThreadFrameTimeMs { get; }
		internal double CpuRenderThreadFrameTimeMs { get; }
		internal double CpuMainThreadPresentWaitTimeMs { get; }
		internal bool GpuTimingAvailable { get; }
		internal double GpuFrameTimeMs { get; }
		internal bool BudgetAvailable { get; }
		internal double FrameBudgetMs { get; }
		internal PerfMeterBottleneck Bottleneck { get; }
		internal int FrameSpikeCount { get; }
		internal int SevereFrameSpikeCount { get; }
		internal bool SceneChanged { get; }
		internal string GraphicsStateTraceId { get; }
	}

	internal readonly struct PerfMeterSessionBudgetViolationRow
	{
		internal PerfMeterSessionBudgetViolationRow(
			int frame,
			bool timeAvailable,
			double relativeTimeSeconds,
			string sceneName,
			PerfMeterSessionBudgetViolationKind kind,
			double valueMs,
			double budgetMs,
			PerfMeterBottleneck bottleneck)
		{
			Frame = frame;
			TimeAvailable = timeAvailable;
			RelativeTimeSeconds = relativeTimeSeconds;
			SceneName = sceneName ?? string.Empty;
			Kind = kind;
			ValueMs = valueMs;
			BudgetMs = budgetMs;
			OverageMs = valueMs - budgetMs;
			Bottleneck = bottleneck;
		}

		internal int Frame { get; }
		internal bool TimeAvailable { get; }
		internal double RelativeTimeSeconds { get; }
		internal string SceneName { get; }
		internal PerfMeterSessionBudgetViolationKind Kind { get; }
		internal double ValueMs { get; }
		internal double BudgetMs { get; }
		internal double OverageMs { get; }
		internal PerfMeterBottleneck Bottleneck { get; }
	}

	internal readonly struct PerfMeterSessionScopeRow
	{
		internal PerfMeterSessionScopeRow(string label, PerfMeterSessionScopeSummarySnapshot snapshot)
		{
			Label = label ?? string.Empty;
			Snapshot = snapshot;
		}

		internal string Label { get; }
		internal PerfMeterSessionScopeSummarySnapshot Snapshot { get; }
		internal bool HasSamples => Snapshot.SampleCount > 0;
	}

	internal sealed class PerfMeterSessionWorstFrameDetails
	{
		internal PerfMeterSessionWorstFrameDetails(PerfMeterSessionWorstFrameSnapshot snapshot)
		{
			Snapshot = snapshot;
			SampleMatched = false;
			Sample = default;
			Metrics = default;
			CustomMetrics = Array.Empty<PerfMeterCustomMetricSnapshot>();
			PlatformTelemetry = default;
			GraphicsStateTraceId = string.Empty;
			CpuTimingAvailable = false;
			GpuTimingAvailable = false;
			BudgetAvailable = false;
			FrameStatsAvailable = false;
		}

		internal PerfMeterSessionWorstFrameSnapshot Snapshot { get; }
		internal bool IsAvailable => Snapshot.IsAvailable;
		internal bool SampleMatched { get; private set; }
		internal PerfMeterSessionSampleSnapshot Sample { get; private set; }
		internal PerfMeterMetricsSnapshot Metrics { get; private set; }
		internal PerfMeterCustomMetricSnapshot[] CustomMetrics { get; private set; }
		internal PerfMeterPlatformTelemetrySnapshot PlatformTelemetry { get; private set; }
		internal string GraphicsStateTraceId { get; private set; }
		internal bool CpuTimingAvailable { get; private set; }
		internal bool GpuTimingAvailable { get; private set; }
		internal bool BudgetAvailable { get; private set; }
		internal bool FrameStatsAvailable { get; private set; }

		internal void SetMatchedSample(PerfMeterSessionSampleSnapshot sample)
		{
			SampleMatched = true;
			Sample = sample;
			Metrics = sample.Metrics;
			CustomMetrics = sample.CustomMetrics ?? Array.Empty<PerfMeterCustomMetricSnapshot>();
			PlatformTelemetry = sample.PlatformTelemetry;
			GraphicsStateTraceId = sample.GraphicsStateTraceId ?? string.Empty;
			CpuTimingAvailable = PerfMeterSessionAnalysisModel.IsCpuTimingAvailable(Metrics);
			GpuTimingAvailable = PerfMeterSessionAnalysisModel.IsGpuTimingAvailable(Metrics);
			BudgetAvailable = PerfMeterSessionAnalysisModel.IsBudgetAvailable(Metrics.FrameBudgetMs);
			FrameStatsAvailable = CpuTimingAvailable && Metrics.FrameSampleCount > 0;
		}
	}

	internal sealed class PerfMeterSessionAnalysisModel
	{
		internal const string UnavailableText = "Unavailable";
		internal const string NoSamplesText = "No samples";
		internal const string NoSessionText = "No session";
		internal const string UnknownSceneText = "Unknown scene";

		private readonly List<PerfMeterSessionTimelineRow> _timelineRows = new List<PerfMeterSessionTimelineRow>();
		private readonly List<PerfMeterSessionBudgetViolationRow> _budgetViolationRows = new List<PerfMeterSessionBudgetViolationRow>();
		private readonly List<PerfMeterSessionScopeRow> _scopeRows = new List<PerfMeterSessionScopeRow>();

		internal PerfMeterSessionSummarySnapshot Summary { get; private set; } = PerfMeterSessionSummarySnapshot.Empty;
		internal PerfMeterSessionWorstFrameDetails WorstFrame { get; private set; } = new PerfMeterSessionWorstFrameDetails(PerfMeterSessionWorstFrameSnapshot.Empty);
		internal List<PerfMeterSessionTimelineRow> TimelineRows => _timelineRows;
		internal List<PerfMeterSessionBudgetViolationRow> BudgetViolationRows => _budgetViolationRows;
		internal List<PerfMeterSessionScopeRow> ScopeRows => _scopeRows;
		internal bool HasSession => Summary.State != PerfMeterSessionState.Idle && !string.IsNullOrEmpty(Summary.SessionId);
		internal bool HasSamples => Summary.SampleCount > 0;

		internal void RefreshSummary(PerfMeterSessionSummarySnapshot summary)
		{
			Summary = summary;
			_scopeRows.Clear();
			_scopeRows.Add(new PerfMeterSessionScopeRow("Whole run", summary.WholeRun));
			_scopeRows.Add(new PerfMeterSessionScopeRow("Current scene", summary.CurrentScene));
		}

		internal void Rebuild(PerfMeterSessionSummarySnapshot summary, PerfMeterSessionSampleSnapshot[] samples)
		{
			RefreshSummary(summary);
			_timelineRows.Clear();
			_budgetViolationRows.Clear();
			WorstFrame = BuildWorstFrame(summary.WorstFrame, samples);

			PerfMeterSessionSampleSnapshot[] source = samples ?? Array.Empty<PerfMeterSessionSampleSnapshot>();
			bool hasPreviousScene = false;
			string previousScene = string.Empty;
			for (int index = 0; index < source.Length; index++)
			{
				PerfMeterSessionSampleSnapshot sample = source[index];
				PerfMeterMetricsSnapshot metrics = sample.Metrics;
				bool timeAvailable = TryGetRelativeTime(summary.StartTimeSeconds, sample.CollectionTimeSeconds, out double relativeTimeSeconds);
				bool cpuTimingAvailable = IsCpuTimingAvailable(metrics);
				bool gpuTimingAvailable = IsGpuTimingAvailable(metrics);
				bool budgetAvailable = IsBudgetAvailable(metrics.FrameBudgetMs);
				bool sceneChanged = hasPreviousScene && !string.Equals(previousScene, sample.SceneName, StringComparison.Ordinal);

				_timelineRows.Add(new PerfMeterSessionTimelineRow(
					sample.CollectionFrame,
					timeAvailable,
					relativeTimeSeconds,
					sample.SceneName,
					cpuTimingAvailable,
					metrics.CpuFrameTimeMs,
					metrics.CpuMainThreadFrameTimeMs,
					metrics.CpuRenderThreadFrameTimeMs,
					metrics.CpuMainThreadPresentWaitTimeMs,
					gpuTimingAvailable,
					metrics.GpuFrameTimeMs,
					budgetAvailable,
					metrics.FrameBudgetMs,
					metrics.Bottleneck,
					metrics.FrameSpikeCount,
					metrics.SevereFrameSpikeCount,
					sceneChanged,
					sample.GraphicsStateTraceId));

				BuildBudgetViolations(sample, relativeTimeSeconds, timeAvailable, cpuTimingAvailable, budgetAvailable, gpuTimingAvailable);
				hasPreviousScene = true;
				previousScene = sample.SceneName;
			}

		}

		internal static bool IsCpuTimingAvailable(PerfMeterMetricsSnapshot metrics)
		{
			return metrics.Availability == PerfMeterAvailability.Available &&
				IsFinitePositive(metrics.CpuFrameTimeMs) &&
				IsFiniteNonNegative(metrics.CpuMainThreadFrameTimeMs) &&
				IsFiniteNonNegative(metrics.CpuRenderThreadFrameTimeMs) &&
				IsFiniteNonNegative(metrics.CpuMainThreadPresentWaitTimeMs);
		}

		internal static bool IsGpuTimingAvailable(PerfMeterMetricsSnapshot metrics)
		{
			return metrics.GpuFrameTimeAvailable && IsFinitePositive(metrics.GpuFrameTimeMs);
		}

		internal static bool IsBudgetAvailable(double frameBudgetMs)
		{
			return IsFinitePositive(frameBudgetMs);
		}

		internal static string FormatMilliseconds(bool available, double value)
		{
			return available && IsFinite(value) ? value.ToString("0.00", CultureInfo.InvariantCulture) + " ms" : UnavailableText;
		}

		internal static string FormatPositiveMilliseconds(double value)
		{
			return FormatMilliseconds(IsFinitePositive(value), value);
		}

		internal static string FormatSeconds(bool available, double value)
		{
			return available && IsFinite(value) ? value.ToString("0.000", CultureInfo.InvariantCulture) + " s" : UnavailableText;
		}

		internal static string FormatFps(bool available, double value)
		{
			return available && IsFinite(value) ? value.ToString("0.0", CultureInfo.InvariantCulture) + " FPS" : UnavailableText;
		}

		internal static string FormatPositiveFps(double value)
		{
			return FormatFps(IsFinitePositive(value), value);
		}

		internal static string FormatInteger(bool available, int value)
		{
			return available ? value.ToString(CultureInfo.InvariantCulture) : UnavailableText;
		}

		internal static string FormatLong(bool available, long value)
		{
			return available ? value.ToString(CultureInfo.InvariantCulture) : UnavailableText;
		}

		internal static string FormatFrame(bool available, int value)
		{
			return available && value >= 0 ? value.ToString(CultureInfo.InvariantCulture) : UnavailableText;
		}

		internal static string FormatText(string value, string fallback = UnavailableText)
		{
			return string.IsNullOrEmpty(value) ? fallback : value;
		}

		internal static string FormatScene(string sceneName)
		{
			return FormatText(sceneName, UnknownSceneText);
		}

		internal static string FormatCustomMetric(PerfMeterCustomMetricSnapshot metric)
		{
			if (!metric.Available || !IsFinite(metric.Value))
			{
				return UnavailableText;
			}

			string value = metric.Value.ToString("0.###", CultureInfo.InvariantCulture);
			return string.IsNullOrEmpty(metric.Unit) ? value : value + " " + metric.Unit;
		}

		internal static string FormatTemperature(bool available, float value)
		{
			return available && !float.IsNaN(value) && !float.IsInfinity(value)
				? value.ToString("0.###", CultureInfo.InvariantCulture)
				: UnavailableText;
		}

		private void BuildBudgetViolations(
			PerfMeterSessionSampleSnapshot sample,
			double relativeTimeSeconds,
			bool timeAvailable,
			bool cpuTimingAvailable,
			bool budgetAvailable,
			bool gpuTimingAvailable)
		{
			PerfMeterMetricsSnapshot metrics = sample.Metrics;
			if (!budgetAvailable)
			{
				return;
			}

			if (cpuTimingAvailable)
			{
				double cpuMainThreadWorkMs = Math.Max(0d, metrics.CpuMainThreadFrameTimeMs - metrics.CpuMainThreadPresentWaitTimeMs);
				if (IsFinite(cpuMainThreadWorkMs) && cpuMainThreadWorkMs > metrics.FrameBudgetMs)
				{
					_budgetViolationRows.Add(new PerfMeterSessionBudgetViolationRow(
						sample.CollectionFrame,
						timeAvailable,
						relativeTimeSeconds,
						sample.SceneName,
						PerfMeterSessionBudgetViolationKind.CpuMainThread,
						cpuMainThreadWorkMs,
						metrics.FrameBudgetMs,
						metrics.Bottleneck));
				}

				if (metrics.CpuRenderThreadFrameTimeMs > metrics.FrameBudgetMs)
				{
					_budgetViolationRows.Add(new PerfMeterSessionBudgetViolationRow(
						sample.CollectionFrame,
						timeAvailable,
						relativeTimeSeconds,
						sample.SceneName,
						PerfMeterSessionBudgetViolationKind.CpuRenderThread,
						metrics.CpuRenderThreadFrameTimeMs,
						metrics.FrameBudgetMs,
						metrics.Bottleneck));
				}
			}

			if (gpuTimingAvailable && metrics.GpuFrameTimeMs > metrics.FrameBudgetMs)
			{
				_budgetViolationRows.Add(new PerfMeterSessionBudgetViolationRow(
					sample.CollectionFrame,
					timeAvailable,
					relativeTimeSeconds,
					sample.SceneName,
					PerfMeterSessionBudgetViolationKind.Gpu,
					metrics.GpuFrameTimeMs,
					metrics.FrameBudgetMs,
					metrics.Bottleneck));
			}
		}

		private static PerfMeterSessionWorstFrameDetails BuildWorstFrame(
			PerfMeterSessionWorstFrameSnapshot worstFrame,
			PerfMeterSessionSampleSnapshot[] samples)
		{
			PerfMeterSessionWorstFrameDetails details = new PerfMeterSessionWorstFrameDetails(worstFrame);
			if (!worstFrame.IsAvailable || samples == null || samples.Length == 0)
			{
				return details;
			}

			int bestIndex = -1;
			double bestDistance = double.MaxValue;
			for (int index = 0; index < samples.Length; index++)
			{
				if (samples[index].CollectionFrame != worstFrame.CollectionFrame)
				{
					continue;
				}

				double distance = IsFinite(samples[index].CollectionTimeSeconds) && IsFinite(worstFrame.CollectionTimeSeconds)
					? Math.Abs(samples[index].CollectionTimeSeconds - worstFrame.CollectionTimeSeconds)
					: double.MaxValue;
				if (bestIndex < 0 || distance < bestDistance)
				{
					bestIndex = index;
					bestDistance = distance;
				}
			}

			if (bestIndex >= 0)
			{
				details.SetMatchedSample(samples[bestIndex]);
			}

			return details;
		}

		private static bool TryGetRelativeTime(double startTimeSeconds, double sampleTimeSeconds, out double relativeTimeSeconds)
		{
			if (!IsFinite(startTimeSeconds) || !IsFinite(sampleTimeSeconds))
			{
				relativeTimeSeconds = 0d;
				return false;
			}

			relativeTimeSeconds = Math.Max(0d, sampleTimeSeconds - startTimeSeconds);
			return true;
		}

		private static bool IsFinitePositive(double value)
		{
			return IsFinite(value) && value > 0d;
		}

		private static bool IsFiniteNonNegative(double value)
		{
			return IsFinite(value) && value >= 0d;
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}
	}
}
