using System;

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterBottleneckStabilizer
	{
		internal const int DefaultWindowSize = 12;
		internal const int DefaultMinimumEvidenceSamples = 5;
		internal const float DefaultInitialConfidence = 0.6f;
		internal const float DefaultSwitchConfidence = 0.7f;
		internal const double DefaultStaleAfterSeconds = 1d;

		private readonly PerfMeterBottleneck[] _samples;
		private readonly int[] _counts = new int[(int)PerfMeterBottleneck.PresentLimited + 1];
		private readonly int _minimumEvidenceSamples;
		private readonly float _initialConfidence;
		private readonly float _switchConfidence;
		private readonly double _staleAfterSeconds;
		private int _nextSampleIndex;
		private int _sampleCount;
		private int _validEvidenceSampleCount;
		private PerfMeterBottleneck _instantaneousBottleneck;
		private PerfMeterBottleneck _stableBottleneck;
		private PerfMeterDiagnosticFlags _latestFlags;
		private int _lastEvidenceFrame = -1;
		private double _lastEvidenceTimeSeconds;
		private string _rawWarning = string.Empty;

		internal PerfMeterBottleneckStabilizer(
			int windowSize = DefaultWindowSize,
			int minimumEvidenceSamples = DefaultMinimumEvidenceSamples,
			float initialConfidence = DefaultInitialConfidence,
			float switchConfidence = DefaultSwitchConfidence,
			double staleAfterSeconds = DefaultStaleAfterSeconds)
		{
			int normalizedWindowSize = Math.Max(1, windowSize);
			_samples = new PerfMeterBottleneck[normalizedWindowSize];
			_minimumEvidenceSamples = Math.Min(normalizedWindowSize, Math.Max(1, minimumEvidenceSamples));
			_initialConfidence = Clamp01(initialConfidence);
			_switchConfidence = Math.Max(_initialConfidence, Clamp01(switchConfidence));
			_staleAfterSeconds = Math.Max(0d, staleAfterSeconds);
		}

		internal void Reset()
		{
			Array.Clear(_samples, 0, _samples.Length);
			Array.Clear(_counts, 0, _counts.Length);
			_nextSampleIndex = 0;
			_sampleCount = 0;
			_validEvidenceSampleCount = 0;
			_instantaneousBottleneck = PerfMeterBottleneck.Unknown;
			_stableBottleneck = PerfMeterBottleneck.Unknown;
			_latestFlags = PerfMeterDiagnosticFlags.None;
			_lastEvidenceFrame = -1;
			_lastEvidenceTimeSeconds = 0d;
			_rawWarning = string.Empty;
		}

		internal void AddSample(
			int collectionFrame,
			double sampleTimeSeconds,
			PerfMeterBottleneck instantaneousBottleneck,
			bool evidenceAvailable,
			PerfMeterDiagnosticFlags flags,
			string rawWarning)
		{
			_instantaneousBottleneck = instantaneousBottleneck;
			_latestFlags = flags;
			_rawWarning = rawWarning ?? string.Empty;

			PerfMeterBottleneck evidence = evidenceAvailable && IsKnownBottleneck(instantaneousBottleneck)
				? instantaneousBottleneck
				: PerfMeterBottleneck.Unknown;
			if (_sampleCount == _samples.Length)
			{
				RemoveSample(_samples[_nextSampleIndex]);
			}
			else
			{
				_sampleCount++;
			}

			_samples[_nextSampleIndex] = evidence;
			_nextSampleIndex = (_nextSampleIndex + 1) % _samples.Length;
			AddEvidence(evidence);

			if (evidence != PerfMeterBottleneck.Unknown)
			{
				_lastEvidenceFrame = collectionFrame;
				_lastEvidenceTimeSeconds = SanitizeTime(sampleTimeSeconds);
			}

			UpdateStableBottleneck();
		}

		internal PerfMeterDiagnosticsSnapshot GetSnapshot(double nowSeconds)
		{
			double ageSeconds = _lastEvidenceFrame >= 0
				? Math.Max(0d, SanitizeTime(nowSeconds) - _lastEvidenceTimeSeconds)
				: 0d;
			PerfMeterDiagnosticEvidenceFreshness freshness = _lastEvidenceFrame < 0
				? PerfMeterDiagnosticEvidenceFreshness.Unknown
				: ageSeconds > _staleAfterSeconds
					? PerfMeterDiagnosticEvidenceFreshness.Stale
					: PerfMeterDiagnosticEvidenceFreshness.Fresh;
			PerfMeterDiagnosticFlags flags = _latestFlags;
			bool hasContradictingEvidence = CountDistinctEvidence() > 1;
			if (hasContradictingEvidence)
			{
				flags |= PerfMeterDiagnosticFlags.ContradictingEvidence;
			}

			PerfMeterBottleneck publishedStableBottleneck = _stableBottleneck;
			PerfMeterAvailability availability = PerfMeterAvailability.Available;
			if (_validEvidenceSampleCount < _minimumEvidenceSamples || publishedStableBottleneck == PerfMeterBottleneck.Unknown)
			{
				flags |= PerfMeterDiagnosticFlags.InsufficientEvidence;
				publishedStableBottleneck = PerfMeterBottleneck.Unknown;
				availability = PerfMeterAvailability.Unknown;
			}

			if (freshness == PerfMeterDiagnosticEvidenceFreshness.Stale)
			{
				flags |= PerfMeterDiagnosticFlags.StaleEvidence;
				publishedStableBottleneck = PerfMeterBottleneck.Unknown;
				availability = PerfMeterAvailability.Unknown;
			}

			float coverage = _sampleCount > 0 ? (float)_validEvidenceSampleCount / _sampleCount : 0f;
			float confidence = publishedStableBottleneck != PerfMeterBottleneck.Unknown && _validEvidenceSampleCount > 0
				? (float)_counts[(int)publishedStableBottleneck] / _validEvidenceSampleCount
				: 0f;
			PerfMeterDiagnosticVerificationSteps verificationSteps = CreateVerificationSteps(flags, publishedStableBottleneck);

			return new PerfMeterDiagnosticsSnapshot(
				availability,
				freshness,
				_lastEvidenceFrame >= 0 ? PerfMeterDiagnosticProvenance.FrameTimingManager : PerfMeterDiagnosticProvenance.Unknown,
				_instantaneousBottleneck,
				publishedStableBottleneck,
				flags,
				verificationSteps,
				confidence,
				coverage,
				_sampleCount,
				_validEvidenceSampleCount,
				_lastEvidenceFrame,
				_lastEvidenceTimeSeconds,
				ageSeconds,
				_rawWarning);
		}

		private void UpdateStableBottleneck()
		{
			if (_validEvidenceSampleCount < _minimumEvidenceSamples)
			{
				_stableBottleneck = PerfMeterBottleneck.Unknown;
				return;
			}

			PerfMeterBottleneck candidate = GetDominantBottleneck();
			float candidateConfidence = (float)_counts[(int)candidate] / _validEvidenceSampleCount;
			if (_stableBottleneck == PerfMeterBottleneck.Unknown)
			{
				if (candidateConfidence >= _initialConfidence)
				{
					_stableBottleneck = candidate;
				}
				return;
			}

			if (_counts[(int)_stableBottleneck] == 0)
			{
				_stableBottleneck = candidateConfidence >= _initialConfidence ? candidate : PerfMeterBottleneck.Unknown;
				return;
			}

			if (candidate != _stableBottleneck && candidateConfidence >= _switchConfidence)
			{
				_stableBottleneck = candidate;
			}
		}

		private PerfMeterBottleneck GetDominantBottleneck()
		{
			PerfMeterBottleneck candidate = PerfMeterBottleneck.Balanced;
			int candidateCount = -1;
			for (int index = (int)PerfMeterBottleneck.Balanced; index <= (int)PerfMeterBottleneck.PresentLimited; index++)
			{
				int count = _counts[index];
				if (count > candidateCount || (count == candidateCount && index == (int)_stableBottleneck))
				{
					candidate = (PerfMeterBottleneck)index;
					candidateCount = count;
				}
			}
			return candidate;
		}

		private void AddEvidence(PerfMeterBottleneck bottleneck)
		{
			if (IsKnownBottleneck(bottleneck))
			{
				_counts[(int)bottleneck]++;
				_validEvidenceSampleCount++;
			}
		}

		private void RemoveSample(PerfMeterBottleneck bottleneck)
		{
			if (IsKnownBottleneck(bottleneck))
			{
				_counts[(int)bottleneck]--;
				_validEvidenceSampleCount--;
			}
		}

		private int CountDistinctEvidence()
		{
			int distinct = 0;
			for (int index = (int)PerfMeterBottleneck.Balanced; index <= (int)PerfMeterBottleneck.PresentLimited; index++)
			{
				if (_counts[index] > 0)
				{
					distinct++;
				}
			}
			return distinct;
		}

		private static PerfMeterDiagnosticVerificationSteps CreateVerificationSteps(
			PerfMeterDiagnosticFlags flags,
			PerfMeterBottleneck stableBottleneck)
		{
			PerfMeterDiagnosticVerificationSteps steps = PerfMeterDiagnosticVerificationSteps.ValidateOnTargetDevice;
			if ((flags & (PerfMeterDiagnosticFlags.InsufficientEvidence | PerfMeterDiagnosticFlags.StaleEvidence)) != 0)
			{
				steps |= PerfMeterDiagnosticVerificationSteps.CollectMoreFrameTimingSamples;
			}
			if ((flags & (PerfMeterDiagnosticFlags.FrameTimingNotCollected | PerfMeterDiagnosticFlags.FrameTimingUnavailable | PerfMeterDiagnosticFlags.InvalidFrameTimingSample | PerfMeterDiagnosticFlags.GpuTimingUnavailable)) != 0)
			{
				steps |= PerfMeterDiagnosticVerificationSteps.EnableFrameTimingStats;
			}
			if ((flags & PerfMeterDiagnosticFlags.OpenGlGpuTiming) != 0)
			{
				steps |= PerfMeterDiagnosticVerificationSteps.UseSupportedGraphicsApi;
			}
			if ((flags & PerfMeterDiagnosticFlags.ContradictingEvidence) != 0)
			{
				steps |= PerfMeterDiagnosticVerificationSteps.CompareCpuAndGpuProfilerMarkers;
			}
			if (stableBottleneck == PerfMeterBottleneck.PresentLimited)
			{
				steps |= PerfMeterDiagnosticVerificationSteps.InspectPresentPacing;
			}
			return steps;
		}

		private static bool IsKnownBottleneck(PerfMeterBottleneck bottleneck)
		{
			return bottleneck >= PerfMeterBottleneck.Balanced && bottleneck <= PerfMeterBottleneck.PresentLimited;
		}

		private static float Clamp01(float value)
		{
			return value < 0f ? 0f : value > 1f ? 1f : value;
		}

		private static double SanitizeTime(double value)
		{
			return double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, value);
		}
	}
}
