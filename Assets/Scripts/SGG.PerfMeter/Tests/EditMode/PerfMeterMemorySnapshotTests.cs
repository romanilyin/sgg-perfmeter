using System;
using System.Collections.Generic;
using NUnit.Framework;
using SGG.PerfMeter.Editor.Mcp;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterMemorySnapshotTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			PerfMeterMemorySnapshotBackendRegistry.ClearForTests();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			PerfMeterMemorySnapshotBackendRegistry.ClearForTests();
			PerfMeterRuntime.ResetCaptureBundlesForTests();
		}

		[Test]
		public void CapabilitiesAreExplicitAndDoNotStartRuntime()
		{
			PerfMeterMemorySnapshotCapabilitiesSnapshot unavailable = PerformanceMeter.GetMemorySnapshotCapabilities();
			Assert.That(unavailable.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(unavailable.Warning, Does.Contain("No memory snapshot backend"));
			Assert.That(PerformanceMeter.RequestMemorySnapshot(new PerfMeterMemorySnapshotOptions(" ")), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.InvalidRequest));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);

			FakeBackend backend = new FakeBackend();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCapabilitiesSnapshot available = PerformanceMeter.GetMemorySnapshotCapabilities();

			Assert.That(available.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(available.BackendId, Is.EqualTo("fake.memory"));
			Assert.That(available.BackendVersion, Is.EqualTo("1.0"));
			Assert.That(available.MaxSnapshotBytes, Is.EqualTo(PerfMeterMemorySnapshotCoordinator.MaxSnapshotBytes));
			Assert.That(available.SnapshotRoot, Is.EqualTo(PerfMeterMemorySnapshotStorage.RelativeSnapshotRoot));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		[Test]
		public void CoordinatorCompletesOnceAndEnforcesCooldown()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(storage, () => 25d);
			PerfMeterMemorySnapshotOptions options = new PerfMeterMemorySnapshotOptions("memory-one", PerfMeterMemorySnapshotOptions.DefaultCaptureFlags, 100L, 100d);

			Assert.That(coordinator.Request(options, 10d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));
			Assert.That(coordinator.GetStatus(10d).State, Is.EqualTo(PerfMeterMemorySnapshotState.Capturing));
			backend.Complete(true);

			Assert.That(coordinator.TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot status, out PerfMeterMemorySnapshotArtifact artifact), Is.True);
			Assert.That(status.State, Is.EqualTo(PerfMeterMemorySnapshotState.Completed));
			Assert.That(status.CompletedTimeSeconds, Is.EqualTo(25d));
			Assert.That(status.ArtifactSizeBytes, Is.EqualTo(42L));
			Assert.That(artifact.IsAvailable, Is.True);
			backend.CompleteAgain(true);
			Assert.That(storage.DeletedPaths, Does.Not.Contain(artifact.SourcePath));
			Assert.That(coordinator.TryConsumeCompletion(out _, out _), Is.False);
			Assert.That(coordinator.GetStatus(20d).CooldownRemainingSeconds, Is.EqualTo(90d));
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("memory-two", cooldownSeconds: 100d), 20d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Cooldown));
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("memory-two", cooldownSeconds: 100d), 111d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));
			Assert.That(storage.DeletedPaths, Does.Contain(artifact.SourcePath));
		}

		[Test]
		public void ActiveCaptureWinsWhenBackendIsUnregistered()
		{
			FakeBackend backend = new FakeBackend();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(new FakeStorage());
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("active", cooldownSeconds: 0d), 1d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));

			PerformanceMeter.UnregisterMemorySnapshotBackend(backend);

			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("replacement", cooldownSeconds: 0d), 2d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));
			Assert.That(coordinator.GetStatus(2d).CaptureId, Is.EqualTo("active"));
		}

		[Test]
		public void ReentrantRequestCannotBypassSingleFlightReservation()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(storage);
			PerfMeterMemorySnapshotRequestResult nestedResult = PerfMeterMemorySnapshotRequestResult.Started;
			storage.OnPrepare = () => nestedResult = coordinator.Request(new PerfMeterMemorySnapshotOptions("nested", cooldownSeconds: 0d), 1d);

			PerfMeterMemorySnapshotRequestResult result = coordinator.Request(new PerfMeterMemorySnapshotOptions("primary", cooldownSeconds: 0d), 1d);

			Assert.That(result, Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));
			Assert.That(nestedResult, Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));
			Assert.That(coordinator.GetStatus(1d).CaptureId, Is.EqualTo("primary"));
		}

		[Test]
		public void CoordinatorContainsBackendAndStorageFailures()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage { PrepareError = "insufficient_free_disk_space" };
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(storage);

			PerfMeterMemorySnapshotRequestResult diskResult = coordinator.Request(new PerfMeterMemorySnapshotOptions("disk"), 1d);
			Assert.That(diskResult, Is.EqualTo(PerfMeterMemorySnapshotRequestResult.InsufficientDiskSpace));
			Assert.That(coordinator.TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot diskStatus, out _), Is.True);
			Assert.That(diskStatus.State, Is.EqualTo(PerfMeterMemorySnapshotState.Unavailable));

			storage.PrepareError = string.Empty;
			backend.ThrowOnCapture = true;
			PerfMeterMemorySnapshotRequestResult backendResult = coordinator.Request(new PerfMeterMemorySnapshotOptions("backend", cooldownSeconds: 0d), 2d);
			Assert.That(backendResult, Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Failed));
			Assert.That(coordinator.TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot backendStatus, out _), Is.True);
			Assert.That(backendStatus.State, Is.EqualTo(PerfMeterMemorySnapshotState.Error));
			Assert.That(backendStatus.Warning, Does.Contain("InvalidOperationException"));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
		}

		[Test]
		public void CoordinatorPreservesArtifactWhenSupersessionCleanupFails()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(storage);
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("first", cooldownSeconds: 0d), 1d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));
			backend.Complete(true);
			Assert.That(coordinator.TryConsumeCompletion(out _, out PerfMeterMemorySnapshotArtifact artifact), Is.True);
			storage.DeleteSucceeds = false;

			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("second", cooldownSeconds: 0d), 2d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));
			Assert.That(coordinator.HasArtifact(artifact.SourcePath), Is.True);
			Assert.That(coordinator.GetStatus(2d).Warning, Does.Contain("could not be deleted"));
			Assert.That(coordinator.CleanupBlocked, Is.True);
			string warning = coordinator.GetStatus(2d).Warning;
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("second", cooldownSeconds: 0d), 3d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.RejectedOverlap));
			Assert.That(coordinator.GetStatus(3d).Warning, Is.EqualTo(warning));
		}

		[Test]
		public void ShutdownRejectsStaleCompletionAndDeletesArtifact()
		{
			FakeBackend backend = new FakeBackend();
			FakeStorage storage = new FakeStorage();
			PerformanceMeter.RegisterMemorySnapshotBackend(backend);
			PerfMeterMemorySnapshotCoordinator coordinator = new PerfMeterMemorySnapshotCoordinator(storage);
			Assert.That(coordinator.Request(new PerfMeterMemorySnapshotOptions("shutdown"), 1d), Is.EqualTo(PerfMeterMemorySnapshotRequestResult.Started));

			coordinator.Shutdown(2d, "runtime stopped");
			Assert.That(coordinator.TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot status, out _), Is.True);
			Assert.That(status.State, Is.EqualTo(PerfMeterMemorySnapshotState.Error));
			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			backend.Complete(true);

			Assert.That(storage.DeletedPaths, Does.Contain(storage.PreparedPath));
			Assert.That(coordinator.TryConsumeCompletion(out _, out _), Is.False);
		}

		[Test]
		public void TriggerEvaluatorRequiresOptInAndUsesBoundedGrowthWindow()
		{
			PerfMeterMemorySnapshotTriggerEvaluator evaluator = new PerfMeterMemorySnapshotTriggerEvaluator();
			Assert.That(evaluator.TryEvaluate(CreateMetrics(1, 200L), PerfMeterMemorySnapshotTriggerOptions.Disabled, out _), Is.False);

			PerfMeterMemorySnapshotTriggerOptions threshold = new PerfMeterMemorySnapshotTriggerOptions(true, 150L, 0L);
			Assert.That(evaluator.TryEvaluate(CreateMetrics(2, 200L), threshold, out PerfMeterMemorySnapshotTrigger thresholdTrigger), Is.True);
			Assert.That(thresholdTrigger, Is.EqualTo(PerfMeterMemorySnapshotTrigger.SystemMemoryThreshold));

			evaluator.Reset();
			PerfMeterMemorySnapshotTriggerOptions leak = new PerfMeterMemorySnapshotTriggerOptions(true, 0L, 50L, 30);
			Assert.That(evaluator.TryEvaluate(CreateMetrics(10, 100L), leak, out _), Is.False);
			Assert.That(evaluator.TryEvaluate(CreateMetrics(39, 200L), leak, out _), Is.False);
			Assert.That(evaluator.TryEvaluate(CreateMetrics(40, 160L), leak, out PerfMeterMemorySnapshotTrigger leakTrigger), Is.True);
			Assert.That(leakTrigger, Is.EqualTo(PerfMeterMemorySnapshotTrigger.LeakGrowth));
		}

		[Test]
		public void MemoryMcpCommandsAreRegisteredAndReadsDoNotStartRuntime()
		{
			string metadata = PerfMeterTestAssets.ReadMcpCommandsJson();
			string status = PerfMeterMcpCommands.MemorySnapshotStatus();
			string capabilities = PerfMeterMcpCommands.MemorySnapshotCapabilities();

			Assert.That(metadata, Does.Contain("perfmeter.memory.snapshot.request"));
			Assert.That(metadata, Does.Contain("perfmeter.memory.snapshot.status"));
			Assert.That(metadata, Does.Contain("perfmeter.memory.snapshot.capabilities"));
			Assert.That(metadata, Does.Contain("perfmeter.memory.snapshot.triggers.configure"));
			Assert.That(metadata, Does.Contain("sensitive process memory"));
			Assert.That(status, Does.Contain("\"state\":\"Idle\""));
			Assert.That(status, Does.Not.Contain(PerfMeterMemorySnapshotStorage.RelativeSnapshotRoot));
			Assert.That(capabilities, Does.Contain("\"availability\":\"Unavailable\""));
			Assert.That(capabilities, Does.Contain("\"max_snapshot_bytes\":" + PerfMeterMemorySnapshotCoordinator.MaxSnapshotBytes));
			Assert.Throws<InvalidOperationException>(() => PerfMeterMcpCommands.MemorySnapshotTriggersConfigure("{}"));
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
		}

		private static PerfMeterMetricsSnapshot CreateMetrics(int frame, long systemMemoryBytes)
		{
			return new PerfMeterMetricsSnapshot(
				PerfMeterRuntimeState.Running,
				PerfMeterAvailability.Available,
				frame,
				PerfMeterBottleneck.Balanced,
				16.666d,
				false,
				16d,
				8d,
				4d,
				1d,
				0d,
				0,
				0,
				0,
				0,
				0,
				0,
				0L,
				systemMemoryBytes,
				0L,
				0L,
				0d,
				PerfMeterOverdrawMeasurementState.Off,
				0f);
		}

		private sealed class FakeBackend : IPerfMeterMemorySnapshotBackend
		{
			private Action<PerfMeterMemorySnapshotBackendResult> _completed;
			private Action<PerfMeterMemorySnapshotBackendResult> _lastCompleted;
			private string _path;

			public string Id => "fake.memory";
			public string Version => "1.0";
			public PerfMeterMemoryCaptureFlags SupportedCaptureFlags => PerfMeterMemorySnapshotCoordinator.AllCaptureFlags;
			internal bool ThrowOnCapture { get; set; }

			public bool TryCapture(string path, PerfMeterMemoryCaptureFlags captureFlags, Action<PerfMeterMemorySnapshotBackendResult> completed, out string error)
			{
				if (ThrowOnCapture)
				{
					throw new InvalidOperationException("backend failure");
				}

				_path = path;
				_completed = completed;
				_lastCompleted = completed;
				error = string.Empty;
				return true;
			}

			internal void Complete(bool success)
			{
				Action<PerfMeterMemorySnapshotBackendResult> completed = _completed;
				_completed = null;
				completed?.Invoke(new PerfMeterMemorySnapshotBackendResult(success, _path, success ? string.Empty : "capture failed"));
			}

			internal void CompleteAgain(bool success)
			{
				_lastCompleted?.Invoke(new PerfMeterMemorySnapshotBackendResult(success, _path, success ? string.Empty : "capture failed"));
			}
		}

		private sealed class FakeStorage : IPerfMeterMemorySnapshotStorage
		{
			internal string PrepareError { get; set; }
			internal Action OnPrepare { get; set; }
			internal bool DeleteSucceeds { get; set; } = true;
			private int _preparedCount;
			internal string PreparedPath { get; private set; } = string.Empty;
			internal List<string> DeletedPaths { get; } = new List<string>();
			public string RelativeRoot => "Temp/TestMemory";

			public bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error)
			{
				OnPrepare?.Invoke();
				PreparedPath = "owned-memory-" + (++_preparedCount) + ".snap";
				path = PreparedPath;
				availableFreeDiskBytes = long.MaxValue;
				error = PrepareError ?? string.Empty;
				return string.IsNullOrEmpty(error);
			}

			public bool TryValidateCompleted(string path, string expectedPath, long maxBytes, out long sizeBytes, out string error)
			{
				sizeBytes = 42L;
				error = string.Empty;
				return string.Equals(path, expectedPath, StringComparison.Ordinal);
			}

			public bool TryDelete(string path)
			{
				DeletedPaths.Add(path);
				return DeleteSucceeds;
			}
		}
	}
}
