using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterProfilerLeaseTests
	{
		[Test]
		public void EnumValuesRemainStable()
		{
			Assert.That((int)PerfMeterProfilerLeaseResource.None, Is.EqualTo(0));
			Assert.That((int)PerfMeterProfilerLeaseResource.Owner, Is.EqualTo(1));
			Assert.That((int)PerfMeterProfilerLeaseResource.Gpu, Is.EqualTo(2));
			Assert.That((int)PerfMeterProfilerLeaseResource.Operation, Is.EqualTo(4));
			Assert.That((int)PerfMeterProfilerLeaseResource.All, Is.EqualTo(7));

			Assert.That((int)PerfMeterProfilerLeaseState.Idle, Is.EqualTo(0));
			Assert.That((int)PerfMeterProfilerLeaseState.Preparing, Is.EqualTo(1));
			Assert.That((int)PerfMeterProfilerLeaseState.Held, Is.EqualTo(2));
			Assert.That((int)PerfMeterProfilerLeaseState.Releasing, Is.EqualTo(3));
			Assert.That((int)PerfMeterProfilerLeaseState.Released, Is.EqualTo(4));
			Assert.That((int)PerfMeterProfilerLeaseState.LostSession, Is.EqualTo(5));
			Assert.That((int)PerfMeterProfilerLeaseState.Rejected, Is.EqualTo(6));
			Assert.That((int)PerfMeterProfilerLeaseState.Unavailable, Is.EqualTo(7));
			Assert.That((int)PerfMeterProfilerLeaseState.Error, Is.EqualTo(8));

			Assert.That((int)PerfMeterProfilerLeaseReason.None, Is.EqualTo(0));
			Assert.That((int)PerfMeterProfilerLeaseReason.KnownConflict, Is.EqualTo(1));
			Assert.That((int)PerfMeterProfilerLeaseReason.PossiblyBusy, Is.EqualTo(2));
			Assert.That((int)PerfMeterProfilerLeaseReason.PermissionDenied, Is.EqualTo(3));
			Assert.That((int)PerfMeterProfilerLeaseReason.LostSession, Is.EqualTo(4));
			Assert.That((int)PerfMeterProfilerLeaseReason.InvalidRequest, Is.EqualTo(5));
			Assert.That((int)PerfMeterProfilerLeaseReason.Unavailable, Is.EqualTo(6));

			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.Acquired, Is.EqualTo(0));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.AlreadyHeld, Is.EqualTo(1));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.KnownConflict, Is.EqualTo(2));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.PossiblyBusy, Is.EqualTo(3));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.PermissionDenied, Is.EqualTo(4));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.LostSession, Is.EqualTo(5));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.InvalidRequest, Is.EqualTo(6));
			Assert.That((int)PerfMeterProfilerLeaseAcquireResult.Unavailable, Is.EqualTo(7));

			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.Released, Is.EqualTo(0));
			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.AlreadyReleased, Is.EqualTo(1));
			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.WrongOwner, Is.EqualTo(2));
			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.LostSession, Is.EqualTo(3));
			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.InvalidRequest, Is.EqualTo(4));
			Assert.That((int)PerfMeterProfilerLeaseReleaseResult.Unavailable, Is.EqualTo(5));
		}

		[Test]
		public void CapabilitiesAreProcessLocalAndNeverPersistHeldAcrossReload()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseCapabilitiesSnapshot capabilities = coordinator.Capabilities;

			Assert.That(capabilities.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(capabilities.ProcessLocal, Is.True);
			Assert.That(capabilities.PersistsHeldAcrossReload, Is.False);
			Assert.That(capabilities.SupportedResources, Is.EqualTo(PerfMeterProfilerLeaseResource.All));

			PerfMeterProfilerLeaseRequestOptions request = CreateRequest("reload-lease", "owner-a");
			Assert.That(coordinator.TryAcquire(request, out PerfMeterProfilerLeaseStatusSnapshot acquired), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(acquired.IsHeld, Is.True);

			coordinator.Reset("domain reload");

			Assert.That(coordinator.GetActiveStatuses(), Is.Empty);
			PerfMeterProfilerLeaseStatusSnapshot lost = coordinator.GetStatus(request.LeaseId);
			Assert.That(lost.State, Is.EqualTo(PerfMeterProfilerLeaseState.LostSession));
			Assert.That(lost.Reason, Is.EqualTo(PerfMeterProfilerLeaseReason.LostSession));
			Assert.That(lost.IsHeld, Is.False);
			Assert.That(coordinator.TryAcquire(request, out PerfMeterProfilerLeaseStatusSnapshot repeated), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.LostSession));
			Assert.That(repeated.IsHeld, Is.False);

			PerfMeterProfilerLeaseCoordinator replacement = new PerfMeterProfilerLeaseCoordinator();
			Assert.That(replacement.GetStatus().IsHeld, Is.False);
			Assert.That(replacement.GetStatus().State, Is.EqualTo(PerfMeterProfilerLeaseState.Idle));
		}

		[Test]
		public void SameRequestAcquireIsIdempotent()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions request = CreateRequest("same-request", "owner-a");

			Assert.That(coordinator.TryAcquire(request, out PerfMeterProfilerLeaseStatusSnapshot first), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.TryAcquire(request, out PerfMeterProfilerLeaseStatusSnapshot second), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.AlreadyHeld));
			Assert.That(second.IsHeld, Is.True);
			Assert.That(second.LeaseId, Is.EqualTo(first.LeaseId));
			Assert.That(second.Generation, Is.EqualTo(first.Generation));
			Assert.That(coordinator.GetActiveStatuses(), Has.Length.EqualTo(1));
		}

		[Test]
		public void OwnedLeaseRequiresTokenIdentityForAcquireAndRelease()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions request = CreateRequest("owned-lease", "owner-a");
			object ownerToken = new object();
			object otherToken = new object();

			Assert.Throws<System.ArgumentNullException>(() => coordinator.TryAcquireOwned(request, null, out _));
			Assert.Throws<System.ArgumentNullException>(() => coordinator.ReleaseOwned(request.LeaseId, request.OwnerId, null, out _));
			Assert.That(coordinator.TryAcquireOwned(request, ownerToken, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.TryAcquireOwned(request, ownerToken, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.AlreadyHeld));
			Assert.That(coordinator.TryAcquire(request, out PerfMeterProfilerLeaseStatusSnapshot publicConflict), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.KnownConflict));
			Assert.That(publicConflict.IsHeld, Is.True);
			Assert.That(coordinator.TryAcquireOwned(request, otherToken, out PerfMeterProfilerLeaseStatusSnapshot otherConflict), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.KnownConflict));
			Assert.That(otherConflict.IsHeld, Is.True);
			Assert.That(coordinator.Release(request.LeaseId, request.OwnerId, out PerfMeterProfilerLeaseStatusSnapshot publicRelease), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.WrongOwner));
			Assert.That(publicRelease.IsHeld, Is.True);
			Assert.That(coordinator.ReleaseOwned(request.LeaseId, request.OwnerId, otherToken, out PerfMeterProfilerLeaseStatusSnapshot otherRelease), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.WrongOwner));
			Assert.That(otherRelease.IsHeld, Is.True);
			Assert.That(coordinator.ReleaseOwned(request.LeaseId, request.OwnerId, ownerToken, out PerfMeterProfilerLeaseStatusSnapshot released), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.Released));
			Assert.That(released.IsHeld, Is.False);
		}

		[Test]
		public void IntersectingOwnerGpuAndOperationResourcesAreKnownConflicts()
		{
			AssertConflict(
				new PerfMeterProfilerLeaseRequestOptions("owner-a", "caller-a", "shared-owner", "gpu-a", "operation-a", PerfMeterProfilerLeaseResource.Owner),
				new PerfMeterProfilerLeaseRequestOptions("owner-b", "caller-b", "shared-owner", "gpu-b", "operation-b", PerfMeterProfilerLeaseResource.Owner));
			AssertConflict(
				new PerfMeterProfilerLeaseRequestOptions("gpu-a", "caller-a", "owner-a", "shared-gpu", "operation-a", PerfMeterProfilerLeaseResource.Gpu),
				new PerfMeterProfilerLeaseRequestOptions("gpu-b", "caller-b", "owner-b", "shared-gpu", "operation-b", PerfMeterProfilerLeaseResource.Gpu));
			AssertConflict(
				new PerfMeterProfilerLeaseRequestOptions("operation-a", "caller-a", "owner-a", "gpu-a", "shared-operation", PerfMeterProfilerLeaseResource.Operation),
				new PerfMeterProfilerLeaseRequestOptions("operation-b", "caller-b", "owner-b", "gpu-b", "shared-operation", PerfMeterProfilerLeaseResource.Operation));
		}

		[Test]
		public void NonIntersectingResourcesCanBeHeldTogether()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions ownerRequest = new PerfMeterProfilerLeaseRequestOptions(
				"owner-lease",
				"caller-a",
				"owner-a",
				"gpu-shared",
				"operation-a",
				PerfMeterProfilerLeaseResource.Owner);
			PerfMeterProfilerLeaseRequestOptions gpuRequest = new PerfMeterProfilerLeaseRequestOptions(
				"gpu-lease",
				"caller-b",
				"owner-a",
				"gpu-shared",
				"operation-a",
				PerfMeterProfilerLeaseResource.Gpu);

			Assert.That(coordinator.TryAcquire(ownerRequest, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.TryAcquire(gpuRequest, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.GetActiveStatuses(), Has.Length.EqualTo(2));
		}

		[Test]
		public void InvalidRequestsAndUnavailableCapabilitiesAreTyped()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			Assert.That(coordinator.TryAcquire(default, out PerfMeterProfilerLeaseStatusSnapshot invalid), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.InvalidRequest));
			Assert.That(invalid.Reason, Is.EqualTo(PerfMeterProfilerLeaseReason.InvalidRequest));
			Assert.That(coordinator.TryAcquire(
				new PerfMeterProfilerLeaseRequestOptions("missing-key", "caller", "", "gpu", "operation", PerfMeterProfilerLeaseResource.Owner),
				out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.InvalidRequest));
			Assert.That(coordinator.Release("missing", "caller", out _), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.InvalidRequest));

			PerfMeterProfilerLeaseCoordinator unavailable = new PerfMeterProfilerLeaseCoordinator(PerfMeterProfilerLeaseCapabilitiesSnapshot.Unavailable, null);
			PerfMeterProfilerLeaseAcquireResult result = unavailable.TryAcquire(CreateRequest("unavailable", "caller"), out PerfMeterProfilerLeaseStatusSnapshot status);
			Assert.That(result, Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Unavailable));
			Assert.That(status.State, Is.EqualTo(PerfMeterProfilerLeaseState.Unavailable));
			Assert.That(status.Reason, Is.EqualTo(PerfMeterProfilerLeaseReason.Unavailable));
		}

		[TestCase(PerfMeterProfilerLeaseReason.PossiblyBusy, PerfMeterProfilerLeaseAcquireResult.PossiblyBusy)]
		[TestCase(PerfMeterProfilerLeaseReason.PermissionDenied, PerfMeterProfilerLeaseAcquireResult.PermissionDenied)]
		[TestCase(PerfMeterProfilerLeaseReason.LostSession, PerfMeterProfilerLeaseAcquireResult.LostSession)]
		public void ProbeReasonsAreReturnedWithoutGrantingLease(
			PerfMeterProfilerLeaseReason reason,
			PerfMeterProfilerLeaseAcquireResult expectedResult)
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator(
				PerfMeterProfilerLeaseCapabilitiesSnapshot.Available,
				new FakeProbe(reason));

			PerfMeterProfilerLeaseAcquireResult result = coordinator.TryAcquire(CreateRequest("probe-" + reason, "caller"), out PerfMeterProfilerLeaseStatusSnapshot status);

			Assert.That(result, Is.EqualTo(expectedResult));
			Assert.That(status.Reason, Is.EqualTo(reason));
			Assert.That(status.IsHeld, Is.False);
			Assert.That(coordinator.GetActiveStatuses(), Is.Empty);
		}

		[Test]
		public void WrongOwnerReleaseFailsAndSameOwnerReleaseIsIdempotent()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions request = CreateRequest("release-lease", "owner-a");
			Assert.That(coordinator.TryAcquire(request, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));

			Assert.That(coordinator.Release(request.LeaseId, "owner-b", out PerfMeterProfilerLeaseStatusSnapshot wrongOwnerStatus), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.WrongOwner));
			Assert.That(wrongOwnerStatus.IsHeld, Is.True);
			Assert.That(coordinator.Release(request.LeaseId, request.OwnerId, out PerfMeterProfilerLeaseStatusSnapshot released), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.Released));
			Assert.That(released.State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
			Assert.That(coordinator.Release(request.LeaseId, request.OwnerId, out PerfMeterProfilerLeaseStatusSnapshot repeated), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.AlreadyReleased));
			Assert.That(repeated.State, Is.EqualTo(PerfMeterProfilerLeaseState.Released));
			Assert.That(coordinator.GetActiveStatuses(), Is.Empty);
		}

		[Test]
		public void ResetMarksEveryHeldLeaseLostAndDoesNotRehydrateIt()
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			PerfMeterProfilerLeaseRequestOptions first = new PerfMeterProfilerLeaseRequestOptions(
				"reset-a", "owner-a", "owner-key", "gpu-a", "operation-a", PerfMeterProfilerLeaseResource.Owner);
			PerfMeterProfilerLeaseRequestOptions second = new PerfMeterProfilerLeaseRequestOptions(
				"reset-b", "owner-b", "owner-key", "gpu-b", "operation-b", PerfMeterProfilerLeaseResource.Gpu);
			Assert.That(coordinator.TryAcquire(first, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.TryAcquire(second, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));

			coordinator.Reset("owner session ended");

			Assert.That(coordinator.GetActiveStatuses(), Is.Empty);
			PerfMeterProfilerLeaseStatusSnapshot status = coordinator.GetStatus(first.LeaseId);
			PerfMeterProfilerLeaseStatusSnapshot secondStatus = coordinator.GetStatus(second.LeaseId);
			Assert.That(status.State, Is.EqualTo(PerfMeterProfilerLeaseState.LostSession));
			Assert.That(status.Reason, Is.EqualTo(PerfMeterProfilerLeaseReason.LostSession));
			Assert.That(status.Warning, Does.Contain("owner session ended"));
			Assert.That(status.IsHeld, Is.False);
			Assert.That(secondStatus.State, Is.EqualTo(PerfMeterProfilerLeaseState.LostSession));
			Assert.That(secondStatus.IsHeld, Is.False);
			Assert.That(coordinator.Release(first.LeaseId, first.OwnerId, out _), Is.EqualTo(PerfMeterProfilerLeaseReleaseResult.LostSession));
		}

		private static void AssertConflict(
			PerfMeterProfilerLeaseRequestOptions first,
			PerfMeterProfilerLeaseRequestOptions second)
		{
			PerfMeterProfilerLeaseCoordinator coordinator = new PerfMeterProfilerLeaseCoordinator();
			Assert.That(coordinator.TryAcquire(first, out _), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.Acquired));
			Assert.That(coordinator.TryAcquire(second, out PerfMeterProfilerLeaseStatusSnapshot conflict), Is.EqualTo(PerfMeterProfilerLeaseAcquireResult.KnownConflict));
			Assert.That(conflict.Reason, Is.EqualTo(PerfMeterProfilerLeaseReason.KnownConflict));
			Assert.That(conflict.LeaseId, Is.EqualTo(first.LeaseId));
		}

		private static PerfMeterProfilerLeaseRequestOptions CreateRequest(string leaseId, string ownerId)
		{
			return new PerfMeterProfilerLeaseRequestOptions(
				leaseId,
				ownerId,
				"owner-key",
				"gpu-key",
				"operation-key");
		}

		private sealed class FakeProbe : IPerfMeterProfilerLeaseProbe
		{
			private readonly PerfMeterProfilerLeaseReason _reason;

			internal FakeProbe(PerfMeterProfilerLeaseReason reason)
			{
				_reason = reason;
			}

			public PerfMeterProfilerLeaseReason Evaluate(PerfMeterProfilerLeaseRequestOptions request, out string warning)
			{
				warning = "probe: " + _reason;
				return _reason;
			}
		}
	}
}
