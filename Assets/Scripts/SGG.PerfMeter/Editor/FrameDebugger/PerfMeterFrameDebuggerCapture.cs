using System;
using System.Collections.Generic;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	// Pumped once per Editor update. Captures detached values; never performs filesystem I/O.
	internal sealed class PerfMeterFrameDebuggerCapture
	{
		private readonly IPerfMeterFrameDebuggerBackend _backend;
		private readonly PerfMeterFrameDebuggerExportOptions _options;
		private readonly PerfMeterFrameDebuggerObservation _origin;
		private readonly PerfMeterFrameDebuggerSnapshot _snapshot;
		private readonly List<PerfMeterFrameDebuggerEvent> _events = new List<PerfMeterFrameDebuggerEvent>();
		private readonly double _start;
		private double _eventStart;
		private int _expectedLimit;
		private int _characters;
		private bool _waiting;
		private bool _attemptedSelection;
		internal bool Finished { get; private set; }
		internal int Processed => _events.Count;
		internal PerfMeterFrameDebuggerSnapshot Snapshot => _snapshot;

		internal PerfMeterFrameDebuggerCapture(IPerfMeterFrameDebuggerBackend backend, PerfMeterFrameDebuggerExportOptions options,
			PerfMeterFrameDebuggerObservation origin, PerfMeterFrameDebuggerSnapshot snapshot, double now)
		{
			_backend = backend; _options = options.Freeze(); _origin = origin; _snapshot = snapshot; _start = now;
			_expectedLimit = origin.SelectedLimit;
			_snapshot.OriginalLimit = origin.SelectedLimit;
			_snapshot.TotalEventCount = origin.EventCount;
			_snapshot.RequestedEventCount = Math.Min(origin.EventCount, _options.MaxEvents);
			_snapshot.TruncatedEventCount = Math.Max(0, origin.EventCount - _snapshot.RequestedEventCount);
			_snapshot.IncludeProperties = options.IncludeProperties;
		}

		internal void Tick(double now)
		{
			if (Finished) return;
			try
			{
				if (!Validate(out string failure)) { Finish(failure); return; }
				if (now - _start >= _options.TotalTimeoutSeconds) { Finish("total_timeout"); return; }
				if (_events.Count >= _snapshot.RequestedEventCount) { Finish(string.Empty); return; }
				if (!_waiting)
				{
					_expectedLimit = _events.Count + 1;
					_attemptedSelection = true;
					if (!_backend.TrySelect(_expectedLimit, out failure)) { Finish("selection_failed: " + failure); return; }
					_eventStart = now; _waiting = true;
					return;
				}
				bool ready = _backend.TryRead(_events.Count, _options, out PerfMeterFrameDebuggerEvent row, out string reason);
				if (!Validate(out failure)) { Finish(failure); return; }
				if (!ready && now - _eventStart < _options.EventTimeoutSeconds) return;
				if (!ready) row = new PerfMeterFrameDebuggerEvent { Index = _events.Count, Status = "unavailable", Reason = "event_timeout: " + reason };
				if (row == null || row.Index != _events.Count) { Finish("wrong_event_index"); return; }
				int characters = CountCharacters(row);
				if (characters > PerfMeterFrameDebuggerExportOptions.MaxCapturedCharacters - _characters) { Finish("snapshot_character_limit"); return; }
				_characters += characters;
				_events.Add(row);
				if (row.Status == "unavailable") _snapshot.MissingEventCount++;
				else _snapshot.ExportedEventCount++;
				if (row.Truncated) _snapshot.TruncatedEventCount++;
				_waiting = false;
			}
			catch (Exception exception) { Finish("capture_error: " + exception.GetType().Name); }
		}

		internal void Finish(string reason)
		{
			if (Finished) return;
			Finished = true;
			_snapshot.Warning = reason ?? string.Empty;
			_snapshot.CompletedUtc = DateTime.UtcNow.ToString("O");
			_snapshot.MissingEventCount += _snapshot.RequestedEventCount - _events.Count;
			_snapshot.Events = _events.ToArray();
			_snapshot.Completeness = string.IsNullOrEmpty(reason) && _snapshot.MissingEventCount == 0 && _snapshot.TruncatedEventCount == 0 ? "complete" : "partial";
			Restore();
		}

		private bool Validate(out string reason)
		{
			if (!_backend.TryObserve(out PerfMeterFrameDebuggerObservation current, out reason)) return false;
			if (!_origin.SameCapture(current)) { reason = "capture_or_target_changed"; return false; }
			if (current.SelectedLimit != _expectedLimit) { reason = "selection_changed"; return false; }
			return true;
		}

		private void Restore()
		{
			try
			{
				if (!_attemptedSelection) { _snapshot.SelectionRestored = true; _snapshot.RestorationReason = "unchanged"; return; }
				if (!Validate(out string reason)) { _snapshot.RestorationReason = "preserved_external_state: " + reason; return; }
				if (!_backend.TrySelect(_origin.SelectedLimit, out reason)) { _snapshot.RestorationReason = "restore_failed: " + reason; return; }
				_snapshot.SelectionRestored = _backend.TryObserve(out PerfMeterFrameDebuggerObservation restored, out reason) &&
					_origin.SameCapture(restored) && restored.SelectedLimit == _origin.SelectedLimit;
				_snapshot.RestorationReason = _snapshot.SelectionRestored ? "restored" : "restore_unverified: " + reason;
			}
			catch (Exception exception) { _snapshot.RestorationReason = "restore_error: " + exception.GetType().Name; }
		}

		private static int CountCharacters(PerfMeterFrameDebuggerEvent row)
		{
			long total = (row.Name?.Length ?? 0) + (row.Reason?.Length ?? 0) + (row.EventType?.Length ?? 0);
			foreach (PerfMeterFrameDebuggerField field in row.Fields ?? Array.Empty<PerfMeterFrameDebuggerField>())
			{
				total += (field.Name?.Length ?? 0) + (field.Kind?.Length ?? 0) + (field.Reason?.Length ?? 0);
				if (field.Value is string text) total += text.Length;
				if (field.Value is string[] strings) foreach (string value in strings) total += value?.Length ?? 0;
			}
			return (int)Math.Min(int.MaxValue, total);
		}
	}
}