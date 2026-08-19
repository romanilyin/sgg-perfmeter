using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.EditMode
{
	/// <summary>
	/// Manual real-tool acceptance entry point. Launch the Editor through RenderDoc and invoke
	/// <c>-executeMethod SGG.PerfMeter.Tests.EditMode.PerfMeterGpuAnnotationsRenderDocSmoke.Run</c>.
	/// This is deliberately not an NUnit test because it requires an injected user-owned tool.
	/// </summary>
	public static class PerfMeterGpuAnnotationsRenderDocSmoke
	{
		public static void Run()
		{
			try
			{
				string artifact = RunCore();
				Debug.Log($"SGG RenderDoc annotation smoke passed: {artifact}");
				EditorApplication.Exit(0);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
			}
		}

		private static string RunCore()
		{
			if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12)
			{
				throw new InvalidOperationException(
					$"D3D12 is required, active renderer is {SystemInfo.graphicsDeviceType}.");
			}

			PerfMeterRenderDocPInvokeBridge bridge = new PerfMeterRenderDocPInvokeBridge();
			SggRdResult capabilityResult = bridge.GetCapabilities(out SggRdCapabilitiesV1 bridgeCapabilities);
			if (capabilityResult != SggRdResult.Ok || bridgeCapabilities.SupportsAnnotations == 0u)
			{
				throw new InvalidOperationException(
					$"RenderDoc App API 1.7 annotation capability is unavailable: {capabilityResult}.");
			}

			ulong requestNonce = unchecked((ulong)DateTime.UtcNow.Ticks);
			if (requestNonce == 0u)
			{
				requestNonce = 1u;
			}
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string smokeRoot = Path.Combine(
				projectRoot,
				"Logs",
				"RenderDocAnnotationSmoke",
				requestNonce.ToString("X16"));
			Directory.CreateDirectory(smokeRoot);
			string captureTemplate = Path.Combine(smokeRoot, "annotation-smoke");

			SggRdResult beginResult = bridge.BeginCapture(
				requestNonce,
				captureTemplate,
				"SGG PerfMeter annotation smoke",
				out SggRdCaptureTokenV1 token);
			if (beginResult != SggRdResult.Ok)
			{
				throw new InvalidOperationException($"RenderDoc capture begin failed: {beginResult}.");
			}

			bool captureEnded = false;
			RenderTexture target = null;
			CommandBuffer commandBuffer = null;
			try
			{
				PerfMeterGpuAnnotationProviderRegistry.Reset();
				PerfMeterRenderDocGpuAnnotationBootstrap.Register();
				PerfMeterGpuAnnotationCapabilities before = PerfMeterGpuAnnotations.Capabilities;
				if (!before.IsReady)
				{
					throw new InvalidOperationException(
						$"Annotation provider is not ready during capture: {before.Availability}.");
				}

				target = new RenderTexture(16, 16, 0, RenderTextureFormat.ARGB32)
				{
					name = "SGG RenderDoc Annotation Smoke Target"
				};
				if (!target.Create())
				{
					throw new InvalidOperationException("Failed to create the smoke render target.");
				}

				commandBuffer = new CommandBuffer { name = "SGG RenderDoc Annotation Smoke" };
				commandBuffer.SetRenderTarget(target);
				PerfMeterGpuAnnotationBatch annotations = new PerfMeterGpuAnnotationBatch(4);
				annotations.TryAdd(PerfMeterGpuAnnotationKeys.Module, "com.sungeargames.perfmeter");
				annotations.TryAdd(PerfMeterGpuAnnotationKeys.RenderGraphPass, "perfmeter.annotation_smoke.clear_red");
				annotations.TryAdd("SGG.PerfMeter.Smoke.Sequence", 1u);

				using (PerfMeterGpuAnnotationScope scope =
					PerfMeterGpuAnnotations.BeginScope(commandBuffer, annotations))
				{
					if (scope == null)
					{
						throw new InvalidOperationException("The annotation scope was not recorded.");
					}
					commandBuffer.ClearRenderTarget(false, true, Color.red);
				}
				commandBuffer.ClearRenderTarget(false, true, Color.blue);

				Graphics.ExecuteCommandBuffer(commandBuffer);
				AsyncGPUReadbackRequest readback = AsyncGPUReadback.Request(target);
				readback.WaitForCompletion();
				if (readback.hasError)
				{
					throw new InvalidOperationException("The smoke GPU readback failed.");
				}

				PerfMeterGpuAnnotationCapabilities after = PerfMeterGpuAnnotations.Capabilities;
				if (after.PacketsExecuted < before.PacketsExecuted + 2u ||
					after.AnnotationCalls < before.AnnotationCalls + 8u ||
					after.AnnotationErrors != before.AnnotationErrors)
				{
					throw new InvalidOperationException(
						$"Annotation callback evidence is incomplete: executed {before.PacketsExecuted}->{after.PacketsExecuted}, " +
						$"calls {before.AnnotationCalls}->{after.AnnotationCalls}, errors {before.AnnotationErrors}->{after.AnnotationErrors}.");
				}

				SggRdResult endResult = bridge.EndCapture(token);
				if (endResult != SggRdResult.Ok)
				{
					throw new InvalidOperationException($"RenderDoc capture end failed: {endResult}.");
				}
				captureEnded = true;

				for (int attempt = 0; attempt < 200; attempt++)
				{
					SggRdResult artifactResult = bridge.TryGetNewArtifact(token, out _, out string observedPath);
					if (artifactResult == SggRdResult.Ok)
					{
						string receipt = Path.Combine(
							projectRoot,
							"Logs",
							"RenderDocAnnotationSmoke",
							"latest.txt");
						File.WriteAllText(receipt, observedPath);
						return observedPath;
					}
					if (artifactResult != SggRdResult.CaptureNotObserved)
					{
						throw new InvalidOperationException($"RenderDoc artifact observation failed: {artifactResult}.");
					}
					Thread.Sleep(50);
				}
				throw new TimeoutException("RenderDoc did not publish the smoke capture within 10 seconds.");
			}
			finally
			{
				if (!captureEnded)
				{
					bridge.DiscardCapture(token);
				}
				if (commandBuffer != null)
				{
					commandBuffer.Release();
				}
				if (target != null)
				{
					target.Release();
					UnityEngine.Object.DestroyImmediate(target);
				}
			}
		}
	}
}
