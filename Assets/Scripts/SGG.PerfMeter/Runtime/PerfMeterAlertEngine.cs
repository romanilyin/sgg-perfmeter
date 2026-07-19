using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterAlertEngine
	{
		private readonly List<PerfMeterAlertSnapshot> _latestAlerts = new List<PerfMeterAlertSnapshot>(16);
		private readonly RuleState[] _states;
		private readonly PerfMeterRule[] _rules;
		private PerfMeterAlertSnapshot _latestAlert;
		private float _editorWarningCooldownSeconds = 8f;
		private float _structuredLogCooldownSeconds = 2f;
		private float _callbackCooldownSeconds = 0.5f;
		private bool _editorWarningsEnabled = true;
		private int _firedAlertCount;
		private int _steadyStateFiredCount;
		private int _lifecycleFiredCount;
		private int _captureFiredCount;
		private string _historyIntervalId;
		private int _historyStartCollectionFrame;
		private double _historyStartTimeSeconds;
		private string _historyStartedUtc;
		private PerfMeterAlertHistoryResetReason _historyResetReason;

		internal PerfMeterAlertEngine()
			: this(CreateDefaultRules(PerfMeterTargetFps.Fps60, PerfMeterSettingsStore.Defaults))
		{
		}

		internal PerfMeterAlertEngine(PerfMeterRule[] rules)
		{
			_rules = rules ?? Array.Empty<PerfMeterRule>();
			_states = new RuleState[_rules.Length];
			ResetHistory(-1, 0d, PerfMeterAlertHistoryResetReason.RuntimeStarted);
		}

		internal int ActiveAlertCount => _latestAlerts.Count;
		internal int FiredAlertCount => _firedAlertCount;
		internal PerfMeterAlertSnapshot LatestAlert => _latestAlert;
		internal PerfMeterAlertHistorySnapshot History => new PerfMeterAlertHistorySnapshot(
			_historyIntervalId,
			_historyStartCollectionFrame,
			_historyStartTimeSeconds,
			_historyStartedUtc,
			_historyResetReason,
			_firedAlertCount,
			_steadyStateFiredCount,
			_lifecycleFiredCount,
			_captureFiredCount,
			_latestAlert);

		internal void ApplySettings(PerfMeterSettingsSnapshot settings, PerfMeterTargetFps targetFps)
		{
			_editorWarningCooldownSeconds = settings.EditorWarningCooldownSeconds;
			_structuredLogCooldownSeconds = settings.StructuredLogCooldownSeconds;
			_callbackCooldownSeconds = settings.CallbackCooldownSeconds;
			_editorWarningsEnabled = settings.EditorWarningsEnabled;
		}

		internal void Evaluate(PerfMeterMetricsSnapshot metrics, double timeSeconds)
		{
			Evaluate(metrics, timeSeconds, PerfMeterAlertClassification.SteadyState, string.Empty);
		}

		internal void Evaluate(PerfMeterMetricsSnapshot metrics, double timeSeconds, PerfMeterAlertClassification classification, string captureId)
		{
			_latestAlerts.Clear();
			for (int index = 0; index < _rules.Length; index++)
			{
				PerfMeterRule rule = _rules[index];
				RuleState state = _states[index];
				double value = GetMetricValue(rule.Metric, metrics);
				bool matched = IsMetricAvailable(rule.Metric, metrics) && Compare(value, rule.Comparison, rule.Threshold);
				state.ConsecutiveFrames = matched ? state.ConsecutiveFrames + 1 : 0;

				if (matched && state.ConsecutiveFrames >= rule.ConsecutiveFrames)
				{
					PerfMeterAlertSnapshot alert = CreateAlert(rule, value, metrics.CollectionFrame, timeSeconds, state.ConsecutiveFrames, true, classification, captureId);
					_latestAlerts.Add(alert);
					FireActions(rule, alert, timeSeconds, ref state);
				}

				_states[index] = state;
			}
		}

		internal PerfMeterAlertSnapshot[] GetLatestAlerts()
		{
			return _latestAlerts.ToArray();
		}

		internal void Clear()
		{
			ResetHistory(-1, 0d, PerfMeterAlertHistoryResetReason.ExplicitClear);
		}

		internal void ResetHistory(int collectionFrame, double timeSeconds, PerfMeterAlertHistoryResetReason resetReason)
		{
			_latestAlerts.Clear();
			_latestAlert = default;
			_firedAlertCount = 0;
			_steadyStateFiredCount = 0;
			_lifecycleFiredCount = 0;
			_captureFiredCount = 0;
			_historyIntervalId = Guid.NewGuid().ToString("N");
			_historyStartCollectionFrame = collectionFrame;
			_historyStartTimeSeconds = timeSeconds;
			_historyStartedUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
			_historyResetReason = resetReason;
			for (int index = 0; index < _states.Length; index++)
			{
				_states[index] = default;
			}
		}

		internal static PerfMeterRule[] CreateDefaultRules(PerfMeterTargetFps targetFps, PerfMeterSettingsSnapshot settings)
		{
			double budget = PerfMeterRuntime.GetFrameBudgetMs(targetFps);
			return new[]
			{
				new PerfMeterRule("cpu.frame.over_budget", PerfMeterMetric.CpuFrameTimeMs, PerfMeterComparison.GreaterThan, budget, settings.AlertTimingConsecutiveFrames),
				new PerfMeterRule("cpu.main.over_budget", PerfMeterMetric.CpuMainThreadFrameTimeMs, PerfMeterComparison.GreaterThan, budget, settings.AlertTimingConsecutiveFrames),
				new PerfMeterRule("gpu.frame.over_budget", PerfMeterMetric.GpuFrameTimeMs, PerfMeterComparison.GreaterThan, budget, settings.AlertTimingConsecutiveFrames),
				new PerfMeterRule("fps.below_target", PerfMeterMetric.AverageFps, PerfMeterComparison.LessThan, (int)targetFps, settings.AlertFpsConsecutiveFrames),
				new PerfMeterRule("gpu.timing.unavailable", PerfMeterMetric.GpuFrameTimeAvailable, PerfMeterComparison.LessThan, 0.5d, settings.AlertGpuTimingUnavailableConsecutiveFrames),
				new PerfMeterRule("overdraw.ratio.high", PerfMeterMetric.OverdrawRatio, PerfMeterComparison.GreaterThan, settings.AlertOverdrawRatioThreshold, settings.AlertOverdrawConsecutiveFrames)
			};
		}

		internal static bool Compare(double value, PerfMeterComparison comparison, double threshold)
		{
			switch (comparison)
			{
				case PerfMeterComparison.GreaterThan:
					return value > threshold;
				case PerfMeterComparison.GreaterThanOrEqual:
					return value >= threshold;
				case PerfMeterComparison.LessThan:
					return value < threshold;
				case PerfMeterComparison.LessThanOrEqual:
					return value <= threshold;
				case PerfMeterComparison.Equal:
					return Math.Abs(value - threshold) < 0.0001d;
				case PerfMeterComparison.NotEqual:
					return Math.Abs(value - threshold) >= 0.0001d;
				default:
					return false;
			}
		}

		private void FireActions(PerfMeterRule rule, PerfMeterAlertSnapshot alert, double timeSeconds, ref RuleState state)
		{
			float cooldown = rule.CooldownSeconds;
			bool actionFired = false;
			if ((rule.Actions & PerfMeterAlertAction.StructuredLog) != 0 && CanFire(timeSeconds, state.StructuredLogFired, state.LastStructuredLogTime, cooldown > 0f ? cooldown : _structuredLogCooldownSeconds))
			{
				Debug.Log("[SGG PerfMeter Alert] " + alert.Message);
				state.LastStructuredLogTime = timeSeconds;
				state.StructuredLogFired = true;
				actionFired = true;
			}

			if ((rule.Actions & PerfMeterAlertAction.Callback) != 0 && CanFire(timeSeconds, state.CallbackFired, state.LastCallbackTime, cooldown > 0f ? cooldown : _callbackCooldownSeconds))
			{
				PerformanceMeter.RaiseAlertFired(alert);
				state.LastCallbackTime = timeSeconds;
				state.CallbackFired = true;
				actionFired = true;
			}

#if UNITY_EDITOR
			if (_editorWarningsEnabled && (rule.Actions & PerfMeterAlertAction.EditorWarning) != 0 && CanFire(timeSeconds, state.EditorWarningFired, state.LastEditorWarningTime, cooldown > 0f ? cooldown : _editorWarningCooldownSeconds))
			{
				Debug.LogWarning("[SGG PerfMeter Alert] " + alert.Message);
				state.LastEditorWarningTime = timeSeconds;
				state.EditorWarningFired = true;
				actionFired = true;
			}
#endif

			if (actionFired)
			{
				_firedAlertCount++;
				switch (alert.Classification)
				{
					case PerfMeterAlertClassification.Capture:
						_captureFiredCount++;
						break;
					case PerfMeterAlertClassification.Lifecycle:
						_lifecycleFiredCount++;
						break;
					default:
						_steadyStateFiredCount++;
						break;
				}
				_latestAlert = alert;
			}
		}

		private static bool CanFire(double timeSeconds, bool hasFired, double lastTimeSeconds, float cooldownSeconds)
		{
			return !hasFired || timeSeconds - lastTimeSeconds >= cooldownSeconds;
		}

		private static PerfMeterAlertSnapshot CreateAlert(PerfMeterRule rule, double value, int frame, double timeSeconds, int consecutiveFrames, bool active, PerfMeterAlertClassification classification, string captureId)
		{
			string message = rule.Id + ": " + rule.Metric + " " + rule.Comparison + " " + rule.Threshold.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ", value " + value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
			return new PerfMeterAlertSnapshot(rule.Id, rule.Metric, rule.Comparison, rule.Threshold, value, frame, timeSeconds, consecutiveFrames, active, message, classification, captureId);
		}

		private static double GetMetricValue(PerfMeterMetric metric, PerfMeterMetricsSnapshot metrics)
		{
			switch (metric)
			{
				case PerfMeterMetric.CpuFrameTimeMs:
					return metrics.CpuFrameTimeMs;
				case PerfMeterMetric.CpuMainThreadFrameTimeMs:
					return metrics.CpuMainThreadFrameTimeMs;
				case PerfMeterMetric.CpuRenderThreadFrameTimeMs:
					return metrics.CpuRenderThreadFrameTimeMs;
				case PerfMeterMetric.GpuFrameTimeMs:
					return metrics.GpuFrameTimeMs;
				case PerfMeterMetric.GpuFrameTimeAvailable:
					return metrics.GpuFrameTimeAvailable ? 1d : 0d;
				case PerfMeterMetric.AverageFps:
					return metrics.AverageFps;
				case PerfMeterMetric.OnePercentLowFps:
					return metrics.OnePercentLowFps;
				case PerfMeterMetric.OverdrawRatio:
					return metrics.OverdrawRatio;
				case PerfMeterMetric.SystemUsedMemoryBytes:
					return metrics.SystemUsedMemoryBytes;
				case PerfMeterMetric.GcReservedMemoryBytes:
					return metrics.GcReservedMemoryBytes;
				case PerfMeterMetric.DrawCalls:
					return metrics.DrawCalls;
				case PerfMeterMetric.SetPassCalls:
					return metrics.SetPassCalls;
				default:
					return 0d;
			}
		}

		private static bool IsMetricAvailable(PerfMeterMetric metric, PerfMeterMetricsSnapshot metrics)
		{
			if (metric == PerfMeterMetric.GpuFrameTimeMs)
			{
				return metrics.GpuFrameTimeAvailable;
			}

			if (metric == PerfMeterMetric.AverageFps || metric == PerfMeterMetric.OnePercentLowFps)
			{
				return metrics.FrameSampleCount > 0;
			}

			if (metric == PerfMeterMetric.OverdrawRatio)
			{
				return metrics.OverdrawState == PerfMeterOverdrawMeasurementState.Completed && metrics.OverdrawRatio > 0d;
			}

			return true;
		}

		private struct RuleState
		{
			public int ConsecutiveFrames;
			public bool StructuredLogFired;
			public bool CallbackFired;
			public bool EditorWarningFired;
			public double LastStructuredLogTime;
			public double LastCallbackTime;
			public double LastEditorWarningTime;
		}
	}
}
