using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRuntimeCleanupTests
	{
		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			Assert.That(OwnedInfrastructureCount(), Is.Zero);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void StopRecoversLostSingletonAndDeletesOwnedRuntime(bool disabled)
		{
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			if (disabled)
			{
				runtime.enabled = false;
			}
			LoseSingleton();
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped));

			PerfMeterMutationResultSnapshot result = PerformanceMeter.TryStop();

			Assert.That(result.Status, Is.EqualTo(PerfMeterMutationStatus.Applied));
			Assert.That(runtime == null, Is.True);
			Assert.That(PerfMeterRuntime.Instance, Is.Null);
			Assert.That(OwnedInfrastructureCount(), Is.Zero);
			Assert.That(PerformanceMeter.TryStop().Status, Is.EqualTo(PerfMeterMutationStatus.NoChange));
		}

		[Test]
		public void StopDeletesStandaloneOwnedHostAndLegacyTransientOverlay()
		{
			GameObject host = new GameObject(PerfMeterOverlayPanelHost.HostObjectName) { hideFlags = HideFlags.DontSave };
			host.SetActive(false);
			PerfMeterOwnedInfrastructure.Mark(host, PerfMeterInfrastructureKind.PanelHost);
			GameObject overlay = new GameObject("SGG PerfMeter Overlay") { hideFlags = HideFlags.DontSave };
			overlay.SetActive(false);
			overlay.AddComponent<PerfMeterOverlay>();

			Assert.That(PerformanceMeter.TryStop().Status, Is.EqualTo(PerfMeterMutationStatus.Applied));
			Assert.That(host == null, Is.True);
			Assert.That(overlay == null, Is.True);
			Assert.That(OwnedInfrastructureCount(), Is.Zero);
		}

		[Test]
		public void StopPreservesForeignUiWithMatchingNameAndHideFlags()
		{
			GameObject foreign = new GameObject(PerfMeterOverlayPanelHost.HostObjectName) { hideFlags = HideFlags.DontSave };
			foreign.SetActive(false);
			foreign.AddComponent<UIDocument>();
			try
			{
				Assert.That(PerformanceMeter.TryStop().Status, Is.EqualTo(PerfMeterMutationStatus.NoChange));
				Assert.That(foreign != null, Is.True);
				Assert.That(foreign.GetComponent<UIDocument>(), Is.Not.Null);
			}
			finally
			{
				Object.DestroyImmediate(foreign);
			}
		}

		[Test]
		public void StopRefusesTrackedAuthoredRuntimeWithoutAddingOwnership()
		{
			GameObject authored = new GameObject("Authored Runtime");
			PerfMeterRuntime runtime = authored.AddComponent<PerfMeterRuntime>();
			typeof(PerfMeterRuntime).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, runtime);
			try
			{
				Assert.That(PerfMeterRuntime.Instance, Is.SameAs(runtime));
				Assert.That(PerformanceMeter.TryStop().Status, Is.EqualTo(PerfMeterMutationStatus.Rejected));
				Assert.That(authored != null, Is.True);
				Assert.That(authored.GetComponent<PerfMeterOwnedInfrastructure>(), Is.Null);
			}
			finally
			{
				Object.DestroyImmediate(authored);
			}
		}

		[Test]
		public void StopPreservesUnownedNonTransientRuntimeAfterSingletonLoss()
		{
			GameObject authored = new GameObject("Authored Runtime");
			authored.SetActive(false);
			authored.AddComponent<PerfMeterRuntime>();
			LoseSingleton();
			try
			{
				Assert.That(PerformanceMeter.TryStop().Status, Is.EqualTo(PerfMeterMutationStatus.NoChange));
				Assert.That(authored != null, Is.True);
			}
			finally
			{
				Object.DestroyImmediate(authored);
			}
		}

		private static void LoseSingleton()
		{
			typeof(PerfMeterRuntime).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
		}

		private static int OwnedInfrastructureCount()
		{
			int count = 0;
			foreach (PerfMeterRuntime runtime in Resources.FindObjectsOfTypeAll<PerfMeterRuntime>())
			{
				if (runtime != null && PerfMeterOwnedInfrastructure.IsTransient(runtime.gameObject)) count++;
			}
			foreach (PerfMeterOverlay overlay in Resources.FindObjectsOfTypeAll<PerfMeterOverlay>())
			{
				if (overlay != null && PerfMeterOwnedInfrastructure.IsTransient(overlay.gameObject)) count++;
			}
			foreach (PerfMeterOwnedInfrastructure marker in Resources.FindObjectsOfTypeAll<PerfMeterOwnedInfrastructure>())
			{
				if (marker != null && PerfMeterOwnedInfrastructure.IsTransient(marker.gameObject)) count++;
			}
			return count;
		}
	}
}
