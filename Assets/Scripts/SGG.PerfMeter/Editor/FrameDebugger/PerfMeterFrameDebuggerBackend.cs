using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	// This adapter intentionally supports one inspected Editor binding layout. It never enables
	// the debugger, opens its window, changes its target, reads pixels, or resolves asset identity.
	[InitializeOnLoad]
	internal sealed class PerfMeterFrameDebuggerBackend : IPerfMeterFrameDebuggerBackend
	{
		internal const int NativeEventSafetyLimit = 16384;
		private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;
		private readonly int _ownerThreadId;
		private readonly Bindings _bindings;
		private readonly Func<object[]> _windows;
		private readonly Func<object, ulong> _windowId;
		private readonly string _unsupportedReason;
		private object _window;
		private int[] _eventTypes;
		private PerfMeterFrameDebuggerObservation _listObservation;
		private bool _hasListObservation;

		static PerfMeterFrameDebuggerBackend() { }

		internal PerfMeterFrameDebuggerBackend()
		{
			_ownerThreadId = MainThreadId;
			if (Application.unityVersion != "6000.6.8f1" || Application.platform != RuntimePlatform.WindowsEditor ||
				SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12)
			{
				_unsupportedReason = "Unsupported: inspected target is Unity 6000.6.8f1 Windows Editor D3D12.";
				return;
			}
			var editor = typeof(EditorWindow).Assembly;
			_bindings = Bindings.Resolve(editor.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility"),
				editor.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData"),
				editor.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEvent"),
				editor.GetType("UnityEditor.FrameDebuggerWindow"), typeof(UnityEngine.FrameDebugger), out _unsupportedReason);
			if (_bindings == null) return;
			_windows = () => Resources.FindObjectsOfTypeAll(_bindings.WindowType);
			_windowId = GetWindowIdentity;
		}

		private static ulong GetWindowIdentity(object value)
		{
#if UNITY_6000_4_OR_NEWER
			return EntityId.ToULong(((UnityEngine.Object)value).GetEntityId());
#else
			return unchecked((ulong)((UnityEngine.Object)value).GetInstanceID());
#endif
		}

		// A detached fixture seam: production still resolves and validates every required member.
		internal PerfMeterFrameDebuggerBackend(Bindings bindings, Func<object[]> windows,
			Func<object, ulong> windowId)
		{
			_ownerThreadId = Thread.CurrentThread.ManagedThreadId;
			_bindings = bindings;
			_windows = windows;
			_windowId = windowId;
			_unsupportedReason = "Unsupported: binding descriptor is unavailable.";
		}

		public bool TryObserve(out PerfMeterFrameDebuggerObservation observation, out string reason)
		{
			observation = default;
			if (!CanCall(out reason)) return false;
			try
			{
				if (!ReadState(out observation, out reason)) return false;
				if (!observation.Enabled || !observation.Local)
				{
					_hasListObservation = false;
					_eventTypes = null;
					return true;
				}
				if (observation.EventCount < 0 || observation.EventCount > NativeEventSafetyLimit)
				{
					reason = "Unavailable: native event count exceeds the 16384 descriptor safety boundary.";
					return false;
				}
				if (observation.EventCount > 0 && (observation.SelectedLimit < 1 || observation.SelectedLimit > observation.EventCount))
				{
					_hasListObservation = false;
					reason = "Unavailable: original selection cannot be restored through ChangeFrameEventLimit (valid limits are 1..event count).";
					return false;
				}
				var events = (Array)_bindings.GetEvents.Invoke(null, null);
				if (events == null || events.Length != observation.EventCount)
				{
					reason = "Unavailable: event list length does not match the observed event count.";
					return false;
				}
				var types = new int[events.Length];
				for (var i = 0; i < types.Length; i++)
					types[i] = Convert.ToInt32(_bindings.EventType.GetValue(events.GetValue(i)), CultureInfo.InvariantCulture);
				if (!ReadState(out var after, out reason) || !SameState(observation, after))
				{
					reason = "Unavailable: debugger state changed while observing its event list.";
					return false;
				}
				if (_hasListObservation && observation.SameCapture(_listObservation) && !EqualTypes(types, _eventTypes))
				{
					reason = "Unavailable: event descriptor list changed under the same native hash/count observations.";
					return false;
				}
				_eventTypes = types;
				_listObservation = observation;
				_hasListObservation = true;
				reason = string.Empty;
				return true;
			}
			catch (Exception ex) { reason = Failure("observe", ex); return false; }
		}

		public bool TrySelect(int limit, out string reason)
		{
			if (!CanCall(out reason)) return false;
			try
			{
				if (!ReadState(out var before, out reason)) return false;
				if (!before.Enabled || !before.Local || before.WindowId == 0 || limit < 1 || limit > before.EventCount ||
					before.SelectedLimit < 1 || before.SelectedLimit > before.EventCount ||
					before.EventCount > NativeEventSafetyLimit || !_hasListObservation || !before.SameCapture(_listObservation))
				{
					reason = "Unavailable: selection requires an enabled local debugger, its existing window, and a valid limit.";
					return false;
				}
				// Exactly one mutation route. An exception can occur after the native limit changed;
				// callers must observe that uncertain result before considering restoration.
				_bindings.Select.Invoke(_window, new object[] { limit });
				_bindings.RepaintOnLimitChange.Invoke(_window, null);
				if (!ReadState(out var after, out reason) || !before.SameCapture(after) || after.SelectedLimit != limit)
				{
					reason = "Unavailable: selection did not retain the expected debugger state/limit.";
					return false;
				}
				reason = string.Empty;
				return true;
			}
			catch (Exception ex) { reason = Failure("select (mutation outcome requires observation)", ex); return false; }
		}

		public bool TryRead(int index, PerfMeterFrameDebuggerExportOptions options, out PerfMeterFrameDebuggerEvent row, out string reason)
		{
			row = null;
			if (!CanCall(out reason)) return false;
			try
			{
				if (options == null) { reason = "Unavailable: export options are missing."; return false; }
				options = options.Freeze();
				if (!ReadState(out var before, out reason)) return false;
				if (!before.Enabled || !before.Local || index < 0 || index >= before.EventCount || before.SelectedLimit != index + 1 ||
					!_hasListObservation || !before.SameCapture(_listObservation) || _eventTypes == null || index >= _eventTypes.Length)
				{
					reason = "Unavailable: read requires the observed local capture and exact selected event limit.";
					return false;
				}
				var data = _bindings.DataConstructor.Invoke(null);
				_bindings.FrameIndex.SetValue(data, -1);
				var ready = (bool)_bindings.GetData.Invoke(null, new[] { (object)index, data });
				if (!ReadState(out var after, out reason) || !SameState(before, after))
				{
					reason = "Unavailable: debugger state changed while retrieving event data.";
					return false;
				}
				if (!ready || (int)_bindings.FrameIndex.GetValue(data) != index)
				{
					reason = "Pending: event data has not confirmed the requested index.";
					return false;
				}
				var rawType = _eventTypes[index];
				var type = Enum.GetName(_bindings.EventType.FieldType, rawType);
				var name = (string)_bindings.GetName.Invoke(null, new object[] { index });
				var nameTruncated = name != null && name.Length > options.MaxStringLength;
				row = new PerfMeterFrameDebuggerEvent
				{
					Index = index, RawEventType = rawType, EventType = type ?? "Unknown",
					Name = Clip(name, options.MaxStringLength), Truncated = nameTruncated
				};
				var fields = new FieldCopy(options, row);
				var staleShader = (rawType >= 0 && rawType <= 7) || rawType == 9 || rawType == 10;
				fields.Member(data, "m_RealShaderName", "shader", typeof(string), staleShader);
				fields.Member(data, "m_OriginalShaderName", "original_shader", typeof(string), staleShader);
				fields.Member(data, "m_PassName", "shader_pass", typeof(string), staleShader);
				fields.Member(data, "m_PassLightMode", "shader_light_mode", typeof(string), staleShader);
				fields.Member(data, "m_DrawCallCount", "draw_calls", typeof(int));
				fields.Member(data, "m_VertexCount", "vertices", typeof(int));
				fields.Member(data, "m_IndexCount", "indices", typeof(int));
				fields.Member(data, "m_InstanceCount", "instances", typeof(int));
				fields.Member(data, "m_RenderTargetName", "render_target_name", typeof(string));
				fields.Member(data, "m_RenderTargetWidth", "width", typeof(int));
				fields.Member(data, "m_RenderTargetHeight", "height", typeof(int));
				fields.Member(data, "shaderKeywords", "keywords_raw", typeof(string), staleShader);
				fields.Keywords(data, staleShader);
				fields.Member(data, "m_MeshSubset", "mesh_subset", typeof(int));
				fields.Unavailable("mesh", "string", "Mesh asset/object identity is not resolved by this adapter; BRG/ECS may expose no mesh.");
				fields.Member(data, "m_ComputeShaderName", "compute_shader", typeof(string));
				fields.Member(data, "m_ComputeShaderKernelName", "compute_kernel", typeof(string));
				fields.Members(data, "m_ComputeShaderThreadGroups", "compute_groups_", "XYZ", typeof(int));
				fields.Members(data, "m_ComputeShaderGroupSize", "compute_group_size_", "XYZ", typeof(int));
				fields.Member(data, "m_RenderTargetFormat", "render_target_format_raw", typeof(int));
				fields.Member(data, "m_RenderTargetDimension", "render_target_dimension_raw", typeof(int));
				fields.Member(data, "m_RenderTargetCount", "render_target_count", typeof(short));
				fields.Member(data, "m_RenderTargetIsBackBuffer", "render_target_backbuffer", typeof(bool));
				fields.Member(data, "m_RenderTargetMemoryless", "render_target_memoryless_raw", typeof(sbyte));
				fields.Member(data, "m_RenderTargetHasDepthTexture", "render_target_has_depth_raw", typeof(sbyte));
				fields.Member(data, "m_RenderTargetHasStencilBits", "render_target_has_stencil_raw", typeof(sbyte));
				fields.Member(data, "m_RenderTargetLoadAction", "color_load_raw", typeof(int));
				fields.Member(data, "m_RenderTargetStoreAction", "color_store_raw", typeof(int));
				fields.Member(data, "m_RenderTargetDepthLoadAction", "depth_load_raw", typeof(int));
				fields.Member(data, "m_RenderTargetDepthStoreAction", "depth_store_raw", typeof(int));
				fields.Members(data, "m_RenderTargetClearColor", "target_clear_", "RGBA", typeof(float));
				fields.Member(data, "m_RenderTargetClearDepth", "target_clear_depth", typeof(float));
				fields.Member(data, "m_RenderTargetClearStencil", "target_clear_stencil", typeof(uint));
				fields.Members(data, "m_ClearColor", "clear_", "RGBA", typeof(float));
				fields.Member(data, "m_ClearDepth", "clear_depth", typeof(float));
				fields.Member(data, "m_ClearStencil", "clear_stencil", typeof(uint));
				fields.State(data, "m_BlendState", "blend");
				fields.State(data, "m_RasterState", "raster");
				fields.State(data, "m_DepthState", "depth");
				fields.State(data, "m_StencilState", "stencil");
				fields.Member(data, "m_StencilRef", "stencil_reference", typeof(int));
				fields.Member(data, "m_BatchBreakCause", "srp_batch_cause_raw", typeof(int));
				fields.BatchCause(data, _bindings.BatchCauses);
				fields.Hash(_bindings.EventDataHash);
				fields.Properties(data, staleShader);
				row.Fields = fields.ToArray();
				if (!ReadState(out var copied, out reason) || !SameState(before, copied))
				{
					row = null;
					reason = "Unavailable: debugger state changed while copying detached event data.";
					return false;
				}
				reason = string.Empty;
				return true;
			}
			catch (Exception ex) { row = null; reason = Failure("read", ex); return false; }
		}

		private bool CanCall(out string reason)
		{
			if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId) { reason = "Unavailable: Frame Debugger access requires the Editor main thread."; return false; }
			if (_bindings == null) { reason = _unsupportedReason; return false; }
			reason = string.Empty;
			return true;
		}

		private bool ReadState(out PerfMeterFrameDebuggerObservation observation, out string reason)
		{
			observation = default;
			var windows = _windows();
			if (windows == null || windows.Length != 1 || windows[0] == null || !_bindings.WindowType.IsInstanceOfType(windows[0]))
			{
				reason = "Unavailable: exactly one existing Frame Debugger window is required.";
				return false;
			}
			_window = windows[0];
			// receivingRemoteFrameEventData can also be true for a local Editor capture.
			// Target ownership comes from the explicit local/remote enable flags.
			var local = (bool)_bindings.Local.Invoke(null, null);
			var remote = (bool)_bindings.Remote.Invoke(null, null);
			observation = new PerfMeterFrameDebuggerObservation((bool)_bindings.Enabled.GetValue(null),
				local && !remote && (bool)_bindings.LocallySupported.GetValue(null), _windowId(_window),
				(int)_bindings.Target.Invoke(null, null), (int)_bindings.Count.GetValue(null),
				(int)_bindings.Limit.GetValue(null), (int)_bindings.EventsHash.GetValue(null));
			reason = string.Empty;
			return true;
		}

		private static bool SameState(PerfMeterFrameDebuggerObservation a, PerfMeterFrameDebuggerObservation b) =>
			a.SameCapture(b) && a.SelectedLimit == b.SelectedLimit;
		private static bool EqualTypes(int[] a, int[] b)
		{
			if (b == null || a.Length != b.Length) return false;
			for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
			return true;
		}
		private static string Clip(string value, int limit) => value == null ? string.Empty : value.Length <= limit ? value : value.Substring(0, limit);
		private static string Failure(string operation, Exception ex)
		{
			var actual = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
			// Native exception text can be unbounded or contain project data; retain its stable CLR type only.
			return "Unavailable: Frame Debugger " + operation + " failed (" + actual.GetType().Name + ").";
		}

		internal sealed class Bindings
		{
			internal Type WindowType;
			internal ConstructorInfo DataConstructor;
			internal MethodInfo GetEvents, GetData, GetName, Select, RepaintOnLimitChange, Target, Local, Remote, BatchCauses;
			internal PropertyInfo Enabled, Count, Limit, EventsHash, EventDataHash, LocallySupported;
			internal FieldInfo FrameIndex, EventType;

			internal static Bindings Resolve(Type utility, Type data, Type descriptor, Type window, Type frame, out string reason)
			{
				try
				{
					if (utility == null || data == null || descriptor == null || window == null || frame == null ||
						!data.IsClass || !descriptor.IsValueType || descriptor.IsEnum) throw new MissingMemberException();
					var result = new Bindings
					{
						WindowType = window,
						DataConstructor = data.GetConstructor(InstanceFlags, null, Type.EmptyTypes, null),
						GetEvents = Method(utility, "GetFrameEvents", true, descriptor.MakeArrayType()),
						GetData = Method(utility, "GetFrameEventData", true, typeof(bool), typeof(int), data),
						GetName = Method(utility, "GetFrameEventInfoName", true, typeof(string), typeof(int)),
						Select = Method(window, "ChangeFrameEventLimit", false, typeof(void), typeof(int)),
						RepaintOnLimitChange = Method(window, "RepaintOnLimitChange", false, typeof(void)),
						Target = Method(utility, "GetRemotePlayerGUID", true, typeof(int)),
						Local = Method(frame, "IsLocalEnabled", true, typeof(bool)),
						Remote = Method(frame, "IsRemoteEnabled", true, typeof(bool)),
						Enabled = Property(frame, "enabled", typeof(bool)),
						Count = Property(utility, "count", typeof(int)),
						Limit = Property(utility, "limit", typeof(int)),
						EventsHash = Property(utility, "eventsHash", typeof(int)),
						LocallySupported = Property(utility, "locallySupported", typeof(bool)),
						FrameIndex = data.GetField("m_FrameEventIndex", InstanceFlags),
						EventType = descriptor.GetField("m_Type", InstanceFlags)
					};
					if (result.DataConstructor == null || result.FrameIndex == null || result.FrameIndex.FieldType != typeof(int) ||
						result.FrameIndex.IsInitOnly || result.EventType == null || !result.EventType.FieldType.IsEnum ||
						Enum.GetUnderlyingType(result.EventType.FieldType) != typeof(int)) throw new MissingMemberException();
					var hash = utility.GetProperty("eventDataHash", StaticFlags);
					if (hash != null && hash.PropertyType == typeof(uint) && hash.GetIndexParameters().Length == 0 && hash.GetGetMethod(true)?.IsStatic == true)
						result.EventDataHash = hash;
					var batchCauses = utility.GetMethod("GetBatchBreakCauseStrings", StaticFlags, null, Type.EmptyTypes, null);
					if (batchCauses != null && batchCauses.ReturnType == typeof(string[]) && !batchCauses.ContainsGenericParameters)
						result.BatchCauses = batchCauses;
					reason = string.Empty;
					return result;
				}
				catch (Exception ex) when (ex is MissingMemberException || ex is AmbiguousMatchException || ex is ArgumentException)
				{
					reason = "Unsupported: Frame Debugger internal members do not match the inspected 6000.6.8f1 signatures.";
					return null;
				}
			}

			private static MethodInfo Method(Type type, string name, bool isStatic, Type returns, params Type[] parameters)
			{
				var method = type.GetMethod(name, isStatic ? StaticFlags : InstanceFlags, null, parameters, null);
				if (method == null || method.ReturnType != returns || method.IsStatic != isStatic || method.ContainsGenericParameters)
					throw new MissingMemberException();
				return method;
			}
			private static PropertyInfo Property(Type type, string name, Type valueType)
			{
				var property = type.GetProperty(name, StaticFlags);
				if (property == null || property.PropertyType != valueType || property.GetIndexParameters().Length != 0 ||
					property.GetGetMethod(true)?.IsStatic != true) throw new MissingMemberException();
				return property;
			}
		}

		private sealed class FieldCopy
		{
			private readonly PerfMeterFrameDebuggerExportOptions _options;
			private readonly PerfMeterFrameDebuggerEvent _row;
			private readonly List<PerfMeterFrameDebuggerField> _fields = new List<PerfMeterFrameDebuggerField>();
			private int _properties;
			internal FieldCopy(PerfMeterFrameDebuggerExportOptions options, PerfMeterFrameDebuggerEvent row) { _options = options; _row = row; }
			internal PerfMeterFrameDebuggerField[] ToArray() => _fields.ToArray();
			internal void Unavailable(string name, string kind, string reason) => Add(name, kind, null, reason);
			internal void Member(object data, string member, string name, Type type, bool stale = false)
			{
				if (stale) { Unavailable(name, Kind(type), "Unity reports potentially previous-event shader data for clear/resolve events."); return; }
				var field = data.GetType().GetField(member, InstanceFlags);
				if (field == null || field.FieldType != type) { Unavailable(name, Kind(type), "Internal field is unavailable or has an unsupported signature."); return; }
				var value = field.GetValue(data);
				Add(name, Kind(type), value, value == null ? "Internal field returned null." : string.Empty);
			}
			internal void Members(object data, string prefix, string name, string suffixes, Type type)
			{
				foreach (var suffix in suffixes) Member(data, prefix + suffix, name + char.ToLowerInvariant(suffix), type);
			}
			internal void State(object data, string member, string prefix)
			{
				var field = data.GetType().GetField(member, InstanceFlags);
				if (field == null || !field.FieldType.IsValueType || field.FieldType.IsPrimitive || field.FieldType.IsEnum)
				{ Unavailable(prefix, "state", "Internal render-state struct is unavailable."); return; }
				var state = field.GetValue(data);
				var members = field.FieldType.GetFields(InstanceFlags);
				Array.Sort(members, (a, b) => string.CompareOrdinal(a.Name, b.Name));
				foreach (var item in members)
				{
					if (_fields.Count >= _options.MaxFieldsPerEvent) { _row.Truncated = true; return; }
					if (item.Name == "m_Padding") continue;
					var value = item.GetValue(state);
					if (item.FieldType.IsEnum && Enum.GetUnderlyingType(item.FieldType) == typeof(int))
						Add(prefix + "." + item.Name, "enum_raw", Convert.ToInt32(value, CultureInfo.InvariantCulture));
					else if (SafeScalar(item.FieldType)) Add(prefix + "." + item.Name, Kind(item.FieldType), value);
					else Unavailable(prefix + "." + item.Name, "unknown", "Unsupported render-state field type.");
				}
			}
			internal void Hash(PropertyInfo hash)
			{
				if (hash == null) Unavailable("event_data_hash", "uint", "Internal event-data hash is unavailable.");
				else
				{
					var value = (uint)hash.GetValue(null);
					Add("event_data_hash", "uint", value);
					Add("event_data_hash_usable", "bool", value != 0);
				}
			}
			internal void BatchCause(object data, MethodInfo getter)
			{
				var field = data.GetType().GetField("m_BatchBreakCause", InstanceFlags);
				if (getter == null || field?.FieldType != typeof(int))
				{ Unavailable("srp_batch_cause", "string", "Internal SRP batch-cause table is unavailable."); return; }
				var causes = (string[])getter.Invoke(null, null);
				var index = (int)field.GetValue(data);
				if (causes == null || causes.Length > 1024 || index < 0 || index >= causes.Length)
				{ Unavailable("srp_batch_cause", "string", "Native SRP batch-cause index/table is unavailable or outside its safety boundary."); return; }
				Add("srp_batch_cause", "string", causes[index]);
			}
			internal void Keywords(object data, bool stale)
			{
				if (stale) { Unavailable("keywords_sorted", "string[]", "Unity reports potentially previous-event shader data for clear/resolve events."); return; }
				var field = data.GetType().GetField("shaderKeywords", InstanceFlags);
				if (field == null || field.FieldType != typeof(string) || !(field.GetValue(data) is string raw))
				{ Unavailable("keywords_sorted", "string[]", "Internal keyword string is unavailable."); return; }
				var clipped = Clip(raw, _options.MaxStringLength);
				if (raw.Length != clipped.Length && clipped.Length > 0 && !char.IsWhiteSpace(raw[clipped.Length]))
				{
					var boundary = clipped.Length - 1;
					while (boundary >= 0 && !char.IsWhiteSpace(clipped[boundary])) boundary--;
					clipped = boundary < 0 ? string.Empty : clipped.Substring(0, boundary);
				}
				var tokens = clipped.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				Array.Sort(tokens, StringComparer.Ordinal);
				Add("keywords_sorted", "string[]", tokens, string.Empty, raw.Length != clipped.Length);
			}
			internal void Properties(object data, bool stale)
			{
				if (!_options.IncludeProperties) { Unavailable("properties", "properties", "Optional shader properties were not requested."); return; }
				if (stale) { Unavailable("properties", "properties", "Unity reports potentially previous-event shader properties for clear/resolve events."); return; }
				var field = data.GetType().GetField("m_ShaderInfo", InstanceFlags);
				var info = field?.GetValue(data);
				if (info == null) { Unavailable("properties", "properties", "Internal shader-info data is unavailable."); return; }
				PropertyArray(info, "m_Floats", "float", typeof(float));
				PropertyArray(info, "m_Ints", "int", typeof(int));
				PropertyArray(info, "m_Vectors", "vector", typeof(Vector4));
				PropertyArray(info, "m_Textures", "texture_name", typeof(string));
				Unavailable("properties.matrices_buffers_cbuffers", "properties", "Matrix/buffer/constant-buffer schemas are outside this adapter's validated property subset.");
			}
			private void PropertyArray(object info, string member, string kind, Type valueType)
			{
				var field = info.GetType().GetField(member, InstanceFlags);
				if (field == null || !field.FieldType.IsArray || !(field.GetValue(info) is Array array))
				{ Unavailable("properties." + member, kind, "Internal property array is unavailable."); return; }
				var element = field.FieldType.GetElementType();
				var nameField = element.GetField("m_Name", InstanceFlags);
				var flagsField = element.GetField("m_Flags", InstanceFlags);
				var valueField = element.GetField(kind == "texture_name" ? "m_TextureName" : "m_Value", InstanceFlags);
				if (nameField?.FieldType != typeof(string) || flagsField?.FieldType != typeof(int) || valueField?.FieldType != valueType)
				{ Unavailable("properties." + member, kind, "Internal property element signature is unsupported."); return; }
				for (var i = 0; i < array.Length; i++)
				{
					if (_properties >= _options.MaxPropertiesPerEvent || _fields.Count + 2 > _options.MaxFieldsPerEvent)
					{ _row.Truncated = true; return; }
					var item = array.GetValue(i);
					var rawName = (string)nameField.GetValue(item);
					var propertyName = "property." + kind + "." + i.ToString(CultureInfo.InvariantCulture) + "." + Clip(rawName, _options.MaxStringLength);
					var flags = (int)flagsField.GetValue(item);
					var value = valueField.GetValue(item);
					if (valueType == typeof(Vector4))
					{
						var vector = (Vector4)value;
						value = new[] { (double)vector.x, vector.y, vector.z, vector.w };
					}
					var invalidCount = kind != "texture_name" && ((flags >> (int)ShaderType.Count) & 0x3ff) == 0;
					Add(propertyName, kind, invalidCount ? null : value,
						invalidCount ? "Native flags report no valid values for this raw property entry." : string.Empty,
						rawName != null && rawName.Length > _options.MaxStringLength);
					Add("property." + kind + "." + i.ToString(CultureInfo.InvariantCulture) + ".flags." + Clip(rawName, _options.MaxStringLength), "int", flags);
					_properties++;
				}
			}
			private void Add(string name, string kind, object value, string reason = "", bool truncated = false)
			{
				if (_fields.Count >= _options.MaxFieldsPerEvent) { _row.Truncated = true; return; }
				if (name.Length > _options.MaxStringLength) truncated = true;
				if (value == null && reason.Length == 0) reason = "Internal field returned null.";
				if (value is string text && text.Length > _options.MaxStringLength) { value = Clip(text, _options.MaxStringLength); truncated = true; }
				if (value is float number && (float.IsNaN(number) || float.IsInfinity(number)))
				{ value = null; reason = "Native numeric value is non-finite."; }
				if (value is double[] values)
					foreach (var component in values) if (double.IsNaN(component) || double.IsInfinity(component))
					{ value = null; reason = "Native vector contains a non-finite value."; break; }
				_fields.Add(new PerfMeterFrameDebuggerField
				{
					Name = Clip(name, _options.MaxStringLength), Kind = kind, Available = reason.Length == 0,
					Value = value, Reason = reason, Truncated = truncated
				});
				_row.Truncated |= truncated;
			}
			private static bool SafeScalar(Type type) => type == typeof(string) || type == typeof(bool) || type == typeof(int) ||
				type == typeof(uint) || type == typeof(float) || type == typeof(short) || type == typeof(sbyte) || type == typeof(byte);
			private static string Kind(Type type) => type == typeof(string) ? "string" : type == typeof(bool) ? "bool" :
				type == typeof(float) ? "float" : type == typeof(uint) ? "uint" : "int";
		}
	}
}