using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SGG.PerfMeter.Tests.URP.PlayMode
{
	// These are actual fixture lifetime controls, not timing or GPU-frame evidence.
	public sealed class UrpCameraPipelineScopeOwnershipPlayModeTests
	{
		private readonly List<Object> _foreignObjects = new List<Object>();
		private readonly List<string> _cleanupErrors = new List<string>();
		private UrpCameraPipelineScope _scope;
		private ScopeLifetimeFeature _feature;
		private Action _undoTestMutation;
		private UrpCameraPipelineControlCustody.Graph _controls;
		private bool _admitted;
		private RenderPipelineAsset _originalQuality, _originalDefault;
		private RenderPipelineGlobalSettings _originalGlobals;
		private int _originalLevel;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			_admitted = false;
			UrpCameraPipelineControlCustody.RequireEmpty();
			Assert.That(UrpCameraPipelineScope.RetainedScopeCount, Is.Zero, "An earlier scope requires cleanup before another test can own global state.");
			_scope = null; _feature = null; _undoTestMutation = null;
			_controls = null;
			_foreignObjects.Clear(); _cleanupErrors.Clear();
			Assert.That(Application.isPlaying, Is.True);
			if (!UrpCameraPipelineScope.HasActiveUrp) Assert.Ignore("URP configuration is required for this lifetime fixture.");
			Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "A real automatic camera loop is required.");
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Stopped), "Foreign runtime must remain untouched.");
			Assert.That(PerformanceMeter.GetSessionSummary().SessionId, Is.Empty, "Retained foreign session must remain untouched.");
			Type runtimeType = typeof(PerformanceMeter).Assembly.GetType("SGG.PerfMeter.PerfMeterRuntime", true);
			Assert.That(Resources.FindObjectsOfTypeAll(runtimeType), Is.Empty, "A disabled or hidden foreign runtime must remain untouched.");
			Assert.That(PerformanceMeter.GetCaptureStatus().CaptureId, Is.Empty);
			Assert.That(PerformanceMeter.GetMemorySnapshotStatus().IsActive, Is.False);
			var graphics = PerformanceMeter.GetGraphicsStateCollectionStatus();
			Assert.That(graphics.IsBusy || graphics.HasPendingCleanup, Is.False);
			_originalLevel = QualitySettings.GetQualityLevel();
			_originalQuality = QualitySettings.renderPipeline; _originalDefault = GraphicsSettings.defaultRenderPipeline;
			_originalGlobals = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
			_admitted = true;
			yield break;
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator StableOwnerRestoresAndReleasesItsObjects()
		{
			Create();
			yield return _scope.Restore();
			_scope.AssertRestored();
			Assert.That(_scope.HasOwnedAssets, Is.False);
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator RunningRuntimeIsNotStoppedToForceScopeRetirement()
		{
			Create();
			Type runtimeType = typeof(PerformanceMeter).Assembly.GetType("SGG.PerfMeter.PerfMeterRuntime", true);
			Object ownedRuntime = null;
			_undoTestMutation = () =>
			{
				Object[] current = Resources.FindObjectsOfTypeAll(runtimeType);
				if (current.Length == 0) { UrpCameraPipelineScope.RequireSdkQuiescent(); return; }
				if (ownedRuntime == null || current.Length != 1 || current[0] != ownedRuntime)
					throw new InvalidOperationException("Startup identity was not observed or was replaced; runtime retained without forced Stop.");
				PerformanceMeter.Stop();
			};
			PerformanceMeter.EnsureRunning();
			ownedRuntime = Resources.FindObjectsOfTypeAll(runtimeType).Single();
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Running));
			yield return _scope.Restore();
			AssertRetained();
			Assert.That(PerformanceMeter.GetStatus().State, Is.EqualTo(PerfMeterRuntimeState.Running), "The scope must not stop another owner.");
			Assert.That(QualitySettings.renderPipeline, Is.SameAs(_scope.OwnedPipeline));
			Assert.That(EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(), Is.SameAs(_scope.OwnedGlobals));
			UndoTestMutation();
			yield return _scope.Restore();
			_scope.AssertRestored();
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator ForeignCurrentSlotAndGlobalsArePreservedAndCleanupCanRetry()
		{
			Create();
			var owned = _scope.OwnedPipeline;
			var foreignPipeline = ForeignPipeline();
			var foreignGlobals = Foreign(Object.Instantiate(_originalGlobals));
			_undoTestMutation = () =>
			{
				if (QualitySettings.GetQualityLevel() != _originalLevel) throw new InvalidOperationException("Test quality slot lost.");
				if (QualitySettings.renderPipeline == foreignPipeline) QualitySettings.renderPipeline = owned;
				else if (QualitySettings.renderPipeline != owned && QualitySettings.renderPipeline != _originalQuality)
					throw new InvalidOperationException("Third-party quality replacement retained.");
				var current = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
				if (current == foreignGlobals) EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(_originalGlobals);
				else if (current != _originalGlobals) throw new InvalidOperationException("Third-party globals retained.");
			};
			QualitySettings.renderPipeline = foreignPipeline;
			EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(foreignGlobals);
			yield return _scope.Restore();
			Assert.That(QualitySettings.renderPipeline, Is.SameAs(foreignPipeline));
			Assert.That(EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(), Is.SameAs(foreignGlobals));
			AssertRetained();
			UndoTestMutation();
			yield return _scope.Restore();
			_scope.AssertRestored();
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator ForeignQualityLevelNeverReceivesTheCapturedSlotsPipeline()
		{
			if (QualitySettings.names.Length < 2) Assert.Ignore("Two actual quality levels are required for the cross-slot control.");
			Create();
			var owned = _scope.OwnedPipeline;
			var foreignPipeline = ForeignPipeline();
			int other = (_originalLevel + 1) % QualitySettings.names.Length;
			var otherOriginal = QualitySettings.GetRenderPipelineAssetAt(other);
			_undoTestMutation = () =>
			{
				if (QualitySettings.GetQualityLevel() != other) throw new InvalidOperationException("Third-party quality transition retained.");
				if (QualitySettings.renderPipeline == foreignPipeline) QualitySettings.renderPipeline = otherOriginal;
				else if (QualitySettings.renderPipeline != otherOriginal) throw new InvalidOperationException("Foreign slot replacement retained.");
				QualitySettings.SetQualityLevel(_originalLevel, false); // Reverses only this test's explicit transition.
			};
			QualitySettings.SetQualityLevel(other, false);
			QualitySettings.renderPipeline = foreignPipeline;
			yield return _scope.Restore();
			Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(other));
			Assert.That(QualitySettings.GetRenderPipelineAssetAt(other), Is.SameAs(foreignPipeline));
			Assert.That(QualitySettings.GetRenderPipelineAssetAt(_originalLevel), Is.SameAs(owned));
			Assert.That(EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(), Is.SameAs(_originalGlobals), "Safe independent global restoration still runs.");
			AssertRetained();
			UndoTestMutation();
			yield return _scope.Restore();
			_scope.AssertRestored();
			Assert.That(QualitySettings.GetRenderPipelineAssetAt(other), Is.SameAs(otherOriginal));
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator ForeignDefaultReferenceKeepsTheOwnedPipelineAlive()
		{
			Create();
			var owned = _scope.OwnedPipeline;
			_undoTestMutation = () =>
			{
				if (GraphicsSettings.defaultRenderPipeline == owned) GraphicsSettings.defaultRenderPipeline = _originalDefault;
				else if (GraphicsSettings.defaultRenderPipeline != _originalDefault) throw new InvalidOperationException("Third-party default pipeline retained.");
			};
			GraphicsSettings.defaultRenderPipeline = owned; // The scope did not own this slot mutation.
			yield return _scope.Restore();
			Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(owned));
			Assert.That(owned, Is.Not.Null);
			AssertRetained();
			UndoTestMutation();
			yield return _scope.Restore();
			_scope.AssertRestored();
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator PartialConstructionExceptionsRestoreOnlyAttemptedOwnedMutations()
		{
			foreach (UrpCameraPipelineScope.ConstructionStage stage in Enum.GetValues(typeof(UrpCameraPipelineScope.ConstructionStage)))
			{
				_feature = ScriptableObject.CreateInstance<ScopeLifetimeFeature>();
				_scope = new UrpCameraPipelineScope();
				Assert.Throws<InvalidOperationException>(() => _scope.Create(_feature, current =>
				{
					if (current == stage) throw new InvalidOperationException("Injected partial construction: " + stage);
				}));
				Assert.That(_scope.CleanupPending, Is.True);
				yield return _scope.Restore();
				_scope.AssertRestored();
				Assert.That(_scope.HasOwnedAssets, Is.False, stage.ToString());
			}
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator ExplicitFeatureDisposalFailureRetainsNativeAssetsForRetry()
		{
			Create();
			// Move the test's owned quality selection back first, while Dispose is still nonthrowing.
			// This prevents URP's swallowed automatic-dispose path from becoming an expected log suppression.
			QualitySettings.renderPipeline = _originalQuality;
			yield return null; yield return null;
			_feature.ThrowOnDispose = true;
			_undoTestMutation = () => _feature.ThrowOnDispose = false;
			yield return _scope.Restore();
			AssertRetained();
			Assert.That(_scope.CleanupErrors.Any(x => x.Contains("Injected feature disposal")), Is.True);
			UndoTestMutation();
			yield return _scope.Restore();
			_scope.AssertRestored();
		}

		[UnityTest, Timeout(30000)]
		public IEnumerator RetainedControlRendererBlocksAdmissionUntilItsExternalReferenceIsReleased()
		{
			Create();
			var controlPipeline = ForeignPipeline();
			var controlRenderer = controlPipeline.rendererDataList[0];
			var borrower = Object.Instantiate(controlPipeline); // Separate owned reference, outside the control graph being retired.
			var borrowerCustody = UrpCameraPipelineControlCustody.Own(new Object[] { borrower }, null);
			_undoTestMutation = () =>
			{
				if (!borrowerCustody.TryRelease()) throw new InvalidOperationException(borrowerCustody.LastFailure);
			};
			borrower.hideFlags = HideFlags.HideAndDontSave;
			yield return _scope.Restore();
			_scope.AssertRestored();
			_controls = UrpCameraPipelineControlCustody.Own(_foreignObjects, _scope);
			Assert.That(_controls.TryRelease(), Is.False);
			Assert.That(controlPipeline == null, Is.True, "The unreferenced control pipeline may retire before its borrowed renderer.");
			Assert.That(controlRenderer, Is.Not.Null, "A loaded borrower must keep the renderer alive.");
			Assert.That(UrpCameraPipelineControlCustody.RetainedCount, Is.EqualTo(1));
			Assert.Throws<InvalidOperationException>(UrpCameraPipelineControlCustody.RequireEmpty);
			var refused = new UrpCameraPipelineScope();
			var rejectedFeature = ScriptableObject.CreateInstance<ScopeLifetimeFeature>();
			try
			{
				Assert.Throws<InvalidOperationException>(() => refused.Create(rejectedFeature));
				Assert.That(refused.CleanupPending, Is.False, "Refusal precedes new custody or caller-state mutation.");
			}
			finally { Object.DestroyImmediate(rejectedFeature); }
			UndoTestMutation();
			Assert.That(UrpCameraPipelineControlCustody.RetryRetainedCleanup(), Is.True);
			Assert.That(controlRenderer == null, Is.True);
			UrpCameraPipelineControlCustody.RequireEmpty();
			Create(); // The same real admission succeeds after authoritative known cleanup.
			yield return _scope.Restore();
			_scope.AssertRestored();
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			if (!_admitted) yield break;
			if (_controls == null || _controls.IsReleased) _controls = UrpCameraPipelineControlCustody.Own(_foreignObjects, _scope);
			_controls.BeginCleanup(); // Strong custody precedes fallible undo and the yielding scope restore.
			Cleanup(UndoTestMutation);
			if (_cleanupErrors.Count != 0) _controls.Retain(_undoTestMutation, string.Join("; ", _cleanupErrors));
			if (_scope != null) yield return _scope.Restore();
			bool retained = _scope != null && _scope.CleanupPending;
			if (_feature != null && (_scope == null || !_scope.HasFeatureCustody(_feature))) Cleanup(() => Object.DestroyImmediate(_feature));
			if (retained || _cleanupErrors.Count != 0)
				_controls.Retain(_undoTestMutation, "Owning fixture cleanup incomplete: " + string.Join("; ", _cleanupErrors));
			else if (!_controls.TryRelease()) _cleanupErrors.Add(_controls.LastFailure);
			Assert.That(_cleanupErrors, Is.Empty, string.Join("; ", _cleanupErrors));
			_scope?.AssertRestored();
			Assert.That(_controls.IsReleased, Is.True, _controls.LastFailure);
		}

		private void Create()
		{
			_feature = ScriptableObject.CreateInstance<ScopeLifetimeFeature>();
			_scope = new UrpCameraPipelineScope();
			_scope.Create(_feature);
		}
		private T Foreign<T>(T value) where T : Object
		{
			_foreignObjects.Add(value); value.hideFlags = HideFlags.HideAndDontSave; return value;
		}
		private UniversalRenderPipelineAsset ForeignPipeline()
		{
			var pipeline = Foreign(Object.Instantiate(_scope.OwnedPipeline));
			var renderer = Foreign(Object.Instantiate(_scope.OwnedPipeline.rendererDataList[0]));
			renderer.rendererFeatures.Clear(); renderer.SetDirty(); // No shared source feature Create/Dispose.
			using var serialized = new SerializedObject(pipeline);
			var list = UrpCameraPipelineScope.Require(serialized, "m_RendererDataList");
			list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
			UrpCameraPipelineScope.Require(serialized, "m_DefaultRendererIndex").intValue = 0;
			serialized.ApplyModifiedPropertiesWithoutUndo();
			return pipeline;
		}
		private void AssertRetained()
		{
			Assert.That(_scope.CleanupPending, Is.True);
			Assert.That(_scope.HasOwnedAssets, Is.True);
			Assert.That(_scope.CleanupErrors, Is.Not.Empty);
		}
		private void UndoTestMutation()
		{
			_undoTestMutation?.Invoke(); _undoTestMutation = null;
		}
		private void Cleanup(Action action)
		{
			try { action(); } catch (Exception error) { _cleanupErrors.Add(error.ToString()); }
		}
	}

	// Narrow custody for this fixture's three control asset types. No render transport or background retries.
	internal static class UrpCameraPipelineControlCustody
	{
		private static readonly HashSet<Graph> Retained = new HashSet<Graph>();
		internal static int RetainedCount => Retained.Count;
		internal static void RequireEmpty()
		{
			if (Retained.Count != 0) throw new InvalidOperationException("Retained control cleanup blocks admission: " +
				string.Join("; ", Retained.Select(x => x.FirstFailure + " | latest: " + x.LastFailure)));
		}
		internal static Graph Own(IEnumerable<Object> values, UrpCameraPipelineScope owner) => new Graph(values, owner);
		internal static bool RetryRetainedCleanup()
		{
			foreach (Graph graph in Retained.ToArray()) graph.TryRelease(); // Only graphs already under known custody.
			return Retained.Count == 0;
		}
		internal static void RequirePipelineUnreferenced(RenderPipelineAsset pipeline)
		{
			if (GraphicsSettings.currentRenderPipeline == pipeline || GraphicsSettings.defaultRenderPipeline == pipeline ||
				Enumerable.Range(0, QualitySettings.names.Length).Any(i => QualitySettings.GetRenderPipelineAssetAt(i) == pipeline))
				throw new InvalidOperationException("A quality/default/current slot still references the control pipeline.");
		}
		private static bool ReferencesRenderer(UniversalRenderPipelineAsset pipeline, ScriptableRendererData renderer)
		{
			var data = pipeline.rendererDataList;
			for (int i = 0; i < data.Length; i++) if (data[i] == renderer) return true;
			return false;
		}

		internal sealed class Graph
		{
			private readonly Object[] _objects;
			private readonly UrpCameraPipelineScope _owner;
			private Action _undo;
			internal bool IsReleased { get; private set; }
			internal string FirstFailure { get; private set; }
			internal string LastFailure { get; private set; }
			internal Graph(IEnumerable<Object> values, UrpCameraPipelineScope owner) { _objects = values.ToArray(); _owner = owner; }
			internal void BeginCleanup() { Retained.Add(this); }
			internal void Retain(Action undo, string reason)
			{
				if (undo != null) _undo = undo;
				IsReleased = false;
				FirstFailure ??= reason; LastFailure = reason;
				Retained.Add(this); // Includes failed undo with no remaining native object; admission still fails closed.
			}
			internal bool TryRelease()
			{
				if (IsReleased) return true;
				try
				{
					_undo?.Invoke(); _undo = null; // Explicit retry of the original guarded, test-owned undo only.
					if (_owner != null && _owner.CleanupPending) throw new InvalidOperationException("Owning pipeline scope still retains cleanup custody.");
					UrpCameraPipelineScope.RequireSdkQuiescent();
					if (_objects.Any(x => x != null && !(x is UniversalRenderPipelineAsset) && !(x is ScriptableRendererData) && !(x is RenderPipelineGlobalSettings)))
						throw new InvalidOperationException("Unsupported feature/target/other control object retained; no unproved dependent release.");
					// Retire pipelines first. A partial failure stops before their renderer/global dependencies.
					foreach (var pipeline in _objects.OfType<UniversalRenderPipelineAsset>().Where(x => x != null))
					{
						UrpCameraPipelineScope.RequireSdkQuiescent();
						RequirePipelineUnreferenced(pipeline);
						var data = pipeline.rendererDataList;
						for (int i = 0; i < data.Length; i++)
							if (data[i] != null && data[i].rendererFeatures.Count != 0)
								throw new InvalidOperationException("Control pipeline has unknown feature ownership; graph retained.");
						Object.DestroyImmediate(pipeline);
						if (pipeline != null) throw new InvalidOperationException("Control pipeline retirement did not complete.");
					}
					foreach (var renderer in _objects.OfType<ScriptableRendererData>().Where(x => x != null))
					{
						UrpCameraPipelineScope.RequireSdkQuiescent();
						if (Resources.FindObjectsOfTypeAll<UniversalRenderPipelineAsset>().Any(x => ReferencesRenderer(x, renderer)))
							throw new InvalidOperationException("A loaded external pipeline still references the control renderer; renderer retained.");
						if (renderer.rendererFeatures.Count != 0) throw new InvalidOperationException("Control renderer has unknown feature ownership; renderer retained.");
						Object.DestroyImmediate(renderer);
						if (renderer != null) throw new InvalidOperationException("Control renderer retirement did not complete.");
					}
					foreach (var globals in _objects.OfType<RenderPipelineGlobalSettings>().Where(x => x != null))
					{
						UrpCameraPipelineScope.RequireSdkQuiescent();
						if (EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() == globals)
							throw new InvalidOperationException("Control globals remain registered; globals retained.");
						Object.DestroyImmediate(globals);
						if (globals != null) throw new InvalidOperationException("Control globals retirement did not complete.");
					}
					if (_objects.Any(x => x != null)) throw new InvalidOperationException("Known control objects remain; custody retained.");
					IsReleased = true; LastFailure = null; Retained.Remove(this); return true;
				}
				catch (Exception error) { Retain(_undo, error.ToString()); return false; }
			}
		}
	}

	internal sealed class ScopeLifetimeFeature : ScriptableRendererFeature
	{
		internal bool ThrowOnDispose;
		public override void Create() { }
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
		protected override void Dispose(bool disposing)
		{
			if (ThrowOnDispose) throw new InvalidOperationException("Injected feature disposal.");
		}
	}
}
