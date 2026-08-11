using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SGG.PerfMeter.Editor.Setup
{
	internal enum PerfMeterRenderDocBridgeInstallState
	{
		Unsupported = 0,
		NotInstalled = 1,
		Downloading = 2,
		Installed = 3,
		Invalid = 4
	}

	internal readonly struct PerfMeterRenderDocBridgeInstallStatus
	{
		internal PerfMeterRenderDocBridgeInstallStatus(
			PerfMeterRenderDocBridgeInstallState state,
			string message,
			string sha256 = "")
		{
			State = state;
			Message = message ?? string.Empty;
			Sha256 = sha256 ?? string.Empty;
		}

		internal PerfMeterRenderDocBridgeInstallState State { get; }
		internal string Message { get; }
		internal string Sha256 { get; }
		internal bool IsInstalled => State == PerfMeterRenderDocBridgeInstallState.Installed;
	}

	[InitializeOnLoad]
	internal static class PerfMeterRenderDocBridgeInstaller
	{
		internal const string ArtifactVersion = "2026.8.11-1";
		internal const string DllFileName = "sgg_renderdoc_bridge.dll";
		internal const long DllByteLength = 125952L;
		internal const string DllSha256 = "01d605d9f7b1454511948ef8ff4905fc87344141555a9435580b875866741734";
		internal const string ArchiveFileName = "sgg-perfmeter-renderdoc-bridge-2026.8.11-1-windows-x64.zip";
		internal const long ArchiveByteLength = 90837L;
		internal const string ArchiveSha256 = "cc4e1551bbda64c7d59372b818cfcdbf250227a23a4acb77d3c66db2c7240fe5";
		internal const string DownloadUrl = "https://github.com/romanilyin/sgg-perfmeter/releases/download/2026.8.11-1/" + ArchiveFileName;
		internal const string InstalledAssetPath = "Assets/Plugins/SGG.PerfMeter/RenderDoc/Editor/Windows/x86_64/" + DllFileName;

		private const long MaximumArchiveBytes = 4L * 1024L * 1024L;
		private const string ManagedAssetLabel = "sgg-perfmeter-renderdoc-bridge-managed";
		private static UnityWebRequest _download;
		private static BoundedFileDownloadHandler _downloadHandler;
		private static string _downloadPath;
		private static string _lastResult;
		private static bool _lastResultSucceeded;

		static PerfMeterRenderDocBridgeInstaller()
		{
			EditorApplication.update += PollDownload;
			AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeDomainExit;
			EditorApplication.quitting += CleanupBeforeDomainExit;
			CleanupStaleStagingDirectories();
		}

		internal static bool HasActiveDownload => _download != null && !_download.isDone;

		internal static PerfMeterRenderDocBridgeInstallStatus GetStatus()
		{
			if (!IsSupportedHost())
			{
				return new PerfMeterRenderDocBridgeInstallStatus(
					PerfMeterRenderDocBridgeInstallState.Unsupported,
					"Bridge installation is available only in the Windows x64 Editor.");
			}
			if (HasActiveDownload)
			{
				return new PerfMeterRenderDocBridgeInstallStatus(
					PerfMeterRenderDocBridgeInstallState.Downloading,
					"Downloading and verifying SGG RenderDoc bridge " + ArtifactVersion + ".");
			}

			string fullPath = GetInstalledFullPath();
			if (!File.Exists(fullPath))
			{
				return new PerfMeterRenderDocBridgeInstallStatus(
					PerfMeterRenderDocBridgeInstallState.NotInstalled,
					"SGG RenderDoc bridge is not installed. RenderDoc itself remains a separate user-owned tool.");
			}

			if (!TryValidateBridgeFile(fullPath, DllByteLength, DllSha256, out string sha256, out string error))
			{
				return new PerfMeterRenderDocBridgeInstallStatus(
					PerfMeterRenderDocBridgeInstallState.Invalid,
					"Installed bridge is invalid: " + error,
					sha256);
			}
			if (!HasExpectedImporterSettings(out error))
			{
				return new PerfMeterRenderDocBridgeInstallStatus(
					PerfMeterRenderDocBridgeInstallState.Invalid,
					"Installed bridge importer is invalid: " + error,
					sha256);
			}

			return new PerfMeterRenderDocBridgeInstallStatus(
				PerfMeterRenderDocBridgeInstallState.Installed,
				"Installed/Ready - verified bridge " + ArtifactVersion + " for Windows x64 Editor. Restart the Editor after install or update.",
				sha256);
		}

		internal static bool TryStartDownload(out string error)
		{
			error = string.Empty;
			if (!IsSupportedHost())
			{
				error = "Bridge installation is available only in the Windows x64 Editor.";
				return false;
			}
			if (HasActiveDownload)
			{
				error = "A bridge download is already active.";
				return false;
			}

			CleanupDownload();
			try
			{
				string stagingRoot = CreateStagingRoot();
				_downloadPath = Path.Combine(stagingRoot, ArchiveFileName);
				_downloadHandler = new BoundedFileDownloadHandler(_downloadPath, MaximumArchiveBytes);
				_download = new UnityWebRequest(DownloadUrl, UnityWebRequest.kHttpVerbGET)
				{
					downloadHandler = _downloadHandler,
					disposeDownloadHandlerOnDispose = true,
					redirectLimit = 4,
					timeout = 120
				};
				_download.SetRequestHeader("User-Agent", "SGG-PerfMeter/" + ArtifactVersion);
				_download.SendWebRequest();
				_lastResult = string.Empty;
				return true;
			}
			catch (Exception exception)
			{
				CleanupDownload();
				error = exception.Message;
				return false;
			}
		}

		internal static bool Update()
		{
			if (_download == null)
			{
				return false;
			}
			if (!_download.isDone)
			{
				if (_download.downloadedBytes > (ulong)MaximumArchiveBytes)
				{
					_download.Abort();
					_lastResult = "Bridge download was rejected: archive exceeds the 4 MiB safety limit.";
					_lastResultSucceeded = false;
					CleanupDownload();
					return true;
				}
				return false;
			}

			string result;
			bool succeeded = false;
			try
			{
				if (_download.result != UnityWebRequest.Result.Success)
				{
					string handlerError = _downloadHandler == null ? string.Empty : _downloadHandler.Error;
					result = "Bridge download failed: " +
						(string.IsNullOrEmpty(handlerError) ? (_download.error ?? "unknown network error") : handlerError) + ".";
				}
				else if (_download.responseCode != 200L)
				{
					result = "Bridge download failed with HTTP " + _download.responseCode + ".";
				}
				else if (TryInstallArchive(_downloadPath, out string installMessage))
				{
					succeeded = true;
					result = installMessage;
				}
				else
				{
					result = "Bridge download was rejected: " + installMessage;
				}
			}
			catch (Exception exception)
			{
				result = "Bridge download failed: " + exception.Message;
			}
			finally
			{
				CleanupDownload();
			}

			_lastResult = result;
			_lastResultSucceeded = succeeded;
			return true;
		}

		internal static void CancelDownload()
		{
			if (_download != null && !_download.isDone)
			{
				_download.Abort();
			}
			CleanupDownload();
			_lastResult = "Bridge download canceled.";
			_lastResultSucceeded = false;
		}

		internal static bool TryConsumeLastResult(out bool succeeded, out string message)
		{
			succeeded = _lastResultSucceeded;
			message = _lastResult ?? string.Empty;
			if (message.Length == 0)
			{
				return false;
			}

			_lastResult = string.Empty;
			return true;
		}

		internal static bool TryInstallLocal(string sourcePath, out string message)
		{
			message = string.Empty;
			if (!IsSupportedHost())
			{
				message = "Bridge installation is available only in the Windows x64 Editor.";
				return false;
			}
			if (!TryValidateLocalSourcePath(sourcePath, out string fullPath, out string error))
			{
				message = error;
				return false;
			}

			return TryInstallBridgeFile(fullPath, out message);
		}

		internal static bool TryRemove(out string message)
		{
			message = string.Empty;
			string fullPath = GetInstalledFullPath();
			if (!File.Exists(fullPath))
			{
				if (IsManagedOrphanMetaFile())
				{
					File.Delete(fullPath + ".meta");
					AssetDatabase.Refresh();
					message = "Orphaned SGG RenderDoc bridge importer metadata removed.";
					return true;
				}
				message = "SGG RenderDoc bridge is not installed.";
				return true;
			}
			if (!CanManageInstalledAsset(out string error))
			{
				message = "Refusing to remove an unrecognized bridge file: " + error;
				return false;
			}

			if (!AssetDatabase.DeleteAsset(InstalledAssetPath))
			{
				message = "Unity could not remove the bridge. Close processes using it or remove it manually: " + InstalledAssetPath;
				return false;
			}

			AssetDatabase.Refresh();
			message = "SGG RenderDoc bridge removed. Restart the Editor before another native capture.";
			return true;
		}

		internal static bool TryValidateBridgeFile(
			string path,
			long expectedByteLength,
			string expectedSha256,
			out string actualSha256,
			out string error)
		{
			actualSha256 = string.Empty;
			error = string.Empty;
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				error = "Bridge DLL does not exist.";
				return false;
			}

			try
			{
				using (FileStream stream = new FileStream(
					path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read,
					4096,
					FileOptions.SequentialScan))
				{
					return TryValidateBridgeStream(stream, expectedByteLength, expectedSha256, out actualSha256, out error);
				}
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		private static bool TryValidateBridgeStream(
			Stream stream,
			long expectedByteLength,
			string expectedSha256,
			out string actualSha256,
			out string error)
		{
			actualSha256 = string.Empty;
			error = string.Empty;
			if (stream.Length != expectedByteLength)
			{
				error = "Unexpected byte length " + stream.Length + "; expected " + expectedByteLength + ".";
				return false;
			}
			if (!TryValidateNativeAmd64Pe(stream, out error))
			{
				return false;
			}

			actualSha256 = ComputeSha256(stream);
			if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
			{
				error = "SHA-256 mismatch.";
				return false;
			}
			return true;
		}

		internal static bool TryValidateNativeAmd64Pe(Stream stream, out string error)
		{
			error = string.Empty;
			if (stream == null || !stream.CanRead || !stream.CanSeek || stream.Length < 304L)
			{
				error = "Invalid or truncated PE image.";
				return false;
			}

			try
			{
				using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
				{
					stream.Position = 0L;
					if (reader.ReadUInt16() != 0x5A4Du)
					{
						error = "Missing DOS MZ signature.";
						return false;
					}
					stream.Position = 0x3c;
					int peOffset = reader.ReadInt32();
					if (peOffset < 64 || peOffset > stream.Length - 264L)
					{
						error = "Invalid PE header offset.";
						return false;
					}

					stream.Position = peOffset;
					if (reader.ReadUInt32() != 0x00004550u)
					{
						error = "Missing PE signature.";
						return false;
					}
					if (reader.ReadUInt16() != 0x8664u)
					{
						error = "Bridge is not an AMD64 image.";
						return false;
					}
					reader.ReadUInt16();
					stream.Position += 12L;
					ushort optionalHeaderSize = reader.ReadUInt16();
					ushort characteristics = reader.ReadUInt16();
					if ((characteristics & 0x2000u) == 0u)
					{
						error = "PE image is not marked as a DLL.";
						return false;
					}
					if (optionalHeaderSize < 240u || peOffset + 24L + optionalHeaderSize > stream.Length)
					{
						error = "Invalid PE32+ optional header.";
						return false;
					}

					long optionalHeaderOffset = peOffset + 24L;
					stream.Position = optionalHeaderOffset;
					if (reader.ReadUInt16() != 0x20Bu)
					{
						error = "Bridge is not a PE32+ image.";
						return false;
					}
					stream.Position = optionalHeaderOffset + 224L;
					uint clrRva = reader.ReadUInt32();
					uint clrSize = reader.ReadUInt32();
					if (clrRva != 0u || clrSize != 0u)
					{
						error = "Bridge must be a native DLL, not a managed assembly.";
						return false;
					}
				}
				return true;
			}
			catch (EndOfStreamException)
			{
				error = "Truncated PE image.";
				return false;
			}
		}

		private static bool TryInstallArchive(string archivePath, out string message)
		{
			message = string.Empty;
			try
			{
				using (FileStream stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					if (stream.Length != ArchiveByteLength || stream.Length > MaximumArchiveBytes)
					{
						message = "Unexpected archive byte length.";
						return false;
					}
					if (!string.Equals(ComputeSha256(stream), ArchiveSha256, StringComparison.OrdinalIgnoreCase))
					{
						message = "Archive SHA-256 mismatch.";
						return false;
					}

					stream.Position = 0L;
					using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
					{
						ZipArchiveEntry bridgeEntry = null;
						for (int index = 0; index < archive.Entries.Count; index++)
						{
							ZipArchiveEntry entry = archive.Entries[index];
							if (string.Equals(entry.FullName, DllFileName, StringComparison.Ordinal))
							{
								if (bridgeEntry != null)
								{
									message = "Archive contains duplicate bridge entries.";
									return false;
								}
								bridgeEntry = entry;
							}
						}
						if (bridgeEntry == null || bridgeEntry.Length != DllByteLength)
						{
							message = "Archive does not contain the expected bridge DLL.";
							return false;
						}

						string extractionRoot = CreateStagingRoot();
						string extractedPath = Path.Combine(extractionRoot, DllFileName);
						try
						{
							using (Stream source = bridgeEntry.Open())
							using (FileStream destination = new FileStream(extractedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
							{
								CopyBounded(source, destination, DllByteLength);
								destination.Flush(true);
							}
							return TryInstallBridgeFile(extractedPath, out message);
						}
						finally
						{
							DeleteDirectory(extractionRoot);
						}
					}
				}
			}
			catch (Exception exception)
			{
				message = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		private static bool TryInstallBridgeFile(string sourcePath, out string message)
		{
			message = string.Empty;
			if (!TryFindDuplicateBridgeAsset(out string duplicatePath))
			{
				message = "Another bridge DLL already exists at " + duplicatePath + ". Remove duplicate plugin basenames before installation.";
				return false;
			}

			string destinationPath = GetInstalledFullPath();
			if (File.Exists(destinationPath))
			{
				if (TryValidateBridgeFile(destinationPath, DllByteLength, DllSha256, out _, out _) &&
					TryConfigureImporter(out _))
				{
					message = "Verified bridge " + ArtifactVersion + " is already installed. Restart the Editor before native capture.";
					return true;
				}
				if (!CanManageInstalledAsset(out string existingError))
				{
					message = "Refusing to overwrite an unrecognized installed bridge: " + existingError;
					return false;
				}
			}
			else if (File.Exists(destinationPath + ".meta") && !IsManagedOrphanMetaFile())
			{
				message = "Refusing to install over orphaned bridge importer metadata. Remove " + InstalledAssetPath + ".meta first.";
				return false;
			}

			string stagingRoot = CreateStagingRoot();
			string stagedPath = Path.Combine(stagingRoot, DllFileName);
			bool moved = false;
			try
			{
				using (FileStream source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					if (source.Length != DllByteLength)
					{
						message = "Unexpected bridge byte length.";
						return false;
					}
					if (!TryValidateNativeAmd64Pe(source, out string peError))
					{
						message = peError;
						return false;
					}
					if (!string.Equals(ComputeSha256(source), DllSha256, StringComparison.OrdinalIgnoreCase))
					{
						message = "Bridge SHA-256 mismatch.";
						return false;
					}

					source.Position = 0L;
					using (FileStream staged = new FileStream(stagedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
					{
						CopyBounded(source, staged, DllByteLength);
						staged.Flush(true);
					}
				}

				if (!TryValidateBridgeFile(stagedPath, DllByteLength, DllSha256, out _, out string stagedError))
				{
					message = "Staged bridge validation failed: " + stagedError;
					return false;
				}

				if (File.Exists(destinationPath) && !AssetDatabase.DeleteAsset(InstalledAssetPath))
				{
					message = "Unity could not replace the managed bridge. Restart the Editor and try again.";
					return false;
				}
				if (!File.Exists(destinationPath) && IsManagedOrphanMetaFile())
				{
					File.Delete(destinationPath + ".meta");
					AssetDatabase.Refresh();
				}
				if (File.Exists(destinationPath) || File.Exists(destinationPath + ".meta"))
				{
					message = "Managed bridge cleanup was incomplete. Restart the Editor and remove " + InstalledAssetPath + " before retrying.";
					return false;
				}

				string destinationDirectory = Path.GetDirectoryName(destinationPath);
				Directory.CreateDirectory(destinationDirectory);
				if (!TryValidateDestinationDirectory(destinationDirectory, out string destinationError))
				{
					message = destinationError;
					return false;
				}
				File.Move(stagedPath, destinationPath);
				moved = true;
				using (FileStream installed = new FileStream(
					destinationPath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read,
					4096,
					FileOptions.SequentialScan))
				{
					if (!TryValidateBridgeStream(installed, DllByteLength, DllSha256, out _, out string installedError))
					{
						throw new InvalidDataException("Installed bridge validation failed: " + installedError);
					}
					AssetDatabase.ImportAsset(InstalledAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
					if (!TryConfigureImporter(out string importerError))
					{
						throw new InvalidOperationException(importerError);
					}
				}

				message = "Verified bridge " + ArtifactVersion + " installed at " + InstalledAssetPath + ". Restart the Editor before native capture.";
				return true;
			}
			catch (Exception exception)
			{
				string rollbackError = string.Empty;
				if (moved)
				{
					TryDeleteInstalledAsset(out rollbackError);
				}
				message = exception.GetType().Name + ": " + exception.Message +
					(string.IsNullOrEmpty(rollbackError) ? string.Empty : " Rollback was incomplete: " + rollbackError);
				return false;
			}
			finally
			{
				DeleteDirectory(stagingRoot);
			}
		}

		private static bool TryValidateLocalSourcePath(string sourcePath, out string fullPath, out string error)
		{
			fullPath = string.Empty;
			error = string.Empty;
			if (string.IsNullOrWhiteSpace(sourcePath))
			{
				error = "Select the extracted " + DllFileName + " file.";
				return false;
			}

			try
			{
				fullPath = Path.GetFullPath(sourcePath);
				if (!string.Equals(Path.GetFileName(fullPath), DllFileName, StringComparison.OrdinalIgnoreCase))
				{
					error = "Selected file must be named " + DllFileName + ".";
					return false;
				}
				if (fullPath.StartsWith("\\\\", StringComparison.Ordinal) || fullPath.IndexOf(':', 2) >= 0)
				{
					error = "Network, device and alternate-data-stream paths are not accepted.";
					return false;
				}
				if (!File.Exists(fullPath) || (File.GetAttributes(fullPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
				{
					error = "Selected bridge must be a regular local file.";
					return false;
				}
				for (string parent = Path.GetDirectoryName(fullPath); !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
				{
					if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
					{
						error = "Selected bridge path contains a reparse point.";
						return false;
					}
					string root = Path.GetPathRoot(parent);
					if (string.Equals(parent, root, StringComparison.OrdinalIgnoreCase))
					{
						break;
					}
				}
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		}

		private static bool TryFindDuplicateBridgeAsset(out string duplicatePath)
		{
			duplicatePath = string.Empty;
			string[] assetPaths = AssetDatabase.GetAllAssetPaths();
			for (int index = 0; index < assetPaths.Length; index++)
			{
				string path = assetPaths[index];
				if (string.Equals(Path.GetFileName(path), DllFileName, StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(path, InstalledAssetPath, StringComparison.OrdinalIgnoreCase))
				{
					duplicatePath = path;
					return false;
				}
			}
			return true;
		}

		private static bool TryConfigureImporter(out string error)
		{
			error = string.Empty;
			PluginImporter importer = AssetImporter.GetAtPath(InstalledAssetPath) as PluginImporter;
			if (importer == null)
			{
				error = "Unity did not create a native PluginImporter for the bridge.";
				return false;
			}

			try
			{
				importer.SetCompatibleWithAnyPlatform(false);
				importer.SetCompatibleWithEditor(true);
				importer.SetEditorData("OS", "Windows");
				importer.SetEditorData("CPU", "x86_64");
				if (!TrySetAllPlayerPlatformsCompatible(importer, false, out error))
				{
					return false;
				}
				importer.isPreloaded = false;
				string[] labels = AssetDatabase.GetLabels(importer);
				if (Array.IndexOf(labels, ManagedAssetLabel) < 0)
				{
					Array.Resize(ref labels, labels.Length + 1);
					labels[labels.Length - 1] = ManagedAssetLabel;
					AssetDatabase.SetLabels(importer, labels);
				}
				importer.SaveAndReimport();
				return HasExpectedImporterSettings(out error);
			}
			catch (Exception exception)
			{
				error = "Could not configure Editor-only bridge importer: " + exception.Message;
				return false;
			}
		}

		private static bool HasExpectedImporterSettings(out string error)
		{
			error = string.Empty;
			PluginImporter importer = AssetImporter.GetAtPath(InstalledAssetPath) as PluginImporter;
			if (importer == null)
			{
				error = "PluginImporter is unavailable.";
				return false;
			}

			bool valid =
				!importer.GetCompatibleWithAnyPlatform() &&
				importer.GetCompatibleWithEditor() &&
				string.Equals(importer.GetEditorData("OS"), "Windows", StringComparison.OrdinalIgnoreCase) &&
				string.Equals(importer.GetEditorData("CPU"), "x86_64", StringComparison.OrdinalIgnoreCase) &&
				!importer.isPreloaded &&
				Array.IndexOf(AssetDatabase.GetLabels(importer), ManagedAssetLabel) >= 0;
			if (valid && !TryVerifyAllPlayerPlatformsDisabled(importer, out error))
			{
				return false;
			}
			if (!valid)
			{
				error = "Expected Windows x86_64 Editor-only, non-preloaded settings.";
			}
			return valid;
		}

		private static bool TrySetAllPlayerPlatformsCompatible(PluginImporter importer, bool compatible, out string error)
		{
			error = string.Empty;
			foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
			{
				if (target == BuildTarget.NoTarget)
				{
					continue;
				}
				try
				{
					importer.SetCompatibleWithPlatform(target, compatible);
				}
				catch (ArgumentException)
				{
				}
				catch (NotSupportedException)
				{
				}
			}
			return TryVerifyAllPlayerPlatformsDisabled(importer, out error);
		}

		private static bool TryVerifyAllPlayerPlatformsDisabled(PluginImporter importer, out string error)
		{
			error = string.Empty;
			foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
			{
				if (target == BuildTarget.NoTarget)
				{
					continue;
				}
				try
				{
					if (importer.GetCompatibleWithPlatform(target))
					{
						error = "Player compatibility remains enabled for " + target + ".";
						return false;
					}
				}
				catch (ArgumentException)
				{
				}
				catch (NotSupportedException)
				{
				}
			}
			return true;
		}

		private static string ComputeSha256(Stream stream)
		{
			stream.Position = 0L;
			using (SHA256 sha256 = SHA256.Create())
			{
				return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
			}
		}

		private static void CopyBounded(Stream source, Stream destination, long expectedBytes)
		{
			byte[] buffer = new byte[81920];
			long total = 0L;
			while (true)
			{
				int read = source.Read(buffer, 0, buffer.Length);
				if (read == 0)
				{
					break;
				}
				total = checked(total + read);
				if (total > expectedBytes)
				{
					throw new InvalidDataException("Bridge payload exceeds its declared size.");
				}
				destination.Write(buffer, 0, read);
			}
			if (total != expectedBytes)
			{
				throw new InvalidDataException("Bridge payload is truncated.");
			}
		}

		private static string CreateStagingRoot()
		{
			string root = Path.Combine(GetStagingBasePath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			return root;
		}

		private static string GetStagingBasePath()
		{
			return Path.Combine(
				Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
				"Temp",
				"PerfMeter",
				"RenderDocBridgeInstaller");
		}

		private static string GetInstalledFullPath()
		{
			return Path.GetFullPath(Path.Combine(Application.dataPath, "..", InstalledAssetPath));
		}

		private static bool IsSupportedHost()
		{
			return Application.platform == RuntimePlatform.WindowsEditor && IntPtr.Size == 8;
		}

		internal static bool CanManageInstalledAsset(out string error)
		{
			string fullPath = GetInstalledFullPath();
			if (TryValidateBridgeFile(fullPath, DllByteLength, DllSha256, out _, out error))
			{
				return true;
			}

			PluginImporter importer = AssetImporter.GetAtPath(InstalledAssetPath) as PluginImporter;
			if (importer != null && Array.IndexOf(AssetDatabase.GetLabels(importer), ManagedAssetLabel) >= 0)
			{
				error = string.Empty;
				return true;
			}
			if (!File.Exists(fullPath) && IsManagedOrphanMetaFile())
			{
				error = string.Empty;
				return true;
			}
			return false;
		}

		private static bool IsManagedOrphanMetaFile()
		{
			string path = GetInstalledFullPath() + ".meta";
			try
			{
				FileInfo info = new FileInfo(path);
				return info.Exists && info.Length <= 64L * 1024L &&
					(info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0 &&
					File.ReadAllText(path).IndexOf(ManagedAssetLabel, StringComparison.Ordinal) >= 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool TryValidateDestinationDirectory(string directory, out string error)
		{
			error = string.Empty;
			try
			{
				string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				string current = Path.GetFullPath(directory);
				string rootPrefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
				if (!current.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
				{
					error = "Bridge destination must remain inside the current project.";
					return false;
				}

				while (true)
				{
					FileAttributes attributes = File.GetAttributes(current);
					if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
					{
						error = "Bridge destination contains a reparse point or non-directory parent: " + current;
						return false;
					}
					if (string.Equals(current, projectRoot, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
					current = Path.GetDirectoryName(current);
				}
			}
			catch (Exception exception)
			{
				error = "Could not validate bridge destination: " + exception.Message;
				return false;
			}
		}

		private static bool TryDeleteInstalledAsset(out string error)
		{
			error = string.Empty;
			if (AssetDatabase.DeleteAsset(InstalledAssetPath) || !File.Exists(GetInstalledFullPath()))
			{
				return true;
			}
			error = "Unity could not remove " + InstalledAssetPath + ". Restart the Editor before retrying.";
			return false;
		}

		private static void PollDownload()
		{
			Update();
		}

		private static void CleanupBeforeDomainExit()
		{
			if (_download != null && !_download.isDone)
			{
				_download.Abort();
			}
			CleanupDownload();
		}

		private static void CleanupStaleStagingDirectories()
		{
			string root = GetStagingBasePath();
			if (!Directory.Exists(root))
			{
				return;
			}
			try
			{
				string[] directories = Directory.GetDirectories(root);
				for (int index = 0; index < directories.Length; index++)
				{
					DeleteDirectory(directories[index]);
				}
			}
			catch (Exception)
			{
			}
		}

		private static void CleanupDownload()
		{
			string stagingRoot = string.IsNullOrEmpty(_downloadPath) ? string.Empty : Path.GetDirectoryName(_downloadPath);
			_download?.Dispose();
			_download = null;
			_downloadHandler?.Close();
			_downloadHandler = null;
			_downloadPath = string.Empty;
			DeleteDirectory(stagingRoot);
		}

		private static void DeleteDirectory(string path)
		{
			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch (Exception)
				{
				}
			}
		}

		private sealed class BoundedFileDownloadHandler : DownloadHandlerScript
		{
			private readonly long _maximumBytes;
			private FileStream _stream;
			private long _receivedBytes;

			internal BoundedFileDownloadHandler(string path, long maximumBytes)
				: base(new byte[64 * 1024])
			{
				_maximumBytes = maximumBytes;
				_stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
			}

			internal string Error { get; private set; }

			protected override bool ReceiveData(byte[] data, int dataLength)
			{
				if (_stream == null || data == null || dataLength < 0 || dataLength > data.Length)
				{
					Error = "invalid download data";
					return false;
				}
				if (_receivedBytes > _maximumBytes - dataLength)
				{
					Error = "archive exceeds the 4 MiB safety limit";
					return false;
				}

				_stream.Write(data, 0, dataLength);
				_receivedBytes += dataLength;
				return true;
			}

			protected override void CompleteContent()
			{
				if (_stream != null)
				{
					_stream.Flush(true);
					Close();
				}
			}

			internal void Close()
			{
				_stream?.Dispose();
				_stream = null;
			}
		}
	}
}
