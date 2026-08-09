using System;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterExternalArtifactTests
	{
		[Test]
		public void EnumValuesRemainStable()
		{
			Assert.That((int)PerfMeterExternalArtifactKind.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactKind.GpuCapture, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactKind.ProfilerTrace, Is.EqualTo(2));
			Assert.That((int)PerfMeterExternalArtifactKind.MemorySnapshot, Is.EqualTo(3));
			Assert.That((int)PerfMeterExternalArtifactKind.Other, Is.EqualTo(4));

			Assert.That((int)PerfMeterExternalArtifactFinalizationState.Unavailable, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactFinalizationState.Observed, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactFinalizationState.Finalized, Is.EqualTo(2));
			Assert.That((int)PerfMeterExternalArtifactFinalizationState.Failed, Is.EqualTo(3));

			Assert.That((int)PerfMeterExternalArtifactAuthorityState.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactAuthorityState.Observed, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactAuthorityState.Authenticated, Is.EqualTo(2));

			Assert.That((int)PerfMeterExternalArtifactAssociationState.None, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactAssociationState.Unverified, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactAssociationState.ToolAuthenticated, Is.EqualTo(2));
			Assert.That((int)PerfMeterExternalArtifactAssociationState.BridgeAuthenticated, Is.EqualTo(3));

			Assert.That((int)PerfMeterExternalArtifactContentState.Unknown, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactContentState.Absent, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactContentState.Present, Is.EqualTo(2));

			Assert.That((int)PerfMeterExternalArtifactStorageMode.MetadataOnly, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactStorageMode.Copy, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactStorageMode.Embed, Is.EqualTo(2));

			Assert.That((int)PerfMeterExternalArtifactSharePolicy.DoNotShare, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactSharePolicy.ProjectLocalOnly, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare, Is.EqualTo(2));

			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.None, Is.EqualTo(0));
			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData, Is.EqualTo(1));
			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.ContainsProcessMemory, Is.EqualTo(2));
			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.Sensitive, Is.EqualTo(4));
			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.RequiresReview, Is.EqualTo(8));
			Assert.That((int)PerfMeterExternalArtifactPrivacyFlags.All, Is.EqualTo(15));
		}

		[Test]
		public void OptionsNormalizeValuesAndUseSafeDefaults()
		{
			PerfMeterExternalArtifactOptions options = new PerfMeterExternalArtifactOptions(
				artifactId: null,
				artifactKind: (PerfMeterExternalArtifactKind)99,
				toolId: new string('t', PerfMeterExternalArtifactOptions.MaxToolIdLength + 10),
				associationState: (PerfMeterExternalArtifactAssociationState)99,
				finalizationState: (PerfMeterExternalArtifactFinalizationState)99,
				authorityState: (PerfMeterExternalArtifactAuthorityState)99,
				containsGpuCaptureData: (PerfMeterExternalArtifactContentState)99,
				privacyFlags: (PerfMeterExternalArtifactPrivacyFlags)(-1),
				storageMode: (PerfMeterExternalArtifactStorageMode)99,
				quotaBytes: long.MaxValue,
				sharePolicy: (PerfMeterExternalArtifactSharePolicy)99,
				sizeBytes: -1L,
				observedSourceSha256: "not-a-hash",
				postCopySha256: "not-a-hash");

			Assert.That(options.ArtifactId, Is.Empty);
			Assert.That(options.ToolId, Has.Length.EqualTo(PerfMeterExternalArtifactOptions.MaxToolIdLength));
			Assert.That(options.ArtifactKind, Is.EqualTo(PerfMeterExternalArtifactKind.Unknown));
			Assert.That(options.AssociationState, Is.EqualTo(PerfMeterExternalArtifactAssociationState.None));
			Assert.That(options.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Unavailable));
			Assert.That(options.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Unknown));
			Assert.That(options.ContainsGpuCaptureData, Is.EqualTo(PerfMeterExternalArtifactContentState.Unknown));
			Assert.That(options.PrivacyFlags, Is.EqualTo(PerfMeterExternalArtifactPrivacyFlags.ContainsGpuCaptureData |
				PerfMeterExternalArtifactPrivacyFlags.ContainsProcessMemory |
				PerfMeterExternalArtifactPrivacyFlags.Sensitive |
				PerfMeterExternalArtifactPrivacyFlags.RequiresReview));
			Assert.That(options.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.MetadataOnly));
			Assert.That(options.QuotaBytes, Is.EqualTo(PerfMeterExternalArtifactOptions.MaxArtifactSizeBytes));
			Assert.That(options.SizeBytes, Is.Zero);
			Assert.That(options.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare));
			Assert.That(options.ObservedSourceSha256, Is.Empty);
			Assert.That(options.PostCopySha256, Is.Empty);
		}

		[Test]
		public void LegacyObservedArtifactRemainsUnverifiedAndContentUnknown()
		{
			const string sourceHash = "ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD";
			const string copyHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
			PerfMeterExternalArtifactSnapshot snapshot = PerfMeterExternalArtifactSnapshot.LegacyObserved(
				"legacy-artifact",
				"capture-01",
				42L,
				sourceHash,
				copyHash);

			Assert.That(snapshot.ArtifactKind, Is.EqualTo(PerfMeterExternalArtifactKind.GpuCapture));
			Assert.That(snapshot.AssociationState, Is.EqualTo(PerfMeterExternalArtifactAssociationState.Unverified));
			Assert.That(snapshot.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Observed));
			Assert.That(snapshot.IsAuthoritative, Is.False);
			Assert.That(snapshot.HasAuthenticatedAssociation, Is.False);
			Assert.That(snapshot.ContainsGpuCaptureData, Is.EqualTo(PerfMeterExternalArtifactContentState.Unknown));
			Assert.That(snapshot.FinalizationState, Is.EqualTo(PerfMeterExternalArtifactFinalizationState.Finalized));
			Assert.That(snapshot.HasPostCopyHash, Is.True);
			Assert.That(snapshot.ObservedSourceSha256, Is.EqualTo(sourceHash.ToLowerInvariant()));
			Assert.That(snapshot.PostCopySha256, Is.EqualTo(copyHash));
			Assert.That(snapshot.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.Embed));
			Assert.That(snapshot.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.ReviewBeforeShare));
		}

		[Test]
		public void AuthorityRequiresToolOrBridgeAuthenticatedAssociation()
		{
			PerfMeterExternalArtifactSnapshot unverified = new PerfMeterExternalArtifactOptions(
				artifactId: "unverified",
				associationState: PerfMeterExternalArtifactAssociationState.Unverified,
				authorityState: PerfMeterExternalArtifactAuthorityState.Authenticated).ToSnapshot();
			PerfMeterExternalArtifactSnapshot toolAuthenticated = new PerfMeterExternalArtifactOptions(
				artifactId: "tool",
				associationState: PerfMeterExternalArtifactAssociationState.ToolAuthenticated,
				authorityState: PerfMeterExternalArtifactAuthorityState.Authenticated).ToSnapshot();
			PerfMeterExternalArtifactSnapshot bridgeAuthenticated = new PerfMeterExternalArtifactOptions(
				artifactId: "bridge",
				associationState: PerfMeterExternalArtifactAssociationState.BridgeAuthenticated,
				authorityState: PerfMeterExternalArtifactAuthorityState.Authenticated).ToSnapshot();

			Assert.That(unverified.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Unknown));
			Assert.That(unverified.IsAuthoritative, Is.False);
			Assert.That(toolAuthenticated.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Authenticated));
			Assert.That(toolAuthenticated.IsAuthoritative, Is.True);
			Assert.That(bridgeAuthenticated.AuthorityState, Is.EqualTo(PerfMeterExternalArtifactAuthorityState.Authenticated));
			Assert.That(bridgeAuthenticated.IsAuthoritative, Is.True);
		}

		[Test]
		public void ContentUnknownIsDistinctFromAbsentAndStoragePoliciesAreRepresentable()
		{
			PerfMeterExternalArtifactOptions metadataOnly = new PerfMeterExternalArtifactOptions(
				artifactId: "metadata",
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Unknown,
				storageMode: PerfMeterExternalArtifactStorageMode.MetadataOnly);
			PerfMeterExternalArtifactOptions copied = new PerfMeterExternalArtifactOptions(
				artifactId: "copy",
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Absent,
				storageMode: PerfMeterExternalArtifactStorageMode.Copy,
				sharePolicy: PerfMeterExternalArtifactSharePolicy.ProjectLocalOnly);
			PerfMeterExternalArtifactOptions embedded = new PerfMeterExternalArtifactOptions(
				artifactId: "embed",
				containsGpuCaptureData: PerfMeterExternalArtifactContentState.Present,
				storageMode: PerfMeterExternalArtifactStorageMode.Embed,
				sharePolicy: PerfMeterExternalArtifactSharePolicy.DoNotShare);

			Assert.That(metadataOnly.ContainsGpuCaptureData, Is.Not.EqualTo(PerfMeterExternalArtifactContentState.Absent));
			Assert.That(metadataOnly.ContainsGpuCaptureData, Is.EqualTo(PerfMeterExternalArtifactContentState.Unknown));
			Assert.That(copied.ContainsGpuCaptureData, Is.EqualTo(PerfMeterExternalArtifactContentState.Absent));
			Assert.That(embedded.ContainsGpuCaptureData, Is.EqualTo(PerfMeterExternalArtifactContentState.Present));
			Assert.That(copied.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.Copy));
			Assert.That(embedded.StorageMode, Is.EqualTo(PerfMeterExternalArtifactStorageMode.Embed));
			Assert.That(embedded.SharePolicy, Is.EqualTo(PerfMeterExternalArtifactSharePolicy.DoNotShare));
		}
	}
}
