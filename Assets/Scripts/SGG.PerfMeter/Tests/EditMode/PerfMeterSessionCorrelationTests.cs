using NUnit.Framework;
using SGG.PerfMeter.Editor;
using SGG.PerfMeter.Editor.Mcp;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSessionCorrelationTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterProfilerInstrumentation.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterProfilerInstrumentation.Reset();
		}

		[Test]
		public void SessionIdLifecycleIsStableThroughStopAndUniqueOnRestart()
		{
			PerfMeterSessionRecorder recorder = CreateRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 4), default, default, PerfMeterSettingsStore.Defaults, 1, 1d, PerfMeterMetricsSnapshot.Stopped);
			string firstSessionId = recorder.GetSummary().SessionId;

			AssertValidSessionId(firstSessionId);
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 4), default, default, PerfMeterSettingsStore.Defaults, 2, 2d, PerfMeterMetricsSnapshot.Stopped);
			string secondSessionId = recorder.GetSummary().SessionId;

			AssertValidSessionId(secondSessionId);
			Assert.That(secondSessionId, Is.Not.EqualTo(firstSessionId));
			recorder.Stop(3d);
			Assert.That(recorder.GetSummary().SessionId, Is.EqualTo(secondSessionId));

			recorder.Reset();
			Assert.That(recorder.GetSummary().SessionId, Is.Empty);
			Assert.That(PerfMeterSessionSummarySnapshot.Empty.SessionId, Is.Empty);
		}

		[Test]
		public void SessionBoundaryMarkerNamesAndEditorSearchPrefixArePure()
		{
			const string sessionId = "0123456789abcdef0123456789abcdef";

			Assert.That(PerfMeterProfilerInstrumentation.SessionMarkerPrefix, Is.EqualTo("SGG.PerfMeter.Session."));
			Assert.That(PerfMeterProfilerInstrumentation.SessionBeginMarkerSuffix, Is.EqualTo(".Begin"));
			Assert.That(PerfMeterProfilerInstrumentation.SessionEndMarkerSuffix, Is.EqualTo(".End"));
			Assert.That(PerfMeterProfilerInstrumentation.GetSessionBoundaryMarkerName(sessionId, true), Is.EqualTo("SGG.PerfMeter.Session." + sessionId + ".Begin"));
			Assert.That(PerfMeterProfilerInstrumentation.GetSessionBoundaryMarkerName(sessionId, false), Is.EqualTo("SGG.PerfMeter.Session." + sessionId + ".End"));
			Assert.That(PerfMeterProfilerInstrumentation.GetSessionBoundaryMarkerName(string.Empty, true), Is.Empty);
			Assert.That(PerfMeterProfilerInstrumentation.GetSessionBoundaryMarkerName(string.Empty, false), Is.Empty);
			Assert.That(PerfMeterProfileAnalyzerIntegration.GetSessionMarkerPrefix(sessionId), Is.EqualTo("SGG.PerfMeter.Session." + sessionId + "."));
			Assert.That(PerfMeterProfileAnalyzerIntegration.GetSessionMarkerPrefix(string.Empty), Is.Empty);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void ExistingSummaryConstructorsRemainEmptyCompatible()
		{
			PerfMeterSessionOptions options = PerfMeterSessionOptions.Default;
			PerfMeterSettingsSnapshot settings = PerfMeterSettingsStore.Defaults;
			PerfMeterSessionScopeSummarySnapshot scope = PerfMeterSessionScopeSummarySnapshot.Empty;
			PerfMeterSessionSummarySnapshot legacySettingsConstructor = new PerfMeterSessionSummarySnapshot(
				PerfMeterSessionState.Stopped,
				options,
				0,
				0,
				-1,
				-1,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				string.Empty,
				default,
				default,
				settings,
				string.Empty,
				string.Empty,
				scope,
				scope);
			PerfMeterSessionSummarySnapshot legacyCompleteConstructor = new PerfMeterSessionSummarySnapshot(
				PerfMeterSessionState.Stopped,
				options,
				0,
				0,
				-1,
				-1,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				string.Empty,
				default,
				default,
				settings,
				settings,
				string.Empty,
				string.Empty,
				scope,
				scope);

			Assert.That(legacySettingsConstructor.SessionId, Is.Empty);
			Assert.That(legacyCompleteConstructor.SessionId, Is.Empty);
		}

		[Test]
		public void SessionIdIsAddedToJsonCsvAndMcpSummaryWithoutChangingSchemaVersion()
		{
			PerfMeterSessionRecorder recorder = CreateRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 4), default, default, PerfMeterSettingsStore.Defaults, 1, 1d, PerfMeterMetricsSnapshot.Stopped);
			recorder.Update(PerfMeterMetricsSnapshot.Stopped, 2, 1.01d);
			recorder.Stop(1.02d);

			PerfMeterSessionSummarySnapshot summary = recorder.GetSummary();
			string json = PerfMeterSessionExporter.BuildJson(summary, recorder.GetSamplesCopy(), PerformanceMeter.GetStatus());
			string csv = PerfMeterSessionExporter.BuildCsv(summary, recorder.GetSamplesCopy(), PerformanceMeter.GetStatus());

			Assert.That(json, Does.StartWith("{\"schema_version\":2,\"session_id\":\"" + summary.SessionId + "\""));
			Assert.That(csv, Does.StartWith("frame,time_seconds,scene,bottleneck"));
			Assert.That(csv.Split('\n')[0].TrimEnd('\r'), Does.EndWith(",session_id"));
			Assert.That(csv, Does.Contain(",\"" + summary.SessionId + "\""));

			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0.01f, 2));
			string mcp = PerfMeterMcpCommands.SessionSummary();
			Assert.That(mcp, Does.Contain("\"session_id\":\"" + PerformanceMeter.GetSessionSummary().SessionId + "\""));
		}

		[Test]
		public void PureEditorIntegrationHelperDoesNotStartRuntime()
		{
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			Assert.That(PerfMeterProfileAnalyzerIntegration.GetSessionMarkerPrefix("session"), Is.EqualTo("SGG.PerfMeter.Session.session."));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		private static PerfMeterSessionRecorder CreateRecorder()
		{
			return new PerfMeterSessionRecorder();
		}

		private static void AssertValidSessionId(string sessionId)
		{
			Assert.That(sessionId, Is.Not.Null);
			Assert.That(sessionId, Has.Length.EqualTo(32));
			for (int index = 0; index < sessionId.Length; index++)
			{
				char character = sessionId[index];
				bool isDigit = character >= '0' && character <= '9';
				bool isLowercaseHex = character >= 'a' && character <= 'f';
				Assert.That(isDigit || isLowercaseHex, Is.True, "SessionId must be lowercase hexadecimal.");
			}
		}
	}
}
