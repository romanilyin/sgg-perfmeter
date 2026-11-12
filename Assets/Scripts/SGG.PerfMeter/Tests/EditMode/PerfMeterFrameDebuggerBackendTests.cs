using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SGG.PerfMeter.Editor.FrameDebugger;
using UnityEditor.Rendering;
using UnityEngine;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterFrameDebuggerBackendTests
	{
		private FixtureWindow _window;
		private PerfMeterFrameDebuggerBackend _backend;

		[SetUp]
		public void SetUp()
		{
			FixtureUtility.Reset();
			FixtureFrame.Local = true;
			FixtureFrame.Remote = false;
			_window = new FixtureWindow();
			var bindings = Resolve(typeof(FixtureUtility));
			Assert.That(bindings, Is.Not.Null);
			_backend = new PerfMeterFrameDebuggerBackend(bindings, () => new object[] { _window }, _ => 73);
		}

		[Test]
		public void UnknownOrByRefOnlyBindingsFailClosed()
		{
			Assert.That(PerfMeterFrameDebuggerBackend.Bindings.Resolve(null, typeof(FixtureData), typeof(FixtureEvent),
				typeof(FixtureWindow), typeof(FixtureFrame), out var missing), Is.Null);
			StringAssert.StartsWith("Unsupported:", missing);
			Assert.That(Resolve(typeof(ByRefUtility)), Is.Null);
			Assert.That(PerfMeterFrameDebuggerBackend.Bindings.Resolve(typeof(FixtureUtility), typeof(FixtureData), typeof(FixtureEvent),
				typeof(WindowWithoutRefresh), typeof(FixtureFrame), out _), Is.Null);
			Assert.That(FixtureUtility.SelectionCalls, Is.Zero);
		}

		[TestCase(false, 0)]
		[TestCase(true, 1)]
		public void SuccessAndExactDataIndexAreBothRequired(bool success, int returnedIndex)
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.ReadSuccess = success;
			FixtureUtility.ReturnedIndex = returnedIndex;
			Assert.That(_backend.TryRead(0, Options(), out var row, out var reason), Is.False);
			Assert.That(row, Is.Null);
			StringAssert.StartsWith("Pending:", reason);
		}

		[Test]
		public void DelayedReadIsRetriedWithoutPublishingPriorData()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.ReadSuccess = false;
			Assert.That(_backend.TryRead(0, Options(), out var pending, out _), Is.False);
			Assert.That(pending, Is.Null);
			FixtureUtility.ReadSuccess = true;
			Assert.That(_backend.TryRead(0, Options(), out var row, out _), Is.True);
			Assert.That(row.Index, Is.Zero);
			Assert.That(Field(row, "shader").Value, Is.EqualTo("FixtureShader"));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void LocalCaptureUsesExplicitTargetFlagsRegardlessOfReceivingDataFlag(bool receivingData)
		{
			FixtureUtility.ReceivingData = receivingData;
			Assert.That(_backend.TryObserve(out var local, out _), Is.True);
			Assert.That(local.Local, Is.True);
			Assert.That(_backend.TryRead(0, Options(), out _, out _), Is.True);
			FixtureFrame.Remote = true;
			Assert.That(_backend.TryObserve(out var remote, out _), Is.True);
			Assert.That(remote.Local, Is.False);
			Assert.That(_backend.TrySelect(1, out _), Is.False);
		}
		[Test]
		public void WrongSelectionDisabledAndRemoteStatesRejectReadsAndMutations()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.limit = 2;
			Assert.That(_backend.TryRead(0, Options(), out _, out _), Is.False);
			FixtureFrame.Remote = true;
			Assert.That(_backend.TryObserve(out var remote, out _), Is.True);
			Assert.That(remote.Local, Is.False);
			Assert.That(_backend.TrySelect(1, out _), Is.False);
			FixtureFrame.Local = false;
			FixtureFrame.Remote = false;
			Assert.That(_backend.TryObserve(out var disabled, out _), Is.True);
			Assert.That(disabled.Enabled, Is.False);
			Assert.That(_backend.TrySelect(1, out _), Is.False);
			Assert.That(FixtureUtility.SelectionCalls, Is.Zero);
		}

		[Test]
		public void CountCeilingIsCheckedBeforeNativeListAllocation()
		{
			FixtureUtility.OverrideCount = PerfMeterFrameDebuggerBackend.NativeEventSafetyLimit + 1;
			Assert.That(_backend.TryObserve(out _, out var reason), Is.False);
			Assert.That(FixtureUtility.EventListCalls, Is.Zero);
			StringAssert.Contains("safety", reason);
		}

		[Test]
		public void DescriptorChangesAndInconsistentListLengthsAreRejected()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.Events[0].m_Type = FixtureEventType.ComputeDispatch;
			Assert.That(_backend.TryObserve(out _, out var reason), Is.False);
			StringAssert.Contains("descriptor list changed", reason);
			FixtureUtility.OverrideCount = 3;
			Assert.That(_backend.TryObserve(out _, out reason), Is.False);
			StringAssert.Contains("list length", reason);
		}

		[Test]
		public void UnknownEventTypeZeroAndMissingFieldRemainDistinct()
		{
			FixtureUtility.Events[0].m_Type = (FixtureEventType)123456;
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			Assert.That(_backend.TryRead(0, Options(), out var row, out _), Is.True);
			Assert.That(row.RawEventType, Is.EqualTo(123456));
			Assert.That(row.EventType, Is.EqualTo("Unknown"));
			Assert.That(Field(row, "draw_calls").Available, Is.True);
			Assert.That(Field(row, "draw_calls").Value, Is.EqualTo(0));
			Assert.That(Field(row, "indices").Available, Is.False);
			Assert.That(Field(row, "indices").Value, Is.Null);
			Assert.That(Field(row, "mesh").Available, Is.False);
		}

		[TestCase(FixtureEventType.ClearColor)]
		[TestCase(FixtureEventType.ResolveRT)]
		[TestCase(FixtureEventType.ResolveDepth)]
		public void ClearAndResolveNeverAttributePriorShaderOrProperties(FixtureEventType eventType)
		{
			FixtureUtility.Events[0].m_Type = eventType;
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			var options = Options(); options.IncludeProperties = true;
			Assert.That(_backend.TryRead(0, options, out var row, out _), Is.True);
			Assert.That(Field(row, "shader").Available, Is.False);
			Assert.That(Field(row, "shader_pass").Available, Is.False);
			Assert.That(Field(row, "keywords_raw").Available, Is.False);
			Assert.That(Field(row, "properties").Available, Is.False);
			Assert.That(row.Fields.Any(field => field.Name.StartsWith("property.", StringComparison.Ordinal)), Is.False);
		}

		[Test]
		public void OptionalPropertiesAreDetachedBoundedAndContainNoObjectReferences()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			var options = Options(); options.IncludeProperties = true; options.MaxPropertiesPerEvent = 4;
			Assert.That(_backend.TryRead(0, options, out var row, out _), Is.True);
			var vector = row.Fields.Single(field => field.Kind == "vector" && field.Available);
			Assert.That(vector.Value, Is.EqualTo(new double[] { 1, 2, 3, 4 }));
			var texture = row.Fields.Single(field => field.Kind == "texture_name" && field.Available);
			Assert.That(texture.Value, Is.EqualTo("DetachedTextureName"));
			FixtureUtility.Info.m_Vectors[0].m_Value = Vector4.zero;
			FixtureUtility.Info.m_Textures[0].m_TextureName = "changed";
			Assert.That(vector.Value, Is.EqualTo(new double[] { 1, 2, 3, 4 }));
			Assert.That(texture.Value, Is.EqualTo("DetachedTextureName"));
			foreach (var field in row.Fields)
				Assert.That(field.Value == null || field.Value is string || field.Value is bool || field.Value is int ||
					field.Value is uint || field.Value is short || field.Value is sbyte || field.Value is byte ||
					field.Value is float || field.Value is string[] || field.Value is double[], Is.True, field.Name);
		}

		[Test]
		public void InvalidPropertyFlagsKeepRawZeroAndUnavailableValue()
		{
			FixtureUtility.Info.m_Floats[0].m_Flags = 0;
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			var options = Options(); options.IncludeProperties = true;
			Assert.That(_backend.TryRead(0, options, out var row, out _), Is.True);
			var property = row.Fields.Single(field => field.Kind == "float" && field.Name.StartsWith("property.", StringComparison.Ordinal));
			Assert.That(property.Available, Is.False);
			Assert.That(property.Value, Is.Null);
			Assert.That(row.Fields.Single(field => field.Name.Contains(".flags._Float")).Value, Is.EqualTo(0));
		}

		[Test]
		public void PropertiesAreOffByDefaultAndLimitsAreExplicit()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			Assert.That(_backend.TryRead(0, Options(), out var noProperties, out _), Is.True);
			Assert.That(Field(noProperties, "properties").Available, Is.False);
			var options = Options(); options.IncludeProperties = true; options.MaxPropertiesPerEvent = 1; options.MaxStringLength = 64;
			FixtureUtility.LongName = new string('x', 100);
			Assert.That(_backend.TryRead(0, options, out var row, out _), Is.True);
			Assert.That(row.Truncated, Is.True);
			Assert.That(row.Name.Length, Is.EqualTo(64));
			Assert.That(row.Fields.Count(field => field.Name.StartsWith("property.", StringComparison.Ordinal)), Is.EqualTo(2));
			options.MaxFieldsPerEvent = 16;
			Assert.That(_backend.TryRead(0, options, out var limited, out _), Is.True);
			Assert.That(limited.Fields.Length, Is.EqualTo(16));
			Assert.That(limited.Truncated, Is.True);
		}

		[Test]
		public void ZeroOriginalLimitIsRejectedBeforeAnyMutation()
		{
			FixtureUtility.limit = 0;
			Assert.That(_backend.TryObserve(out var observation, out var reason), Is.False);
			Assert.That(observation.SelectedLimit, Is.Zero);
			StringAssert.Contains("cannot be restored", reason);
			Assert.That(_backend.TrySelect(1, out _), Is.False);
			Assert.That(FixtureUtility.SelectionCalls, Is.Zero);
			Assert.That(FixtureUtility.EventListCalls, Is.Zero);
			FixtureUtility.limit = 1;
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.limit = 0;
			Assert.That(_backend.TrySelect(1, out _), Is.False);
			Assert.That(FixtureUtility.SelectionCalls, Is.Zero);
		}

		[Test]
		public void SuccessfulSelectionUsesExactWindowRefreshOnce()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			Assert.That(_backend.TrySelect(2, out _), Is.True);
			Assert.That(FixtureUtility.limit, Is.EqualTo(2));
			Assert.That(FixtureUtility.SelectionCalls, Is.EqualTo(1));
			Assert.That(_window.RefreshCalls, Is.EqualTo(1));
		}

		[Test]
		public void MutationExceptionNeverFallsBackOrRepeatsAndCanBeObserved()
		{
			Assert.That(_backend.TryObserve(out _, out _), Is.True);
			FixtureUtility.limit = 2;
			_window.ThrowAfterMutation = true;
			Assert.That(_backend.TrySelect(1, out var reason), Is.False);
			StringAssert.Contains("mutation outcome requires observation", reason);
			Assert.That(FixtureUtility.SelectionCalls, Is.EqualTo(1));
			Assert.That(_window.RefreshCalls, Is.Zero);
			Assert.That(_backend.TryObserve(out var state, out _), Is.True);
			Assert.That(state.SelectedLimit, Is.EqualTo(1));
		}

		[Test]
		public void WrongThreadAndAmbiguousWindowsDoNotTouchNativeState()
		{
			bool result = true; string reason = null;
			var thread = new Thread(() => result = _backend.TryObserve(out _, out reason));
			thread.Start(); thread.Join();
			Assert.That(result, Is.False);
			StringAssert.Contains("main thread", reason);
			Assert.That(FixtureUtility.EventListCalls, Is.Zero);
			var ambiguous = new PerfMeterFrameDebuggerBackend(Resolve(typeof(FixtureUtility)),
				() => new object[] { _window, new FixtureWindow() }, _ => 73);
			Assert.That(ambiguous.TryObserve(out _, out _), Is.False);
			Assert.That(FixtureUtility.EventListCalls, Is.Zero);
		}

		private static PerfMeterFrameDebuggerExportOptions Options() => new PerfMeterFrameDebuggerExportOptions();
		private static PerfMeterFrameDebuggerField Field(PerfMeterFrameDebuggerEvent row, string name) => row.Fields.Single(field => field.Name == name);
		private static PerfMeterFrameDebuggerBackend.Bindings Resolve(Type utility) =>
			PerfMeterFrameDebuggerBackend.Bindings.Resolve(utility, typeof(FixtureData), typeof(FixtureEvent),
				typeof(FixtureWindow), typeof(FixtureFrame), out _);

		public enum FixtureEventType { ClearColor = 1, ResolveRT = 9, ResolveDepth = 10, Mesh = 14, ComputeDispatch = 22 }
		public struct FixtureEvent { public FixtureEventType m_Type; public object m_Obj; }
		public sealed class FixtureWindow
		{
			internal bool ThrowAfterMutation;
			internal int RefreshCalls;
			internal void ChangeFrameEventLimit(int value)
			{
				FixtureUtility.SelectionCalls++;
				FixtureUtility.limit = value;
				if (ThrowAfterMutation) throw new InvalidOperationException("fixture");
			}
			internal void RepaintOnLimitChange() { RefreshCalls++; }
		}
		public sealed class WindowWithoutRefresh
		{
			internal void ChangeFrameEventLimit(int value) { FixtureUtility.SelectionCalls++; }
		}
		public static class FixtureFrame
		{
			internal static bool Local, Remote;
			public static bool enabled => Local || Remote;
			internal static bool IsLocalEnabled() => Local;
			internal static bool IsRemoteEnabled() => Remote;
		}
		public sealed class FixtureData
		{
			public int m_FrameEventIndex;
			public int m_DrawCallCount;
			public string m_IndexCount = "unsupported field signature";
			public string m_RealShaderName = "FixtureShader", m_OriginalShaderName = "OriginalShader", m_PassName = "Forward", m_PassLightMode = "UniversalForward";
			public string shaderKeywords = "ZZ AA";
			public FixtureShaderInfo m_ShaderInfo;
		}
		public struct FixtureShaderInfo
		{
			public FixtureFloat[] m_Floats;
			public FixtureInt[] m_Ints;
			public FixtureVector[] m_Vectors;
			public FixtureTexture[] m_Textures;
		}
		public struct FixtureFloat { public string m_Name; public int m_Flags; public float m_Value; }
		public struct FixtureInt { public string m_Name; public int m_Flags; public int m_Value; }
		public struct FixtureVector { public string m_Name; public int m_Flags; public Vector4 m_Value; }
		public struct FixtureTexture { public string m_Name; public int m_Flags; public string m_TextureName; public object m_Value; }
		public static class FixtureUtility
		{
			internal static int SelectionCalls, EventListCalls, OverrideCount, ReturnedIndex;
			internal static bool ReadSuccess, ReceivingData;
			internal static string LongName;
			internal static FixtureShaderInfo Info;
			internal static FixtureEvent[] Events;
			public static int count => OverrideCount >= 0 ? OverrideCount : Events.Length;
			public static int limit { get; set; }
			public static int eventsHash => 123;
			public static uint eventDataHash => 0;
			public static bool receivingRemoteFrameEventData => ReceivingData;
			public static bool locallySupported => true;
			public static int GetRemotePlayerGUID() => 0;
			public static FixtureEvent[] GetFrameEvents() { EventListCalls++; return Events; }
			public static string GetFrameEventInfoName(int index) => LongName ?? "Fixture event";
			public static bool GetFrameEventData(int index, FixtureData data)
			{
				data.m_FrameEventIndex = ReturnedIndex;
				data.m_ShaderInfo = Info;
				return ReadSuccess;
			}
			internal static void Reset()
			{
				SelectionCalls = EventListCalls = ReturnedIndex = 0; OverrideCount = -1; ReadSuccess = true; ReceivingData = false; LongName = null; limit = 1;
				Events = new[] { new FixtureEvent { m_Type = FixtureEventType.Mesh }, new FixtureEvent { m_Type = FixtureEventType.Mesh } };
				var flags = 1 << (int)ShaderType.Count;
				Info = new FixtureShaderInfo
				{
					m_Floats = new[] { new FixtureFloat { m_Name = "_Float", m_Flags = flags, m_Value = 0 } },
					m_Ints = new[] { new FixtureInt { m_Name = "_Int", m_Flags = flags, m_Value = 7 } },
					m_Vectors = new[] { new FixtureVector { m_Name = "_Vector", m_Flags = flags, m_Value = new Vector4(1, 2, 3, 4) } },
					m_Textures = new[] { new FixtureTexture { m_Name = "_Texture", m_TextureName = "DetachedTextureName", m_Value = new object() } }
				};
			}
		}
		public static class ByRefUtility
		{
			public static int count => FixtureUtility.count;
			public static int limit => FixtureUtility.limit;
			public static int eventsHash => 123;
			public static bool receivingRemoteFrameEventData => FixtureUtility.ReceivingData;
			public static bool locallySupported => true;
			public static int GetRemotePlayerGUID() => 0;
			public static FixtureEvent[] GetFrameEvents() => FixtureUtility.Events;
			public static string GetFrameEventInfoName(int index) => "fixture";
			public static bool GetFrameEventData(int index, ref FixtureData data) => true;
		}
	}
}