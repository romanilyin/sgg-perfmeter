using System;

namespace SGG.PerfMeter
{
	// Editor-only callers use the same runtime lease resources without starting collection.
	internal static class PerfMeterEditorDiagnosticScope
	{
		private static object _owner;
		private static string _leaseId;
		private static int _generation;
		private static PerfMeterProfilerLeaseCoordinator _coordinator;
		internal static bool IsActive => _owner != null;

		internal static bool TryBegin(object owner, string id, out string reason)
		{
			reason = string.Empty;
			if (owner == null || IsActive) { reason = "diagnostic_operation_busy"; return false; }
			if (!PerfMeterRuntime.CanBeginEditorDiagnostic()) { reason = "runtime_cleanup_pending"; return false; }
			PerfMeterProfilerLeaseCoordinator coordinator = PerfMeterRuntime.Instance != null
				? PerfMeterRuntime.Instance.EditorDiagnosticLeaseCoordinator : new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions request = new PerfMeterProfilerLeaseRequestOptions(id, "perfmeter.frame-debugger",
				"frame-debugger", PerfMeterProfilerLeaseResourceKeys.ActiveGpu, PerfMeterProfilerLeaseResourceKeys.ExclusiveProfilingOperation,
				PerfMeterProfilerLeaseResource.Gpu | PerfMeterProfilerLeaseResource.Operation);
			PerfMeterProfilerLeaseAcquireResult result = coordinator.TryAcquireOwned(request, owner, out PerfMeterProfilerLeaseStatusSnapshot lease);
			if (result != PerfMeterProfilerLeaseAcquireResult.Acquired) { reason = lease.Warning; return false; }
			_owner = owner; _coordinator = coordinator; _leaseId = id; _generation = lease.Generation;
			return true;
		}

		internal static bool IsHeld(object owner)
		{
			if (!ReferenceEquals(_owner, owner) || _coordinator == null) return false;
			PerfMeterProfilerLeaseStatusSnapshot status = _coordinator.GetStatus(_leaseId);
			return status.IsHeld && status.Generation == _generation;
		}

		internal static void End(object owner)
		{
			if (!ReferenceEquals(_owner, owner)) return;
			try { _coordinator?.ReleaseOwned(_leaseId, "perfmeter.frame-debugger", owner, out _); }
			finally
			{
				_owner = null; _coordinator = null; _leaseId = null; _generation = 0;
				PerfMeterRuntime.Instance?.ResumeAfterEditorDiagnostic();
			}
		}
	}
}