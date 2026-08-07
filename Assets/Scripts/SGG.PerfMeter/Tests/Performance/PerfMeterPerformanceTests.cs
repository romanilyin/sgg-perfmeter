using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using Unity.PerformanceTesting;
using UnityEngine;

namespace SGG.PerfMeter.Tests.Performance
{
	public sealed class PerfMeterPerformanceTests
	{
		private const string BaselineAssetGuid = "5f385ba6c6cb4fb0963163db9bc1311a";
		private const string InstrumentationBenchmarkId = "profiler_instrumentation_warmed";
		private const string SessionBoundaryBenchmarkId = "session_boundary_markers_warmed";

		[SetUp]
		public void SetUp()
		{
			PerfMeterProfilerInstrumentation.Reset();
		}

		[Test, Performance]
		public void ProfilerInstrumentationWarmedBaseline()
		{
			PerformanceBaseline baseline = LoadBaseline(InstrumentationBenchmarkId);
			Assert.That(baseline.iterations, Is.GreaterThan(0), "The performance baseline must define a positive iteration count.");
			Assert.That(baseline.max_average_cpu_ms_per_invocation, Is.GreaterThan(0d), "The performance baseline must define a positive CPU threshold.");
			Assert.That(baseline.max_allocated_bytes_per_invocation, Is.EqualTo(0L), "The warmed instrumentation allocation guard must remain exact zero.");

			RecordFixedProfilerInstrumentationCounters();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			long startTimestamp = Stopwatch.GetTimestamp();
			for (int iteration = 0; iteration < baseline.iterations; iteration++)
			{
				PerfMeterProfilerInstrumentation.RecordFrameTimings(true, 16d, 8d, 4d, 1d, true, 12d);
				PerfMeterProfilerInstrumentation.RecordBottleneck(PerfMeterBottleneck.Balanced);
				PerfMeterProfilerInstrumentation.RecordCustomMetricCount(1);
				PerfMeterProfilerInstrumentation.RecordSessionState(PerfMeterSessionState.Recording);
				PerfMeterProfilerInstrumentation.RecordAlertScopeActive(false);
				PerfMeterProfilerInstrumentation.RecordOverdrawState(PerfMeterOverdrawMeasurementState.Off);
				PerfMeterProfilerInstrumentation.RecordCaptureState(PerfMeterCaptureState.Idle);
				PerfMeterProfilerInstrumentation.RecordThermalAvailability(false);
			}

			long elapsedTimestamp = Stopwatch.GetTimestamp() - startTimestamp;
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
			double averageCpuMilliseconds = elapsedTimestamp * 1000d / Stopwatch.Frequency / baseline.iterations;
			double allocatedBytesPerInvocation = allocatedBytes / (double)baseline.iterations;

			Measure.Custom(InstrumentationBenchmarkId + ".cpu_ms_per_invocation", averageCpuMilliseconds);
			Measure.Custom(InstrumentationBenchmarkId + ".allocated_bytes_per_invocation", allocatedBytesPerInvocation);

			Assert.That(averageCpuMilliseconds, Is.LessThanOrEqualTo(baseline.max_average_cpu_ms_per_invocation),
				$"Warmed profiler instrumentation averaged {averageCpuMilliseconds:R} ms per invocation, above the {baseline.max_average_cpu_ms_per_invocation:R} ms baseline.");
			Assert.That(allocatedBytesPerInvocation, Is.LessThanOrEqualTo(baseline.max_allocated_bytes_per_invocation),
				$"Warmed profiler instrumentation allocated {allocatedBytesPerInvocation:R} bytes per invocation, above the exact-zero baseline.");
		}

