using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterProfilerMetricCatalog
	{
		private const int SemanticCount = 11;

		private static readonly RecorderDefinition[] Definitions =
		{
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "Standard Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "Standard Indirect Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "Standard Instanced Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "SRP Batcher Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "BRG Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "BRG Indirect Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "Null Geometry Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.DrawCalls, PerfMeterCounterAvailability.DrawCalls, ProfilerCategory.Render, "Null Geometry Indirect Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.SetPassCalls, PerfMeterCounterAvailability.SetPassCalls, ProfilerCategory.Render, "SetPass Calls Count", "SetPass Calls"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.Batches, PerfMeterCounterAvailability.Batches, ProfilerCategory.Render, "Dynamic Batches Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.Batches, PerfMeterCounterAvailability.Batches, ProfilerCategory.Render, "Static Batches Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.Batches, PerfMeterCounterAvailability.Batches, ProfilerCategory.Render, "Instanced Batches Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.Vertices, PerfMeterCounterAvailability.Vertices, ProfilerCategory.Render, "Vertices Count", "Vertices"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.SrpBatcherInstances, PerfMeterCounterAvailability.SrpBatcherInstances, ProfilerCategory.Render, "SRP Batcher Instances Count", "SRP Batcher Instances"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.BrgDrawCalls, PerfMeterCounterAvailability.BrgDrawCalls, ProfilerCategory.Render, "BRG Draw Calls Count", "Hybrid Renderer (BRG) Draw Calls Count", "BatchRendererGroup Draw Calls Count", "GPU Resident Drawer Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.BrgDrawCalls, PerfMeterCounterAvailability.BrgDrawCalls, ProfilerCategory.Render, "BRG Indirect Draw Calls Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.BrgInstances, PerfMeterCounterAvailability.BrgInstances, ProfilerCategory.Render, "BRG Instances Count", "Hybrid Renderer (BRG) Instances Count", "BatchRendererGroup Instances Count", "GPU Resident Drawer Instances Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.BrgInstances, PerfMeterCounterAvailability.BrgInstances, ProfilerCategory.Render, "BRG Indirect Instances Count"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.IndexBufferUploadInFrameBytes, PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes, ProfilerCategory.Render, "Index Buffer Upload In Frame Bytes", "Index Buffer Upload Bytes", "Index Buffer Upload In Frame"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.SystemUsedMemory, PerfMeterCounterAvailability.SystemUsedMemory, ProfilerCategory.Memory, "System Used Memory"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.GcReservedMemory, PerfMeterCounterAvailability.GcReservedMemory, ProfilerCategory.Memory, "GC Reserved Memory"),
			new RecorderDefinition(PerfMeterProfilerMetricSemantic.GpuMemory, PerfMeterCounterAvailability.GpuMemory, ProfilerCategory.Memory, "Gfx Used Memory", "GPU Used Memory", "Graphics Used Memory", "GPU Memory")
		};

		private readonly List<ProfilerRecorderHandle> _availableHandles = new List<ProfilerRecorderHandle>(256);
		private readonly List<PerfMeterProfilerMetricDescriptor> _availableMetrics = new List<PerfMeterProfilerMetricDescriptor>(256);
		private readonly RecorderSlot[] _recorders = new RecorderSlot[Definitions.Length];
		private readonly PerfMeterProfilerMetricCapabilitySnapshot[] _capabilities = new PerfMeterProfilerMetricCapabilitySnapshot[SemanticCount];
		private readonly Action<List<PerfMeterProfilerMetricDescriptor>> _discoverMetrics;
		private readonly Func<ProfilerCategory, string, int, ProfilerRecorder> _startRecorder;
		private PerfMeterCounterAvailability _availableCounters;
		private PerfMeterCounterAvailability _unavailableCounters;
		private PerfMeterProfilerMetricCatalogState _state;
		private string _lastError = string.Empty;
		private int _revision;
		private int _discoveryCount;
		private bool _isRunning;

		internal PerfMeterCounterAvailability AvailableCounters => _availableCounters;
		internal PerfMeterCounterAvailability UnavailableCounters => _unavailableCounters;
		internal string LastError => _lastError;

		internal PerfMeterProfilerMetricCatalog()
		{
		}

		internal PerfMeterProfilerMetricCatalog(
			Action<List<PerfMeterProfilerMetricDescriptor>> discoverMetrics,
			Func<ProfilerCategory, string, int, ProfilerRecorder> startRecorder)
		{
			_discoverMetrics = discoverMetrics;
			_startRecorder = startRecorder;
		}

		internal void Start()
		{
			if (_isRunning)
			{
				return;
			}

			_isRunning = true;
			Refresh();
		}

		internal bool Refresh()
		{
			_discoveryCount++;
			_availableHandles.Clear();
			_availableMetrics.Clear();
			RecorderSlot[] candidateRecorders = new RecorderSlot[Definitions.Length];
			string candidateError = string.Empty;
			PerfMeterProfilerMetricCatalogState previousState = _state;

			try
			{
				DiscoverAvailableMetrics();

				for (int i = 0; i < Definitions.Length; i++)
				{
					RecorderDefinition definition = Definitions[i];
					if (!TryStartCandidateRecorder(definition, out candidateRecorders[i], ref candidateError))
					{
						throw new InvalidOperationException(candidateError);
					}
				}

				DisposeRecorders(_recorders);
				Array.Copy(candidateRecorders, _recorders, candidateRecorders.Length);
				_lastError = candidateError;
				_revision++;
				_state = PerfMeterProfilerMetricCatalogState.Ready;
				RebuildCapabilities();
				return true;
			}
			catch (Exception exception)
			{
				DisposeRecorders(candidateRecorders);
				_lastError = exception.Message ?? exception.GetType().Name;
				if (previousState != PerfMeterProfilerMetricCatalogState.Ready)
				{
					DisposeRecorders(_recorders);
					_state = PerfMeterProfilerMetricCatalogState.Error;
					RebuildCapabilities();
				}
				return false;
			}
		}

		internal void Stop()
		{
			DisposeRecorders(_recorders);
			_isRunning = false;
			_state = PerfMeterProfilerMetricCatalogState.NotInitialized;
			_availableCounters = PerfMeterCounterAvailability.None;
			_unavailableCounters = PerfMeterCounterAvailability.None;
			_lastError = string.Empty;
			_revision = 0;
			_discoveryCount = 0;
			Array.Clear(_capabilities, 0, _capabilities.Length);
		}

		internal void RefreshSampleStates()
		{
			if (_state == PerfMeterProfilerMetricCatalogState.Ready)
			{
				UpdateSampleStates();
			}
		}

		internal long ReadLongCounter(PerfMeterCounterAvailability counter)
		{
			long value = 0L;
			for (int i = 0; i < _recorders.Length; i++)
			{
				if (_recorders[i].Counter == counter && _recorders[i].HasSample)
				{
					value += _recorders[i].LastValue;
				}
			}

			return value;
		}

		internal PerfMeterProfilerMetricCatalogSnapshot GetSnapshot()
		{
			if (_state == PerfMeterProfilerMetricCatalogState.NotInitialized)
			{
				return PerfMeterProfilerMetricCatalogSnapshot.NotInitialized;
			}

			PerfMeterProfilerMetricCapabilitySnapshot[] capabilities = new PerfMeterProfilerMetricCapabilitySnapshot[_capabilities.Length];
			Array.Copy(_capabilities, capabilities, _capabilities.Length);
			return new PerfMeterProfilerMetricCatalogSnapshot(_state, _revision, _discoveryCount, capabilities, _lastError);
		}

		internal static PerfMeterProfilerMetricResolution ResolveMetricName(
			string category,
			string exactName,
			string[] aliases,
			IList<PerfMeterProfilerMetricDescriptor> availableMetrics,
			out PerfMeterProfilerMetricDescriptor descriptor)
		{
			if (TryFindMetric(category, exactName, availableMetrics, out descriptor))
			{
				return PerfMeterProfilerMetricResolution.Exact;
			}

			if (aliases != null)
			{
				for (int i = 0; i < aliases.Length; i++)
				{
					if (TryFindMetric(category, aliases[i], availableMetrics, out descriptor))
					{
						return PerfMeterProfilerMetricResolution.Alias;
					}
				}
			}

			descriptor = default;
			return PerfMeterProfilerMetricResolution.None;
		}

		internal static PerfMeterProfilerMetricSampleState GetSampleState(int resolvedComponentCount, int sampledComponentCount)
		{
			if (resolvedComponentCount <= 0)
			{
				return PerfMeterProfilerMetricSampleState.Unavailable;
			}

			return sampledComponentCount > 0
				? PerfMeterProfilerMetricSampleState.AvailableSampled
				: PerfMeterProfilerMetricSampleState.AvailableNoSample;
		}

		private static bool TryFindMetric(string category, string name, IList<PerfMeterProfilerMetricDescriptor> availableMetrics, out PerfMeterProfilerMetricDescriptor descriptor)
		{
			for (int i = 0; i < availableMetrics.Count; i++)
			{
				PerfMeterProfilerMetricDescriptor candidate = availableMetrics[i];
				if (string.Equals(candidate.Category, category, StringComparison.Ordinal) && string.Equals(candidate.Name, name, StringComparison.Ordinal))
				{
					descriptor = candidate;
					return true;
				}
			}

			descriptor = default;
			return false;
		}

		private void DiscoverAvailableMetrics()
		{
			if (_discoverMetrics != null)
			{
				_discoverMetrics(_availableMetrics);
				return;
			}

			ProfilerRecorderHandle.GetAvailable(_availableHandles);
			for (int i = 0; i < _availableHandles.Count; i++)
			{
				ProfilerRecorderHandle handle = _availableHandles[i];
				if (!handle.Valid)
				{
					continue;
				}

				ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
				if (!string.IsNullOrEmpty(description.Name))
				{
					_availableMetrics.Add(new PerfMeterProfilerMetricDescriptor(
						description.Category.Name,
						description.Name,
						description.UnitType.ToString(),
						description.DataType.ToString()));
				}
			}
		}

		private bool TryStartCandidateRecorder(RecorderDefinition definition, out RecorderSlot recorder, ref string candidateError)
		{
			bool foundDescriptor = false;
			string startError = string.Empty;
			if (TryFindMetric(definition.Category.Name, definition.ExactName, _availableMetrics, out PerfMeterProfilerMetricDescriptor exactDescriptor))
			{
				foundDescriptor = true;
				recorder = new RecorderSlot(definition, exactDescriptor, PerfMeterProfilerMetricResolution.Exact);
				if (recorder.Start(_startRecorder, ref startError))
				{
					return true;
				}
			}

			for (int i = 0; i < definition.Aliases.Length; i++)
			{
				if (!TryFindMetric(definition.Category.Name, definition.Aliases[i], _availableMetrics, out PerfMeterProfilerMetricDescriptor aliasDescriptor))
				{
					continue;
				}

				foundDescriptor = true;
				recorder = new RecorderSlot(definition, aliasDescriptor, PerfMeterProfilerMetricResolution.Alias);
				if (recorder.Start(_startRecorder, ref startError))
				{
					return true;
				}
			}

			if (!foundDescriptor)
			{
				recorder = new RecorderSlot(definition, default, PerfMeterProfilerMetricResolution.None);
				return true;
			}

			AppendError(ref candidateError, startError);
			recorder = default;
			return false;
		}

		private void RebuildCapabilities()
		{
			_availableCounters = PerfMeterCounterAvailability.None;
			_unavailableCounters = PerfMeterCounterAvailability.None;

			for (int semanticIndex = 0; semanticIndex < SemanticCount; semanticIndex++)
			{
				PerfMeterProfilerMetricSemantic semantic = (PerfMeterProfilerMetricSemantic)semanticIndex;
				PerfMeterCounterAvailability counter = GetCounter(semantic);
				PerfMeterProfilerMetricResolution resolution = PerfMeterProfilerMetricResolution.None;
				string category = string.Empty;
				string unit = string.Empty;
				string dataType = string.Empty;
				int resolvedCount = 0;
				int sampledCount = 0;
				StringBuilder resolvedNames = null;

				for (int recorderIndex = 0; recorderIndex < _recorders.Length; recorderIndex++)
				{
					RecorderSlot recorder = _recorders[recorderIndex];
					if (recorder.Semantic != semantic)
					{
						continue;
					}

					if (!recorder.IsValid)
					{
						continue;
					}

					resolvedCount++;
					if (recorder.HasSample)
					{
						sampledCount++;
					}

					if (recorder.Resolution == PerfMeterProfilerMetricResolution.Alias)
					{
						resolution = PerfMeterProfilerMetricResolution.Alias;
					}
					else if (resolution == PerfMeterProfilerMetricResolution.None)
					{
						resolution = PerfMeterProfilerMetricResolution.Exact;
					}

					category = MergeMetadata(category, recorder.Category);
					unit = MergeMetadata(unit, recorder.Unit);
					dataType = MergeMetadata(dataType, recorder.DataType);
					resolvedNames ??= new StringBuilder(96);
					if (resolvedNames.Length > 0)
					{
						resolvedNames.Append(", ");
					}
					resolvedNames.Append(recorder.Name);
				}

				PerfMeterProfilerMetricSampleState sampleState = GetSampleState(resolvedCount, sampledCount);
				if (counter != PerfMeterCounterAvailability.None)
				{
					if (sampleState == PerfMeterProfilerMetricSampleState.Unavailable)
					{
						_unavailableCounters |= counter;
					}
					else
					{
						_availableCounters |= counter;
					}
				}

				_capabilities[semanticIndex] = new PerfMeterProfilerMetricCapabilitySnapshot(
					semantic,
					sampleState,
					resolution,
					category,
					resolvedNames?.ToString() ?? string.Empty,
					unit,
					dataType,
					resolvedCount,
					sampledCount);
			}
		}

		private void UpdateSampleStates()
		{
			for (int semanticIndex = 0; semanticIndex < SemanticCount; semanticIndex++)
			{
				PerfMeterProfilerMetricSemantic semantic = (PerfMeterProfilerMetricSemantic)semanticIndex;
				int sampledCount = 0;
				for (int recorderIndex = 0; recorderIndex < _recorders.Length; recorderIndex++)
				{
					RecorderSlot recorder = _recorders[recorderIndex];
					if (recorder.Semantic == semantic && recorder.HasSample)
					{
						sampledCount++;
					}
				}

				PerfMeterProfilerMetricCapabilitySnapshot current = _capabilities[semanticIndex];
				_capabilities[semanticIndex] = new PerfMeterProfilerMetricCapabilitySnapshot(
					current.Semantic,
					GetSampleState(current.ResolvedComponentCount, sampledCount),
					current.Resolution,
					current.Category,
					current.ResolvedRecorderNames,
					current.Unit,
					current.DataType,
					current.ResolvedComponentCount,
					sampledCount);
			}
		}

		private static string MergeMetadata(string current, string next)
		{
			if (string.IsNullOrEmpty(current))
			{
				return next ?? string.Empty;
			}

			return string.IsNullOrEmpty(next) || string.Equals(current, next, StringComparison.Ordinal) ? current : "Mixed";
		}

		private static PerfMeterCounterAvailability GetCounter(PerfMeterProfilerMetricSemantic semantic)
		{
			switch (semantic)
			{
				case PerfMeterProfilerMetricSemantic.DrawCalls:
					return PerfMeterCounterAvailability.DrawCalls;
				case PerfMeterProfilerMetricSemantic.SetPassCalls:
					return PerfMeterCounterAvailability.SetPassCalls;
				case PerfMeterProfilerMetricSemantic.Batches:
					return PerfMeterCounterAvailability.Batches;
				case PerfMeterProfilerMetricSemantic.Vertices:
					return PerfMeterCounterAvailability.Vertices;
				case PerfMeterProfilerMetricSemantic.SrpBatcherInstances:
					return PerfMeterCounterAvailability.SrpBatcherInstances;
				case PerfMeterProfilerMetricSemantic.BrgDrawCalls:
					return PerfMeterCounterAvailability.BrgDrawCalls;
				case PerfMeterProfilerMetricSemantic.BrgInstances:
					return PerfMeterCounterAvailability.BrgInstances;
				case PerfMeterProfilerMetricSemantic.IndexBufferUploadInFrameBytes:
					return PerfMeterCounterAvailability.IndexBufferUploadInFrameBytes;
				case PerfMeterProfilerMetricSemantic.SystemUsedMemory:
					return PerfMeterCounterAvailability.SystemUsedMemory;
				case PerfMeterProfilerMetricSemantic.GcReservedMemory:
					return PerfMeterCounterAvailability.GcReservedMemory;
				case PerfMeterProfilerMetricSemantic.GpuMemory:
					return PerfMeterCounterAvailability.GpuMemory;
				default:
					return PerfMeterCounterAvailability.None;
			}
		}

		private static void DisposeRecorders(RecorderSlot[] recorders)
		{
			for (int i = 0; i < recorders.Length; i++)
			{
				recorders[i].Dispose();
				recorders[i] = default;
			}
		}

		private readonly struct RecorderDefinition
		{
			internal RecorderDefinition(PerfMeterProfilerMetricSemantic semantic, PerfMeterCounterAvailability counter, ProfilerCategory category, string exactName, params string[] aliases)
			{
				Semantic = semantic;
				Counter = counter;
				Category = category;
				ExactName = exactName;
				Aliases = aliases ?? Array.Empty<string>();
			}

			internal PerfMeterProfilerMetricSemantic Semantic { get; }
			internal PerfMeterCounterAvailability Counter { get; }
			internal ProfilerCategory Category { get; }
			internal string ExactName { get; }
			internal string[] Aliases { get; }
		}

		private struct RecorderSlot
		{
			private readonly RecorderDefinition _definition;
			private readonly PerfMeterProfilerMetricDescriptor _descriptor;
			private ProfilerRecorder _recorder;

			internal RecorderSlot(RecorderDefinition definition, PerfMeterProfilerMetricDescriptor descriptor, PerfMeterProfilerMetricResolution resolution)
			{
				_definition = definition;
				_descriptor = descriptor;
				Resolution = resolution;
				_recorder = default;
			}

			internal PerfMeterProfilerMetricSemantic Semantic => _definition.Semantic;
			internal PerfMeterCounterAvailability Counter => _definition.Counter;
			internal PerfMeterProfilerMetricResolution Resolution { get; }
			internal bool IsValid => _recorder.Valid;
			internal bool HasSample => _recorder.Valid && _recorder.Count > 0;
			internal long LastValue => HasSample ? _recorder.LastValue : 0L;
			internal string Category => _descriptor.Category;
			internal string Name => _descriptor.Name;
			internal string Unit => _descriptor.Unit;
			internal string DataType => _descriptor.DataType;

			internal bool Start(Func<ProfilerCategory, string, int, ProfilerRecorder> startRecorder, ref string lastError)
			{
				if (Resolution == PerfMeterProfilerMetricResolution.None)
				{
					return true;
				}

				try
				{
					_recorder = startRecorder != null
						? startRecorder(_definition.Category, _descriptor.Name, 1)
						: ProfilerRecorder.StartNew(_definition.Category, _descriptor.Name, 1);
					if (!_recorder.Valid)
					{
						AppendError(ref lastError, $"ProfilerRecorder '{_definition.Category.Name}/{_descriptor.Name}' could not be started.");
						Dispose();
						return false;
					}

					return true;
				}
				catch (Exception exception)
				{
					AppendError(
						ref lastError,
						string.IsNullOrEmpty(exception.Message)
							? $"ProfilerRecorder '{_definition.Category.Name}/{_descriptor.Name}' could not be started."
							: exception.Message);
					Dispose();
					return false;
				}
			}

			internal void Dispose()
			{
				if (_recorder.Valid)
				{
					_recorder.Dispose();
				}

				_recorder = default;
			}
		}

		private static void AppendError(ref string lastError, string message)
		{
			if (!string.IsNullOrEmpty(message) && !lastError.Contains(message))
			{
				lastError = string.IsNullOrEmpty(lastError) ? message : lastError + " " + message;
			}
		}
	}

	internal readonly struct PerfMeterProfilerMetricDescriptor
	{
		internal PerfMeterProfilerMetricDescriptor(string category, string name, string unit = "", string dataType = "")
		{
			Category = category ?? string.Empty;
			Name = name ?? string.Empty;
			Unit = unit ?? string.Empty;
			DataType = dataType ?? string.Empty;
		}

		internal string Category { get; }
		internal string Name { get; }
		internal string Unit { get; }
		internal string DataType { get; }
	}
}
