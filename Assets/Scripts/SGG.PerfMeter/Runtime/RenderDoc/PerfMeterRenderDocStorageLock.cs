using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderDocStorageLock
	{
		private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(5);

		internal static bool TryAcquire(string canonicalRootPath, out IDisposable lease, out string error)
		{
			lease = null;
			error = string.Empty;
			if (string.IsNullOrEmpty(canonicalRootPath))
			{
				error = "renderdoc_storage_lock_path_invalid";
				return false;
			}

			Mutex mutex = null;
			try
			{
				mutex = new Mutex(false, CreateName(canonicalRootPath));
				bool acquired;
				try
				{
					acquired = mutex.WaitOne(AcquireTimeout);
				}
				catch (AbandonedMutexException)
				{
					acquired = true;
				}

				if (!acquired)
				{
					mutex.Dispose();
					error = "renderdoc_storage_lock_timeout";
					return false;
				}

				lease = new MutexLease(mutex);
				return true;
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SecurityException || exception is WaitHandleCannotBeOpenedException || exception is ArgumentException)
			{
				mutex?.Dispose();
				error = "renderdoc_storage_lock_unavailable";
				return false;
			}
		}

		private static string CreateName(string canonicalRootPath)
		{
			string normalized = canonicalRootPath.Replace('\\', '/').ToUpperInvariant();
			byte[] digest;
			using (SHA256 sha256 = SHA256.Create())
			{
				digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
			}

			StringBuilder builder = new StringBuilder(32);
			for (int index = 0; index < 16; index++)
			{
				builder.Append(digest[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			return "Local\\SGG.PerfMeter.RenderDoc." + builder;
		}

		private sealed class MutexLease : IDisposable
		{
			private Mutex _mutex;

			internal MutexLease(Mutex mutex)
			{
				_mutex = mutex;
			}

			public void Dispose()
			{
				Mutex mutex = Interlocked.Exchange(ref _mutex, null);
				if (mutex == null)
				{
					return;
				}

				try
				{
					mutex.ReleaseMutex();
				}
				finally
				{
					mutex.Dispose();
				}
			}
		}
	}
}
