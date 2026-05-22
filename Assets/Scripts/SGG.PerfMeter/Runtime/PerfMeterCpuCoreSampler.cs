using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal enum PerfMeterCpuCoreLoadAvailability
	{
		Unsupported = 0,
		Unavailable = 1,
		WarmingUp = 2,
		Available = 3
	}

	public readonly struct PerfMeterCpuCoreLoadSnapshot
	{
		public PerfMeterCpuCoreLoadSnapshot(int coreIndex, double loadPercent, bool available)
		{
			CoreIndex = Mathf.Max(0, coreIndex);
			LoadPercent = double.IsNaN(loadPercent) || double.IsInfinity(loadPercent) ? 0d : Mathf.Clamp((float)loadPercent, 0f, 100f);
			Available = available;
		}

		public int CoreIndex { get; }
		public double LoadPercent { get; }
		public bool Available { get; }
	}

	internal sealed class PerfMeterCpuCoreSampler : IDisposable
	{
		internal const int MaxCoreCount = 64;
		private const float SampleIntervalSeconds = 0.75f;
		private const int ProcStatBufferSize = 16 * 1024;
		private const string ProcStatPath = "/proc/stat";
		private const string UnsupportedWarning = "CPU per-core load is supported on Windows 11/Windows Editor via NtQuerySystemInformation, Apple platforms via host_processor_info, and Android/Linux via /proc/stat.";

		private readonly CpuCoreTick[] _previousTicks = new CpuCoreTick[MaxCoreCount];
		private readonly CpuCoreTick[] _currentTicks = new CpuCoreTick[MaxCoreCount];
		private readonly PerfMeterCpuCoreLoadSnapshot[] _loads = new PerfMeterCpuCoreLoadSnapshot[MaxCoreCount];
		private readonly byte[] _procStatBuffer = new byte[ProcStatBufferSize];
		private readonly WindowsCpuCoreBackend _windowsBackend = new WindowsCpuCoreBackend();
		private readonly DarwinCpuCoreBackend _darwinBackend = new DarwinCpuCoreBackend();
		private float _nextSampleTime;
		private int _coreCount;
		private bool _hasPrevious;
		private PerfMeterCpuCoreLoadAvailability _availability;
		private string _warning = string.Empty;

		internal PerfMeterCpuCoreSampler()
		{
			_availability = IsPlatformSupported() ? PerfMeterCpuCoreLoadAvailability.WarmingUp : PerfMeterCpuCoreLoadAvailability.Unsupported;
			if (_availability == PerfMeterCpuCoreLoadAvailability.Unsupported)
			{
				_warning = UnsupportedWarning;
			}
		}

		internal PerfMeterCpuCoreLoadAvailability Availability => _availability;
		internal string Warning => _warning;
		internal int CoreCount => _coreCount;

		internal void Reset()
		{
			_nextSampleTime = 0f;
			_coreCount = 0;
			_hasPrevious = false;
			Array.Clear(_loads, 0, _loads.Length);
			_availability = IsPlatformSupported() ? PerfMeterCpuCoreLoadAvailability.WarmingUp : PerfMeterCpuCoreLoadAvailability.Unsupported;
			_warning = _availability == PerfMeterCpuCoreLoadAvailability.Unsupported ? UnsupportedWarning : string.Empty;
		}

		public void Dispose()
		{
			_windowsBackend.Dispose();
			_darwinBackend.Dispose();
		}

		internal void Update(float unscaledTime)
		{
			if (!IsPlatformSupported())
			{
				_availability = PerfMeterCpuCoreLoadAvailability.Unsupported;
				_warning = UnsupportedWarning;
				return;
			}

			if (unscaledTime < _nextSampleTime)
			{
				return;
			}

			_nextSampleTime = unscaledTime + SampleIntervalSeconds;
			try
			{
				if (!TrySampleTicks(out int count))
				{
					ClearLoads();
					_hasPrevious = false;
					_availability = PerfMeterCpuCoreLoadAvailability.Unavailable;
					if (string.IsNullOrEmpty(_warning))
					{
						_warning = "Failed to sample CPU core rows.";
					}

					return;
				}

				if (!_hasPrevious)
				{
					CopyTicks(_currentTicks, _previousTicks, count);
					_hasPrevious = true;
					ClearLoads();
					_availability = PerfMeterCpuCoreLoadAvailability.WarmingUp;
					_warning = string.Empty;
					return;
				}

				for (int i = 0; i < count; i++)
				{
					double load = CalculateLoadPercent(_previousTicks[i], _currentTicks[i]);
					_loads[i] = new PerfMeterCpuCoreLoadSnapshot(i, load, true);
				}

				_coreCount = count;

				for (int i = count; i < _loads.Length; i++)
				{
					_loads[i] = default;
				}

				CopyTicks(_currentTicks, _previousTicks, count);
				_availability = PerfMeterCpuCoreLoadAvailability.Available;
				_warning = string.Empty;
			}
			catch (Exception exception)
			{
				ClearLoads();
				_hasPrevious = false;
				_availability = PerfMeterCpuCoreLoadAvailability.Unavailable;
				_warning = exception.GetType().Name + ": " + exception.Message;
			}
		}

		internal PerfMeterCpuCoreLoadSnapshot[] GetLoadsCopy()
		{
			if (_coreCount <= 0)
			{
				return Array.Empty<PerfMeterCpuCoreLoadSnapshot>();
			}

			PerfMeterCpuCoreLoadSnapshot[] copy = new PerfMeterCpuCoreLoadSnapshot[_coreCount];
			Array.Copy(_loads, copy, _coreCount);
			return copy;
		}

		internal PerfMeterCpuCoreLoadSnapshot[] PeekLoads()
		{
			return _loads;
		}

		private bool TrySampleTicks(out int count)
		{
			if (IsWindowsPlatform())
			{
				bool sampled = _windowsBackend.TrySample(_currentTicks, out count, out string warning);
				_warning = warning;
				return sampled;
			}

			if (IsDarwinPlatform())
			{
				bool sampled = _darwinBackend.TrySample(_currentTicks, out count, out string warning);
				_warning = warning;
				return sampled;
			}

			bool parsed = TrySampleProcStat(out count);
			if (parsed)
			{
				_warning = string.Empty;
			}
			else if (string.IsNullOrEmpty(_warning))
			{
				_warning = "Failed to parse /proc/stat CPU core rows.";
			}

			return parsed;
		}

		private bool TrySampleProcStat(out int count)
		{
			count = 0;
			if (!File.Exists(ProcStatPath))
			{
				_warning = "/proc/stat is unavailable; CPU per-core load cannot be sampled on this platform.";
				return false;
			}

			int bytesRead;
			using (FileStream stream = new FileStream(ProcStatPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				bytesRead = stream.Read(_procStatBuffer, 0, _procStatBuffer.Length);
			}

			return TryParseProcStat(_procStatBuffer, bytesRead, _currentTicks, out count);
		}

		internal static bool TryParseProcStat(byte[] buffer, int length, CpuCoreTick[] ticks, out int count)
		{
			count = 0;
			if (buffer == null || ticks == null || length <= 0)
			{
				return false;
			}

			int limit = Math.Min(length, buffer.Length);
			int index = 0;
			while (index < limit && count < ticks.Length)
			{
				int lineStart = index;
				while (index < limit && buffer[index] != '\n' && buffer[index] != '\r')
				{
					index++;
				}

				int lineEnd = index;
				while (index < limit && (buffer[index] == '\n' || buffer[index] == '\r'))
				{
					index++;
				}

				if (TryParseProcStatLine(buffer, lineStart, lineEnd, out CpuCoreTick tick))
				{
					ticks[count++] = tick;
				}
			}

			return count > 0;
		}

		internal static bool TryParseProcStat(string[] lines, CpuCoreTick[] ticks, out int count)
		{
			count = 0;
			if (lines == null || ticks == null)
			{
				return false;
			}

			for (int lineIndex = 0; lineIndex < lines.Length && count < ticks.Length; lineIndex++)
			{
				string line = lines[lineIndex];
				if (string.IsNullOrEmpty(line) || !line.StartsWith("cpu", StringComparison.Ordinal))
				{
					continue;
				}

				int nameEnd = IndexOfWhitespace(line, 0);
				if (nameEnd <= 3 || !IsCpuCoreName(line, nameEnd))
				{
					continue;
				}

				if (TryParseCpuCoreTick(line, nameEnd + 1, out CpuCoreTick tick))
				{
					ticks[count++] = tick;
				}
			}

			return count > 0;
		}

		private static bool IsPlatformSupported()
		{
			return IsWindowsPlatform() || IsDarwinPlatform() || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor;
		}

		private static bool IsWindowsPlatform()
		{
			return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
		}

		private static bool IsDarwinPlatform()
		{
			return Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS;
		}

		private static bool TryParseProcStatLine(byte[] buffer, int start, int end, out CpuCoreTick tick)
		{
			tick = default;
			if (end - start < 5 || buffer[start] != (byte)'c' || buffer[start + 1] != (byte)'p' || buffer[start + 2] != (byte)'u')
			{
				return false;
			}

			int index = start + 3;
			if (index >= end || !IsAsciiDigit(buffer[index]))
			{
				return false;
			}

			while (index < end && IsAsciiDigit(buffer[index]))
			{
				index++;
			}

			if (index >= end || !IsProcWhitespace(buffer[index]))
			{
				return false;
			}

			return TryParseCpuCoreTick(buffer, index + 1, end, out tick);
		}

		private static bool IsCpuCoreName(string line, int nameEnd)
		{
			for (int i = 3; i < nameEnd; i++)
			{
				if (!char.IsDigit(line[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static int IndexOfWhitespace(string line, int start)
		{
			for (int i = Math.Max(0, start); i < line.Length; i++)
			{
				if (line[i] == ' ' || line[i] == '\t')
				{
					return i;
				}
			}

			return -1;
		}

		private static bool TryParseCpuCoreTick(string line, int start, out CpuCoreTick tick)
		{
			tick = default;
			int valueCount = 0;
			int index = start;
			long total = 0L;
			long idle = 0L;
			while (index < line.Length && valueCount < 10)
			{
				while (index < line.Length && (line[index] == ' ' || line[index] == '\t'))
				{
					index++;
				}

				long value = 0L;
				bool hasDigits = false;
				while (index < line.Length && line[index] != ' ' && line[index] != '\t')
				{
					char character = line[index];
					if (character >= '0' && character <= '9')
					{
						value = value * 10L + (character - '0');
						hasDigits = true;
					}

					index++;
				}

				if (hasDigits)
				{
					if (valueCount == 3 || valueCount == 4)
					{
						idle += value;
					}

					total += value;
					valueCount++;
				}
			}

			if (valueCount < 4)
			{
				return false;
			}

			tick = new CpuCoreTick(total, idle);
			return total > 0L;
		}

		private static bool TryParseCpuCoreTick(byte[] buffer, int start, int end, out CpuCoreTick tick)
		{
			tick = default;
			int valueCount = 0;
			int index = start;
			long total = 0L;
			long idle = 0L;
			while (index < end && valueCount < 10)
			{
				while (index < end && IsProcWhitespace(buffer[index]))
				{
					index++;
				}

				if (index >= end)
				{
					break;
				}

				long value = 0L;
				bool hasDigits = false;
				while (index < end && !IsProcWhitespace(buffer[index]))
				{
					byte character = buffer[index];
					if (!IsAsciiDigit(character))
					{
						return false;
					}

					value = value * 10L + (character - (byte)'0');
					hasDigits = true;
					index++;
				}

				if (!hasDigits)
				{
					return false;
				}

				if (valueCount == 3 || valueCount == 4)
				{
					idle += value;
				}

				total += value;
				valueCount++;
			}

			if (valueCount < 4)
			{
				return false;
			}

			tick = new CpuCoreTick(total, idle);
			return total > 0L;
		}

		private static bool IsAsciiDigit(byte value)
		{
			return value >= (byte)'0' && value <= (byte)'9';
		}

		private static bool IsProcWhitespace(byte value)
		{
			return value == (byte)' ' || value == (byte)'\t';
		}

		private void ClearLoads()
		{
			_coreCount = 0;
			Array.Clear(_loads, 0, _loads.Length);
		}

		internal static double CalculateLoadPercent(CpuCoreTick previous, CpuCoreTick current)
		{
			long totalDelta = current.Total - previous.Total;
			long idleDelta = current.Idle - previous.Idle;
			if (totalDelta <= 0L)
			{
				return 0d;
			}

			return Math.Max(0d, Math.Min(100d, (totalDelta - idleDelta) * 100d / totalDelta));
		}

		private static void CopyTicks(CpuCoreTick[] source, CpuCoreTick[] destination, int count)
		{
			for (int i = 0; i < count; i++)
			{
				destination[i] = source[i];
			}
		}

		internal readonly struct CpuCoreTick
		{
			internal CpuCoreTick(long total, long idle)
			{
				Total = total;
				Idle = idle;
			}

			internal long Total { get; }
			internal long Idle { get; }
		}

		private sealed class DarwinCpuCoreBackend : IDisposable
		{
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS || UNITY_TVOS
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
			private const string LibSystem = "__Internal";
#else
			private const string LibSystem = "libSystem.dylib";
#endif
			private const int KernSuccess = 0;
			private const int ProcessorCpuLoadInfo = 2;
			private const int CpuStateUser = 0;
			private const int CpuStateSystem = 1;
			private const int CpuStateIdle = 2;
			private const int CpuStateNice = 3;
			private const int CpuStateMax = 4;

			[DllImport(LibSystem)]
			private static extern IntPtr mach_host_self();

			[DllImport(LibSystem)]
			private static extern IntPtr mach_task_self();

			[DllImport(LibSystem)]
			private static extern int host_processor_info(IntPtr host, int flavor, out uint outProcessorCount, out IntPtr outProcessorInfo, out uint outProcessorInfoCount);

			[DllImport(LibSystem)]
			private static extern int vm_deallocate(IntPtr targetTask, IntPtr address, IntPtr size);

			internal bool TrySample(CpuCoreTick[] ticks, out int count, out string warning)
			{
				count = 0;
				warning = string.Empty;
				if (ticks == null || ticks.Length == 0)
				{
					warning = "CPU core load buffer is unavailable.";
					return false;
				}

				IntPtr processorInfo = IntPtr.Zero;
				uint processorInfoCount = 0;
				try
				{
					int result = host_processor_info(mach_host_self(), ProcessorCpuLoadInfo, out uint processorCount, out processorInfo, out processorInfoCount);
					if (result != KernSuccess)
					{
						warning = "host_processor_info(PROCESSOR_CPU_LOAD_INFO) failed with status " + result.ToString(CultureInfo.InvariantCulture) + ".";
						return false;
					}

					int availableCount = Math.Min((int)processorCount, (int)(processorInfoCount / CpuStateMax));
					count = Math.Min(ticks.Length, availableCount);
					for (int core = 0; core < count; core++)
					{
						long user = ReadDarwinTick(processorInfo, core, CpuStateUser);
						long system = ReadDarwinTick(processorInfo, core, CpuStateSystem);
						long idle = ReadDarwinTick(processorInfo, core, CpuStateIdle);
						long nice = ReadDarwinTick(processorInfo, core, CpuStateNice);
						ticks[core] = new CpuCoreTick(user + system + idle + nice, idle);
					}

					return count > 0;
				}
				finally
				{
					if (processorInfo != IntPtr.Zero)
					{
						vm_deallocate(mach_task_self(), processorInfo, new IntPtr((long)processorInfoCount * sizeof(int)));
					}
				}
			}

			private static long ReadDarwinTick(IntPtr processorInfo, int core, int state)
			{
				int value = Marshal.ReadInt32(processorInfo, (core * CpuStateMax + state) * sizeof(int));
				return unchecked((uint)value);
			}

			public void Dispose()
			{
			}
#else
			internal bool TrySample(CpuCoreTick[] ticks, out int count, out string warning)
			{
				count = 0;
				warning = UnsupportedWarning;
				return false;
			}

			public void Dispose()
			{
			}
#endif
		}

		private sealed class WindowsCpuCoreBackend : IDisposable
		{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			private const int SystemProcessorPerformanceInformation = 8;
			private const int StatusSuccess = 0;

			private readonly int _structSize = Marshal.SizeOf<SystemProcessorPerformanceInformationStruct>();
			private readonly int _processorCount = Math.Max(1, Math.Min(MaxCoreCount, Environment.ProcessorCount));
			private IntPtr _buffer;

			[DllImport("ntdll.dll")]
			private static extern int NtQuerySystemInformation(int systemInformationClass, IntPtr systemInformation, uint systemInformationLength, out uint returnLength);

			[StructLayout(LayoutKind.Sequential)]
			private struct SystemProcessorPerformanceInformationStruct
			{
				public long IdleTime;
				public long KernelTime;
				public long UserTime;
				public long Reserved1;
				public long Reserved2;
				public uint Reserved3;
			}

			internal bool TrySample(CpuCoreTick[] ticks, out int count, out string warning)
			{
				count = 0;
				warning = string.Empty;
				if (ticks == null || ticks.Length == 0)
				{
					warning = "CPU core load buffer is unavailable.";
					return false;
				}

				EnsureBuffer();
				if (_buffer == IntPtr.Zero)
				{
					warning = "Failed to allocate Windows CPU core load buffer.";
					return false;
				}

				int status = NtQuerySystemInformation(SystemProcessorPerformanceInformation, _buffer, (uint)(_structSize * _processorCount), out uint returnLength);
				if (status != StatusSuccess)
				{
					warning = "NtQuerySystemInformation(SystemProcessorPerformanceInformation) failed with status " + status.ToString(CultureInfo.InvariantCulture) + ".";
					return false;
				}

				int availableCount = Math.Min(_processorCount, returnLength > 0 ? (int)(returnLength / _structSize) : _processorCount);
				count = Math.Min(ticks.Length, availableCount);
				for (int i = 0; i < count; i++)
				{
					IntPtr pointer = IntPtr.Add(_buffer, i * _structSize);
					SystemProcessorPerformanceInformationStruct info = Marshal.PtrToStructure<SystemProcessorPerformanceInformationStruct>(pointer);
					ticks[i] = new CpuCoreTick(info.KernelTime + info.UserTime, info.IdleTime);
				}

				return count > 0;
			}

			private void EnsureBuffer()
			{
				if (_buffer == IntPtr.Zero)
				{
					_buffer = Marshal.AllocHGlobal(_structSize * _processorCount);
				}
			}

			public void Dispose()
			{
				if (_buffer != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(_buffer);
					_buffer = IntPtr.Zero;
				}
			}
#else
			internal bool TrySample(CpuCoreTick[] ticks, out int count, out string warning)
			{
				count = 0;
				warning = UnsupportedWarning;
				return false;
			}

			public void Dispose()
			{
			}
#endif
		}
	}
}
