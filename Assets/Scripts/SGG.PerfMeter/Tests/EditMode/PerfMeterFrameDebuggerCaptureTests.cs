using NUnit.Framework;
using SGG.PerfMeter.Editor.FrameDebugger;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFrameDebuggerCaptureTests
	{
		[Test]
		public void ReadyEventRestoresOriginalSelectionAndCompletes()
		{
			Backend backend = new Backend();
			PerfMeterFrameDebuggerCapture capture = Create(backend);
			capture.Tick(0); capture.Tick(0.1); capture.Tick(0.2);
			Assert.That(capture.Finished, Is.True);
			Assert.That(capture.Snapshot.Completeness, Is.EqualTo("complete"));
			Assert.That(capture.Snapshot.Events.Length, Is.EqualTo(1));
			Assert.That(capture.Snapshot.SelectionRestored, Is.True);
			Assert.That(backend.Limit, Is.EqualTo(1));
		}

		[Test]
		public void ManualSelectionIsPreservedInsteadOfRestored()
		{
			Backend backend = new Backend { Count = 3, Limit = 3 };
			PerfMeterFrameDebuggerCapture capture = Create(backend);
			capture.Tick(0); backend.Limit = 2; capture.Tick(0.1);
			Assert.That(capture.Finished, Is.True);
			Assert.That(backend.Limit, Is.EqualTo(2));
			Assert.That(capture.Snapshot.SelectionRestored, Is.False);
			Assert.That(capture.Snapshot.Warning, Is.EqualTo("selection_changed"));
		}

		[Test]
		public void PendingDataTimesOutAsMissingWithoutAcceptingStaleRow()
		{
			Backend backend = new Backend { Ready = false };
			PerfMeterFrameDebuggerCapture capture = Create(backend);
			capture.Tick(0); capture.Tick(0.1);
			Assert.That(capture.Processed, Is.Zero);
			capture.Tick(0.6); capture.Tick(0.7);
			Assert.That(capture.Snapshot.MissingEventCount, Is.EqualTo(1));
			Assert.That(capture.Snapshot.ExportedEventCount, Is.Zero);
			Assert.That(capture.Snapshot.Completeness, Is.EqualTo("partial"));
			Assert.That(capture.Snapshot.Events[0].Status, Is.EqualTo("unavailable"));
		}

		[Test]
		public void CaptureChangeDoesNotRestoreSelectionIntoNewCapture()
		{
			Backend backend = new Backend();
			PerfMeterFrameDebuggerCapture capture = Create(backend);
			capture.Tick(0); backend.Hash++; capture.Tick(0.1);
			Assert.That(capture.Finished, Is.True);
			Assert.That(capture.Snapshot.SelectionRestored, Is.False);
			Assert.That(backend.Limit, Is.EqualTo(1));
		}

		[Test]
		public void ExplicitCancelRestoresOwnedSelection()
		{
			Backend backend = new Backend { Count = 2, Limit = 2 };
			PerfMeterFrameDebuggerCapture capture = Create(backend);
			capture.Tick(0); capture.Finish("cancelled");
			Assert.That(capture.Snapshot.SelectionRestored, Is.True);
			Assert.That(backend.Limit, Is.EqualTo(2));
			Assert.That(capture.Snapshot.MissingEventCount, Is.EqualTo(2));
		}

		private static PerfMeterFrameDebuggerCapture Create(Backend backend)
		{
			backend.TryObserve(out PerfMeterFrameDebuggerObservation initial, out _);
			return new PerfMeterFrameDebuggerCapture(backend, new PerfMeterFrameDebuggerExportOptions(), initial,
				new PerfMeterFrameDebuggerSnapshot(), 0);
		}

		private sealed class Backend : IPerfMeterFrameDebuggerBackend
		{
			internal int Limit = 1;
			internal int Count = 1;
			internal int Hash = 123;
			internal bool Ready = true;
			public bool TryObserve(out PerfMeterFrameDebuggerObservation observation, out string reason)
			{ observation = new PerfMeterFrameDebuggerObservation(true, true, 1, 0, Count, Limit, Hash); reason = string.Empty; return true; }
			public bool TrySelect(int limit, out string reason) { Limit = limit; reason = string.Empty; return true; }
			public bool TryRead(int index, PerfMeterFrameDebuggerExportOptions options, out PerfMeterFrameDebuggerEvent row, out string reason)
			{ row = new PerfMeterFrameDebuggerEvent { Index = index, Name = "draw" }; reason = "previous_event_data"; return Ready; }
		}
	}
}