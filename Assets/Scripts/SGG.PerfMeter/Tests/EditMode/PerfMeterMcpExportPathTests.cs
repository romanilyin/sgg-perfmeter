using System;
using System.IO;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterMcpExportPathTests
	{
		[Serializable]
		private sealed class Arguments { public string path; public string format = "json"; }
		[Serializable]
		private sealed class Result { public bool success; public string error; public string status; public string next_action; }

		[SetUp]
		public void SetUp() => PerformanceMeter.Stop();

		[TestCase("\"line\\n\\r\\t\\b\\f\"", "line\n\r\t\b\f")]
		[TestCase("\"\\u8def\\u5f84\"", "路径")]
		[TestCase("\"\\ud83d\\ude80\"", "🚀")]
		[TestCase("\"slash\\\\\\/\\\"\"", "slash\\/\"")]
		public void SharedArgumentReaderDecodesStandardJsonEscapes(string input, string expected)
		{
			object[] args = { input, 0, null, 0 };
			System.Reflection.MethodInfo reader = typeof(PerfMeterMcpCommands).GetMethod("TryReadJsonString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			Assert.That(reader.Invoke(null, args), Is.True);
			Assert.That(args[2], Is.EqualTo(expected));
			Assert.That(args[3], Is.EqualTo(input.Length));
		}

		[TestCase("\"\\q\"")]
		[TestCase("\"\\u00G0\"")]
		[TestCase("\"\\u123\0\"")]
		[TestCase("\"line\n\"")]
		public void SharedArgumentReaderRejectsInvalidJsonEscapes(string input)
		{
			object[] args = { input, 0, null, 0 };
			System.Reflection.MethodInfo reader = typeof(PerfMeterMcpCommands).GetMethod("TryReadJsonString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			Assert.That(reader.Invoke(null, args), Is.False);
		}

		[Test]
		public void IncorrectArgumentTypeRemainsSchemaFailure()
		{
			InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => PerfMeterMcpCommands.SessionExport("{\"path\":42,\"format\":\"json\"}"));
			Assert.That(error.Message, Does.StartWith("schema_validation_failed"));
		}

		[TestCase("absolute")]
		[TestCase("traversal")]
		[TestCase("sibling")]
		public void OutOfProjectStringReturnsTypedPolicyFailureWithoutWriting(string kind)
		{
			string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string filename = "perfmeter-policy-" + Guid.NewGuid().ToString("N") + ".json";
			string path = kind == "absolute" ? Path.Combine(Path.GetTempPath(), filename) :
				kind == "traversal" ? "../" + filename : root + "-sibling/" + filename;
			string fullPath = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
			string json = PerfMeterMcpCommands.SessionExport(JsonUtility.ToJson(new Arguments { path = path }));
			Result result = JsonUtility.FromJson<Result>(json);
			Assert.That(result.success, Is.False);
			Assert.That(result.error, Is.EqualTo("path_policy_violation"));
			Assert.That(result.status, Is.EqualTo("not_exported"));
			Assert.That(result.next_action, Does.Contain("project-relative"));
			Assert.That(json, Does.Not.Contain("schema_validation_failed"));
			Assert.That(File.Exists(fullPath), Is.False);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[TestCase("Temp/invalid\0.json")]
		[TestCase("")]
		[TestCase(" ")]
		public void MalformedStringReturnsInvalidPathWithoutThrowingSchemaFailure(string path)
		{
			Result result = JsonUtility.FromJson<Result>(PerfMeterMcpCommands.SessionExport(
				"{\"path\":\"" + path.Replace("\0", "\\u0000") + "\",\"format\":\"json\"}"));
			Assert.That(result.success, Is.False);
			Assert.That(result.error, Is.EqualTo("invalid_path"));
			Assert.That(result.next_action, Is.Not.Empty);
		}

		[TestCase(false, "json")]
		[TestCase(false, "unicode")]
		[TestCase(true, "json")]
		[TestCase(false, "csv")]
		[TestCase(true, "csv")]
		public void InProjectRelativeAndAbsolutePathsExportAndKeepExistingArtifact(bool absolute, string format)
		{
			bool unicode = format == "unicode";
			if (unicode) format = "json";
			string relative = "Temp/perfmeter-path-" + Guid.NewGuid().ToString("N") + "." + format;
			if (unicode) relative = relative.Replace("perfmeter-path", "perfmeter-路径");
			string full = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);
			string args = JsonUtility.ToJson(new Arguments { path = absolute ? full : relative, format = format });
			if (unicode) args = args.Replace("路径", "\\u8def\\u5f84");
			try
			{
				Assert.That(JsonUtility.FromJson<Result>(PerfMeterMcpCommands.SessionExport(args)).success, Is.True);
				byte[] original = File.ReadAllBytes(full);
				Assert.That(original.Length, Is.GreaterThan(0));
				Assert.That(JsonUtility.FromJson<Result>(PerfMeterMcpCommands.SessionExport(args)).error, Is.EqualTo("file_exists"));
				Assert.That(File.ReadAllBytes(full), Is.EqualTo(original));
			}
			finally
			{
				if (File.Exists(full)) File.Delete(full);
			}
		}
	}
}
