using System;
using System.IO;
using UnityEngine;

namespace SGG.PerfMeter
{
	[Flags]
	public enum PerfMeterMemoryCaptureFlags
	{
		None = 0,
		ManagedObjects = 1 << 0,
		NativeObjects = 1 << 1,
		NativeAllocations = 1 << 2,
		NativeAllocationSites = 1 << 3,
		NativeStackTraces = 1 << 4
	}

	public enum PerfMeterMemorySnapshotTrigger
	{
		Manual = 0,
		SystemMemoryThreshold = 1,
		LeakGrowth = 2
	}

	public enum PerfMeterMemorySnapshotState
	{
		NotRequested = 0,
		Idle = 1,
		Capturing = 2,
		Completed = 3,
		Unavailable = 4,
		Error = 5
	}

	public enum PerfMeterMemorySnapshotRequestResult
	{
		Started = 0,
		AlreadyActive = 1,
		RejectedOverlap = 2,
		Cooldown = 3,
		Unavailable = 4,
		InsufficientDiskSpace = 5,
		InvalidRequest = 6,
		Failed = 7
	}

	public readonly struct PerfMeterMemorySnapshotOptions
	{
		public const long DefaultMinimumFreeDiskBytes = 1024L * 1024L * 1024L;
		public const double DefaultCooldownSeconds = 300d;
		public const PerfMeterMemoryCaptureFlags DefaultCaptureFlags = PerfMeterMemoryCaptureFlags.ManagedObjects | PerfMeterMemoryCaptureFlags.NativeObjects;

		public PerfMeterMemorySnapshotOptions(
			string captureId,
			PerfMeterMemoryCaptureFlags captureFlags = DefaultCaptureFlags,
			long minimumFreeDiskBytes = DefaultMinimumFreeDiskBytes,
			double cooldownSeconds = DefaultCooldownSeconds)
			: this(captureId, PerfMeterMemorySnapshotTrigger.Manual, captureFlags, minimumFreeDiskBytes, cooldownSeconds)
		{
		}

		internal PerfMeterMemorySnapshotOptions(
			string captureId,
			PerfMeterMemorySnapshotTrigger trigger,
			PerfMeterMemoryCaptureFlags captureFlags,
			long minimumFreeDiskBytes,
			double cooldownSeconds)
		{
			CaptureId = captureId ?? string.Empty;
			Trigger = trigger;
			CaptureFlags = captureFlags;
			MinimumFreeDiskBytes = Math.Max(0L, minimumFreeDiskBytes);
			CooldownSeconds = Clamp(cooldownSeconds, 0d, 86400d);
		}

		public string CaptureId { get; }
		public PerfMeterMemorySnapshotTrigger Trigger { get; }
		public PerfMeterMemoryCaptureFlags CaptureFlags { get; }
		public long MinimumFreeDiskBytes { get; }
		public double CooldownSeconds { get; }

		private static double Clamp(double value, double minimum, double maximum)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				return minimum;
			}

