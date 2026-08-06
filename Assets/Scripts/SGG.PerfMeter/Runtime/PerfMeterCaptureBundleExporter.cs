using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SGG.PerfMeter
{
	internal static class PerfMeterCaptureBundleExporter
	{
		internal const string RelativeBundleRoot = "Temp/PerfMeter/CaptureBundles";
		internal const long MaxBundleBytes = 64L * 1024L * 1024L;
		internal const long MaxScreenshotBytes = 16L * 1024L * 1024L;
		internal const long MaxMemorySnapshotBytes = PerfMeterMemorySnapshotCoordinator.MaxSnapshotBytes;
		internal const long TotalQuotaBytes = 2L * 1024L * 1024L * 1024L;
		internal const int MaxCommittedBundles = 16;
		internal const int RetentionDays = 7;
		private const string BundleSchema = "sgg.perfmeter.capture-bundle";
		private const string OwnershipMarker = "sgg.perfmeter.capture-bundle\n1\n";

		internal static PerfMeterCaptureBundleExportResult Export(
			PerfMeterCaptureBundleExportData data,
			string path,
			string externalArtifactPath,
			bool requireAuthoritativeExternalArtifact)
		{
			if (data == null)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.NotFound, string.Empty, "capture_not_found", PerfMeterCaptureBundleStatusSnapshot.None);
			}

			if (!data.Status.IsExportReady)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.NotReady, string.Empty, "capture_not_ready", data.Status);
			}

			if (requireAuthoritativeExternalArtifact)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.AuthorityRequired, string.Empty, "authoritative_external_metadata_unavailable", data.Status);
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				path = RelativeBundleRoot + "/capture-" + data.Status.BundleId;
			}

			string projectRoot;
			string bundleRoot;
			string finalPath;
			string relativePath;
			string pathError;
			try
			{
				if (!TryResolveBundleDestination(path, out projectRoot, out bundleRoot, out finalPath, out relativePath, out pathError))
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.PathRejected, string.Empty, pathError, data.Status);
				}
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.PathRejected, string.Empty, "invalid_bundle_path", data.Status);
			}

			if (Directory.Exists(finalPath) || File.Exists(finalPath))
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.Conflict, relativePath, "destination_exists", data.Status);
			}

			ExternalArtifactMetadata externalMetadata;
			try
			{
				if (!TryObserveExternalArtifact(projectRoot, data.CaptureOptions.Tool, externalArtifactPath, out externalMetadata, out pathError))
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.PathRejected, string.Empty, pathError, data.Status);
				}
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.PathRejected, string.Empty, "invalid_external_artifact_path", data.Status);
			}

			Dictionary<string, byte[]> baseComponents = BuildComponents(data, externalMetadata, null);
			long availableArtifactBytes = MaxBundleBytes - GetTotalBytes(baseComponents);
			byte[] externalArtifactBytes = null;
			if (!string.IsNullOrEmpty(externalMetadata.SourcePath))
			{
				try
				{
					externalArtifactBytes = ReadBoundedFile(externalMetadata.SourcePath, projectRoot, availableArtifactBytes);
					externalMetadata = externalMetadata.WithObservedBytes(externalArtifactBytes);
				}
				catch (InvalidDataException)
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.QuotaExceeded, string.Empty, "bundle_size_limit_exceeded", data.Status);
				}
				catch (Exception exception) when (IsPathOrIoException(exception))
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.IoError, string.Empty, "external_artifact_io_error: " + exception.Message, data.Status);
				}
			}

			Dictionary<string, byte[]> components = BuildComponents(data, externalMetadata, externalArtifactBytes);
			long componentBytes = GetTotalBytes(components);
			if (componentBytes > MaxBundleBytes || (data.ScreenshotBytes != null && data.ScreenshotBytes.LongLength > MaxScreenshotBytes))
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.QuotaExceeded, string.Empty, "bundle_size_limit_exceeded", data.Status);
			}

			if (!TryInspectMemorySnapshot(projectRoot, data.MemorySnapshotArtifact, out MemoryArtifactMetadata memoryMetadata, out string memoryError))
			{
				PerfMeterCaptureBundleExportStatus status = string.Equals(memoryError, "memory_snapshot_size_limit_exceeded", StringComparison.Ordinal)
					? PerfMeterCaptureBundleExportStatus.QuotaExceeded
					: PerfMeterCaptureBundleExportStatus.IoError;
				return Result(false, status, string.Empty, memoryError, data.Status);
			}

			if (componentBytes + memoryMetadata.SizeBytes > MaxBundleBytes + MaxMemorySnapshotBytes)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.QuotaExceeded, string.Empty, "bundle_size_limit_exceeded", data.Status);
			}

			string stagingPath = Path.Combine(bundleRoot, ".sgg-perfmeter-staging-" + Guid.NewGuid().ToString("N"));
			try
			{
				Directory.CreateDirectory(bundleRoot);
				Directory.CreateDirectory(stagingPath);
				List<FileManifestEntry> entries = new List<FileManifestEntry>(components.Count);
				foreach (KeyValuePair<string, byte[]> component in components)
				{
					WriteFile(stagingPath, component.Key, component.Value);
					entries.Add(new FileManifestEntry(component.Key, component.Value.LongLength, Sha256(component.Value)));
				}

				if (!string.IsNullOrEmpty(memoryMetadata.SourcePath))
				{
					FileManifestEntry memoryEntry = CopyMemorySnapshot(stagingPath, projectRoot, memoryMetadata);
					entries.Add(memoryEntry);
					memoryMetadata = memoryMetadata.WithObservedFile(memoryEntry.SizeBytes, memoryEntry.Hash);
				}

				if (memoryMetadata.State != PerfMeterMemorySnapshotState.NotRequested)
				{
					byte[] memoryMetadataBytes = Utf8(BuildMemorySnapshotMetadata(data, memoryMetadata));
					WriteFile(stagingPath, "memory-snapshot.json", memoryMetadataBytes);
					entries.Add(new FileManifestEntry("memory-snapshot.json", memoryMetadataBytes.LongLength, Sha256(memoryMetadataBytes)));
					componentBytes += memoryMetadataBytes.LongLength;
				}

				byte[] manifest = Utf8(BuildManifest(data, externalMetadata, memoryMetadata, entries));
				if (componentBytes + memoryMetadata.SizeBytes + manifest.LongLength > MaxBundleBytes + MaxMemorySnapshotBytes)
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.QuotaExceeded, string.Empty, "bundle_size_limit_exceeded", data.Status);
				}

				WriteFile(stagingPath, "manifest.json", manifest);
				try
				{
					Directory.Move(stagingPath, finalPath);
				}
				catch (IOException) when (Directory.Exists(finalPath) || File.Exists(finalPath))
				{
					return Result(false, PerfMeterCaptureBundleExportStatus.Conflict, relativePath, "destination_exists", data.Status);
				}

				string retentionWarning = string.Empty;
				try
				{
					ApplyRetentionAfterCommit(bundleRoot);
				}
				catch (Exception exception) when (IsPathOrIoException(exception))
				{
					retentionWarning = "retention_warning: " + exception.Message;
				}

				PerfMeterCaptureBundleStatusSnapshot exportedStatus = new PerfMeterCaptureBundleStatusSnapshot(
					data.Status.Availability,
					PerfMeterCaptureBundleState.Exported,
					data.Status.BundleId,
					data.Status.CaptureId,
					data.Status.CaptureState,
					data.Status.RequestedTool,
					data.Status.BaselineSampleCount,
					data.Status.CaptureSampleCount,
					data.Status.DroppedCaptureSampleCount,
					data.Status.AlertEventCount,
					data.Status.AlertEventsTruncated,
					data.Status.ScreenshotState,
					externalMetadata.State,
					relativePath,
					data.Status.Warning,
					data.Status.MemorySnapshotState);
				return Result(true, PerfMeterCaptureBundleExportStatus.Exported, relativePath, retentionWarning, exportedStatus);
			}
			catch (MemorySnapshotQuotaExceededException)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.QuotaExceeded, string.Empty, "memory_snapshot_size_limit_exceeded", data.Status);
			}
			catch (Exception exception)
			{
				return Result(false, PerfMeterCaptureBundleExportStatus.IoError, string.Empty, "bundle_io_error: " + exception.Message, data.Status);
			}
			finally
			{
				TryDeleteOwnedStaging(stagingPath);
			}
		}

		internal static PerfMeterCaptureCapabilitiesSnapshot GetCapabilities()
		{
			bool windows = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
			bool linux = Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer;
			UnityEngine.Rendering.GraphicsDeviceType api = SystemInfo.graphicsDeviceType;
			bool renderDoc = (windows || linux) && (api == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 || api == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 || api == UnityEngine.Rendering.GraphicsDeviceType.Vulkan);
			bool pix = windows && api == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;
			return new PerfMeterCaptureCapabilitiesSnapshot(
				renderDoc,
				pix,
				Application.isPlaying && !Application.isBatchMode,
				120,
				600,
				MaxBundleBytes,
				MaxScreenshotBytes,
				TotalQuotaBytes,
				MaxCommittedBundles,
				RetentionDays,
				RelativeBundleRoot,
				MaxMemorySnapshotBytes);
		}

		private static Dictionary<string, byte[]> BuildComponents(PerfMeterCaptureBundleExportData data, ExternalArtifactMetadata externalMetadata, byte[] externalArtifactBytes)
		{
			Dictionary<string, byte[]> components = new Dictionary<string, byte[]>(StringComparer.Ordinal)
			{
				{ ".sgg-perfmeter-bundle", Utf8(OwnershipMarker) },
				{ "session.json", Utf8(RedactSensitivePaths(PerfMeterSessionExporter.BuildJson(data.SessionSummary, data.BaselineSamples, data.RuntimeStatus))) },
				{ "capture-samples.json", Utf8(RedactSensitivePaths(PerfMeterSessionExporter.BuildCaptureSamplesJson(data.Status.CaptureId, data.CaptureSamples))) },
				{ "alerts.json", Utf8(BuildAlerts(data)) },
				{ "context.json", Utf8(RedactSensitivePaths(BuildContext(data))) }
			};

			if (data.CaptureOptions.Tool != PerfMeterCaptureTool.MemoryProfiler)
			{
				components.Add("external-capture.json", Utf8(BuildExternalCapture(data, externalMetadata)));
			}

			if (data.ScreenshotBytes != null && data.ScreenshotBytes.Length > 0)
			{
				components.Add("screenshot.png", data.ScreenshotBytes);
			}

			if (externalArtifactBytes != null)
			{
				components.Add("external-capture" + externalMetadata.Extension, externalArtifactBytes);
			}

			return components;
		}

		private static string BuildManifest(PerfMeterCaptureBundleExportData data, ExternalArtifactMetadata externalMetadata, MemoryArtifactMetadata memoryMetadata, List<FileManifestEntry> entries)
		{
			StringBuilder builder = new StringBuilder(2048);
			builder.Append("{\"schema\":").Append(JsonString(BundleSchema));
			builder.Append(",\"schema_version\":1");
			builder.Append(",\"package\":").Append(JsonString(PerfMeterSessionExporter.RuntimePackageIdentity.Name));
			builder.Append(",\"package_version\":").Append(JsonString(PerfMeterSessionExporter.RuntimePackageIdentity.Version));
			builder.Append(",\"bundle_id\":").Append(JsonString(data.Status.BundleId));
			builder.Append(",\"capture_id\":").Append(JsonString(data.Status.CaptureId));
			builder.Append(",\"started_utc\":").Append(JsonString(data.StartedUtc));
			builder.Append(",\"completed_utc\":").Append(JsonString(data.CompletedUtc));
			builder.Append(",\"bundle_state\":").Append(JsonString(data.Status.State.ToString()));
			builder.Append(",\"capture_state\":").Append(JsonString(data.Status.CaptureState.ToString()));
			builder.Append(",\"requested_tool\":").Append(JsonString(data.Status.RequestedTool.ToString()));
			builder.Append(",\"tool_identity\":\"unknown\"");
			builder.Append(",\"tool_version\":\"unknown\"");
			builder.Append(",\"baseline_sample_count\":").Append(data.BaselineSamples.Length);
			builder.Append(",\"capture_sample_count\":").Append(data.CaptureSamples.Length);
			builder.Append(",\"dropped_capture_sample_count\":").Append(data.Status.DroppedCaptureSampleCount);
			builder.Append(",\"alert_event_count\":").Append(data.AlertEvents.Length);
			builder.Append(",\"alert_events_truncated\":").Append(JsonBool(data.AlertEventsTruncated));
			builder.Append(",\"screenshot_state\":").Append(JsonString(data.Status.ScreenshotState.ToString()));
			builder.Append(",\"contains_runtime_pixels\":").Append(JsonBool(data.ScreenshotBytes != null && data.ScreenshotBytes.Length > 0));
			builder.Append(",\"external_artifact_state\":").Append(JsonString(externalMetadata.State.ToString()));
			builder.Append(",\"memory_snapshot_state\":").Append(JsonString(memoryMetadata.State.ToString()));
			builder.Append(",\"memory_snapshot_trigger\":").Append(JsonString(memoryMetadata.Trigger.ToString()));
			builder.Append(",\"memory_snapshot_requested_flags\":").Append(JsonString(memoryMetadata.RequestedFlags.ToString()));
			builder.Append(",\"memory_snapshot_backend_id\":").Append(JsonString(memoryMetadata.BackendId));
			builder.Append(",\"memory_snapshot_backend_version\":").Append(JsonString(memoryMetadata.BackendVersion));
			builder.Append(",\"memory_snapshot_file\":").Append(JsonString(string.IsNullOrEmpty(memoryMetadata.SourcePath) ? string.Empty : "memory-snapshot.snap"));
			builder.Append(",\"memory_snapshot_size_bytes\":").Append(memoryMetadata.SizeBytes);
			builder.Append(",\"memory_snapshot_sha256\":").Append(JsonString(memoryMetadata.Hash));
			builder.Append(",\"contains_sensitive_memory\":").Append(JsonBool(!string.IsNullOrEmpty(memoryMetadata.SourcePath)));
			builder.Append(",\"warning\":").Append(JsonString(MemoryWarning(data.Status.Warning, !string.IsNullOrEmpty(memoryMetadata.SourcePath))));
			builder.Append(",\"files\":[");
			for (int i = 0; i < entries.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				FileManifestEntry entry = entries[i];
				builder.Append("{\"path\":").Append(JsonString(entry.Path));
				builder.Append(",\"size_bytes\":").Append(entry.SizeBytes);
				builder.Append(",\"sha256\":").Append(JsonString(entry.Hash));
				builder.Append('}');
			}

			builder.Append("]}");
			return builder.ToString();
		}

		private static string BuildAlerts(PerfMeterCaptureBundleExportData data)
		{
			StringBuilder builder = new StringBuilder(512 + data.AlertEvents.Length * 384);
			builder.Append("{\"schema\":\"sgg.perfmeter.capture-alerts\",\"schema_version\":1");
			builder.Append(",\"capture_id\":").Append(JsonString(data.Status.CaptureId));
			builder.Append(",\"truncated\":").Append(JsonBool(data.AlertEventsTruncated));
			builder.Append(",\"events\":[");
			for (int i = 0; i < data.AlertEvents.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(',');
				}

				PerfMeterAlertSnapshot alert = data.AlertEvents[i];
				builder.Append("{\"rule_id\":").Append(JsonString(alert.RuleId));
				builder.Append(",\"metric\":").Append(JsonString(alert.Metric.ToString()));
				builder.Append(",\"value\":").Append(JsonNumber(alert.Value));
				builder.Append(",\"collection_frame\":").Append(alert.CollectionFrame);
				builder.Append(",\"time_seconds\":").Append(JsonNumber(alert.TimeSeconds));
				builder.Append(",\"classification\":").Append(JsonString(alert.Classification.ToString()));
				builder.Append(",\"capture_id\":").Append(JsonString(alert.CaptureId));
				builder.Append(",\"message\":").Append(JsonString(RedactSensitivePaths(alert.Message)));
				builder.Append('}');
			}

			builder.Append("]}");
			return builder.ToString();
		}

		private static string BuildContext(PerfMeterCaptureBundleExportData data)
		{
			PerfMeterDeviceSnapshot device = data.Device;
			PerfMeterCameraSnapshot camera = data.Camera;
			PerfMeterRenderGraphSnapshot render = data.Render;
			PerfMeterStatusSnapshot runtime = data.RuntimeStatus;
			StringBuilder builder = new StringBuilder(1536);
			builder.Append("{\"schema\":\"sgg.perfmeter.capture-context\",\"schema_version\":1");
			builder.Append(",\"device\":{");
			builder.Append("\"unity_version\":").Append(JsonString(device.UnityVersion));
			builder.Append(",\"platform\":").Append(JsonString(device.ApplicationPlatform.ToString()));
			builder.Append(",\"operating_system\":").Append(JsonString(device.OperatingSystem));
			builder.Append(",\"device_model\":").Append(JsonString(device.DeviceModel));
			builder.Append(",\"graphics_device_type\":").Append(JsonString(device.GraphicsDeviceType.ToString()));
			builder.Append(",\"graphics_device_name\":").Append(JsonString(device.GraphicsDeviceName));
			builder.Append(",\"render_pipeline\":").Append(JsonString(device.RenderPipeline.ToString()));
			builder.Append('}');
			builder.Append(",\"camera\":{");
			builder.Append("\"available\":").Append(JsonBool(camera.IsAvailable));
			builder.Append(",\"name\":").Append(JsonString(camera.CameraName));
			builder.Append(",\"scene_name\":").Append(JsonString(camera.SceneName));
			builder.Append(",\"projection\":").Append(JsonString(camera.Projection.ToString()));
			builder.Append(",\"field_of_view\":").Append(JsonNumber(camera.FieldOfView));
			builder.Append(",\"warning\":").Append(JsonString(RedactSensitivePaths(camera.Warning)));
			builder.Append('}');
			builder.Append(",\"render\":{");
			builder.Append("\"availability\":").Append(JsonString(render.Availability.ToString()));
			builder.Append(",\"state\":").Append(JsonString(render.State.ToString()));
			builder.Append(",\"pipeline\":").Append(JsonString(render.RenderPipeline.ToString()));
			builder.Append(",\"integration_name\":").Append(JsonString(render.IntegrationName));
			builder.Append(",\"warning\":").Append(JsonString(RedactSensitivePaths(render.Warning)));
			builder.Append('}');
			builder.Append(",\"runtime\":{");
			builder.Append("\"state\":").Append(JsonString(runtime.State.ToString()));
			builder.Append(",\"collection_frame\":").Append(runtime.CollectionFrame);
			builder.Append(",\"bottleneck\":").Append(JsonString(runtime.Bottleneck.ToString()));
			builder.Append(",\"warning\":").Append(JsonString(RedactSensitivePaths(runtime.Warning)));
			builder.Append('}');
			builder.Append(",\"configured_settings\":");
			PerfMeterSessionExporter.AppendSettings(builder, data.ConfiguredSettings);
			builder.Append(",\"effective_settings\":");
			PerfMeterSessionExporter.AppendSettings(builder, data.EffectiveSettings);
			builder.Append('}');
			return builder.ToString();
		}

		private static string BuildExternalCapture(PerfMeterCaptureBundleExportData data, ExternalArtifactMetadata metadata)
		{
			StringBuilder builder = new StringBuilder(512);
			builder.Append("{\"schema\":\"sgg.perfmeter.external-capture\",\"schema_version\":1");
			builder.Append(",\"capture_id\":").Append(JsonString(data.Status.CaptureId));
			builder.Append(",\"requested_tool\":").Append(JsonString(data.Status.RequestedTool.ToString()));
			builder.Append(",\"tool_identity\":\"unknown\"");
			builder.Append(",\"tool_version\":\"unknown\"");
			builder.Append(",\"artifact_state\":").Append(JsonString(metadata.State.ToString()));
			builder.Append(",\"artifact_extension\":").Append(JsonString(metadata.Extension));
			builder.Append(",\"artifact_size_bytes\":").Append(metadata.SizeBytes);
			builder.Append(",\"artifact_sha256\":").Append(JsonString(metadata.Hash));
			builder.Append(",\"artifact_path_included\":false");
			builder.Append(",\"artifact_file\":").Append(JsonString(string.IsNullOrEmpty(metadata.Extension) ? string.Empty : "external-capture" + metadata.Extension));
			builder.Append(",\"association_verified\":false}");
			return builder.ToString();
		}

		private static string BuildMemorySnapshotMetadata(PerfMeterCaptureBundleExportData data, MemoryArtifactMetadata metadata)
		{
			StringBuilder builder = new StringBuilder(768);
			builder.Append("{\"schema\":\"sgg.perfmeter.memory-snapshot\",\"schema_version\":1");
			builder.Append(",\"capture_id\":").Append(JsonString(data.Status.CaptureId));
			builder.Append(",\"state\":").Append(JsonString(metadata.State.ToString()));
			builder.Append(",\"trigger\":").Append(JsonString(metadata.Trigger.ToString()));
			builder.Append(",\"requested_capture_flags\":").Append(JsonString(metadata.RequestedFlags.ToString()));
			builder.Append(",\"capture_flags_confirmed\":false");
			builder.Append(",\"backend_id\":").Append(JsonString(metadata.BackendId));
			builder.Append(",\"backend_version\":").Append(JsonString(metadata.BackendVersion));
			builder.Append(",\"started_time_seconds\":").Append(JsonNumber(metadata.StartedTimeSeconds));
			builder.Append(",\"completed_time_seconds\":").Append(JsonNumber(metadata.CompletedTimeSeconds));
			builder.Append(",\"artifact_file\":").Append(JsonString(string.IsNullOrEmpty(metadata.SourcePath) ? string.Empty : "memory-snapshot.snap"));
			builder.Append(",\"artifact_size_bytes\":").Append(metadata.SizeBytes);
			builder.Append(",\"artifact_sha256\":").Append(JsonString(metadata.Hash));
			builder.Append(",\"contains_sensitive_memory\":").Append(JsonBool(!string.IsNullOrEmpty(metadata.SourcePath)));
			builder.Append(",\"warning\":").Append(JsonString(MemoryWarning(metadata.Warning, !string.IsNullOrEmpty(metadata.SourcePath))));
			builder.Append('}');
			return builder.ToString();
		}

		private static bool TryResolveBundleDestination(string path, out string projectRoot, out string bundleRoot, out string finalPath, out string relativePath, out string error)
		{
			projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			bundleRoot = Path.GetFullPath(Path.Combine(projectRoot, RelativeBundleRoot));
			finalPath = string.Empty;
			relativePath = string.Empty;
			error = string.Empty;
			if (string.IsNullOrWhiteSpace(path))
			{
				error = "path_required";
				return false;
			}

			if (Path.IsPathRooted(path) || ContainsTraversalSegment(path))
			{
				error = "path_must_be_relative_without_traversal";
				return false;
			}

			string combined = Path.Combine(projectRoot, path);
			finalPath = Path.GetFullPath(combined).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string normalizedBundleRoot = bundleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!finalPath.StartsWith(normalizedBundleRoot, PathComparison) || !string.Equals(Path.GetDirectoryName(finalPath), bundleRoot, PathComparison))
			{
				error = "path_must_be_direct_child_of_bundle_root";
				return false;
			}

			if (!IsSafeExistingPath(projectRoot, finalPath, out error))
			{
				return false;
			}

			string normalizedProjectRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			relativePath = finalPath.Substring(normalizedProjectRoot.Length).Replace('\\', '/');
			return true;
		}

		private static bool TryObserveExternalArtifact(string projectRoot, PerfMeterCaptureTool tool, string path, out ExternalArtifactMetadata metadata, out string error)
		{
			metadata = ExternalArtifactMetadata.Unavailable;
			error = string.Empty;
			if (tool == PerfMeterCaptureTool.MemoryProfiler)
			{
				if (!string.IsNullOrWhiteSpace(path))
				{
					error = "external_artifact_not_supported_for_memory_snapshot";
					return false;
				}

				return true;
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				return true;
			}

			if (Path.IsPathRooted(path) || ContainsTraversalSegment(path))
			{
				error = "external_artifact_path_must_be_relative_without_traversal";
				return false;
			}

			string combined = Path.Combine(projectRoot, path);
			string fullPath = Path.GetFullPath(combined);
			string normalizedRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(normalizedRoot, PathComparison) || !File.Exists(fullPath) || !IsSafeExistingPath(projectRoot, fullPath, out error))
			{
				error = string.IsNullOrEmpty(error) ? "external_artifact_must_be_project_local_regular_file" : error;
				return false;
			}

			string extension = Path.GetExtension(fullPath).ToLowerInvariant();
			string expected = tool == PerfMeterCaptureTool.Pix ? ".wpix" : ".rdc";
			if (!string.Equals(extension, expected, StringComparison.Ordinal))
			{
				error = "external_artifact_extension_mismatch";
				return false;
			}

			FileInfo info = new FileInfo(fullPath);
			if ((info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
			{
				error = "external_artifact_must_be_project_local_regular_file";
				return false;
			}

			metadata = new ExternalArtifactMetadata(PerfMeterCaptureExternalArtifactState.FileObserved, extension, info.Length, string.Empty, fullPath);
			return true;
		}

		private static bool TryInspectMemorySnapshot(string projectRoot, PerfMeterMemorySnapshotArtifact artifact, out MemoryArtifactMetadata metadata, out string error)
		{
			PerfMeterMemorySnapshotStatusSnapshot status = artifact.Status;
			metadata = new MemoryArtifactMetadata(
				status.State,
				status.Trigger,
				status.RequestedCaptureFlags,
				status.BackendId,
				status.BackendVersion,
				status.StartedTimeSeconds,
				status.CompletedTimeSeconds,
				status.ArtifactSizeBytes,
				string.Empty,
				artifact.SourcePath,
				status.Warning);
			error = string.Empty;
			if (!artifact.IsAvailable)
			{
				return true;
			}

			try
			{
				string fullPath = Path.GetFullPath(artifact.SourcePath);
				string snapshotRoot = Path.GetFullPath(Path.Combine(projectRoot, PerfMeterMemorySnapshotStorage.RelativeSnapshotRoot)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string normalizedRoot = snapshotRoot + Path.DirectorySeparatorChar;
				if (!fullPath.StartsWith(normalizedRoot, PathComparison) ||
					!string.Equals(Path.GetDirectoryName(fullPath), snapshotRoot, PathComparison) ||
					!string.Equals(Path.GetExtension(fullPath), ".snap", StringComparison.OrdinalIgnoreCase) ||
					!File.Exists(fullPath) ||
					!IsSafeExistingPath(projectRoot, fullPath, out error))
				{
					error = string.IsNullOrEmpty(error) ? "memory_snapshot_artifact_must_be_owned_project_local_file" : error;
					return false;
				}

				FileInfo info = new FileInfo(fullPath);
				if ((info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0 || info.Length <= 0L)
				{
					error = "memory_snapshot_artifact_must_be_regular_nonempty_file";
					return false;
				}

				if (info.Length > MaxMemorySnapshotBytes)
				{
					error = "memory_snapshot_size_limit_exceeded";
					return false;
				}

				if (status.ArtifactSizeBytes > 0L && info.Length != status.ArtifactSizeBytes)
				{
					error = "memory_snapshot_artifact_changed_after_capture";
					return false;
				}

				metadata = metadata.WithSource(fullPath, info.Length);
				return true;
			}
			catch (Exception exception) when (IsPathOrIoException(exception))
			{
				error = "memory_snapshot_artifact_io_error: " + exception.Message;
				return false;
			}
		}

		private static bool IsSafeExistingPath(string root, string target, out string error)
		{
			error = string.Empty;
			string current = Path.GetFullPath(target);
			while (!string.IsNullOrEmpty(current) && current.Length >= root.Length)
			{
				if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
				{
					error = "reparse_points_are_not_allowed";
					return false;
				}

				if (string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), PathComparison))
				{
					break;
				}

				current = Path.GetDirectoryName(current);
			}

			return true;
		}

		private static void ApplyRetentionAfterCommit(string bundleRoot)
		{
			DirectoryInfo root = new DirectoryInfo(bundleRoot);
			List<CommittedBundle> bundles = new List<CommittedBundle>();
			DirectoryInfo[] directories = root.GetDirectories();
			for (int i = 0; i < directories.Length; i++)
			{
				DirectoryInfo directory = directories[i];
				if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
				{
					continue;
				}

				if (directory.Name.StartsWith(".sgg-perfmeter-staging-", StringComparison.Ordinal))
				{
					string stagingMarker = Path.Combine(directory.FullName, ".sgg-perfmeter-bundle");
					if (directory.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1d) && File.Exists(stagingMarker) && new FileInfo(stagingMarker).Length == Encoding.UTF8.GetByteCount(OwnershipMarker) && string.Equals(File.ReadAllText(stagingMarker), OwnershipMarker, StringComparison.Ordinal))
					{
						directory.Delete(true);
					}

					continue;
				}

				string markerPath = Path.Combine(directory.FullName, ".sgg-perfmeter-bundle");
				string manifestPath = Path.Combine(directory.FullName, "manifest.json");
				if (!File.Exists(markerPath) || new FileInfo(markerPath).Length != Encoding.UTF8.GetByteCount(OwnershipMarker) || !string.Equals(File.ReadAllText(markerPath), OwnershipMarker, StringComparison.Ordinal) || !File.Exists(manifestPath))
				{
					continue;
				}

				FileInfo manifestInfo = new FileInfo(manifestPath);
				if (manifestInfo.Length <= 0L || manifestInfo.Length > 256L * 1024L)
				{
					continue;
				}

				string manifest = File.ReadAllText(manifestPath);
				if (!manifest.StartsWith("{\"schema\":\"" + BundleSchema + "\",\"schema_version\":1,", StringComparison.Ordinal) || manifest.IndexOf("\"bundle_id\":", StringComparison.Ordinal) < 0 || manifest.IndexOf("\"files\":[", StringComparison.Ordinal) < 0)
				{
					continue;
				}

				bundles.Add(new CommittedBundle(directory, GetDirectoryBytes(directory.FullName)));
			}

			bundles.Sort((left, right) => left.Directory.LastWriteTimeUtc.CompareTo(right.Directory.LastWriteTimeUtc));
			DateTime cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
			for (int i = bundles.Count - 1; i >= 0; i--)
			{
				if (bundles[i].Directory.LastWriteTimeUtc < cutoff)
				{
					bundles[i].Directory.Delete(true);
					bundles.RemoveAt(i);
				}
			}

			long total = 0L;
			for (int i = 0; i < bundles.Count; i++)
			{
				total += bundles[i].SizeBytes;
			}

			while (bundles.Count > MaxCommittedBundles || total > TotalQuotaBytes)
			{
				CommittedBundle oldest = bundles[0];
				oldest.Directory.Delete(true);
				total -= oldest.SizeBytes;
				bundles.RemoveAt(0);
			}

		}

		private static StringComparison PathComparison
		{
			get
			{
				bool windows = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
				return windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			}
		}

		private static bool ContainsTraversalSegment(string path)
		{
			string[] segments = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < segments.Length; i++)
			{
				if (string.Equals(segments[i], ".", StringComparison.Ordinal) || string.Equals(segments[i], "..", StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private static byte[] ReadBoundedFile(string path, string projectRoot, long maxBytes)
		{
			if (maxBytes < 0L)
			{
				throw new InvalidDataException();
			}

			using (FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (!IsSafeExistingPath(projectRoot, path, out _) || (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
				{
					throw new IOException("External artifact changed during validation.");
				}

				if (source.Length > maxBytes)
				{
					throw new InvalidDataException();
				}

				using (MemoryStream destination = new MemoryStream((int)Math.Min(source.Length, int.MaxValue)))
				{
					byte[] buffer = new byte[81920];
					long total = 0L;
					int read;
					while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
					{
						total += read;
						if (total > maxBytes)
						{
							throw new InvalidDataException();
						}

						destination.Write(buffer, 0, read);
					}

					return destination.ToArray();
				}
			}
		}

		private static FileManifestEntry CopyMemorySnapshot(string stagingPath, string projectRoot, MemoryArtifactMetadata metadata)
		{
			string destinationPath = Path.Combine(stagingPath, "memory-snapshot.snap");
			using (FileStream source = new FileStream(metadata.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (FileStream destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (SHA256 sha = SHA256.Create())
			{
				if (!IsSafeExistingPath(projectRoot, metadata.SourcePath, out _) || (File.GetAttributes(metadata.SourcePath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
				{
					throw new IOException("Memory snapshot changed during validation.");
				}

				if (source.Length > MaxMemorySnapshotBytes)
				{
					throw new MemorySnapshotQuotaExceededException();
				}

				if (source.Length != metadata.SizeBytes)
				{
					throw new InvalidDataException("Memory snapshot changed before export.");
				}

				byte[] buffer = new byte[81920];
				long total = 0L;
				int read;
				while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
				{
					total += read;
					if (total > MaxMemorySnapshotBytes)
					{
						throw new MemorySnapshotQuotaExceededException();
					}

					destination.Write(buffer, 0, read);
					sha.TransformBlock(buffer, 0, read, buffer, 0);
				}

				sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
				destination.Flush(true);
				if (total != metadata.SizeBytes)
				{
					throw new InvalidDataException("Memory snapshot changed during export.");
				}

				return new FileManifestEntry("memory-snapshot.snap", total, ToHex(sha.Hash));
			}
		}

		private static string MemoryWarning(string warning, bool containsSensitiveMemory)
		{
			string redacted = RedactSensitivePaths(warning);
			if (!containsSensitiveMemory)
			{
				return redacted;
			}

			const string sensitiveWarning = "Memory snapshot contains sensitive process memory.";
			return string.IsNullOrEmpty(redacted) ? sensitiveWarning : redacted + " " + sensitiveWarning;
		}

		private sealed class MemorySnapshotQuotaExceededException : Exception
		{
		}

		private static bool IsPathOrIoException(Exception exception)
		{
			return exception is ArgumentException ||
				exception is IOException ||
				exception is UnauthorizedAccessException ||
				exception is NotSupportedException ||
				exception is System.Security.SecurityException;
		}

		private static void WriteFile(string stagingPath, string relativePath, byte[] bytes)
		{
			string fullPath = Path.Combine(stagingPath, relativePath);
			using (FileStream stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush(true);
			}
		}

		private static long GetDirectoryBytes(string path)
		{
			long total = 0L;
			string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				total += new FileInfo(files[i]).Length;
			}

			return total;
		}

		private static long GetTotalBytes(Dictionary<string, byte[]> components)
		{
			long total = 0L;
			foreach (byte[] bytes in components.Values)
			{
				total += bytes.LongLength;
			}

			return total;
		}

		private static void TryDeleteOwnedStaging(string stagingPath)
		{
			try
			{
				if (Directory.Exists(stagingPath) && Path.GetFileName(stagingPath).StartsWith(".sgg-perfmeter-staging-", StringComparison.Ordinal))
				{
					Directory.Delete(stagingPath, true);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		private static string RedactSensitivePaths(string value)
		{
			string redacted = value ?? string.Empty;
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			redacted = ReplacePath(redacted, projectRoot, "<project>");
			redacted = ReplacePath(redacted, Application.persistentDataPath, "<persistent-data>");
			redacted = ReplacePath(redacted, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "<user>");
			return redacted;
		}

		private static string ReplacePath(string value, string path, string replacement)
		{
			if (string.IsNullOrEmpty(path))
			{
				return value;
			}

			string result = value.Replace(path.Replace('\\', '/'), replacement);
			return result.Replace(path.Replace('/', '\\'), replacement);
		}

		private static byte[] Utf8(string value)
		{
			return Encoding.UTF8.GetBytes(value ?? string.Empty);
		}

		private static string Sha256(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
			{
				return ToHex(sha.ComputeHash(bytes));
			}
		}

		private static string ToHex(byte[] bytes)
		{
			StringBuilder builder = new StringBuilder(bytes.Length * 2);
			for (int i = 0; i < bytes.Length; i++)
			{
				builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
			}

			return builder.ToString();
		}

		private static string JsonString(string value)
		{
			StringBuilder builder = new StringBuilder((value ?? string.Empty).Length + 2);
			builder.Append('"');
			string safe = value ?? string.Empty;
			for (int i = 0; i < safe.Length; i++)
			{
				switch (safe[i])
				{
					case '\\': builder.Append("\\\\"); break;
					case '"': builder.Append("\\\""); break;
					case '\n': builder.Append("\\n"); break;
					case '\r': builder.Append("\\r"); break;
					case '\t': builder.Append("\\t"); break;
					default:
						if (safe[i] < ' ')
						{
							builder.Append("\\u").Append(((int)safe[i]).ToString("x4", CultureInfo.InvariantCulture));
						}
						else
						{
							builder.Append(safe[i]);
						}
						break;
				}
			}

			builder.Append('"');
			return builder.ToString();
		}

		private static string JsonBool(bool value) => value ? "true" : "false";

		private static string JsonNumber(double value)
		{
			return double.IsNaN(value) || double.IsInfinity(value) ? JsonString(value.ToString(CultureInfo.InvariantCulture)) : value.ToString("R", CultureInfo.InvariantCulture);
		}

		private static PerfMeterCaptureBundleExportResult Result(bool success, PerfMeterCaptureBundleExportStatus status, string path, string error, PerfMeterCaptureBundleStatusSnapshot bundle)
		{
			return new PerfMeterCaptureBundleExportResult(success, status, path, error, bundle);
		}

		private readonly struct ExternalArtifactMetadata
		{
			internal ExternalArtifactMetadata(PerfMeterCaptureExternalArtifactState state, string extension, long sizeBytes, string hash, string sourcePath)
			{
				State = state;
				Extension = extension ?? string.Empty;
				SizeBytes = Math.Max(0L, sizeBytes);
				Hash = hash ?? string.Empty;
				SourcePath = sourcePath ?? string.Empty;
			}

			internal static ExternalArtifactMetadata Unavailable => new ExternalArtifactMetadata(PerfMeterCaptureExternalArtifactState.Unavailable, string.Empty, 0L, string.Empty, string.Empty);
			internal PerfMeterCaptureExternalArtifactState State { get; }
			internal string Extension { get; }
			internal long SizeBytes { get; }
			internal string Hash { get; }
			internal string SourcePath { get; }

			internal ExternalArtifactMetadata WithObservedBytes(byte[] bytes)
			{
				return new ExternalArtifactMetadata(State, Extension, bytes.LongLength, Sha256(bytes), SourcePath);
			}
		}

		private readonly struct MemoryArtifactMetadata
		{
			internal MemoryArtifactMetadata(
				PerfMeterMemorySnapshotState state,
				PerfMeterMemorySnapshotTrigger trigger,
				PerfMeterMemoryCaptureFlags requestedFlags,
				string backendId,
				string backendVersion,
				double startedTimeSeconds,
				double completedTimeSeconds,
				long sizeBytes,
				string hash,
				string sourcePath,
				string warning)
			{
				State = state;
				Trigger = trigger;
				RequestedFlags = requestedFlags;
				BackendId = backendId ?? string.Empty;
				BackendVersion = backendVersion ?? string.Empty;
				StartedTimeSeconds = startedTimeSeconds;
				CompletedTimeSeconds = completedTimeSeconds;
				SizeBytes = Math.Max(0L, sizeBytes);
				Hash = hash ?? string.Empty;
				SourcePath = sourcePath ?? string.Empty;
				Warning = warning ?? string.Empty;
			}

			internal PerfMeterMemorySnapshotState State { get; }
			internal PerfMeterMemorySnapshotTrigger Trigger { get; }
			internal PerfMeterMemoryCaptureFlags RequestedFlags { get; }
			internal string BackendId { get; }
			internal string BackendVersion { get; }
			internal double StartedTimeSeconds { get; }
			internal double CompletedTimeSeconds { get; }
			internal long SizeBytes { get; }
			internal string Hash { get; }
			internal string SourcePath { get; }
			internal string Warning { get; }

			internal MemoryArtifactMetadata WithSource(string sourcePath, long sizeBytes)
			{
				return new MemoryArtifactMetadata(State, Trigger, RequestedFlags, BackendId, BackendVersion, StartedTimeSeconds, CompletedTimeSeconds, sizeBytes, Hash, sourcePath, Warning);
			}

			internal MemoryArtifactMetadata WithObservedFile(long sizeBytes, string hash)
			{
				return new MemoryArtifactMetadata(State, Trigger, RequestedFlags, BackendId, BackendVersion, StartedTimeSeconds, CompletedTimeSeconds, sizeBytes, hash, SourcePath, Warning);
			}
		}

		private readonly struct FileManifestEntry
		{
			internal FileManifestEntry(string path, long sizeBytes, string hash)
			{
				Path = path;
				SizeBytes = sizeBytes;
				Hash = hash;
			}

			internal string Path { get; }
			internal long SizeBytes { get; }
			internal string Hash { get; }
		}

		private readonly struct CommittedBundle
		{
			internal CommittedBundle(DirectoryInfo directory, long sizeBytes)
			{
				Directory = directory;
				SizeBytes = sizeBytes;
			}

			internal DirectoryInfo Directory { get; }
			internal long SizeBytes { get; }
		}
	}
}
