using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	/// <summary>Producer evidence, not a result inferred from a sampler's GPU sample count.</summary>
	public enum PerfMeterGpuPassAvailability
	{
		Unknown = 0,
		Unavailable = 1,
		NotScheduled = 2,
		Scheduled = 3
	}

	/// <summary>Optional URP custom metric backed by the exact sampler supplied to a Render Graph pass.</summary>
	/// <remarks>
	/// All methods must be called on the Unity main thread. The caller owns recording and the sampler's lifetime;
	/// this provider never changes enableRecording, disposes the sampler, or enables global GPU profiling.
	/// Timings are sampler-wide aggregates, not per-camera or per-GPU-frame measurements.
	/// </remarks>
	public sealed class PerfMeterProfilingSamplerMetricProvider : IPerfMeterCustomMetricProvider, IDisposable
	{
		private const string DisabledWarning = "RecordingNotDeclared: the caller has not enabled this metric's recording epoch.";
		private const string DisposedWarning = "ProviderDisposed: unregister this provider to remove the metric.";
		private const string UnknownWarning = "ProducerEvidenceMissing: the producer has not reported pass availability.";
		private const string UnavailableWarning = "PassUnavailable: the producer reported that the workload is unavailable.";
		private const string NotScheduledWarning = "PassNotScheduled: the producer reported that this pass was not scheduled.";
		private const string ProducerStaleWarning = "ProducerEvidenceStale: pass scheduling evidence exceeded the configured observation window.";
		private const string ExecutionMissingWarning = "PassCallbackNotObserved: scheduling alone does not prove that the Render Graph render function ran.";
		private const string ExecutionStaleWarning = "PassCallbackEvidenceStale: render-function evidence exceeded the configured observation window.";
		private const string ReadFailedWarning = "SamplerReadFailed: the exact sampler's GPU properties could not be read.";
		private const string InvalidWarning = "InvalidGpuReading: GPU count or elapsed milliseconds is negative or non-finite.";
		private const string NoSamplesWarning = "GpuSamplesPending: no GPU samples; this may be warm-up, delayed results, or unavailable GPU recording, not proof of a missing pass.";
		private const string UnchangedWarning = "SamplerObservationUnchanged: waiting for a post-callback change from the recording epoch's baseline.";
		private const string ObservationStaleWarning = "SamplerObservationStale: unchanged readings cannot prove that a new GPU result arrived.";
		private const string DelayedWarning = "DelayedSamplerObservation: source GPU frame and camera attribution are not exposed by ProfilingSampler.";
		private const string ClockResetWarning = "ObservationClockReset: the CPU observation clock moved backwards; producer evidence was reset.";

		private readonly string _name;
		private readonly IPerfMeterProfilingSamplerReadings _readings;
		private readonly int _maxObservationAgeFrames;
		private bool _enabled;
		private bool _disposed;
		private int _enableFrame;
		private int _lastFrame;
		private int _producerFrame = -1;
		private PerfMeterGpuPassAvailability _availability;
		private int _firstCallbackFrame = -1;
		private int _lastCallbackFrame = -1;
		private int _lastReadFrame = -1;
		private bool _readSucceeded;
		private bool _hasBaseline;
		private bool _changedThisFrame;
		private double _elapsedMs;
		private int _sampleCount;

		public PerfMeterProfilingSamplerMetricProvider(
			string id,
			string name,
			ProfilingSampler sampler,
			int maxObservationAgeFrames = 8)
			: this(id, name, new SamplerReadings(sampler), maxObservationAgeFrames)
		{
		}

		internal PerfMeterProfilingSamplerMetricProvider(
			string id,
			string name,
			IPerfMeterProfilingSamplerReadings readings,
			int maxObservationAgeFrames = 8)
		{
			if (string.IsNullOrWhiteSpace(id))
				throw new ArgumentException("A stable custom metric ID is required.", nameof(id));
			if (readings == null)
				throw new ArgumentNullException(nameof(readings));
			if (maxObservationAgeFrames < 1)
				throw new ArgumentOutOfRangeException(nameof(maxObservationAgeFrames));
			Id = id;
			_name = string.IsNullOrEmpty(name) ? id : name;
			_readings = readings;
			_maxObservationAgeFrames = maxObservationAgeFrames;
		}

		public string Id { get; }
		public bool IsEnabled => _enabled && !_disposed;

		/// <summary>CPU frame when a qualified change was observed; never the measured GPU frame. -1 means none.</summary>
		public int LastObservationFrame { get; private set; } = -1;

		/// <summary>Count from the most recent successful read, not an attributed camera/pass-instance count.</summary>
		public int ObservedGpuSampleCount => _readSucceeded ? _sampleCount : 0;

		/// <summary>
		/// Starts/stops this provider's observation epoch only. Enable after the recording owner enables the sampler;
		/// disable before that owner stops recording. Re-enabling takes a new baseline and requires new producer evidence.
		/// </summary>
		public void SetEnabled(bool enabled)
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(PerfMeterProfilingSamplerMetricProvider));
			if (_enabled == enabled)
				return;
			_enabled = enabled;
			ResetEpoch(_readings.FrameCount);
			if (enabled)
				Observe(_enableFrame); // Baseline only; never accepted as a new measurement.
		}

		/// <summary>
		/// Report from the producer's scheduling/gating path, including explicit negative decisions.
		/// Reports aggregate across cameras in one CPU frame: Scheduled wins over a skipped/unavailable camera.
		/// </summary>
		public void ReportProducerState(PerfMeterGpuPassAvailability availability)
		{
			if (availability < PerfMeterGpuPassAvailability.Unknown || availability > PerfMeterGpuPassAvailability.Scheduled)
				throw new ArgumentOutOfRangeException(nameof(availability));
			if (!IsEnabled)
				return;
			int frame = _readings.FrameCount;
			CheckClock(frame);
			if (_producerFrame != frame)
			{
				_producerFrame = frame;
				_availability = availability;
			}
			else if (AvailabilityPriority(availability) > AvailabilityPriority(_availability))
			{
				_availability = availability;
			}
		}

		/// <summary>
		/// Call inside the Render Graph render function when it emits the measured workload.
		/// This is CPU evidence that the callback ran, not evidence that the GPU completed that frame.
		/// </summary>
		public void ReportPassExecution()
		{
			if (!IsEnabled)
				return;
			ReportProducerState(PerfMeterGpuPassAvailability.Scheduled);
			int frame = _readings.FrameCount;
			if (_lastCallbackFrame < 0 || Age(frame, _lastCallbackFrame) > _maxObservationAgeFrames)
			{
				_firstCallbackFrame = frame;
				LastObservationFrame = -1;
				Observe(frame); // Baseline after an idle/stale callback gap; do not revive a retained GPU result.
			}
			_lastCallbackFrame = frame;
		}

		/// <summary>Always reports the metric, including explicit unavailable states, to the existing custom registry.</summary>
		public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
		{
			if (_disposed)
				return Unavailable(DisposedWarning, out metric);
			if (!_enabled)
				return Unavailable(DisabledWarning, out metric);

			int frame = _readings.FrameCount;
			if (CheckClock(frame))
			{
				Observe(frame);
				return Unavailable(ClockResetWarning, out metric);
			}
			Observe(frame); // One native read per CPU frame, even while producer evidence is unavailable.

			if (_producerFrame < 0 || _availability == PerfMeterGpuPassAvailability.Unknown)
				return InvalidateProducer(UnknownWarning, out metric);
			if (Age(frame, _producerFrame) > _maxObservationAgeFrames)
				return InvalidateProducer(ProducerStaleWarning, out metric);
			if (_availability == PerfMeterGpuPassAvailability.Unavailable)
				return InvalidateProducer(UnavailableWarning, out metric);
			if (_availability == PerfMeterGpuPassAvailability.NotScheduled)
				return InvalidateProducer(NotScheduledWarning, out metric);
			if (_firstCallbackFrame < 0)
				return Unavailable(ExecutionMissingWarning, out metric);
			if (Age(frame, _lastCallbackFrame) > _maxObservationAgeFrames)
				return InvalidateProducer(ExecutionStaleWarning, out metric);
			if (!_readSucceeded)
			{
				LastObservationFrame = -1;
				return Unavailable(ReadFailedWarning, out metric);
			}
			if (_sampleCount < 0 || double.IsNaN(_elapsedMs) || double.IsInfinity(_elapsedMs) || _elapsedMs < 0d)
			{
				LastObservationFrame = -1;
				return Unavailable(InvalidWarning, out metric);
			}
			if (_sampleCount == 0)
			{
				LastObservationFrame = -1;
				return Unavailable(NoSamplesWarning, out metric);
			}

			// The sampler exposes no result sequence or GPU frame. A value/count transition is only evidence of
			// an observation change. Identical real measurements therefore eventually fail closed as stale.
			if (_changedThisFrame && frame > _enableFrame && frame > _firstCallbackFrame)
				LastObservationFrame = frame;
			if (LastObservationFrame < 0)
				return Unavailable(UnchangedWarning, out metric);
			if (Age(frame, LastObservationFrame) > _maxObservationAgeFrames)
				return Unavailable(ObservationStaleWarning, out metric);

			metric = new PerfMeterCustomMetricSnapshot(Id, _name, "GPU", "ms", _elapsedMs, true, DelayedWarning);
			return true; // A sampled zero is available; a zero count is not.
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_enabled = false;
			_disposed = true;
			ResetEpoch(_readings.FrameCount);
			// No recording toggle, sampler disposal, or implicit registry mutation: all are caller-owned.
		}

		private bool Unavailable(string warning, out PerfMeterCustomMetricSnapshot metric)
		{
			metric = new PerfMeterCustomMetricSnapshot(Id, _name, "GPU", "ms", 0d, false, warning);
			return true;
		}

		private bool InvalidateProducer(string warning, out PerfMeterCustomMetricSnapshot metric)
		{
			LastObservationFrame = -1;
			_firstCallbackFrame = -1;
			_lastCallbackFrame = -1;
			return Unavailable(warning, out metric);
		}

		private void Observe(int frame)
		{
			if (_lastReadFrame == frame)
				return;
			_lastReadFrame = frame;
			_changedThisFrame = false;
			try
			{
				_readings.Read(out int count, out double elapsedMs);
				_changedThisFrame = _hasBaseline && (count != _sampleCount || elapsedMs != _elapsedMs);
				_sampleCount = count;
				_elapsedMs = elapsedMs;
				_hasBaseline = true;
				_readSucceeded = true;
			}
			catch
			{
				_readSucceeded = false;
				_hasBaseline = false;
			}
		}

		private bool CheckClock(int frame)
		{
			bool reset = frame < _lastFrame;
			if (reset)
				ResetEpoch(frame);
			_lastFrame = frame;
			return reset;
		}

		private void ResetEpoch(int frame)
		{
			_enableFrame = frame;
			_lastFrame = frame;
			_producerFrame = -1;
			_availability = PerfMeterGpuPassAvailability.Unknown;
			_firstCallbackFrame = -1;
			_lastCallbackFrame = -1;
			_lastReadFrame = -1;
			_readSucceeded = false;
			_hasBaseline = false;
			_changedThisFrame = false;
			_sampleCount = 0;
			_elapsedMs = 0d;
			LastObservationFrame = -1;
		}

		private static long Age(int frame, int previousFrame) => (long)frame - previousFrame;

		private static int AvailabilityPriority(PerfMeterGpuPassAvailability availability)
		{
			// One unavailable/skipped camera must not erase positive evidence from another camera.
			return availability == PerfMeterGpuPassAvailability.Scheduled ? 3 :
				availability == PerfMeterGpuPassAvailability.Unavailable ? 2 :
				availability == PerfMeterGpuPassAvailability.NotScheduled ? 1 : 0;
		}

		private sealed class SamplerReadings : IPerfMeterProfilingSamplerReadings
		{
			private readonly ProfilingSampler _sampler;

			internal SamplerReadings(ProfilingSampler sampler)
			{
				_sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
			}

			public int FrameCount => Time.frameCount;

			public void Read(out int sampleCount, out double elapsedMs)
			{
				sampleCount = _sampler.gpuSampleCount;
				elapsedMs = _sampler.gpuElapsedTime;
			}
		}
	}

	// Deterministic, allocation-free seam; no recording/lifetime mutation is available through it.
	internal interface IPerfMeterProfilingSamplerReadings
	{
		int FrameCount { get; }
		void Read(out int sampleCount, out double elapsedMs);
	}
}
