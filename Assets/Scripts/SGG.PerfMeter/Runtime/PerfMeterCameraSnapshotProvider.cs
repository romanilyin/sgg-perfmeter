using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SGG.PerfMeter
{
	internal static class PerfMeterCameraSnapshotProvider
	{
		private static Camera[] _cameraBuffer = new Camera[8];

		internal static PerfMeterCameraSnapshot CreateSnapshot(PerfMeterCameraSource source, string cameraNameFilter)
		{
			Camera camera = ResolveCamera(source, cameraNameFilter, out int gameCameraCount, out PerfMeterCameraSource resolvedSource);
			if (camera == null)
			{
				return CreateUnavailable(resolvedSource, gameCameraCount, string.IsNullOrEmpty(cameraNameFilter) ? "No enabled Game camera was found." : "No camera matched the requested name filter.");
			}

			UniversalAdditionalCameraData urpData = null;
			bool hasUrpData = camera.TryGetComponent(out urpData);
			return new PerfMeterCameraSnapshot(
				true,
				string.Empty,
				resolvedSource,
				gameCameraCount,
				camera.name,
				camera.GetInstanceID(),
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
				hasUrpData ? urpData.renderType.ToString() : string.Empty,
				hasUrpData && urpData.renderPostProcessing,
				hasUrpData ? urpData.antialiasing.ToString() : string.Empty,
				hasUrpData ? urpData.antialiasingQuality.ToString() : string.Empty,
				hasUrpData && urpData.stopNaN,
				hasUrpData && urpData.renderShadows,
				hasUrpData && urpData.clearDepth,
				hasUrpData ? urpData.requiresDepthOption.ToString() : string.Empty,
				hasUrpData ? urpData.requiresColorOption.ToString() : string.Empty,
				hasUrpData && urpData.requiresDepthTexture,
				hasUrpData && urpData.requiresColorTexture);
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

		private static PerfMeterCameraSnapshot CreateUnavailable(PerfMeterCameraSource source, int gameCameraCount, string warning)
		{
			return new PerfMeterCameraSnapshot(
				false,
				warning,
				source,
				gameCameraCount,
				string.Empty,
				0,
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
