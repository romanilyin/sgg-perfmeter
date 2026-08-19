using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SGG.PerfMeter
{
	internal static class PerfMeterRenderDocAnnotationAbiV1
	{
		internal const uint AbiMajor = 1u;
		internal const uint AbiMinor = 0u;
		internal const int CapabilitiesSize = 88;
		internal const int EntrySize = 440;
		internal const int MaxEntries = 32;
		internal const int MaxKeyBytes = 127;
		internal const int KeyCapacity = 128;
		internal const int MaxStringBytes = 255;
		internal const int StringCapacity = 256;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct SggRdAnnotationCapabilitiesV1
	{
		internal uint StructSize;
		internal uint AnnotationAbiMajor;
		internal uint AnnotationAbiMinor;
		internal uint UnityPluginLoaded;
		internal uint GraphicsRenderer;
		internal uint BackendSupported;
		internal uint ModuleLoaded;
		internal uint ApiNegotiated;
		internal uint SupportsAnnotations;
		internal uint RenderDocApiMajor;
		internal uint RenderDocApiMinor;
		internal uint RenderDocApiPatch;
		internal uint IsCapturing;
		internal uint EventIdValid;
		internal uint EventId;
		internal uint PacketCapacity;
		internal uint PacketsInUse;
		internal uint PacketsCreated;
		internal uint PacketsExecuted;
		internal uint PacketsDropped;
		internal uint AnnotationCalls;
		internal uint AnnotationErrors;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 8, Size = PerfMeterRenderDocAnnotationAbiV1.EntrySize)]
	internal unsafe struct SggRdAnnotationEntryV1
	{
		internal uint StructSize;
		internal uint ValueType;
		internal uint VectorWidth;
		internal uint KeyBytes;
		internal uint StringBytes;
		internal uint Reserved0;
		internal fixed ulong ValueData[4];
		internal fixed byte Key[PerfMeterRenderDocAnnotationAbiV1.KeyCapacity];
		internal fixed byte StringValue[PerfMeterRenderDocAnnotationAbiV1.StringCapacity];
	}

	internal sealed class PerfMeterRenderDocGpuAnnotationProvider : IPerfMeterGpuAnnotationProvider
	{
		private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
		[ThreadStatic] private static SggRdAnnotationEntryV1[] _nativeEntries;
		private IntPtr _callback;
		private int _eventId = -1;

		public PerfMeterGpuAnnotationCapabilities GetCapabilities()
		{
			SggRdAnnotationCapabilitiesV1 native = new SggRdAnnotationCapabilitiesV1
			{
				StructSize = PerfMeterRenderDocAnnotationAbiV1.CapabilitiesSize
			};
			SggRdResult result;
			try
			{
				unsafe
				{
					result = NativeMethods.SggRd_GetAnnotationCapabilitiesV1(&native);
				}
			}
			catch (DllNotFoundException)
			{
				return Unavailable(PerfMeterGpuAnnotationAvailability.BridgeUnavailable);
			}
			catch (EntryPointNotFoundException)
			{
				return Unavailable(PerfMeterGpuAnnotationAvailability.BridgeTooOld);
			}
			catch (BadImageFormatException)
			{
				return Unavailable(PerfMeterGpuAnnotationAvailability.BridgeUnavailable);
			}
			catch
			{
				return Unavailable(PerfMeterGpuAnnotationAvailability.InternalError);
			}

			PerfMeterGpuAnnotationAvailability availability = MapAvailability(result, native);
			return new PerfMeterGpuAnnotationCapabilities(
				availability,
				native.AnnotationAbiMajor,
				native.AnnotationAbiMinor,
				native.RenderDocApiMajor,
				native.RenderDocApiMinor,
				native.RenderDocApiPatch,
				unchecked((int)native.GraphicsRenderer),
				unchecked((int)native.PacketCapacity),
				unchecked((int)native.PacketsInUse),
				native.PacketsCreated,
				native.PacketsExecuted,
				native.PacketsDropped,
				native.AnnotationCalls,
				native.AnnotationErrors);
		}

		public bool TryCreateEvent(
			PerfMeterGpuAnnotationEntry[] entries,
			int count,
			out PerfMeterGpuAnnotationPreparedEvent preparedEvent)
		{
			preparedEvent = default;
			if (entries == null || count < 1 || count > PerfMeterRenderDocAnnotationAbiV1.MaxEntries || count > entries.Length)
			{
				return false;
			}

			if (!TryGetEvent(out IntPtr callback, out int eventId))
			{
				return false;
			}

			SggRdAnnotationEntryV1[] nativeEntries = _nativeEntries;
			if (nativeEntries == null || nativeEntries.Length != PerfMeterRenderDocAnnotationAbiV1.MaxEntries)
			{
				nativeEntries = new SggRdAnnotationEntryV1[PerfMeterRenderDocAnnotationAbiV1.MaxEntries];
				_nativeEntries = nativeEntries;
			}
			Array.Clear(nativeEntries, 0, count);
			for (int index = 0; index < count; index++)
			{
				if (!TryEncode(entries[index], ref nativeEntries[index]))
				{
					return false;
				}
			}

			IntPtr packet = IntPtr.Zero;
			try
			{
				unsafe
				{
					fixed (SggRdAnnotationEntryV1* entriesPointer = nativeEntries)
					{
						SggRdResult result = NativeMethods.SggRd_CreateAnnotationPacketV1(
							entriesPointer,
							checked((uint)count),
							&packet);
						if (result != SggRdResult.Ok || packet == IntPtr.Zero)
						{
							return false;
						}
					}
				}
			}
			catch
			{
				return false;
			}

			preparedEvent = new PerfMeterGpuAnnotationPreparedEvent
			{
				Provider = this,
				Callback = callback,
				EventId = eventId,
				EventData = packet
			};
			return true;
		}

		public void ReleaseEvent(IntPtr eventData)
		{
			if (eventData == IntPtr.Zero)
			{
				return;
			}
			try
			{
				NativeMethods.SggRd_ReleaseAnnotationPacketV1(eventData);
			}
			catch
			{
				// Release is a best-effort safety path during failed command recording or domain teardown.
			}
		}

		internal static PerfMeterGpuAnnotationAvailability MapAvailability(
			SggRdResult result,
			SggRdAnnotationCapabilitiesV1 capabilities)
		{
			switch (result)
			{
				case SggRdResult.Ok:
					return capabilities.AnnotationAbiMajor == PerfMeterRenderDocAnnotationAbiV1.AbiMajor &&
						capabilities.SupportsAnnotations != 0u && capabilities.IsCapturing != 0u &&
						capabilities.BackendSupported != 0u && capabilities.EventIdValid != 0u
							? PerfMeterGpuAnnotationAvailability.Ready
							: PerfMeterGpuAnnotationAvailability.InternalError;
				case SggRdResult.NotLoaded:
					return PerfMeterGpuAnnotationAvailability.RenderDocNotLoaded;
				case SggRdResult.ExportMissing:
				case SggRdResult.ApiNegotiationFailed:
				case SggRdResult.AnnotationsUnavailable:
					return PerfMeterGpuAnnotationAvailability.ApiUnsupported;
				case SggRdResult.NotCapturing:
				case SggRdResult.CaptureInactive:
					return PerfMeterGpuAnnotationAvailability.CaptureInactive;
				case SggRdResult.UnsupportedPlatform:
				case SggRdResult.BackendUnsupported:
					return PerfMeterGpuAnnotationAvailability.BackendUnsupported;
				case SggRdResult.PacketPoolExhausted:
					return PerfMeterGpuAnnotationAvailability.PacketBudgetExceeded;
				case SggRdResult.InvalidArgument:
					return PerfMeterGpuAnnotationAvailability.InvalidData;
				default:
					return PerfMeterGpuAnnotationAvailability.InternalError;
			}
		}

		private static PerfMeterGpuAnnotationCapabilities Unavailable(PerfMeterGpuAnnotationAvailability availability)
		{
			return new PerfMeterGpuAnnotationCapabilities(availability);
		}

		private bool TryGetEvent(out IntPtr callback, out int eventId)
		{
			callback = _callback;
			eventId = _eventId;
			if (callback != IntPtr.Zero && eventId >= 0)
			{
				return true;
			}

			try
			{
				SggRdResult result = NativeMethods.SggRd_GetAnnotationEventV1(out callback, out eventId);
				if (result != SggRdResult.Ok || callback == IntPtr.Zero || eventId < 0)
				{
					callback = IntPtr.Zero;
					eventId = -1;
					return false;
				}
				_callback = callback;
				_eventId = eventId;
				return true;
			}
			catch
			{
				callback = IntPtr.Zero;
				eventId = -1;
				return false;
			}
		}

		private static unsafe bool TryEncode(PerfMeterGpuAnnotationEntry entry, ref SggRdAnnotationEntryV1 destination)
		{
			if (!PerfMeterGpuAnnotationBatch.IsValidKey(entry.Key) || !PerfMeterGpuAnnotationBatch.IsValidValue(entry.Value))
			{
				return false;
			}
			destination.StructSize = PerfMeterRenderDocAnnotationAbiV1.EntrySize;
			destination.ValueType = (uint)entry.Value.Type;
			destination.VectorWidth = checked((uint)entry.Value.VectorWidth);
			destination.Reserved0 = 0u;
			destination.ValueData[0] = entry.Value.Raw0;
			destination.ValueData[1] = entry.Value.Raw1;
			destination.ValueData[2] = entry.Value.Raw2;
			destination.ValueData[3] = entry.Value.Raw3;

			fixed (byte* keyPointer = destination.Key)
			{
				if (!TryEncodeUtf8(entry.Key, keyPointer, PerfMeterRenderDocAnnotationAbiV1.KeyCapacity, out uint keyBytes))
				{
					return false;
				}
				destination.KeyBytes = keyBytes;
			}

			if (entry.Value.Type == PerfMeterGpuAnnotationValueType.String)
			{
				fixed (byte* stringPointer = destination.StringValue)
				{
					if (!TryEncodeUtf8(entry.Value.StringValue, stringPointer, PerfMeterRenderDocAnnotationAbiV1.StringCapacity, out uint stringBytes))
					{
						return false;
					}
					destination.StringBytes = stringBytes;
				}
			}
			return true;
		}

		private static unsafe bool TryEncodeUtf8(string value, byte* destination, int capacity, out uint byteCount)
		{
			byteCount = 0u;
			if (value == null || destination == null || capacity < 1)
			{
				return false;
			}
			try
			{
				int requiredBytes = StrictUtf8.GetByteCount(value);
				if (requiredBytes >= capacity)
				{
					return false;
				}
				fixed (char* source = value)
				{
					int written = StrictUtf8.GetBytes(source, value.Length, destination, capacity - 1);
					if (written != requiredBytes)
					{
						return false;
					}
				}
				destination[requiredBytes] = 0;
				byteCount = checked((uint)requiredBytes);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private static class NativeMethods
		{
			[DllImport("sgg_renderdoc_bridge", EntryPoint = "SggRd_GetAnnotationCapabilitiesV1", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_GetAnnotationCapabilitiesV1(SggRdAnnotationCapabilitiesV1* outCapabilities);

			[DllImport("sgg_renderdoc_bridge", EntryPoint = "SggRd_GetAnnotationEventV1", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern SggRdResult SggRd_GetAnnotationEventV1(out IntPtr outCallback, out int outEventId);

			[DllImport("sgg_renderdoc_bridge", EntryPoint = "SggRd_CreateAnnotationPacketV1", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern unsafe SggRdResult SggRd_CreateAnnotationPacketV1(SggRdAnnotationEntryV1* entries, uint entryCount, IntPtr* outPacket);

			[DllImport("sgg_renderdoc_bridge", EntryPoint = "SggRd_ReleaseAnnotationPacketV1", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
			internal static extern SggRdResult SggRd_ReleaseAnnotationPacketV1(IntPtr packet);
		}
	}

	internal static class PerfMeterRenderDocGpuAnnotationBootstrap
	{
		private static PerfMeterRenderDocGpuAnnotationProvider _provider;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			if (_provider != null)
			{
				PerfMeterGpuAnnotationProviderRegistry.Unregister(_provider);
			}
			_provider = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RegisterRuntime()
		{
			Register();
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void RegisterEditor()
		{
			Register();
		}
#endif

		internal static void Register()
		{
			if (_provider == null)
			{
				_provider = new PerfMeterRenderDocGpuAnnotationProvider();
			}
			PerfMeterGpuAnnotationProviderRegistry.Register(_provider);
		}
	}
}
