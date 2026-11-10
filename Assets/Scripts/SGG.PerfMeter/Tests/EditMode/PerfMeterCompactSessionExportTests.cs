#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterCompactSessionExportTests
	{
		[Test]
		public void CompactReferencesRoundTripCapturedIdentityValuesWarningsAndOrder()
		{
			PerfMeterSessionSampleSnapshot[] samples =
			{
				Sample(1,
					new PerfMeterCustomMetricSnapshot("same.id", "Original", "GPU", "ms", 0d),
					new PerfMeterCustomMetricSnapshot("same.id", "Changed\n\"name", "GPU", "ms", 1.125d, false, "waiting\n\"recorder")),
				Sample(2,
					new PerfMeterCustomMetricSnapshot("same.id", "Original", "GPU", "ms", 9d, false, "later warning"),
					new PerfMeterCustomMetricSnapshot("same.id", "Original", "gpu", "ms", -7d, true, "waiting\n\"recorder"),
					new PerfMeterCustomMetricSnapshot("same.id", "Original", "GPU", "seconds", 8.25d)),
				Sample(3, default(PerfMeterCustomMetricSnapshot)),
				Sample(4)
			};
			FullSessionJson full = JsonUtility.FromJson<FullSessionJson>(Build(samples, PerfMeterSessionJsonFormat.Full));
			SessionJson compact = JsonUtility.FromJson<SessionJson>(Build(samples, PerfMeterSessionJsonFormat.Compact));
			Assert.That(full.schema_version, Is.EqualTo(2));
			Assert.That(compact.schema_version, Is.EqualTo(3));
			Assert.That(compact.custom_metric_descriptors.Length, Is.EqualTo(5), "Name, category case, and unit changes must each create a descriptor.");
			Assert.That(compact.custom_metric_warnings, Is.EqualTo(new[] { "", "waiting\n\"recorder", "later warning" }));
			Assert.That(compact.samples.Length, Is.EqualTo(samples.Length));
			for (int i = 0; i < samples.Length; i++)
			{
				Assert.That(compact.samples[i].frame, Is.EqualTo(samples[i].CollectionFrame));
				Assert.That(compact.samples[i].time_seconds, Is.EqualTo(samples[i].CollectionTimeSeconds));
				Assert.That(compact.samples[i].custom_metrics.Length, Is.EqualTo(samples[i].CustomMetrics.Length));
				for (int j = 0; j < samples[i].CustomMetrics.Length; j++)
				{
					MetricJson item = compact.samples[i].custom_metrics[j];
					Assert.That(item.descriptor, Is.InRange(0, compact.custom_metric_descriptors.Length - 1));
					Assert.That(item.warning, Is.InRange(0, compact.custom_metric_warnings.Length - 1));
					DescriptorJson descriptor = compact.custom_metric_descriptors[item.descriptor];
					FullMetricJson expected = full.samples[i].custom_metrics[j];
					Assert.That(descriptor.id, Is.EqualTo(expected.id));
					Assert.That(descriptor.name, Is.EqualTo(expected.name));
					Assert.That(descriptor.category, Is.EqualTo(expected.category));
					Assert.That(descriptor.unit, Is.EqualTo(expected.unit));
					Assert.That(item.value, Is.EqualTo(expected.value));
					Assert.That(item.available, Is.EqualTo(expected.available));
					Assert.That(compact.custom_metric_warnings[item.warning], Is.EqualTo(expected.warning));
				}
			}
			Assert.That(compact.samples[0].custom_metrics[0].descriptor, Is.EqualTo(0));
			Assert.That(compact.samples[0].custom_metrics[1].descriptor, Is.EqualTo(1));
			Assert.That(compact.samples[1].custom_metrics[0].descriptor, Is.EqualTo(0));
		}

		[Test]
		public void CompactEmptyInputHasEmptyTablesAndCaptureSamplesKeepTheirContract()
		{
			string full = Build(null, PerfMeterSessionJsonFormat.Full);
			string compact = Build(null, PerfMeterSessionJsonFormat.Compact);
			Assert.That(full, Does.StartWith("{\"schema_version\":2"));
			Assert.That(full, Does.Not.Contain("custom_metric_descriptors"));
			Assert.That(compact, Does.Contain("\"custom_metric_descriptors\":[],\"custom_metric_warnings\":[],\"samples\":[]"));
			PerfMeterSessionSampleSnapshot[] samples = { Sample(1, new PerfMeterCustomMetricSnapshot("retained", "Retained", "GPU", "ms", 1d)) };
			string capture = PerfMeterSessionExporter.BuildCaptureSamplesJson("capture", samples);
			Build(samples, PerfMeterSessionJsonFormat.Compact);
			Assert.That(PerfMeterSessionExporter.BuildCaptureSamplesJson("capture", samples), Is.EqualTo(capture));
			Assert.That(capture, Does.Contain("\"schema\":\"sgg.perfmeter.capture-samples\",\"schema_version\":1"));
			Assert.That(capture, Does.Contain("\"custom_metrics\":[{\"id\":\"retained\""));
			Assert.That(capture, Does.Not.Contain("custom_metric_descriptors"));
			Assert.That(Build(samples, PerfMeterSessionJsonFormat.Full), Is.EqualTo(PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, samples, default)));
			Assert.Throws<ArgumentOutOfRangeException>(() => Build(samples, (PerfMeterSessionJsonFormat)99));
		}

		[Test]
		public void CompactKeepsExistingNonfiniteAndNullStringEncoding()
		{
			PerfMeterSessionSampleSnapshot[] samples = { Sample(1,
				new PerfMeterCustomMetricSnapshot(null, null, null, null, double.NaN, false, null),
				new PerfMeterCustomMetricSnapshot("infinite", "Infinite", "", "", double.PositiveInfinity),
				new PerfMeterCustomMetricSnapshot("negative", "Negative", "", "", double.NegativeInfinity, false, "n/a")) };
			foreach (PerfMeterSessionJsonFormat format in new[] { PerfMeterSessionJsonFormat.Full, PerfMeterSessionJsonFormat.Compact })
			{
				string json = Build(samples, format);
				Assert.That(json, Does.Contain("\"value\":\"NaN\",\"available\":false"));
				Assert.That(json, Does.Contain("\"value\":\"Infinity\",\"available\":true"));
				Assert.That(json, Does.Contain("\"value\":\"-Infinity\",\"available\":false"));
				Assert.That(json, Does.Contain("\"id\":\"\",\"name\":\"\",\"category\":\"\",\"unit\":\"\""));
			}
		}

		[Test]
		public void CompactRepeatedFixtureIsDeterministicAcrossCultureAndReducesUtf8Bytes()
		{
			PerfMeterSessionSampleSnapshot[] samples = new PerfMeterSessionSampleSnapshot[160];
			for (int i = 0; i < samples.Length; i++)
			{
				PerfMeterCustomMetricSnapshot[] metrics = new PerfMeterCustomMetricSnapshot[24];
				for (int j = 0; j < metrics.Length; j++)
				{
					metrics[j] = new PerfMeterCustomMetricSnapshot("sgg.repeated.rendering.stage." + j.ToString(CultureInfo.InvariantCulture) + ".gpu_ms",
						"Repeated captured rendering stage with detailed metadata " + j.ToString(CultureInfo.InvariantCulture), "GPU rendering", "milliseconds", i * 0.125d + j, j % 4 != 0,
						j % 4 == 0 ? "No delayed recorder sample for this conditional rendering stage." : "");
				}
				samples[i] = Sample(i, metrics);
			}
			string full = Build(samples, PerfMeterSessionJsonFormat.Full);
			string compact = Build(samples, PerfMeterSessionJsonFormat.Compact);
			CultureInfo original = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
				Assert.That(Build(samples, PerfMeterSessionJsonFormat.Compact), Is.EqualTo(compact));
			}
			finally { CultureInfo.CurrentCulture = original; }
			Assert.That(Build(samples, PerfMeterSessionJsonFormat.Compact), Is.EqualTo(compact));
			Assert.That(Encoding.UTF8.GetByteCount(compact), Is.LessThan(Encoding.UTF8.GetByteCount(full) * 0.75d));
		}

		[TestCase(PerfMeterSessionJsonFormat.Full)]
		[TestCase(PerfMeterSessionJsonFormat.Compact)]
		public void JsonFormatsUseAtomicReplacementAndConflictPolicy(PerfMeterSessionJsonFormat format)
		{
			string path = Path.Combine(Application.temporaryCachePath, "perfmeter-compact-" + Guid.NewGuid().ToString("N") + ".json");
			try
			{
				File.WriteAllText(path, "previous artifact");
				Assert.That(Export(path, false, format).Error, Is.EqualTo("file_exists"));
				Assert.That(File.ReadAllText(path), Is.EqualTo("previous artifact"));
				Assert.That(PerformanceMeter.ExportSessionJson(path, format), Is.True);
				Assert.That(File.ReadAllText(path), Does.StartWith("{\"schema_version\":" + (format == PerfMeterSessionJsonFormat.Full ? "2" : "3")));
				Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".*.tmp"), Is.Empty);
			}
			finally { if (File.Exists(path)) File.Delete(path); }
		}

		[TestCase(PerfMeterSessionJsonFormat.Full)]
		[TestCase(PerfMeterSessionJsonFormat.Compact)]
		[Platform(Include = "Win")]
		public void JsonFormatsLeavePreviousArtifactWhenAtomicReplacementFails(PerfMeterSessionJsonFormat format)
		{
			string path = Path.Combine(Application.temporaryCachePath, "perfmeter-compact-" + Guid.NewGuid().ToString("N") + ".json");
			try
			{
				File.WriteAllText(path, "previous artifact");
				using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					Assert.That(PerformanceMeter.ExportSessionJson(path, format), Is.False);
				}
				Assert.That(File.ReadAllText(path), Is.EqualTo("previous artifact"));
				Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".*.tmp"), Is.Empty);
			}
			finally { if (File.Exists(path)) File.Delete(path); }
		}

		[Test]
		public void McpCompactSelectionExportsSchemaThreeAndRefusesExistingPath()
		{
			string path = "Temp/perfmeter-compact-" + Guid.NewGuid().ToString("N") + ".json";
			try
			{
				string args = "{\"path\":\"" + path + "\",\"format\":\"json_compact\"}";
				Assert.That(PerfMeterMcpCommands.SessionExport(args), Does.Contain("\"success\":true"));
				byte[] bytes = File.ReadAllBytes(path);
				Assert.That(Encoding.UTF8.GetString(bytes), Does.StartWith("{\"schema_version\":3"));
				Assert.That(PerfMeterMcpCommands.SessionExport(args), Does.Contain("\"error\":\"file_exists\""));
				Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
			}
			finally { if (File.Exists(path)) File.Delete(path); }
		}

		private static PerfMeterSessionSampleSnapshot Sample(int frame, params PerfMeterCustomMetricSnapshot[] metrics)
			=> new PerfMeterSessionSampleSnapshot(frame, frame * 0.125d, "Retained scene", default, metrics);

		private static string Build(PerfMeterSessionSampleSnapshot[] samples, PerfMeterSessionJsonFormat format)
			=> PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, samples, default, format);

		private static PerfMeterSessionExportResult Export(string path, bool overwrite, PerfMeterSessionJsonFormat format)
			=> PerfMeterSessionExporter.ExportJson(path, PerfMeterSessionSummarySnapshot.Empty, Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerfMeterSessionTimelineSnapshot.Empty, default, overwrite, PerfMeterSessionExporter.RuntimePackageIdentity,
				PerfMeterSelfOverheadWindowSnapshot.Unavailable, format);

		[Serializable] private sealed class SessionJson { public int schema_version; public DescriptorJson[] custom_metric_descriptors; public string[] custom_metric_warnings; public SampleJson[] samples; }
		[Serializable] private sealed class DescriptorJson { public string id; public string name; public string category; public string unit; }
		[Serializable] private sealed class SampleJson { public int frame; public double time_seconds; public MetricJson[] custom_metrics; }
		[Serializable] private sealed class FullSessionJson { public int schema_version; public FullSampleJson[] samples; }
		[Serializable] private sealed class FullSampleJson { public FullMetricJson[] custom_metrics; }
		[Serializable] private sealed class MetricJson { public int descriptor; public double value; public bool available; public int warning; }
		[Serializable] private sealed class FullMetricJson { public string id; public string name; public string category; public string unit; public double value; public bool available; public string warning; }
	}
}
#endif
