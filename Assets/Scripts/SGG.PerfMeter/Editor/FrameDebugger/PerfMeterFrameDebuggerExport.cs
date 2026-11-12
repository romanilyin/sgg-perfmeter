using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	[InitializeOnLoad]
	public static class PerfMeterFrameDebuggerExport
	{
		private static readonly object Owner = new object();
		private static PerfMeterFrameDebuggerExportStatus _status = new PerfMeterFrameDebuggerExportStatus();
		private static PerfMeterFrameDebuggerCapture _capture;
		private static Task<PerfMeterFrameDebuggerWriteResult> _write;
		private static CancellationTokenSource _cancellation;
		private static bool _cancelRequested;
		private static bool _scopeHeld;
		private static string _projectRoot;

		static PerfMeterFrameDebuggerExport()
		{
			AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
			EditorApplication.quitting += Shutdown;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		public static PerfMeterFrameDebuggerExportStatus Status => _status.Copy();

		public static bool CanExport(out string reason)
		{
			if (_status.IsBusy) { reason = "A Frame Debugger export is already running."; return false; }
			if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying)
			{ reason = "Wait for the Editor to finish compiling, importing or changing Play Mode."; return false; }
			IPerfMeterFrameDebuggerBackend backend = new PerfMeterFrameDebuggerBackend();
			return Ready(backend, out _, out reason);
		}

		// Call from the Editor main thread after an explicit user action. Does not enable Frame Debugger or collection.
		public static bool Request(PerfMeterFrameDebuggerExportOptions options = null)
		{
			if (!CanExport(out string reason)) { if (!_status.IsBusy) _status = new PerfMeterFrameDebuggerExportStatus { State = PerfMeterFrameDebuggerExportState.Failed, Message = reason }; return false; }
			try
			{
				PerfMeterFrameDebuggerExportOptions frozen = (options ?? new PerfMeterFrameDebuggerExportOptions()).Freeze();
				IPerfMeterFrameDebuggerBackend backend = new PerfMeterFrameDebuggerBackend();
				if (!Ready(backend, out PerfMeterFrameDebuggerObservation origin, out reason)) throw new InvalidOperationException(reason);
				string id = Guid.NewGuid().ToString("N");
				if (!PerfMeterEditorDiagnosticScope.TryBegin(Owner, id, out reason)) throw new InvalidOperationException("Profiling conflict: " + reason);
				_scopeHeld = true;
				_projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				PerfMeterFrameDebuggerSnapshot snapshot = new PerfMeterFrameDebuggerSnapshot
				{
					ExportId = id, StartedUtc = DateTime.UtcNow.ToString("O"), UnityVersion = Application.unityVersion,
					RenderPipeline = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().FullName : "BuiltIn",
					GraphicsApi = SystemInfo.graphicsDeviceType.ToString(), SceneName = SceneManager.GetActiveScene().name,
					SessionId = PerformanceMeter.GetSessionSummary().SessionId
				};
				_capture = new PerfMeterFrameDebuggerCapture(backend, frozen, origin, snapshot, EditorApplication.timeSinceStartup);
				_status = new PerfMeterFrameDebuggerExportStatus { State = PerfMeterFrameDebuggerExportState.Capturing, ExportId = id,
					RequestedEvents = snapshot.RequestedEventCount, Message = "Reading local Frame Debugger events…" };
				_cancelRequested = false;
				EditorApplication.update -= Update;
				EditorApplication.update += Update;
				return true;
			}
			catch (Exception exception)
			{
				EndScope(); _capture = null;
				_status = new PerfMeterFrameDebuggerExportStatus { State = PerfMeterFrameDebuggerExportState.Failed, Message = exception.Message };
				return false;
			}
		}

		public static void Cancel()
		{
			if (!_status.IsBusy) return;
			_cancelRequested = true;
			_cancellation?.Cancel();
			if (_capture != null && !_capture.Finished) _capture.Finish("cancelled");
			EndScope();
			_status.Message = "Cancellation requested; waiting for owned work to finish.";
		}

		private static bool Ready(IPerfMeterFrameDebuggerBackend backend, out PerfMeterFrameDebuggerObservation observation, out string reason)
		{
			if (!backend.TryObserve(out observation, out reason)) return false;
			if (!observation.Enabled || !observation.Local) { reason = "Enable Frame Debugger for the local Editor target first."; return false; }
			if (observation.EventCount <= 0) { reason = "The current Frame Debugger capture contains no events."; return false; }
			return true;
		}

		private static void Update()
		{
			try
			{
				if (_capture != null && _write == null)
				{
					if (!_capture.Finished && !PerfMeterEditorDiagnosticScope.IsHeld(Owner)) _capture.Finish("profiling_lease_lost");
					_capture.Tick(EditorApplication.timeSinceStartup);
					_status.ProcessedEvents = _capture.Processed;
					if (!_capture.Finished) return;
					EndScope();
					_capture.Snapshot.DiagnosticIntervalEndedUtc = DateTime.UtcNow.ToString("O");
					if (_cancelRequested) { Complete(PerfMeterFrameDebuggerExportState.Cancelled, "Export cancelled; no artifact published."); return; }
					PerfMeterFrameDebuggerSnapshot detached = _capture.Snapshot;
					_cancellation = new CancellationTokenSource();
					_cancellation.CancelAfter(TimeSpan.FromSeconds(30));
					CancellationToken token = _cancellation.Token;
					string project = _projectRoot;
					_write = Task.Run(() => PerfMeterFrameDebuggerWriter.Write(project, detached, token));
					_status.State = PerfMeterFrameDebuggerExportState.Writing;
					_status.Message = "Writing JSON/CSV and verifying artifact hashes…";
				}
				if (_write == null || !_write.IsCompleted) return;
				if (_write.IsCanceled || _write.IsFaulted && _write.Exception.GetBaseException() is OperationCanceledException)
				{ Complete(PerfMeterFrameDebuggerExportState.Cancelled, "Export cancelled or exceeded its writing deadline; no successful artifact reported."); return; }
				if (_write.IsFaulted) { Complete(PerfMeterFrameDebuggerExportState.Failed, _write.Exception.GetBaseException().Message); return; }
				PerfMeterFrameDebuggerWriteResult result = _write.Result;
				_status.OutputDirectory = result.OutputDirectory;
				bool partial = _capture.Snapshot.Completeness != "complete";
				Complete(partial ? PerfMeterFrameDebuggerExportState.Partial : PerfMeterFrameDebuggerExportState.Completed,
					partial ? "Partial export saved: " + _capture.Snapshot.Warning : "Export saved. Selection restore: " + _capture.Snapshot.RestorationReason);
			}
			catch (Exception exception) { _capture?.Finish("service_error"); EndScope(); Complete(PerfMeterFrameDebuggerExportState.Failed, exception.Message); }
		}

		private static void Complete(PerfMeterFrameDebuggerExportState state, string message)
		{
			_status.State = state; _status.Message = message;
			EditorApplication.update -= Update;
			_cancellation?.Dispose(); _cancellation = null; _write = null; _capture = null;
		}

		private static void EndScope()
		{
			if (!_scopeHeld) return;
			try { PerfMeterEditorDiagnosticScope.End(Owner); }
			finally { _scopeHeld = false; }
		}

		private static void OnPlayModeChanged(PlayModeStateChange state) { if (_status.IsBusy) Cancel(); }
		private static void Shutdown()
		{
			Cancel(); EndScope(); EditorApplication.update -= Update;
		}
	}
}