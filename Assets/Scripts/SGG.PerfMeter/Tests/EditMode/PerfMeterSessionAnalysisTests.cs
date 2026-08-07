using System;
using System.Collections.Generic;
using NUnit.Framework;
using SGG.PerfMeter.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSessionAnalysisTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
		}

		[Test]
		public void IdleAndUnavailableValuesDoNotFormatAsNumericZero()
		{
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			model.Rebuild(PerfMeterSessionSummarySnapshot.Empty, Array.Empty<PerfMeterSessionSampleSnapshot>());

			Assert.That(model.HasSession, Is.False);
			Assert.That(model.HasSamples, Is.False);
			Assert.That(model.ScopeRows, Has.Count.EqualTo(2));
			Assert.That(model.ScopeRows[0].HasSamples, Is.False);
			Assert.That(PerfMeterSessionAnalysisModel.FormatMilliseconds(false, 0d), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatSeconds(false, 0d), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatFps(false, 0d), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatPositiveMilliseconds(0d), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatPositiveFps(0d), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatInteger(false, 0), Is.EqualTo("Unavailable"));
			Assert.That(PerfMeterSessionAnalysisModel.FormatCustomMetric(new PerfMeterCustomMetricSnapshot("unavailable", "Unavailable", "test", "ms", 0d, false)), Is.EqualTo("Unavailable"));
		}

		[Test]
		public void TimelineClampsRelativeTimeAndMarksSceneBoundary()
		{
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			PerfMeterSessionSummarySnapshot summary = CreateSummary(
				PerfMeterSessionState.Recording,
				"session-timeline",
				2,
				10d,
				CreateScope("Scene B", 2, 11, 12, PerfMeterSessionWorstFrameSnapshot.Empty),
				CreateScope("Scene B", 1, 12, 12, PerfMeterSessionWorstFrameSnapshot.Empty));
			PerfMeterSessionSampleSnapshot[] samples =
			{
				CreateSample(11, 9d, "Scene A", CreateMetrics(11, 16d, 16d, 16d, 0d, false, 0d, 0, 0)),
				CreateSample(12, 10.5d, "Scene B", CreateMetrics(12, 16d, 16d, 16d, 0d, false, 0d, 1, 0), traceId: "trace-12")
			};

			model.Rebuild(summary, samples);

			Assert.That(model.TimelineRows, Has.Count.EqualTo(2));
			Assert.That(model.TimelineRows[0].TimeAvailable, Is.True);
			Assert.That(model.TimelineRows[0].RelativeTimeSeconds, Is.EqualTo(0d));
			Assert.That(model.TimelineRows[1].RelativeTimeSeconds, Is.EqualTo(0.5d).Within(0.0001d));
			Assert.That(model.TimelineRows[1].SceneChanged, Is.True);
			Assert.That(model.TimelineRows[1].GraphicsStateTraceId, Is.EqualTo("trace-12"));
			Assert.That(model.TimelineRows[1].GpuTimingAvailable, Is.False);
			Assert.That(PerfMeterSessionAnalysisModel.FormatMilliseconds(model.TimelineRows[1].GpuTimingAvailable, 0d), Is.EqualTo("Unavailable"));
		}

		[Test]
		public void BudgetViolationsAdjustMainThreadForPresentWaitAndUseStrictEquality()
		{
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			PerfMeterSessionSummarySnapshot summary = CreateSummary(
				PerfMeterSessionState.Stopped,
				"session-budget",
				4,
				10d,
				CreateScope("Scene", 4, 1, 4, PerfMeterSessionWorstFrameSnapshot.Empty),
				CreateScope("Scene", 4, 1, 4, PerfMeterSessionWorstFrameSnapshot.Empty));
			PerfMeterSessionSampleSnapshot[] samples =
			{
				CreateSample(1, 10d, "Scene", CreateMetrics(1, 20d, 20d, 16d, 4d, true, 16d, 1, 0)),
				CreateSample(2, 10.1d, "Scene", CreateMetrics(2, 20d, 22d, 17d, 5d, true, 17d, 1, 0)),
				CreateSample(3, 10.2d, "Scene", CreateMetrics(3, 20d, 16d, 16d, 0d, false, 100d, 1, 0))
			};

			model.Rebuild(summary, samples);

			Assert.That(model.BudgetViolationRows, Has.Count.EqualTo(3));
			Assert.That(CountViolations(model.BudgetViolationRows, PerfMeterSessionBudgetViolationKind.CpuMainThread), Is.EqualTo(1));
			Assert.That(CountViolations(model.BudgetViolationRows, PerfMeterSessionBudgetViolationKind.CpuRenderThread), Is.EqualTo(1));
			Assert.That(CountViolations(model.BudgetViolationRows, PerfMeterSessionBudgetViolationKind.Gpu), Is.EqualTo(1));
			Assert.That(model.BudgetViolationRows[0].Frame, Is.EqualTo(2));
			Assert.That(model.BudgetViolationRows[0].ValueMs, Is.EqualTo(17d).Within(0.0001d));
			Assert.That(model.BudgetViolationRows[0].OverageMs, Is.EqualTo(1d).Within(0.0001d));
			Assert.That(model.BudgetViolationRows[1].Frame, Is.EqualTo(2));
			Assert.That(model.BudgetViolationRows[2].Frame, Is.EqualTo(2));
		}

		[Test]
		public void UnavailableCpuTimingStillReportsIndependentGpuViolation()
		{
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			PerfMeterSessionSummarySnapshot summary = CreateSummary(
				PerfMeterSessionState.Stopped,
				"session-gpu-only",
				1,
				10d,
				CreateScope("Scene", 1, 4, 4, PerfMeterSessionWorstFrameSnapshot.Empty),
				CreateScope("Scene", 1, 4, 4, PerfMeterSessionWorstFrameSnapshot.Empty));
			PerfMeterSessionSampleSnapshot sample = CreateSample(
				4,
				10.1d,
				"Scene",
				CreateMetrics(4, 0d, 0d, 0d, 0d, true, 20d, 0, 0, PerfMeterAvailability.Unavailable));

			model.Rebuild(summary, new[] { sample });

			Assert.That(model.BudgetViolationRows, Has.Count.EqualTo(1));
			Assert.That(model.BudgetViolationRows[0].Kind, Is.EqualTo(PerfMeterSessionBudgetViolationKind.Gpu));
			Assert.That(model.BudgetViolationRows[0].ValueMs, Is.EqualTo(20d).Within(0.0001d));
		}

		[Test]
		public void WorstFrameMatchesNearestRetainedSampleAndReportsMissingSample()
		{
			PerfMeterSessionWorstFrameSnapshot worst = new PerfMeterSessionWorstFrameSnapshot(20, 20.04d, "Scene", 25d, 40d, PerfMeterBottleneck.CpuMainThreadBound);
			PerfMeterCustomMetricSnapshot[] customMetrics =
			{
				new PerfMeterCustomMetricSnapshot("missing", "Missing", "test", "count", 0d, false)
			};
			PerfMeterSessionSampleSnapshot[] samples =
			{
				CreateSample(20, 20.10d, "Scene", CreateMetrics(20, 25d, 25d, 10d, 0d, true, 11d, 2, 1)),
				CreateSample(20, 20.00d, "Scene", CreateMetrics(20, 25d, 25d, 10d, 0d, true, 11d, 2, 1), customMetrics)
			};
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			model.Rebuild(CreateSummaryWithWorst(worst, 2), samples);

			Assert.That(model.WorstFrame.IsAvailable, Is.True);
			Assert.That(model.WorstFrame.SampleMatched, Is.True);
			Assert.That(model.WorstFrame.Sample.CollectionTimeSeconds, Is.EqualTo(20.00d).Within(0.0001d));
			Assert.That(model.WorstFrame.CpuTimingAvailable, Is.True);
			Assert.That(model.WorstFrame.GpuTimingAvailable, Is.True);
			Assert.That(model.WorstFrame.CustomMetrics[0].Available, Is.False);
			Assert.That(PerfMeterSessionAnalysisModel.FormatCustomMetric(model.WorstFrame.CustomMetrics[0]), Is.EqualTo("Unavailable"));

			model.Rebuild(CreateSummaryWithWorst(new PerfMeterSessionWorstFrameSnapshot(99, 30d, "Scene", 25d, 40d, PerfMeterBottleneck.Unknown), 2), samples);
			Assert.That(model.WorstFrame.IsAvailable, Is.True);
			Assert.That(model.WorstFrame.SampleMatched, Is.False);
		}

		[Test]
		public void SceneScopesExposeOnlyAuthoritativeSummaryRows()
		{
			PerfMeterSessionScopeSummarySnapshot wholeRun = CreateScope("Whole", 3, 10, 12, PerfMeterSessionWorstFrameSnapshot.Empty);
			PerfMeterSessionScopeSummarySnapshot currentScene = CreateScope("Current", 1, 12, 12, PerfMeterSessionWorstFrameSnapshot.Empty);
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();

			model.Rebuild(CreateSummary(PerfMeterSessionState.Stopped, "session-scopes", 3, 10d, wholeRun, currentScene), new[]
			{
				CreateSample(10, 10d, "Historical", CreateMetrics(10, 16d, 16d, 16d, 0d, false, 0d, 1, 0))
			});

			Assert.That(model.ScopeRows, Has.Count.EqualTo(2));
			Assert.That(model.ScopeRows[0].Label, Is.EqualTo("Whole run"));
			Assert.That(model.ScopeRows[0].Snapshot.SceneName, Is.EqualTo("Whole"));
			Assert.That(model.ScopeRows[0].Snapshot, Is.EqualTo(wholeRun));
			Assert.That(model.ScopeRows[1].Label, Is.EqualTo("Current scene"));
			Assert.That(model.ScopeRows[1].Snapshot, Is.EqualTo(currentScene));
		}

		[Test]
		public void SummaryRefreshPreservesSampleRowsAndUpdatesAuthoritativeScopes()
		{
			PerfMeterSessionAnalysisModel model = new PerfMeterSessionAnalysisModel();
			PerfMeterSessionScopeSummarySnapshot firstScope = CreateScope("First", 1, 10, 10, PerfMeterSessionWorstFrameSnapshot.Empty);
			model.Rebuild(
				CreateSummary(PerfMeterSessionState.Recording, "session-refresh", 1, 10d, firstScope, firstScope),
				new[] { CreateSample(10, 10d, "First", CreateMetrics(10, 16d, 16d, 16d, 0d, false, 0d, 0, 0)) });

			PerfMeterSessionScopeSummarySnapshot refreshedScope = CreateScope("Refreshed", 1, 10, 10, PerfMeterSessionWorstFrameSnapshot.Empty);
			model.RefreshSummary(CreateSummary(PerfMeterSessionState.Recording, "session-refresh", 1, 10d, refreshedScope, refreshedScope));

			Assert.That(model.TimelineRows, Has.Count.EqualTo(1));
			Assert.That(model.ScopeRows, Has.Count.EqualTo(2));
			Assert.That(model.ScopeRows[0].Snapshot.SceneName, Is.EqualTo("Refreshed"));
			Assert.That(model.ScopeRows[1].Snapshot.SceneName, Is.EqualTo("Refreshed"));
		}

		[Test]
		public void SampleCacheIdentityDetectsRecordingWindowResetAtSameCount()
		{
			PerfMeterSessionScopeSummarySnapshot firstScope = CreateScope("Scene", 1, 10, 10, PerfMeterSessionWorstFrameSnapshot.Empty);
			PerfMeterSessionSummarySnapshot first = CreateSummary(PerfMeterSessionState.Recording, "session-cache", 1, 10d, firstScope, firstScope);
			Assert.That(PerfMeterSessionAnalysisWindow.RequiresSampleRefresh(
				true,
				false,
				first.SessionId,
				first.State,
				first.SampleCount,
				first.StartTimeSeconds,
				first.FirstFrame,
				first), Is.False);

			PerfMeterSessionScopeSummarySnapshot resetScope = CreateScope("Scene", 1, 20, 20, PerfMeterSessionWorstFrameSnapshot.Empty);
			PerfMeterSessionSummarySnapshot reset = CreateSummary(PerfMeterSessionState.Recording, "session-cache", 1, 20d, resetScope, resetScope);
			Assert.That(PerfMeterSessionAnalysisWindow.RequiresSampleRefresh(
				true,
				false,
				first.SessionId,
				first.State,
				first.SampleCount,
				first.StartTimeSeconds,
				first.FirstFrame,
				reset), Is.True);
		}

		[Test]
		public void SessionPublicReadsDoNotStartRuntime()
		{
			Assert.That(PerfMeterRuntime.Instance, Is.Null);

			PerfMeterSessionSummarySnapshot summary = PerformanceMeter.GetSessionSummary();
			PerfMeterSessionSampleSnapshot[] samples = PerformanceMeter.GetSessionSamples();

			Assert.That(summary.State, Is.EqualTo(PerfMeterSessionState.Idle));
			Assert.That(samples, Is.Empty);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void WindowCreateGuiDoesNotStartRuntime()
		{
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			PerfMeterSessionAnalysisWindow window = ScriptableObject.CreateInstance<PerfMeterSessionAnalysisWindow>();

			try
			{
				Assert.DoesNotThrow(window.CreateGUI);
				Assert.That(PerfMeterRuntime.Instance, Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(window);
			}
		}

		[Test]
		public void CopyValueHelperWritesClipboardValue()
		{
			string previousValue = EditorGUIUtility.systemCopyBuffer;
			try
			{
				PerfMeterSessionAnalysisWindow.CopyValueToClipboard("session-copy");
				Assert.That(EditorGUIUtility.systemCopyBuffer, Is.EqualTo("session-copy"));
			}
			finally
			{
				PerfMeterSessionAnalysisWindow.CopyValueToClipboard(previousValue);
			}
		}

		private static int CountViolations(List<PerfMeterSessionBudgetViolationRow> rows, PerfMeterSessionBudgetViolationKind kind)
		{
			int count = 0;
			for (int index = 0; index < rows.Count; index++)
			{
				if (rows[index].Kind == kind)
				{
					count++;
				}
			}

			return count;
		}

		private static PerfMeterSessionSampleSnapshot CreateSample(
			int frame,
			double timeSeconds,
			string sceneName,
			PerfMeterMetricsSnapshot metrics,
			PerfMeterCustomMetricSnapshot[] customMetrics = null,
			string traceId = "")
		{
			return new PerfMeterSessionSampleSnapshot(
				frame,
				timeSeconds,
				sceneName,
				metrics,
				customMetrics ?? Array.Empty<PerfMeterCustomMetricSnapshot>(),
				PerfMeterPlatformTelemetrySnapshot.Unavailable(),
				traceId);
		}

		private static PerfMeterMetricsSnapshot CreateMetrics(
			int frame,
			double cpuFrameTimeMs,
			double cpuMainThreadFrameTimeMs,
			double cpuRenderThreadFrameTimeMs,
			double presentWaitTimeMs,
			bool gpuAvailable,
			double gpuFrameTimeMs,
			int frameSpikeCount,
			int severeFrameSpikeCount,
			PerfMeterAvailability availability = PerfMeterAvailability.Available)
		{
			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				availability,
				frame,
				PerfMeterBottleneck.Balanced,
				16d,
				gpuAvailable,
				cpuFrameTimeMs,
				cpuMainThreadFrameTimeMs,
				cpuRenderThreadFrameTimeMs,
				presentWaitTimeMs,
				gpuFrameTimeMs,
				0,
				0,
				0,
				0,
				0,
				0,
				0L,
				0L,
				0L,
				0L,
				0d,
				frameSampleCount: 2,
				gpuValidSampleCount: gpuAvailable ? 1 : 0,
				averageFps: 60d,
				onePercentLowFps: 50d,
				pointOnePercentLowFps: 45d,
				frameSpikeCount: frameSpikeCount,
				severeFrameSpikeCount: severeFrameSpikeCount);
		}

		private static PerfMeterSessionSummarySnapshot CreateSummary(
			PerfMeterSessionState state,
			string sessionId,
			int sampleCount,
			double startTimeSeconds,
			PerfMeterSessionScopeSummarySnapshot wholeRun,
			PerfMeterSessionScopeSummarySnapshot currentScene)
		{
			return new PerfMeterSessionSummarySnapshot(
				state,
				PerfMeterSessionOptions.Default,
				sampleCount,
				0,
				sampleCount > 0 ? wholeRun.FirstFrame : -1,
				sampleCount > 0 ? wholeRun.LastFrame : -1,
				startTimeSeconds,
				state == PerfMeterSessionState.Stopped ? startTimeSeconds + 1d : 0d,
				sampleCount > 0 ? 1d : 0d,
				sampleCount > 0 ? 16d : 0d,
				sampleCount > 0 ? 16d : 0d,
				sampleCount > 0 ? 16d : 0d,
				sampleCount > 0 ? 60d : 0d,
				sampleCount > 0 ? 60d : 0d,
				sampleCount > 0 ? 60d : 0d,
				0,
				0,
				0,
				0,
				0,
				0,
				string.Empty,
				default,
				default,
				default,
				default,
				"Start",
				"Current",
				wholeRun,
				currentScene,
				0,
				0,
				0d,
				sessionId);
		}

		private static PerfMeterSessionSummarySnapshot CreateSummaryWithWorst(PerfMeterSessionWorstFrameSnapshot worstFrame, int sampleCount)
		{
			PerfMeterSessionScopeSummarySnapshot scope = CreateScope("Scene", sampleCount, 20, 20, worstFrame);
			return CreateSummary(PerfMeterSessionState.Stopped, "session-worst", sampleCount, 20d, scope, scope);
		}

		private static PerfMeterSessionScopeSummarySnapshot CreateScope(
			string sceneName,
			int sampleCount,
			int firstFrame,
			int lastFrame,
			PerfMeterSessionWorstFrameSnapshot worstFrame)
		{
			return new PerfMeterSessionScopeSummarySnapshot(
				sceneName,
				sampleCount,
				firstFrame,
				lastFrame,
				10d,
				11d,
				sampleCount > 0 ? 1d : 0d,
				sampleCount > 0 ? 16d : 0d,
				sampleCount > 0 ? 12d : 0d,
				sampleCount > 0 ? 20d : 0d,
				sampleCount > 0 ? 60d : 0d,
				sampleCount > 0 ? 50d : 0d,
				sampleCount > 0 ? 60d : 0d,
				1,
				1,
				1,
				0,
				0,
				0,
				worstFrame);
		}
	}
}
