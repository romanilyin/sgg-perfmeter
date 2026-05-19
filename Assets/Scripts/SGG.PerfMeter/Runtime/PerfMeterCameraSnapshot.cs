using UnityEngine;

namespace SGG.PerfMeter
{
	public enum PerfMeterCameraSource
	{
		Auto = 0,
		MainCamera = 1,
		NameFilter = 2,
		FirstGameCamera = 3
	}

	public enum PerfMeterCameraProjection
	{
		Unknown = 0,
		Perspective = 1,
		Orthographic = 2
	}

	public readonly struct PerfMeterCameraSnapshot
	{
		public PerfMeterCameraSnapshot(
			bool isAvailable,
			string warning,
			PerfMeterCameraSource source,
			int detectedGameCameraCount,
			string cameraName,
			int cameraInstanceId,
			string sceneName,
			string scenePath,
			bool enabled,
			bool isActiveAndEnabled,
			CameraType cameraType,
			PerfMeterCameraProjection projection,
			Vector3 position,
			Quaternion rotation,
			Vector3 eulerAngles,
			Vector3 forward,
			Vector3 up,
			float fieldOfView,
			float orthographicSize,
			float nearClipPlane,
			float farClipPlane,
			float aspect,
			Rect pixelRect,
			int targetDisplay,
			float depth,
			CameraClearFlags clearFlags,
			int cullingMask,
			bool allowHdr,
			bool allowMsaa,
			RenderingPath actualRenderingPath,
			bool hasUniversalAdditionalCameraData,
			string urpRenderType,
			bool urpRenderPostProcessing,
			string urpAntialiasing,
			string urpAntialiasingQuality,
			bool urpStopNaN,
			bool urpRenderShadows,
			bool urpClearDepth,
			string urpRequiresDepthOption,
			string urpRequiresColorOption,
			bool urpRequiresDepthTexture,
			bool urpRequiresColorTexture)
		{
			IsAvailable = isAvailable;
			Warning = warning ?? string.Empty;
			Source = source;
			DetectedGameCameraCount = detectedGameCameraCount;
			CameraName = cameraName ?? string.Empty;
			CameraInstanceId = cameraInstanceId;
			SceneName = sceneName ?? string.Empty;
			ScenePath = scenePath ?? string.Empty;
			Enabled = enabled;
			IsActiveAndEnabled = isActiveAndEnabled;
			CameraType = cameraType;
			Projection = projection;
			Position = position;
			Rotation = rotation;
			EulerAngles = eulerAngles;
			Forward = forward;
			Up = up;
			FieldOfView = fieldOfView;
			OrthographicSize = orthographicSize;
			NearClipPlane = nearClipPlane;
			FarClipPlane = farClipPlane;
			Aspect = aspect;
			PixelRect = pixelRect;
			TargetDisplay = targetDisplay;
			Depth = depth;
			ClearFlags = clearFlags;
			CullingMask = cullingMask;
			AllowHdr = allowHdr;
			AllowMsaa = allowMsaa;
			ActualRenderingPath = actualRenderingPath;
			HasUniversalAdditionalCameraData = hasUniversalAdditionalCameraData;
			UrpRenderType = urpRenderType ?? string.Empty;
			UrpRenderPostProcessing = urpRenderPostProcessing;
			UrpAntialiasing = urpAntialiasing ?? string.Empty;
			UrpAntialiasingQuality = urpAntialiasingQuality ?? string.Empty;
			UrpStopNaN = urpStopNaN;
			UrpRenderShadows = urpRenderShadows;
			UrpClearDepth = urpClearDepth;
			UrpRequiresDepthOption = urpRequiresDepthOption ?? string.Empty;
			UrpRequiresColorOption = urpRequiresColorOption ?? string.Empty;
			UrpRequiresDepthTexture = urpRequiresDepthTexture;
			UrpRequiresColorTexture = urpRequiresColorTexture;
		}

		public bool IsAvailable { get; }
		public string Warning { get; }
		public PerfMeterCameraSource Source { get; }
		public int DetectedGameCameraCount { get; }
		public string CameraName { get; }
		public int CameraInstanceId { get; }
		public string SceneName { get; }
		public string ScenePath { get; }
		public bool Enabled { get; }
		public bool IsActiveAndEnabled { get; }
		public CameraType CameraType { get; }
		public PerfMeterCameraProjection Projection { get; }
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public Vector3 EulerAngles { get; }
		public Vector3 Forward { get; }
		public Vector3 Up { get; }
		public float FieldOfView { get; }
		public float OrthographicSize { get; }
		public float NearClipPlane { get; }
		public float FarClipPlane { get; }
		public float Aspect { get; }
		public Rect PixelRect { get; }
		public int TargetDisplay { get; }
		public float Depth { get; }
		public CameraClearFlags ClearFlags { get; }
		public int CullingMask { get; }
		public bool AllowHdr { get; }
		public bool AllowMsaa { get; }
		public RenderingPath ActualRenderingPath { get; }
		public bool HasUniversalAdditionalCameraData { get; }
		public string UrpRenderType { get; }
		public bool UrpRenderPostProcessing { get; }
		public string UrpAntialiasing { get; }
		public string UrpAntialiasingQuality { get; }
		public bool UrpStopNaN { get; }
		public bool UrpRenderShadows { get; }
		public bool UrpClearDepth { get; }
		public string UrpRequiresDepthOption { get; }
		public string UrpRequiresColorOption { get; }
		public bool UrpRequiresDepthTexture { get; }
		public bool UrpRequiresColorTexture { get; }
	}
}
