using System;
using System.Runtime.InteropServices;

namespace SGG.PerfMeter
{
	internal enum SggRdResult : uint
	{
		Ok = 0,
		NotLoaded = 1,
		ExportMissing = 2,
		ApiNegotiationFailed = 3,
		AlreadyCapturing = 4,
		NotCapturing = 5,
		CaptureFailed = 6,
		CaptureNotObserved = 7,
		BufferTooSmall = 8,
		UnsupportedPlatform = 9,
		InvalidArgument = 10,
		InternalError = 11,
		AnnotationsUnavailable = 12,
		CaptureInactive = 13,
		BackendUnsupported = 14,
		PacketPoolExhausted = 15,
		AnnotationRejected = 16
	}

	[Flags]
	internal enum SggRdFeatureBitsV1 : uint
	{
		None = 0,
		Discard = 1u << 0,
		Comments = 1u << 1,
		Title = 1u << 2,
		Annotations = 1u << 3
	}

	internal static class PerfMeterRenderDocAbiV1
	{
		internal const uint AbiMajor = 1u;
		internal const uint AbiMinor = 0u;
		internal const int MaxTitleBytes = 256;
		internal const int MaxCommentsBytes = 1024;
		internal const int MaxPathBytes = 32768;

		internal const int CapabilitiesSize = 72;
		internal const int CaptureTokenSize = 32;
		internal const int ArtifactSize = 32;
		internal const uint CapabilitiesSizeAsUInt = 72u;
		internal const uint CaptureTokenSizeAsUInt = 32u;
		internal const uint ArtifactSizeAsUInt = 32u;

