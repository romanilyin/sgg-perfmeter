using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SGG.PerfMeter.Editor;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocReplayAnalyzerTests
	{
		private const string FixtureRoot = "Tests/EditMode/Fixtures/RenderDocAnalyzer/";
		private string _root;
		private string _projectRoot;
		private string _packageRoot;
		private string _renderDocRoot;
		private string _capturePath;

		[SetUp]
		public void SetUp()
		{
			_root = Path.Combine(Path.GetTempPath(), "sgg-perfmeter-rda-" + Guid.NewGuid().ToString("N"));
			_projectRoot = Path.Combine(_root, "project");
			_packageRoot = Path.Combine(_root, "package");
			_renderDocRoot = Path.Combine(_root, "renderdoc");
			_capturePath = Path.Combine(_projectRoot, "Temp", "PerfMeter", "RenderDocCopies", "golden", "capture.rdc");
			Directory.CreateDirectory(Path.GetDirectoryName(_capturePath));
			Directory.CreateDirectory(Path.Combine(_packageRoot, "Tools~", "RenderDocAnalyzer"));
			Directory.CreateDirectory(_renderDocRoot);
			File.WriteAllBytes(_capturePath, new byte[] { 1, 2, 3, 4 });
			File.WriteAllText(Path.Combine(_packageRoot, PerfMeterRenderDocReplayAnalyzer.AnalyzerScriptRelativePath.Replace('/', Path.DirectorySeparatorChar)), "raise SystemExit(0)\n");
			File.WriteAllBytes(Path.Combine(_renderDocRoot, PerfMeterRenderDocReplayAnalyzer.RenderDocExecutableName), new byte[] { 1 });
		}

		[TearDown]
		public void TearDown()
		{
			if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root))
			{
				Directory.Delete(_root, true);
			}
		}

		[Test]
		public void LaunchUsesOnlyPackageScriptAndMarkerOwnedWorkspace()
		{
			string response = ReadFixture("result-v1.json").Replace("\"size_bytes\": \"536870912\"", "\"size_bytes\": \"4\"");
			RecordingProcessRunner runner = new RecordingProcessRunner(start =>
			{
				Assert.That(start.FileName, Is.EqualTo(Path.Combine(_renderDocRoot, PerfMeterRenderDocReplayAnalyzer.RenderDocExecutableName)));
				Assert.That(start.Arguments, Is.EqualTo("--python " + PerfMeterRenderDocReplayAnalyzer.QuoteWindowsArgument(Path.Combine(_packageRoot, PerfMeterRenderDocReplayAnalyzer.AnalyzerScriptRelativePath.Replace('/', Path.DirectorySeparatorChar)))));
				Assert.That(start.Arguments, Does.Not.Contain(_capturePath));
				Assert.That(start.Arguments, Does.Not.Contain("rd-analysis-golden"));
				Assert.That(Path.GetFileName(start.WorkingDirectory), Has.Length.EqualTo(32));
				Assert.That(File.ReadAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.WorkspaceMarkerFileName)), Does.Contain("nonce=" + Path.GetFileName(start.WorkingDirectory)));
				Assert.That(File.ReadAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.RequestFileName)), Does.Contain("rd-analysis-golden"));
				Assert.That(start.Environment["USERPROFILE"], Is.EqualTo(Path.Combine(start.WorkingDirectory, "profile")));
				Assert.That(start.Environment["APPDATA"], Is.EqualTo(Path.Combine(start.Environment["USERPROFILE"], "AppData", "Roaming")));
				Assert.That(File.ReadAllText(Path.Combine(start.Environment["APPDATA"], "qrenderdoc", "UI.config")), Does.Contain("\"Analytics_TotalOptOut\":true"));
				Assert.That(start.Environment.Values, Has.None.Contains(_capturePath));
				File.WriteAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.ResponseFileName), response);
				return CompletedProcess();
			});
			PerfMeterRenderDocReplayAnalyzer analyzer = CreateAnalyzer(runner);

			PerfMeterRenderDocAnalyzerExecution execution = analyzer.Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.Status, Is.EqualTo(PerfMeterRenderDocAnalyzerExecutionStatus.Completed));
			Assert.That(execution.Result, Is.Not.Null);
			Assert.That(execution.Result.request_id, Is.EqualTo("rd-analysis-golden"));
			Assert.That(Directory.Exists(runner.Start.WorkingDirectory), Is.False);
		}

		[TestCase((int)PerfMeterRenderDocAnalyzerProcessStatus.TimedOut, (int)PerfMeterRenderDocAnalyzerExecutionStatus.TimedOut, "analysis_timed_out")]
		[TestCase((int)PerfMeterRenderDocAnalyzerProcessStatus.Canceled, (int)PerfMeterRenderDocAnalyzerExecutionStatus.Canceled, "analysis_canceled")]
		[TestCase((int)PerfMeterRenderDocAnalyzerProcessStatus.StartFailed, (int)PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "process_start_failed")]
		public void ProcessFailuresMapExplicitlyAndCleanWorkspace(
			int processStatusValue,
			int expectedStatusValue,
			string expectedError)
		{
			PerfMeterRenderDocAnalyzerProcessStatus processStatus = (PerfMeterRenderDocAnalyzerProcessStatus)processStatusValue;
			PerfMeterRenderDocAnalyzerExecutionStatus expectedStatus = (PerfMeterRenderDocAnalyzerExecutionStatus)expectedStatusValue;
			RecordingProcessRunner runner = new RecordingProcessRunner(_ => new PerfMeterRenderDocAnalyzerProcessResult(processStatus, -1, "out", "err", true));

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.Status, Is.EqualTo(expectedStatus));
			Assert.That(execution.ErrorCode, Is.EqualTo(expectedError));
			Assert.That(execution.StandardOutput, Is.EqualTo("out"));
			Assert.That(execution.StandardError, Is.EqualTo("err"));
			Assert.That(execution.LogsTruncated, Is.True);
			Assert.That(Directory.Exists(runner.Start.WorkingDirectory), Is.False);
		}

		[Test]
		public void TerminationFailurePreservesWorkspaceForManualCleanup()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(_ => new PerfMeterRenderDocAnalyzerProcessResult(
				PerfMeterRenderDocAnalyzerProcessStatus.TerminationFailed,
				-1,
				string.Empty,
				string.Empty,
				false));

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.ErrorCode, Is.EqualTo("process_termination_failed"));
			Assert.That(Directory.Exists(runner.Start.WorkingDirectory), Is.True);
		}

		[Test]
		public void CleanupFailureDoesNotMaskTimeout()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(start =>
			{
				File.Delete(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.WorkspaceMarkerFileName));
				return new PerfMeterRenderDocAnalyzerProcessResult(
					PerfMeterRenderDocAnalyzerProcessStatus.TimedOut,
					-1,
					string.Empty,
					string.Empty,
					false);
			});

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.Status, Is.EqualTo(PerfMeterRenderDocAnalyzerExecutionStatus.TimedOut));
			Assert.That(execution.ErrorCode, Is.EqualTo("analysis_timed_out"));
		}

		[Test]
		public void ProcessLogsRedactKnownHostRoots()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(start => new PerfMeterRenderDocAnalyzerProcessResult(
				PerfMeterRenderDocAnalyzerProcessStatus.StartFailed,
				-1,
				_projectRoot + "\\capture " + _packageRoot,
				_renderDocRoot + " " + start.WorkingDirectory,
				false));

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.StandardOutput, Does.Not.Contain(_projectRoot));
			Assert.That(execution.StandardOutput, Does.Not.Contain(_packageRoot));
			Assert.That(execution.StandardError, Does.Not.Contain(_renderDocRoot));
			Assert.That(execution.StandardError, Does.Not.Contain(runner.Start.WorkingDirectory));
			Assert.That(execution.StandardError, Does.Contain("<workspace>"));
		}

		[Test]
		public void TimeoutReportsOnlyValidatedMarkerOwnedStage()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(start =>
			{
				File.WriteAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.StageFileName), "counters");
				return new PerfMeterRenderDocAnalyzerProcessResult(
					PerfMeterRenderDocAnalyzerProcessStatus.TimedOut,
					-1,
					string.Empty,
					string.Empty,
					false);
			});

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.StandardError, Is.EqualTo("stage:counters"));
			Assert.That(Directory.Exists(runner.Start.WorkingDirectory), Is.False);
		}

		[Test]
		public void BoundAnalyzerErrorIsReturnedWithoutTrustingLogs()
		{
			string response = ReadFixture("error-v1.json");
			RecordingProcessRunner runner = new RecordingProcessRunner(start =>
			{
				File.WriteAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.ResponseFileName), response);
				return new PerfMeterRenderDocAnalyzerProcessResult(PerfMeterRenderDocAnalyzerProcessStatus.Completed, 0, "host output", "host error", false);
			});

			PerfMeterRenderDocAnalyzerExecution execution = CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(execution.Status, Is.EqualTo(PerfMeterRenderDocAnalyzerExecutionStatus.Failed));
			Assert.That(execution.ErrorCode, Is.EqualTo("capture_hash_mismatch"));
			Assert.That(execution.Failure, Is.Not.Null);
			Assert.That(execution.StandardOutput, Is.EqualTo("host output"));
			Assert.That(Directory.Exists(runner.Start.WorkingDirectory), Is.False);
		}

		[Test]
		public void StaleOrOversizedResponsesFailClosed()
		{
			string stale = ReadFixture("result-v1.json")
				.Replace("rd-analysis-golden", "rd-analysis-stale")
				.Replace("\"size_bytes\": \"536870912\"", "\"size_bytes\": \"4\"");
			RecordingProcessRunner staleRunner = RunnerWriting(stale);
			PerfMeterRenderDocAnalyzerExecution staleExecution = CreateAnalyzer(staleRunner).Analyze(_renderDocRoot, RequestJson());

			Assert.That(staleExecution.ErrorCode, Is.EqualTo("invalid_response"));

			string smallLimitRequest = RequestJson().Replace("\"max_output_bytes\": 67108864", "\"max_output_bytes\": 1024");
			RecordingProcessRunner oversizedRunner = RunnerWriting(new string('x', 1025));
			PerfMeterRenderDocAnalyzerExecution oversizedExecution = CreateAnalyzer(oversizedRunner).Analyze(_renderDocRoot, smallLimitRequest);
			Assert.That(oversizedExecution.ErrorCode, Is.EqualTo("invalid_response_file"));
		}

		[Test]
		public void MissingExecutableCaptureOrScriptNeverStartsProcess()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(_ => CompletedProcess());
			File.Delete(Path.Combine(_renderDocRoot, PerfMeterRenderDocReplayAnalyzer.RenderDocExecutableName));
			Assert.That(CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson()).ErrorCode, Is.EqualTo("invalid_analyzer_inputs"));
			Assert.That(runner.CallCount, Is.Zero);

			File.WriteAllBytes(Path.Combine(_renderDocRoot, PerfMeterRenderDocReplayAnalyzer.RenderDocExecutableName), new byte[] { 1 });
			File.Delete(_capturePath);
			Assert.That(CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson()).ErrorCode, Is.EqualTo("invalid_analyzer_inputs"));
			Assert.That(runner.CallCount, Is.Zero);

			File.WriteAllBytes(_capturePath, new byte[] { 1, 2, 3, 4 });
			File.Delete(Path.Combine(_packageRoot, PerfMeterRenderDocReplayAnalyzer.AnalyzerScriptRelativePath.Replace('/', Path.DirectorySeparatorChar)));
			Assert.That(CreateAnalyzer(runner).Analyze(_renderDocRoot, RequestJson()).ErrorCode, Is.EqualTo("invalid_analyzer_inputs"));
			Assert.That(runner.CallCount, Is.Zero);
		}

		[Test]
		public void InvalidRequestAndPreCanceledRunNeverCreateWorkspace()
		{
			RecordingProcessRunner runner = new RecordingProcessRunner(_ => CompletedProcess());
			PerfMeterRenderDocReplayAnalyzer analyzer = CreateAnalyzer(runner);

			Assert.That(analyzer.Analyze(_renderDocRoot, "{}").ErrorCode, Is.EqualTo("invalid_request"));
			Assert.That(analyzer.Analyze(_renderDocRoot, RequestJson(), () => true).Status, Is.EqualTo(PerfMeterRenderDocAnalyzerExecutionStatus.Canceled));
			Assert.That(runner.CallCount, Is.Zero);
			Assert.That(Directory.Exists(Path.Combine(_projectRoot, PerfMeterRenderDocReplayAnalyzer.RelativeWorkspaceRoot.Replace('/', Path.DirectorySeparatorChar))), Is.False);
		}

		[Test]
		public void WindowsArgumentQuotingHandlesSpacesQuotesAndTrailingSlashes()
		{
			Assert.That(PerfMeterRenderDocReplayAnalyzer.QuoteWindowsArgument("plain.py"), Is.EqualTo("plain.py"));
			Assert.That(PerfMeterRenderDocReplayAnalyzer.QuoteWindowsArgument("C:\\package root\\analyzer.py"), Is.EqualTo("\"C:\\package root\\analyzer.py\""));
			Assert.That(PerfMeterRenderDocReplayAnalyzer.QuoteWindowsArgument("C:\\package root\\"), Is.EqualTo("\"C:\\package root\\\\\""));
		}

		[Test]
		public void UserOwnedRenderDocReplaysRealCaptureWhenSmokeInputsAreConfigured()
		{
			string renderDocRoot = Environment.GetEnvironmentVariable("SGG_PERFMETER_RDA_SMOKE_RENDERDOC_ROOT");
			string smokeProjectRoot = Environment.GetEnvironmentVariable("SGG_PERFMETER_RDA_SMOKE_PROJECT_ROOT");
			string captureRelativePath = Environment.GetEnvironmentVariable("SGG_PERFMETER_RDA_SMOKE_CAPTURE_PATH");
			string expectedGraphicsApi = Environment.GetEnvironmentVariable("SGG_PERFMETER_RDA_SMOKE_EXPECTED_GRAPHICS_API");
			if (string.IsNullOrEmpty(renderDocRoot) || string.IsNullOrEmpty(smokeProjectRoot) || string.IsNullOrEmpty(captureRelativePath))
			{
				Assert.Ignore("Real RenderDoc analyzer smoke inputs are not configured.");
			}

			string capturePath = Path.GetFullPath(Path.Combine(smokeProjectRoot, captureRelativePath.Replace('/', Path.DirectorySeparatorChar)));
			string hash;
			using (FileStream stream = File.OpenRead(capturePath))
			using (SHA256 sha256 = SHA256.Create())
			{
				hash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
			}
			string packageRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "Scripts", "SGG.PerfMeter"));
			string request = ReadFixture("request-v1.json")
				.Replace("Temp/PerfMeter/RenderDocCopies/golden/capture.rdc", captureRelativePath.Replace('\\', '/'))
				.Replace("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", hash)
				.Replace("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", hash)
				.Replace("\"size_bytes\": \"536870912\"", "\"size_bytes\": \"" + new FileInfo(capturePath).Length + "\"")
				.Replace("\"timeout_seconds\": 300", "\"timeout_seconds\": 60")
				.Replace("\"max_actions\": 100000", "\"max_actions\": 1000000")
				.Replace("\"max_counter_results\": 500000", "\"max_counter_results\": 1000000");

			PerfMeterRenderDocAnalyzerExecution execution = new PerfMeterRenderDocReplayAnalyzer(smokeProjectRoot, packageRoot).Analyze(renderDocRoot, request);

			Assert.That(execution.Status, Is.EqualTo(PerfMeterRenderDocAnalyzerExecutionStatus.Completed),
				execution.ErrorCode + "\n" + execution.StandardError);
			Assert.That(execution.Result.capture.hash_verified, Is.True);
			Assert.That(execution.Result.actions, Is.Not.Empty);
			Assert.That(execution.Result.counter_catalog, Is.Not.Empty);
			Assert.That(execution.Result.summary.fetched_counter_count, Is.GreaterThan(0));
			Assert.That(execution.Result.results, Is.Not.Empty);
			Assert.That(execution.Result.analyzer.renderdoc_build, Is.Not.Empty);
			Assert.That(execution.Result.analyzer.graphics_api, Is.EqualTo(string.IsNullOrEmpty(expectedGraphicsApi) ? "D3D11" : expectedGraphicsApi));
		}

		private PerfMeterRenderDocReplayAnalyzer CreateAnalyzer(IPerfMeterRenderDocAnalyzerProcessRunner runner)
		{
			return new PerfMeterRenderDocReplayAnalyzer(_projectRoot, _packageRoot, runner);
		}

		private RecordingProcessRunner RunnerWriting(string response)
		{
			return new RecordingProcessRunner(start =>
			{
				File.WriteAllText(Path.Combine(start.WorkingDirectory, PerfMeterRenderDocReplayAnalyzer.ResponseFileName), response);
				return CompletedProcess();
			});
		}

		private static PerfMeterRenderDocAnalyzerProcessResult CompletedProcess()
		{
			return new PerfMeterRenderDocAnalyzerProcessResult(PerfMeterRenderDocAnalyzerProcessStatus.Completed, 0, string.Empty, string.Empty, false);
		}

		private static string RequestJson()
		{
			return ReadFixture("request-v1.json").Replace("\"size_bytes\": \"536870912\"", "\"size_bytes\": \"4\"");
		}

		private static string ReadFixture(string fileName)
		{
			return PerfMeterTestAssets.ReadRenderDocAnalyzerAsset(FixtureRoot + fileName);
		}

		private sealed class RecordingProcessRunner : IPerfMeterRenderDocAnalyzerProcessRunner
		{
			private readonly Func<PerfMeterRenderDocAnalyzerProcessStart, PerfMeterRenderDocAnalyzerProcessResult> _run;

			internal RecordingProcessRunner(Func<PerfMeterRenderDocAnalyzerProcessStart, PerfMeterRenderDocAnalyzerProcessResult> run)
			{
				_run = run;
			}

			internal int CallCount { get; private set; }
			internal PerfMeterRenderDocAnalyzerProcessStart Start { get; private set; }

			public PerfMeterRenderDocAnalyzerProcessResult Run(
				PerfMeterRenderDocAnalyzerProcessStart start,
				int timeoutMilliseconds,
				Func<bool> shouldCancel)
			{
				CallCount++;
				Start = start;
				Assert.That(timeoutMilliseconds, Is.EqualTo(300000));
				return _run(start);
			}
		}
	}
}
