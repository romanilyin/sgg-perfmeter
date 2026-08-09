using System;
using System.Collections.Generic;

namespace SGG.PerfMeter
{
	[Flags]
	public enum PerfMeterProfilerLeaseResource
	{
		None = 0,
		Owner = 1,
		Gpu = 2,
		Operation = 4,
		All = Owner | Gpu | Operation
	}

	public enum PerfMeterProfilerLeaseState
	{
		Idle = 0,
		Preparing = 1,
		Held = 2,
		Releasing = 3,
		Released = 4,
		LostSession = 5,
		Rejected = 6,
		Unavailable = 7,
		Error = 8
	}

	public enum PerfMeterProfilerLeaseReason
	{
		None = 0,
		KnownConflict = 1,
		PossiblyBusy = 2,
		PermissionDenied = 3,
		LostSession = 4,
		InvalidRequest = 5,
		Unavailable = 6
	}

	public enum PerfMeterProfilerLeaseAcquireResult
	{
		Acquired = 0,
		AlreadyHeld = 1,
		KnownConflict = 2,
		PossiblyBusy = 3,
		PermissionDenied = 4,
		LostSession = 5,
		InvalidRequest = 6,
		Unavailable = 7
	}

	public enum PerfMeterProfilerLeaseReleaseResult
	{
		Released = 0,
		AlreadyReleased = 1,
		WrongOwner = 2,
		LostSession = 3,
		InvalidRequest = 4,
		Unavailable = 5
	}

	public static class PerfMeterProfilerLeaseResourceKeys
	{
		public const string ActiveGpu = "active-gpu";
		public const string ExclusiveProfilingOperation = "exclusive-profiling-operation";
	}

	public readonly struct PerfMeterProfilerLeaseRequestOptions
	{
		public const int MaxLeaseIdLength = 128;
		public const int MaxOwnerIdLength = 128;
		public const int MaxResourceKeyLength = 128;
		public const int MaxHostNamespaceLength = 128;

		public PerfMeterProfilerLeaseRequestOptions(
			string leaseId,
			string ownerId,
			string ownerKey,
			string gpuKey,
			string operationKey,
			PerfMeterProfilerLeaseResource resources = PerfMeterProfilerLeaseResource.All,
			string hostNamespace = "")
		{
			LeaseId = PerfMeterProfilerLeaseContract.NormalizeText(leaseId, MaxLeaseIdLength);
			OwnerId = PerfMeterProfilerLeaseContract.NormalizeText(ownerId, MaxOwnerIdLength);
			OwnerKey = PerfMeterProfilerLeaseContract.NormalizeText(ownerKey, MaxResourceKeyLength);
			GpuKey = PerfMeterProfilerLeaseContract.NormalizeText(gpuKey, MaxResourceKeyLength);
			OperationKey = PerfMeterProfilerLeaseContract.NormalizeText(operationKey, MaxResourceKeyLength);
			Resources = PerfMeterProfilerLeaseContract.NormalizeResources(resources);
			HostNamespace = PerfMeterProfilerLeaseContract.NormalizeText(hostNamespace, MaxHostNamespaceLength);
		}

		public bool IsValid =>
			PerfMeterProfilerLeaseContract.IsValidIdentity(LeaseId, MaxLeaseIdLength) &&
			PerfMeterProfilerLeaseContract.IsValidIdentity(OwnerId, MaxOwnerIdLength) &&
			Resources != PerfMeterProfilerLeaseResource.None &&
			(!Claims(PerfMeterProfilerLeaseResource.Owner) || PerfMeterProfilerLeaseContract.IsValidIdentity(OwnerKey, MaxResourceKeyLength)) &&
			(!Claims(PerfMeterProfilerLeaseResource.Gpu) || PerfMeterProfilerLeaseContract.IsValidIdentity(GpuKey, MaxResourceKeyLength)) &&
			(!Claims(PerfMeterProfilerLeaseResource.Operation) || PerfMeterProfilerLeaseContract.IsValidIdentity(OperationKey, MaxResourceKeyLength)) &&
			PerfMeterProfilerLeaseContract.IsValidIdentity(HostNamespace, MaxHostNamespaceLength, true);

		public string LeaseId { get; }
		public string OwnerId { get; }
		public string OwnerKey { get; }
		public string GpuKey { get; }
		public string OperationKey { get; }
		public PerfMeterProfilerLeaseResource Resources { get; }
		public string HostNamespace { get; }

		private bool Claims(PerfMeterProfilerLeaseResource resource)
		{
			return (Resources & resource) == resource;
		}
	}

