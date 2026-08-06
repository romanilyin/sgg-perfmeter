using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	public enum PerfMeterCaptureTool
	{
		Unknown = 0,
		RenderDoc = 1,
		Pix = 2,
		MemoryProfiler = 3
	}

	public enum PerfMeterCaptureState
	{
		Idle = 0,
		PreRoll = 1,
		Capturing = 2,
		PostRoll = 3,
		Completed = 4,
		Canceled = 5,
		Unavailable = 6,
		Error = 7
	}

	public enum PerfMeterCaptureRequestResult
	{
		Started = 0,
		AlreadyActive = 1,
		RejectedOverlap = 2,
		Unavailable = 3,
		InvalidRequest = 4,
		Failed = 5
	}

	public readonly struct PerfMeterCaptureOptions
	{
		public PerfMeterCaptureOptions(string captureId, PerfMeterCaptureTool tool, int captureFrames = 1, int preRollFrames = 0, int postRollFrames = 0)
		{
			CaptureId = captureId ?? string.Empty;
			Tool = tool;
			CaptureFrames = Mathf.Max(1, captureFrames);
			PreRollFrames = Mathf.Max(0, preRollFrames);
			PostRollFrames = Mathf.Max(0, postRollFrames);
		}

		public string CaptureId { get; }
		public PerfMeterCaptureTool Tool { get; }
		public int CaptureFrames { get; }
		public int PreRollFrames { get; }
		public int PostRollFrames { get; }
	}

	public readonly struct PerfMeterCaptureStatusSnapshot
	{
		public PerfMeterCaptureStatusSnapshot(
			PerfMeterAvailability availability,
			PerfMeterCaptureState state,
			string captureId,
			PerfMeterCaptureTool tool,
			int requestedPreRollFrames,
			int requestedCaptureFrames,
			int requestedPostRollFrames,
			int completedPreRollFrames,
			int completedCaptureFrames,
			int completedPostRollFrames,
			string warning)
		{
			Availability = availability;
			State = state;
			CaptureId = captureId ?? string.Empty;
			Tool = tool;
			RequestedPreRollFrames = Mathf.Max(0, requestedPreRollFrames);
			RequestedCaptureFrames = Mathf.Max(0, requestedCaptureFrames);
			RequestedPostRollFrames = Mathf.Max(0, requestedPostRollFrames);
			CompletedPreRollFrames = Mathf.Clamp(completedPreRollFrames, 0, RequestedPreRollFrames);
			CompletedCaptureFrames = Mathf.Clamp(completedCaptureFrames, 0, RequestedCaptureFrames);
			CompletedPostRollFrames = Mathf.Clamp(completedPostRollFrames, 0, RequestedPostRollFrames);
			Warning = warning ?? string.Empty;
		}

		public static PerfMeterCaptureStatusSnapshot NotRunning => new PerfMeterCaptureStatusSnapshot(
			PerfMeterAvailability.Unknown,
			PerfMeterCaptureState.Idle,
			string.Empty,
			PerfMeterCaptureTool.Unknown,
			0,
			0,
			0,
			0,
			0,
			0,
			"Capture coordinator is not running.");

		public bool IsActive => State == PerfMeterCaptureState.PreRoll || State == PerfMeterCaptureState.Capturing || State == PerfMeterCaptureState.PostRoll;
		public PerfMeterAvailability Availability { get; }
		public PerfMeterCaptureState State { get; }
		public string CaptureId { get; }
		public PerfMeterCaptureTool Tool { get; }
		public int RequestedPreRollFrames { get; }
		public int RequestedCaptureFrames { get; }
		public int RequestedPostRollFrames { get; }
		public int CompletedPreRollFrames { get; }
		public int CompletedCaptureFrames { get; }
		public int CompletedPostRollFrames { get; }
		public string Warning { get; }
	}

	internal readonly struct PerfMeterCaptureBackendCapability
	{
		internal PerfMeterCaptureBackendCapability(PerfMeterAvailability availability, string warning)
		{
			Availability = availability;
			Warning = warning ?? string.Empty;
		}

		internal PerfMeterAvailability Availability { get; }
		internal string Warning { get; }
	}

	internal interface IPerfMeterCaptureBackend
	{
		PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool);
		bool TryBegin(PerfMeterCaptureTool tool, out string error);
		bool TryEnd(out string error);
	}

	internal interface IPerfMeterCaptureScope
	{
		bool TryBegin(string captureId);
		bool TryEnd(string captureId);
	}

	internal sealed class PerfMeterCaptureCoordinator
	{
		private readonly IPerfMeterCaptureBackend _backend;
		private readonly IPerfMeterCaptureScope _scope;
		private PerfMeterCaptureOptions _options;
		private PerfMeterCaptureState _state;
		private PerfMeterAvailability _availability;
		private int _completedPreRollFrames;
		private int _completedCaptureFrames;
		private int _completedPostRollFrames;
		private string _warning = string.Empty;
		private bool _backendActive;
		private bool _scopeActive;

		internal PerfMeterCaptureCoordinator(IPerfMeterCaptureBackend backend, IPerfMeterCaptureScope scope)
		{
			_backend = backend ?? throw new ArgumentNullException(nameof(backend));
			_scope = scope ?? throw new ArgumentNullException(nameof(scope));
			SetState(PerfMeterCaptureState.Idle, PerfMeterAvailability.Unknown, string.Empty);
		}

		internal PerfMeterCaptureStatusSnapshot Status => new PerfMeterCaptureStatusSnapshot(
			_availability,
			_state,
			_options.CaptureId,
			_options.Tool,
			_options.PreRollFrames,
			_options.CaptureFrames,
			_options.PostRollFrames,
			_completedPreRollFrames,
			_completedCaptureFrames,
			_completedPostRollFrames,
			_warning);
		internal bool ScopeActive => _scopeActive;
		internal bool HasActiveResources => IsActiveState(_state) || _backendActive || _scopeActive;

		internal PerfMeterCaptureRequestResult Request(PerfMeterCaptureOptions options)
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				if (string.IsNullOrEmpty(options.CaptureId) || options.Tool == PerfMeterCaptureTool.Unknown)
				{
					return PerfMeterCaptureRequestResult.InvalidRequest;
				}

				if (IsActiveState(_state))
				{
					return string.Equals(_options.CaptureId, options.CaptureId, StringComparison.Ordinal)
						? PerfMeterCaptureRequestResult.AlreadyActive
						: PerfMeterCaptureRequestResult.RejectedOverlap;
				}

				if (_backendActive || _scopeActive)
				{
					return PerfMeterCaptureRequestResult.RejectedOverlap;
				}

				PerfMeterCaptureBackendCapability capability;
				try
				{
					capability = _backend.GetCapability(options.Tool);
				}
				catch (Exception exception)
				{
					SetRequest(options);
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, FormatException(exception));
					return PerfMeterCaptureRequestResult.Failed;
				}

				SetRequest(options);
				if (capability.Availability != PerfMeterAvailability.Available)
				{
					SetState(PerfMeterCaptureState.Unavailable, PerfMeterAvailability.Unavailable, capability.Warning);
					return PerfMeterCaptureRequestResult.Unavailable;
				}

				SetState(PerfMeterCaptureState.PreRoll, PerfMeterAvailability.Available, capability.Warning);
				if (_options.PreRollFrames == 0 && !TryBeginCapture())
				{
					return PerfMeterCaptureRequestResult.Failed;
				}

				return PerfMeterCaptureRequestResult.Started;
			}
		}

		internal void Tick()
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				switch (_state)
				{
					case PerfMeterCaptureState.PreRoll:
						_completedPreRollFrames++;
						if (_completedPreRollFrames >= _options.PreRollFrames)
						{
							TryBeginCapture();
						}
						break;
					case PerfMeterCaptureState.Capturing:
						_completedCaptureFrames++;
						if (_completedCaptureFrames >= _options.CaptureFrames)
						{
							TryEndCapture();
						}
						break;
					case PerfMeterCaptureState.PostRoll:
						_completedPostRollFrames++;
						if (_completedPostRollFrames >= _options.PostRollFrames)
						{
							SetState(PerfMeterCaptureState.Completed, PerfMeterAvailability.Available, string.Empty);
						}
						break;
				}
			}
		}

		internal bool Cancel(string captureId)
		{
			using (PerfMeterProfilerInstrumentation.CaptureCoordinatorMarker.Auto())
			{
				if ((!IsActiveState(_state) && !_backendActive && !_scopeActive) || !string.Equals(_options.CaptureId, captureId, StringComparison.Ordinal))
				{
					return false;
				}

				if (!TryReleaseCaptureResources(out string error))
				{
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
					return false;
				}

				SetState(PerfMeterCaptureState.Canceled, PerfMeterAvailability.Available, string.Empty);
				return true;
			}
		}

		internal bool Reset()
		{
			if (!TryReleaseCaptureResources(out string error))
			{
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
				return false;
			}

			_options = default;
			_completedPreRollFrames = 0;
			_completedCaptureFrames = 0;
			_completedPostRollFrames = 0;
			SetState(PerfMeterCaptureState.Idle, PerfMeterAvailability.Unknown, string.Empty);
			return true;
		}

		private bool TryBeginCapture()
		{
			_scopeActive = true;
			try
			{
				if (!_scope.TryBegin(_options.CaptureId))
				{
					_scopeActive = false;
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, "Another alert capture scope is active.");
					return false;
				}
			}
			catch (Exception exception)
			{
				TryReleaseCaptureResources(out string cleanupError);
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(FormatException(exception), cleanupError));
				return false;
			}

			try
			{
				if (!_backend.TryBegin(_options.Tool, out string error))
				{
					TryReleaseCaptureResources(out string cleanupError);
					SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(error, cleanupError));
					return false;
				}
			}
			catch (Exception exception)
			{
				TryReleaseCaptureResources(out string cleanupError);
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, CombineErrors(FormatException(exception), cleanupError));
				return false;
			}

			_backendActive = true;
			SetState(PerfMeterCaptureState.Capturing, PerfMeterAvailability.Available, string.Empty);
			return true;
		}

		private void TryEndCapture()
		{
			if (!TryReleaseCaptureResources(out string error))
			{
				SetState(PerfMeterCaptureState.Error, PerfMeterAvailability.Unavailable, error);
				return;
			}

			if (_options.PostRollFrames > 0)
			{
				SetState(PerfMeterCaptureState.PostRoll, PerfMeterAvailability.Available, string.Empty);
			}
			else
			{
				SetState(PerfMeterCaptureState.Completed, PerfMeterAvailability.Available, string.Empty);
			}
		}

		private bool TryReleaseCaptureResources(out string error)
		{
			string backendError = string.Empty;
			string scopeError = string.Empty;
			if (_backendActive)
			{
				try
				{
					if (_backend.TryEnd(out backendError))
					{
						_backendActive = false;
					}
				}
				catch (Exception exception)
				{
					backendError = FormatException(exception);
				}
			}

			if (_scopeActive)
			{
				try
				{
					if (_scope.TryEnd(_options.CaptureId))
					{
						_scopeActive = false;
					}
					else
					{
						scopeError = "Capture alert scope could not be released.";
					}
				}
				catch (Exception exception)
				{
					scopeError = FormatException(exception);
				}
			}

			error = CombineErrors(backendError, scopeError);
			return !_backendActive && !_scopeActive;
		}

		private void SetRequest(PerfMeterCaptureOptions options)
		{
			_options = options;
			_completedPreRollFrames = 0;
			_completedCaptureFrames = 0;
			_completedPostRollFrames = 0;
			_warning = string.Empty;
		}

		private void SetState(PerfMeterCaptureState state, PerfMeterAvailability availability, string warning)
		{
			_state = state;
			_availability = availability;
			_warning = warning ?? string.Empty;
			PerfMeterProfilerInstrumentation.RecordCaptureState(state);
		}

		private static bool IsActiveState(PerfMeterCaptureState state)
		{
			return state == PerfMeterCaptureState.PreRoll || state == PerfMeterCaptureState.Capturing || state == PerfMeterCaptureState.PostRoll;
		}

		private static string FormatException(Exception exception)
		{
			return exception.GetType().Name + ": " + exception.Message;
		}

		private static string CombineErrors(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			return string.IsNullOrEmpty(second) ? first : first + " " + second;
		}
	}

	internal sealed class PerfMeterExternalGpuProfilerBackend : IPerfMeterCaptureBackend
	{
		internal static PerfMeterCaptureBackendCapability EvaluateCapability(
			PerfMeterCaptureTool tool,
			RuntimePlatform platform,
			GraphicsDeviceType graphicsDeviceType,
			bool captureBuild,
			bool externalProfilerAttached)
		{
			if (tool != PerfMeterCaptureTool.RenderDoc && tool != PerfMeterCaptureTool.Pix)
			{
				return Unavailable("A supported external capture tool must be selected.");
			}

			if (!captureBuild)
			{
				return Unavailable("External GPU capture is limited to the Editor and Development builds.");
			}

			bool windows = platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.WindowsPlayer;
			bool linux = platform == RuntimePlatform.LinuxEditor || platform == RuntimePlatform.LinuxPlayer;
			bool supported = tool == PerfMeterCaptureTool.Pix
				? windows && graphicsDeviceType == GraphicsDeviceType.Direct3D12
				: (windows || linux) && (graphicsDeviceType == GraphicsDeviceType.Direct3D11 || graphicsDeviceType == GraphicsDeviceType.Direct3D12 || graphicsDeviceType == GraphicsDeviceType.Vulkan);
			if (!supported)
			{
				return Unavailable(tool + " is not supported for " + platform + " with " + graphicsDeviceType + ".");
			}

			if (!externalProfilerAttached)
			{
				return Unavailable("The requested external GPU profiler is not attached.");
			}

			return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Available, string.Empty);
		}

		public PerfMeterCaptureBackendCapability GetCapability(PerfMeterCaptureTool tool)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			bool attached;
			try
			{
				attached = UnityEngine.Experimental.Rendering.ExternalGPUProfiler.IsAttached();
			}
			catch (Exception exception)
			{
				return Unavailable(exception.GetType().Name + ": " + exception.Message);
			}

			return EvaluateCapability(tool, Application.platform, SystemInfo.graphicsDeviceType, true, attached);
		#else
			return EvaluateCapability(tool, Application.platform, SystemInfo.graphicsDeviceType, false, false);
		#endif
		}

		public bool TryBegin(PerfMeterCaptureTool tool, out string error)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			try
			{
				UnityEngine.Experimental.Rendering.ExternalGPUProfiler.BeginGPUCapture();
				error = string.Empty;
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		#else
			error = "External GPU capture is unavailable in non-development builds.";
			return false;
		#endif
		}

		public bool TryEnd(out string error)
		{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
			try
			{
				UnityEngine.Experimental.Rendering.ExternalGPUProfiler.EndGPUCapture();
				error = string.Empty;
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
		#else
			error = "External GPU capture is unavailable in non-development builds.";
			return false;
		#endif
		}

		private static PerfMeterCaptureBackendCapability Unavailable(string warning)
		{
			return new PerfMeterCaptureBackendCapability(PerfMeterAvailability.Unavailable, warning);
		}
	}
}
