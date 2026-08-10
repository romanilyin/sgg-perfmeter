using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterRenderDocPlatformInfo
	{
		internal PerfMeterRenderDocPlatformInfo(
			RuntimePlatform platform,
			GraphicsDeviceType graphicsDeviceType,
			bool isEditor,
			bool is64Bit)
		{
			Platform = platform;
			GraphicsDeviceType = graphicsDeviceType;
			IsEditor = isEditor;
			Is64Bit = is64Bit;
		}

		internal RuntimePlatform Platform { get; }
		internal GraphicsDeviceType GraphicsDeviceType { get; }
		internal bool IsEditor { get; }
		internal bool Is64Bit { get; }

		internal bool IsSupportedMatrixRow =>
			IsEditor &&
			Is64Bit &&
			Platform == RuntimePlatform.WindowsEditor &&
			(GraphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
			 GraphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
			 GraphicsDeviceType == GraphicsDeviceType.Vulkan);
	}

	internal interface IPerfMeterRenderDocPlatformProvider
	{
		PerfMeterRenderDocPlatformInfo GetPlatformInfo();
	}

	internal sealed class PerfMeterRenderDocUnityPlatformProvider : IPerfMeterRenderDocPlatformProvider
	{
		public PerfMeterRenderDocPlatformInfo GetPlatformInfo()
		{
			return new PerfMeterRenderDocPlatformInfo(
				Application.platform,
				SystemInfo.graphicsDeviceType,
				Application.isEditor,
				IntPtr.Size == 8);
		}
	}

	internal readonly struct PerfMeterRenderDocCapabilitySnapshot
	{
		internal PerfMeterRenderDocCapabilitySnapshot(
			PerfMeterAvailability availability,
			SggRdResult result,
			SggRdCapabilitiesV1 capabilities)
		{
			Availability = availability;
			Result = result;
			BridgeAbiMajor = capabilities.BridgeAbiMajor;
			BridgeAbiMinor = capabilities.BridgeAbiMinor;
			PlatformSupported = capabilities.PlatformSupported != 0u;
			ModuleLoaded = capabilities.ModuleLoaded != 0u;
			ExportAvailable = capabilities.ExportAvailable != 0u;
			ApiNegotiated = capabilities.ApiNegotiated != 0u;
			TargetControlConnected = capabilities.TargetControlConnected != 0u;
			IsCapturing = capabilities.IsCapturing != 0u;
			ApiMajor = capabilities.ApiMajor;
			ApiMinor = capabilities.ApiMinor;
			ApiPatch = capabilities.ApiPatch;
			FeatureFlags = (SggRdFeatureBitsV1)capabilities.FeatureFlags;
			SupportsDiscard = capabilities.SupportsDiscard != 0u;
			SupportsComments = capabilities.SupportsComments != 0u;
			SupportsTitle = capabilities.SupportsTitle != 0u;
			SupportsAnnotations = capabilities.SupportsAnnotations != 0u;
			CaptureCount = capabilities.CaptureCount;
		}

		internal PerfMeterAvailability Availability { get; }
		internal SggRdResult Result { get; }
		internal uint BridgeAbiMajor { get; }
		internal uint BridgeAbiMinor { get; }
		internal bool PlatformSupported { get; }
		internal bool ModuleLoaded { get; }
		internal bool ExportAvailable { get; }
		internal bool ApiNegotiated { get; }
		internal bool TargetControlConnected { get; }
		internal bool IsCapturing { get; }
		internal uint ApiMajor { get; }
		internal uint ApiMinor { get; }
		internal uint ApiPatch { get; }
		internal SggRdFeatureBitsV1 FeatureFlags { get; }
		internal bool SupportsDiscard { get; }
		internal bool SupportsComments { get; }
		internal bool SupportsTitle { get; }
		internal bool SupportsAnnotations { get; }
		internal uint CaptureCount { get; }
	}

	internal readonly struct PerfMeterRenderDocPreflightWorkerResult
	{
		internal PerfMeterRenderDocPreflightWorkerResult(
			SggRdResult result,
			PerfMeterRenderDocPreflight preflight,
			string warning)
		{
			Result = result;
			Preflight = preflight;
			Warning = warning ?? string.Empty;
		}

		internal SggRdResult Result { get; }
		internal PerfMeterRenderDocPreflight Preflight { get; }
		internal string Warning { get; }
	}

	internal readonly struct PerfMeterRenderDocCleanupWorkerResult
	{
		internal PerfMeterRenderDocCleanupWorkerResult(SggRdResult result, string warning)
		{
			Result = result;
			Warning = warning ?? string.Empty;
		}

		internal SggRdResult Result { get; }
		internal string Warning { get; }
	}

	internal sealed class PerfMeterRenderDocCaptureBackend : IPerfMeterCaptureBackendV3
	{
		private readonly IPerfMeterRenderDocBridge _bridge;
		private readonly IPerfMeterRenderDocPreflightProvider _preflightProvider;
		private readonly IPerfMeterRenderDocPlatformProvider _platformProvider;
		private readonly IPerfMeterRenderDocWorkerScheduler _worker;
		private readonly IPerfMeterRenderDocArtifactFinalizer _finalizer;
		private readonly IPerfMeterRenderDocCleanupProvider _cleanupProvider;
		private PerfMeterCaptureBackendV2Snapshot _snapshot;
		private PerfMeterRenderDocCapabilitySnapshot _capabilityDetails;
		private PerfMeterRenderDocCapabilitySnapshot _operationCapabilityDetails;
		private bool _capabilityQueried;
		private PerfMeterCaptureOptions _capabilityOptions;
		private PerfMeterRenderDocPreflight _preflight;
		private SggRdCaptureTokenV1 _token;
		private bool _hasToken;
		private bool _begun;
		private bool _endScheduled;
		private bool _endInvoked;
		private bool _discardInvoked;
		private bool _beginUncertain;
		private IPerfMeterRenderDocWorkerOperation<PerfMeterRenderDocPreflightWorkerResult> _preflightOperation;
		private IPerfMeterRenderDocWorkerOperation<PerfMeterRenderDocFinalizationResult> _finalizationOperation;
		private IPerfMeterRenderDocWorkerOperation<PerfMeterRenderDocCleanupWorkerResult> _cleanupOperation;
		private PerfMeterCaptureOptions _operationOptions;
		private int _operationGeneration;
		private volatile bool _workerCancellationRequested;
		private PerfMeterRenderDocCapturePhase _cleanupTerminalPhase;
		private int _cleanupResultCode;
		private string _cleanupWarning = string.Empty;
		private int _persistentCleanupAttempts;
		private bool _artifactCompletionAvailable;
		private PerfMeterCaptureExternalArtifactCompletion _artifactCompletion;

		internal PerfMeterRenderDocCaptureBackend(
			IPerfMeterRenderDocBridge bridge,
			IPerfMeterRenderDocPreflightProvider preflightProvider)
			: this(bridge, preflightProvider, new PerfMeterRenderDocUnityPlatformProvider())
		{
		}

		internal PerfMeterRenderDocCaptureBackend(
			IPerfMeterRenderDocBridge bridge,
			IPerfMeterRenderDocPreflightProvider preflightProvider,
			IPerfMeterRenderDocPlatformProvider platformProvider)
			: this(bridge, preflightProvider, platformProvider, null, null)
		{
		}

		internal PerfMeterRenderDocCaptureBackend(
			IPerfMeterRenderDocBridge bridge,
			IPerfMeterRenderDocPreflightProvider preflightProvider,
			IPerfMeterRenderDocPlatformProvider platformProvider,
			IPerfMeterRenderDocWorkerScheduler worker,
			IPerfMeterRenderDocArtifactFinalizer finalizer)
		{
			_bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
			_preflightProvider = preflightProvider ?? throw new ArgumentNullException(nameof(preflightProvider));
			_platformProvider = platformProvider ?? throw new ArgumentNullException(nameof(platformProvider));
			if ((worker == null) != (finalizer == null))
			{
				throw new ArgumentException("RenderDoc worker and finalizer must be configured together.");
			}
			_worker = worker;
			_finalizer = finalizer;
			_cleanupProvider = preflightProvider as IPerfMeterRenderDocCleanupProvider;
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Unknown,
				PerfMeterRenderDocCapturePhase.None,
				-1,
				string.Empty,
				false,
				false,
				false);
		}

		public PerfMeterCaptureBackendV2Snapshot Snapshot => _snapshot;

		internal PerfMeterRenderDocCapabilitySnapshot CapabilityDetails => _capabilityDetails;

		public PerfMeterCaptureBackendV2Snapshot GetCapability(PerfMeterCaptureOptions options)
		{
			if (_begun && _hasToken)
			{
				return _snapshot;
			}

			_capabilityOptions = options;
			_capabilityQueried = true;
			_snapshot = QueryCapability(options);
			return _snapshot;
		}

		public bool TryBegin(PerfMeterCaptureOptions options, out string error)
		{
			if (!TryPrepareSynchronously(options, 0, out PerfMeterRenderDocPreflight preflight, out error))
			{
				return false;
			}

			return TryBeginPrepared(options, preflight, out error);
		}

		public PerfMeterCaptureBackendBeginResult TryBegin(
			PerfMeterCaptureOptions options,
			int generation,
			out string error)
		{
			if (_worker == null)
			{
				return TryBegin(options, out error)
					? PerfMeterCaptureBackendBeginResult.Started
					: PerfMeterCaptureBackendBeginResult.Failed;
			}

			error = string.Empty;
			if (_hasToken || _finalizationOperation != null || _cleanupOperation != null)
			{
				error = "RenderDoc capture already owns active resources.";
				return PerfMeterCaptureBackendBeginResult.Failed;
			}

			if (_preflightOperation == null)
			{
				if (!EnsureAvailable(options, out error))
				{
					return PerfMeterCaptureBackendBeginResult.Failed;
				}

				_operationOptions = options;
				_operationGeneration = generation;
				_operationCapabilityDetails = _capabilityDetails;
				_workerCancellationRequested = false;
				_artifactCompletionAvailable = false;
				_preflightOperation = _worker.Start(() => PrepareOnWorker(options, generation));
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderDocCapturePhase.Preflight,
					(int)SggRdResult.Ok,
					string.Empty,
					false,
					true,
					true);
				return PerfMeterCaptureBackendBeginResult.Pending;
			}

			if (!AreSameOptions(_operationOptions, options) || _operationGeneration != generation)
			{
				error = "RenderDoc pending preflight belongs to another capture generation.";
				return PerfMeterCaptureBackendBeginResult.Failed;
			}

			if (!_preflightOperation.IsCompleted)
			{
				return PerfMeterCaptureBackendBeginResult.Pending;
			}

			PerfMeterRenderDocPreflightWorkerResult workerResult;
			try
			{
				workerResult = _preflightOperation.GetResult();
			}
			catch (Exception exception)
			{
				_preflightOperation = null;
				return FailPendingBegin(
					PerfMeterRenderDocPInvokeBridge.MapInteropException(exception),
					FormatException(exception),
					default,
					out error);
			}
			_preflightOperation = null;

			if (_workerCancellationRequested)
			{
				return FailPendingBegin(
					SggRdResult.CaptureFailed,
					"RenderDoc preflight was canceled.",
					workerResult.Preflight,
					out error,
					PerfMeterRenderDocCapturePhase.Completed);
			}

			if (workerResult.Result != SggRdResult.Ok)
			{
				return FailPendingBegin(workerResult.Result, workerResult.Warning, workerResult.Preflight, out error);
			}

			bool started = TryBeginPrepared(options, workerResult.Preflight, out error);
			if (!started && !_begun && workerResult.Preflight.Reservation != null)
			{
				ScheduleCleanup(
					workerResult.Preflight,
					PerfMeterRenderDocCapturePhase.Failed,
					_snapshot.NativeResultCode,
					error);
			}
			return started
				? PerfMeterCaptureBackendBeginResult.Started
				: PerfMeterCaptureBackendBeginResult.Failed;
		}

		private bool TryPrepareSynchronously(
			PerfMeterCaptureOptions options,
			int generation,
			out PerfMeterRenderDocPreflight preflight,
			out string error)
		{
			preflight = default;
			error = string.Empty;
			if (_hasToken)
			{
				error = "RenderDoc capture is already active.";
				return false;
			}

			if (!EnsureAvailable(options, out error))
			{
				return false;
			}
			_operationCapabilityDetails = _capabilityDetails;

			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.Preflight,
				-1,
				string.Empty,
				false,
				false,
				false);

			SggRdResult preflightResult;
			try
			{
				preflightResult = _preflightProvider is IPerfMeterRenderDocPreflightProviderV2 providerV2
					? providerV2.Prepare(options, generation, out preflight)
					: _preflightProvider.Prepare(options, out preflight);
			}
			catch (Exception exception)
			{
				return FailBegin(PerfMeterRenderDocPInvokeBridge.MapInteropException(exception), FormatException(exception), out error);
			}

			if (preflightResult != SggRdResult.Ok)
			{
				string warning = preflightResult == SggRdResult.InternalError
					? PerfMeterRenderDocPreflightProvider.PolicyNotReadyMessage
					: DescribeResult(preflightResult);
				return FailBegin(preflightResult, warning, out error);
			}
			return true;
		}

		private bool TryBeginPrepared(
			PerfMeterCaptureOptions options,
			PerfMeterRenderDocPreflight preflight,
			out string error)
		{
			error = string.Empty;
			_preflight = preflight;
			if (!IsValidPreflight(_preflight, out string preflightError))
			{
				return FailBegin(SggRdResult.InvalidArgument, preflightError, out error);
			}

			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.BeginScheduled,
				-1,
				string.Empty,
				false,
				false,
				false);

			SggRdResult beginResult;
			SggRdCaptureTokenV1 returnedToken = default;
			_token = default;
			try
			{
				beginResult = _bridge.BeginCapture(
					_preflight.RequestNonce,
					_preflight.CapturePathTemplate,
					_preflight.Title,
					out returnedToken);
				_token = returnedToken;
			}
			catch (Exception exception)
			{
				_token = returnedToken;
				MarkBeginOwnership(true);
				return FailBegin(
					PerfMeterRenderDocPInvokeBridge.MapInteropException(exception),
					FormatException(exception),
					out error,
					true);
			}

			if (beginResult != SggRdResult.Ok)
			{
				return FailBegin(beginResult, DescribeResult(beginResult), out error);
			}

			// The native side owns an operation as soon as it reports OK. Claim the
			// returned token before validating it so an invalid token remains
			// discardable and cannot be mistaken for a safe, inactive failure.
			MarkBeginOwnership(false);
			if (_token.StructSize < PerfMeterRenderDocAbiV1.CaptureTokenSize ||
				_token.RequestNonce != _preflight.RequestNonce)
			{
				_beginUncertain = true;
				return FailBegin(
					SggRdResult.InternalError,
					"RenderDoc bridge returned an invalid capture token.",
					out error,
					true);
			}

			_beginUncertain = false;
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.BeginExecuted,
				(int)SggRdResult.Ok,
				string.Empty,
				true,
				false,
				true);
			return true;
		}

		public bool ScheduleEnd(out string error)
		{
			error = string.Empty;
			if (_beginUncertain)
			{
				error = _snapshot.Warning;
				return false;
			}

			if (!_hasToken)
			{
				return _snapshot.NativePhase == PerfMeterRenderDocCapturePhase.Completed;
			}

			if (_endScheduled && _endInvoked)
			{
				error = _snapshot.Warning;
				return _snapshot.NativePhase == PerfMeterRenderDocCapturePhase.Completed;
			}

			_endScheduled = true;
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.EndScheduled,
				(int)SggRdResult.Ok,
				string.Empty,
				true,
				false,
				true);

			// Set this before crossing the bridge. Even an exception is an attempted
			// end and must not be replayed by a later coordinator tick.
			_endInvoked = true;
			SggRdResult endResult;
			try
			{
				endResult = _bridge.EndCapture(_token);
			}
			catch (Exception exception)
			{
				return FailEnd(PerfMeterRenderDocPInvokeBridge.MapInteropException(exception), FormatException(exception), out error);
			}

			if (endResult != SggRdResult.Ok)
			{
				return FailEnd(endResult, DescribeResult(endResult), out error);
			}

			_begun = false;
			_hasToken = false;
			_beginUncertain = false;
			_endScheduled = false;
			if (_worker != null && _finalizer != null && _preflight.Reservation != null)
			{
				SggRdCaptureTokenV1 completedToken = _token;
				PerfMeterRenderDocPreflight completedPreflight = _preflight;
				_workerCancellationRequested = false;
				_finalizationOperation = _worker.Start(() => _finalizer.Run(
					_bridge,
					completedToken,
					completedPreflight,
					() => _workerCancellationRequested));
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderDocCapturePhase.AwaitingArtifact,
					(int)SggRdResult.Ok,
					string.Empty,
					false,
					true,
					true);
				return true;
			}
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.Completed,
				(int)SggRdResult.Ok,
				string.Empty,
				false,
				false,
				false);
			return true;
		}

		public bool TryDiscard(out string error)
		{
			error = string.Empty;
			if (_preflightOperation != null)
			{
				_workerCancellationRequested = true;
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Available,
					PerfMeterRenderDocCapturePhase.Preflight,
					(int)SggRdResult.Ok,
					"RenderDoc preflight cancellation is pending.",
					false,
					true,
					true);
				return true;
			}

			if (_finalizationOperation != null)
			{
				_workerCancellationRequested = true;
				return true;
			}

			if (_cleanupOperation != null)
			{
				return true;
			}

			if (!_begun)
			{
				if (_worker != null && _preflight.Reservation != null && !_preflight.Reservation.IsReleased)
				{
					ScheduleCleanup(
						_preflight,
						PerfMeterRenderDocCapturePhase.Completed,
						(int)SggRdResult.Ok,
						string.Empty);
					return true;
				}
				return !_snapshot.HasActiveResources;
			}

			if (_discardInvoked)
			{
				error = _snapshot.Warning;
				return false;
			}

			_discardInvoked = true;
			SggRdResult discardResult;
			try
			{
				discardResult = _bridge.DiscardCapture(_token);
			}
			catch (Exception exception)
			{
				return FailDiscard(PerfMeterRenderDocPInvokeBridge.MapInteropException(exception), FormatException(exception), out error);
			}

			if (discardResult == SggRdResult.NotCapturing)
			{
				_begun = false;
				_hasToken = false;
				_beginUncertain = false;
				_endScheduled = false;
				_endInvoked = false;
				if (_worker != null && _preflight.Reservation != null && !_preflight.Reservation.IsReleased)
				{
					ScheduleCleanup(
						_preflight,
						PerfMeterRenderDocCapturePhase.LostSession,
						(int)discardResult,
						DescribeResult(discardResult));
					return true;
				}
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterRenderDocCapturePhase.LostSession,
					(int)discardResult,
					DescribeResult(discardResult),
					false,
					false,
					false);
				return true;
			}

			if (discardResult != SggRdResult.Ok)
			{
				if (discardResult == SggRdResult.CaptureFailed)
				{
					_discardInvoked = false;
				}
				return FailDiscard(discardResult, DescribeResult(discardResult), out error);
			}

			_begun = false;
			_hasToken = false;
			_beginUncertain = false;
			_endScheduled = false;
			_endInvoked = false;
			if (_worker != null && _preflight.Reservation != null && !_preflight.Reservation.IsReleased)
			{
				ScheduleCleanup(
					_preflight,
					PerfMeterRenderDocCapturePhase.Completed,
					(int)SggRdResult.Ok,
					string.Empty);
				return true;
			}
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.Completed,
				(int)SggRdResult.Ok,
				string.Empty,
				false,
				false,
				false);
			return true;
		}

		public void Tick()
		{
			TickCanceledPreflight();
			TickFinalization();
			TickCleanup();
		}

		public bool TryConsumeExternalArtifact(out PerfMeterCaptureExternalArtifactCompletion completion)
		{
			completion = default;
			if (!_artifactCompletionAvailable)
			{
				return false;
			}

			completion = _artifactCompletion;
			_artifactCompletion = default;
			_artifactCompletionAvailable = false;
			return true;
		}

		private bool EnsureAvailable(PerfMeterCaptureOptions options, out string error)
		{
			error = string.Empty;
			if (!_capabilityQueried || !AreSameOptions(_capabilityOptions, options))
			{
				_capabilityOptions = options;
				_capabilityQueried = true;
				_snapshot = QueryCapability(options);
			}

			if (_snapshot.Availability == PerfMeterAvailability.Available)
			{
				return true;
			}

			error = _snapshot.Warning;
			return false;
		}

		private PerfMeterRenderDocPreflightWorkerResult PrepareOnWorker(
			PerfMeterCaptureOptions options,
			int generation)
		{
			PerfMeterRenderDocPreflight preflight = default;
			try
			{
				SggRdResult result = _preflightProvider is IPerfMeterRenderDocPreflightProviderV2 providerV2
					? providerV2.Prepare(options, generation, out preflight)
					: _preflightProvider.Prepare(options, out preflight);
				if (result != SggRdResult.Ok)
				{
					return new PerfMeterRenderDocPreflightWorkerResult(
						result,
						preflight,
						result == SggRdResult.InternalError
							? PerfMeterRenderDocPreflightProvider.PolicyNotReadyMessage
							: DescribeResult(result));
				}

				if (preflight.Reservation != null)
				{
					SggRdResult stateResult = preflight.Reservation.SetState(
						PerfMeterRenderDocStorageState.Capturing,
						out string stateError);
					if (stateResult != SggRdResult.Ok)
					{
						preflight.Abort(out string abortError);
						return new PerfMeterRenderDocPreflightWorkerResult(
							stateResult,
							default,
							CombineWarnings(stateError, abortError));
					}
				}

				return new PerfMeterRenderDocPreflightWorkerResult(SggRdResult.Ok, preflight, string.Empty);
			}
			catch (Exception exception)
			{
				if (preflight.Reservation != null)
				{
					preflight.Abort(out _);
				}
				return new PerfMeterRenderDocPreflightWorkerResult(
					PerfMeterRenderDocPInvokeBridge.MapInteropException(exception),
					default,
					FormatException(exception));
			}
		}

		private PerfMeterCaptureBackendBeginResult FailPendingBegin(
			SggRdResult result,
			string warning,
			PerfMeterRenderDocPreflight preflight,
			out string error,
			PerfMeterRenderDocCapturePhase terminalPhase = PerfMeterRenderDocCapturePhase.Failed)
		{
			error = string.IsNullOrEmpty(warning) ? DescribeResult(result) : warning;
			if (preflight.Reservation != null && !preflight.Reservation.IsReleased)
			{
				ScheduleCleanup(preflight, terminalPhase, (int)result, error);
			}
			else
			{
				_snapshot = CreateSnapshot(
					terminalPhase == PerfMeterRenderDocCapturePhase.Completed
						? PerfMeterAvailability.Available
						: PerfMeterAvailability.Unavailable,
					terminalPhase,
					(int)result,
					error,
					false,
					false,
					false);
			}
			return PerfMeterCaptureBackendBeginResult.Failed;
		}

		private void ScheduleCleanup(
			PerfMeterRenderDocPreflight preflight,
			PerfMeterRenderDocCapturePhase terminalPhase,
			int resultCode,
			string warning)
		{
			_cleanupTerminalPhase = terminalPhase;
			_cleanupResultCode = resultCode;
			_cleanupWarning = warning ?? string.Empty;
			_persistentCleanupAttempts = 0;
			_preflight = preflight;
			_cleanupOperation = _worker.Start(() =>
			{
				SggRdResult cleanupResult = preflight.Abort(out string cleanupError);
				return new PerfMeterRenderDocCleanupWorkerResult(cleanupResult, cleanupError);
			});
			_snapshot = CreateSnapshot(
				terminalPhase == PerfMeterRenderDocCapturePhase.Completed
					? PerfMeterAvailability.Available
					: PerfMeterAvailability.Unavailable,
				terminalPhase,
				resultCode,
				_cleanupWarning,
				false,
				true,
				true);
		}

		private void TickCanceledPreflight()
		{
			if (_preflightOperation == null || !_workerCancellationRequested || !_preflightOperation.IsCompleted)
			{
				return;
			}

			try
			{
				PerfMeterRenderDocPreflightWorkerResult result = _preflightOperation.GetResult();
				_preflightOperation = null;
				if (result.Preflight.Reservation != null && !result.Preflight.Reservation.IsReleased)
				{
					ScheduleCleanup(
						result.Preflight,
						PerfMeterRenderDocCapturePhase.Completed,
						(int)SggRdResult.Ok,
						string.Empty);
				}
				else
				{
					_snapshot = CreateSnapshot(
						PerfMeterAvailability.Available,
						PerfMeterRenderDocCapturePhase.Completed,
						(int)SggRdResult.Ok,
						string.Empty,
						false,
						false,
						false);
				}
			}
			catch (Exception exception)
			{
				_preflightOperation = null;
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterRenderDocCapturePhase.Failed,
					(int)PerfMeterRenderDocPInvokeBridge.MapInteropException(exception),
					FormatException(exception),
					false,
					false,
					false);
			}
		}

		private void TickFinalization()
		{
			if (_finalizationOperation == null || !_finalizationOperation.IsCompleted)
			{
				return;
			}

			try
			{
				PerfMeterRenderDocFinalizationResult result = _finalizationOperation.GetResult();
				_finalizationOperation = null;
				bool canceled = _workerCancellationRequested;
				if (!canceled)
				{
					PerfMeterNativeExternalArtifactSourceDescriptor sourceDescriptor = CreateSourceDescriptor(result);
					_artifactCompletion = new PerfMeterCaptureExternalArtifactCompletion(
						_operationOptions.CaptureId,
						_operationGeneration,
						result.Artifact,
						result.RetainedPayloadPath,
						sourceDescriptor);
					_artifactCompletionAvailable = true;
				}
				_preflight = default;
				_snapshot = CreateSnapshot(
					canceled || result.Succeeded ? PerfMeterAvailability.Available : PerfMeterAvailability.Unavailable,
					canceled || result.Succeeded
						? PerfMeterRenderDocCapturePhase.Completed
						: PerfMeterRenderDocCapturePhase.Failed,
					canceled ? (int)SggRdResult.Ok : (int)result.Result,
					canceled ? string.Empty : result.Warning,
					false,
					false,
					false);
			}
			catch (Exception exception)
			{
				_finalizationOperation = null;
				SggRdResult result = PerfMeterRenderDocPInvokeBridge.MapInteropException(exception);
				string warning = FormatException(exception);
				if (_preflight.Reservation != null && !_preflight.Reservation.IsReleased)
				{
					ScheduleCleanup(
						_preflight,
						PerfMeterRenderDocCapturePhase.Failed,
						(int)result,
						warning);
				}
				else
				{
					_preflight = default;
					_snapshot = CreateSnapshot(
						PerfMeterAvailability.Unavailable,
						PerfMeterRenderDocCapturePhase.Failed,
						(int)result,
						warning,
						false,
						false,
						false);
				}
			}
		}

		private void TickCleanup()
		{
			if (_cleanupOperation == null || !_cleanupOperation.IsCompleted)
			{
				return;
			}

			try
			{
				PerfMeterRenderDocCleanupWorkerResult cleanup = _cleanupOperation.GetResult();
				_cleanupOperation = null;
				bool cleaned = cleanup.Result == SggRdResult.Ok;
				if (!cleaned && TrySchedulePersistentCleanup(cleanup.Result, cleanup.Warning))
				{
					return;
				}

				_preflight = default;
				_snapshot = CreateSnapshot(
					cleaned && _cleanupTerminalPhase == PerfMeterRenderDocCapturePhase.Completed
						? PerfMeterAvailability.Available
						: PerfMeterAvailability.Unavailable,
					cleaned ? _cleanupTerminalPhase : PerfMeterRenderDocCapturePhase.Failed,
					cleaned ? _cleanupResultCode : (int)cleanup.Result,
					cleaned ? _cleanupWarning : CombineWarnings(_cleanupWarning, cleanup.Warning),
					false,
					false,
					false);
			}
			catch (Exception exception)
			{
				_cleanupOperation = null;
				SggRdResult result = PerfMeterRenderDocPInvokeBridge.MapInteropException(exception);
				string warning = FormatException(exception);
				if (TrySchedulePersistentCleanup(result, warning))
				{
					return;
				}

				_preflight = default;
				_snapshot = CreateSnapshot(
					PerfMeterAvailability.Unavailable,
					PerfMeterRenderDocCapturePhase.Failed,
					(int)result,
					CombineWarnings(_cleanupWarning, warning),
					false,
					false,
					false);
			}
		}

		private PerfMeterNativeExternalArtifactSourceDescriptor CreateSourceDescriptor(
			PerfMeterRenderDocFinalizationResult result)
		{
			if (!result.Succeeded ||
				result.Token.StructSize < PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt ||
				result.ObservedArtifact.StructSize < PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt)
			{
				return default;
			}

			return new PerfMeterNativeExternalArtifactSourceDescriptor(
				PerfMeterNativeExternalArtifactSourceKind.RenderDoc,
				_operationCapabilityDetails.BridgeAbiMajor,
				_operationCapabilityDetails.BridgeAbiMinor,
				_operationCapabilityDetails.ApiMajor,
				_operationCapabilityDetails.ApiMinor,
				_operationCapabilityDetails.ApiPatch,
				"managed_end_of_frame",
				"wildcard_device_window",
				unchecked((ulong)Math.Max(0, _operationGeneration)),
				result.Token.RequestNonce,
				result.Token.CountBefore,
				result.Token.StartUnixNanoseconds,
				result.ObservedArtifact.Index,
				result.ObservedArtifact.RenderDocTimestampSeconds,
				result.ObservedArtifact.ObservedUnixNanoseconds,
				result.PayloadSource);
		}

		private bool TrySchedulePersistentCleanup(SggRdResult failedResult, string warning)
		{
			if (_worker == null || _cleanupProvider == null ||
				_persistentCleanupAttempts >= PerfMeterRenderDocStoragePolicy.PersistentCleanupAttempts)
			{
				return false;
			}

			_persistentCleanupAttempts++;
			_cleanupWarning = CombineWarnings(_cleanupWarning, warning);
			string rootPath = _preflight.RootPath;
			_cleanupOperation = _worker.Start(() =>
			{
				SggRdResult cleanupResult = _cleanupProvider.RetryPendingCleanup(rootPath, out string cleanupError);
				return new PerfMeterRenderDocCleanupWorkerResult(cleanupResult, cleanupError);
			});
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Unavailable,
				_cleanupTerminalPhase,
				(int)failedResult,
				_cleanupWarning,
				false,
				true,
				true);
			return true;
		}

		private PerfMeterCaptureBackendV2Snapshot QueryCapability(PerfMeterCaptureOptions options)
		{
			if (options.Tool != PerfMeterCaptureTool.RenderDoc)
			{
				_capabilityDetails = default;
				return CreateUnavailable(SggRdResult.InvalidArgument, "The RenderDoc backend requires the RenderDoc capture tool.");
			}

			if (options.BackendMode == PerfMeterCaptureBackendMode.GenericUnity || !options.IsValidBackendMode)
			{
				_capabilityDetails = default;
				return CreateUnavailable(SggRdResult.InvalidArgument, "The RenderDoc backend requires an explicit native backend mode.");
			}

			PerfMeterRenderDocPlatformInfo platformInfo;
			try
			{
				platformInfo = _platformProvider.GetPlatformInfo();
			}
			catch (Exception exception)
			{
				_capabilityDetails = default;
				return CreateUnavailable(SggRdResult.InternalError, FormatException(exception));
			}

			if (!platformInfo.IsSupportedMatrixRow)
			{
				_capabilityDetails = default;
				return CreateUnavailable(
					SggRdResult.UnsupportedPlatform,
					"RenderDoc native capture is limited to Windows x64 Editor D3D11, D3D12, and Vulkan.");
			}

			SggRdCapabilitiesV1 capabilities;
			SggRdResult result;
			try
			{
				result = _bridge.GetCapabilities(out capabilities);
			}
			catch (Exception exception)
			{
				_capabilityDetails = default;
				return CreateUnavailable(PerfMeterRenderDocPInvokeBridge.MapInteropException(exception), FormatException(exception));
			}

			if (result != SggRdResult.Ok)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(PerfMeterAvailability.Unavailable, result, capabilities);
				return CreateUnavailable(result, DescribeResult(result));
			}

			if (capabilities.StructSize < PerfMeterRenderDocAbiV1.CapabilitiesSize)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.InternalError,
					capabilities);
				return CreateUnavailable(SggRdResult.InternalError, "RenderDoc bridge returned a short capabilities structure.");
			}

			if (capabilities.BridgeAbiMajor != PerfMeterRenderDocAbiV1.AbiMajor)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.ApiNegotiationFailed,
					capabilities);
				return CreateUnavailable(SggRdResult.ApiNegotiationFailed, "RenderDoc bridge ABI major version is unsupported.");
			}

			if (capabilities.PlatformSupported == 0u)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.UnsupportedPlatform,
					capabilities);
				return CreateUnavailable(SggRdResult.UnsupportedPlatform, "The RenderDoc bridge reports an unsupported platform.");
			}

			if (capabilities.ModuleLoaded == 0u)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.NotLoaded,
					capabilities);
				return CreateUnavailable(SggRdResult.NotLoaded, DescribeResult(SggRdResult.NotLoaded));
			}

			if (capabilities.ExportAvailable == 0u)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.ExportMissing,
					capabilities);
				return CreateUnavailable(SggRdResult.ExportMissing, DescribeResult(SggRdResult.ExportMissing));
			}

			if (capabilities.ApiNegotiated == 0u ||
				capabilities.ApiMajor != 1u ||
				capabilities.ApiMinor < 4u ||
				capabilities.SupportsDiscard == 0u ||
				capabilities.SupportsComments == 0u)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.ApiNegotiationFailed,
					capabilities);
				return CreateUnavailable(SggRdResult.ApiNegotiationFailed, DescribeResult(SggRdResult.ApiNegotiationFailed));
			}

			if (capabilities.IsCapturing != 0u)
			{
				_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(
					PerfMeterAvailability.Unavailable,
					SggRdResult.AlreadyCapturing,
					capabilities);
				return CreateUnavailable(SggRdResult.AlreadyCapturing, DescribeResult(SggRdResult.AlreadyCapturing));
			}

			_capabilityDetails = new PerfMeterRenderDocCapabilitySnapshot(PerfMeterAvailability.Available, result, capabilities);
			return CreateSnapshot(
				PerfMeterAvailability.Available,
				PerfMeterRenderDocCapturePhase.None,
				(int)SggRdResult.Ok,
				string.Empty,
				false,
				false,
				false);
		}

		private bool IsValidPreflight(PerfMeterRenderDocPreflight preflight, out string error)
		{
			error = string.Empty;
			if (preflight.RequestNonce == 0u)
			{
				error = "RenderDoc preflight returned a zero request nonce.";
				return false;
			}

			if (!IsAbsolutePathTemplate(preflight.CapturePathTemplate) ||
				!PerfMeterRenderDocUtf8.TryEncode(
					preflight.CapturePathTemplate,
					PerfMeterRenderDocAbiV1.MaxPathInputBytes,
					false,
					out byte[] pathBytes) ||
				pathBytes.Length == 0 ||
				IsPathSeparator(preflight.CapturePathTemplate[preflight.CapturePathTemplate.Length - 1]))
			{
				error = "RenderDoc preflight returned a non-absolute or oversized capture path template.";
				return false;
			}

			if (!PerfMeterRenderDocUtf8.TryEncode(
					preflight.Title,
					PerfMeterRenderDocAbiV1.MaxTitleBytes,
					true,
					out _))
			{
				error = "RenderDoc preflight returned a title over the UTF-8 byte limit.";
				return false;
			}

			return true;
		}

		private void MarkBeginOwnership(bool uncertain)
		{
			_begun = true;
			_hasToken = true;
			_beginUncertain = uncertain;
			_endScheduled = false;
			_endInvoked = false;
			_discardInvoked = false;
		}

		private bool FailBegin(
			SggRdResult result,
			string warning,
			out string error,
			bool hasActiveResources = false)
		{
			error = warning ?? DescribeResult(result);
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Unavailable,
				PerfMeterRenderDocCapturePhase.Failed,
				(int)result,
				error,
				false,
				false,
				hasActiveResources);
			return false;
		}

		private bool FailEnd(SggRdResult result, string warning, out string error)
		{
			error = warning ?? DescribeResult(result);
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Unavailable,
				PerfMeterRenderDocCapturePhase.Failed,
				(int)result,
				error,
				true,
				false,
				true);
			return false;
		}

		private bool FailDiscard(SggRdResult result, string warning, out string error)
		{
			error = warning ?? DescribeResult(result);
			_snapshot = CreateSnapshot(
				PerfMeterAvailability.Unavailable,
				PerfMeterRenderDocCapturePhase.Failed,
				(int)result,
				error,
				true,
				false,
				true);
			return false;
		}

		private PerfMeterCaptureBackendV2Snapshot CreateUnavailable(SggRdResult result, string warning)
		{
			return CreateSnapshot(
				PerfMeterAvailability.Unavailable,
				PerfMeterRenderDocCapturePhase.None,
				(int)result,
				warning,
				false,
				false,
				false);
		}

		private static PerfMeterCaptureBackendV2Snapshot CreateSnapshot(
			PerfMeterAvailability availability,
			PerfMeterRenderDocCapturePhase phase,
			int resultCode,
			string warning,
			bool requiresEndOfFrame,
			bool hasPendingCompletion,
			bool hasActiveResources)
		{
			return new PerfMeterCaptureBackendV2Snapshot(
				availability,
				warning,
				PerfMeterCaptureBackendKind.RenderDocNative,
				phase,
				resultCode,
				string.Empty,
				requiresEndOfFrame,
				hasPendingCompletion,
				hasActiveResources);
		}

		private static bool AreSameOptions(PerfMeterCaptureOptions left, PerfMeterCaptureOptions right)
		{
			return string.Equals(left.CaptureId, right.CaptureId, StringComparison.Ordinal) &&
				left.Tool == right.Tool &&
				left.CaptureFrames == right.CaptureFrames &&
				left.PreRollFrames == right.PreRollFrames &&
				left.PostRollFrames == right.PostRollFrames &&
				left.BackendMode == right.BackendMode &&
				left.ExternalArtifactStorageMode == right.ExternalArtifactStorageMode;
		}

		private static bool IsAbsolutePathTemplate(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			if (Path.IsPathRooted(path))
			{
				return true;
			}

			if (path.StartsWith("\\\\", StringComparison.Ordinal))
			{
				return true;
			}

			return path.Length >= 3 &&
				char.IsLetter(path[0]) &&
				path[1] == ':' &&
				IsPathSeparator(path[2]);
		}

		private static bool IsPathSeparator(char value)
		{
			return value == '/' || value == '\\';
		}

		private static string DescribeResult(SggRdResult result)
		{
			switch (result)
			{
				case SggRdResult.NotLoaded:
					return "RenderDoc bridge module is not loaded.";
				case SggRdResult.ExportMissing:
					return "RenderDoc bridge export is missing.";
				case SggRdResult.ApiNegotiationFailed:
					return "RenderDoc app API negotiation failed or lacks mandatory discard support.";
				case SggRdResult.AlreadyCapturing:
					return "RenderDoc already has an active capture.";
				case SggRdResult.NotCapturing:
					return "RenderDoc has no active capture for this token.";
				case SggRdResult.CaptureFailed:
					return "RenderDoc capture operation failed.";
				case SggRdResult.CaptureNotObserved:
					return "RenderDoc capture artifact was not observed.";
				case SggRdResult.BufferTooSmall:
					return "RenderDoc returned a buffer-too-small result.";
				case SggRdResult.UnsupportedPlatform:
					return "RenderDoc native capture is unsupported on this platform.";
				case SggRdResult.InvalidArgument:
					return "RenderDoc bridge rejected an argument.";
				case SggRdResult.InternalError:
					return "RenderDoc bridge returned an internal error.";
				default:
					return "RenderDoc bridge returned an unknown result.";
			}
		}

		private static string FormatException(Exception exception)
		{
			return exception.GetType().Name + ": " + exception.Message;
		}

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrEmpty(first))
			{
				return second ?? string.Empty;
			}

			return string.IsNullOrEmpty(second) ? first : first + " " + second;
		}
	}
}