	public readonly struct PerfMeterProfilerLeaseStatusSnapshot
	{
		public PerfMeterProfilerLeaseStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterProfilerLeaseState state,
			PerfMeterProfilerLeaseReason reason,
			string leaseId,
			string ownerId,
			string ownerKey,
			string gpuKey,
			string operationKey,
			PerfMeterProfilerLeaseResource resources,
			string hostNamespace,
			int generation,
			string warning)
		{
			Availability = availability;
			State = PerfMeterProfilerLeaseContract.NormalizeState(state);
			Reason = PerfMeterProfilerLeaseContract.NormalizeReason(reason);
			LeaseId = PerfMeterProfilerLeaseContract.NormalizeText(leaseId, PerfMeterProfilerLeaseRequestOptions.MaxLeaseIdLength);
			OwnerId = PerfMeterProfilerLeaseContract.NormalizeText(ownerId, PerfMeterProfilerLeaseRequestOptions.MaxOwnerIdLength);
			OwnerKey = PerfMeterProfilerLeaseContract.NormalizeText(ownerKey, PerfMeterProfilerLeaseRequestOptions.MaxResourceKeyLength);
			GpuKey = PerfMeterProfilerLeaseContract.NormalizeText(gpuKey, PerfMeterProfilerLeaseRequestOptions.MaxResourceKeyLength);
			OperationKey = PerfMeterProfilerLeaseContract.NormalizeText(operationKey, PerfMeterProfilerLeaseRequestOptions.MaxResourceKeyLength);
			Resources = PerfMeterProfilerLeaseContract.NormalizeResources(resources);
			HostNamespace = PerfMeterProfilerLeaseContract.NormalizeText(hostNamespace, PerfMeterProfilerLeaseRequestOptions.MaxHostNamespaceLength);
			Generation = Math.Max(0, generation);
			Warning = PerfMeterProfilerLeaseContract.NormalizeWarning(warning);
		}

