using System;
using Unity.Jobs;
using UnityEngine;

#if UNITY_6000_5_OR_NEWER
using Gsc = UnityEngine.Rendering.GraphicsStateCollection;
#else
using Gsc = UnityEngine.Experimental.Rendering.GraphicsStateCollection;
#endif

namespace SGG.PerfMeter
{
	internal sealed class PerfMeterUnityGraphicsStateCollectionBackend : IPerfMeterGraphicsStateCollectionBackend
	{
		internal const string BackendId = "com.unity.graphics-state-collection";
		private static readonly string BackendVersion = typeof(Gsc).Assembly.GetName().Version?.ToString() ?? string.Empty;
		private Gsc _collection;

		public string Id => BackendId;
		public string Version => BackendVersion;

		public bool SupportsCacheMissTracing => false;

		public bool SupportsParallelPsoCreation => SystemInfo.supportsParallelPSOCreation;

		public bool TryBeginTrace(out string error)
		{
			error = string.Empty;
			try
			{
				DestroyCollection();
				_collection = new Gsc();
				if (!_collection.BeginTrace())
				{
					error = "Unity GraphicsStateCollection rejected trace start.";
					DestroyCollection();
					return false;
				}

				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				DestroyCollection();
				return false;
			}
		}

		public bool TryEndTrace(
			string outputPath,
			out PerfMeterGraphicsStateTraceBackendResult result,
			out string error)
		{
			result = default;
			error = string.Empty;
			try
			{
				if (_collection == null)
				{
					error = "Unity GraphicsStateCollection trace is not active.";
					return false;
				}

				_collection.EndTrace();
				bool saved = _collection.SaveToFile(outputPath);
				result = new PerfMeterGraphicsStateTraceBackendResult(
					saved,
					_collection.totalGraphicsStateCount,
					_collection.variantCount);
				if (!saved)
				{
					error = "Unity GraphicsStateCollection failed to save the trace artifact.";
				}

				return saved;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
			finally
			{
				DestroyCollection();
			}
		}

		public void CancelTrace()
		{
			try
			{
				if (_collection != null && _collection.isTracing)
				{
					_collection.EndTrace();
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				DestroyCollection();
			}
		}

		public bool TryPrewarm(
			string inputPath,
			int maxStateCount,
			bool traceCacheMisses,
			out PerfMeterGraphicsStatePrewarmBackendResult result,
			out string error)
		{
			result = default;
			error = string.Empty;
			Gsc collection = null;
			try
			{
				if (traceCacheMisses)
				{
					error = "Unity GraphicsStateCollection cache-miss tracing evidence is not supported.";
					return false;
				}

				collection = new Gsc();
				if (!collection.LoadFromFile(inputPath))
				{
					error = "Unity GraphicsStateCollection failed to load the prewarm artifact.";
					return false;
				}

				JobHandle handle;
				if (maxStateCount > 0)
				{
					handle = collection.WarmUpProgressively(maxStateCount, default(JobHandle));
				}
				else
				{
					handle = collection.WarmUp(default(JobHandle));
				}

				handle.Complete();
				result = new PerfMeterGraphicsStatePrewarmBackendResult(
					true,
					collection.completedWarmupCount,
					collection.totalGraphicsStateCount,
					collection.isWarmedUp);
				return true;
			}
			catch (Exception exception)
			{
				error = exception.GetType().Name + ": " + exception.Message;
				return false;
			}
			finally
			{
				if (collection != null)
				{
					try
					{
						DestroyCollectionObject(collection);
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private void DestroyCollection()
		{
			if (_collection != null)
			{
				Gsc collection = _collection;
				_collection = null;
				try
				{
					DestroyCollectionObject(collection);
				}
				catch (Exception)
				{
				}
			}
		}

		private static void DestroyCollectionObject(Gsc collection)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(collection);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(collection);
			}
		}
	}

	internal static class PerfMeterUnityGraphicsStateCollectionBootstrap
	{
		private static PerfMeterUnityGraphicsStateCollectionBackend _backend;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			if (_backend != null)
			{
				PerformanceMeter.UnregisterGraphicsStateCollectionBackend(_backend);
				_backend = null;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register()
		{
			if (_backend != null)
			{
				return;
			}

			_backend = new PerfMeterUnityGraphicsStateCollectionBackend();
			try
			{
				PerformanceMeter.RegisterGraphicsStateCollectionBackend(_backend);
			}
			catch (InvalidOperationException)
			{
				_backend = null;
			}
		}
	}
}
