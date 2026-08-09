using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGG.PerfMeter
{
	public enum PerfMeterSessionTimelineKind
	{
		Unknown = 0,
		Valid = 1,
		Missing = 2,
		CaptureBoundary = 3
	}

	public enum PerfMeterSessionTimelineStream
	{
		Unknown = 0,
		Baseline = 1,
		Capture = 2
	}

	[Flags]
	public enum PerfMeterSessionTimelineReasonFlags
	{
		None = 0,
		InvalidTiming = 1 << 0,
		ApplicationUnfocused = 1 << 1,
		ApplicationPaused = 1 << 2,
		FrameTimingUnavailable = 1 << 3,
		CaptureFrameMissing = 1 << 4,
		SampleBufferFull = 1 << 5,
		Warmup = 1 << 6,
		SceneLoadIgnore = 1 << 7,
		Unknown = 1 << 30
	}

	public enum PerfMeterSessionTimelineTimingState
	{
		Unknown = 0,
		Valid = 1,
		Missing = 2
	}

	public enum PerfMeterSessionTimelineCaptureBoundary
	{
		Unknown = 0,
		Begin = 1,
		End = 2
	}

	public enum PerfMeterSessionTimelineCapturePhase
	{
		Unknown = 0,
		PreRoll = 1,
		Capturing = 2,
		PostRoll = 3,
		Completed = 4,
		Canceled = 5,
		Unavailable = 6,
		Error = 7
	}

	public enum PerfMeterTimelineCompatibilityStatus
	{
		Unknown = 0,
		Current = 1,
		LegacyUnknown = 2,
		Invalid = 3
	}

	public readonly struct PerfMeterMeasurementProvenanceSnapshot
	{
		public PerfMeterMeasurementProvenanceSnapshot(
			string measurementKind,
			string collectors,
			string perturbation,
			string metricSet,
			string frequency,
			string clockState,
			string administrativeState,
			string vSyncState)
		{
			MeasurementKind = measurementKind ?? string.Empty;
			Collectors = collectors ?? string.Empty;
			Perturbation = perturbation ?? string.Empty;
			MetricSet = metricSet ?? string.Empty;
			Frequency = frequency ?? string.Empty;
			ClockState = clockState ?? string.Empty;
			AdministrativeState = administrativeState ?? string.Empty;
			VSyncState = vSyncState ?? string.Empty;
		}

		public static PerfMeterMeasurementProvenanceSnapshot Unknown => new PerfMeterMeasurementProvenanceSnapshot(
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty);

		public string MeasurementKind { get; }
		public string Collectors { get; }
		public string Perturbation { get; }
		public string MetricSet { get; }
		public string Frequency { get; }
		public string ClockState { get; }
		public string AdministrativeState { get; }
		public string VSyncState { get; }
	}

	public readonly struct PerfMeterSessionTimelineEventSnapshot
	{
		public PerfMeterSessionTimelineEventSnapshot(
			PerfMeterSessionTimelineKind kind,
			PerfMeterSessionTimelineStream stream,
			PerfMeterSessionTimelineReasonFlags reason,
			PerfMeterSessionTimelineTimingState timingState,
			int firstFrame,
			int lastFrame,
			int frameCount,
			double firstTimeSeconds,
			double lastTimeSeconds,
			int referenceIndex = -1,
			string captureId = "",
			string bundleId = "",
			int captureFrameOrdinal = 0,
			int requestedCaptureFrameCount = 0,
			PerfMeterSessionTimelineCaptureBoundary captureBoundary = PerfMeterSessionTimelineCaptureBoundary.Unknown,
			PerfMeterSessionTimelineCapturePhase capturePhase = PerfMeterSessionTimelineCapturePhase.Unknown,
			PerfMeterMeasurementProvenanceSnapshot provenance = default)
		{
			Kind = NormalizeKind(kind);
			Stream = NormalizeStream(stream);
			Reason = NormalizeReason(reason);
			TimingState = NormalizeTimingState(timingState);
			FirstFrame = firstFrame;
			LastFrame = lastFrame < firstFrame ? firstFrame : lastFrame;
			FrameCount = Math.Max(1, frameCount);
			FirstTimeSeconds = firstTimeSeconds;
			LastTimeSeconds = lastTimeSeconds;
			ReferenceIndex = referenceIndex < 0 ? -1 : referenceIndex;
			CaptureId = captureId ?? string.Empty;
			BundleId = bundleId ?? string.Empty;
			CaptureFrameOrdinal = Math.Max(0, captureFrameOrdinal);
			RequestedCaptureFrameCount = Math.Max(0, requestedCaptureFrameCount);
			CaptureBoundary = NormalizeBoundary(captureBoundary);
			CapturePhase = NormalizePhase(capturePhase);
			Provenance = provenance;
		}

		public PerfMeterSessionTimelineKind Kind { get; }
		public PerfMeterSessionTimelineStream Stream { get; }
		public PerfMeterSessionTimelineReasonFlags Reason { get; }
		public PerfMeterSessionTimelineTimingState TimingState { get; }
		public int FirstFrame { get; }
		public int LastFrame { get; }
		public int FrameCount { get; }
		public double FirstTimeSeconds { get; }
		public double LastTimeSeconds { get; }
		public int ReferenceIndex { get; }
		public int SampleIndex => Stream == PerfMeterSessionTimelineStream.Baseline ? ReferenceIndex : -1;
		public int CaptureSampleIndex => Stream == PerfMeterSessionTimelineStream.Capture ? ReferenceIndex : -1;
		public string CaptureId { get; }
		public string BundleId { get; }
		public int CaptureFrameOrdinal { get; }
		public int RequestedCaptureFrameCount { get; }
		public int CaptureFrameEndOrdinal => CaptureFrameOrdinal <= 0 ? 0 : CaptureFrameOrdinal + FrameCount - 1;
		public PerfMeterSessionTimelineCaptureBoundary CaptureBoundary { get; }
		public PerfMeterSessionTimelineCapturePhase CapturePhase { get; }
		public PerfMeterMeasurementProvenanceSnapshot Provenance { get; }

		private static PerfMeterSessionTimelineKind NormalizeKind(PerfMeterSessionTimelineKind value)
		{
			return value == PerfMeterSessionTimelineKind.Valid || value == PerfMeterSessionTimelineKind.Missing || value == PerfMeterSessionTimelineKind.CaptureBoundary
				? value
				: PerfMeterSessionTimelineKind.Unknown;
		}

		private static PerfMeterSessionTimelineStream NormalizeStream(PerfMeterSessionTimelineStream value)
		{
			return value == PerfMeterSessionTimelineStream.Baseline || value == PerfMeterSessionTimelineStream.Capture
				? value
				: PerfMeterSessionTimelineStream.Unknown;
		}

		private static PerfMeterSessionTimelineReasonFlags NormalizeReason(PerfMeterSessionTimelineReasonFlags value)
		{
			const PerfMeterSessionTimelineReasonFlags known =
				PerfMeterSessionTimelineReasonFlags.InvalidTiming |
				PerfMeterSessionTimelineReasonFlags.ApplicationUnfocused |
				PerfMeterSessionTimelineReasonFlags.ApplicationPaused |
				PerfMeterSessionTimelineReasonFlags.FrameTimingUnavailable |
				PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing |
				PerfMeterSessionTimelineReasonFlags.SampleBufferFull |
				PerfMeterSessionTimelineReasonFlags.Warmup |
				PerfMeterSessionTimelineReasonFlags.SceneLoadIgnore;
			PerfMeterSessionTimelineReasonFlags unknown = value & ~known;
			return (value & known) | (unknown != 0 ? PerfMeterSessionTimelineReasonFlags.Unknown : PerfMeterSessionTimelineReasonFlags.None);
		}

		private static PerfMeterSessionTimelineTimingState NormalizeTimingState(PerfMeterSessionTimelineTimingState value)
		{
			return value == PerfMeterSessionTimelineTimingState.Valid || value == PerfMeterSessionTimelineTimingState.Missing
				? value
				: PerfMeterSessionTimelineTimingState.Unknown;
		}

		private static PerfMeterSessionTimelineCaptureBoundary NormalizeBoundary(PerfMeterSessionTimelineCaptureBoundary value)
		{
			return value == PerfMeterSessionTimelineCaptureBoundary.Begin || value == PerfMeterSessionTimelineCaptureBoundary.End
				? value
				: PerfMeterSessionTimelineCaptureBoundary.Unknown;
		}

		private static PerfMeterSessionTimelineCapturePhase NormalizePhase(PerfMeterSessionTimelineCapturePhase value)
		{
			return value >= PerfMeterSessionTimelineCapturePhase.Unknown && value <= PerfMeterSessionTimelineCapturePhase.Error
				? value
				: PerfMeterSessionTimelineCapturePhase.Unknown;
		}

	}

	public readonly struct PerfMeterSessionTimelineSnapshot
	{
		public const int CurrentSchemaVersion = 1;

		internal PerfMeterSessionTimelineSnapshot(PerfMeterSessionTimelineEventSnapshot[] events, int droppedEventCount, bool isComplete)
		{
			if (events == null || events.Length == 0)
			{
				Events = Array.Empty<PerfMeterSessionTimelineEventSnapshot>();
			}
			else
			{
				Events = new PerfMeterSessionTimelineEventSnapshot[events.Length];
				Array.Copy(events, Events, events.Length);
			}

			DroppedEventCount = Math.Max(0, droppedEventCount);
			IsComplete = isComplete && DroppedEventCount == 0;
		}

		public static PerfMeterSessionTimelineSnapshot Empty => new PerfMeterSessionTimelineSnapshot(Array.Empty<PerfMeterSessionTimelineEventSnapshot>(), 0, true);

		public int TimelineSchemaVersion => CurrentSchemaVersion;
		public bool IsComplete { get; }
		public int EventCount => Events == null ? 0 : Events.Length;
		public int DroppedEventCount { get; }
		public PerfMeterSessionTimelineEventSnapshot[] Events { get; }
	}

	public readonly struct PerfMeterTimelineCompatibilitySnapshot
	{
		internal PerfMeterTimelineCompatibilitySnapshot(
			PerfMeterTimelineCompatibilityStatus status,
			int timelineSchemaVersion,
			int eventCount,
			int unknownEnumCount,
			string warning,
			PerfMeterSessionTimelineSnapshot timeline)
		{
			Status = status;
			TimelineSchemaVersion = Math.Max(0, timelineSchemaVersion);
			EventCount = Math.Max(0, eventCount);
			UnknownEnumCount = Math.Max(0, unknownEnumCount);
			Warning = warning ?? string.Empty;
			Timeline = timeline;
		}

		public PerfMeterTimelineCompatibilityStatus Status { get; }
		public int TimelineSchemaVersion { get; }
		public int EventCount { get; }
		public int UnknownEnumCount { get; }
		public string Warning { get; }
		public PerfMeterSessionTimelineSnapshot Timeline { get; }
		public bool IsAccepted => Status != PerfMeterTimelineCompatibilityStatus.Invalid;
		public bool IsLegacyUnknown => Status == PerfMeterTimelineCompatibilityStatus.LegacyUnknown;
	}

	public static class PerfMeterTimelineCompatibilityReader
	{
		public static PerfMeterTimelineCompatibilitySnapshot Read(string payload)
		{
			if (string.IsNullOrWhiteSpace(payload))
			{
				return new PerfMeterTimelineCompatibilitySnapshot(
					PerfMeterTimelineCompatibilityStatus.LegacyUnknown,
					0,
					0,
					0,
					"Timeline fields are absent; payload is a legacy unknown format.",
					PerfMeterSessionTimelineSnapshot.Empty);
			}

			TimelineCompatibilityPayload parsed;
			try
			{
				parsed = JsonUtility.FromJson<TimelineCompatibilityPayload>(payload);
			}
			catch (Exception exception)
			{
				return new PerfMeterTimelineCompatibilitySnapshot(PerfMeterTimelineCompatibilityStatus.Invalid, 0, 0, 0, exception.GetType().Name + ": " + exception.Message, PerfMeterSessionTimelineSnapshot.Empty);
			}

			if (parsed == null)
			{
				return new PerfMeterTimelineCompatibilitySnapshot(PerfMeterTimelineCompatibilityStatus.Invalid, 0, 0, 0, "Timeline payload could not be parsed.", PerfMeterSessionTimelineSnapshot.Empty);
			}

			if (parsed.timeline_schema_version <= 0)
			{
				return new PerfMeterTimelineCompatibilitySnapshot(
					PerfMeterTimelineCompatibilityStatus.LegacyUnknown,
					0,
					0,
					0,
					"Timeline fields are absent; payload is a legacy unknown format.",
					PerfMeterSessionTimelineSnapshot.Empty);
			}

			if (parsed.timeline_schema_version > PerfMeterSessionTimelineSnapshot.CurrentSchemaVersion)
			{
				return new PerfMeterTimelineCompatibilitySnapshot(
					PerfMeterTimelineCompatibilityStatus.Unknown,
					parsed.timeline_schema_version,
					parsed.timeline == null ? 0 : parsed.timeline.Length,
					0,
					"Timeline schema version is newer than this reader.",
					PerfMeterSessionTimelineSnapshot.Empty);
			}

			int unknownEnumCount = 0;
			List<PerfMeterSessionTimelineEventSnapshot> events = new List<PerfMeterSessionTimelineEventSnapshot>();
			if (parsed.timeline != null)
			{
				for (int i = 0; i < parsed.timeline.Length; i++)
				{
					TimelineCompatibilityEvent timelineEvent = parsed.timeline[i];
					if (timelineEvent == null)
					{
						events.Add(new PerfMeterSessionTimelineEventSnapshot(
							PerfMeterSessionTimelineKind.Unknown,
							PerfMeterSessionTimelineStream.Unknown,
							PerfMeterSessionTimelineReasonFlags.Unknown,
							PerfMeterSessionTimelineTimingState.Unknown,
							0,
							0,
							1,
							0d,
							0d));
						unknownEnumCount++;
						continue;
					}
					if (ParseKind(timelineEvent.kind) == PerfMeterSessionTimelineKind.Unknown && !string.IsNullOrEmpty(timelineEvent.kind))
					{
						unknownEnumCount++;
					}

					if (ParseStream(timelineEvent.stream) == PerfMeterSessionTimelineStream.Unknown && !string.IsNullOrEmpty(timelineEvent.stream))
					{
						unknownEnumCount++;
					}

					if (ParseTimingState(timelineEvent.timing_state) == PerfMeterSessionTimelineTimingState.Unknown && !string.IsNullOrEmpty(timelineEvent.timing_state))
					{
						unknownEnumCount++;
					}

					if (ParseBoundary(timelineEvent.capture_boundary) == PerfMeterSessionTimelineCaptureBoundary.Unknown && !string.IsNullOrEmpty(timelineEvent.capture_boundary))
					{
						unknownEnumCount++;
					}

					if (ParsePhase(timelineEvent.capture_phase) == PerfMeterSessionTimelineCapturePhase.Unknown && !string.IsNullOrEmpty(timelineEvent.capture_phase))
					{
						unknownEnumCount++;
					}

					if ((ParseReason(timelineEvent.reason_flags) & PerfMeterSessionTimelineReasonFlags.Unknown) != 0 && !string.IsNullOrEmpty(timelineEvent.reason_flags))
					{
						unknownEnumCount++;
					}

					events.Add(new PerfMeterSessionTimelineEventSnapshot(
						ParseKind(timelineEvent.kind),
						ParseStream(timelineEvent.stream),
						ParseReason(timelineEvent.reason_flags),
						ParseTimingState(timelineEvent.timing_state),
						timelineEvent.first_frame,
						timelineEvent.last_frame,
						timelineEvent.frame_count,
						timelineEvent.first_time_seconds,
						timelineEvent.last_time_seconds,
						timelineEvent.reference_index,
						timelineEvent.capture_id,
						timelineEvent.bundle_id,
								timelineEvent.capture_frame_ordinal,
								timelineEvent.requested_capture_frame_count,
								ParseBoundary(timelineEvent.capture_boundary),
								ParsePhase(timelineEvent.capture_phase),
								ParseProvenance(timelineEvent.measurement_provenance)));
				}
			}

			return new PerfMeterTimelineCompatibilitySnapshot(
				PerfMeterTimelineCompatibilityStatus.Current,
				parsed.timeline_schema_version,
				parsed.timeline == null ? 0 : parsed.timeline.Length,
				unknownEnumCount,
				unknownEnumCount == 0 ? string.Empty : "Unknown timeline enum tokens were mapped to Unknown.",
				new PerfMeterSessionTimelineSnapshot(events.ToArray(), parsed.timeline_dropped_event_count, parsed.timeline_complete));
		}

		[Serializable]
		private sealed class TimelineCompatibilityPayload
		{
			public int timeline_schema_version;
			public bool timeline_complete;
			public int timeline_dropped_event_count;
			public TimelineCompatibilityEvent[] timeline;
		}

		[Serializable]
		private sealed class TimelineCompatibilityEvent
		{
			public string kind;
			public string stream;
			public string reason_flags;
			public string timing_state;
			public string capture_boundary;
			public string capture_phase;
			public int first_frame;
			public int last_frame;
			public int frame_count;
			public double first_time_seconds;
			public double last_time_seconds;
			public int reference_index;
			public string capture_id;
			public string bundle_id;
			public int capture_frame_ordinal;
			public int requested_capture_frame_count;
			public TimelineCompatibilityProvenance measurement_provenance;
		}

		[Serializable]
		private sealed class TimelineCompatibilityProvenance
		{
			public string measurement_kind;
			public string collectors;
			public string perturbation;
			public string metric_set;
			public string frequency;
			public string clock_state;
			public string administrative_state;
			public string vsync_state;
		}

		private static PerfMeterSessionTimelineKind ParseKind(string token)
		{
			return Enum.TryParse(token, true, out PerfMeterSessionTimelineKind value) && IsKnown(value) ? value : PerfMeterSessionTimelineKind.Unknown;
		}

		private static PerfMeterSessionTimelineStream ParseStream(string token)
		{
			return Enum.TryParse(token, true, out PerfMeterSessionTimelineStream value) && IsKnown(value) ? value : PerfMeterSessionTimelineStream.Unknown;
		}

		private static PerfMeterSessionTimelineTimingState ParseTimingState(string token)
		{
			return Enum.TryParse(token, true, out PerfMeterSessionTimelineTimingState value) && IsKnown(value) ? value : PerfMeterSessionTimelineTimingState.Unknown;
		}

		private static PerfMeterSessionTimelineReasonFlags ParseReason(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
			{
				return PerfMeterSessionTimelineReasonFlags.Unknown;
			}

			PerfMeterSessionTimelineReasonFlags result = PerfMeterSessionTimelineReasonFlags.None;
			string[] parts = token.Split(',');
			for (int i = 0; i < parts.Length; i++)
			{
				if (Enum.TryParse(parts[i].Trim(), true, out PerfMeterSessionTimelineReasonFlags value) && IsKnown(value))
				{
					result |= value;
				}
				else
				{
					result |= PerfMeterSessionTimelineReasonFlags.Unknown;
				}
			}

			return result;
		}

		private static PerfMeterSessionTimelineCaptureBoundary ParseBoundary(string token)
		{
			return Enum.TryParse(token, true, out PerfMeterSessionTimelineCaptureBoundary value) && IsKnown(value) ? value : PerfMeterSessionTimelineCaptureBoundary.Unknown;
		}

		private static PerfMeterSessionTimelineCapturePhase ParsePhase(string token)
		{
			return Enum.TryParse(token, true, out PerfMeterSessionTimelineCapturePhase value) && IsKnown(value) ? value : PerfMeterSessionTimelineCapturePhase.Unknown;
		}

		private static PerfMeterMeasurementProvenanceSnapshot ParseProvenance(TimelineCompatibilityProvenance provenance)
		{
			return provenance == null
				? PerfMeterMeasurementProvenanceSnapshot.Unknown
				: new PerfMeterMeasurementProvenanceSnapshot(
					provenance.measurement_kind,
					provenance.collectors,
					provenance.perturbation,
					provenance.metric_set,
					provenance.frequency,
					provenance.clock_state,
					provenance.administrative_state,
					provenance.vsync_state);
		}

		private static bool IsKnown(PerfMeterSessionTimelineKind value) => value == PerfMeterSessionTimelineKind.Valid || value == PerfMeterSessionTimelineKind.Missing || value == PerfMeterSessionTimelineKind.CaptureBoundary || value == PerfMeterSessionTimelineKind.Unknown;
		private static bool IsKnown(PerfMeterSessionTimelineStream value) => value == PerfMeterSessionTimelineStream.Baseline || value == PerfMeterSessionTimelineStream.Capture || value == PerfMeterSessionTimelineStream.Unknown;
		private static bool IsKnown(PerfMeterSessionTimelineTimingState value) => value == PerfMeterSessionTimelineTimingState.Valid || value == PerfMeterSessionTimelineTimingState.Missing || value == PerfMeterSessionTimelineTimingState.Unknown;
		private static bool IsKnown(PerfMeterSessionTimelineCaptureBoundary value) => value == PerfMeterSessionTimelineCaptureBoundary.Begin || value == PerfMeterSessionTimelineCaptureBoundary.End || value == PerfMeterSessionTimelineCaptureBoundary.Unknown;
		private static bool IsKnown(PerfMeterSessionTimelineCapturePhase value) => value >= PerfMeterSessionTimelineCapturePhase.Unknown && value <= PerfMeterSessionTimelineCapturePhase.Error;
		private static bool IsKnown(PerfMeterSessionTimelineReasonFlags value)
		{
			const PerfMeterSessionTimelineReasonFlags known =
				PerfMeterSessionTimelineReasonFlags.None |
				PerfMeterSessionTimelineReasonFlags.InvalidTiming |
				PerfMeterSessionTimelineReasonFlags.ApplicationUnfocused |
				PerfMeterSessionTimelineReasonFlags.ApplicationPaused |
				PerfMeterSessionTimelineReasonFlags.FrameTimingUnavailable |
				PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing |
				PerfMeterSessionTimelineReasonFlags.SampleBufferFull |
				PerfMeterSessionTimelineReasonFlags.Warmup |
				PerfMeterSessionTimelineReasonFlags.SceneLoadIgnore |
				PerfMeterSessionTimelineReasonFlags.Unknown;
			return (value & ~known) == 0;
		}
	}

	internal sealed class PerfMeterSessionTimelineStore
	{
		internal const int DefaultCaptureEventCapacity = 600;
		private PerfMeterSessionTimelineEventSnapshot[] _events = Array.Empty<PerfMeterSessionTimelineEventSnapshot>();
		private int _eventCount;
		private int _droppedEventCount;

		internal int EventCount => _eventCount;
		internal int DroppedEventCount => _droppedEventCount;

		internal void Start(int baselineCapacity, int captureCapacity = DefaultCaptureEventCapacity)
		{
			int capacity = Math.Max(8, Math.Max(0, baselineCapacity) + Math.Max(0, captureCapacity) + 8);
			_events = new PerfMeterSessionTimelineEventSnapshot[capacity];
			_eventCount = 0;
			_droppedEventCount = 0;
		}

		internal void Reset()
		{
			if (_eventCount > 0)
			{
				Array.Clear(_events, 0, _eventCount);
			}

			_eventCount = 0;
			_droppedEventCount = 0;
		}

		internal PerfMeterSessionTimelineSnapshot GetSnapshotCopy()
		{
			PerfMeterSessionTimelineEventSnapshot[] events = _eventCount == 0
				? Array.Empty<PerfMeterSessionTimelineEventSnapshot>()
				: new PerfMeterSessionTimelineEventSnapshot[_eventCount];
			if (_eventCount > 0)
			{
				Array.Copy(_events, events, _eventCount);
			}

			return new PerfMeterSessionTimelineSnapshot(events, _droppedEventCount, _droppedEventCount == 0);
		}

		internal void AddValidBaseline(int frame, double timeSeconds, int sampleIndex, PerfMeterMeasurementProvenanceSnapshot provenance = default)
		{
			Add(new PerfMeterSessionTimelineEventSnapshot(
				PerfMeterSessionTimelineKind.Valid,
				PerfMeterSessionTimelineStream.Baseline,
				PerfMeterSessionTimelineReasonFlags.None,
				PerfMeterSessionTimelineTimingState.Valid,
				frame,
				frame,
				1,
				timeSeconds,
				timeSeconds,
				sampleIndex,
				provenance: provenance));
		}

		internal void AddMissingBaseline(int firstFrame, int lastFrame, double firstTimeSeconds, double lastTimeSeconds, PerfMeterSessionTimelineReasonFlags reason)
		{
			Add(new PerfMeterSessionTimelineEventSnapshot(
				PerfMeterSessionTimelineKind.Missing,
				PerfMeterSessionTimelineStream.Baseline,
				reason,
				PerfMeterSessionTimelineTimingState.Missing,
				firstFrame,
				lastFrame,
				Math.Max(1, lastFrame - firstFrame + 1),
				firstTimeSeconds,
				lastTimeSeconds));
		}

		internal void AddValidCapture(int frame, double timeSeconds, string captureId, string bundleId, int captureFrameOrdinal, int requestedCaptureFrameCount, int captureSampleIndex, PerfMeterMeasurementProvenanceSnapshot provenance = default)
		{
			Add(new PerfMeterSessionTimelineEventSnapshot(
				PerfMeterSessionTimelineKind.Valid,
				PerfMeterSessionTimelineStream.Capture,
				PerfMeterSessionTimelineReasonFlags.None,
				PerfMeterSessionTimelineTimingState.Valid,
				frame,
				frame,
				1,
				timeSeconds,
				timeSeconds,
				captureSampleIndex,
				captureId,
				bundleId,
				captureFrameOrdinal,
				requestedCaptureFrameCount,
				PerfMeterSessionTimelineCaptureBoundary.Unknown,
				PerfMeterSessionTimelineCapturePhase.Capturing,
				provenance: provenance));
		}

		internal void AddMissingCapture(int firstFrame, int lastFrame, double firstTimeSeconds, double lastTimeSeconds, string captureId, string bundleId, int captureFrameOrdinal, int requestedCaptureFrameCount, PerfMeterSessionTimelineReasonFlags reason)
		{
			Add(new PerfMeterSessionTimelineEventSnapshot(
				PerfMeterSessionTimelineKind.Missing,
				PerfMeterSessionTimelineStream.Capture,
				reason | PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing,
				PerfMeterSessionTimelineTimingState.Missing,
				firstFrame,
				lastFrame,
				Math.Max(1, lastFrame - firstFrame + 1),
				firstTimeSeconds,
				lastTimeSeconds,
				-1,
				captureId,
				bundleId,
				captureFrameOrdinal,
				requestedCaptureFrameCount,
				PerfMeterSessionTimelineCaptureBoundary.Unknown,
				PerfMeterSessionTimelineCapturePhase.Capturing));
		}

		internal void AddCaptureBoundary(int frame, double timeSeconds, string captureId, string bundleId, PerfMeterSessionTimelineCaptureBoundary boundary, PerfMeterSessionTimelineCapturePhase phase, int requestedCaptureFrameCount, PerfMeterSessionTimelineReasonFlags reason = PerfMeterSessionTimelineReasonFlags.None)
		{
			Add(new PerfMeterSessionTimelineEventSnapshot(
				PerfMeterSessionTimelineKind.CaptureBoundary,
				PerfMeterSessionTimelineStream.Capture,
				reason,
				PerfMeterSessionTimelineTimingState.Valid,
				frame,
				frame,
				1,
				timeSeconds,
				timeSeconds,
				-1,
				captureId,
				bundleId,
				0,
				requestedCaptureFrameCount,
				boundary,
				phase));
		}

		private void Add(PerfMeterSessionTimelineEventSnapshot timelineEvent)
		{
			if (_eventCount > 0 && CanCoalesce(_events[_eventCount - 1], timelineEvent))
			{
				_events[_eventCount - 1] = Merge(_events[_eventCount - 1], timelineEvent);
				return;
			}

			if (_eventCount >= _events.Length)
			{
				_droppedEventCount++;
				return;
			}

			_events[_eventCount++] = timelineEvent;
		}

		private static bool CanCoalesce(PerfMeterSessionTimelineEventSnapshot first, PerfMeterSessionTimelineEventSnapshot second)
		{
			return first.Kind == PerfMeterSessionTimelineKind.Missing &&
				second.Kind == PerfMeterSessionTimelineKind.Missing &&
				first.Stream == second.Stream &&
				first.Reason == second.Reason &&
				first.TimingState == second.TimingState &&
				string.Equals(first.CaptureId, second.CaptureId, StringComparison.Ordinal) &&
				string.Equals(first.BundleId, second.BundleId, StringComparison.Ordinal) &&
				first.CapturePhase == second.CapturePhase &&
				first.LastFrame + 1 == second.FirstFrame &&
				(first.Stream != PerfMeterSessionTimelineStream.Capture || first.CaptureFrameOrdinal + first.FrameCount == second.CaptureFrameOrdinal);
		}

		private static PerfMeterSessionTimelineEventSnapshot Merge(PerfMeterSessionTimelineEventSnapshot first, PerfMeterSessionTimelineEventSnapshot second)
		{
			return new PerfMeterSessionTimelineEventSnapshot(
				first.Kind,
				first.Stream,
				first.Reason,
				first.TimingState,
				first.FirstFrame,
				second.LastFrame,
				first.FrameCount + second.FrameCount,
				first.FirstTimeSeconds,
				second.LastTimeSeconds,
				-1,
				first.CaptureId,
				first.BundleId,
				first.CaptureFrameOrdinal,
				first.RequestedCaptureFrameCount,
				first.CaptureBoundary,
				first.CapturePhase,
				first.Provenance);
		}

		private static PerfMeterSessionTimelineKind NormalizeKind(PerfMeterSessionTimelineKind value)
		{
			return value == PerfMeterSessionTimelineKind.Valid || value == PerfMeterSessionTimelineKind.Missing || value == PerfMeterSessionTimelineKind.CaptureBoundary
				? value
				: PerfMeterSessionTimelineKind.Unknown;
		}

		private static PerfMeterSessionTimelineStream NormalizeStream(PerfMeterSessionTimelineStream value)
		{
			return value == PerfMeterSessionTimelineStream.Baseline || value == PerfMeterSessionTimelineStream.Capture
				? value
				: PerfMeterSessionTimelineStream.Unknown;
		}

		private static PerfMeterSessionTimelineReasonFlags NormalizeReason(PerfMeterSessionTimelineReasonFlags value)
		{
			const PerfMeterSessionTimelineReasonFlags known =
				PerfMeterSessionTimelineReasonFlags.InvalidTiming |
				PerfMeterSessionTimelineReasonFlags.ApplicationUnfocused |
				PerfMeterSessionTimelineReasonFlags.ApplicationPaused |
				PerfMeterSessionTimelineReasonFlags.FrameTimingUnavailable |
				PerfMeterSessionTimelineReasonFlags.CaptureFrameMissing |
				PerfMeterSessionTimelineReasonFlags.SampleBufferFull |
				PerfMeterSessionTimelineReasonFlags.Warmup |
				PerfMeterSessionTimelineReasonFlags.SceneLoadIgnore;
			PerfMeterSessionTimelineReasonFlags unknown = value & ~known;
			return (value & known) | (unknown != 0 ? PerfMeterSessionTimelineReasonFlags.Unknown : PerfMeterSessionTimelineReasonFlags.None);
		}

		private static PerfMeterSessionTimelineTimingState NormalizeTimingState(PerfMeterSessionTimelineTimingState value)
		{
			return value == PerfMeterSessionTimelineTimingState.Valid || value == PerfMeterSessionTimelineTimingState.Missing
				? value
				: PerfMeterSessionTimelineTimingState.Unknown;
		}

		private static PerfMeterSessionTimelineCaptureBoundary NormalizeBoundary(PerfMeterSessionTimelineCaptureBoundary value)
		{
			return value == PerfMeterSessionTimelineCaptureBoundary.Begin || value == PerfMeterSessionTimelineCaptureBoundary.End
				? value
				: PerfMeterSessionTimelineCaptureBoundary.Unknown;
		}

		private static PerfMeterSessionTimelineCapturePhase NormalizePhase(PerfMeterSessionTimelineCapturePhase value)
		{
			return value >= PerfMeterSessionTimelineCapturePhase.Unknown && value <= PerfMeterSessionTimelineCapturePhase.Error
				? value
				: PerfMeterSessionTimelineCapturePhase.Unknown;
		}
	}

	internal static class PerfMeterSessionTimelineUtility
	{
		internal static PerfMeterSessionTimelineCapturePhase GetCapturePhase(PerfMeterCaptureState state)
		{
			switch (state)
			{
				case PerfMeterCaptureState.PreRoll:
					return PerfMeterSessionTimelineCapturePhase.PreRoll;
				case PerfMeterCaptureState.Capturing:
					return PerfMeterSessionTimelineCapturePhase.Capturing;
				case PerfMeterCaptureState.PostRoll:
					return PerfMeterSessionTimelineCapturePhase.PostRoll;
				case PerfMeterCaptureState.Completed:
					return PerfMeterSessionTimelineCapturePhase.Completed;
				case PerfMeterCaptureState.Canceled:
					return PerfMeterSessionTimelineCapturePhase.Canceled;
				case PerfMeterCaptureState.Unavailable:
					return PerfMeterSessionTimelineCapturePhase.Unavailable;
				case PerfMeterCaptureState.Error:
					return PerfMeterSessionTimelineCapturePhase.Error;
				default:
					return PerfMeterSessionTimelineCapturePhase.Unknown;
			}
		}
	}
}