		public static PerfMeterProfilerLeaseStatusSnapshot None => new PerfMeterProfilerLeaseStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterProfilerLeaseState.Idle,
			PerfMeterProfilerLeaseReason.None,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			PerfMeterProfilerLeaseResource.None,
			string.Empty,
			0,
			string.Empty);

		public bool IsHeld => State == PerfMeterProfilerLeaseState.Held;
		public bool IsTerminal => State == PerfMeterProfilerLeaseState.Released ||
			State == PerfMeterProfilerLeaseState.LostSession ||
			State == PerfMeterProfilerLeaseState.Rejected ||
			State == PerfMeterProfilerLeaseState.Unavailable ||
			State == PerfMeterProfilerLeaseState.Error;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterProfilerLeaseState State { get; }
		public PerfMeterProfilerLeaseReason Reason { get; }
		public string LeaseId { get; }
		public string OwnerId { get; }
		public string OwnerKey { get; }
		public string GpuKey { get; }
		public string OperationKey { get; }
		public PerfMeterProfilerLeaseResource Resources { get; }
		public string HostNamespace { get; }
		public int Generation { get; }
		public string Warning { get; }
	}

	public readonly struct PerfMeterProfilerLeaseCapabilitiesSnapshot
	{
		public const int MaxSupportedActiveLeases = 16;

		public PerfMeterProfilerLeaseCapabilitiesSnapshot(
			PerfMeterAvailability availability,
			PerfMeterProfilerLeaseResource supportedResources,
			int maxActiveLeases,
			string warning = "")
		{
			Availability = availability;
			SupportedResources = PerfMeterProfilerLeaseContract.NormalizeResources(supportedResources);
			MaxActiveLeases = Math.Max(0, Math.Min(MaxSupportedActiveLeases, maxActiveLeases));
			Warning = PerfMeterProfilerLeaseContract.NormalizeWarning(warning);
		}

		public static PerfMeterProfilerLeaseCapabilitiesSnapshot Available => new PerfMeterProfilerLeaseCapabilitiesSnapshot(
			PerfMeterAvailability.Available,
			PerfMeterProfilerLeaseResource.All,
			MaxSupportedActiveLeases);

		public static PerfMeterProfilerLeaseCapabilitiesSnapshot Unavailable => new PerfMeterProfilerLeaseCapabilitiesSnapshot(
			PerfMeterAvailability.Unavailable,
			PerfMeterProfilerLeaseResource.None,
			0,
			"Profiler lease coordinator is unavailable.");

		public bool ProcessLocal => true;
		public bool PersistsHeldAcrossReload => false;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterProfilerLeaseResource SupportedResources { get; }
		public int MaxActiveLeases { get; }
		public string Warning { get; }
	}

	internal interface IPerfMeterProfilerLeaseProbe
	{
		PerfMeterProfilerLeaseReason Evaluate(PerfMeterProfilerLeaseRequestOptions request, out string warning);
	}

	internal sealed class PerfMeterProfilerLeaseCoordinator
	{
		private const int MaxHistoryEntries = 32;
		private readonly object _sync = new object();
		private readonly List<LeaseRecord> _activeLeases = new List<LeaseRecord>();
		private readonly Dictionary<string, PerfMeterProfilerLeaseStatusSnapshot> _history = new Dictionary<string, PerfMeterProfilerLeaseStatusSnapshot>(StringComparer.Ordinal);
		private readonly List<string> _historyOrder = new List<string>();
		private readonly IPerfMeterProfilerLeaseProbe _probe;
		private readonly PerfMeterProfilerLeaseCapabilitiesSnapshot _capabilities;
		private PerfMeterProfilerLeaseStatusSnapshot _lastStatus;
		private int _generation;

		internal PerfMeterProfilerLeaseCoordinator()
			: this(PerfMeterProfilerLeaseCapabilitiesSnapshot.Available, null)
		{
		}

		internal PerfMeterProfilerLeaseCoordinator(
			PerfMeterProfilerLeaseCapabilitiesSnapshot capabilities,
			IPerfMeterProfilerLeaseProbe probe)
		{
			_capabilities = capabilities;
			_probe = probe;
			_lastStatus = PerfMeterProfilerLeaseStatusSnapshot.None;
		}

		internal PerfMeterProfilerLeaseCapabilitiesSnapshot Capabilities => _capabilities;

		internal PerfMeterProfilerLeaseAcquireResult TryAcquire(
			PerfMeterProfilerLeaseRequestOptions request,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			return TryAcquire(request, null, out status);
		}

		internal PerfMeterProfilerLeaseAcquireResult TryAcquireOwned(
			PerfMeterProfilerLeaseRequestOptions request,
			object ownerToken,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			if (ownerToken == null)
			{
				throw new ArgumentNullException(nameof(ownerToken));
			}

			return TryAcquire(request, ownerToken, out status);
		}

		private PerfMeterProfilerLeaseAcquireResult TryAcquire(
			PerfMeterProfilerLeaseRequestOptions request,
			object ownerToken,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			lock (_sync)
			{
				if (!request.IsValid)
				{
					status = CreateRejectedStatus(request, PerfMeterProfilerLeaseReason.InvalidRequest, "Profiler lease request is invalid.");
					return PerfMeterProfilerLeaseAcquireResult.InvalidRequest;
				}

				if (_capabilities.Availability != PerfMeterAvailability.Available ||
					(request.Resources & ~_capabilities.SupportedResources) != PerfMeterProfilerLeaseResource.None)
				{
					status = CreateRejectedStatus(request, PerfMeterProfilerLeaseReason.Unavailable, _capabilities.Warning);
					return PerfMeterProfilerLeaseAcquireResult.Unavailable;
				}

				LeaseRecord existing = FindActive(request.LeaseId);
				if (existing != null)
				{
					if (!string.Equals(existing.Options.OwnerId, request.OwnerId, StringComparison.Ordinal))
					{
						status = CreateConflictStatus(existing);
						return PerfMeterProfilerLeaseAcquireResult.KnownConflict;
					}

					if (Matches(existing.Options, request) && ReferenceEquals(existing.OwnerToken, ownerToken))
					{
						status = existing.Status;
						return PerfMeterProfilerLeaseAcquireResult.AlreadyHeld;
					}

					if (Matches(existing.Options, request))
					{
						status = CreateConflictStatus(existing);
						return PerfMeterProfilerLeaseAcquireResult.KnownConflict;
					}

					status = CreateRejectedStatus(request, PerfMeterProfilerLeaseReason.InvalidRequest, "A held lease cannot change its request.");
					return PerfMeterProfilerLeaseAcquireResult.InvalidRequest;
				}

				PerfMeterProfilerLeaseStatusSnapshot lostStatus;
				if (_history.TryGetValue(request.LeaseId, out lostStatus) && lostStatus.State == PerfMeterProfilerLeaseState.LostSession)
				{
					status = lostStatus;
					return PerfMeterProfilerLeaseAcquireResult.LostSession;
				}

				for (int i = 0; i < _activeLeases.Count; i++)
				{
					LeaseRecord active = _activeLeases[i];
					if (Intersects(active.Options, request))
					{
						status = CreateConflictStatus(active);
						return PerfMeterProfilerLeaseAcquireResult.KnownConflict;
					}
				}

				if (_activeLeases.Count >= _capabilities.MaxActiveLeases)
				{
					status = CreateRejectedStatus(request, PerfMeterProfilerLeaseReason.Unavailable, "The profiler lease capacity is exhausted.");
					return PerfMeterProfilerLeaseAcquireResult.Unavailable;
				}

				if (_probe != null)
				{
					PerfMeterProfilerLeaseReason probeReason;
					string probeWarning;
					try
					{
						probeReason = PerfMeterProfilerLeaseContract.NormalizeReason(_probe.Evaluate(request, out probeWarning));
					}
					catch (Exception exception)
					{
						probeReason = PerfMeterProfilerLeaseReason.Unavailable;
						probeWarning = exception.GetType().Name + ": " + exception.Message;
					}

					if (probeReason != PerfMeterProfilerLeaseReason.None)
					{
						status = CreateRejectedStatus(request, probeReason, probeWarning);
						return ToAcquireResult(probeReason);
					}
				}

				PerfMeterProfilerLeaseStatusSnapshot acquired = CreateStatus(
					request,
					PerfMeterProfilerLeaseState.Held,
					PerfMeterProfilerLeaseReason.None,
					string.Empty);
				_activeLeases.Add(new LeaseRecord(request, acquired, ownerToken));
				_history.Remove(request.LeaseId);
				_lastStatus = acquired;
				return SetStatusAndReturn(acquired, out status, PerfMeterProfilerLeaseAcquireResult.Acquired);
			}
		}

		internal PerfMeterProfilerLeaseReleaseResult Release(
			string leaseId,
			string ownerId,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			return Release(leaseId, ownerId, null, out status);
		}

		internal PerfMeterProfilerLeaseReleaseResult ReleaseOwned(
			string leaseId,
			string ownerId,
			object ownerToken,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			if (ownerToken == null)
			{
				throw new ArgumentNullException(nameof(ownerToken));
			}

			return Release(leaseId, ownerId, ownerToken, out status);
		}

		private PerfMeterProfilerLeaseReleaseResult Release(
			string leaseId,
			string ownerId,
			object ownerToken,
			out PerfMeterProfilerLeaseStatusSnapshot status)
		{
			if (!PerfMeterProfilerLeaseContract.IsValidIdentity(leaseId, PerfMeterProfilerLeaseRequestOptions.MaxLeaseIdLength) ||
				!PerfMeterProfilerLeaseContract.IsValidIdentity(ownerId, PerfMeterProfilerLeaseRequestOptions.MaxOwnerIdLength))
			{
				status = CreateRejectedStatus(default, PerfMeterProfilerLeaseReason.InvalidRequest, "Profiler lease identity is invalid.");
				return PerfMeterProfilerLeaseReleaseResult.InvalidRequest;
			}

			string normalizedLeaseId = PerfMeterProfilerLeaseContract.NormalizeText(leaseId, PerfMeterProfilerLeaseRequestOptions.MaxLeaseIdLength);
			string normalizedOwnerId = PerfMeterProfilerLeaseContract.NormalizeText(ownerId, PerfMeterProfilerLeaseRequestOptions.MaxOwnerIdLength);
			lock (_sync)
			{
				LeaseRecord active = FindActive(normalizedLeaseId);
				if (active != null)
				{
					if (!string.Equals(active.Options.OwnerId, normalizedOwnerId, StringComparison.Ordinal))
					{
						status = active.Status;
						return PerfMeterProfilerLeaseReleaseResult.WrongOwner;
					}

					if (!ReferenceEquals(active.OwnerToken, ownerToken))
					{
						status = active.Status;
						return PerfMeterProfilerLeaseReleaseResult.WrongOwner;
					}

					PerfMeterProfilerLeaseStatusSnapshot released = CreateStatus(
						active.Options,
						PerfMeterProfilerLeaseState.Released,
						PerfMeterProfilerLeaseReason.None,
						string.Empty);
					_activeLeases.Remove(active);
					RememberHistory(released);
					_lastStatus = released;
					status = released;
					return PerfMeterProfilerLeaseReleaseResult.Released;
				}

				if (_history.TryGetValue(normalizedLeaseId, out status))
				{
					if (!string.Equals(status.OwnerId, normalizedOwnerId, StringComparison.Ordinal))
					{
						return PerfMeterProfilerLeaseReleaseResult.WrongOwner;
					}

					return status.State == PerfMeterProfilerLeaseState.LostSession
						? PerfMeterProfilerLeaseReleaseResult.LostSession
						: PerfMeterProfilerLeaseReleaseResult.AlreadyReleased;
				}

				if (_capabilities.Availability != PerfMeterAvailability.Available)
				{
					status = CreateRejectedStatus(
						new PerfMeterProfilerLeaseRequestOptions(normalizedLeaseId, normalizedOwnerId, string.Empty, string.Empty, string.Empty, PerfMeterProfilerLeaseResource.None),
						PerfMeterProfilerLeaseReason.Unavailable,
						_capabilities.Warning);
					return PerfMeterProfilerLeaseReleaseResult.Unavailable;
				}

				status = CreateRejectedStatus(
					new PerfMeterProfilerLeaseRequestOptions(normalizedLeaseId, normalizedOwnerId, string.Empty, string.Empty, string.Empty, PerfMeterProfilerLeaseResource.None),
					PerfMeterProfilerLeaseReason.InvalidRequest,
					"Profiler lease was not acquired.");
				return PerfMeterProfilerLeaseReleaseResult.InvalidRequest;
			}
		}

		internal PerfMeterProfilerLeaseStatusSnapshot GetStatus(string leaseId = null)
		{
			lock (_sync)
			{
				if (!string.IsNullOrEmpty(leaseId))
				{
					LeaseRecord active = FindActive(leaseId);
					if (active != null)
					{
						return active.Status;
					}

					if (_history.TryGetValue(leaseId, out PerfMeterProfilerLeaseStatusSnapshot historyStatus))
					{
						return historyStatus;
					}

					return PerfMeterProfilerLeaseStatusSnapshot.None;
				}

				return _activeLeases.Count > 0 ? _activeLeases[0].Status : _lastStatus;
			}
		}

		internal PerfMeterProfilerLeaseStatusSnapshot[] GetActiveStatuses()
		{
			lock (_sync)
			{
				PerfMeterProfilerLeaseStatusSnapshot[] statuses = new PerfMeterProfilerLeaseStatusSnapshot[_activeLeases.Count];
				for (int i = 0; i < _activeLeases.Count; i++)
				{
					statuses[i] = _activeLeases[i].Status;
				}

				return statuses;
			}
		}

		internal void Reset(string warning = "Profiler lease session was reset.")
		{
			lock (_sync)
			{
				_generation++;
				if (_activeLeases.Count == 0)
				{
					_lastStatus = new PerfMeterProfilerLeaseStatusSnapshot(
						_capabilities.Availability,
						PerfMeterProfilerLeaseState.Idle,
						PerfMeterProfilerLeaseReason.None,
						string.Empty,
						string.Empty,
						string.Empty,
						string.Empty,
						string.Empty,
						PerfMeterProfilerLeaseResource.None,
						string.Empty,
						_generation,
						warning);
					return;
				}

				for (int i = 0; i < _activeLeases.Count; i++)
				{
					LeaseRecord active = _activeLeases[i];
					PerfMeterProfilerLeaseStatusSnapshot lost = CreateStatus(
						active.Options,
						PerfMeterProfilerLeaseState.LostSession,
						PerfMeterProfilerLeaseReason.LostSession,
						warning);
					RememberHistory(lost);
					_lastStatus = lost;
				}

				_activeLeases.Clear();
			}
		}

		private LeaseRecord FindActive(string leaseId)
		{
			for (int i = 0; i < _activeLeases.Count; i++)
			{
				if (string.Equals(_activeLeases[i].Options.LeaseId, leaseId, StringComparison.Ordinal))
				{
					return _activeLeases[i];
				}
			}

			return null;
		}

		private void RememberHistory(PerfMeterProfilerLeaseStatusSnapshot status)
		{
			_history[status.LeaseId] = status;
			_historyOrder.Remove(status.LeaseId);
			_historyOrder.Add(status.LeaseId);
			while (_historyOrder.Count > MaxHistoryEntries)
			{
				string oldest = _historyOrder[0];
				_historyOrder.RemoveAt(0);
				_history.Remove(oldest);
			}
		}

		private PerfMeterProfilerLeaseStatusSnapshot CreateRejectedStatus(
			PerfMeterProfilerLeaseRequestOptions request,
			PerfMeterProfilerLeaseReason reason,
			string warning)
		{
			PerfMeterProfilerLeaseState state = reason == PerfMeterProfilerLeaseReason.LostSession
				? PerfMeterProfilerLeaseState.LostSession
				: reason == PerfMeterProfilerLeaseReason.Unavailable
					? PerfMeterProfilerLeaseState.Unavailable
					: PerfMeterProfilerLeaseState.Rejected;
			return CreateStatus(request, state, reason, warning);
		}

		private PerfMeterProfilerLeaseStatusSnapshot CreateConflictStatus(LeaseRecord active)
		{
			return CreateStatus(
				active.Options,
				PerfMeterProfilerLeaseState.Held,
				PerfMeterProfilerLeaseReason.KnownConflict,
				"The requested profiler lease intersects an active lease.");
		}

		private PerfMeterProfilerLeaseStatusSnapshot CreateStatus(
			PerfMeterProfilerLeaseRequestOptions request,
			PerfMeterProfilerLeaseState state,
			PerfMeterProfilerLeaseReason reason,
			string warning)
		{
			return new PerfMeterProfilerLeaseStatusSnapshot(
				_capabilities.Availability,
				state,
				reason,
				request.LeaseId,
				request.OwnerId,
				request.OwnerKey,
				request.GpuKey,
				request.OperationKey,
				request.Resources,
				request.HostNamespace,
				_generation,
				warning);
		}

		private static bool Matches(PerfMeterProfilerLeaseRequestOptions left, PerfMeterProfilerLeaseRequestOptions right)
		{
			return string.Equals(left.LeaseId, right.LeaseId, StringComparison.Ordinal) &&
				string.Equals(left.OwnerId, right.OwnerId, StringComparison.Ordinal) &&
				string.Equals(left.OwnerKey, right.OwnerKey, StringComparison.Ordinal) &&
				string.Equals(left.GpuKey, right.GpuKey, StringComparison.Ordinal) &&
				string.Equals(left.OperationKey, right.OperationKey, StringComparison.Ordinal) &&
				string.Equals(left.HostNamespace, right.HostNamespace, StringComparison.Ordinal) &&
				left.Resources == right.Resources;
		}

		private static bool Intersects(PerfMeterProfilerLeaseRequestOptions left, PerfMeterProfilerLeaseRequestOptions right)
		{
			return Intersects(PerfMeterProfilerLeaseResource.Owner, left.Resources, right.Resources, left.OwnerKey, right.OwnerKey) ||
				Intersects(PerfMeterProfilerLeaseResource.Gpu, left.Resources, right.Resources, left.GpuKey, right.GpuKey) ||
				Intersects(PerfMeterProfilerLeaseResource.Operation, left.Resources, right.Resources, left.OperationKey, right.OperationKey);
		}

		private static bool Intersects(
			PerfMeterProfilerLeaseResource resource,
			PerfMeterProfilerLeaseResource leftResources,
			PerfMeterProfilerLeaseResource rightResources,
			string leftKey,
			string rightKey)
		{
			return (leftResources & resource) == resource &&
				(rightResources & resource) == resource &&
				string.Equals(leftKey, rightKey, StringComparison.Ordinal);
		}

		private static PerfMeterProfilerLeaseAcquireResult ToAcquireResult(PerfMeterProfilerLeaseReason reason)
		{
			switch (reason)
			{
				case PerfMeterProfilerLeaseReason.KnownConflict: return PerfMeterProfilerLeaseAcquireResult.KnownConflict;
				case PerfMeterProfilerLeaseReason.PossiblyBusy: return PerfMeterProfilerLeaseAcquireResult.PossiblyBusy;
				case PerfMeterProfilerLeaseReason.PermissionDenied: return PerfMeterProfilerLeaseAcquireResult.PermissionDenied;
				case PerfMeterProfilerLeaseReason.LostSession: return PerfMeterProfilerLeaseAcquireResult.LostSession;
				case PerfMeterProfilerLeaseReason.InvalidRequest: return PerfMeterProfilerLeaseAcquireResult.InvalidRequest;
				default: return PerfMeterProfilerLeaseAcquireResult.Unavailable;
			}
		}

		private static PerfMeterProfilerLeaseAcquireResult SetStatusAndReturn(
			PerfMeterProfilerLeaseStatusSnapshot value,
			out PerfMeterProfilerLeaseStatusSnapshot status,
			PerfMeterProfilerLeaseAcquireResult result)
		{
			status = value;
			return result;
		}

		private sealed class LeaseRecord
		{
			internal LeaseRecord(PerfMeterProfilerLeaseRequestOptions options, PerfMeterProfilerLeaseStatusSnapshot status, object ownerToken)
			{
				Options = options;
				Status = status;
				OwnerToken = ownerToken;
			}

			internal PerfMeterProfilerLeaseRequestOptions Options { get; }
			internal PerfMeterProfilerLeaseStatusSnapshot Status { get; }
			internal object OwnerToken { get; }
		}
	}

	internal static class PerfMeterProfilerLeaseContract
	{
		internal static string NormalizeText(string value, int maximumLength)
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length <= maximumLength)
			{
				return normalized;
			}

			return normalized.Substring(0, maximumLength);
		}

		internal static string NormalizeWarning(string value)
		{
			return NormalizeText(value, 1024);
		}

		internal static bool IsValidIdentity(string value, int maximumLength, bool allowEmpty = false)
		{
			if (string.IsNullOrEmpty(value) && allowEmpty)
			{
				return true;
			}

			if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
			{
				return false;
			}

			for (int i = 0; i < value.Length; i++)
			{
				if (char.IsControl(value[i]))
				{
					return false;
				}
			}

			return true;
		}

		internal static PerfMeterProfilerLeaseResource NormalizeResources(PerfMeterProfilerLeaseResource value)
		{
			return value & PerfMeterProfilerLeaseResource.All;
		}

		internal static PerfMeterProfilerLeaseState NormalizeState(PerfMeterProfilerLeaseState value)
		{
			switch (value)
			{
				case PerfMeterProfilerLeaseState.Idle:
				case PerfMeterProfilerLeaseState.Preparing:
				case PerfMeterProfilerLeaseState.Held:
				case PerfMeterProfilerLeaseState.Releasing:
				case PerfMeterProfilerLeaseState.Released:
				case PerfMeterProfilerLeaseState.LostSession:
				case PerfMeterProfilerLeaseState.Rejected:
				case PerfMeterProfilerLeaseState.Unavailable:
				case PerfMeterProfilerLeaseState.Error:
					return value;
				default:
					return PerfMeterProfilerLeaseState.Error;
			}
		}

		internal static PerfMeterProfilerLeaseReason NormalizeReason(PerfMeterProfilerLeaseReason value)
		{
			switch (value)
			{
				case PerfMeterProfilerLeaseReason.None:
				case PerfMeterProfilerLeaseReason.KnownConflict:
				case PerfMeterProfilerLeaseReason.PossiblyBusy:
				case PerfMeterProfilerLeaseReason.PermissionDenied:
				case PerfMeterProfilerLeaseReason.LostSession:
				case PerfMeterProfilerLeaseReason.InvalidRequest:
				case PerfMeterProfilerLeaseReason.Unavailable:
					return value;
				default:
					return PerfMeterProfilerLeaseReason.Unavailable;
			}
		}
	}
}
