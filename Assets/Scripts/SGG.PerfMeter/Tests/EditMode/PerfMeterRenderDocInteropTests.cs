using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocInteropTests
	{
		[SetUp]
		public void SetUp()
		{
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
		}

		[TearDown]
		public void TearDown()
		{
			PerfMeterNativeCaptureBackendRegistry.ResetForTests();
		}

		[Test]
		public void FixedAbiLayoutAndResultValuesRemainStable()
		{
			Assert.That(Marshal.SizeOf(Enum.GetUnderlyingType(typeof(SggRdResult))), Is.EqualTo(4));
			Assert.That(Marshal.SizeOf(typeof(SggRdCapabilitiesV1)), Is.EqualTo(72));
			Assert.That(Marshal.SizeOf(typeof(SggRdCaptureTokenV1)), Is.EqualTo(32));
			Assert.That(Marshal.SizeOf(typeof(SggRdArtifactV1)), Is.EqualTo(32));
			Assert.That(typeof(SggRdCapabilitiesV1).StructLayout().Pack, Is.EqualTo(8));
			Assert.That(typeof(SggRdCaptureTokenV1).StructLayout().Pack, Is.EqualTo(8));
			Assert.That(typeof(SggRdArtifactV1).StructLayout().Pack, Is.EqualTo(8));

			AssertOffsets<SggRdCapabilitiesV1>(
				("StructSize", 0),
				("BridgeAbiMajor", 4),
				("BridgeAbiMinor", 8),
				("PlatformSupported", 12),
				("ModuleLoaded", 16),
				("ExportAvailable", 20),
				("ApiNegotiated", 24),
				("TargetControlConnected", 28),
				("IsCapturing", 32),
				("ApiMajor", 36),
				("ApiMinor", 40),
				("ApiPatch", 44),
				("FeatureFlags", 48),
				("SupportsDiscard", 52),
				("SupportsComments", 56),
				("SupportsTitle", 60),
				("SupportsAnnotations", 64),
				("CaptureCount", 68));
			AssertOffsets<SggRdCaptureTokenV1>(
				("StructSize", 0),
				("Reserved0", 4),
				("RequestNonce", 8),
				("CountBefore", 16),
				("Reserved1", 20),
				("StartUnixNanoseconds", 24));
			AssertOffsets<SggRdArtifactV1>(
				("StructSize", 0),
				("Index", 4),
				("RenderDocTimestampSeconds", 8),
				("ObservedUnixNanoseconds", 16),
				("RequiredPathBytes", 24),
				("Reserved0", 28));

			Assert.That((uint)SggRdResult.Ok, Is.EqualTo(0u));
			Assert.That((uint)SggRdResult.NotLoaded, Is.EqualTo(1u));
			Assert.That((uint)SggRdResult.ExportMissing, Is.EqualTo(2u));
			Assert.That((uint)SggRdResult.ApiNegotiationFailed, Is.EqualTo(3u));
			Assert.That((uint)SggRdResult.AlreadyCapturing, Is.EqualTo(4u));
			Assert.That((uint)SggRdResult.NotCapturing, Is.EqualTo(5u));
			Assert.That((uint)SggRdResult.CaptureFailed, Is.EqualTo(6u));
			Assert.That((uint)SggRdResult.CaptureNotObserved, Is.EqualTo(7u));
			Assert.That((uint)SggRdResult.BufferTooSmall, Is.EqualTo(8u));
			Assert.That((uint)SggRdResult.UnsupportedPlatform, Is.EqualTo(9u));
			Assert.That((uint)SggRdResult.InvalidArgument, Is.EqualTo(10u));
			Assert.That((uint)SggRdResult.InternalError, Is.EqualTo(11u));
			Assert.That(PerfMeterRenderDocAbiV1.MaxTitleBytes, Is.EqualTo(256));
			Assert.That(PerfMeterRenderDocAbiV1.MaxCommentsBytes, Is.EqualTo(1024));
			Assert.That(PerfMeterRenderDocAbiV1.MaxPathBytes, Is.EqualTo(32768));
		}

		[Test]
		public void PInvokeContractHasTheSixFixedCAbiEntryPoints()
		{
			Type nativeMethods = typeof(PerfMeterRenderDocPInvokeBridge).GetNestedType(
				"NativeMethods",
				BindingFlags.NonPublic);
			Assert.That(nativeMethods, Is.Not.Null);

			string[] entryPoints =
			{
				"SggRd_GetCapabilitiesV1",
				"SggRd_BeginCaptureV1",
				"SggRd_EndCaptureV1",
				"SggRd_DiscardCaptureV1",
				"SggRd_TryGetNewArtifactV1",
				"SggRd_SetCaptureCommentsV1"
			};
			foreach (string entryPoint in entryPoints)
			{
				MethodInfo method = nativeMethods.GetMethod(entryPoint, BindingFlags.NonPublic | BindingFlags.Static);
				DllImportAttribute import = method == null
					? null
					: method.GetCustomAttribute<DllImportAttribute>();
				Assert.That(import, Is.Not.Null, entryPoint);
				Assert.That(import.Value, Is.EqualTo("sgg_renderdoc_bridge"));
				Assert.That(import.EntryPoint, Is.EqualTo(entryPoint));
				Assert.That(import.ExactSpelling, Is.True);
				Assert.That(import.CallingConvention, Is.EqualTo(CallingConvention.Cdecl));
			}
		}

		[TestCase(RuntimePlatform.LinuxEditor, GraphicsDeviceType.Direct3D11, true, true)]
		[TestCase(RuntimePlatform.WindowsEditor, GraphicsDeviceType.OpenGLCore, true, true)]
		[TestCase(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11, false, true)]
		[TestCase(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11, true, false)]
		public void CapabilityMatrixRejectsRowsOutsideWindowsX64EditorRenderDoc(
			RuntimePlatform platform,
			GraphicsDeviceType graphicsDeviceType,
			bool isEditor,
			bool is64Bit)
		{
			FakeBridge bridge = new FakeBridge();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(
				bridge,
				new FakePlatformProvider(new PerfMeterRenderDocPlatformInfo(platform, graphicsDeviceType, isEditor, is64Bit)));

			PerfMeterCaptureBackendV2Snapshot snapshot = backend.GetCapability(CreateOptions());

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(snapshot.NativeResultCode, Is.EqualTo((int)SggRdResult.UnsupportedPlatform));
			Assert.That(bridge.GetCapabilitiesCount, Is.Zero);
		}

		[Test]
		public void CapabilityRejectsNonRenderDocToolAndGenericMode()
		{
			FakeBridge bridge = new FakeBridge();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			PerfMeterCaptureOptions wrongTool = new PerfMeterCaptureOptions(
				"pix",
				PerfMeterCaptureTool.Pix,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired);
			Assert.That(backend.GetCapability(wrongTool).NativeResultCode, Is.EqualTo((int)SggRdResult.InvalidArgument));

			PerfMeterCaptureOptions genericMode = new PerfMeterCaptureOptions(
				"generic",
				PerfMeterCaptureTool.RenderDoc);
			Assert.That(backend.GetCapability(genericMode).NativeResultCode, Is.EqualTo((int)SggRdResult.InvalidArgument));
			Assert.That(bridge.GetCapabilitiesCount, Is.Zero);
		}

		[TestCase(4, false, false)]
		[TestCase(6, true, false)]
		[TestCase(7, true, true)]
		public void CapabilityMapsRenderDocApi14Api16AndApi17Features(
			int apiMinor,
			bool supportsTitle,
			bool supportsAnnotations)
		{
			FakeBridge bridge = new FakeBridge
			{
				Capabilities = ReadyCapabilities((uint)apiMinor, supportsTitle, supportsAnnotations)
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			PerfMeterCaptureBackendV2Snapshot snapshot = backend.GetCapability(CreateOptions());
			PerfMeterRenderDocCapabilitySnapshot details = backend.CapabilityDetails;

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(snapshot.NativeResultCode, Is.EqualTo((int)SggRdResult.Ok));
			Assert.That(details.ApiMajor, Is.EqualTo(1u));
			Assert.That(details.ApiMinor, Is.EqualTo((uint)apiMinor));
			Assert.That(details.SupportsDiscard, Is.True);
			Assert.That(details.SupportsComments, Is.True);
			Assert.That(details.SupportsTitle, Is.EqualTo(supportsTitle));
			Assert.That(details.SupportsAnnotations, Is.EqualTo(supportsAnnotations));
		}

		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void MissingModuleExportOrApiIsReportedWithoutFallback(int resultCode)
		{
			SggRdResult result = (SggRdResult)resultCode;
			FakeBridge bridge = new FakeBridge
			{
				CapabilitiesResult = result
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			PerfMeterCaptureBackendV2Snapshot snapshot = backend.GetCapability(CreateOptions());

			Assert.That(snapshot.Availability, Is.EqualTo(PerfMeterAvailability.Unavailable));
			Assert.That(snapshot.NativeResultCode, Is.EqualTo((int)result));
			Assert.That(snapshot.FallbackReason, Is.Empty);
		}

		[Test]
		public void CapabilityFieldsTruthfullyMapMissingModuleAndMandatoryDiscard()
		{
			FakeBridge bridge = new FakeBridge
			{
				Capabilities = ReadyCapabilities(7, true, true)
			};
			bridge.Capabilities.ModuleLoaded = 0u;
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.GetCapability(CreateOptions()).NativeResultCode, Is.EqualTo((int)SggRdResult.NotLoaded));

			bridge.Capabilities = ReadyCapabilities(7, true, true);
			bridge.Capabilities.SupportsDiscard = 0u;
			Assert.That(backend.GetCapability(CreateOptions()).NativeResultCode, Is.EqualTo((int)SggRdResult.ApiNegotiationFailed));

			bridge.Capabilities = ReadyCapabilities(7, true, true);
			bridge.Capabilities.SupportsComments = 0u;
			Assert.That(backend.GetCapability(CreateOptions()).NativeResultCode, Is.EqualTo((int)SggRdResult.ApiNegotiationFailed));
		}

		[Test]
		public void CapabilityRejectsFeatureBitAndApiLevelTitleMismatches()
		{
			FakeBridge bridge = new FakeBridge();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			bridge.Capabilities.FeatureFlags &= ~(uint)SggRdFeatureBitsV1.Comments;
			Assert.That(backend.GetCapability(CreateOptions()).Warning, Does.Contain("inconsistent"));

			bridge.Capabilities = ReadyCapabilities(7u, false, true);
			Assert.That(backend.GetCapability(CreateOptions()).Availability, Is.EqualTo(PerfMeterAvailability.Available));

			bridge.Capabilities = ReadyCapabilities(4u, true, false);
			Assert.That(backend.GetCapability(CreateOptions()).NativeResultCode, Is.EqualTo((int)SggRdResult.ApiNegotiationFailed));

			bridge.Capabilities = ReadyCapabilities(7u, true, true);
			bridge.Capabilities.SupportsAnnotations = 2u;
			Assert.That(backend.GetCapability(CreateOptions()).NativeResultCode, Is.EqualTo((int)SggRdResult.ApiNegotiationFailed));
		}

		[Test]
		public void ProductionPreflightRemainsFailClosedUntilWorkerLifecycleWiring()
		{
			PerfMeterRenderDocPreflightProvider provider = new PerfMeterRenderDocPreflightProvider();
			SggRdResult result = provider.Prepare(
				new PerfMeterCaptureOptions("a capture with a bounded title", PerfMeterCaptureTool.RenderDoc),
				out PerfMeterRenderDocPreflight preflight);

			Assert.That(result, Is.EqualTo(SggRdResult.InternalError));
			Assert.That(provider.Storage, Is.Null);
			Assert.That(preflight.RequestNonce, Is.Zero);
			Assert.That(preflight.CapturePathTemplate, Is.Null);
			Assert.That(preflight.Title, Is.Null);
			Assert.That(preflight.Reservation, Is.Null);
			Assert.That(PerfMeterRenderDocPreflightProvider.PolicyNotReadyMessage, Does.Contain("PM-RDOC-003C/003D"));
			Assert.That(PerfMeterRenderDocPreflightProvider.PolicyNotReadyMessage, Does.Contain("worker/lifecycle"));
		}

		[Test]
		public void PreflightFailureStopsBeforeBridgeBegin()
		{
			FakeBridge bridge = new FakeBridge();
			FakePreflight preflight = new FakePreflight
			{
				Result = SggRdResult.InternalError
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform(), preflight);

			Assert.That(backend.GetCapability(CreateOptions()).Availability, Is.EqualTo(PerfMeterAvailability.Available));
			Assert.That(backend.TryBegin(CreateOptions(), out string error), Is.False);
			Assert.That(error, Does.Contain("PM-RDOC-003C/003D"));
			Assert.That(bridge.BeginCount, Is.Zero);
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Failed));
			Assert.That(backend.Snapshot.NativeResultCode, Is.EqualTo((int)SggRdResult.InternalError));
		}

		[Test]
		public void SuccessfulFakeBeginUsesPreflightTokenAndRequiresEndOfFrame()
		{
			FakeBridge bridge = new FakeBridge();
			FakePreflight preflight = new FakePreflight();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform(), preflight);

			Assert.That(backend.TryBegin(CreateOptions(), out string error), Is.True, error);
			Assert.That(bridge.BeginCount, Is.EqualTo(1));
			Assert.That(bridge.LastNonce, Is.EqualTo(preflight.Data.RequestNonce));
			Assert.That(bridge.LastPathTemplate, Is.EqualTo(preflight.Data.CapturePathTemplate));
			Assert.That(bridge.LastTitle, Is.EqualTo(preflight.Data.Title));
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.BeginExecuted));
			Assert.That(backend.Snapshot.RequiresEndOfFrame, Is.True);
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);
		}

		[TestCase(4, false, "")]
		[TestCase(6, true, "RenderDoc fake capture")]
		[TestCase(7, false, "")]
		public void BeginUsesTitleOnlyWhenCapabilitySupportsIt(int apiMinor, bool supportsTitle, string expectedTitle)
		{
			FakeBridge bridge = new FakeBridge
			{
				Capabilities = ReadyCapabilities((uint)apiMinor, supportsTitle, false)
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			Assert.That(backend.TryBegin(CreateOptions(), out string error), Is.True, error);

			Assert.That(bridge.LastTitle, Is.EqualTo(expectedTitle));
		}

		[Test]
		public void OkWithInvalidTokenRemainsActiveAndRequiresDiscard()
		{
			FakeBridge bridge = new FakeBridge
			{
				ReturnInvalidBeginToken = true
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			Assert.That(backend.TryBegin(CreateOptions(), out string error), Is.False);
			Assert.That(error, Does.Contain("invalid capture token"));
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Failed));
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);
			Assert.That(backend.ScheduleEnd(out string endError), Is.False);
			Assert.That(endError, Does.Contain("invalid capture token"));
			Assert.That(bridge.EndCount, Is.Zero);
			Assert.That(backend.TryDiscard(out string discardError), Is.True, discardError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
		}

		[Test]
		public void BeginExceptionRemainsActiveAndRequiresDiscardAttempt()
		{
			FakeBridge bridge = new FakeBridge
			{
				ThrowOnBegin = new InvalidOperationException("uncertain fake begin")
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());

			Assert.That(backend.TryBegin(CreateOptions(), out string error), Is.False);
			Assert.That(error, Does.Contain("InvalidOperationException"));
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Failed));
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);
			Assert.That(backend.ScheduleEnd(out string endError), Is.False);
			Assert.That(endError, Does.Contain("uncertain fake begin"));
			Assert.That(bridge.EndCount, Is.Zero);
			Assert.That(backend.TryDiscard(out string discardError), Is.True, discardError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
		}

		[Test]
		public void ScheduleEndIsExactlyOnceAndCompletesControlOnlyBackend()
		{
			FakeBridge bridge = new FakeBridge();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.ScheduleEnd(out string firstError), Is.True, firstError);
			Assert.That(backend.ScheduleEnd(out string secondError), Is.True, secondError);
			Assert.That(bridge.EndCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.Completed));
			Assert.That(backend.Snapshot.RequiresEndOfFrame, Is.False);
			Assert.That(backend.Snapshot.HasPendingCompletion, Is.False);
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
		}

		[Test]
		public void DiscardIsMandatoryForBegunCaptureAndClearsResources()
		{
			FakeBridge bridge = new FakeBridge();
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.TryDiscard(out string discardError), Is.True, discardError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
			Assert.That(backend.TryDiscard(out discardError), Is.True, discardError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
		}

		[Test]
		public void DiscardFailureRemainsRetryable()
		{
			FakeBridge bridge = new FakeBridge { DiscardResult = SggRdResult.CaptureFailed };
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.TryDiscard(out string firstError), Is.False);
			Assert.That(firstError, Does.Contain("capture operation failed"));
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);

			bridge.DiscardResult = SggRdResult.Ok;
			Assert.That(backend.TryDiscard(out string retryError), Is.True, retryError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(2));
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
		}

		[TestCase((int)SggRdResult.ExportMissing)]
		[TestCase((int)SggRdResult.UnsupportedPlatform)]
		[TestCase((int)SggRdResult.InternalError)]
		public void NonCaptureFailureDiscardResultIsNotRetried(int resultCode)
		{
			FakeBridge bridge = new FakeBridge { DiscardResult = (SggRdResult)resultCode };
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.TryDiscard(out _), Is.False);
			bridge.DiscardResult = SggRdResult.Ok;
			Assert.That(backend.TryDiscard(out _), Is.False);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);
		}

		[Test]
		public void DiscardExceptionIsNotRetried()
		{
			FakeBridge bridge = new FakeBridge { ThrowOnDiscard = new InvalidOperationException("uncertain discard") };
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.TryDiscard(out string firstError), Is.False);
			Assert.That(firstError, Does.Contain("uncertain discard"));
			bridge.ThrowOnDiscard = null;
			Assert.That(backend.TryDiscard(out _), Is.False);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.HasActiveResources, Is.True);
		}

		[Test]
		public void NotCapturingDiscardReleasesOwnershipAsLostSession()
		{
			FakeBridge bridge = new FakeBridge { DiscardResult = SggRdResult.NotCapturing };
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			Assert.That(backend.TryBegin(CreateOptions(), out string beginError), Is.True, beginError);

			Assert.That(backend.TryDiscard(out string discardError), Is.True, discardError);
			Assert.That(bridge.DiscardCount, Is.EqualTo(1));
			Assert.That(backend.Snapshot.NativePhase, Is.EqualTo(PerfMeterRenderDocCapturePhase.LostSession));
			Assert.That(backend.Snapshot.NativeResultCode, Is.EqualTo((int)SggRdResult.NotCapturing));
			Assert.That(backend.Snapshot.HasActiveResources, Is.False);
		}

		[Test]
		public void Utf8TitleLimitRejectsWithoutTruncation()
		{
			FakeBridge acceptedBridge = new FakeBridge();
			FakePreflight acceptedPreflight = new FakePreflight
			{
				Data = new PerfMeterRenderDocPreflight(1u, "C:\\Project\\capture", new string('é', 128))
			};
			PerfMeterRenderDocCaptureBackend acceptedBackend = CreateBackend(acceptedBridge, SupportedPlatform(), acceptedPreflight);
			Assert.That(PerfMeterRenderDocUtf8.GetByteCount(acceptedPreflight.Data.Title), Is.EqualTo(256));
			Assert.That(acceptedBackend.TryBegin(CreateOptions(), out string acceptedError), Is.True, acceptedError);

			FakeBridge rejectedBridge = new FakeBridge();
			FakePreflight rejectedPreflight = new FakePreflight
			{
				Data = new PerfMeterRenderDocPreflight(2u, "C:\\Project\\capture", new string('é', 129))
			};
			PerfMeterRenderDocCaptureBackend rejectedBackend = CreateBackend(rejectedBridge, SupportedPlatform(), rejectedPreflight);
			Assert.That(rejectedBackend.TryBegin(CreateOptions(), out string rejectedError), Is.False);
			Assert.That(rejectedError, Does.Contain("UTF-8 byte limit"));
			Assert.That(rejectedBridge.BeginCount, Is.Zero);
		}

		[Test]
		public void ArtifactPathBufferValidationIsBoundedAndStrict()
		{
			Assert.That(
				PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(1u, out int oneByte),
				Is.True);
			Assert.That(oneByte, Is.EqualTo(1));
			Assert.That(
				PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(
					(uint)PerfMeterRenderDocAbiV1.MaxPathBytes,
					out int maximumBytes),
				Is.True);
			Assert.That(maximumBytes, Is.EqualTo(PerfMeterRenderDocAbiV1.MaxPathBytes));
			Assert.That(
				PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(0u, out _),
				Is.False);
			Assert.That(
				PerfMeterRenderDocUtf8.TryValidateArtifactPathBytes(
					(uint)PerfMeterRenderDocAbiV1.MaxPathBytes + 1u,
					out _),
				Is.False);
			Assert.That(
				PerfMeterRenderDocUtf8.TryDecodeOutput(
					new byte[] { (byte)'C', 0 },
					2u,
					out string decodedPath),
				Is.True);
			Assert.That(decodedPath, Is.EqualTo("C"));
			Assert.That(
				PerfMeterRenderDocUtf8.TryDecodeOutput(
					new byte[] { 0xC3, 0x28, 0 },
					3u,
					out _),
				Is.False);
		}

		[Test]
		public void BridgeAndBackendContainInteropExceptions()
		{
			Assert.That(
				PerfMeterRenderDocPInvokeBridge.MapInteropException(new DllNotFoundException()),
				Is.EqualTo(SggRdResult.UnsupportedPlatform));
			Assert.That(
				PerfMeterRenderDocPInvokeBridge.MapInteropException(new EntryPointNotFoundException()),
				Is.EqualTo(SggRdResult.ExportMissing));
			Assert.That(
				PerfMeterRenderDocPInvokeBridge.MapInteropException(new BadImageFormatException()),
				Is.EqualTo(SggRdResult.UnsupportedPlatform));

			FakeBridge bridge = new FakeBridge
			{
				ThrowOnCapabilities = new InvalidOperationException("fake capability failure")
			};
			PerfMeterRenderDocCaptureBackend backend = CreateBackend(bridge, SupportedPlatform());
			PerfMeterCaptureBackendV2Snapshot snapshot = backend.GetCapability(CreateOptions());
			Assert.That(snapshot.NativeResultCode, Is.EqualTo((int)SggRdResult.InternalError));
			Assert.That(snapshot.Warning, Does.Contain("InvalidOperationException"));
		}

		[Test]
		public void ArtifactAndCommentsOperationsAreExposedOnBridgeContract()
		{
			FakeBridge bridge = new FakeBridge();
			SggRdCaptureTokenV1 token = new SggRdCaptureTokenV1
			{
				StructSize = PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt,
				RequestNonce = 1u
			};

			Assert.That(bridge.TryGetNewArtifact(token, out SggRdArtifactV1 artifact, out string path), Is.EqualTo(SggRdResult.CaptureNotObserved));
			Assert.That(artifact.StructSize, Is.EqualTo(PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt));
			Assert.That(path, Is.Empty);
			Assert.That(bridge.SetCaptureComments(token, "C:\\Project\\capture.rdc", "comments"), Is.EqualTo(SggRdResult.Ok));
			Assert.That(bridge.ArtifactQueryCount, Is.EqualTo(1));
			Assert.That(bridge.CommentsCount, Is.EqualTo(1));
		}

		[Test]
		public void ConstructingManagedBackendDoesNotRegisterBootstrap()
		{
			Assert.That(PerfMeterNativeCaptureBackendRegistry.TryGet(out _), Is.False);
			_ = CreateBackend(new FakeBridge(), SupportedPlatform());
			Assert.That(PerfMeterNativeCaptureBackendRegistry.TryGet(out _), Is.False);
		}

		private static PerfMeterRenderDocCaptureBackend CreateBackend(
			FakeBridge bridge,
			FakePlatformProvider platformProvider,
			FakePreflight preflight = null)
		{
			return new PerfMeterRenderDocCaptureBackend(
				bridge,
				preflight ?? new FakePreflight(),
				platformProvider);
		}

		private static FakePlatformProvider SupportedPlatform()
		{
			return new FakePlatformProvider(new PerfMeterRenderDocPlatformInfo(
				RuntimePlatform.WindowsEditor,
				GraphicsDeviceType.Direct3D11,
				true,
				true));
		}

		private static PerfMeterCaptureOptions CreateOptions()
		{
			return new PerfMeterCaptureOptions(
				"renderdoc-test",
				PerfMeterCaptureTool.RenderDoc,
				1,
				0,
				0,
				PerfMeterCaptureBackendMode.NativeRequired);
		}

		private static SggRdCapabilitiesV1 ReadyCapabilities(uint apiMinor, bool supportsTitle, bool supportsAnnotations)
		{
			SggRdFeatureBitsV1 featureFlags = SggRdFeatureBitsV1.Discard | SggRdFeatureBitsV1.Comments;
			if (supportsTitle)
			{
				featureFlags |= SggRdFeatureBitsV1.Title;
			}
			if (supportsAnnotations)
			{
				featureFlags |= SggRdFeatureBitsV1.Annotations;
			}

			return new SggRdCapabilitiesV1
			{
				StructSize = PerfMeterRenderDocAbiV1.CapabilitiesSizeAsUInt,
				BridgeAbiMajor = PerfMeterRenderDocAbiV1.AbiMajor,
				BridgeAbiMinor = PerfMeterRenderDocAbiV1.AbiMinor,
				PlatformSupported = 1u,
				ModuleLoaded = 1u,
				ExportAvailable = 1u,
				ApiNegotiated = 1u,
				TargetControlConnected = 1u,
				IsCapturing = 0u,
				ApiMajor = 1u,
				ApiMinor = apiMinor,
				ApiPatch = 0u,
				FeatureFlags = (uint)featureFlags,
				SupportsDiscard = 1u,
				SupportsComments = 1u,
				SupportsTitle = supportsTitle ? 1u : 0u,
				SupportsAnnotations = supportsAnnotations ? 1u : 0u,
				CaptureCount = 0u
			};
		}

		private static void AssertOffsets<T>(params (string Name, int Offset)[] expected)
			where T : struct
		{
			foreach ((string name, int offset) in expected)
			{
				Assert.That(Marshal.OffsetOf(typeof(T), name).ToInt32(), Is.EqualTo(offset), name);
			}
		}

		private sealed class FakePlatformProvider : IPerfMeterRenderDocPlatformProvider
		{
			private readonly PerfMeterRenderDocPlatformInfo _platformInfo;

			internal FakePlatformProvider(PerfMeterRenderDocPlatformInfo platformInfo)
			{
				_platformInfo = platformInfo;
			}

			public PerfMeterRenderDocPlatformInfo GetPlatformInfo()
			{
				return _platformInfo;
			}
		}

		private sealed class FakePreflight : IPerfMeterRenderDocPreflightProvider
		{
			internal SggRdResult Result = SggRdResult.Ok;
			internal PerfMeterRenderDocPreflight Data = new PerfMeterRenderDocPreflight(
				0x1020304050607080u,
				"C:\\Project\\Temp\\PerfMeter\\RenderDoc\\1020304050607080\\capture",
				"RenderDoc fake capture");
			internal int PrepareCount;

			public SggRdResult Prepare(PerfMeterCaptureOptions options, out PerfMeterRenderDocPreflight preflight)
			{
				PrepareCount++;
				preflight = Data;
				return Result;
			}
		}

		private sealed class FakeBridge : IPerfMeterRenderDocBridge
		{
			internal SggRdCapabilitiesV1 Capabilities = ReadyCapabilities(7, true, true);
			internal SggRdResult CapabilitiesResult = SggRdResult.Ok;
			internal SggRdResult BeginResult = SggRdResult.Ok;
			internal SggRdResult EndResult = SggRdResult.Ok;
			internal SggRdResult DiscardResult = SggRdResult.Ok;
			internal Exception ThrowOnCapabilities;
			internal Exception ThrowOnBegin;
			internal Exception ThrowOnEnd;
			internal Exception ThrowOnDiscard;
			internal bool ReturnInvalidBeginToken;
			internal int GetCapabilitiesCount;
			internal int BeginCount;
			internal int EndCount;
			internal int DiscardCount;
			internal int ArtifactQueryCount;
			internal int CommentsCount;
			internal ulong LastNonce;
			internal string LastPathTemplate;
			internal string LastTitle;

			public SggRdResult GetCapabilities(out SggRdCapabilitiesV1 capabilities)
			{
				GetCapabilitiesCount++;
				if (ThrowOnCapabilities != null)
				{
					throw ThrowOnCapabilities;
				}

				capabilities = Capabilities;
				return CapabilitiesResult;
			}

			public SggRdResult BeginCapture(ulong requestNonce, string capturePathTemplate, string title, out SggRdCaptureTokenV1 token)
			{
				BeginCount++;
				LastNonce = requestNonce;
				LastPathTemplate = capturePathTemplate;
				LastTitle = title;
				if (ThrowOnBegin != null)
				{
					throw ThrowOnBegin;
				}

				token = BeginResult == SggRdResult.Ok
					? new SggRdCaptureTokenV1
					{
						StructSize = PerfMeterRenderDocAbiV1.CaptureTokenSizeAsUInt,
						RequestNonce = ReturnInvalidBeginToken ? requestNonce + 1u : requestNonce,
						CountBefore = 7u,
						StartUnixNanoseconds = 99u
					}
					: default;
				return BeginResult;
			}

			public SggRdResult EndCapture(SggRdCaptureTokenV1 token)
			{
				EndCount++;
				if (ThrowOnEnd != null)
				{
					throw ThrowOnEnd;
				}

				return EndResult;
			}

			public SggRdResult DiscardCapture(SggRdCaptureTokenV1 token)
			{
				DiscardCount++;
				if (ThrowOnDiscard != null)
				{
					throw ThrowOnDiscard;
				}

				return DiscardResult;
			}

			public SggRdResult TryGetNewArtifact(SggRdCaptureTokenV1 token, out SggRdArtifactV1 artifact, out string observedPath)
			{
				ArtifactQueryCount++;
				artifact = new SggRdArtifactV1
				{
					StructSize = PerfMeterRenderDocAbiV1.ArtifactSizeAsUInt
				};
				observedPath = string.Empty;
				return SggRdResult.CaptureNotObserved;
			}

			public SggRdResult SetCaptureComments(SggRdCaptureTokenV1 token, string observedPath, string comments)
			{
				CommentsCount++;
				return SggRdResult.Ok;
			}
		}
	}

	internal static class RenderDocInteropTestReflectionExtensions
	{
		internal static StructLayoutAttribute StructLayout(this Type type)
		{
			return type.StructLayoutAttribute;
		}
	}
}
