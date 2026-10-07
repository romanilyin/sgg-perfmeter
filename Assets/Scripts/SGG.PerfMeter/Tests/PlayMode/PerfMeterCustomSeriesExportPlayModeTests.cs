#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SGG.PerfMeter.Tests.PlayMode
{
	public sealed class PerfMeterCustomSeriesExportPlayModeTests
	{
		private static readonly string[] Ids =
		{
			"sgg.sky.cloud.raymarch.gpu_ms", "sgg.sky.cloud.post.gpu_ms",
			"sgg.sky.froxel.gpu_ms", "sgg.sky.ssms.gpu_ms"
		};
		private SyntheticProvider[] _providers;
		private string _firstPath;
		private string _secondPath;

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
			_providers = new SyntheticProvider[Ids.Length];
			for (int index = 0; index < Ids.Length; index++)
			{
				_providers[index] = new SyntheticProvider(Ids[index], index);
				PerformanceMeter.RegisterCustomMetricProvider(_providers[index]);
			}
			string stem = "Temp/perfmeter-series-" + Guid.NewGuid().ToString("N");
			_firstPath = stem + "-first.json";
			_secondPath = stem + "-second.json";
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
			if (File.Exists(_firstPath)) File.Delete(_firstPath);
			if (File.Exists(_secondPath)) File.Delete(_secondPath);
		}

		[UnityTest]
		public IEnumerator LiveProvidersRetainThreeHundredSamplesAndMcpBytesMatchExactSession()
		{
			// Synthetic providers exercise routing/retention/serialization, not real GPU measurement.
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.01f, 300, false, 0, 0f));
			yield return WaitForSamples(300);
			PerformanceMeter.StopSession(); // Keep runtime alive: MCP exports its retained session.
			PerfMeterSessionSampleSnapshot[] first = PerformanceMeter.GetSessionSamples();
			string firstId = PerformanceMeter.GetSessionSummary().SessionId;
			Assert.That(first.Length, Is.EqualTo(300));
			byte[] firstBytes = ExportAndCompare(_firstPath, firstId, first);
			foreach (PerfMeterSessionSampleSnapshot sample in first)
			{
				AssertSyntheticValues(sample);
			}

			// Reuse collection capacity with fewer reports and a failed provider in a new session.
			_providers[0].ReturnFalse = true;
			_providers[1].Throw = true;
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.01f, 2, false, 0, 0f));
			yield return WaitForSamples(2);
			PerformanceMeter.StopSession();
			PerfMeterSessionSampleSnapshot[] second = PerformanceMeter.GetSessionSamples();
			string secondId = PerformanceMeter.GetSessionSummary().SessionId;
			Assert.That(secondId, Is.Not.EqualTo(firstId));
			Assert.That(second.Length, Is.EqualTo(2));
			foreach (PerfMeterSessionSampleSnapshot sample in second)
			{
				Assert.That(sample.CustomMetrics.Length, Is.EqualTo(3), "Unused buffer capacity must not be exported.");
				Assert.That(sample.CustomMetrics[0].Id, Is.EqualTo(Ids[1]));
				Assert.That(sample.CustomMetrics[0].Available, Is.False);
				Assert.That(sample.CustomMetrics[0].Warning, Does.Contain("Synthetic provider failure"));
				Assert.That(sample.CollectionFrame, Is.GreaterThan(first[first.Length - 1].CollectionFrame));
			}
			ExportAndCompare(_secondPath, secondId, second);
			foreach (PerfMeterSessionSampleSnapshot sample in first) AssertSyntheticValues(sample);
			Assert.That(File.ReadAllBytes(_firstPath), Is.EqualTo(firstBytes));
		}

		private static void AssertSyntheticValues(PerfMeterSessionSampleSnapshot sample)
		{
			Assert.That(sample.CustomMetrics.Length, Is.EqualTo(4));
			for (int index = 0; index < 4; index++)
			{
				PerfMeterCustomMetricSnapshot metric = sample.CustomMetrics[index];
				Assert.That(metric.Id, Is.EqualTo(Ids[index]));
				Assert.That(metric.Available, Is.EqualTo(index != 3));
				double expected = index == 1 || index == 3 ? 0d : 1d + index + (sample.CollectionFrame % 8) * 0.125d;
				Assert.That(metric.Value, Is.EqualTo(expected), "Historical values must be tied to the collection frame, not a reused latest buffer.");
				Assert.That(metric.Warning, Is.EqualTo(index == 3 ? "PassUnavailable: synthetic fixture declares no SSMS pass." : string.Empty));
			}
		}

		private static IEnumerator WaitForSamples(int count)
		{
			long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 30L;
			while (PerformanceMeter.GetSessionSummary().SampleCount < count && Stopwatch.GetTimestamp() < deadline)
			{
				yield return null;
			}
			Assert.That(PerformanceMeter.GetSessionSummary().SampleCount, Is.EqualTo(count), "Live runtime did not retain the requested samples within 30 seconds.");
		}

		private static byte[] ExportAndCompare(string path, string sessionId, PerfMeterSessionSampleSnapshot[] retained)
		{
			Type commands = Type.GetType("SGG.PerfMeter.Editor.Mcp.PerfMeterMcpCommands, SGG.PerfMeter.Editor", true);
			MethodInfo export = commands.GetMethod("SessionExport", BindingFlags.Public | BindingFlags.Static);
			string envelope = (string)export.Invoke(null, new object[] { "{\"path\":\"" + path + "\",\"format\":\"json\"}" });
			Assert.That(envelope, Does.Contain("\"success\":true"));
			byte[] bytes = File.ReadAllBytes(path);
			SessionJson json = JsonUtility.FromJson<SessionJson>(Encoding.UTF8.GetString(bytes));
			Assert.That(json.session_id, Is.EqualTo(sessionId));
			Assert.That(json.metadata.custom_metric_sample_count, Is.EqualTo(retained.Length));
			Assert.That(json.samples.Length, Is.EqualTo(retained.Length));
			for (int sampleIndex = 0; sampleIndex < retained.Length; sampleIndex++)
			{
				PerfMeterSessionSampleSnapshot actual = retained[sampleIndex];
				SampleJson encoded = json.samples[sampleIndex];
				Assert.That(encoded.frame, Is.EqualTo(actual.CollectionFrame));
				Assert.That(encoded.time_seconds, Is.EqualTo(actual.CollectionTimeSeconds).Within(0.000000001d));
				Assert.That(encoded.custom_metrics.Length, Is.EqualTo(actual.CustomMetrics.Length));
				for (int metricIndex = 0; metricIndex < actual.CustomMetrics.Length; metricIndex++)
				{
					PerfMeterCustomMetricSnapshot metric = actual.CustomMetrics[metricIndex];
					MetricJson item = encoded.custom_metrics[metricIndex];
					Assert.That(item.id, Is.EqualTo(metric.Id));
					Assert.That(item.name, Is.EqualTo(metric.Name));
					Assert.That(item.category, Is.EqualTo(metric.Category));
					Assert.That(item.unit, Is.EqualTo(metric.Unit));
					Assert.That(item.value, Is.EqualTo(metric.Value));
					Assert.That(item.available, Is.EqualTo(metric.Available));
					Assert.That(item.warning, Is.EqualTo(metric.Warning));
				}
			}
			return bytes;
		}

		private sealed class SyntheticProvider : IPerfMeterCustomMetricProvider
		{
			private readonly int _index;
			public string Id { get; }
			internal bool ReturnFalse;
			internal bool Throw;
			internal SyntheticProvider(string id, int index) { Id = id; _index = index; }
			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				if (Throw) throw new InvalidOperationException("Synthetic provider failure");
				bool available = _index != 3;
				double value = _index == 1 || !available ? 0d : 1d + _index + (Time.frameCount % 8) * 0.125d;
				metric = new PerfMeterCustomMetricSnapshot(Id, Id, "GPU", "ms", value, available,
					available ? string.Empty : "PassUnavailable: synthetic fixture declares no SSMS pass.");
				return !ReturnFalse;
			}
		}

		[Serializable] private sealed class SessionJson { public string session_id; public MetadataJson metadata; public SampleJson[] samples; }
		[Serializable] private sealed class MetadataJson { public int custom_metric_sample_count; }
		[Serializable] private sealed class SampleJson { public int frame; public double time_seconds; public MetricJson[] custom_metrics; }
		[Serializable] private sealed class MetricJson { public string id; public string name; public string category; public string unit; public double value; public bool available; public string warning; }
	}
}
#endif