			return value < minimum ? minimum : value > maximum ? maximum : value;
		}
	}

	public readonly struct PerfMeterMemorySnapshotTriggerOptions
	{
		public PerfMeterMemorySnapshotTriggerOptions(
			bool enabled,
			long systemMemoryThresholdBytes,
			long leakGrowthThresholdBytes,
			int leakWindowFrames = 300,
			PerfMeterMemoryCaptureFlags captureFlags = PerfMeterMemorySnapshotOptions.DefaultCaptureFlags,
			long minimumFreeDiskBytes = PerfMeterMemorySnapshotOptions.DefaultMinimumFreeDiskBytes,
			double cooldownSeconds = PerfMeterMemorySnapshotOptions.DefaultCooldownSeconds)
		{
			Enabled = enabled;
			SystemMemoryThresholdBytes = Math.Max(0L, systemMemoryThresholdBytes);
			LeakGrowthThresholdBytes = Math.Max(0L, leakGrowthThresholdBytes);
			LeakWindowFrames = Mathf.Clamp(leakWindowFrames, 30, 36000);
			CaptureFlags = captureFlags;
			MinimumFreeDiskBytes = Math.Max(0L, minimumFreeDiskBytes);
			CooldownSeconds = double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds)
				? 0d
				: Math.Max(0d, Math.Min(86400d, cooldownSeconds));
		}

		public static PerfMeterMemorySnapshotTriggerOptions Disabled => default;
		public bool Enabled { get; }
		public long SystemMemoryThresholdBytes { get; }
		public long LeakGrowthThresholdBytes { get; }
		public int LeakWindowFrames { get; }
		public PerfMeterMemoryCaptureFlags CaptureFlags { get; }
		public long MinimumFreeDiskBytes { get; }
		public double CooldownSeconds { get; }
	}

	public readonly struct PerfMeterMemorySnapshotBackendResult
	{
		public PerfMeterMemorySnapshotBackendResult(bool success, string path, string error)
		{
			Success = success;
			Path = path ?? string.Empty;
			Error = error ?? string.Empty;
		}

		public bool Success { get; }
		public string Path { get; }
		public string Error { get; }
	}

	public interface IPerfMeterMemorySnapshotBackend
	{
		string Id { get; }
		string Version { get; }
		PerfMeterMemoryCaptureFlags SupportedCaptureFlags { get; }
		bool TryCapture(
			string path,
			PerfMeterMemoryCaptureFlags captureFlags,
			Action<PerfMeterMemorySnapshotBackendResult> completed,
			out string error);
	}

	public readonly struct PerfMeterMemorySnapshotCapabilitiesSnapshot
	{
		public PerfMeterMemorySnapshotCapabilitiesSnapshot(
			PerfMeterAvailability availability,
			string backendId,
			string backendVersion,
			PerfMeterMemoryCaptureFlags supportedCaptureFlags,
			long maxSnapshotBytes,
			string snapshotRoot,
			string warning)
		{
			Availability = availability;
			BackendId = backendId ?? string.Empty;
			BackendVersion = backendVersion ?? string.Empty;
			SupportedCaptureFlags = supportedCaptureFlags;
			MaxSnapshotBytes = Math.Max(0L, maxSnapshotBytes);
			SnapshotRoot = snapshotRoot ?? string.Empty;
			Warning = warning ?? string.Empty;
		}

		public PerfMeterAvailability Availability { get; }
		public string BackendId { get; }
		public string BackendVersion { get; }
		public PerfMeterMemoryCaptureFlags SupportedCaptureFlags { get; }
		public long MaxSnapshotBytes { get; }
		public string SnapshotRoot { get; }
		public string Warning { get; }
	}

	public readonly struct PerfMeterMemorySnapshotStatusSnapshot
	{
		public PerfMeterMemorySnapshotStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterMemorySnapshotState state,
			string captureId,
			PerfMeterMemorySnapshotTrigger trigger,
			PerfMeterMemoryCaptureFlags requestedCaptureFlags,
			string backendId,
			string backendVersion,
			double startedTimeSeconds,
			double completedTimeSeconds,
			long artifactSizeBytes,
			double cooldownRemainingSeconds,
			string warning)
		{
			Availability = availability;
			State = state;
			CaptureId = captureId ?? string.Empty;
			Trigger = trigger;
			RequestedCaptureFlags = requestedCaptureFlags;
			BackendId = backendId ?? string.Empty;
			BackendVersion = backendVersion ?? string.Empty;
			StartedTimeSeconds = Math.Max(0d, startedTimeSeconds);
			CompletedTimeSeconds = Math.Max(0d, completedTimeSeconds);
			ArtifactSizeBytes = Math.Max(0L, artifactSizeBytes);
			CooldownRemainingSeconds = Math.Max(0d, cooldownRemainingSeconds);
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterMemorySnapshotStatusSnapshot NotRunning => new PerfMeterMemorySnapshotStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterMemorySnapshotState.Idle,
			string.Empty,
			PerfMeterMemorySnapshotTrigger.Manual,
			PerfMeterMemoryCaptureFlags.None,
			string.Empty,
			string.Empty,
			0d,
			0d,
			0L,
			0d,
			"Memory snapshot coordinator is not running.");

		public bool IsActive => State == PerfMeterMemorySnapshotState.Capturing;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterMemorySnapshotState State { get; }
		public string CaptureId { get; }
		public PerfMeterMemorySnapshotTrigger Trigger { get; }
		public PerfMeterMemoryCaptureFlags RequestedCaptureFlags { get; }
		public string BackendId { get; }
		public string BackendVersion { get; }
		public double StartedTimeSeconds { get; }
		public double CompletedTimeSeconds { get; }
		public long ArtifactSizeBytes { get; }
		public double CooldownRemainingSeconds { get; }
		public string Warning { get; }
	}

	internal readonly struct PerfMeterMemorySnapshotArtifact
	{
		internal PerfMeterMemorySnapshotArtifact(PerfMeterMemorySnapshotStatusSnapshot status, string sourcePath)
		{
			Status = status;
			SourcePath = sourcePath ?? string.Empty;
		}

		internal PerfMeterMemorySnapshotStatusSnapshot Status { get; }
		internal string SourcePath { get; }
		internal bool IsAvailable => Status.State == PerfMeterMemorySnapshotState.Completed && !string.IsNullOrEmpty(SourcePath);
	}

	internal static class PerfMeterMemorySnapshotBackendRegistry
	{
		private static readonly object Sync = new object();
		private static IPerfMeterMemorySnapshotBackend _backend;

		internal static void Register(IPerfMeterMemorySnapshotBackend backend)
		{
			if (backend == null)
			{
				throw new ArgumentNullException(nameof(backend));
			}

			lock (Sync)
			{
				if (_backend != null && !ReferenceEquals(_backend, backend))
				{
					throw new InvalidOperationException("A memory snapshot backend is already registered.");
				}

				_backend = backend;
			}
		}

		internal static void Unregister(IPerfMeterMemorySnapshotBackend backend)
		{
			if (backend == null)
			{
				return;
			}

			lock (Sync)
			{
				if (ReferenceEquals(_backend, backend))
				{
					_backend = null;
				}
			}
		}

		internal static bool TryGet(out IPerfMeterMemorySnapshotBackend backend, out string backendId, out string backendVersion, out PerfMeterMemoryCaptureFlags supportedFlags, out string error)
		{
			lock (Sync)
			{
				backend = _backend;
			}

			backendId = string.Empty;
			backendVersion = string.Empty;
			supportedFlags = PerfMeterMemoryCaptureFlags.None;
			error = string.Empty;
			if (backend == null)
			{
				error = "No memory snapshot backend is registered.";
				return false;
			}

			try
			{
				backendId = string.IsNullOrWhiteSpace(backend.Id) ? backend.GetType().FullName : backend.Id.Trim();
				backendVersion = string.IsNullOrWhiteSpace(backend.Version) ? string.Empty : backend.Version.Trim();
				supportedFlags = backend.SupportedCaptureFlags & PerfMeterMemorySnapshotCoordinator.AllCaptureFlags;
				if (supportedFlags == PerfMeterMemoryCaptureFlags.None)
				{
					error = "Memory snapshot backend '" + backendId + "' exposes no supported capture flags.";
					return false;
				}

				return true;
			}
			catch (Exception exception)
			{
				error = "Memory snapshot backend identity failed: " + exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		internal static void ClearForTests()
		{
			lock (Sync)
			{
				_backend = null;
			}
		}
	}

	internal interface IPerfMeterMemorySnapshotStorage
	{
		string RelativeRoot { get; }
		bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error);
		bool TryValidateCompleted(string path, string expectedPath, long maxBytes, out long sizeBytes, out string error);
		bool TryDelete(string path);
	}

	internal sealed class PerfMeterMemorySnapshotStorage : IPerfMeterMemorySnapshotStorage
	{
		internal const string RelativeSnapshotRoot = "Temp/PerfMeter/MemorySnapshots";
		private const string OwnedPrefix = ".sgg-perfmeter-memory-";
		private readonly string _projectRoot;
		private readonly string _snapshotRoot;

		internal PerfMeterMemorySnapshotStorage(string projectRoot)
		{
			_projectRoot = Path.GetFullPath(projectRoot ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			_snapshotRoot = Path.GetFullPath(Path.Combine(_projectRoot, RelativeSnapshotRoot)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public string RelativeRoot => RelativeSnapshotRoot;

		public bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error)
		{
			path = string.Empty;
			availableFreeDiskBytes = 0L;
			error = string.Empty;
			try
			{
				Directory.CreateDirectory(_snapshotRoot);
				if (!IsSafePath(_snapshotRoot))
				{
					error = "memory_snapshot_root_must_not_use_reparse_points";
					return false;
				}

				string driveRoot = Path.GetPathRoot(_snapshotRoot);
				availableFreeDiskBytes = string.IsNullOrEmpty(driveRoot) ? 0L : new DriveInfo(driveRoot).AvailableFreeSpace;
				if (availableFreeDiskBytes < minimumFreeDiskBytes)
				{
					error = "insufficient_free_disk_space";
					return false;
				}

				path = Path.Combine(_snapshotRoot, OwnedPrefix + Guid.NewGuid().ToString("N") + ".snap");
				return true;
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				error = "memory_snapshot_storage_error: " + exception.Message;
				return false;
			}
		}

		public bool TryValidateCompleted(string path, string expectedPath, long maxBytes, out long sizeBytes, out string error)
		{
			sizeBytes = 0L;
			error = string.Empty;
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				string expectedFullPath = Path.GetFullPath(expectedPath ?? string.Empty);
				if (!string.Equals(fullPath, expectedFullPath, PathComparison) || !IsOwnedSnapshotPath(fullPath) || !File.Exists(fullPath) || !IsSafePath(fullPath))
				{
					error = "memory_snapshot_backend_returned_unowned_path";
					return false;
				}

				FileInfo info = new FileInfo(fullPath);
				if ((info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 || info.Length <= 0L)
				{
					error = "memory_snapshot_artifact_is_not_a_regular_nonempty_file";
					return false;
				}

				if (info.Length > maxBytes)
				{
					error = "memory_snapshot_size_limit_exceeded";
					return false;
				}

				sizeBytes = info.Length;
				return true;
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				error = "memory_snapshot_validation_error: " + exception.Message;
				return false;
			}
		}

		public bool TryDelete(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				if (!IsOwnedSnapshotPath(fullPath))
				{
					return false;
				}

				if (!File.Exists(fullPath))
				{
					return true;
				}

				if (!IsSafePath(fullPath) || (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}

				File.Delete(fullPath);
				return !File.Exists(fullPath);
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				return false;
			}
		}

		private bool IsOwnedSnapshotPath(string path)
		{
			string normalizedRoot = _snapshotRoot + Path.DirectorySeparatorChar;
			return path.StartsWith(normalizedRoot, PathComparison) &&
				string.Equals(Path.GetDirectoryName(path), _snapshotRoot, PathComparison) &&
				Path.GetFileName(path).StartsWith(OwnedPrefix, StringComparison.Ordinal) &&
				string.Equals(Path.GetExtension(path), ".snap", StringComparison.OrdinalIgnoreCase);
		}

		private bool IsSafePath(string path)
		{
			string current = Path.GetFullPath(path);
			while (!string.IsNullOrEmpty(current) && current.Length >= _projectRoot.Length)
			{
				if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}

				if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), _projectRoot, PathComparison))
				{
					return true;
				}

				current = Path.GetDirectoryName(current);
			}

			return false;
		}

		private static StringComparison PathComparison => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		private static bool IsPathOrIoException(Exception exception)
		{
			return exception is ArgumentException ||
				exception is IOException ||
				exception is UnauthorizedAccessException ||
				exception is NotSupportedException ||
				exception is System.Security.SecurityException;
		}
	}

	internal sealed class PerfMeterMemorySnapshotCoordinator
	{
		internal const long MaxSnapshotBytes = 512L * 1024L * 1024L;
		internal const PerfMeterMemoryCaptureFlags AllCaptureFlags =
			PerfMeterMemoryCaptureFlags.ManagedObjects |
			PerfMeterMemoryCaptureFlags.NativeObjects |
			PerfMeterMemoryCaptureFlags.NativeAllocations |
			PerfMeterMemoryCaptureFlags.NativeAllocationSites |
			PerfMeterMemoryCaptureFlags.NativeStackTraces;

		private readonly object _sync = new object();
		private readonly IPerfMeterMemorySnapshotStorage _storage;
		private readonly Func<double> _nowSeconds;
		private PerfMeterMemorySnapshotStatusSnapshot _status = PerfMeterMemorySnapshotStatusSnapshot.NotRunning;
		private PerfMeterMemorySnapshotArtifact _artifact;
		private string _pendingPath = string.Empty;
		private bool _completionPending;
		private bool _cleanupBlocked;
		private bool _requestPreparing;
		private string _preparingCaptureId = string.Empty;
		private int _generation;
		private double _lastStartedTimeSeconds = double.NegativeInfinity;
		private double _cooldownSeconds;

		internal PerfMeterMemorySnapshotCoordinator(IPerfMeterMemorySnapshotStorage storage, Func<double> nowSeconds = null)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_nowSeconds = nowSeconds ?? (() => Time.realtimeSinceStartupAsDouble);
			_status = IdleStatus();
		}

		internal PerfMeterMemorySnapshotStatusSnapshot GetStatus(double nowSeconds)
		{
			lock (_sync)
			{
				double remaining = _status.State == PerfMeterMemorySnapshotState.Capturing
					? 0d
					: Math.Max(0d, GetCooldownEnd() - nowSeconds);
				return WithCooldown(_status, remaining);
			}
		}

		internal bool CleanupBlocked
		{
			get
			{
				lock (_sync)
				{
					return _cleanupBlocked;
				}
			}
		}

		internal PerfMeterMemorySnapshotRequestResult Request(PerfMeterMemorySnapshotOptions options, double nowSeconds)
		{
			if (!IsValidOptions(options))
			{
				return PerfMeterMemorySnapshotRequestResult.InvalidRequest;
			}

			IPerfMeterMemorySnapshotBackend backend = null;
			string backendId = string.Empty;
			string backendVersion = string.Empty;
			string path = string.Empty;
			int generation = 0;
			PerfMeterMemorySnapshotRequestResult immediateResult = PerfMeterMemorySnapshotRequestResult.Started;
			lock (_sync)
			{
				if (_status.State == PerfMeterMemorySnapshotState.Capturing || _completionPending || _requestPreparing)
				{
					string activeCaptureId = _requestPreparing ? _preparingCaptureId : _status.CaptureId;
					return string.Equals(activeCaptureId, options.CaptureId, StringComparison.Ordinal)
						? PerfMeterMemorySnapshotRequestResult.AlreadyActive
						: PerfMeterMemorySnapshotRequestResult.RejectedOverlap;
				}

				if (nowSeconds < GetCooldownEnd())
				{
					return PerfMeterMemorySnapshotRequestResult.Cooldown;
				}

				_requestPreparing = true;
				_preparingCaptureId = options.CaptureId;
				try
				{
					if (_artifact.IsAvailable && !_storage.TryDelete(_artifact.SourcePath))
					{
						_cleanupBlocked = true;
						_status = WithWarning(_status, "Previous memory snapshot could not be deleted; the replacement request was rejected.");
						return PerfMeterMemorySnapshotRequestResult.RejectedOverlap;
					}

					_cleanupBlocked = false;
					_artifact = default;
					if (!PerfMeterMemorySnapshotBackendRegistry.TryGet(out backend, out backendId, out backendVersion, out PerfMeterMemoryCaptureFlags supportedFlags, out string registryError))
					{
						SetTerminalLocked(options, PerfMeterMemorySnapshotState.Unavailable, backendId, backendVersion, nowSeconds, registryError);
						immediateResult = PerfMeterMemorySnapshotRequestResult.Unavailable;
					}
					else if ((options.CaptureFlags & supportedFlags) != options.CaptureFlags)
					{
						SetTerminalLocked(options, PerfMeterMemorySnapshotState.Unavailable, backendId, backendVersion, nowSeconds, "The registered memory snapshot backend does not support all requested capture flags.");
						immediateResult = PerfMeterMemorySnapshotRequestResult.Unavailable;
					}
					else if (!_storage.TryPrepare(options.MinimumFreeDiskBytes, out path, out _, out string storageError))
					{
						PerfMeterMemorySnapshotState state = string.Equals(storageError, "insufficient_free_disk_space", StringComparison.Ordinal)
							? PerfMeterMemorySnapshotState.Unavailable
							: PerfMeterMemorySnapshotState.Error;
						SetTerminalLocked(options, state, backendId, backendVersion, nowSeconds, storageError);
						immediateResult = string.Equals(storageError, "insufficient_free_disk_space", StringComparison.Ordinal)
							? PerfMeterMemorySnapshotRequestResult.InsufficientDiskSpace
							: PerfMeterMemorySnapshotRequestResult.Failed;
					}
					else
					{
						_generation++;
						generation = _generation;
						_lastStartedTimeSeconds = nowSeconds;
						_cooldownSeconds = options.CooldownSeconds;
						_pendingPath = path;
						_completionPending = false;
						_status = new PerfMeterMemorySnapshotStatusSnapshot(
							PerfMeterAvailability.Available,
							PerfMeterMemorySnapshotState.Capturing,
							options.CaptureId,
							options.Trigger,
							options.CaptureFlags,
							backendId,
							backendVersion,
							nowSeconds,
							0d,
							0L,
							0d,
							string.Empty);
					}
				}
				finally
				{
					_requestPreparing = false;
					_preparingCaptureId = string.Empty;
				}
			}

			if (immediateResult != PerfMeterMemorySnapshotRequestResult.Started)
			{
				return immediateResult;
			}

			try
			{
				bool started = backend.TryCapture(path, options.CaptureFlags, result => Complete(generation, options, result), out string error);
				if (!started)
				{
					FailIfCapturing(generation, options, backendId, backendVersion, nowSeconds, string.IsNullOrEmpty(error) ? "Memory snapshot backend rejected the request." : error);
					return PerfMeterMemorySnapshotRequestResult.Failed;
				}
			}
			catch (Exception exception)
			{
				FailIfCapturing(generation, options, backendId, backendVersion, nowSeconds, exception.GetType().Name + ": " + exception.Message);
				return PerfMeterMemorySnapshotRequestResult.Failed;
			}

			return PerfMeterMemorySnapshotRequestResult.Started;
		}

		internal bool TryConsumeCompletion(out PerfMeterMemorySnapshotStatusSnapshot status, out PerfMeterMemorySnapshotArtifact artifact)
		{
			lock (_sync)
			{
				if (!_completionPending)
				{
					status = default;
					artifact = default;
					return false;
				}

				_completionPending = false;
				status = _status;
				artifact = _artifact;
				return true;
			}
		}

		internal void Shutdown(double nowSeconds, string warning)
		{
			string pendingPath = string.Empty;
			lock (_sync)
			{
				_generation++;
				if (_status.State == PerfMeterMemorySnapshotState.Capturing)
				{
					pendingPath = _pendingPath;
					_pendingPath = string.Empty;
					_status = new PerfMeterMemorySnapshotStatusSnapshot(
						PerfMeterAvailability.Unavailable,
						PerfMeterMemorySnapshotState.Error,
						_status.CaptureId,
						_status.Trigger,
						_status.RequestedCaptureFlags,
						_status.BackendId,
						_status.BackendVersion,
						_status.StartedTimeSeconds,
						nowSeconds,
						0L,
						0d,
						warning);
					_artifact = default;
					_completionPending = true;
				}
			}

			if (!string.IsNullOrEmpty(pendingPath))
			{
				TryDeleteOrRecordFailure(pendingPath, "Interrupted memory snapshot could not be deleted.");
			}
		}

		internal bool HasArtifact(string path)
		{
			lock (_sync)
			{
				return _artifact.IsAvailable && string.Equals(path, _artifact.SourcePath, StringComparison.Ordinal);
			}
		}

		internal bool DiscardArtifact(string expectedPath = null)
		{
			lock (_sync)
			{
				string path;
				if (_artifact.IsAvailable && (string.IsNullOrEmpty(expectedPath) || string.Equals(expectedPath, _artifact.SourcePath, StringComparison.Ordinal)))
				{
					path = _artifact.SourcePath;
				}
				else
				{
					path = expectedPath ?? string.Empty;
				}

				if (string.IsNullOrEmpty(path) || _storage.TryDelete(path))
				{
					_cleanupBlocked = false;
					if (_artifact.IsAvailable && (string.IsNullOrEmpty(expectedPath) || string.Equals(expectedPath, _artifact.SourcePath, StringComparison.Ordinal)))
					{
						_artifact = default;
					}

					return true;
				}

				_status = WithWarning(_status, "Memory snapshot artifact could not be deleted.");
				_cleanupBlocked = true;
				return false;
			}
		}

		private void Complete(int generation, PerfMeterMemorySnapshotOptions options, PerfMeterMemorySnapshotBackendResult result)
		{
			string pendingPath;
			bool deleteStaleResult;
			lock (_sync)
			{
				if (generation != _generation || _status.State != PerfMeterMemorySnapshotState.Capturing)
				{
					deleteStaleResult = ShouldDeleteStaleResultLocked(result.Path);
					pendingPath = string.Empty;
				}
				else
				{
					deleteStaleResult = false;
					pendingPath = _pendingPath;
				}
			}

			if (string.IsNullOrEmpty(pendingPath))
			{
				if (deleteStaleResult)
				{
					TryDeleteOrRecordFailure(result.Path, "Stale memory snapshot result could not be deleted.");
				}

				return;
			}

			long sizeBytes = 0L;
			string validationError = result.Error;
			bool valid = result.Success && _storage.TryValidateCompleted(result.Path, pendingPath, MaxSnapshotBytes, out sizeBytes, out validationError);
			lock (_sync)
			{
				if (generation != _generation || _status.State != PerfMeterMemorySnapshotState.Capturing)
				{
					deleteStaleResult = ShouldDeleteStaleResultLocked(result.Path);
				}
				else
				{
					deleteStaleResult = false;
					double completedTime = Math.Max(_status.StartedTimeSeconds, _nowSeconds());
					_status = new PerfMeterMemorySnapshotStatusSnapshot(
						valid ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
						valid ? PerfMeterMemorySnapshotState.Completed : PerfMeterMemorySnapshotState.Error,
						options.CaptureId,
						options.Trigger,
						options.CaptureFlags,
						_status.BackendId,
						_status.BackendVersion,
						_status.StartedTimeSeconds,
						completedTime,
						sizeBytes,
						0d,
						valid ? string.Empty : (string.IsNullOrEmpty(validationError) ? "Memory snapshot capture failed." : validationError));
					_artifact = valid ? new PerfMeterMemorySnapshotArtifact(_status, pendingPath) : default;
					_pendingPath = string.Empty;
					_completionPending = true;
				}
			}

			if (deleteStaleResult)
			{
				TryDeleteOrRecordFailure(result.Path, "Stale memory snapshot result could not be deleted.");
				return;
			}

			if (!valid)
			{
				TryDeleteOrRecordFailure(pendingPath, "Invalid memory snapshot artifact could not be deleted.");
			}
		}

		private void FailIfCapturing(int generation, PerfMeterMemorySnapshotOptions options, string backendId, string backendVersion, double nowSeconds, string warning)
		{
			string pendingPath;
			lock (_sync)
			{
				if (generation != _generation || _status.State != PerfMeterMemorySnapshotState.Capturing)
				{
					return;
				}

				pendingPath = _pendingPath;
				SetTerminalLocked(options, PerfMeterMemorySnapshotState.Error, backendId, backendVersion, nowSeconds, warning);
			}

			TryDeleteOrRecordFailure(pendingPath, "Failed memory snapshot artifact could not be deleted.");
		}

		private void SetTerminalLocked(PerfMeterMemorySnapshotOptions options, PerfMeterMemorySnapshotState state, string backendId, string backendVersion, double nowSeconds, string warning)
		{
			_pendingPath = string.Empty;
			_status = new PerfMeterMemorySnapshotStatusSnapshot(
				PerfMeterAvailability.Unavailable,
				state,
				options.CaptureId,
				options.Trigger,
				options.CaptureFlags,
				backendId,
				backendVersion,
				nowSeconds,
				nowSeconds,
				0L,
				0d,
				warning);
			_artifact = default;
			_completionPending = true;
		}

		private bool ShouldDeleteStaleResultLocked(string path)
		{
			return !string.IsNullOrEmpty(path) && (!_artifact.IsAvailable || !string.Equals(path, _artifact.SourcePath, StringComparison.Ordinal));
		}

		private void TryDeleteOrRecordFailure(string path, string warning)
		{
			if (_storage.TryDelete(path))
			{
				return;
			}

			lock (_sync)
			{
				_cleanupBlocked = true;
				_status = WithWarning(_status, warning);
			}
		}

		private double GetCooldownEnd()
		{
			return double.IsNegativeInfinity(_lastStartedTimeSeconds) ? 0d : _lastStartedTimeSeconds + _cooldownSeconds;
		}

		private static PerfMeterMemorySnapshotStatusSnapshot WithCooldown(PerfMeterMemorySnapshotStatusSnapshot status, double remaining)
		{
			return new PerfMeterMemorySnapshotStatusSnapshot(
				status.Availability,
				status.State,
				status.CaptureId,
				status.Trigger,
				status.RequestedCaptureFlags,
				status.BackendId,
				status.BackendVersion,
				status.StartedTimeSeconds,
				status.CompletedTimeSeconds,
				status.ArtifactSizeBytes,
				remaining,
				status.Warning);
		}

		private static PerfMeterMemorySnapshotStatusSnapshot WithWarning(PerfMeterMemorySnapshotStatusSnapshot status, string warning)
		{
			if (status.Warning.IndexOf(warning, StringComparison.Ordinal) >= 0)
			{
				return status;
			}

			string combined = string.IsNullOrEmpty(status.Warning) ? warning : status.Warning + " " + warning;
			return new PerfMeterMemorySnapshotStatusSnapshot(
				status.Availability,
				status.State,
				status.CaptureId,
				status.Trigger,
				status.RequestedCaptureFlags,
				status.BackendId,
				status.BackendVersion,
				status.StartedTimeSeconds,
				status.CompletedTimeSeconds,
				status.ArtifactSizeBytes,
				status.CooldownRemainingSeconds,
				combined);
		}

		private static PerfMeterMemorySnapshotStatusSnapshot IdleStatus()
		{
			return new PerfMeterMemorySnapshotStatusSnapshot(
				PerfMeterAvailability.Unknown,
				PerfMeterMemorySnapshotState.Idle,
				string.Empty,
				PerfMeterMemorySnapshotTrigger.Manual,
				PerfMeterMemoryCaptureFlags.None,
				string.Empty,
				string.Empty,
				0d,
				0d,
				0L,
				0d,
				string.Empty);
		}

		internal static bool IsValidOptions(PerfMeterMemorySnapshotOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.CaptureId) || options.CaptureId.Length > 128 || options.CaptureFlags == PerfMeterMemoryCaptureFlags.None || (options.CaptureFlags & ~AllCaptureFlags) != 0)
			{
				return false;
			}

			for (int i = 0; i < options.CaptureId.Length; i++)
			{
				if (char.IsControl(options.CaptureId[i]))
				{
					return false;
				}
			}

			return true;
		}
	}

	internal sealed class PerfMeterMemorySnapshotTriggerEvaluator
	{
		private int _baselineFrame = -1;
		private long _baselineBytes;

		internal bool TryEvaluate(PerfMeterMetricsSnapshot metrics, PerfMeterMemorySnapshotTriggerOptions options, out PerfMeterMemorySnapshotTrigger trigger)
		{
			trigger = PerfMeterMemorySnapshotTrigger.Manual;
			if (!options.Enabled)
			{
				Reset();
				return false;
			}

			if (options.SystemMemoryThresholdBytes > 0L && metrics.SystemUsedMemoryBytes >= options.SystemMemoryThresholdBytes)
			{
				trigger = PerfMeterMemorySnapshotTrigger.SystemMemoryThreshold;
				return true;
			}

			if (options.LeakGrowthThresholdBytes <= 0L)
			{
				return false;
			}

			if (_baselineFrame < 0 || metrics.CollectionFrame < _baselineFrame)
			{
				_baselineFrame = metrics.CollectionFrame;
				_baselineBytes = metrics.SystemUsedMemoryBytes;
				return false;
			}

			if (metrics.CollectionFrame - _baselineFrame < options.LeakWindowFrames)
			{
				return false;
			}

			long growth = metrics.SystemUsedMemoryBytes - _baselineBytes;
			_baselineFrame = metrics.CollectionFrame;
			_baselineBytes = metrics.SystemUsedMemoryBytes;
			if (growth >= options.LeakGrowthThresholdBytes)
			{
				trigger = PerfMeterMemorySnapshotTrigger.LeakGrowth;
				return true;
			}

			return false;
		}

		internal void Reset()
		{
			_baselineFrame = -1;
			_baselineBytes = 0L;
		}
	}
}
