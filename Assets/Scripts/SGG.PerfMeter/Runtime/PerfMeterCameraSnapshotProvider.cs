using System;
using System.Reflection;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal static class PerfMeterCameraSnapshotProvider
	{
		private const string UniversalAdditionalCameraDataFullName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData";
		private const string UniversalRuntimeAssemblyName = "Unity.RenderPipelines.Universal.Runtime";
		private const string HighDefinitionAdditionalCameraDataFullName = "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData";
		private const string HighDefinitionRuntimeAssemblyName = "Unity.RenderPipelines.HighDefinition.Runtime";
		private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static Camera[] _cameraBuffer = new Camera[8];
		private static Type _universalAdditionalCameraDataType;
		private static bool _universalAdditionalCameraDataTypeResolved;
		private static Type _highDefinitionAdditionalCameraDataType;
		private static bool _highDefinitionAdditionalCameraDataTypeResolved;

		internal static PerfMeterCameraSnapshot CreateSnapshot(PerfMeterCameraSource source, string cameraNameFilter)
		{
			Camera camera = ResolveCamera(source, cameraNameFilter, out int gameCameraCount, out PerfMeterCameraSource resolvedSource);
			if (camera == null)
			{
				return CreateUnavailable(resolvedSource, gameCameraCount, string.IsNullOrEmpty(cameraNameFilter) ? "No enabled Game camera was found." : "No camera matched the requested name filter.");
			}

			Component urpData = GetUniversalAdditionalCameraData(camera);
			bool hasUrpData = urpData != null;
			Component hdrpData = GetHighDefinitionAdditionalCameraData(camera);
			bool hasHdrpData = hdrpData != null;
			return new PerfMeterCameraSnapshot(
				true,
				string.Empty,
				resolvedSource,
				gameCameraCount,
				camera.name,
			#if UNITY_6000_4_OR_NEWER
				GetCameraEntityId(camera),
			#else
				GetCameraInstanceId(camera),
			#endif
				camera.gameObject.scene.name,
				camera.gameObject.scene.path,
				camera.enabled,
				camera.isActiveAndEnabled,
				camera.cameraType,
				camera.orthographic ? PerfMeterCameraProjection.Orthographic : PerfMeterCameraProjection.Perspective,
				camera.transform.position,
				camera.transform.rotation,
				camera.transform.eulerAngles,
				camera.transform.forward,
				camera.transform.up,
				camera.fieldOfView,
				camera.orthographicSize,
				camera.nearClipPlane,
				camera.farClipPlane,
				camera.aspect,
				camera.pixelRect,
				camera.targetDisplay,
				camera.depth,
				camera.clearFlags,
				camera.cullingMask,
				camera.allowHDR,
				camera.allowMSAA,
				camera.actualRenderingPath,
				hasUrpData,
				hasUrpData ? GetMemberString(urpData, "renderType") : string.Empty,
				hasUrpData && GetMemberBool(urpData, "renderPostProcessing"),
				hasUrpData ? GetMemberString(urpData, "antialiasing") : string.Empty,
				hasUrpData ? GetMemberString(urpData, "antialiasingQuality") : string.Empty,
				hasUrpData && GetMemberBool(urpData, "stopNaN"),
				hasUrpData && GetMemberBool(urpData, "renderShadows"),
				hasUrpData && GetMemberBool(urpData, "clearDepth"),
				hasUrpData ? GetMemberString(urpData, "requiresDepthOption") : string.Empty,
				hasUrpData ? GetMemberString(urpData, "requiresColorOption") : string.Empty,
				hasUrpData && GetMemberBool(urpData, "requiresDepthTexture"),
				hasUrpData && GetMemberBool(urpData, "requiresColorTexture"),
				hasHdrpData,
				hasHdrpData ? GetMemberString(hdrpData, "clearColorMode") : string.Empty,
				hasHdrpData && GetMemberBool(hdrpData, "clearDepth"),
				hasHdrpData ? GetMemberString(hdrpData, "antialiasing") : string.Empty,
				hasHdrpData ? GetMemberString(hdrpData, "SMAAQuality") : string.Empty,
				hasHdrpData && GetMemberBool(hdrpData, "stopNaNs"),
				hasHdrpData && GetMemberBool(hdrpData, "dithering"),
				hasHdrpData && GetMemberBool(hdrpData, "allowDynamicResolution"),
				hasHdrpData && GetMemberBool(hdrpData, "customRenderingSettings"),
				hasHdrpData ? GetMemberInt(hdrpData, "volumeLayerMask") : 0,
				hasHdrpData && GetMemberValue(hdrpData, "volumeAnchorOverride") != null);
		}

		private static Component GetUniversalAdditionalCameraData(Camera camera)
		{
			Type type = GetUniversalAdditionalCameraDataType();
			return type != null ? camera.GetComponent(type) : null;
		}

		private static Component GetHighDefinitionAdditionalCameraData(Camera camera)
		{
			Type type = GetHighDefinitionAdditionalCameraDataType();
			return type != null ? camera.GetComponent(type) : null;
		}

		private static Type GetUniversalAdditionalCameraDataType()
		{
			if (_universalAdditionalCameraDataTypeResolved)
			{
				return _universalAdditionalCameraDataType;
			}

			_universalAdditionalCameraDataTypeResolved = true;
			_universalAdditionalCameraDataType = Type.GetType(UniversalAdditionalCameraDataFullName + ", " + UniversalRuntimeAssemblyName);
			return _universalAdditionalCameraDataType;
		}

		private static Type GetHighDefinitionAdditionalCameraDataType()
		{
			if (_highDefinitionAdditionalCameraDataTypeResolved)
			{
				return _highDefinitionAdditionalCameraDataType;
			}

			_highDefinitionAdditionalCameraDataTypeResolved = true;
			_highDefinitionAdditionalCameraDataType = Type.GetType(HighDefinitionAdditionalCameraDataFullName + ", " + HighDefinitionRuntimeAssemblyName);
			return _highDefinitionAdditionalCameraDataType;
		}

		private static string GetMemberString(object instance, string memberName)
		{
			object value = GetMemberValue(instance, memberName);
			return value != null ? value.ToString() : string.Empty;
		}

		private static bool GetMemberBool(object instance, string memberName)
		{
			object value = GetMemberValue(instance, memberName);
			return value is bool boolValue && boolValue;
		}

		private static int GetMemberInt(object instance, string memberName)
		{
			object value = GetMemberValue(instance, memberName);
			if (value is int intValue)
			{
				return intValue;
			}

			if (value is LayerMask layerMask)
			{
				return layerMask.value;
			}

			PropertyInfo property = value?.GetType().GetProperty("value", InstanceFlags);
			object propertyValue = property != null && property.GetIndexParameters().Length == 0 ? property.GetValue(value) : null;
			return propertyValue is int reflectedInt ? reflectedInt : 0;
		}

		private static object GetMemberValue(object instance, string memberName)
		{
			if (instance == null)
			{
				return null;
			}

			Type type = instance.GetType();
			PropertyInfo property = type.GetProperty(memberName, InstanceFlags);
			if (property != null && property.GetIndexParameters().Length == 0)
			{
				return property.GetValue(instance);
			}

			FieldInfo field = type.GetField(memberName, InstanceFlags);
			return field != null ? field.GetValue(instance) : null;
		}

		private static Camera ResolveCamera(PerfMeterCameraSource source, string cameraNameFilter, out int gameCameraCount, out PerfMeterCameraSource resolvedSource)
		{
			EnsureCameraBuffer();
			int count = Camera.GetAllCameras(_cameraBuffer);
			gameCameraCount = CountGameCameras(count);
			if (!string.IsNullOrEmpty(cameraNameFilter))
			{
				resolvedSource = PerfMeterCameraSource.NameFilter;
				return FindCameraByName(count, cameraNameFilter);
			}

			if (source == PerfMeterCameraSource.MainCamera || source == PerfMeterCameraSource.Auto)
			{
				Camera mainCamera = Camera.main;
				if (mainCamera != null && mainCamera.enabled && mainCamera.cameraType == CameraType.Game)
				{
					resolvedSource = PerfMeterCameraSource.MainCamera;
					return mainCamera;
				}
			}

			if (source == PerfMeterCameraSource.MainCamera)
			{
				resolvedSource = PerfMeterCameraSource.MainCamera;
				return null;
			}

			resolvedSource = PerfMeterCameraSource.FirstGameCamera;
			return FindFirstGameCamera(count);
		}

		private static void EnsureCameraBuffer()
		{
			int count = Camera.allCamerasCount;
			if (count > _cameraBuffer.Length)
			{
				_cameraBuffer = new Camera[Mathf.NextPowerOfTwo(count)];
			}
		}

		private static int CountGameCameras(int count)
		{
			int gameCameraCount = 0;
			for (int i = 0; i < count; i++)
			{
				Camera camera = _cameraBuffer[i];
				if (camera != null && camera.cameraType == CameraType.Game)
				{
					gameCameraCount++;
				}
			}

			return gameCameraCount;
		}

		private static Camera FindCameraByName(int count, string cameraNameFilter)
		{
			for (int i = 0; i < count; i++)
			{
				Camera camera = _cameraBuffer[i];
				if (camera != null && camera.enabled && camera.cameraType == CameraType.Game && camera.name.IndexOf(cameraNameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return camera;
				}
			}

			return null;
		}

		private static Camera FindFirstGameCamera(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Camera camera = _cameraBuffer[i];
				if (camera != null && camera.enabled && camera.cameraType == CameraType.Game)
				{
					return camera;
				}
			}

			return null;
		}

	#if UNITY_6000_4_OR_NEWER
		private static ulong GetCameraEntityId(Camera camera)
		{
			return EntityId.ToULong(camera.GetEntityId());
		}
	#else
		private static int GetCameraInstanceId(Camera camera)
		{
			return camera.GetInstanceID();
		}
	#endif

		private static PerfMeterCameraSnapshot CreateUnavailable(PerfMeterCameraSource source, int gameCameraCount, string warning)
		{
			return new PerfMeterCameraSnapshot(
				false,
				warning,
				source,
				gameCameraCount,
				string.Empty,
			#if UNITY_6000_4_OR_NEWER
				0UL,
			#else
				0,
			#endif
				string.Empty,
				string.Empty,
				false,
				false,
				CameraType.Game,
				PerfMeterCameraProjection.Unknown,
				Vector3.zero,
				Quaternion.identity,
				Vector3.zero,
				Vector3.forward,
				Vector3.up,
				0f,
				0f,
				0f,
				0f,
				0f,
				new Rect(0f, 0f, 0f, 0f),
				0,
				0f,
				CameraClearFlags.Skybox,
				0,
				false,
				false,
				RenderingPath.UsePlayerSettings,
				false,
				string.Empty,
				false,
				string.Empty,
				string.Empty,
				false,
				false,
				false,
				string.Empty,
				string.Empty,
				false,
				false);
		}
	}
}
