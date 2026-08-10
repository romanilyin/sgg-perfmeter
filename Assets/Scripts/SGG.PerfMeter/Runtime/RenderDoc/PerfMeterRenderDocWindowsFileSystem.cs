using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderDocWindowsFileSystem
	{
		private const uint GenericRead = 0x80000000u;
		private const uint DeleteAccess = 0x00010000u;
		private const uint FileListDirectory = 0x00000001u;
		private const uint FileReadAttributes = 0x00000080u;
		private const uint ShareRead = 0x00000001u;
		private const uint ShareWrite = 0x00000002u;
		private const uint ShareDelete = 0x00000004u;
		private const uint OpenExisting = 3u;
		private const uint FileFlagBackupSemantics = 0x02000000u;
		private const uint FileFlagOpenReparsePoint = 0x00200000u;
		private const uint FileFlagSequentialScan = 0x08000000u;

		internal static bool IsSupported => Environment.OSVersion.Platform == PlatformID.Win32NT;

		internal static SggRdResult TryOpenRegularFile(
			string path,
			out FileStream stream,
			out string error)
		{
			stream = null;
			error = string.Empty;
			if (!IsSupported)
			{
				error = "renderdoc_file_identity_unsupported";
				return SggRdResult.UnsupportedPlatform;
			}

			if (!TryGetFullPath(path, out string fullPath))
			{
				error = "renderdoc_file_path_invalid";
				return SggRdResult.InvalidArgument;
			}

			SafeFileHandle handle = CreateFileW(
				fullPath,
				GenericRead | FileReadAttributes,
				ShareRead | ShareWrite | ShareDelete,
				IntPtr.Zero,
				OpenExisting,
				FileFlagOpenReparsePoint | FileFlagSequentialScan,
				IntPtr.Zero);
			if (handle.IsInvalid)
			{
				handle.Dispose();
				error = "renderdoc_file_open_failed";
				return SggRdResult.CaptureNotObserved;
			}

			if (!TryValidateHandle(handle, fullPath, expectDirectory: false))
			{
				handle.Dispose();
				error = "renderdoc_file_path_reparse_or_changed";
				return SggRdResult.CaptureFailed;
			}

			try
			{
				stream = new FileStream(handle, FileAccess.Read, 81920, false);
				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				handle.Dispose();
				error = "renderdoc_file_open_failed";
				return SggRdResult.CaptureNotObserved;
			}
		}

		internal static SggRdResult TryDeleteOwnedRoot(
			string rootPath,
			byte[] expectedMarkerBytes,
			out string error)
		{
			error = string.Empty;
			if (!IsSupported)
			{
				error = "renderdoc_storage_handle_delete_unsupported";
				return SggRdResult.UnsupportedPlatform;
			}

			if (!TryGetFullPath(rootPath, out string fullRootPath) ||
				expectedMarkerBytes == null || expectedMarkerBytes.Length == 0 ||
				expectedMarkerBytes.Length > PerfMeterRenderDocStoragePolicy.MaxMarkerBytes)
			{
				error = "renderdoc_storage_cleanup_claim_invalid";
				return SggRdResult.InvalidArgument;
			}

			SafeFileHandle rootHandle = CreateFileW(
				fullRootPath,
				DeleteAccess | FileListDirectory | FileReadAttributes,
				ShareRead | ShareWrite,
				IntPtr.Zero,
				OpenExisting,
				FileFlagBackupSemantics | FileFlagOpenReparsePoint,
				IntPtr.Zero);
			if (rootHandle.IsInvalid)
			{
				rootHandle.Dispose();
				error = "renderdoc_storage_cleanup_pending";
				return SggRdResult.InternalError;
			}

			List<OwnedEntry> entries = new List<OwnedEntry>();
			try
			{
				if (!TryValidateHandle(rootHandle, fullRootPath, expectDirectory: true))
				{
					error = "renderdoc_storage_root_reparse_or_changed";
					return SggRdResult.InvalidArgument;
				}

				SggRdResult openResult = TryOpenOwnedEntries(
					rootHandle,
					fullRootPath,
					expectedMarkerBytes,
					entries,
					out error);
				if (openResult != SggRdResult.Ok)
				{
					return openResult;
				}
				if (!TryValidateHandle(rootHandle, fullRootPath, expectDirectory: true))
				{
					error = "renderdoc_storage_root_reparse_or_changed";
					return SggRdResult.InvalidArgument;
				}

				for (int index = 0; index < entries.Count; index++)
				{
					if (entries[index].IsMarker)
					{
						continue;
					}

					if (!TrySetDeleteDisposition(entries[index].Handle))
					{
						error = "renderdoc_storage_cleanup_pending";
						return SggRdResult.InternalError;
					}
					entries[index].Dispose();
				}

				OwnedEntry markerEntry = entries.Find(entry => entry.IsMarker);
				if (markerEntry == null || !TrySetDeleteDisposition(markerEntry.Handle))
				{
					error = "renderdoc_storage_cleanup_pending";
					return SggRdResult.InternalError;
				}
				markerEntry.Dispose();

				if (!TrySetDeleteDisposition(rootHandle))
				{
					error = "renderdoc_storage_cleanup_pending";
					return SggRdResult.InternalError;
				}
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_storage_cleanup_pending";
				return SggRdResult.InternalError;
			}
			finally
			{
				for (int index = 0; index < entries.Count; index++)
				{
					entries[index].Dispose();
				}
				rootHandle.Dispose();
			}

			return SggRdResult.Ok;
		}

		private static SggRdResult TryOpenOwnedEntries(
			SafeFileHandle rootHandle,
			string rootPath,
			byte[] expectedMarkerBytes,
			List<OwnedEntry> entries,
			out string error)
		{
			error = string.Empty;
			HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
			bool hasMarker = false;
			bool hasPayload = false;
			foreach (string entryPath in Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.TopDirectoryOnly))
			{
				string name = Path.GetFileName(entryPath);
				bool isMarker = string.Equals(name, PerfMeterRenderDocStoragePolicy.MarkerFileName, StringComparison.Ordinal);
				bool isPayload = !isMarker && string.Equals(Path.GetExtension(name), ".rdc", StringComparison.OrdinalIgnoreCase);
				if ((!isMarker && !isPayload) || (isMarker && hasMarker) || (isPayload && hasPayload) || !names.Add(name))
				{
					error = "renderdoc_storage_unknown_content";
					return SggRdResult.InvalidArgument;
				}

				SafeFileHandle handle = CreateFileW(
					entryPath,
					GenericRead | DeleteAccess | FileReadAttributes,
					ShareRead | ShareWrite,
					IntPtr.Zero,
					OpenExisting,
					FileFlagOpenReparsePoint | FileFlagSequentialScan,
					IntPtr.Zero);
				if (handle.IsInvalid)
				{
					handle.Dispose();
					error = "renderdoc_storage_cleanup_pending";
					return SggRdResult.InternalError;
				}

				if (!TryValidateOwnedEntryHandle(rootHandle, handle, name))
				{
					handle.Dispose();
					error = "renderdoc_storage_unknown_content";
					return SggRdResult.InvalidArgument;
				}

				OwnedEntry entry = new OwnedEntry(handle, isMarker);
				entries.Add(entry);
				if (isMarker)
				{
					hasMarker = true;
					if (!MarkerBytesEqual(handle, expectedMarkerBytes))
					{
						error = "renderdoc_storage_marker_mismatch";
						return SggRdResult.InvalidArgument;
					}
				}
				else
				{
					hasPayload = true;
				}
			}

			if (!hasMarker)
			{
				error = "renderdoc_storage_marker_invalid";
				return SggRdResult.InvalidArgument;
			}
			if (!TryValidateHandle(rootHandle, rootPath, expectDirectory: true))
			{
				error = "renderdoc_storage_root_reparse_or_changed";
				return SggRdResult.InvalidArgument;
			}

			HashSet<string> currentNames = new HashSet<string>(StringComparer.Ordinal);
			foreach (string entryPath in Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.TopDirectoryOnly))
			{
				currentNames.Add(Path.GetFileName(entryPath));
			}
			if (!currentNames.SetEquals(names))
			{
				error = "renderdoc_storage_unknown_content";
				return SggRdResult.InvalidArgument;
			}

			return SggRdResult.Ok;
		}

		private static bool MarkerBytesEqual(SafeFileHandle handle, byte[] expected)
		{
			if (!GetFileSizeEx(handle, out long length) || length != expected.LongLength)
			{
				return false;
			}

			byte[] actual = new byte[expected.Length];
			if (!ReadFile(handle, actual, actual.Length, out int bytesRead, IntPtr.Zero) || bytesRead != actual.Length)
			{
				return false;
			}

			int difference = 0;
			for (int index = 0; index < expected.Length; index++)
			{
				difference |= actual[index] ^ expected[index];
			}
			return difference == 0;
		}

		private static bool TryValidateHandle(
			SafeFileHandle handle,
			string expectedPath,
			bool expectDirectory)
		{
			if (!GetFileInformationByHandle(handle, out ByHandleFileInformation information))
			{
				return false;
			}

			FileAttributes attributes = (FileAttributes)information.FileAttributes;
			if ((attributes & FileAttributes.ReparsePoint) != 0 ||
				((attributes & FileAttributes.Directory) != 0) != expectDirectory ||
				!TryGetFinalPath(handle, out string finalPath) ||
				!TryGetFullPath(expectedPath, out string fullExpectedPath))
			{
				return false;
			}

			return string.Equals(finalPath, fullExpectedPath, StringComparison.OrdinalIgnoreCase);
		}

		private static bool TryValidateOwnedEntryHandle(
			SafeFileHandle rootHandle,
			SafeFileHandle entryHandle,
			string expectedName)
		{
			if (!GetFileInformationByHandle(entryHandle, out ByHandleFileInformation information))
			{
				return false;
			}

			FileAttributes attributes = (FileAttributes)information.FileAttributes;
			if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 ||
				!TryGetFinalPath(rootHandle, out string finalRootPath) ||
				!TryGetFinalPath(entryHandle, out string finalEntryPath))
			{
				return false;
			}

			return string.Equals(Path.GetDirectoryName(finalEntryPath), finalRootPath, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(Path.GetFileName(finalEntryPath), expectedName, StringComparison.OrdinalIgnoreCase);
		}

		private static bool TryGetFinalPath(SafeFileHandle handle, out string path)
		{
			path = string.Empty;
			StringBuilder buffer = new StringBuilder(512);
			for (int attempt = 0; attempt < 2; attempt++)
			{
				uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0u);
				if (length == 0u)
				{
					return false;
				}
				if (length < buffer.Capacity)
				{
					return TryGetFullPath(RemoveExtendedPrefix(buffer.ToString()), out path);
				}
				if (length >= int.MaxValue - 1)
				{
					return false;
				}
				buffer = new StringBuilder((int)length + 1);
			}

			return false;
		}

		private static string RemoveExtendedPrefix(string path)
		{
			const string uncPrefix = @"\\?\UNC\";
			const string localPrefix = @"\\?\";
			if (path.StartsWith(uncPrefix, StringComparison.OrdinalIgnoreCase))
			{
				return @"\\" + path.Substring(uncPrefix.Length);
			}
			return path.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase)
				? path.Substring(localPrefix.Length)
				: path;
		}

		private static bool TryGetFullPath(string path, out string fullPath)
		{
			fullPath = string.Empty;
			try
			{
				if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
				{
					return false;
				}

				fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				return fullPath.Length > 0;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				return false;
			}
		}

		private static bool TrySetDeleteDisposition(SafeFileHandle handle)
		{
			FileDispositionInformation information = new FileDispositionInformation { DeleteFile = true };
			return SetFileInformationByHandle(
				handle,
				FileInformationClass.FileDispositionInfo,
				ref information,
				(uint)Marshal.SizeOf<FileDispositionInformation>());
		}

		private static bool IsIoException(Exception exception)
		{
			return exception is IOException ||
				exception is UnauthorizedAccessException ||
				exception is ArgumentException ||
				exception is NotSupportedException;
		}

		private sealed class OwnedEntry : IDisposable
		{
			private bool _disposed;

			internal OwnedEntry(SafeFileHandle handle, bool isMarker)
			{
				Handle = handle;
				IsMarker = isMarker;
			}

			internal SafeFileHandle Handle { get; }
			internal bool IsMarker { get; }

			public void Dispose()
			{
				if (_disposed)
				{
					return;
				}
				_disposed = true;
				Handle.Dispose();
			}
		}

		internal sealed class FileBindingFactory : IPerfMeterRenderDocFileBindingFactory
		{
			public SggRdResult TryOpen(string path, out IPerfMeterRenderDocFileBinding binding, out string error)
			{
				binding = null;
				SggRdResult result = TryOpenRegularFile(path, out FileStream stream, out error);
				if (result == SggRdResult.Ok)
				{
					binding = new FileBinding(stream);
				}
				return result;
			}
		}

		private sealed class FileBinding : IPerfMeterRenderDocFileBinding
		{
			private readonly FileStream _stream;
			private readonly SafeFileHandle _handle;

			internal FileBinding(FileStream stream)
			{
				_stream = stream;
				_handle = stream.SafeFileHandle;
			}

			public SggRdResult TrySample(out PerfMeterRenderDocFileSample sample, out string error)
			{
				sample = default;
				error = string.Empty;
				if (!GetFileInformationByHandle(_stream.SafeFileHandle, out ByHandleFileInformation information))
				{
					error = "renderdoc_file_identity_failed";
					return SggRdResult.InternalError;
				}

				byte[] identity = new byte[12];
				WriteUInt32(identity, 0, information.VolumeSerialNumber);
				WriteUInt32(identity, 4, information.FileIndexHigh);
				WriteUInt32(identity, 8, information.FileIndexLow);
				long size = ((long)information.FileSizeHigh << 32) | information.FileSizeLow;
				long writeTicks = ((long)information.LastWriteTimeHigh << 32) | information.LastWriteTimeLow;
				sample = new PerfMeterRenderDocFileSample(identity, size, writeTicks);
				return SggRdResult.Ok;
			}

			public SggRdResult TryComputeSha256(
				long maximumBytes,
				Func<bool> shouldStop,
				out string sha256,
				out string error)
			{
				sha256 = string.Empty;
				error = string.Empty;
				try
				{
					_stream.Position = 0L;
					using (System.Security.Cryptography.SHA256 algorithm = System.Security.Cryptography.SHA256.Create())
					{
						byte[] buffer = new byte[81920];
						long total = 0L;
						int read;
						while ((read = _stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							bool stopped = IsCanceled(shouldStop);
							if (stopped || read > maximumBytes - total)
							{
								error = stopped ? "renderdoc_file_hash_stopped" : "renderdoc_storage_payload_limit_exceeded";
								return SggRdResult.CaptureFailed;
							}
							algorithm.TransformBlock(buffer, 0, read, null, 0);
							total += read;
						}
						algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
						if (IsCanceled(shouldStop))
						{
							error = "renderdoc_file_hash_stopped";
							return SggRdResult.CaptureFailed;
						}
						sha256 = ToHex(algorithm.Hash);
					}
					return SggRdResult.Ok;
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
				{
					error = "renderdoc_file_hash_failed";
					return SggRdResult.InternalError;
				}
			}

			public SggRdResult TryCopyTo(
				string destinationPath,
				long maximumBytes,
				Func<bool> shouldStop,
				out string error)
			{
				error = string.Empty;
				try
				{
					_stream.Position = 0L;
					using (FileStream destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
					{
						byte[] buffer = new byte[81920];
						long total = 0L;
						int read;
						while ((read = _stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							bool stopped = IsCanceled(shouldStop);
							if (stopped || read > maximumBytes - total)
							{
								error = stopped ? "renderdoc_copy_stopped" : "renderdoc_storage_payload_limit_exceeded";
								return SggRdResult.CaptureFailed;
							}
							destination.Write(buffer, 0, read);
							total += read;
						}
						destination.Flush(true);
						if (IsCanceled(shouldStop))
						{
							error = "renderdoc_copy_stopped";
							return SggRdResult.CaptureFailed;
						}
					}
					return SggRdResult.Ok;
				}
				catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
				{
					error = "renderdoc_copy_failed";
					return SggRdResult.InternalError;
				}
			}

			public void Dispose()
			{
				_stream.Dispose();
				_handle.Dispose();
			}

			private static void WriteUInt32(byte[] bytes, int offset, uint value)
			{
				bytes[offset] = (byte)value;
				bytes[offset + 1] = (byte)(value >> 8);
				bytes[offset + 2] = (byte)(value >> 16);
				bytes[offset + 3] = (byte)(value >> 24);
			}
		}

		private static bool IsCanceled(Func<bool> predicate)
		{
			if (predicate == null)
			{
				return false;
			}
			try
			{
				return predicate();
			}
			catch (Exception)
			{
				return true;
			}
		}

		private static string ToHex(byte[] bytes)
		{
			char[] characters = new char[bytes.Length * 2];
			const string alphabet = "0123456789abcdef";
			for (int index = 0; index < bytes.Length; index++)
			{
				characters[index * 2] = alphabet[bytes[index] >> 4];
				characters[index * 2 + 1] = alphabet[bytes[index] & 0xf];
			}
			return new string(characters);
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeFileHandle CreateFileW(
			string fileName,
			uint desiredAccess,
			uint shareMode,
			IntPtr securityAttributes,
			uint creationDisposition,
			uint flagsAndAttributes,
			IntPtr templateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetFileInformationByHandle(
			SafeFileHandle file,
			out ByHandleFileInformation information);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetFileSizeEx(SafeFileHandle file, out long fileSize);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ReadFile(
			SafeFileHandle file,
			[Out] byte[] buffer,
			int bytesToRead,
			out int bytesRead,
			IntPtr overlapped);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint GetFinalPathNameByHandleW(
			SafeFileHandle file,
			[Out] StringBuilder filePath,
			uint filePathLength,
			uint flags);

		[DllImport("kernel32.dll", EntryPoint = "SetFileInformationByHandle", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetFileInformationByHandle(
			SafeFileHandle file,
			FileInformationClass informationClass,
			ref FileDispositionInformation information,
			uint bufferSize);

		private enum FileInformationClass
		{
			FileDispositionInfo = 4
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct FileDispositionInformation
		{
			[MarshalAs(UnmanagedType.Bool)]
			internal bool DeleteFile;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ByHandleFileInformation
		{
			internal uint FileAttributes;
			internal uint CreationTimeLow;
			internal uint CreationTimeHigh;
			internal uint LastAccessTimeLow;
			internal uint LastAccessTimeHigh;
			internal uint LastWriteTimeLow;
			internal uint LastWriteTimeHigh;
			internal uint VolumeSerialNumber;
			internal uint FileSizeHigh;
			internal uint FileSizeLow;
			internal uint NumberOfLinks;
			internal uint FileIndexHigh;
			internal uint FileIndexLow;
		}
	}
}
