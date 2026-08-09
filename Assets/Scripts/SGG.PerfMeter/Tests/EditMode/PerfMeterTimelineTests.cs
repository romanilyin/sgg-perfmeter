using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterTimelineTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[Test]
		public void TimelineEnumsKeepStableNumericBaselines()
		{
			Assert.That((int)PerfMeterSessionTimelineKind.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterSessionTimelineKind.Valid, Is.EqualTo(1));
			Assert.That((int)PerfMeterSessionTimelineKind.Missing, Is.EqualTo(2));
			Assert.That((int)PerfMeterSessionTimelineKind.CaptureBoundary, Is.EqualTo(3));
			Assert.That((int)PerfMeterSessionTimelineStream.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterSessionTimelineTimingState.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterSessionTimelineCaptureBoundary.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterSessionTimelineCapturePhase.Unknown, Is.EqualTo(0));
		}

		[Test]
		public void PublicSessionTimelineQueryIsSafeWithoutRuntime()
		{
			PerfMeterSessionTimelineSnapshot timeline = PerformanceMeter.GetSessionTimeline();

			Assert.That(timeline.Events, Is.Empty);
			Assert.That(timeline.EventCount, Is.Zero);
			Assert.That(timeline.DroppedEventCount, Is.Zero);
			Assert.That(timeline.IsComplete, Is.True);
		}

		[Test]
		public void UnbundledCaptureMissingCollectionRemainsBaselineTimeline()
		{
			PerfMeterRuntime.ResetCaptureBundlesForTests();
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.StartSession(new PerfMeterSessionOptions(0, 0f, 0.01f, 8, false, 0, 0f));
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
			System.Reflection.MethodInfo recordMissing = typeof(PerfMeterRuntime).GetMethod("RecordMissingTimeline", flags);

			recordMissing.Invoke(runtime, new object[]
			{
				7,
				1d,
				PerfMeterSessionTimelineReasonFlags.InvalidTiming,
				CreateCaptureStatus(PerfMeterCaptureState.Capturing, 0)
			});

			PerfMeterSessionTimelineSnapshot timeline = PerformanceMeter.GetSessionTimeline();
			Assert.That(timeline.Events, Has.Length.EqualTo(1));
			Assert.That(timeline.Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Missing));
			Assert.That(timeline.Events[0].Stream, Is.EqualTo(PerfMeterSessionTimelineStream.Baseline));
			Assert.That(timeline.Events[0].BundleId, Is.Empty);
		}

		[Test]
		public void SessionTimelineRetainsValidSamplesAndCoalescesMissingRanges()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 0.01f, 8, false, 0, 0f), default, default, PerfMeterSettingsStore.Defaults, 1, 1d, PerfMeterMetricsSnapshot.Stopped);
			recorder.Update(PerfMeterMetricsSnapshot.Stopped, 1, 1.01d);
			recorder.RecordMissingCollection(2, 1.02d, PerfMeterSessionTimelineReasonFlags.InvalidTiming, PerfMeterCaptureStatusSnapshot.NotRunning, string.Empty);
			recorder.RecordMissingCollection(3, 1.03d, PerfMeterSessionTimelineReasonFlags.InvalidTiming, PerfMeterCaptureStatusSnapshot.NotRunning, string.Empty);
			recorder.Update(PerfMeterMetricsSnapshot.Stopped, 4, 1.04d);

			PerfMeterSessionTimelineSnapshot timeline = recorder.GetTimelineCopy();
			Assert.That(timeline.IsComplete, Is.True);
			Assert.That(timeline.DroppedEventCount, Is.Zero);
			Assert.That(timeline.Events, Has.Length.EqualTo(3));
			Assert.That(timeline.Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Valid));
			Assert.That(timeline.Events[0].Stream, Is.EqualTo(PerfMeterSessionTimelineStream.Baseline));
			Assert.That(timeline.Events[0].SampleIndex, Is.EqualTo(0));
			Assert.That(timeline.Events[1].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Missing));
			Assert.That(timeline.Events[1].FirstFrame, Is.EqualTo(2));
			Assert.That(timeline.Events[1].LastFrame, Is.EqualTo(3));
			Assert.That(timeline.Events[1].FrameCount, Is.EqualTo(2));
			Assert.That(timeline.Events[1].TimingState, Is.EqualTo(PerfMeterSessionTimelineTimingState.Missing));
			Assert.That(timeline.Events[2].SampleIndex, Is.EqualTo(1));
		}

		[Test]
		public void TimelineStorageIsBoundedAndReportsDroppedEventsDefensively()
		{
			PerfMeterSessionTimelineStore store = new PerfMeterSessionTimelineStore();
			store.Start(0, 0);
			for (int frame = 0; frame < 9; frame++)
			{
				store.AddValidBaseline(frame, frame + 1d, frame);
			}

			PerfMeterSessionTimelineSnapshot timeline = store.GetSnapshotCopy();
			Assert.That(timeline.Events, Has.Length.EqualTo(8));
			Assert.That(timeline.DroppedEventCount, Is.EqualTo(1));
			Assert.That(timeline.IsComplete, Is.False);

			timeline.Events[0] = default;
			Assert.That(store.GetSnapshotCopy().Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Valid));
		}

		[Test]
		public void CaptureTimelineUsesCaptureStreamWithoutBaselineDualWrite()
		{
			PerfMeterSessionRecorder recorder = new PerfMeterSessionRecorder();
			recorder.Start(new PerfMeterSessionOptions(0, 0.01f, 0.01f, 8, false, 0, 0f), default, default, PerfMeterSettingsStore.Defaults, 1, 1d, PerfMeterMetricsSnapshot.Stopped);
			PerfMeterCaptureStatusSnapshot capturing = CreateCaptureStatus(PerfMeterCaptureState.Capturing, 0);
			PerfMeterCaptureStatusSnapshot completedFrame = CreateCaptureStatus(PerfMeterCaptureState.Capturing, 1);
			PerfMeterCaptureStatusSnapshot completed = CreateCaptureStatus(PerfMeterCaptureState.Completed, 2);

			recorder.RecordCaptureBoundary(1, 1.01d, capturing, "bundle-1", PerfMeterSessionTimelineCaptureBoundary.Begin);
			recorder.RecordValidCapture(2, 1.02d, capturing, "bundle-1", 0);
			recorder.RecordMissingCollection(3, 1.03d, PerfMeterSessionTimelineReasonFlags.InvalidTiming, completedFrame, "bundle-1");
			recorder.RecordCaptureBoundary(4, 1.04d, completed, "bundle-1", PerfMeterSessionTimelineCaptureBoundary.End);

			PerfMeterSessionTimelineSnapshot timeline = recorder.GetTimelineCopy();
			Assert.That(timeline.Events, Has.Length.EqualTo(4));
			Assert.That(recorder.GetSamplesCopy(), Is.Empty);
			Assert.That(timeline.Events, Has.All.Property(nameof(PerfMeterSessionTimelineEventSnapshot.Stream)).EqualTo(PerfMeterSessionTimelineStream.Capture));
			Assert.That(timeline.Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.CaptureBoundary));
			Assert.That(timeline.Events[1].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Valid));
			Assert.That(timeline.Events[1].CaptureFrameOrdinal, Is.EqualTo(1));
			Assert.That(timeline.Events[1].RequestedCaptureFrameCount, Is.EqualTo(2));
			Assert.That(timeline.Events[1].CaptureSampleIndex, Is.EqualTo(0));
			Assert.That(timeline.Events[1].CapturePhase, Is.EqualTo(PerfMeterSessionTimelineCapturePhase.Capturing));
			Assert.That(timeline.Events[2].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Missing));
			Assert.That(timeline.Events[2].CaptureFrameOrdinal, Is.EqualTo(2));
			Assert.That(timeline.Events[2].TimingState, Is.EqualTo(PerfMeterSessionTimelineTimingState.Missing));
			Assert.That(timeline.Events[3].CaptureBoundary, Is.EqualTo(PerfMeterSessionTimelineCaptureBoundary.End));
		}

		[Test]
		public void TimelineJsonNullsInvalidTimingAndLeavesCsvUnchanged()
		{
			PerfMeterSessionTimelineStore store = new PerfMeterSessionTimelineStore();
			store.Start(2, 0);
			store.AddMissingBaseline(7, 8, 0d, 0d, PerfMeterSessionTimelineReasonFlags.InvalidTiming);
			PerfMeterSessionTimelineSnapshot timeline = store.GetSnapshotCopy();

			string json = PerfMeterSessionExporter.BuildJson(
				PerfMeterSessionSummarySnapshot.Empty,
				System.Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus(),
				PerfMeterSessionExporter.RuntimePackageIdentity,
				timeline);
			string csv = PerfMeterSessionExporter.BuildCsv(
				PerfMeterSessionSummarySnapshot.Empty,
				System.Array.Empty<PerfMeterSessionSampleSnapshot>(),
				PerformanceMeter.GetStatus());

			Assert.That(json, Does.Contain("\"timeline_schema_version\":1"));
			Assert.That(json, Does.Contain("\"timeline_event_count\":1"));
			Assert.That(json, Does.Contain("\"first_time_seconds\":null"));
			Assert.That(json, Does.Contain("\"last_time_seconds\":null"));
			Assert.That(json, Does.Not.Contain("\"first_time_seconds\":0"));
			Assert.That(json, Does.Contain("\"measurement_provenance\""));
			Assert.That(json, Does.Not.Contain("RenderDoc"));
			Assert.That(csv, Does.StartWith("frame,time_seconds,scene,bottleneck"));
			Assert.That(csv, Does.Not.Contain("timeline_schema_version"));
		}

		[Test]
		public void CompatibilityReaderAcceptsLegacyAndMapsUnknownValues()
		{
			PerfMeterTimelineCompatibilitySnapshot legacy = PerfMeterTimelineCompatibilityReader.Read("{\"schema_version\":2,\"samples\":[]}");
			PerfMeterTimelineCompatibilitySnapshot manifest = PerfMeterTimelineCompatibilityReader.Read("{\"schema\":\"sgg.perfmeter.capture-bundle\",\"schema_version\":1,\"files\":[]}");
			PerfMeterTimelineCompatibilitySnapshot current = PerfMeterTimelineCompatibilityReader.Read(
				"{\"timeline_schema_version\":1,\"timeline_complete\":false,\"timeline_dropped_event_count\":2,\"future_field\":true,\"timeline\":[{\"kind\":\"FutureKind\",\"stream\":\"FutureStream\",\"reason_flags\":\"InvalidTiming, FutureReason\",\"timing_state\":\"FutureTiming\",\"capture_boundary\":\"FutureBoundary\",\"capture_phase\":\"FuturePhase\",\"first_frame\":1,\"last_frame\":1,\"frame_count\":1,\"measurement_provenance\":{\"measurement_kind\":\"generic\",\"collectors\":\"collector-a\"}}]}");
			PerfMeterTimelineCompatibilitySnapshot future = PerfMeterTimelineCompatibilityReader.Read(
				"{\"timeline_schema_version\":99,\"timeline\":[{\"kind\":\"Missing\",\"stream\":\"Baseline\",\"first_frame\":1,\"last_frame\":1,\"frame_count\":1}]}" );

			Assert.That(legacy.Status, Is.EqualTo(PerfMeterTimelineCompatibilityStatus.LegacyUnknown));
			Assert.That(legacy.IsAccepted, Is.True);
			Assert.That(legacy.Timeline.Events, Is.Empty);
			Assert.That(manifest.Status, Is.EqualTo(PerfMeterTimelineCompatibilityStatus.LegacyUnknown));
			Assert.That(manifest.IsAccepted, Is.True);
			Assert.That(future.Status, Is.EqualTo(PerfMeterTimelineCompatibilityStatus.Unknown));
			Assert.That(future.Timeline.Events, Is.Empty);
			Assert.That(current.Status, Is.EqualTo(PerfMeterTimelineCompatibilityStatus.Current));
			Assert.That(current.IsAccepted, Is.True);
			Assert.That(current.UnknownEnumCount, Is.GreaterThanOrEqualTo(5));
			Assert.That(current.Timeline.Events, Has.Length.EqualTo(1));
			Assert.That(current.Timeline.DroppedEventCount, Is.EqualTo(2));
			Assert.That(current.Timeline.IsComplete, Is.False);
			Assert.That(current.Timeline.Events[0].Kind, Is.EqualTo(PerfMeterSessionTimelineKind.Unknown));
			Assert.That(current.Timeline.Events[0].Stream, Is.EqualTo(PerfMeterSessionTimelineStream.Unknown));
			Assert.That(current.Timeline.Events[0].TimingState, Is.EqualTo(PerfMeterSessionTimelineTimingState.Unknown));
			Assert.That(current.Timeline.Events[0].Reason & PerfMeterSessionTimelineReasonFlags.InvalidTiming, Is.EqualTo(PerfMeterSessionTimelineReasonFlags.InvalidTiming));
			Assert.That(current.Timeline.Events[0].Reason & PerfMeterSessionTimelineReasonFlags.Unknown, Is.EqualTo(PerfMeterSessionTimelineReasonFlags.Unknown));
			Assert.That(current.Timeline.Events[0].Provenance.MeasurementKind, Is.EqualTo("generic"));
			Assert.That(current.Timeline.Events[0].Provenance.Collectors, Is.EqualTo("collector-a"));
		}

		private static PerfMeterCaptureStatusSnapshot CreateCaptureStatus(PerfMeterCaptureState state, int completedCaptureFrames)
		{
			return new PerfMeterCaptureStatusSnapshot(
				PerfMeterAvailability.Available,
				state,
				"capture-1",
				PerfMeterCaptureTool.Unknown,
				0,
				2,
				0,
				0,
				completedCaptureFrames,
				0,
				string.Empty);
		}
	}
}