		internal static int MaxPathInputBytes => MaxPathBytes - 1;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct SggRdCapabilitiesV1
	{
		internal uint StructSize;
		internal uint BridgeAbiMajor;
		internal uint BridgeAbiMinor;
		internal uint PlatformSupported;
		internal uint ModuleLoaded;
		internal uint ExportAvailable;
		internal uint ApiNegotiated;
		internal uint TargetControlConnected;
		internal uint IsCapturing;
		internal uint ApiMajor;
		internal uint ApiMinor;
		internal uint ApiPatch;
		internal uint FeatureFlags;
		internal uint SupportsDiscard;
		internal uint SupportsComments;
		internal uint SupportsTitle;
		internal uint SupportsAnnotations;
		internal uint CaptureCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct SggRdCaptureTokenV1
	{
		internal uint StructSize;
		internal uint Reserved0;
		internal ulong RequestNonce;
		internal uint CountBefore;
		internal uint Reserved1;
		internal ulong StartUnixNanoseconds;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct SggRdArtifactV1
	{
		internal uint StructSize;
		internal uint Index;
		internal ulong RenderDocTimestampSeconds;
		internal ulong ObservedUnixNanoseconds;
		internal uint RequiredPathBytes;
		internal uint Reserved0;
	}

	internal interface IPerfMeterRenderDocBridge
	{
		SggRdResult GetCapabilities(out SggRdCapabilitiesV1 capabilities);
		SggRdResult BeginCapture(ulong requestNonce, string capturePathTemplate, string title, out SggRdCaptureTokenV1 token);
		SggRdResult EndCapture(SggRdCaptureTokenV1 token);
		SggRdResult DiscardCapture(SggRdCaptureTokenV1 token);
		SggRdResult TryGetNewArtifact(SggRdCaptureTokenV1 token, out SggRdArtifactV1 artifact, out string observedPath);
		SggRdResult SetCaptureComments(SggRdCaptureTokenV1 token, string observedPath, string comments);
	}

	internal static class PerfMeterRenderDocUtf8
	{
		private static readonly System.Text.UTF8Encoding StrictEncoding = new System.Text.UTF8Encoding(false, true);

		internal static bool TryEncode(string value, int maximumBytes, bool allowNull, out byte[] bytes)
		{
			bytes = Array.Empty<byte>();
			if (value == null)
			{
				return allowNull;
			}

			try
			{
				bytes = StrictEncoding.GetBytes(value);
			}
			catch (ArgumentException)
			{
				return false;
			}

			if (bytes.Length > maximumBytes)
			{
				bytes = Array.Empty<byte>();
				return false;
			}

			for (int index = 0; index < bytes.Length; index++)
			{
				if (bytes[index] == 0)
				{
					bytes = Array.Empty<byte>();
					return false;
				}
			}

			return true;
		}

		internal static bool TryDecodeOutput(byte[] bytes, uint requiredBytes, out string value)
		{
			value = string.Empty;
			if (!TryValidateArtifactPathBytes(requiredBytes, out int requiredByteCount) ||
				bytes == null ||
				requiredByteCount > bytes.Length ||
				bytes[requiredByteCount - 1] != 0u)
			{
				return false;
			}

			int payloadBytes = requiredByteCount - 1;
			for (int index = 0; index < payloadBytes; index++)
			{
				if (bytes[index] == 0u)
				{
					return false;
				}
			}

			try
			{
				value = StrictEncoding.GetString(bytes, 0, payloadBytes);
				return true;
			}
			catch (ArgumentException)
			{
				value = string.Empty;
				return false;
			}
		}

		internal static int GetByteCount(string value)
		{
			if (value == null)
			{
				return 0;
			}

			try
			{
				return StrictEncoding.GetByteCount(value);
			}
			catch (ArgumentException)
			{
				return -1;
			}
		}

		internal static bool TryValidateArtifactPathBytes(uint requiredBytes, out int byteCount)
		{
			byteCount = 0;
			if (requiredBytes == 0u || requiredBytes > (uint)PerfMeterRenderDocAbiV1.MaxPathBytes)
			{
				return false;
			}

			byteCount = checked((int)requiredBytes);
			return true;
		}
	}

	internal sealed class PerfMeterRenderDocPInvokeBridge : IPerfMeterRenderDocBridge
	{
		public unsafe SggRdResult GetCapabilities(out SggRdCapabilitiesV1 capabilities)
		{
			capabilities = new SggRdCapabilitiesV1
			{
				StructSize = PerfMeterRenderDocAbiV1.CapabilitiesSizeAsUInt
			};

			try
			{
				SggRdCapabilitiesV1 value = capabilities;
				SggRdResult result = NativeMethods.SggRd_GetCapabilitiesV1(&value);
				capabilities = value;
				return result;
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		public unsafe SggRdResult BeginCapture(
			ulong requestNonce,
			string capturePathTemplate,
			string title,
			out SggRdCaptureTokenV1 token)
		{
			token = default;
			if (requestNonce == 0u ||
				!PerfMeterRenderDocUtf8.TryEncode(
					capturePathTemplate,
					PerfMeterRenderDocAbiV1.MaxPathInputBytes,
					false,
					out byte[] pathBytes) ||
				pathBytes.Length == 0 ||
				!PerfMeterRenderDocUtf8.TryEncode(
					title,
					PerfMeterRenderDocAbiV1.MaxTitleBytes,
					true,
					out byte[] titleBytes))
			{
				return SggRdResult.InvalidArgument;
			}

			try
			{
				SggRdCaptureTokenV1 value = new SggRdCaptureTokenV1
				{
					StructSize = PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt
				};
				SggRdResult result;
				fixed (byte* pathPointer = pathBytes)
				{
					if (titleBytes.Length == 0)
					{
						result = NativeMethods.SggRd_BeginCaptureV1(
							requestNonce,
							pathPointer,
							checked((uint)pathBytes.Length),
							null,
							0u,
							&value);
					}
					else
					{
						fixed (byte* titlePointer = titleBytes)
						{
							result = NativeMethods.SggRd_BeginCaptureV1(
								requestNonce,
								pathPointer,
								checked((uint)pathBytes.Length),
								titlePointer,
								checked((uint)titleBytes.Length),
								&value);
						}
					}
				}

				token = value;
				return result;
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		public unsafe SggRdResult EndCapture(SggRdCaptureTokenV1 token)
		{
			try
			{
				return NativeMethods.SggRd_EndCaptureV1(&token);
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		public unsafe SggRdResult DiscardCapture(SggRdCaptureTokenV1 token)
		{
			try
			{
				return NativeMethods.SggRd_DiscardCaptureV1(&token);
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		public unsafe SggRdResult TryGetNewArtifact(
			SggRdCaptureTokenV1 token,
			out SggRdArtifactV1 artifact,
			out string observedPath)
		{
			artifact = new SggRdArtifactV1
			{
				StructSize = PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt
			};
			observedPath = string.Empty;
			try
			{
				SggRdArtifactV1 firstValue = artifact;
				SggRdResult firstResult = NativeMethods.SggRd_TryGetNewArtifactV1(
					&token,
					&firstValue,
					null,
					0u);
				artifact = firstValue;

				if (firstResult != SggRdResult.BufferTooSmall)
				{
					return firstResult == SggRdResult.Ok ? SggRdResult.CaptureFailed : firstResult;
				}

				if (!PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(
					firstValue.RequiredPathBytes,
					out int requiredPathBytes))
				{
					return SggRdResult.CaptureFailed;
				}

				byte[] pathBuffer = new byte[requiredPathBytes];
				SggRdArtifactV1 secondValue = firstValue;
				SggRdResult secondResult;
				fixed (byte* pathPointer = pathBuffer)
				{
					secondResult = NativeMethods.SggRd_TryGetNewArtifactV1(
						&token,
						&secondValue,
						pathPointer,
						checked((uint)pathBuffer.Length));
				}

				artifact = secondValue;
				if (secondResult != SggRdResult.Ok)
				{
					if (secondResult == SggRdResult.BufferTooSmall &&
						!PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(secondValue.RequiredPathBytes, out _))
					{
						return SggRdResult.CaptureFailed;
					}

					return secondResult;
				}

				if (secondValue.RequiredPathBytes != firstValue.RequiredPathBytes ||
					!PerfMeterRenderDocUtf8.TryDecodeOutput(pathBuffer, secondValue.RequiredPathBytes, out observedPath))
				{
					return SggRdResult.CaptureFailed;
				}

				return SggRdResult.Ok;
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		public unsafe SggRdResult SetCaptureComments(
			SggRdCaptureTokenV1 token,
			string observedPath,
			string comments)
		{
			if (!PerfMeterRenderDocUtf8.TryEncode(
					observedPath,
					PerfMeterRenderDocAbiV1.MaxPathInputBytes,
					false,
					out byte[] pathBytes) ||
				pathBytes.Length == 0 ||
				!PerfMeterRenderDocUtf8.TryEncode(
					comments,
					PerfMeterRenderDocAbiV1.MaxCommentsBytes,
					true,
					out byte[] commentsBytes))
			{
				return SggRdResult.InvalidArgument;
			}

			try
			{
				fixed (byte* pathPointer = pathBytes)
				{
					if (commentsBytes.Length == 0)
					{
						return NativeMethods.SggRd_SetCaptureCommentsV1(
							&token,
							pathPointer,
							checked((uint)pathBytes.Length),
							null,
							0u);
					}

					fixed (byte* commentsPointer = commentsBytes)
					{
						return NativeMethods.SggRd_SetCaptureCommentsV1(
							&token,
							pathPointer,
							checked((uint)pathBytes.Length),
							commentsPointer,
							checked((uint)commentsBytes.Length));
					}
				}
			}
			catch (Exception exception)
			{
				return MapInteropException(exception);
			}
		}

		internal static SggRdResult MapInteropException(Exception exception)
		{
			if (exception is DllNotFoundException)
			{
				return SggRdResult.UnsupportedPlatform;
			}

			if (exception is EntryPointNotFoundException)
			{
				return SggRdResult.ExportMissing;
			}

			if (exception is BadImageFormatException)
			{
				return SggRdResult.UnsupportedPlatform;
			}

			return SggRdResult.InternalError;
		}

		private static class NativeMethods
		{
			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_GetCapabilitiesV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_GetCapabilitiesV1(SggRdCapabilitiesV1* outCapabilities);

			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_BeginCaptureV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_BeginCaptureV1(
				ulong requestNonce,
				byte* capturePathTemplate,
				uint capturePathTemplateBytes,
				byte* title,
				uint titleBytes,
				SggRdCaptureTokenV1* outToken);

			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_EndCaptureV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_EndCaptureV1(SggRdCaptureTokenV1* token);

			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_DiscardCaptureV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_DiscardCaptureV1(SggRdCaptureTokenV1* token);

			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_TryGetNewArtifactV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_TryGetNewArtifactV1(
				SggRdCaptureTokenV1* token,
				SggRdArtifactV1* outArtifact,
				byte* pathBuffer,
				uint pathBufferBytes);

			[DllImport(
				"sgg_renderdoc_bridge",
				EntryPoint = "SggRd_SetCaptureCommentsV1",
				ExactSpelling = true,
				CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_SetCaptureCommentsV1(
				SggRdCaptureTokenV1* token,
				byte* observedPath,
				uint observedPathBytes,
				byte* comments,
				uint commentsBytes);
		}
	}
}
