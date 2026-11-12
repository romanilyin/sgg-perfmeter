#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NUnit.Framework;
using SGG.PerfMeter.Editor.FrameDebugger;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFrameDebuggerWriterTests
	{
		private string _project;
		private string Root => Path.Combine(_project, "Temp", "PerfMeter", "FrameDebuggerExports");

		[SetUp]
		public void SetUp()
		{
			_project = Path.Combine(Path.GetTempPath(), "sgg-frame-debugger-writer-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_project);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_project)) Directory.Delete(_project, true);
		}

		[TestCase(65)]
		[TestCase(128)]
		public void ValueAndFlagsPairsForMoreThan64PropertiesCanBePublished(int count)
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			snapshot.IncludeProperties = true;
			snapshot.Events[0].Fields = Enumerable.Range(0, count).SelectMany(index => new[]
			{
				Field("property.float." + index + ".value", (double)index),
				Field("property.float." + index + ".flags.value", 256)
			}).ToArray();
			PerfMeterFrameDebuggerWriteResult result = PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None);
			Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "events.json")), Is.True);
			Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "properties.csv")), Is.True);
			StringAssert.Contains("property.float." + (count - 1) + ".flags.value", File.ReadAllText(Path.Combine(result.OutputDirectory, "events.json")));
		}

		[Test]
		public void JsonPreservesUnicodeEscapedNamesAndRawFormulaText()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			snapshot.SceneName = "雪山🚀";
			snapshot.Events[0].Name = "=DRAW(\"雪\")\n\\draw\t";
			snapshot.Events[0].Fields = new[] { Field("keywords", new[] { "FOO", "\"BAR\"\n雪", "🚀" }) };
			string json = PerfMeterFrameDebuggerWriter.BuildJson(snapshot);
			JsonEnvelope parsed = JsonUtility.FromJson<JsonEnvelope>(json);
			Assert.That(parsed.schema, Is.EqualTo("sgg.perfmeter.frame-debugger"));
			Assert.That(parsed.schema_version, Is.EqualTo(1));
			Assert.That(parsed.provenance, Is.EqualTo("observed_not_exact_frame"));
			Assert.That(parsed.metadata.scene_name, Is.EqualTo(snapshot.SceneName));
			Assert.That(parsed.events[0].name, Is.EqualTo(snapshot.Events[0].Name));
			Assert.That(json, Does.Contain("\"value\":[\"FOO\",\"\\\"BAR\\\"\\n雪\",\"\\ud83d\\ude80\"]"));
			Assert.That(json, Does.Not.Contain("'=DRAW"));
		}

		[Test]
		public void JsonUsesTypedBooleansInvariantNumbersAndScalarArrays()
		{
			CultureInfo original = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
				PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
				snapshot.SelectionRestored = true;
				snapshot.Events[0].Fields = new[] { Field("enabled", true), Field("value", 1.25d), Field("vector", new[] { 1.25d, -2d, 0d }), Field("count", 12L) };
				string json = PerfMeterFrameDebuggerWriter.BuildJson(snapshot);
				Assert.That(json, Does.Contain("\"selection_restored\":true"));
				Assert.That(json, Does.Contain("\"name\":\"enabled\",\"kind\":\"test\",\"available\":true,\"value\":true"));
				Assert.That(json, Does.Contain("\"value\":1.25,"));
				Assert.That(json, Does.Contain("\"value\":[1.25,-2,0]"));
				Assert.That(json, Does.Contain("\"value\":12,"));
			}
			finally { CultureInfo.CurrentCulture = original; }
		}

		[TestCase(double.NaN)]
		[TestCase(double.PositiveInfinity)]
		[TestCase(double.NegativeInfinity)]
		public void NonFiniteValuesBecomeUnavailableNullWithReason(double value)
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			snapshot.Events[0].Fields = new[] { Field("scalar", value), Field("vector", new[] { 1d, value }) };
			string json = PerfMeterFrameDebuggerWriter.BuildJson(snapshot);
			Assert.That(json.Split(new[] { "\"available\":false,\"value\":null,\"reason\":\"non_finite_value\"" }, StringSplitOptions.None).Length - 1, Is.EqualTo(2));
			Assert.That(json, Does.Not.Contain("NaN"));
			Assert.That(json, Does.Not.Contain("Infinity"));
		}

		[Test]
		public void UnavailableAndUnsupportedValuesRetainExplicitReasonsWithoutObjectFormatting()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			PerfMeterFrameDebuggerField missing = Field("vertices", 999);
			missing.Available = false; missing.Reason = "API not exposed";
			snapshot.Events[0].Fields = new[] { missing, Field("null", null), Field("object", new ThrowOnFormatting()) };
			string json = PerfMeterFrameDebuggerWriter.BuildJson(snapshot);
			Assert.That(json, Does.Contain("\"available\":false,\"value\":null,\"reason\":\"API not exposed\""));
			Assert.That(json, Does.Contain("\"reason\":\"value_missing\""));
			Assert.That(json, Does.Contain("\"reason\":\"unsupported_value_type\""));
			Assert.That(PerfMeterFrameDebuggerWriter.BuildCsv(snapshot), Does.Not.Contain("999"));
		}

		[Test]
		public void CsvHasStableColumnsQuotesMultilineTextAndNeutralizesTextFormulas()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			PerfMeterFrameDebuggerEvent row = snapshot.Events[0];
			row.Index = 3; row.Name = "=SUM(1,2)\n\"雪\""; row.Reason = "\tunsafe";
			row.Fields = new[] { Field("shader", "-shader"), Field("draw_calls", -2), Field("vertices", 1.25d) };
			string csv = PerfMeterFrameDebuggerWriter.BuildCsv(snapshot);
			Assert.That(csv, Is.EqualTo("index,type,name,status,reason,shader,shader_pass,draw_calls,vertices,indices,instances,render_target_name,width,height\r\n" +
				"3,\"Draw\",\"'=SUM(1,2)\n\"\"雪\"\"\",\"available\",\"'\tunsafe\",\"'-shader\",,-2,1.25,,,,,\r\n"));
		}

		[TestCase("+cmd")]
		[TestCase("-cmd")]
		[TestCase("@cmd")]
		[TestCase(" =cmd")]
		[TestCase("\r=cmd")]
		[TestCase("\u0001cmd")]
		public void CsvNeutralizesAllDangerousTextPrefixes(string text)
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.Events[0].Name = text;
			Assert.That(PerfMeterFrameDebuggerWriter.BuildCsv(snapshot), Does.Contain(",\"'" + text + "\","));
		}

		[Test]
		public void WritePublishesOnlyCompleteUtf8FilesAndManifestWithHashesAndExactTotal()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.SceneName = "雪🚀";
			PerfMeterFrameDebuggerWriteResult result = PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None);
			Assert.That(result.OutputDirectory, Is.EqualTo(Path.Combine(Root, snapshot.ExportId)));
			byte[] json = File.ReadAllBytes(Path.Combine(result.OutputDirectory, "events.json"));
			Assert.That(json, Is.EqualTo(new UTF8Encoding(false).GetBytes(PerfMeterFrameDebuggerWriter.BuildJson(snapshot))));
			Assert.That(json[0], Is.EqualTo((byte)'{'));
			Assert.That(result.JsonSha256, Is.EqualTo(Sha256(json)));
			Assert.That(result.CsvSha256, Is.EqualTo(Sha256(File.ReadAllBytes(Path.Combine(result.OutputDirectory, "events.csv")))));
			Assert.That(result.TotalBytes, Is.EqualTo(Directory.GetFiles(result.OutputDirectory).Sum(path => new FileInfo(path).Length)));
			string manifest = File.ReadAllText(Path.Combine(result.OutputDirectory, "manifest.json"));
			Assert.That(manifest, Does.Contain(result.JsonSha256));
			Assert.That(manifest, Does.Contain(result.CsvSha256));
			Assert.That(manifest, Does.Contain("\"size_bytes\":" + json.Length.ToString(CultureInfo.InvariantCulture)));
			Assert.That(manifest, Does.Contain("\"completeness\":\"partial\""));
			Assert.That(manifest, Does.Contain("\"provenance\":\"observed_not_exact_frame\""));
			Assert.That(Directory.GetDirectories(Root, ".staging-*"), Is.Empty);
		}

		[Test]
		public void PropertiesAreOptInAndLongFormCsvReportsAvailability()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			PerfMeterFrameDebuggerField absent = Field("property.texture.0._MainTex", null); absent.Reason = "not exposed"; absent.Available = false;
			snapshot.Events[0].Fields = new[] { Field("property.float.0._Gloss", 0.5d), absent };
			Assert.That(PerfMeterFrameDebuggerWriter.BuildJson(snapshot), Does.Not.Contain("property.float"));
			PerfMeterFrameDebuggerWriteResult basic = PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None);
			Assert.That(File.Exists(Path.Combine(basic.OutputDirectory, "properties.csv")), Is.False);
			snapshot.ExportId = "properties"; snapshot.IncludeProperties = true;
			PerfMeterFrameDebuggerWriteResult detailed = PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None);
			Assert.That(File.ReadAllText(Path.Combine(detailed.OutputDirectory, "properties.csv")), Is.EqualTo(
				"event_index,name,kind,value,available,reason\r\n0,\"property.float.0._Gloss\",\"test\",0.5,true,\"\"\r\n0,\"property.texture.0._MainTex\",\"test\",,false,\"not exposed\"\r\n"));
			Assert.That(File.ReadAllText(Path.Combine(detailed.OutputDirectory, "events.json")), Does.Contain("property.float.0._Gloss"));
		}

		[Test]
		public void PreCancelledWriteCreatesNoExportOrStagingContent()
		{
			using (CancellationTokenSource cancellation = new CancellationTokenSource())
			{
				cancellation.Cancel();
				Assert.Throws<OperationCanceledException>(() => PerfMeterFrameDebuggerWriter.Write(_project, Snapshot(), cancellation.Token));
			}
			Assert.That(Directory.Exists(Root), Is.False);
		}

		[TestCase("../escape")]
		[TestCase("a/b")]
		[TestCase("a\\b")]
		[TestCase(".")]
		[TestCase("..")]
		[TestCase("C:escape")]
		public void ExportIdentifierCannotEscapeFixedRoot(string id)
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.ExportId = id;
			Assert.Throws<ArgumentException>(() => PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None));
			Assert.That(Directory.GetFileSystemEntries(_project), Is.Empty);
		}

		[Test]
		public void ProjectTraversalAndRelativePathsAreRefused()
		{
			Assert.Throws<ArgumentException>(() => PerfMeterFrameDebuggerWriter.Write("relative", Snapshot(), CancellationToken.None));
			Assert.Throws<ArgumentException>(() => PerfMeterFrameDebuggerWriter.Write(Path.Combine(_project, "child", ".."), Snapshot(), CancellationToken.None));
		}

		[Test]
		public void ExistingDestinationIsPreservedWithoutOverwritingOrDeletingUnknownSibling()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot();
			string destination = Path.Combine(Root, snapshot.ExportId); Directory.CreateDirectory(destination);
			string keep = Path.Combine(destination, "keep"); File.WriteAllText(keep, "original");
			Assert.Throws<IOException>(() => PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None));
			Assert.That(File.ReadAllText(keep), Is.EqualTo("original"));
			Assert.That(Directory.GetDirectories(Root, ".staging-*"), Is.Empty);
		}

		[Test]
		public void SerializationFailureRemovesOnlyItsOwnMarkedStagingDirectory()
		{
			string unknown = Path.Combine(Root, ".staging-unknown"); Directory.CreateDirectory(unknown);
			File.WriteAllText(Path.Combine(unknown, "keep"), "keep");
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.Events[0].Name = "invalid-\ud800";
			Assert.Throws<EncoderFallbackException>(() => PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None));
			Assert.That(Directory.GetDirectories(Root), Is.EqualTo(new[] { unknown }));
			Assert.That(File.ReadAllText(Path.Combine(unknown, "keep")), Is.EqualTo("keep"));
		}

		[Test]
		public void RetentionKeepsEightOwnedExportsAndPreservesUnknownDirectories()
		{
			string unknown = Path.Combine(Root, "user-content"); Directory.CreateDirectory(unknown); File.WriteAllText(Path.Combine(unknown, "keep"), "keep");
			for (int i = 0; i < 9; i++)
			{
				PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.ExportId = "export-" + i.ToString("D2", CultureInfo.InvariantCulture);
				PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None);
			}
			Assert.That(Directory.GetDirectories(Root).Length, Is.EqualTo(9));
			Assert.That(Directory.Exists(Path.Combine(Root, "export-08")), Is.True);
			Assert.That(File.ReadAllText(Path.Combine(unknown, "keep")), Is.EqualTo("keep"));
		}

		[Test]
		public void UnremovableOwnedExportsCauseExplicitQuotaFailureAndPreserveUnknownFiles()
		{
			for (int i = 0; i < 8; i++)
			{
				string id = "owned-" + i.ToString(CultureInfo.InvariantCulture); string directory = Path.Combine(Root, id); Directory.CreateDirectory(directory);
				File.WriteAllText(Path.Combine(directory, PerfMeterFrameDebuggerWriter.MarkerFileName), "sgg.perfmeter.frame-debugger\n1\ncommitted\n" + id + "\n0123456789abcdef0123456789abcdef\n");
				File.WriteAllText(Path.Combine(directory, "manifest.json"), "{}"); File.WriteAllText(Path.Combine(directory, "unknown-keep"), "keep");
			}
			IOException error = Assert.Throws<IOException>(() => PerfMeterFrameDebuggerWriter.Write(_project, Snapshot(), CancellationToken.None));
			Assert.That(error.Message, Does.Contain("quota"));
			Assert.That(Directory.GetDirectories(Root).Length, Is.EqualTo(8));
			Assert.That(Directory.GetFiles(Root, "unknown-keep", SearchOption.AllDirectories).Length, Is.EqualTo(8));
		}

		[Test]
		public void ReparseProjectAncestorIsRefusedWhenSymbolicLinksAreSupported()
		{
			MethodInfo createLink = typeof(Directory).GetMethod("CreateSymbolicLink", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
			string target = Path.Combine(_project, "target"); string alias = Path.Combine(_project, "alias"); Directory.CreateDirectory(target);
			try
			{
				if (!TryCreateDirectoryLink(createLink, alias, target)) Assert.Ignore("The test host does not permit directory symbolic links or junctions.");
				Assert.Throws<IOException>(() => PerfMeterFrameDebuggerWriter.Write(alias, Snapshot(), CancellationToken.None));
				Assert.That(Directory.GetFileSystemEntries(target), Is.Empty);
			}
			finally { if (Directory.Exists(alias)) Directory.Delete(alias, false); }
		}

		[Test]
		public void OversizedDetachedValuesAreRejectedBeforeCreatingExportRoot()
		{
			PerfMeterFrameDebuggerSnapshot snapshot = Snapshot(); snapshot.Events[0].Name = new string('x', 4097);
			Assert.Throws<InvalidDataException>(() => PerfMeterFrameDebuggerWriter.Write(_project, snapshot, CancellationToken.None));
			Assert.That(Directory.Exists(Root), Is.False);
			snapshot.Events[0].Name = "draw"; snapshot.Events[0].Fields = new[] { Field("array", new double[4097]) };
			Assert.Throws<InvalidDataException>(() => PerfMeterFrameDebuggerWriter.BuildJson(snapshot));
		}

		private static PerfMeterFrameDebuggerSnapshot Snapshot() => new PerfMeterFrameDebuggerSnapshot
		{
			ExportId = "test-export", StartedUtc = "2026-11-13T00:00:00.0000000Z", CompletedUtc = "2026-11-13T00:00:01.0000000Z",
			UnityVersion = "test", TotalEventCount = 2, RequestedEventCount = 2, ExportedEventCount = 1, MissingEventCount = 1,
			Events = new[] { new PerfMeterFrameDebuggerEvent { EventType = "Draw", Name = "draw" } }
		};

		private static PerfMeterFrameDebuggerField Field(string name, object value) => new PerfMeterFrameDebuggerField { Name = name, Kind = "test", Available = true, Value = value };
		private static bool TryCreateDirectoryLink(MethodInfo method, string alias, string target)
		{
			if (method != null)
			{
				try { method.Invoke(null, new object[] { alias, target }); return true; }
				catch (TargetInvocationException exception) when (exception.InnerException is UnauthorizedAccessException || exception.InnerException is IOException || exception.InnerException is PlatformNotSupportedException) { }
			}
			if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
			using (Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe", Arguments = "/c mklink /J \"" + alias + "\" \"" + target + "\"",
				UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
			}))
			{
				if (process == null) return false;
				if (!process.WaitForExit(5000)) { process.Kill(); return false; }
				return process.ExitCode == 0;
			}
		}
		private static string Sha256(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
		}
		private sealed class ThrowOnFormatting { public override string ToString() => throw new InvalidOperationException("Must not format unsupported objects."); }
		[Serializable] private sealed class JsonEnvelope { public string schema; public int schema_version; public string provenance; public JsonMetadata metadata; public JsonEvent[] events; }
		[Serializable] private sealed class JsonMetadata { public string scene_name; }
		[Serializable] private sealed class JsonEvent { public string name; }
	}
}
#endif
