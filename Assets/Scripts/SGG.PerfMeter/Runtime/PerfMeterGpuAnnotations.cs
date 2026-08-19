using System;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	/// <summary>Value types accepted by the PerfMeter GPU annotation contract.</summary>
	public enum PerfMeterGpuAnnotationValueType
	{
		Empty = 0,
		Bool = 1,
		Int32 = 2,
		UInt32 = 3,
		Int64 = 4,
		UInt64 = 5,
		Float = 6,
		Double = 7,
		String = 8
	}

	/// <summary>Explains why the annotation transport is or is not ready.</summary>
	public enum PerfMeterGpuAnnotationAvailability
	{
		Ready = 0,
		ProviderUnavailable = 1,
		BridgeUnavailable = 2,
		BridgeTooOld = 3,
		RenderDocNotLoaded = 4,
		ApiUnsupported = 5,
		CaptureInactive = 6,
		BackendUnsupported = 7,
		PacketBudgetExceeded = 8,
		InvalidData = 9,
		InternalError = 10
	}

	/// <summary>Stable schema keys supplied by PerfMeter annotation schema v1.</summary>
	public static class PerfMeterGpuAnnotationKeys
	{
		public const uint SchemaVersion = 1u;
		public const string SchemaVersionKey = "SGG.Annotation.SchemaVersion";
		public const string Module = "SGG.Module";
		public const string RenderGraphPass = "SGG.RenderGraph.Pass";
		public const string CameraStableId = "SGG.Camera.StableId";
		public const string AssetMaterial = "SGG.Asset.Material";
		public const string StableObjectId = "SGG.StableObjectId";
	}

	/// <summary>Read-only snapshot of the active annotation transport.</summary>
	public readonly struct PerfMeterGpuAnnotationCapabilities
	{
		public PerfMeterGpuAnnotationAvailability Availability { get; }
		public uint AnnotationAbiMajor { get; }
		public uint AnnotationAbiMinor { get; }
		public uint RenderDocApiMajor { get; }
		public uint RenderDocApiMinor { get; }
		public uint RenderDocApiPatch { get; }
		public int GraphicsRenderer { get; }
		public int PacketCapacity { get; }
		public int PacketsInUse { get; }
		public uint PacketsCreated { get; }
		public uint PacketsExecuted { get; }
		public uint PacketsDropped { get; }
		public uint AnnotationCalls { get; }
		public uint AnnotationErrors { get; }
		public bool IsReady => Availability == PerfMeterGpuAnnotationAvailability.Ready;

		internal PerfMeterGpuAnnotationCapabilities(
			PerfMeterGpuAnnotationAvailability availability,
			uint annotationAbiMajor = 0u,
			uint annotationAbiMinor = 0u,
			uint renderDocApiMajor = 0u,
			uint renderDocApiMinor = 0u,
			uint renderDocApiPatch = 0u,
			int graphicsRenderer = 0,
			int packetCapacity = 0,
			int packetsInUse = 0,
			uint packetsCreated = 0u,
			uint packetsExecuted = 0u,
			uint packetsDropped = 0u,
			uint annotationCalls = 0u,
			uint annotationErrors = 0u)
		{
			Availability = availability;
			AnnotationAbiMajor = annotationAbiMajor;
			AnnotationAbiMinor = annotationAbiMinor;
			RenderDocApiMajor = renderDocApiMajor;
			RenderDocApiMinor = renderDocApiMinor;
			RenderDocApiPatch = renderDocApiPatch;
			GraphicsRenderer = graphicsRenderer;
			PacketCapacity = packetCapacity;
			PacketsInUse = packetsInUse;
			PacketsCreated = packetsCreated;
			PacketsExecuted = packetsExecuted;
			PacketsDropped = packetsDropped;
			AnnotationCalls = annotationCalls;
			AnnotationErrors = annotationErrors;
		}
	}

	/// <summary>A scalar or 2-4 component value stored in a GPU annotation.</summary>
	public readonly struct PerfMeterGpuAnnotationValue
	{
		public PerfMeterGpuAnnotationValueType Type { get; }
		public int VectorWidth { get; }
		public string StringValue { get; }
		internal ulong Raw0 { get; }
		internal ulong Raw1 { get; }
		internal ulong Raw2 { get; }
		internal ulong Raw3 { get; }

		private PerfMeterGpuAnnotationValue(
			PerfMeterGpuAnnotationValueType type,
			int vectorWidth,
			ulong raw0,
			ulong raw1,
			ulong raw2,
			ulong raw3,
			string stringValue)
		{
			Type = type;
			VectorWidth = vectorWidth;
			Raw0 = raw0;
			Raw1 = raw1;
			Raw2 = raw2;
			Raw3 = raw3;
			StringValue = stringValue;
		}

		public static PerfMeterGpuAnnotationValue Empty()
		{
			return new PerfMeterGpuAnnotationValue(PerfMeterGpuAnnotationValueType.Empty, 0, 0u, 0u, 0u, 0u, null);
		}

		public static PerfMeterGpuAnnotationValue Boolean(bool x) => Boolean(x, false, false, false, 1);
		public static PerfMeterGpuAnnotationValue Boolean(bool x, bool y) => Boolean(x, y, false, false, 2);
		public static PerfMeterGpuAnnotationValue Boolean(bool x, bool y, bool z) => Boolean(x, y, z, false, 3);
		public static PerfMeterGpuAnnotationValue Boolean(bool x, bool y, bool z, bool w) => Boolean(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue Int32(int x) => Int32(x, 0, 0, 0, 1);
		public static PerfMeterGpuAnnotationValue Int32(int x, int y) => Int32(x, y, 0, 0, 2);
		public static PerfMeterGpuAnnotationValue Int32(int x, int y, int z) => Int32(x, y, z, 0, 3);
		public static PerfMeterGpuAnnotationValue Int32(int x, int y, int z, int w) => Int32(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue UInt32(uint x) => UInt32(x, 0u, 0u, 0u, 1);
		public static PerfMeterGpuAnnotationValue UInt32(uint x, uint y) => UInt32(x, y, 0u, 0u, 2);
		public static PerfMeterGpuAnnotationValue UInt32(uint x, uint y, uint z) => UInt32(x, y, z, 0u, 3);
		public static PerfMeterGpuAnnotationValue UInt32(uint x, uint y, uint z, uint w) => UInt32(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue Int64(long x) => Int64(x, 0L, 0L, 0L, 1);
		public static PerfMeterGpuAnnotationValue Int64(long x, long y) => Int64(x, y, 0L, 0L, 2);
		public static PerfMeterGpuAnnotationValue Int64(long x, long y, long z) => Int64(x, y, z, 0L, 3);
		public static PerfMeterGpuAnnotationValue Int64(long x, long y, long z, long w) => Int64(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue UInt64(ulong x) => UInt64(x, 0uL, 0uL, 0uL, 1);
		public static PerfMeterGpuAnnotationValue UInt64(ulong x, ulong y) => UInt64(x, y, 0uL, 0uL, 2);
		public static PerfMeterGpuAnnotationValue UInt64(ulong x, ulong y, ulong z) => UInt64(x, y, z, 0uL, 3);
		public static PerfMeterGpuAnnotationValue UInt64(ulong x, ulong y, ulong z, ulong w) => UInt64(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue Float(float x) => Float(x, 0f, 0f, 0f, 1);
		public static PerfMeterGpuAnnotationValue Float(float x, float y) => Float(x, y, 0f, 0f, 2);
		public static PerfMeterGpuAnnotationValue Float(float x, float y, float z) => Float(x, y, z, 0f, 3);
		public static PerfMeterGpuAnnotationValue Float(float x, float y, float z, float w) => Float(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue Double(double x) => Double(x, 0d, 0d, 0d, 1);
		public static PerfMeterGpuAnnotationValue Double(double x, double y) => Double(x, y, 0d, 0d, 2);
		public static PerfMeterGpuAnnotationValue Double(double x, double y, double z) => Double(x, y, z, 0d, 3);
		public static PerfMeterGpuAnnotationValue Double(double x, double y, double z, double w) => Double(x, y, z, w, 4);

		public static PerfMeterGpuAnnotationValue String(string value)
		{
			return new PerfMeterGpuAnnotationValue(PerfMeterGpuAnnotationValueType.String, 1, 0u, 0u, 0u, 0u, value);
		}

		private static PerfMeterGpuAnnotationValue Boolean(bool x, bool y, bool z, bool w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.Bool, width, x ? 1uL : 0uL, y ? 1uL : 0uL, z ? 1uL : 0uL, w ? 1uL : 0uL);
		}

		private static PerfMeterGpuAnnotationValue Int32(int x, int y, int z, int w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.Int32, width, unchecked((uint)x), unchecked((uint)y), unchecked((uint)z), unchecked((uint)w));
		}

		private static PerfMeterGpuAnnotationValue UInt32(uint x, uint y, uint z, uint w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.UInt32, width, x, y, z, w);
		}

		private static PerfMeterGpuAnnotationValue Int64(long x, long y, long z, long w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.Int64, width, unchecked((ulong)x), unchecked((ulong)y), unchecked((ulong)z), unchecked((ulong)w));
		}

		private static PerfMeterGpuAnnotationValue UInt64(ulong x, ulong y, ulong z, ulong w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.UInt64, width, x, y, z, w);
		}

		private static PerfMeterGpuAnnotationValue Float(float x, float y, float z, float w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.Float, width, BitConverter32.ToUInt32(x), BitConverter32.ToUInt32(y), BitConverter32.ToUInt32(z), BitConverter32.ToUInt32(w));
		}

		private static PerfMeterGpuAnnotationValue Double(double x, double y, double z, double w, int width)
		{
			return Numeric(PerfMeterGpuAnnotationValueType.Double, width, BitConverter64.ToUInt64(x), BitConverter64.ToUInt64(y), BitConverter64.ToUInt64(z), BitConverter64.ToUInt64(w));
		}

		private static PerfMeterGpuAnnotationValue Numeric(PerfMeterGpuAnnotationValueType type, int width, ulong x, ulong y, ulong z, ulong w)
		{
			return new PerfMeterGpuAnnotationValue(type, width, x, y, z, w, null);
		}

		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
		private struct BitConverter32
		{
			[System.Runtime.InteropServices.FieldOffset(0)] private float _float;
			[System.Runtime.InteropServices.FieldOffset(0)] private uint _uint;

			internal static uint ToUInt32(float value)
			{
				return new BitConverter32 { _float = value }._uint;
			}
		}

		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
		private struct BitConverter64
		{
			[System.Runtime.InteropServices.FieldOffset(0)] private double _double;
			[System.Runtime.InteropServices.FieldOffset(0)] private ulong _ulong;

			internal static ulong ToUInt64(double value)
			{
				return new BitConverter64 { _double = value }._ulong;
			}
		}
	}

	internal readonly struct PerfMeterGpuAnnotationEntry
	{
		internal string Key { get; }
		internal PerfMeterGpuAnnotationValue Value { get; }

		internal PerfMeterGpuAnnotationEntry(string key, PerfMeterGpuAnnotationValue value)
		{
			Key = key;
			Value = value;
		}
	}

	/// <summary>A reusable bounded collection of typed annotation values.</summary>
	public sealed class PerfMeterGpuAnnotationBatch
	{
		public const int MaximumEntries = 32;
		public const int MaximumKeyBytes = 127;
		public const int MaximumStringBytes = 255;
		private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
		private readonly PerfMeterGpuAnnotationEntry[] _entries;

		public int Count { get; private set; }
		public int Capacity => _entries.Length;

		public PerfMeterGpuAnnotationBatch(int capacity = 16)
		{
			if (capacity < 1 || capacity > MaximumEntries)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Capacity must be between 1 and {MaximumEntries}.");
			}
			_entries = new PerfMeterGpuAnnotationEntry[capacity];
		}

		public void Reset()
		{
			Array.Clear(_entries, 0, Count);
			Count = 0;
		}

		public bool TryAdd(string key, PerfMeterGpuAnnotationValue value)
		{
			if (!IsValidKey(key) || !IsValidValue(value))
			{
				return false;
			}

			for (int index = 0; index < Count; index++)
			{
				if (string.Equals(_entries[index].Key, key, StringComparison.Ordinal))
				{
					_entries[index] = new PerfMeterGpuAnnotationEntry(key, value);
					return true;
				}
			}

			if (Count >= _entries.Length)
			{
				return false;
			}
			_entries[Count++] = new PerfMeterGpuAnnotationEntry(key, value);
			return true;
		}

		public bool TryAdd(string key, bool value) => TryAdd(key, PerfMeterGpuAnnotationValue.Boolean(value));
		public bool TryAdd(string key, int value) => TryAdd(key, PerfMeterGpuAnnotationValue.Int32(value));
		public bool TryAdd(string key, uint value) => TryAdd(key, PerfMeterGpuAnnotationValue.UInt32(value));
		public bool TryAdd(string key, long value) => TryAdd(key, PerfMeterGpuAnnotationValue.Int64(value));
		public bool TryAdd(string key, ulong value) => TryAdd(key, PerfMeterGpuAnnotationValue.UInt64(value));
		public bool TryAdd(string key, float value) => TryAdd(key, PerfMeterGpuAnnotationValue.Float(value));
		public bool TryAdd(string key, double value) => TryAdd(key, PerfMeterGpuAnnotationValue.Double(value));
		public bool TryAdd(string key, string value) => TryAdd(key, PerfMeterGpuAnnotationValue.String(value));

		internal PerfMeterGpuAnnotationEntry GetEntry(int index)
		{
			return _entries[index];
		}

		internal static bool IsValidKey(string key)
		{
			if (string.IsNullOrEmpty(key) || key[0] == '.' || key[key.Length - 1] == '.' || key.Contains(".."))
			{
				return false;
			}
			for (int index = 0; index < key.Length; index++)
			{
				char value = key[index];
				if (!((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') ||
					(value >= '0' && value <= '9') || value == '_' || value == '-' || value == '.'))
				{
					return false;
				}
			}
			return StrictUtf8.GetByteCount(key) <= MaximumKeyBytes;
		}

		internal static bool IsValidValue(PerfMeterGpuAnnotationValue value)
		{
			if (value.Type == PerfMeterGpuAnnotationValueType.Empty)
			{
				return value.VectorWidth == 0;
			}
			if (value.Type == PerfMeterGpuAnnotationValueType.String)
			{
				if (value.VectorWidth != 1 || value.StringValue == null || value.StringValue.IndexOf('\0') >= 0)
				{
					return false;
				}
				try
				{
					return StrictUtf8.GetByteCount(value.StringValue) <= MaximumStringBytes;
				}
				catch (ArgumentException)
				{
					return false;
				}
			}
			return value.Type >= PerfMeterGpuAnnotationValueType.Bool &&
				value.Type <= PerfMeterGpuAnnotationValueType.Double &&
				value.VectorWidth >= 1 && value.VectorWidth <= 4 && value.StringValue == null;
		}
	}

	internal interface IPerfMeterGpuAnnotationProvider
	{
		PerfMeterGpuAnnotationCapabilities GetCapabilities();
		bool TryCreateEvent(PerfMeterGpuAnnotationEntry[] entries, int count, out PerfMeterGpuAnnotationPreparedEvent preparedEvent);
		void ReleaseEvent(IntPtr eventData);
	}

	internal interface IPerfMeterGpuAnnotationCommandSink
	{
		void Issue(IntPtr callback, int eventId, IntPtr eventData);
	}

	internal struct PerfMeterGpuAnnotationPreparedEvent
	{
		internal IPerfMeterGpuAnnotationProvider Provider;
		internal IntPtr Callback;
		internal int EventId;
		internal IntPtr EventData;

		internal bool IsValid => Provider != null && Callback != IntPtr.Zero && EventData != IntPtr.Zero;

		internal void MarkEnqueued()
		{
			Provider = null;
			Callback = IntPtr.Zero;
			EventId = 0;
			EventData = IntPtr.Zero;
		}

		internal void Cancel()
		{
			IPerfMeterGpuAnnotationProvider provider = Provider;
			IntPtr data = EventData;
			MarkEnqueued();
			if (provider != null && data != IntPtr.Zero)
			{
				provider.ReleaseEvent(data);
			}
		}
	}

	/// <summary>
	/// Owns the end packet of one non-nested GPU annotation scope. Dispose it after recording the
	/// draw or dispatch commands that the annotation describes.
	/// </summary>
	public sealed class PerfMeterGpuAnnotationScope : IDisposable
	{
		private IPerfMeterGpuAnnotationCommandSink _sink;
		private PerfMeterGpuAnnotationPreparedEvent _endEvent;

		internal PerfMeterGpuAnnotationScope(IPerfMeterGpuAnnotationCommandSink sink, PerfMeterGpuAnnotationPreparedEvent endEvent)
		{
			_sink = sink;
			_endEvent = endEvent;
		}

		~PerfMeterGpuAnnotationScope()
		{
			_endEvent.Cancel();
		}

		public void Dispose()
		{
			IPerfMeterGpuAnnotationCommandSink sink = _sink;
			_sink = null;
			if (sink == null)
			{
				return;
			}

			try
			{
				sink.Issue(_endEvent.Callback, _endEvent.EventId, _endEvent.EventData);
				_endEvent.MarkEnqueued();
			}
			catch
			{
				_endEvent.Cancel();
				throw;
			}
			finally
			{
				GC.SuppressFinalize(this);
			}
		}
	}

	internal static class PerfMeterGpuAnnotationProviderRegistry
	{
		private static readonly object Gate = new object();
		private static IPerfMeterGpuAnnotationProvider _provider;

		internal static IPerfMeterGpuAnnotationProvider Get()
		{
			lock (Gate)
			{
				return _provider;
			}
		}

		internal static void Register(IPerfMeterGpuAnnotationProvider provider)
		{
			lock (Gate)
			{
				_provider = provider;
			}
		}

		internal static void Unregister(IPerfMeterGpuAnnotationProvider provider)
		{
			lock (Gate)
			{
				if (ReferenceEquals(_provider, provider))
				{
					_provider = null;
				}
			}
		}

		internal static void Reset()
		{
			lock (Gate)
			{
				_provider = null;
			}
		}
	}

	internal static class PerfMeterGpuAnnotationContextRegistry
	{
		private const int MaximumOwners = 8;
		private static readonly object Gate = new object();
		private static readonly ContextSlot[] Slots = new ContextSlot[MaximumOwners];

		internal static bool TryPublish(string ownerId, ulong generation, PerfMeterGpuAnnotationBatch batch)
		{
			if (!PerfMeterGpuAnnotationBatch.IsValidKey(ownerId) || generation == 0u || batch == null)
			{
				return false;
			}
			lock (Gate)
			{
				int target = -1;
				int free = -1;
				for (int slotIndex = 0; slotIndex < Slots.Length; slotIndex++)
				{
					ContextSlot slot = Slots[slotIndex];
					if (slot == null)
					{
						if (free < 0)
						{
							free = slotIndex;
						}
						continue;
					}
					if (string.Equals(slot.OwnerId, ownerId, StringComparison.Ordinal))
					{
						target = slotIndex;
						if (generation < slot.Generation)
						{
							return false;
						}
						continue;
					}
					for (int entryIndex = 0; entryIndex < batch.Count; entryIndex++)
					{
						string key = batch.GetEntry(entryIndex).Key;
						for (int existingIndex = 0; existingIndex < slot.Entries.Length; existingIndex++)
						{
							if (string.Equals(slot.Entries[existingIndex].Key, key, StringComparison.Ordinal))
							{
								return false;
							}
						}
					}
				}
				if (target < 0)
				{
					target = free;
				}
				if (target < 0)
				{
					return false;
				}
				PerfMeterGpuAnnotationEntry[] entries = new PerfMeterGpuAnnotationEntry[batch.Count];
				for (int index = 0; index < batch.Count; index++)
				{
					entries[index] = batch.GetEntry(index);
				}
				Slots[target] = new ContextSlot(ownerId, generation, entries);
				return true;
			}
		}

		internal static bool TryClear(string ownerId, ulong generation)
		{
			if (string.IsNullOrEmpty(ownerId) || generation == 0u)
			{
				return false;
			}
			lock (Gate)
			{
				for (int index = 0; index < Slots.Length; index++)
				{
					ContextSlot slot = Slots[index];
					if (slot != null && string.Equals(slot.OwnerId, ownerId, StringComparison.Ordinal) && slot.Generation == generation)
					{
						Slots[index] = null;
						return true;
					}
				}
			}
			return false;
		}

		internal static bool TryCopyTo(PerfMeterGpuAnnotationEntry[] destination, ref int count)
		{
			lock (Gate)
			{
				for (int slotIndex = 0; slotIndex < Slots.Length; slotIndex++)
				{
					ContextSlot slot = Slots[slotIndex];
					if (slot == null)
					{
						continue;
					}
					for (int entryIndex = 0; entryIndex < slot.Entries.Length; entryIndex++)
					{
						if (!TryAddOrReplace(destination, ref count, slot.Entries[entryIndex]))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		internal static void Reset()
		{
			lock (Gate)
			{
				Array.Clear(Slots, 0, Slots.Length);
			}
		}

		internal static bool TryAddOrReplace(PerfMeterGpuAnnotationEntry[] entries, ref int count, PerfMeterGpuAnnotationEntry value)
		{
			for (int index = 0; index < count; index++)
			{
				if (string.Equals(entries[index].Key, value.Key, StringComparison.Ordinal))
				{
					entries[index] = value;
					return true;
				}
			}
			if (count >= entries.Length)
			{
				return false;
			}
			entries[count++] = value;
			return true;
		}

		private sealed class ContextSlot
		{
			internal string OwnerId { get; }
			internal ulong Generation { get; }
			internal PerfMeterGpuAnnotationEntry[] Entries { get; }

			internal ContextSlot(string ownerId, ulong generation, PerfMeterGpuAnnotationEntry[] entries)
			{
				OwnerId = ownerId;
				Generation = generation;
				Entries = entries;
			}
		}
	}

	/// <summary>Public, RenderDoc-neutral entry point for bounded GPU annotation scopes.</summary>
	public static class PerfMeterGpuAnnotations
	{
		public static PerfMeterGpuAnnotationCapabilities Capabilities
		{
			get
			{
				IPerfMeterGpuAnnotationProvider provider = PerfMeterGpuAnnotationProviderRegistry.Get();
				return provider == null
					? new PerfMeterGpuAnnotationCapabilities(PerfMeterGpuAnnotationAvailability.ProviderUnavailable)
					: provider.GetCapabilities();
			}
		}

		public static bool ShouldRecord => Capabilities.IsReady;

		public static bool TryPublishContext(string ownerId, ulong generation, PerfMeterGpuAnnotationBatch context)
		{
			return PerfMeterGpuAnnotationContextRegistry.TryPublish(ownerId, generation, context);
		}

		public static bool TryClearContext(string ownerId, ulong generation)
		{
			return PerfMeterGpuAnnotationContextRegistry.TryClear(ownerId, generation);
		}

		public static PerfMeterGpuAnnotationScope BeginScope(CommandBuffer commandBuffer, PerfMeterGpuAnnotationBatch annotations)
		{
			if (commandBuffer == null)
			{
				throw new ArgumentNullException(nameof(commandBuffer));
			}
			return TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
				? BeginScope(new CommandBufferSink(commandBuffer), annotations, provider)
				: null;
		}

		internal static PerfMeterGpuAnnotationScope BeginScope(IPerfMeterGpuAnnotationCommandSink sink, PerfMeterGpuAnnotationBatch annotations)
		{
			return TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
				? BeginScope(sink, annotations, provider)
				: null;
		}

		internal static bool TryGetReadyProvider(out IPerfMeterGpuAnnotationProvider provider)
		{
			provider = PerfMeterGpuAnnotationProviderRegistry.Get();
			return provider != null && provider.GetCapabilities().IsReady;
		}

		internal static PerfMeterGpuAnnotationScope BeginScope(
			IPerfMeterGpuAnnotationCommandSink sink,
			PerfMeterGpuAnnotationBatch annotations,
			IPerfMeterGpuAnnotationProvider provider)
		{
			if (sink == null || annotations == null || annotations.Count == 0 || provider == null)
			{
				return null;
			}

			PerfMeterGpuAnnotationEntry[] beginEntries = new PerfMeterGpuAnnotationEntry[PerfMeterGpuAnnotationBatch.MaximumEntries];
			int beginCount = 0;
			if (!PerfMeterGpuAnnotationContextRegistry.TryCopyTo(beginEntries, ref beginCount))
			{
				return null;
			}
			for (int index = 0; index < annotations.Count; index++)
			{
				if (!PerfMeterGpuAnnotationContextRegistry.TryAddOrReplace(beginEntries, ref beginCount, annotations.GetEntry(index)))
				{
					return null;
				}
			}
			PerfMeterGpuAnnotationEntry schema = new PerfMeterGpuAnnotationEntry(
				PerfMeterGpuAnnotationKeys.SchemaVersionKey,
				PerfMeterGpuAnnotationValue.UInt32(PerfMeterGpuAnnotationKeys.SchemaVersion));
			if (!PerfMeterGpuAnnotationContextRegistry.TryAddOrReplace(beginEntries, ref beginCount, schema))
			{
				return null;
			}

			PerfMeterGpuAnnotationEntry[] endEntries = new PerfMeterGpuAnnotationEntry[beginCount];
			for (int index = 0; index < beginCount; index++)
			{
				endEntries[index] = new PerfMeterGpuAnnotationEntry(beginEntries[index].Key, PerfMeterGpuAnnotationValue.Empty());
			}

			if (!provider.TryCreateEvent(beginEntries, beginCount, out PerfMeterGpuAnnotationPreparedEvent beginEvent))
			{
				return null;
			}
			if (!provider.TryCreateEvent(endEntries, endEntries.Length, out PerfMeterGpuAnnotationPreparedEvent endEvent))
			{
				beginEvent.Cancel();
				return null;
			}

			try
			{
				sink.Issue(beginEvent.Callback, beginEvent.EventId, beginEvent.EventData);
				beginEvent.MarkEnqueued();
				return new PerfMeterGpuAnnotationScope(sink, endEvent);
			}
			catch
			{
				beginEvent.Cancel();
				endEvent.Cancel();
				throw;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			PerfMeterGpuAnnotationProviderRegistry.Reset();
			PerfMeterGpuAnnotationContextRegistry.Reset();
		}

		private sealed class CommandBufferSink : IPerfMeterGpuAnnotationCommandSink
		{
			private readonly CommandBuffer _commandBuffer;

			internal CommandBufferSink(CommandBuffer commandBuffer)
			{
				_commandBuffer = commandBuffer;
			}

			public void Issue(IntPtr callback, int eventId, IntPtr eventData)
			{
				_commandBuffer.IssuePluginEventAndData(callback, eventId, eventData);
			}
		}
	}
}
