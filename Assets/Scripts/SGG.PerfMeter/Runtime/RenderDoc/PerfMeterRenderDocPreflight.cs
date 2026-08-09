using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterRenderDocPreflight
	{
		internal PerfMeterRenderDocPreflight(ulong requestNonce, string capturePathTemplate, string title)
		{
			RequestNonce = requestNonce;
			CapturePathTemplate = capturePathTemplate ?? string.Empty;
			Title = title ?? string.Empty;
		}

		internal ulong RequestNonce { get; }
		internal string CapturePathTemplate { get; }
		internal string Title { get; }
	}

	internal interface IPerfMeterRenderDocPreflightProvider
	{
		SggRdResult Prepare(PerfMeterCaptureOptions options, out PerfMeterRenderDocPreflight preflight);
	}

	internal sealed class PerfMeterRenderDocPreflightProvider : IPerfMeterRenderDocPreflightProvider
	{
		private const string PolicyNotReadyWarning =
			"RenderDoc native preflight is unavailable until PM-RDOC-003C source ownership and quota policy are implemented.";

		public SggRdResult Prepare(PerfMeterCaptureOptions options, out PerfMeterRenderDocPreflight preflight)
		{
			try
			{
				ulong requestNonce = CreateCryptographicNonce();
				string capturePathTemplate = CreateAbsolutePathTemplate(requestNonce);
				string title = CreateBoundedTitle(options.CaptureId);
				preflight = new PerfMeterRenderDocPreflight(requestNonce, capturePathTemplate, title);
			}
			catch (Exception)
			{
				preflight = default;
				return SggRdResult.InternalError;
			}

			// This control-only assembly deliberately does not own marker creation, quota
			// reservation, free-space checks, or cleanup. PM-RDOC-003C must make this
			// provider successful before any production bootstrap can use it.
			return SggRdResult.InternalError;
		}

		internal static string PolicyNotReadyMessage => PolicyNotReadyWarning;

		private static ulong CreateCryptographicNonce()
		{
			byte[] randomBytes = new byte[sizeof(ulong)];
			using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
			{
				do
				{
					generator.GetBytes(randomBytes);
				}
				while (BitConverter.ToUInt64(randomBytes, 0) == 0u);
			}

			return BitConverter.ToUInt64(randomBytes, 0);
		}

		private static string CreateAbsolutePathTemplate(ulong requestNonce)
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string nonceDirectory = requestNonce.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
			return Path.GetFullPath(Path.Combine(
				projectRoot,
				"Temp",
				"PerfMeter",
				"RenderDoc",
				nonceDirectory,
				"capture"));
		}

		private static string CreateBoundedTitle(string captureId)
		{
			string candidate = string.IsNullOrEmpty(captureId)
				? "PerfMeter RenderDoc Capture"
				: "PerfMeter " + captureId;
			if (PerfMeterRenderDocUtf8.TryEncode(
				candidate,
				PerfMeterRenderDocAbiV1.MaxTitleBytes,
				false,
				out byte[] candidateBytes))
			{
				return candidate;
			}

			if (!PerfMeterRenderDocUtf8.TryEncode(candidate, int.MaxValue, false, out candidateBytes))
			{
				return "PerfMeter RenderDoc Capture";
			}

			byte[] digest;
			using (SHA256 sha256 = SHA256.Create())
			{
				digest = sha256.ComputeHash(candidateBytes);
			}

			StringBuilder hex = new StringBuilder(digest.Length * 2);
			for (int index = 0; index < digest.Length; index++)
			{
				hex.Append(digest[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			return "PerfMeter RenderDoc " + hex;
		}
	}
}