		[Test, Performance]
		public void SessionBoundaryMarkersWarmedBaseline()
		{
			PerformanceBaseline baseline = LoadBaseline(SessionBoundaryBenchmarkId);
			Assert.That(baseline.iterations, Is.GreaterThan(0), "The performance baseline must define a positive iteration count.");
			Assert.That(baseline.max_average_cpu_ms_per_invocation, Is.GreaterThan(0d), "The performance baseline must define a positive CPU threshold.");
			Assert.That(baseline.max_allocated_bytes_per_invocation, Is.EqualTo(0L), "The warmed boundary marker allocation guard must remain exact zero.");

			const string sessionId = "0123456789abcdef0123456789abcdef";
			PerfMeterProfilerInstrumentation.RecordSessionBegin(sessionId);
			PerfMeterProfilerInstrumentation.RecordSessionEnd(sessionId);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			long startTimestamp = Stopwatch.GetTimestamp();
			for (int iteration = 0; iteration < baseline.iterations; iteration++)
			{
				PerfMeterProfilerInstrumentation.RecordSessionBegin(sessionId);
				PerfMeterProfilerInstrumentation.RecordSessionEnd(sessionId);
			}

			long elapsedTimestamp = Stopwatch.GetTimestamp() - startTimestamp;
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
			double averageCpuMilliseconds = elapsedTimestamp * 1000d / Stopwatch.Frequency / baseline.iterations;
			double allocatedBytesPerInvocation = allocatedBytes / (double)baseline.iterations;

			Measure.Custom(SessionBoundaryBenchmarkId + ".cpu_ms_per_invocation", averageCpuMilliseconds);
			Measure.Custom(SessionBoundaryBenchmarkId + ".allocated_bytes_per_invocation", allocatedBytesPerInvocation);

			Assert.That(averageCpuMilliseconds, Is.LessThanOrEqualTo(baseline.max_average_cpu_ms_per_invocation),
				$"Warmed session boundary markers averaged {averageCpuMilliseconds:R} ms per pair, above the {baseline.max_average_cpu_ms_per_invocation:R} ms baseline.");
			Assert.That(allocatedBytesPerInvocation, Is.LessThanOrEqualTo(baseline.max_allocated_bytes_per_invocation),
				$"Warmed session boundary markers allocated {allocatedBytesPerInvocation:R} bytes per pair, above the exact-zero baseline.");
		}

		private static PerformanceBaseline LoadBaseline(string benchmarkId)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(BaselineAssetGuid);
			TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
			Assert.That(asset, Is.Not.Null, "The versioned performance baseline asset is missing.");
			PerformanceBaselineDocument document = JsonUtility.FromJson<PerformanceBaselineDocument>(asset.text);
			Assert.That(document, Is.Not.Null, "The performance baseline asset is not valid JSON.");
			Assert.That(document.schema_version, Is.EqualTo(1), "Unsupported performance baseline schema version.");
			Assert.That(document.benchmarks, Is.Not.Null, "The performance baseline has no benchmarks.");

			for (int index = 0; index < document.benchmarks.Length; index++)
			{
				if (string.Equals(document.benchmarks[index].id, benchmarkId, StringComparison.Ordinal))
				{
					return document.benchmarks[index];
				}
			}

			Assert.Fail("The performance baseline does not define " + benchmarkId + ".");
			return null;
		}

		private static void RecordFixedProfilerInstrumentationCounters()
		{
			PerfMeterProfilerInstrumentation.RecordFrameTimings(true, 16d, 8d, 4d, 1d, true, 12d);
			PerfMeterProfilerInstrumentation.RecordBottleneck(PerfMeterBottleneck.Balanced);
			PerfMeterProfilerInstrumentation.RecordCustomMetricCount(1);
			PerfMeterProfilerInstrumentation.RecordSessionState(PerfMeterSessionState.Recording);
			PerfMeterProfilerInstrumentation.RecordAlertScopeActive(false);
			PerfMeterProfilerInstrumentation.RecordOverdrawState(PerfMeterOverdrawMeasurementState.Off);
			PerfMeterProfilerInstrumentation.RecordCaptureState(PerfMeterCaptureState.Idle);
			PerfMeterProfilerInstrumentation.RecordThermalAvailability(false);
		}

		[Serializable]
		private sealed class PerformanceBaselineDocument
		{
			public int schema_version;
			public PerformanceBaseline[] benchmarks;
		}

		[Serializable]
		private sealed class PerformanceBaseline
		{
			public string id;
			public int iterations;
			public double max_average_cpu_ms_per_invocation;
			public long max_allocated_bytes_per_invocation;
		}
	}
}
