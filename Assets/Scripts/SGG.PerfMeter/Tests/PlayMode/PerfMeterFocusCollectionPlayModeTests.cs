using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SGG.PerfMeter.Tests.PlayMode
{
	public sealed class PerfMeterFocusCollectionPlayModeTests
	{
		private bool _originalRunInBackground;

		[SetUp]
		public void SetUp()
		{
			PerformanceMeter.Stop();
			_originalRunInBackground = Application.runInBackground;
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[TearDown]
		public void TearDown()
		{
			PerformanceMeter.Stop();
			Application.runInBackground = _originalRunInBackground;
			PerfMeterSettingsBootstrap.ResetExplicitSettingsApplication();
			PerformanceMeter.ClearCustomMetricProviders();
		}

		[UnityTest]
		public IEnumerator DefaultCollectionContinuesWithoutFocusButOptOutAndPauseStopSampling()
		{
			Application.runInBackground = false;
			PerformanceMeter.EnsureRunning();
			Assert.That(PerformanceMeter.CollectWhenUnfocused, Is.True);
			Assert.That(Application.runInBackground, Is.True);
			CountingProvider provider = new CountingProvider();
			PerformanceMeter.RegisterCustomMetricProvider(provider);
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			SetField(runtime, "_applicationFocused", false);
			SetField(runtime, "_applicationPaused", false);
			yield return WaitForSample(provider);
			Assert.That(PerformanceMeter.GetStatus().ApplicationFocused, Is.False);

			PerformanceMeter.SetCollectWhenUnfocused(false);
			Assert.That(Application.runInBackground, Is.False);
			int stoppedCount = provider.Count;
			for (int frame = 0; frame < 4; frame++) yield return null;
			Assert.That(provider.Count, Is.EqualTo(stoppedCount));
			Assert.That(PerformanceMeter.GetStatus().FrameTimingAvailability, Is.EqualTo(PerfMeterFrameTimingAvailability.NotCollected));

			PerformanceMeter.SetCollectWhenUnfocused(true);
			yield return WaitForSample(provider);
			SetField(runtime, "_applicationPaused", true);
			stoppedCount = provider.Count;
			for (int frame = 0; frame < 4; frame++) yield return null;
			Assert.That(provider.Count, Is.EqualTo(stoppedCount));
			Assert.That(PerformanceMeter.GetStatus().ApplicationPaused, Is.True);
			PerformanceMeter.Stop();
			Assert.That(Application.runInBackground, Is.False);
		}

		[UnityTest]
		public IEnumerator BackgroundExecutionOwnershipRestoresOnDisableDestroyAndStop()
		{
			Application.runInBackground = false;
			PerformanceMeter.EnsureRunning();
			PerfMeterRuntime runtime = PerfMeterRuntime.Instance;
			Assert.That(Application.runInBackground, Is.True);
			runtime.enabled = false;
			Assert.That(Application.runInBackground, Is.False);
			runtime.enabled = true;
			Assert.That(Application.runInBackground, Is.True);
			Object.Destroy(runtime.gameObject);
			yield return null;
			Assert.That(Application.runInBackground, Is.False);

			Application.runInBackground = true;
			PerformanceMeter.EnsureRunning();
			PerformanceMeter.SetCollectWhenUnfocused(false);
			Assert.That(Application.runInBackground, Is.True);
			PerformanceMeter.SetCollectWhenUnfocused(true);
			Application.runInBackground = false;
			PerformanceMeter.Stop();
			Assert.That(Application.runInBackground, Is.False, "A later external false must not be overwritten during cleanup.");
		}

		private static IEnumerator WaitForSample(CountingProvider provider)
		{
			int previous = provider.Count;
			double deadline = Time.realtimeSinceStartupAsDouble + 5d;
			while (provider.Count == previous && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
			Assert.That(provider.Count, Is.GreaterThan(previous), "The live runtime must sample custom metrics while unfocused.");
		}

		private static void SetField(PerfMeterRuntime runtime, string name, bool value)
		{
			typeof(PerfMeterRuntime).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(runtime, value);
		}

		private sealed class CountingProvider : IPerfMeterCustomMetricProvider
		{
			public string Id => "tests.focus.counter";
			internal int Count;
			public bool TryCollect(out PerfMeterCustomMetricSnapshot metric)
			{
				metric = new PerfMeterCustomMetricSnapshot(Id, "Focus test", "Tests", "count", ++Count, true);
				return true;
			}
		}
	}
}