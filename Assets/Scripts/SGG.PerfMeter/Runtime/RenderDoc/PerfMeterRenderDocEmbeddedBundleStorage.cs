using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterRenderDocEmbeddedBundle
	{
		internal PerfMeterRenderDocEmbeddedBundle(
			string rootPath,
			string sessionId,
			ulong generation,
			ulong requestNonce,
			DateTimeOffset stateUtc,
			long ownedBytes,
			long payloadBytes,
			string payloadSha256)
		{
			RootPath = rootPath ?? string.Empty;
			SessionId = sessionId ?? string.Empty;
			Generation = generation;
			RequestNonce = requestNonce;
			StateUtc = stateUtc;
			OwnedBytes = ownedBytes;
			PayloadBytes = payloadBytes;
			PayloadSha256 = payloadSha256 ?? string.Empty;
		}

		internal string RootPath { get; }
		internal string SessionId { get; }
		internal ulong Generation { get; }
		internal ulong RequestNonce { get; }
		internal DateTimeOffset StateUtc { get; }
		internal long OwnedBytes { get; }
		internal long PayloadBytes { get; }
		internal string PayloadSha256 { get; }
	}

	internal sealed class PerfMeterRenderDocEmbeddedBundleStorage
	{
		private const int MarkerLineCount = 10;
		private const int MaxEnvelopeBytes = 256 * 1024;
		private const int MaxTreeEntries = 1024;
		private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
		private readonly string _projectRoot;
		private readonly string _bundleRoot;
		private readonly Func<DateTimeOffset> _utcNow;

		internal PerfMeterRenderDocEmbeddedBundleStorage(string projectRoot, Func<DateTimeOffset> utcNow)
		{
			_projectRoot = NormalizeDirectory(Path.GetFullPath(projectRoot));
			_bundleRoot = NormalizeDirectory(Path.GetFullPath(Path.Combine(
				_projectRoot,
				PerfMeterCaptureBundleExporter.RelativeBundleRoot)));
			_utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
		}

		internal static byte[] CreateMarkerBytes(
			PerfMeterRenderDocStorageRequest request,
			ulong requestNonce,
			DateTimeOffset stateUtc,
			long payloadBytes,
			string payloadSha256)
		{
			string marker = PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedMarkerHeader +
				"owning_session=" + Convert.ToBase64String(StrictUtf8.GetBytes(request.SessionId ?? string.Empty)) + "\n" +
				"generation=" + request.Generation.ToString(CultureInfo.InvariantCulture) + "\n" +
				"request_nonce=" + requestNonce.ToString("x16", CultureInfo.InvariantCulture) + "\n" +
				"state=terminal\n" +
				"state_utc=" + stateUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + "\n" +
				"payload_size_bytes=" + payloadBytes.ToString(CultureInfo.InvariantCulture) + "\n" +
				"payload_sha256=" + (payloadSha256 ?? string.Empty) + "\n";
			return StrictUtf8.GetBytes(marker);
		}

		internal SggRdResult TryScan(
			out List<PerfMeterRenderDocEmbeddedBundle> bundles,
			out string error)
		{
			bundles = new List<PerfMeterRenderDocEmbeddedBundle>();
			error = string.Empty;
			if (!Directory.Exists(_bundleRoot))
			{
				return SggRdResult.Ok;
			}

			try
			{
				if (!IsSafeDirectory(_bundleRoot) || !IsSafePathUnder(_projectRoot, _bundleRoot))
				{
					error = "renderdoc_embed_bundle_root_invalid";
					return SggRdResult.InvalidArgument;
				}

				foreach (string bundlePath in Directory.EnumerateDirectories(_bundleRoot, "*", SearchOption.TopDirectoryOnly))
				{
					string markerPath = Path.Combine(
						bundlePath,
						PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedMarkerRelativePath);
					if (!File.Exists(markerPath))
					{
						continue;
					}

					SggRdResult result = TryInspect(bundlePath, markerPath, out PerfMeterRenderDocEmbeddedBundle bundle, out error);
					if (result != SggRdResult.Ok)
					{
						return result;
					}
					bundles.Add(bundle);
				}

				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_embed_bundle_scan_failed";
				return SggRdResult.InternalError;
			}
		}

		internal SggRdResult TryDelete(PerfMeterRenderDocEmbeddedBundle bundle, out string error)
		{
			error = string.Empty;
			try
			{
				if (!Directory.Exists(bundle.RootPath))
				{
					return SggRdResult.Ok;
				}
				string markerPath = Path.Combine(
					bundle.RootPath,
					PerfMeterNativeExternalArtifactSourceDescriptor.EmbeddedMarkerRelativePath);
				SggRdResult inspectResult = TryInspect(
					bundle.RootPath,
					markerPath,
					out PerfMeterRenderDocEmbeddedBundle current,
					out error);
				if (inspectResult != SggRdResult.Ok)
				{
					return inspectResult;
				}
				if (!Matches(bundle, current))
				{
					error = string.IsNullOrEmpty(error)
						? "renderdoc_embed_bundle_identity_changed"
						: error;
					return SggRdResult.InvalidArgument;
				}

				Directory.Delete(bundle.RootPath, true);
				return SggRdResult.Ok;
			}
			catch (Exception exception) when (IsIoException(exception))
			{
				error = "renderdoc_embed_bundle_cleanup_pending";
				return SggRdResult.InternalError;
			}
		}

		private SggRdResult TryInspect(
			string bundlePath,
			string markerPath,
			out PerfMeterRenderDocEmbeddedBundle bundle,
			out string error)
		{
			bundle = default;
			error = string.Empty;
			if (!IsDirectChild(_bundleRoot, bundlePath) ||
				!IsSafePathUnder(_projectRoot, bundlePath) ||
				!PerfMeterCaptureBundleExporter.IsOwnedCommittedBundle(bundlePath) ||
				!TryMeasureSafeTree(bundlePath, out long ownedBytes) ||
				!IsSafeRegularFile(markerPath) ||
				!IsSafePathUnder(bundlePath, markerPath))
			{
				error = "renderdoc_embed_bundle_ownership_invalid";
				return SggRdResult.InvalidArgument;
			}

			FileInfo markerInfo = new FileInfo(markerPath);
			if (markerInfo.Length <= 0L || markerInfo.Length > 64L * 1024L)
			{
				error = "renderdoc_embed_marker_invalid";
				return SggRdResult.InvalidArgument;
			}

			string[] lines = File.ReadAllText(markerPath, StrictUtf8)
				.Split(new[] { '\n' }, StringSplitOptions.None);
			if (lines.Length != MarkerLineCount ||
				lines[MarkerLineCount - 1].Length != 0 ||
				!string.Equals(lines[0], "sgg.perfmeter.renderdoc-embed", StringComparison.Ordinal) ||
				!string.Equals(lines[1], "1", StringComparison.Ordinal) ||
				!TryValue(lines[2], "owning_session", out string encodedSession) ||
				!TryValue(lines[3], "generation", out string generationText) ||
				!TryValue(lines[4], "request_nonce", out string nonceText) ||
				!string.Equals(lines[5], "state=terminal", StringComparison.Ordinal) ||
				!TryValue(lines[6], "state_utc", out string stateUtcText) ||
				!TryValue(lines[7], "payload_size_bytes", out string payloadBytesText) ||
				!TryValue(lines[8], "payload_sha256", out string payloadSha256))
			{
				error = "renderdoc_embed_marker_invalid";
				return SggRdResult.InvalidArgument;
			}

			string sessionId;
			try
			{
				sessionId = StrictUtf8.GetString(Convert.FromBase64String(encodedSession));
			}
			catch (Exception exception) when (exception is FormatException || exception is DecoderFallbackException)
			{
				error = "renderdoc_embed_marker_invalid";
				return SggRdResult.InvalidArgument;
			}

			if (string.IsNullOrEmpty(sessionId) ||
				sessionId.Length > PerfMeterRenderDocStoragePolicy.MaxSessionIdLength ||
				ContainsControlCharacter(sessionId) ||
				!ulong.TryParse(generationText, NumberStyles.None, CultureInfo.InvariantCulture, out ulong generation) ||
				!ulong.TryParse(nonceText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong nonce) ||
				nonce == 0u || nonceText.Length != 16 ||
				!DateTimeOffset.TryParseExact(stateUtcText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset stateUtc) ||
				stateUtc.Offset != TimeSpan.Zero ||
				stateUtc.ToUniversalTime() > _utcNow().ToUniversalTime() ||
				!long.TryParse(payloadBytesText, NumberStyles.None, CultureInfo.InvariantCulture, out long payloadBytes) ||
				payloadBytes <= 0L || payloadBytes > PerfMeterRenderDocStoragePolicy.MaxPayloadBytes ||
				!IsSha256(payloadSha256))
			{
				error = "renderdoc_embed_marker_invalid";
				return SggRdResult.InvalidArgument;
			}

			string payloadPath = Path.Combine(bundlePath, "external", "renderdoc", "capture.rdc");
			string envelopePath = Path.Combine(bundlePath, "external-artifact.json");
			FileInfo envelopeInfo = new FileInfo(envelopePath);
			if (!IsSafeRegularFile(payloadPath) ||
				!IsSafePathUnder(bundlePath, payloadPath) ||
				new FileInfo(payloadPath).Length != payloadBytes ||
				!IsSafeRegularFile(envelopePath) ||
				!IsSafePathUnder(bundlePath, envelopePath) ||
				envelopeInfo.Length <= 0L || envelopeInfo.Length > MaxEnvelopeBytes)
			{
				error = "renderdoc_embed_payload_invalid";
				return SggRdResult.InvalidArgument;
			}

			string envelope = File.ReadAllText(envelopePath, StrictUtf8);
			if (!envelope.StartsWith("{\"schema\":\"sgg.perfmeter.external-artifact\",\"schema_version\":1,", StringComparison.Ordinal) ||
				envelope.IndexOf("\"storage_mode\":\"Embed\"", StringComparison.Ordinal) < 0 ||
				envelope.IndexOf("\"authority_state\":\"Authenticated\"", StringComparison.Ordinal) < 0 ||
				envelope.IndexOf("\"post_copy_sha256\":\"" + payloadSha256 + "\"", StringComparison.Ordinal) < 0 ||
				envelope.IndexOf(
					"\"generation\":" + generation.ToString(CultureInfo.InvariantCulture) +
					",\"request_nonce\":\"" + nonceText + "\"",
					StringComparison.Ordinal) < 0)
			{
				error = "renderdoc_embed_provenance_invalid";
				return SggRdResult.InvalidArgument;
			}

			bundle = new PerfMeterRenderDocEmbeddedBundle(
				bundlePath,
				sessionId,
				generation,
				nonce,
				stateUtc.ToUniversalTime(),
				ownedBytes,
				payloadBytes,
				payloadSha256);
			return SggRdResult.Ok;
		}

		private static bool TryValue(string line, string key, out string value)
		{
			string prefix = key + "=";
			if (!line.StartsWith(prefix, StringComparison.Ordinal))
			{
				value = string.Empty;
				return false;
			}

			value = line.Substring(prefix.Length);
			return value.Length > 0;
		}

		private static bool Matches(
			PerfMeterRenderDocEmbeddedBundle expected,
			PerfMeterRenderDocEmbeddedBundle current)
		{
			return string.Equals(expected.RootPath, current.RootPath, PathComparison) &&
				string.Equals(expected.SessionId, current.SessionId, StringComparison.Ordinal) &&
				expected.Generation == current.Generation &&
				expected.RequestNonce == current.RequestNonce &&
				expected.StateUtc.Equals(current.StateUtc) &&
				expected.OwnedBytes == current.OwnedBytes &&
				expected.PayloadBytes == current.PayloadBytes &&
				string.Equals(expected.PayloadSha256, current.PayloadSha256, StringComparison.Ordinal);
		}

		private static bool ContainsControlCharacter(string value)
		{
			for (int index = 0; index < value.Length; index++)
			{
				if (char.IsControl(value[index]))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsDirectChild(string root, string path)
		{
			string fullRoot = NormalizeDirectory(Path.GetFullPath(root));
			string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return string.Equals(
				Path.GetDirectoryName(fullPath)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				fullRoot,
				PathComparison) &&
				IsSafeDirectory(fullPath);
		}

		private static bool IsSafeDirectory(string path)
		{
			return Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
		}

		private static bool IsSafeRegularFile(string path)
		{
			return File.Exists(path) &&
				(File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
		}

		private static bool TryMeasureSafeTree(string root, out long ownedBytes)
		{
			ownedBytes = 0L;
			Stack<string> pending = new Stack<string>();
			pending.Push(root);
			int entryCount = 0;
			while (pending.Count > 0)
			{
				foreach (string entry in Directory.EnumerateFileSystemEntries(
					pending.Pop(),
					"*",
					SearchOption.TopDirectoryOnly))
				{
					if (++entryCount > MaxTreeEntries)
					{
						return false;
					}

					FileAttributes attributes = File.GetAttributes(entry);
					if ((attributes & FileAttributes.ReparsePoint) != 0)
					{
						return false;
					}
					if ((attributes & FileAttributes.Directory) != 0)
					{
						pending.Push(entry);
					}
					else
					{
						ownedBytes = SaturatingAdd(ownedBytes, new FileInfo(entry).Length);
					}
				}
			}

			return true;
		}

		private static bool IsSafePathUnder(string root, string target)
		{
			string fullRoot = NormalizeDirectory(Path.GetFullPath(root));
			string current = Path.GetFullPath(target);
			while (!string.IsNullOrEmpty(current) && current.Length >= fullRoot.Length)
			{
				if ((Directory.Exists(current) || File.Exists(current)) &&
					(File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
				{
					return false;
				}
				if (string.Equals(NormalizeDirectory(current), fullRoot, PathComparison))
				{
					return true;
				}
				current = Path.GetDirectoryName(current);
			}

			return false;
		}

		private static bool IsSha256(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length != 64)
			{
				return false;
			}
			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
				{
					return false;
				}
			}
			return true;
		}

		private static string NormalizeDirectory(string path)
		{
			return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		private static long SaturatingAdd(long left, long right)
		{
			return left > long.MaxValue - right ? long.MaxValue : left + right;
		}

		private static bool IsIoException(Exception exception)
		{
			return exception is IOException ||
				exception is UnauthorizedAccessException ||
				exception is ArgumentException ||
				exception is NotSupportedException ||
				exception is System.Security.SecurityException;
		}
	}
}
