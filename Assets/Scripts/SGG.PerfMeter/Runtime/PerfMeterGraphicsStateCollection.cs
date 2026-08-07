using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SGG.PerfMeter
{
	public enum PerfMeterGraphicsStateCollectionState
	{
		Idle = 0,
		Tracing = 1,
		Completed = 2,
		Prewarmed = 3,
		Unavailable = 4,
		Error = 5,
		Canceled = 6
	}

	public enum PerfMeterGraphicsStateCollectionRequestResult
	{
		Started = 0,
		AlreadyActive = 1,
		RejectedOverlap = 2,
		Unavailable = 3,
		InvalidRequest = 4,
		Failed = 5,
		Completed = 6
	}

	public readonly struct PerfMeterGraphicsStateTraceOptions
	{
		public const int DefaultTraceFrames = 60;
		public const long DefaultMinimumFreeDiskBytes = 1024L * 1024L * 1024L;

		public PerfMeterGraphicsStateTraceOptions(
			string captureId,
			int traceFrames = DefaultTraceFrames,
			long minimumFreeDiskBytes = DefaultMinimumFreeDiskBytes)
		{
			CaptureId = captureId ?? string.Empty;
			TraceFrames = traceFrames;
			MinimumFreeDiskBytes = Math.Max(0L, minimumFreeDiskBytes);
		}

		public string CaptureId { get; }
		public int TraceFrames { get; }
		public long MinimumFreeDiskBytes { get; }
	}

	public readonly struct PerfMeterGraphicsStatePrewarmOptions
	{
		public PerfMeterGraphicsStatePrewarmOptions(
			string relativePath,
			int maxStateCount = 0,
			bool traceCacheMisses = false)
		{
			RelativePath = relativePath ?? string.Empty;
			MaxStateCount = maxStateCount;
			TraceCacheMisses = traceCacheMisses;
		}

		public string RelativePath { get; }
		public int MaxStateCount { get; }
		public bool TraceCacheMisses { get; }
	}

	public readonly struct PerfMeterGraphicsStateTraceBackendResult
	{
		public PerfMeterGraphicsStateTraceBackendResult(bool success, int totalGraphicsStateCount, int variantCount)
		{
			Success = success;
			TotalGraphicsStateCount = Math.Max(0, totalGraphicsStateCount);
			VariantCount = Math.Max(0, variantCount);
		}

		public bool Success { get; }
		public int TotalGraphicsStateCount { get; }
		public int VariantCount { get; }
	}

	public readonly struct PerfMeterGraphicsStatePrewarmBackendResult
	{
		public PerfMeterGraphicsStatePrewarmBackendResult(
			bool success,
			int completedWarmupCount,
			int totalGraphicsStateCount,
			bool isWarmedUp)
		{
			Success = success;
			CompletedWarmupCount = Math.Max(0, completedWarmupCount);
			TotalGraphicsStateCount = Math.Max(0, totalGraphicsStateCount);
			IsWarmedUp = isWarmedUp;
		}

		public bool Success { get; }
		public int CompletedWarmupCount { get; }
		public int TotalGraphicsStateCount { get; }
		public bool IsWarmedUp { get; }
	}

	public interface IPerfMeterGraphicsStateCollectionBackend
	{
		string Id { get; }
		string Version { get; }
		bool SupportsCacheMissTracing { get; }
		bool SupportsParallelPsoCreation { get; }
		bool TryBeginTrace(out string error);
		bool TryEndTrace(
			string outputPath,
			out PerfMeterGraphicsStateTraceBackendResult result,
			out string error);
		void CancelTrace();
		bool TryPrewarm(
			string inputPath,
			int maxStateCount,
			bool traceCacheMisses,
			out PerfMeterGraphicsStatePrewarmBackendResult result,
			out string error);
	}

	public readonly struct PerfMeterGraphicsStateCollectionCapabilitiesSnapshot
	{
		public PerfMeterGraphicsStateCollectionCapabilitiesSnapshot(
			PerfMeterAvailability availability,
			string backendId,
			string backendVersion,
			bool supportsTrace,
			bool supportsPrewarm,
			bool supportsCacheMissTracing,
			bool supportsParallelPsoCreation,
			int maxTraceFrames,
			long maxArtifactBytes,
			string artifactRoot,
			string warning)
		{
			Availability = availability;
			BackendId = backendId ?? string.Empty;
			BackendVersion = backendVersion ?? string.Empty;
			SupportsTrace = supportsTrace;
			SupportsPrewarm = supportsPrewarm;
			SupportsCacheMissTracing = supportsCacheMissTracing;
			SupportsParallelPsoCreation = supportsParallelPsoCreation;
			MaxTraceFrames = Math.Max(0, maxTraceFrames);
			MaxArtifactBytes = Math.Max(0L, maxArtifactBytes);
			ArtifactRoot = artifactRoot ?? string.Empty;
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterGraphicsStateCollectionCapabilitiesSnapshot Unavailable => new PerfMeterGraphicsStateCollectionCapabilitiesSnapshot(
			PerfMeterAvailability.Unavailable,
			string.Empty,
			string.Empty,
			false,
			false,
			false,
			false,
			PerfMeterGraphicsStateCollectionCoordinator.MaxTraceFrames,
			PerfMeterGraphicsStateCollectionCoordinator.MaxArtifactBytes,
			PerfMeterGraphicsStateCollectionStorage.RelativeGraphicsStateCollectionRoot,
			"No graphics state collection backend is registered.");

		public PerfMeterAvailability Availability { get; }
		public string BackendId { get; }
		public string BackendVersion { get; }
		public bool SupportsTrace { get; }
		public bool SupportsPrewarm { get; }
		public bool SupportsCacheMissTracing { get; }
		public bool SupportsParallelPsoCreation { get; }
		public bool RequiresSessionRecording => true;
		public int MaxTraceFrames { get; }
		public long MaxArtifactBytes { get; }
		public string ArtifactRoot { get; }
		public string Warning { get; }
	}

	public readonly struct PerfMeterGraphicsStateCollectionStatusSnapshot
	{
		public PerfMeterGraphicsStateCollectionStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterGraphicsStateCollectionState state,
			string captureId,
			int requestedTraceFrames,
			int completedTraceFrames,
			string backendId,
			string backendVersion,
			string artifactRelativePath,
			long artifactSizeBytes,
			int totalGraphicsStateCount,
			int variantCount,
			int completedWarmupCount,
			string warning,
			bool isWarmedUp = false,
			bool isBusy = false,
			bool hasPendingCleanup = false)
		{
			Availability = availability;
			State = state;
			CaptureId = captureId ?? string.Empty;
			RequestedTraceFrames = Math.Max(0, requestedTraceFrames);
			CompletedTraceFrames = Math.Max(0, completedTraceFrames);
			BackendId = backendId ?? string.Empty;
			BackendVersion = backendVersion ?? string.Empty;
			ArtifactRelativePath = artifactRelativePath ?? string.Empty;
			ArtifactSizeBytes = Math.Max(0L, artifactSizeBytes);
			TotalGraphicsStateCount = Math.Max(0, totalGraphicsStateCount);
			VariantCount = Math.Max(0, variantCount);
			CompletedWarmupCount = Math.Max(0, completedWarmupCount);
			Warning = warning ?? string.Empty;
			IsWarmedUp = isWarmedUp;
			IsBusy = isBusy;
			HasPendingCleanup = hasPendingCleanup;
		}

		public static PerfMeterGraphicsStateCollectionStatusSnapshot Idle => new PerfMeterGraphicsStateCollectionStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterGraphicsStateCollectionState.Idle,
			string.Empty,
			0,
			0,
			string.Empty,
			string.Empty,
			string.Empty,
			0L,
			0,
			0,
			0,
			string.Empty);

		public bool IsActive => State == PerfMeterGraphicsStateCollectionState.Tracing;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterGraphicsStateCollectionState State { get; }
		public string CaptureId { get; }
		public int RequestedTraceFrames { get; }
		public int CompletedTraceFrames { get; }
		public string BackendId { get; }
		public string BackendVersion { get; }
		public string ArtifactRelativePath { get; }
		public long ArtifactSizeBytes { get; }
		public int TotalGraphicsStateCount { get; }
		public int VariantCount { get; }
		public int CompletedWarmupCount { get; }
		public string Warning { get; }
		public bool IsWarmedUp { get; }
		public bool IsBusy { get; }
		public bool HasPendingCleanup { get; }
	}

	internal static class PerfMeterGraphicsStateCollectionBackendRegistry
	{
		private static readonly object Sync = new object();
		private static IPerfMeterGraphicsStateCollectionBackend _backend;

		internal static void Register(IPerfMeterGraphicsStateCollectionBackend backend)
		{
			if (backend == null)
			{
				throw new ArgumentNullException(nameof(backend));
			}

			lock (Sync)
			{
				if (_backend != null && !ReferenceEquals(_backend, backend))
				{
					throw new InvalidOperationException("A graphics state collection backend is already registered.");
				}

				_backend = backend;
			}
		}

		internal static void Unregister(IPerfMeterGraphicsStateCollectionBackend backend)
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

		internal static bool TryGet(
			out IPerfMeterGraphicsStateCollectionBackend backend,
			out string backendId,
			out string backendVersion,
			out bool supportsCacheMissTracing,
			out bool supportsParallelPsoCreation,
			out string error)
		{
			lock (Sync)
			{
				backend = _backend;
			}

			backendId = string.Empty;
			backendVersion = string.Empty;
			supportsCacheMissTracing = false;
			supportsParallelPsoCreation = false;
			error = string.Empty;
			if (backend == null)
			{
				error = "No graphics state collection backend is registered.";
				return false;
			}

			try
			{
				backendId = string.IsNullOrWhiteSpace(backend.Id) ? backend.GetType().FullName : backend.Id.Trim();
				backendVersion = string.IsNullOrWhiteSpace(backend.Version) ? string.Empty : backend.Version.Trim();
				supportsCacheMissTracing = backend.SupportsCacheMissTracing;
				supportsParallelPsoCreation = backend.SupportsParallelPsoCreation;
				if (string.IsNullOrWhiteSpace(backendId))
				{
					error = "Graphics state collection backend identity is empty.";
					return false;
				}

				return true;
			}
			catch (Exception exception)
			{
				error = "Graphics state collection backend identity failed: " + exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		internal static PerfMeterGraphicsStateCollectionCapabilitiesSnapshot GetCapabilities()
		{
			if (!TryGet(
				out _,
				out string backendId,
				out string backendVersion,
				out bool supportsCacheMissTracing,
				out bool supportsParallelPsoCreation,
				out string error))
			{
				return new PerfMeterGraphicsStateCollectionCapabilitiesSnapshot(
					PerfMeterAvailability.Unavailable,
					backendId,
					backendVersion,
					false,
					false,
					false,
					false,
					PerfMeterGraphicsStateCollectionCoordinator.MaxTraceFrames,
					PerfMeterGraphicsStateCollectionCoordinator.MaxArtifactBytes,
					PerfMeterGraphicsStateCollectionStorage.RelativeGraphicsStateCollectionRoot,
					error);
			}

			return new PerfMeterGraphicsStateCollectionCapabilitiesSnapshot(
				PerfMeterAvailability.Available,
				backendId,
				backendVersion,
				true,
				true,
				supportsCacheMissTracing,
				supportsParallelPsoCreation,
				PerfMeterGraphicsStateCollectionCoordinator.MaxTraceFrames,
				PerfMeterGraphicsStateCollectionCoordinator.MaxArtifactBytes,
				PerfMeterGraphicsStateCollectionStorage.RelativeGraphicsStateCollectionRoot,
				string.Empty);
		}

		internal static void ClearForTests()
		{
			lock (Sync)
			{
				_backend = null;
			}
		}
	}

	internal interface IPerfMeterGraphicsStateCollectionStorage
	{
		string RelativeRoot { get; }
		bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error);
		bool TryValidateCompleted(string path, string expectedPath, long maxBytes, out long sizeBytes, out string error);
		bool TryResolvePrewarmInput(string relativePath, long maxBytes, out string path, out long sizeBytes, out string error);
		bool TryDelete(string path);
		string[] GetPendingCleanupPaths();
		bool TryMarkPendingCleanup(string path);
		bool TryClearPendingCleanup(string path);
		string GetRelativePath(string path);
	}

	internal sealed class PerfMeterGraphicsStateCollectionStorage : IPerfMeterGraphicsStateCollectionStorage
	{
		internal const string RelativeGraphicsStateCollectionRoot = "Temp/PerfMeter/GraphicsStateCollections";
		private const string OwnedPrefix = ".sgg-perfmeter-graphics-";
		private const string OwnedExtension = ".graphicsstate";
		private const string PendingCleanupExtension = ".delete-pending";
		private readonly string _projectRoot;
		private readonly string _graphicsStateCollectionRoot;

		internal PerfMeterGraphicsStateCollectionStorage(string projectRoot)
		{
			_projectRoot = NormalizeDirectory(Path.GetFullPath(projectRoot ?? string.Empty));
			_graphicsStateCollectionRoot = NormalizeDirectory(Path.GetFullPath(Path.Combine(_projectRoot, RelativeGraphicsStateCollectionRoot)));
		}

		public string RelativeRoot => RelativeGraphicsStateCollectionRoot;

		public bool TryPrepare(long minimumFreeDiskBytes, out string path, out long availableFreeDiskBytes, out string error)
		{
			path = string.Empty;
			availableFreeDiskBytes = 0L;
			error = string.Empty;
			try
			{
				if (!IsSafePath(_graphicsStateCollectionRoot))
				{
					error = "graphics_state_collection_root_must_not_use_reparse_points";
					return false;
				}

				Directory.CreateDirectory(_graphicsStateCollectionRoot);
				if (!IsSafePath(_graphicsStateCollectionRoot))
				{
					error = "graphics_state_collection_root_must_not_use_reparse_points";
					return false;
				}

				string driveRoot = Path.GetPathRoot(_graphicsStateCollectionRoot);
				if (string.IsNullOrEmpty(driveRoot))
				{
					error = "graphics_state_collection_storage_drive_unavailable";
					return false;
				}

				availableFreeDiskBytes = new DriveInfo(driveRoot).AvailableFreeSpace;
				if (availableFreeDiskBytes < Math.Max(0L, minimumFreeDiskBytes))
				{
					error = "insufficient_free_disk_space";
					return false;
				}

				path = Path.Combine(_graphicsStateCollectionRoot, OwnedPrefix + Guid.NewGuid().ToString("N") + OwnedExtension);
				return true;
			}
			catch (Exception exception)
			{
				error = "graphics_state_collection_storage_error: " + exception.Message;
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
				if (!PathsEqual(fullPath, expectedFullPath) || !IsOwnedArtifactPath(fullPath) || !IsSafePath(fullPath))
				{
					error = "graphics_state_collection_backend_returned_unowned_path";
					return false;
				}

				return TryValidateRegularFile(fullPath, maxBytes, out sizeBytes, out error);
			}
			catch (Exception exception)
			{
				error = "graphics_state_collection_validation_error: " + exception.Message;
				return false;
			}
		}

		public bool TryResolvePrewarmInput(string relativePath, long maxBytes, out string path, out long sizeBytes, out string error)
		{
			path = string.Empty;
			sizeBytes = 0L;
			error = string.Empty;
			try
			{
				if (!TryResolveOwnedRelativePath(relativePath, out path, out error))
				{
					return false;
				}

				return TryValidateRegularFile(path, maxBytes, out sizeBytes, out error);
			}
			catch (Exception exception)
			{
				path = string.Empty;
				sizeBytes = 0L;
				error = "graphics_state_collection_input_validation_error: " + exception.Message;
				return false;
			}
		}

		public bool TryDelete(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				if (!IsOwnedArtifactPath(fullPath))
				{
					return false;
				}

				FileAttributes attributes;
				try
				{
					attributes = File.GetAttributes(fullPath);
				}
				catch (FileNotFoundException)
				{
					return !DirectoryEntryExists(fullPath) && IsSafePath(fullPath);
				}
				catch (DirectoryNotFoundException)
				{
					return !DirectoryEntryExists(fullPath) && IsSafePath(fullPath);
				}

				if ((attributes & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}

				if ((attributes & FileAttributes.Directory) != 0)
				{
					return false;
				}

				if (!IsSafePath(fullPath))
				{
					return false;
				}

				File.Delete(fullPath);
				return !File.Exists(fullPath);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public string[] GetPendingCleanupPaths()
		{
			List<string> paths = new List<string>();
			try
			{
				if (!Directory.Exists(_graphicsStateCollectionRoot) || !IsSafePath(_graphicsStateCollectionRoot))
				{
					return paths.ToArray();
				}

				foreach (string markerPath in Directory.EnumerateFiles(
					_graphicsStateCollectionRoot,
					OwnedPrefix + "*" + OwnedExtension + PendingCleanupExtension,
					SearchOption.TopDirectoryOnly))
				{
					if (!IsSafePath(markerPath) || (File.GetAttributes(markerPath) & FileAttributes.ReparsePoint) != 0)
					{
						continue;
					}

					string artifactPath = markerPath.Substring(0, markerPath.Length - PendingCleanupExtension.Length);
					if (IsOwnedArtifactPath(artifactPath))
					{
						paths.Add(artifactPath);
					}
				}
			}
			catch (Exception)
			{
			}

			return paths.ToArray();
		}

		public bool TryMarkPendingCleanup(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				if (!IsOwnedArtifactPath(fullPath) || !IsSafePath(fullPath))
				{
					return false;
				}

				Directory.CreateDirectory(_graphicsStateCollectionRoot);
				string markerPath = fullPath + PendingCleanupExtension;
				File.WriteAllText(markerPath, string.Empty);
				return IsSafePath(markerPath) && (File.GetAttributes(markerPath) & FileAttributes.ReparsePoint) == 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool TryClearPendingCleanup(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				if (!IsOwnedArtifactPath(fullPath))
				{
					return false;
				}

				string markerPath = fullPath + PendingCleanupExtension;
				if (!File.Exists(markerPath))
				{
					return !DirectoryEntryExists(markerPath) && IsSafePath(markerPath);
				}

				if (!IsSafePath(markerPath) || (File.GetAttributes(markerPath) & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}

				File.Delete(markerPath);
				return !File.Exists(markerPath);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public string GetRelativePath(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path ?? string.Empty);
				return IsOwnedArtifactPath(fullPath) ? RelativeGraphicsStateCollectionRoot + "/" + Path.GetFileName(fullPath) : string.Empty;
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		private bool TryResolveOwnedRelativePath(string relativePath, out string path, out string error)
		{
			path = string.Empty;
			error = string.Empty;
			if (string.IsNullOrWhiteSpace(relativePath) || IsRootedInput(relativePath) || ContainsTraversal(relativePath))
			{
				error = "graphics_state_collection_input_path_must_be_owned_relative_path";
				return false;
			}

			string normalizedRelativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
			path = Path.GetFullPath(Path.Combine(_projectRoot, normalizedRelativePath));
			if (!IsOwnedArtifactPath(path) || !IsSafePath(path))
			{
				path = string.Empty;
				error = "graphics_state_collection_input_path_must_be_owned_relative_path";
				return false;
			}

			return true;
		}

		private bool TryValidateRegularFile(string fullPath, long maxBytes, out long sizeBytes, out string error)
		{
			sizeBytes = 0L;
			error = string.Empty;
			if (!File.Exists(fullPath))
			{
				error = "graphics_state_collection_artifact_not_found";
				return false;
			}

			FileInfo info = new FileInfo(fullPath);
			if ((info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 || info.Length <= 0L)
			{
				error = "graphics_state_collection_artifact_is_not_a_regular_nonempty_file";
				return false;
			}

			if (info.Length > Math.Max(0L, maxBytes))
			{
				error = "graphics_state_collection_size_limit_exceeded";
				return false;
			}

			sizeBytes = info.Length;
			return true;
		}

		private bool IsOwnedArtifactPath(string path)
		{
			string fullPath = NormalizeFullPath(path);
			string rootPrefix = _graphicsStateCollectionRoot + Path.DirectorySeparatorChar;
			string fileName = Path.GetFileName(fullPath);
			string guidPart = fileName.Length > OwnedPrefix.Length + OwnedExtension.Length
				? fileName.Substring(OwnedPrefix.Length, fileName.Length - OwnedPrefix.Length - OwnedExtension.Length)
				: string.Empty;
			return fullPath.StartsWith(rootPrefix, PathComparison) &&
				PathsEqual(Path.GetDirectoryName(fullPath), _graphicsStateCollectionRoot) &&
				fileName.StartsWith(OwnedPrefix, StringComparison.Ordinal) &&
				string.Equals(Path.GetExtension(fullPath), OwnedExtension, StringComparison.OrdinalIgnoreCase) &&
				Guid.TryParseExact(guidPart, "N", out _);
		}

		private bool IsSafePath(string path)
		{
			string current = NormalizeFullPath(path);
			while (!string.IsNullOrEmpty(current))
			{
				if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}

				if (PathsEqual(current, _projectRoot))
				{
					return true;
				}

				string parent = Path.GetDirectoryName(current);
				if (string.IsNullOrEmpty(parent) || PathsEqual(parent, current))
				{
					return false;
				}

				current = parent;
			}

			return false;
		}

		private static bool DirectoryEntryExists(string path)
		{
			string directory = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
			{
				return false;
			}

			string fileName = Path.GetFileName(path);
			foreach (string entry in Directory.EnumerateFileSystemEntries(directory, fileName, SearchOption.TopDirectoryOnly))
			{
				if (PathsEqual(entry, path))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsRootedInput(string path)
		{
			return Path.IsPathRooted(path) ||
				path[0] == '/' ||
				path[0] == '\\' ||
				(path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');
		}

		private static bool ContainsTraversal(string path)
		{
			string[] parts = path.Replace('\\', '/').Split('/');
			for (int i = 0; i < parts.Length; i++)
			{
				if (string.Equals(parts[i], ".", StringComparison.Ordinal) || string.Equals(parts[i], "..", StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private static string NormalizeFullPath(string path)
		{
			return Path.GetFullPath(path ?? string.Empty);
		}

		private static string NormalizeDirectory(string path)
		{
			string fullPath = NormalizeFullPath(path);
			string root = Path.GetPathRoot(fullPath);
			if (string.IsNullOrEmpty(root) || fullPath.Length <= root.Length)
			{
				return fullPath;
			}

			return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		private static bool PathsEqual(string left, string right)
		{
			return string.Equals(left ?? string.Empty, right ?? string.Empty, PathComparison);
		}

		private static StringComparison PathComparison => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
	}

	internal sealed class PerfMeterGraphicsStateCollectionCoordinator
	{
		internal const int MaxTraceFrames = 600;
		internal const int MaxPrewarmStateCount = 1000000;
		internal const long MaxArtifactBytes = 64L * 1024L * 1024L;

		private readonly object _sync = new object();
		private readonly IPerfMeterGraphicsStateCollectionStorage _storage;
		private readonly List<string> _pendingCleanupPaths = new List<string>();
		private PerfMeterGraphicsStateCollectionStatusSnapshot _status;
		private IPerfMeterGraphicsStateCollectionBackend _activeBackend;
		private string _pendingPath = string.Empty;
		private string _artifactPath = string.Empty;
		private int _generation;
		private int _requestGeneration;
		private bool _requestPreparing;
		private string _preparingCaptureId = string.Empty;
		private bool _endingTrace;
		private bool _prewarming;
		private int _prewarmGeneration;
		private bool _cleanupInProgress;

		internal PerfMeterGraphicsStateCollectionCoordinator(IPerfMeterGraphicsStateCollectionStorage storage)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_status = PerfMeterGraphicsStateCollectionStatusSnapshot.Idle;
			string[] pendingCleanupPaths = _storage.GetPendingCleanupPaths() ?? Array.Empty<string>();
			for (int i = 0; i < pendingCleanupPaths.Length; i++)
			{
				if (!string.IsNullOrEmpty(pendingCleanupPaths[i]) && !_pendingCleanupPaths.Contains(pendingCleanupPaths[i]))
				{
					_pendingCleanupPaths.Add(pendingCleanupPaths[i]);
				}
			}

			if (_pendingCleanupPaths.Count > 0)
			{
				_status = CopyStatus(
					_status,
					availability: PerfMeterAvailability.Unavailable,
					state: PerfMeterGraphicsStateCollectionState.Error,
					warning: "Pending graphics state collection cleanup was restored after reload.");
			}
		}

		internal PerfMeterGraphicsStateCollectionStatusSnapshot GetStatus()
		{
			lock (_sync)
			{
				return CopyStatus(
					_status,
					isBusy: IsBusyLocked() || _pendingCleanupPaths.Count > 0,
					hasPendingCleanup: _pendingCleanupPaths.Count > 0);
			}
		}

		internal bool IsBusy
		{
			get
			{
				lock (_sync)
				{
					return IsBusyLocked() || _pendingCleanupPaths.Count > 0;
				}
			}
		}

		internal bool HasPendingCleanup
		{
			get
			{
				lock (_sync)
				{
					return _pendingCleanupPaths.Count > 0;
				}
			}
		}

		internal bool RetryPendingCleanup()
		{
			string[] paths;
			lock (_sync)
			{
				if (_cleanupInProgress || IsBusyLocked())
				{
					return false;
				}

				_cleanupInProgress = true;
				paths = _pendingCleanupPaths.ToArray();
			}

			bool succeeded = true;
			for (int i = 0; i < paths.Length; i++)
			{
				if (TryDeletePath(paths[i], out string cleanupError))
				{
					if (!RemovePendingCleanupPath(paths[i]))
					{
						succeeded = false;
					}
					lock (_sync)
					{
						if (PathsEqual(_artifactPath, paths[i]))
						{
							_artifactPath = string.Empty;
						}
					}
				}
				else
				{
					succeeded = false;
					AppendWarning(cleanupError);
				}
			}

			lock (_sync)
			{
				_cleanupInProgress = false;
			}
			return succeeded;
		}

		internal PerfMeterGraphicsStateCollectionRequestResult RequestTrace(PerfMeterGraphicsStateTraceOptions options)
		{
			return RequestTrace(options, out _);
		}

		internal PerfMeterGraphicsStateCollectionRequestResult RequestTrace(PerfMeterGraphicsStateTraceOptions options, out int startedGeneration)
		{
			startedGeneration = 0;
			if (!IsValidTraceOptions(options))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
			}

			if (HasPendingCleanup && (!RetryPendingCleanup() || HasPendingCleanup))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
			}

			int generation;
			lock (_sync)
			{
				if (HasActiveFlightLocked())
				{
					return GetOverlapResultLocked(options.CaptureId);
				}

				generation = ++_generation;
				_requestPreparing = true;
				_preparingCaptureId = options.CaptureId;
				_requestGeneration = generation;
			}

			try
			{
				if (!PerfMeterGraphicsStateCollectionBackendRegistry.TryGet(
					out IPerfMeterGraphicsStateCollectionBackend backend,
					out string backendId,
					out string backendVersion,
					out _,
					out _,
					out string registryError))
				{
					if (IsGenerationCurrent(generation))
					{
						SetUnavailableAttempt(options, registryError);
					}

					return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (!TryDeleteExistingArtifact(generation, out string cleanupError))
				{
					if (IsGenerationCurrent(generation))
					{
						AppendWarning(cleanupError);
					}

					return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				string path = string.Empty;
				try
				{
					if (!_storage.TryPrepare(options.MinimumFreeDiskBytes, out path, out _, out string storageError))
					{
						if (!string.IsNullOrEmpty(path))
						{
							bool deleted = TryDeleteDetachedPath(generation, path, out string prepareCleanupError);
							if (!deleted)
							{
								RememberUndeletedPath(path);
							}

							if (!string.IsNullOrEmpty(prepareCleanupError))
							{
								storageError = CombineWarnings(storageError, prepareCleanupError);
							}
						}

						string warning = string.IsNullOrEmpty(storageError)
							? "Graphics state collection storage rejected the trace artifact."
							: storageError;
						if (IsGenerationCurrent(generation))
						{
							SetTerminal(options, IsInsufficientDiskError(storageError) ? PerfMeterGraphicsStateCollectionState.Unavailable : PerfMeterGraphicsStateCollectionState.Error, PerfMeterAvailability.Unavailable, backendId, backendVersion, warning);
						}
						return IsInsufficientDiskError(storageError)
							? PerfMeterGraphicsStateCollectionRequestResult.Unavailable
							: PerfMeterGraphicsStateCollectionRequestResult.Failed;
					}

					if (string.IsNullOrEmpty(path))
					{
						if (IsGenerationCurrent(generation))
						{
							SetTerminal(options, PerfMeterGraphicsStateCollectionState.Error, PerfMeterAvailability.Unavailable, backendId, backendVersion, "Graphics state collection storage returned an empty artifact path.");
						}
						return PerfMeterGraphicsStateCollectionRequestResult.Failed;
					}
				}
				catch (Exception exception)
				{
					if (IsGenerationCurrent(generation))
					{
						SetTerminal(options, PerfMeterGraphicsStateCollectionState.Error, PerfMeterAvailability.Unavailable, backendId, backendVersion, exception.GetType().Name + ": " + exception.Message);
					}
					else
					{
						CleanupStalePath(generation, path);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (!IsGenerationCurrent(generation))
				{
					CleanupStalePath(generation, path);
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				bool stalePreparation;
				lock (_sync)
				{
					stalePreparation = _generation != generation || !_requestPreparing || _requestGeneration != generation;
					if (!stalePreparation)
					{
						_pendingPath = path;
						_activeBackend = backend;
						_endingTrace = false;
						_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
							PerfMeterAvailability.Available,
							PerfMeterGraphicsStateCollectionState.Tracing,
							options.CaptureId,
							options.TraceFrames,
							0,
							backendId,
							backendVersion,
							string.Empty,
							0L,
							0,
							0,
							0,
							string.Empty);
					}
				}

				if (stalePreparation)
				{
					CleanupStalePath(generation, path);
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				try
				{
					if (!backend.TryBeginTrace(out string beginError))
					{
						string warning = string.IsNullOrEmpty(beginError) ? "Graphics state collection backend rejected trace start." : beginError;
						warning = CombineWarnings(warning, TryCancelBackend(backend));
						if (IsGenerationCurrent(generation))
						{
							FailTrace(generation, options, backendId, backendVersion, warning);
						}
						else
						{
							CleanupStalePath(generation, path, warning);
						}
						return PerfMeterGraphicsStateCollectionRequestResult.Failed;
					}

					if (!IsGenerationCurrent(generation))
					{
						string warning = TryCancelBackend(backend);
						CleanupStalePath(generation, path, warning);
						return PerfMeterGraphicsStateCollectionRequestResult.Failed;
					}
				}
				catch (Exception exception)
				{
					string warning = exception.GetType().Name + ": " + exception.Message;
					warning = CombineWarnings(warning, TryCancelBackend(backend));
					if (IsGenerationCurrent(generation))
					{
						FailTrace(generation, options, backendId, backendVersion, warning);
					}
					else
					{
						CleanupStalePath(generation, path, warning);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				startedGeneration = generation;
				return PerfMeterGraphicsStateCollectionRequestResult.Started;
			}
			catch (Exception exception)
			{
				string warning = exception.GetType().Name + ": " + exception.Message;
				if (!IsGenerationCurrent(generation))
				{
					CleanupStalePath(generation, string.Empty, warning);
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				PerfMeterGraphicsStateCollectionBackendRegistry.TryGet(
					out IPerfMeterGraphicsStateCollectionBackend backend,
					out string backendId,
					out string backendVersion,
					out _,
					out _,
					out _);
				if (backend != null)
				{
					warning = CombineWarnings(warning, TryCancelBackend(backend));
				}

				if (IsGenerationCurrent(generation) && GetStatus().State == PerfMeterGraphicsStateCollectionState.Tracing)
				{
					FailTrace(generation, options, backendId, backendVersion, warning);
				}
				else if (IsGenerationCurrent(generation))
				{
					SetTerminal(options, PerfMeterGraphicsStateCollectionState.Error, PerfMeterAvailability.Unavailable, backendId, backendVersion, warning);
				}
				else
				{
					CleanupStalePath(generation, string.Empty, warning);
				}

				return PerfMeterGraphicsStateCollectionRequestResult.Failed;
			}
			finally
			{
				lock (_sync)
				{
					if (_requestGeneration == generation)
					{
						_requestPreparing = false;
						_preparingCaptureId = string.Empty;
						_requestGeneration = 0;
					}
				}
			}
		}

		internal void Tick()
		{
			Tick(0);
		}

		internal void Tick(int expectedGeneration)
		{
			IPerfMeterGraphicsStateCollectionBackend backend;
			string path;
			PerfMeterGraphicsStateTraceOptions options;
			string backendId;
			string backendVersion;
			int generation;
			lock (_sync)
			{
				if ((expectedGeneration > 0 && _generation != expectedGeneration) ||
					_status.State != PerfMeterGraphicsStateCollectionState.Tracing ||
					_endingTrace)
				{
					return;
				}

				int completedFrames = Math.Min(_status.RequestedTraceFrames, _status.CompletedTraceFrames + 1);
				if (completedFrames < _status.RequestedTraceFrames)
				{
					_status = CopyStatus(_status, completedTraceFrames: completedFrames);
					return;
				}

				_status = CopyStatus(_status, completedTraceFrames: completedFrames);
				_endingTrace = true;
				generation = _generation;
				backend = _activeBackend;
				path = _pendingPath;
				options = new PerfMeterGraphicsStateTraceOptions(_status.CaptureId, _status.RequestedTraceFrames);
				backendId = _status.BackendId;
				backendVersion = _status.BackendVersion;
			}

			PerfMeterGraphicsStateTraceBackendResult result = default;
			string error = string.Empty;
			bool ended = false;
			try
			{
				ended = backend != null && backend.TryEndTrace(path, out result, out error);
				if (!IsGenerationCurrent(generation))
				{
					CleanupStalePath(generation, path, TryCancelBackend(backend));
					return;
				}

				if (!ended || !result.Success)
				{
					error = string.IsNullOrEmpty(error) ? "Graphics state collection backend failed to save the trace artifact." : error;
					FinishTraceFailure(generation, path, backend, options, backendId, backendVersion, error, !ended && backend != null);
					return;
				}
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				error = CombineWarnings(error, TryCancelBackend(backend));
				FinishTraceFailure(generation, path, backend, options, backendId, backendVersion, error, false);
				return;
			}

			if (!IsGenerationCurrent(generation))
			{
				CleanupStalePath(generation, path);
				return;
			}

			long sizeBytes;
			string validationError;
			bool valid;
			try
			{
				valid = _storage.TryValidateCompleted(path, path, MaxArtifactBytes, out sizeBytes, out validationError);
			}
			catch (Exception exception)
			{
				valid = false;
				sizeBytes = 0L;
				validationError = exception.GetType().Name + ": " + exception.Message;
			}

			if (!valid)
			{
				FinishTraceFailure(generation, path, backend, options, backendId, backendVersion, string.IsNullOrEmpty(validationError) ? "Graphics state collection artifact validation failed." : validationError, false);
				return;
			}

			if (!IsGenerationCurrent(generation))
			{
				CleanupStalePath(generation, path);
				return;
			}

			string relativePath;
			try
			{
				relativePath = _storage.GetRelativePath(path);
			}
			catch (Exception exception)
			{
				FinishTraceFailure(generation, path, backend, options, backendId, backendVersion, exception.GetType().Name + ": " + exception.Message, false);
				return;
			}

			if (string.IsNullOrEmpty(relativePath))
			{
				FinishTraceFailure(generation, path, backend, options, backendId, backendVersion, "Graphics state collection artifact did not resolve to a project-local relative path.", false);
				return;
			}

			if (!IsGenerationCurrent(generation))
			{
				CleanupStalePath(generation, path);
				return;
			}

			bool stale;
			lock (_sync)
			{
				stale = _generation != generation || _status.State != PerfMeterGraphicsStateCollectionState.Tracing || !_endingTrace;
				if (stale)
				{
					// The stale path is cleaned outside the coordinator lock.
				}
				else
				{

					_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterGraphicsStateCollectionState.Completed,
					options.CaptureId,
					options.TraceFrames,
					options.TraceFrames,
					backendId,
					backendVersion,
					relativePath,
					sizeBytes,
					result.TotalGraphicsStateCount,
					result.VariantCount,
					0,
					string.Empty);
				_artifactPath = path;
					_pendingPath = string.Empty;
					_activeBackend = null;
					_endingTrace = false;
				}
			}

			if (stale)
			{
				CleanupStalePath(generation, path);
			}
		}

		internal PerfMeterGraphicsStateCollectionRequestResult Prewarm(PerfMeterGraphicsStatePrewarmOptions options)
		{
			if (!IsValidPrewarmOptions(options))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
			}

			if (HasPendingCleanup && (!RetryPendingCleanup() || HasPendingCleanup))
			{
				return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
			}

			int generation;
			lock (_sync)
			{
				if (HasActiveFlightLocked())
				{
					return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
				}

				generation = ++_generation;
				_prewarming = true;
				_prewarmGeneration = generation;
			}

			try
			{
				if (!PerfMeterGraphicsStateCollectionBackendRegistry.TryGet(
					out IPerfMeterGraphicsStateCollectionBackend backend,
					out string backendId,
					out string backendVersion,
					out bool supportsCacheMissTracing,
					out _,
					out string registryError))
				{
					if (IsGenerationCurrent(generation))
					{
						SetUnavailableAttempt(new PerfMeterGraphicsStateTraceOptions(GetStatus().CaptureId), registryError);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				string inputPath;
				long inputSize;
				try
				{
					if (!_storage.TryResolvePrewarmInput(options.RelativePath, MaxArtifactBytes, out inputPath, out inputSize, out _))
					{
						return PerfMeterGraphicsStateCollectionRequestResult.InvalidRequest;
					}
				}
				catch (Exception exception)
				{
					if (IsGenerationCurrent(generation))
					{
						SetPrewarmFailure(exception.GetType().Name + ": " + exception.Message);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (options.TraceCacheMisses && !supportsCacheMissTracing)
				{
					if (IsGenerationCurrent(generation))
					{
						SetPrewarmUnavailable(backendId, backendVersion, "The graphics state collection backend does not support cache-miss tracing.");
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Unavailable;
				}

				PerfMeterGraphicsStatePrewarmBackendResult result = default;
				string error = string.Empty;
				bool succeeded;
				try
				{
					succeeded = backend.TryPrewarm(inputPath, options.MaxStateCount, options.TraceCacheMisses, out result, out error);
				}
				catch (Exception exception)
				{
					if (IsGenerationCurrent(generation))
					{
						SetPrewarmFailure(exception.GetType().Name + ": " + exception.Message);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				if (!succeeded || !result.Success)
				{
					if (IsGenerationCurrent(generation))
					{
						SetPrewarmFailure(string.IsNullOrEmpty(error) ? "Graphics state collection backend failed to prewarm the artifact." : error);
					}
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				string relativeInputPath;
				try
				{
					relativeInputPath = _storage.GetRelativePath(inputPath);
				}
				catch (Exception exception)
				{
					relativeInputPath = string.Empty;
					if (IsGenerationCurrent(generation))
					{
						AppendWarning(exception.GetType().Name + ": " + exception.Message);
					}
				}

				if (!IsGenerationCurrent(generation))
				{
					return PerfMeterGraphicsStateCollectionRequestResult.Failed;
				}

				string replacedArtifactPath;
				lock (_sync)
				{
					if (_generation != generation || !_prewarming || _prewarmGeneration != generation)
					{
						return PerfMeterGraphicsStateCollectionRequestResult.Failed;
					}

					_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
						PerfMeterAvailability.Available,
						PerfMeterGraphicsStateCollectionState.Prewarmed,
						_status.CaptureId,
						_status.RequestedTraceFrames,
						_status.CompletedTraceFrames,
						backendId,
						backendVersion,
						relativeInputPath,
						inputSize,
						result.TotalGraphicsStateCount,
						_status.VariantCount,
						result.CompletedWarmupCount,
						CombineWarnings(
							_status.Warning,
							result.IsWarmedUp ? string.Empty : "Progressive graphics state collection prewarm is incomplete."),
							result.IsWarmedUp);
					replacedArtifactPath = !string.IsNullOrEmpty(_artifactPath) && !PathsEqual(_artifactPath, inputPath)
						? _artifactPath
						: string.Empty;
					RemovePendingCleanupPath(inputPath);
					_artifactPath = inputPath;
				}

				if (!string.IsNullOrEmpty(replacedArtifactPath) && !TryDeletePath(replacedArtifactPath, out string cleanupError))
				{
					RememberUndeletedPath(replacedArtifactPath);
					AppendWarning(string.IsNullOrEmpty(cleanupError) ? "Replaced graphics state collection artifact could not be deleted." : cleanupError);
				}

				return PerfMeterGraphicsStateCollectionRequestResult.Completed;
			}
			finally
			{
				lock (_sync)
				{
					if (_prewarmGeneration == generation)
					{
						_prewarming = false;
						_prewarmGeneration = 0;
					}
				}
			}
		}

		internal bool CancelTrace(string captureId)
		{
			if (string.IsNullOrEmpty(captureId))
			{
				return false;
			}

			IPerfMeterGraphicsStateCollectionBackend backend;
			string pendingPath;
			int generation;
			lock (_sync)
			{
				bool isPreparingMatch = _requestPreparing && string.Equals(_preparingCaptureId, captureId, StringComparison.Ordinal);
				bool isActiveMatch = (_status.State == PerfMeterGraphicsStateCollectionState.Tracing || _endingTrace) && string.Equals(_status.CaptureId, captureId, StringComparison.Ordinal);
				if (!isPreparingMatch && !isActiveMatch)
				{
					return false;
				}

				generation = ++_generation;
				_cleanupInProgress = true;
				backend = _activeBackend;
				pendingPath = _pendingPath;
				_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterGraphicsStateCollectionState.Canceled,
					captureId,
					_status.RequestedTraceFrames,
					_status.CompletedTraceFrames,
					_status.BackendId,
					_status.BackendVersion,
					string.Empty,
					0L,
					0,
					0,
					0,
					string.Empty);
				_pendingPath = string.Empty;
				_activeBackend = null;
				_requestPreparing = false;
				_requestGeneration = 0;
				_preparingCaptureId = string.Empty;
				_endingTrace = false;
			}

			string warning = TryCancelBackend(backend);
			if (!string.IsNullOrEmpty(pendingPath) && !TryDeleteDetachedPath(generation, pendingPath, out string deleteError))
			{
				RememberUndeletedPath(pendingPath);
				warning = CombineWarnings(warning, string.IsNullOrEmpty(deleteError) ? "Canceled graphics state collection artifact could not be deleted." : deleteError);
			}

			if (!string.IsNullOrEmpty(warning))
			{
				lock (_sync)
				{
					if (_generation == generation)
					{
						_status = CopyStatus(
							_status,
							availability: PerfMeterAvailability.Unavailable,
							state: PerfMeterGraphicsStateCollectionState.Error,
							warning: CombineWarnings(_status.Warning, warning));
					}
				}
				if (!IsGenerationCurrent(generation))
				{
					AppendWarning(warning);
				}
			}

			lock (_sync)
			{
				if (_generation == generation)
				{
					_cleanupInProgress = false;
				}
			}

			return true;
		}

		internal void Shutdown()
		{
			IPerfMeterGraphicsStateCollectionBackend backend;
			string pendingPath;
			int generation;
			lock (_sync)
			{
				if (_cleanupInProgress || !IsBusyLocked())
				{
					return;
				}

				generation = ++_generation;
				_cleanupInProgress = true;
				backend = _activeBackend;
				pendingPath = _pendingPath;
				string captureId = string.IsNullOrEmpty(_status.CaptureId) ? _preparingCaptureId : _status.CaptureId;
				_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterGraphicsStateCollectionState.Error,
					captureId,
					_status.RequestedTraceFrames,
					_status.CompletedTraceFrames,
					_status.BackendId,
					_status.BackendVersion,
					string.Empty,
					0L,
					0,
					0,
					0,
					"Graphics state collection trace was interrupted by shutdown.");
				_pendingPath = string.Empty;
				_activeBackend = null;
				_requestPreparing = false;
				_requestGeneration = 0;
				_preparingCaptureId = string.Empty;
				_endingTrace = false;
				_prewarming = false;
				_prewarmGeneration = 0;
			}

			string warning = TryCancelBackend(backend);
			if (!string.IsNullOrEmpty(pendingPath) && !TryDeleteDetachedPath(generation, pendingPath, out string deleteError))
			{
				RememberUndeletedPath(pendingPath);
				warning = CombineWarnings(warning, string.IsNullOrEmpty(deleteError) ? "Interrupted graphics state collection artifact could not be deleted." : deleteError);
			}

			if (!string.IsNullOrEmpty(warning))
			{
				AppendWarning(warning);
			}

			lock (_sync)
			{
				if (_generation == generation)
				{
					_cleanupInProgress = false;
				}
			}
		}

		internal static bool IsValidTraceOptions(PerfMeterGraphicsStateTraceOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.CaptureId) || options.CaptureId.Length > 128 || options.TraceFrames < 1 || options.TraceFrames > MaxTraceFrames)
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

		internal static bool IsValidPrewarmOptions(PerfMeterGraphicsStatePrewarmOptions options)
		{
			return options.MaxStateCount >= 0 && options.MaxStateCount <= MaxPrewarmStateCount;
		}

		private bool HasActiveFlightLocked()
		{
			return IsBusyLocked() || _pendingCleanupPaths.Count > 0;
		}

		internal bool IsActiveTrace(string captureId, int generation)
		{
			lock (_sync)
			{
				return generation > 0 &&
					_generation == generation &&
					_status.State == PerfMeterGraphicsStateCollectionState.Tracing &&
					string.Equals(_status.CaptureId, captureId, StringComparison.Ordinal);
			}
		}

		private bool IsBusyLocked()
		{
			return _requestPreparing ||
				_status.State == PerfMeterGraphicsStateCollectionState.Tracing ||
				_endingTrace ||
				_prewarming ||
				_cleanupInProgress;
		}

		private PerfMeterGraphicsStateCollectionRequestResult GetOverlapResultLocked(string captureId)
		{
			if (_prewarming)
			{
				return PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
			}

			string activeCaptureId = _requestPreparing ? _preparingCaptureId : _status.CaptureId;
			return string.Equals(activeCaptureId, captureId, StringComparison.Ordinal)
				? PerfMeterGraphicsStateCollectionRequestResult.AlreadyActive
				: PerfMeterGraphicsStateCollectionRequestResult.RejectedOverlap;
		}

		private bool TryDeleteExistingArtifact(int generation, out string error)
		{
			error = string.Empty;
			string artifactPath;
			string[] pendingCleanupPaths;
			lock (_sync)
			{
				if (_generation != generation)
				{
					error = "Graphics state collection request was superseded.";
					return false;
				}

				artifactPath = _artifactPath;
				pendingCleanupPaths = _pendingCleanupPaths.ToArray();
			}

			if (string.IsNullOrEmpty(artifactPath) && pendingCleanupPaths.Length == 0)
			{
				error = string.Empty;
				return true;
			}

			if (!string.IsNullOrEmpty(artifactPath) && !TryDeletePath(artifactPath, out error))
			{
				RememberUndeletedPath(artifactPath);
				return false;
			}

			if (!string.IsNullOrEmpty(artifactPath))
			{
				lock (_sync)
				{
					if (_generation == generation && PathsEqual(_artifactPath, artifactPath))
					{
						_artifactPath = string.Empty;
					}
				}
			}

			for (int i = 0; i < pendingCleanupPaths.Length; i++)
			{
				if (!IsGenerationCurrent(generation))
				{
					error = "Graphics state collection request was superseded.";
					return false;
				}

				string pendingPath = pendingCleanupPaths[i];
				if (!TryDeletePath(pendingPath, out error))
				{
					return false;
				}

				if (!IsGenerationCurrent(generation))
				{
					error = "Graphics state collection request was superseded.";
					return false;
				}

				RemovePendingCleanupPath(pendingPath);
			}

			return true;
		}

		private void FailTrace(int generation, PerfMeterGraphicsStateTraceOptions options, string backendId, string backendVersion, string warning)
		{
			string pendingPath;
			lock (_sync)
			{
				if (_generation != generation)
				{
					return;
				}

				pendingPath = _pendingPath;
				SetTerminalLocked(options, PerfMeterGraphicsStateCollectionState.Error, PerfMeterAvailability.Unavailable, backendId, backendVersion, warning);
			}

			if (!string.IsNullOrEmpty(pendingPath) && !TryDeleteDetachedPath(generation, pendingPath, out string deleteError))
			{
				RememberUndeletedPath(pendingPath);
				AppendWarning(string.IsNullOrEmpty(deleteError) ? "Failed graphics state collection artifact could not be deleted." : deleteError);
			}
		}

		private void RememberUndeletedPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			lock (_sync)
			{
				if (!_pendingCleanupPaths.Contains(path))
				{
					_pendingCleanupPaths.Add(path);
				}
			}

			if (!_storage.TryMarkPendingCleanup(path))
			{
				AppendWarning("Pending graphics state collection cleanup could not be persisted across reload.");
			}
		}

		private bool RemovePendingCleanupPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return true;
			}

			if (!_storage.TryClearPendingCleanup(path))
			{
				AppendWarning("Pending graphics state collection cleanup marker could not be removed.");
				return false;
			}

			lock (_sync)
			{
				_pendingCleanupPaths.Remove(path);
			}
			return true;
		}

		private bool IsGenerationCurrent(int generation)
		{
			lock (_sync)
			{
				return _generation == generation;
			}
		}

		private void CleanupStalePath(int generation, string path, string warning = null)
		{
			if (!string.IsNullOrEmpty(warning))
			{
				AppendWarning(warning);
			}

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			lock (_sync)
			{
				if (PathsEqual(path, _pendingPath) || PathsEqual(path, _artifactPath))
				{
					return;
				}

				if (_generation != generation && IsBusyLocked())
				{
					RememberUndeletedPath(path);
					return;
				}
			}

			if (!TryDeletePath(path, out string deleteError))
			{
				RememberUndeletedPath(path);
				string cleanupWarning = string.IsNullOrEmpty(deleteError) ? "Stale graphics state collection artifact could not be deleted." : deleteError;
				AppendWarning(cleanupWarning);
				lock (_sync)
				{
					if (_status.State == PerfMeterGraphicsStateCollectionState.Canceled)
					{
						_status = CopyStatus(_status, availability: PerfMeterAvailability.Unavailable, state: PerfMeterGraphicsStateCollectionState.Error);
					}
				}
			}
		}

		private bool TryDeleteDetachedPath(int generation, string path, out string error)
		{
			error = string.Empty;
			if (string.IsNullOrEmpty(path))
			{
				return true;
			}

			lock (_sync)
			{
				if (_generation != generation || PathsEqual(path, _pendingPath) || PathsEqual(path, _artifactPath))
				{
					if (_generation != generation)
					{
						RememberUndeletedPath(path);
					}

					return true;
				}
			}

			return TryDeletePath(path, out error);
		}

		private void FinishTraceFailure(
			int generation,
			string path,
			IPerfMeterGraphicsStateCollectionBackend backend,
			PerfMeterGraphicsStateTraceOptions options,
			string backendId,
			string backendVersion,
			string warning,
			bool cancelBackend)
		{
			if (!IsGenerationCurrent(generation))
			{
				CleanupStalePath(generation, path, warning);
				if (cancelBackend)
				{
					TryCancelBackend(backend);
				}
				return;
			}

			if (cancelBackend)
			{
				warning = CombineWarnings(warning, TryCancelBackend(backend));
			}

			if (IsGenerationCurrent(generation))
			{
				FailTrace(generation, options, backendId, backendVersion, warning);
			}
			else
			{
				CleanupStalePath(generation, path, warning);
			}
		}

		private void SetTerminal(
			PerfMeterGraphicsStateTraceOptions options,
			PerfMeterGraphicsStateCollectionState state,
			PerfMeterAvailability availability,
			string backendId,
			string backendVersion,
			string warning)
		{
			lock (_sync)
			{
				SetTerminalLocked(options, state, availability, backendId, backendVersion, warning);
			}
		}

		private void SetTerminalLocked(
			PerfMeterGraphicsStateTraceOptions options,
			PerfMeterGraphicsStateCollectionState state,
			PerfMeterAvailability availability,
			string backendId,
			string backendVersion,
			string warning)
		{
			_status = new PerfMeterGraphicsStateCollectionStatusSnapshot(
				availability,
				state,
				options.CaptureId,
				options.TraceFrames,
				0,
				backendId,
				backendVersion,
				string.Empty,
				0L,
				0,
				0,
				0,
				warning);
			_pendingPath = string.Empty;
			_activeBackend = null;
			_endingTrace = false;
		}

		private void SetUnavailableAttempt(PerfMeterGraphicsStateTraceOptions options, string warning)
		{
			lock (_sync)
			{
				if (!string.IsNullOrEmpty(_artifactPath))
				{
					_status = CopyStatus(_status, warning: CombineWarnings(_status.Warning, warning));
				}
				else
				{
					SetTerminalLocked(options, PerfMeterGraphicsStateCollectionState.Unavailable, PerfMeterAvailability.Unavailable, string.Empty, string.Empty, warning);
				}
			}
		}

		private void SetPrewarmFailure(string warning)
		{
			lock (_sync)
			{
				_status = CopyStatus(
					_status,
					availability: PerfMeterAvailability.Unavailable,
					state: PerfMeterGraphicsStateCollectionState.Error,
					warning: CombineWarnings(_status.Warning, warning));
			}
		}

		private void SetPrewarmUnavailable(string backendId, string backendVersion, string warning)
		{
			lock (_sync)
			{
				_status = CopyStatus(
					_status,
					availability: PerfMeterAvailability.Unavailable,
					state: PerfMeterGraphicsStateCollectionState.Unavailable,
					backendId: backendId,
					backendVersion: backendVersion,
					warning: CombineWarnings(_status.Warning, warning));
			}
		}

		private void AppendWarning(string warning)
		{
			if (string.IsNullOrEmpty(warning))
			{
				return;
			}

			lock (_sync)
			{
				_status = CopyStatus(_status, warning: CombineWarnings(_status.Warning, warning));
			}
		}

		private bool TryDeletePath(string path, out string error)
		{
			error = string.Empty;
			if (string.IsNullOrEmpty(path))
			{
				return true;
			}

			try
			{
				if (_storage.TryDelete(path))
				{
					return true;
				}

				error = "Graphics state collection artifact could not be deleted.";
				return false;
			}
			catch (Exception exception)
			{
				error = "Graphics state collection artifact cleanup failed: " + exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		private static string TryCancelBackend(IPerfMeterGraphicsStateCollectionBackend backend)
		{
			if (backend == null)
			{
				return string.Empty;
			}

			try
			{
				backend.CancelTrace();
				return string.Empty;
			}
			catch (Exception exception)
			{
				return "Graphics state collection backend cancellation failed: " + exception.GetType().Name + ": " + exception.Message;
			}
		}

		private static bool IsInsufficientDiskError(string error)
		{
			return string.Equals(error, "insufficient_free_disk_space", StringComparison.Ordinal);
		}

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(second))
			{
				return first ?? string.Empty;
			}

			if (string.IsNullOrEmpty(first))
			{
				return second;
			}

			return first.IndexOf(second, StringComparison.Ordinal) >= 0 ? first : first + " " + second;
		}

		private static bool PathsEqual(string left, string right)
		{
			return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
		}

		private static PerfMeterGraphicsStateCollectionStatusSnapshot CopyStatus(
			PerfMeterGraphicsStateCollectionStatusSnapshot status,
			PerfMeterAvailability? availability = null,
			PerfMeterGraphicsStateCollectionState? state = null,
			string captureId = null,
			int? requestedTraceFrames = null,
			int? completedTraceFrames = null,
			string backendId = null,
			string backendVersion = null,
			string artifactRelativePath = null,
			long? artifactSizeBytes = null,
			int? totalGraphicsStateCount = null,
			int? variantCount = null,
			int? completedWarmupCount = null,
			string warning = null,
			bool? isWarmedUp = null,
			bool? isBusy = null,
			bool? hasPendingCleanup = null)
		{
			return new PerfMeterGraphicsStateCollectionStatusSnapshot(
				availability ?? status.Availability,
				state ?? status.State,
				captureId ?? status.CaptureId,
				requestedTraceFrames ?? status.RequestedTraceFrames,
				completedTraceFrames ?? status.CompletedTraceFrames,
				backendId ?? status.BackendId,
				backendVersion ?? status.BackendVersion,
				artifactRelativePath ?? status.ArtifactRelativePath,
				artifactSizeBytes ?? status.ArtifactSizeBytes,
				totalGraphicsStateCount ?? status.TotalGraphicsStateCount,
				variantCount ?? status.VariantCount,
				completedWarmupCount ?? status.CompletedWarmupCount,
				warning ?? status.Warning,
				isWarmedUp ?? status.IsWarmedUp,
				isBusy ?? status.IsBusy,
				hasPendingCleanup ?? status.HasPendingCleanup);
		}
	}
}
