using System;
using System.IO;
using System.Threading;

namespace SGG.PerfMeter
{
	internal readonly struct PerfMeterCaptureBundleExportEnvironment
	{
		internal PerfMeterCaptureBundleExportEnvironment(
			string projectRoot,
			string bundleRoot,
			string persistentDataPath,
			string userProfilePath,
			bool caseInsensitivePaths,
			string hostNamespace)
		{
			ProjectRoot = projectRoot ?? string.Empty;
			BundleRoot = bundleRoot ?? string.Empty;
			PersistentDataPath = persistentDataPath ?? string.Empty;
			UserProfilePath = userProfilePath ?? string.Empty;
			PathComparison = caseInsensitivePaths ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			HostNamespace = hostNamespace ?? string.Empty;
		}

		internal string ProjectRoot { get; }
		internal string BundleRoot { get; }
		internal string PersistentDataPath { get; }
		internal string UserProfilePath { get; }
		internal StringComparison PathComparison { get; }
		internal string HostNamespace { get; }
	}

	internal sealed class PerfMeterCaptureBundleExportCoordinator
	{
		private readonly object _sync = new object();
		private readonly Func<WaitCallback, object, bool> _queueWorkItem;
		private ExportOperation _operation;
		private PerfMeterCaptureBundleExportCompletion _pendingCompletion;
		private bool _blockingExportActive;

		internal PerfMeterCaptureBundleExportCoordinator(Func<WaitCallback, object, bool> queueWorkItem = null)
		{
			_queueWorkItem = queueWorkItem ?? ((callback, state) => ThreadPool.QueueUserWorkItem(callback, state));
		}

		internal PerfMeterCaptureBundleExportRequestResult Request(
			PerfMeterCaptureBundleExportData data,
			string path,
			string externalArtifactPath,
			bool requireAuthoritativeExternalArtifact,
			PerfMeterCaptureBundleExportEnvironment environment,
			Func<string, bool> beginExport,
			out string exportId)
		{
			exportId = string.Empty;
			if (data == null)
			{
				return PerfMeterCaptureBundleExportRequestResult.NotFound;
			}

			if (!data.Status.IsExportReady)
			{
				return PerfMeterCaptureBundleExportRequestResult.NotReady;
			}

			ExportOperation operation;
			lock (_sync)
			{
				if (_blockingExportActive || _pendingCompletion != null || (_operation != null && !_operation.Status.IsTerminal))
				{
					return PerfMeterCaptureBundleExportRequestResult.AlreadyActive;
				}

				exportId = Guid.NewGuid().ToString("N");
				if (beginExport != null && !beginExport(exportId))
				{
					exportId = string.Empty;
					return PerfMeterCaptureBundleExportRequestResult.Conflict;
				}

				operation = new ExportOperation(
					exportId,
					data,
					path,
					externalArtifactPath,
					requireAuthoritativeExternalArtifact,
					environment);
				_operation = operation;
				operation.Status = CreateQueuedStatus(operation);
			}

			try
			{
				if (!_queueWorkItem(WorkItem, operation))
				{
					SetTerminal(
						operation,
						new PerfMeterCaptureBundleExportResult(
							false,
							PerfMeterCaptureBundleExportStatus.IoError,
							string.Empty,
							"export_queue_rejected",
							data.Status),
						PerfMeterCaptureBundleExportPhase.Failed,
						false);
					return PerfMeterCaptureBundleExportRequestResult.Failed;
				}

				return PerfMeterCaptureBundleExportRequestResult.Started;
			}
			catch (Exception exception)
			{
				SetTerminal(
					operation,
					new PerfMeterCaptureBundleExportResult(
						false,
						PerfMeterCaptureBundleExportStatus.IoError,
						string.Empty,
						"export_queue_failed: " + PerfMeterCaptureBundleExporter.RedactSensitivePaths(exception.Message, environment),
						data.Status),
					PerfMeterCaptureBundleExportPhase.Failed,
					false);
				return PerfMeterCaptureBundleExportRequestResult.Failed;
			}
		}

		internal PerfMeterCaptureBundleExportStatusSnapshot GetStatus(string exportId = null)
		{
			lock (_sync)
			{
				if (_operation == null || (!string.IsNullOrEmpty(exportId) && !string.Equals(_operation.ExportId, exportId, StringComparison.Ordinal)))
				{
					return PerfMeterCaptureBundleExportStatusSnapshot.None;
				}

				return _operation.Status;
			}
		}

		internal void AppendTerminalWarning(string exportId, string warning)
		{
			if (string.IsNullOrEmpty(warning))
			{
				return;
			}

			lock (_sync)
			{
				if (_operation == null ||
					!string.Equals(_operation.ExportId, exportId, StringComparison.Ordinal) ||
					!_operation.Status.IsTerminal)
				{
					return;
				}

				PerfMeterCaptureBundleExportStatusSnapshot status = _operation.Status;
				_operation.Status = new PerfMeterCaptureBundleExportStatusSnapshot(
					status.ExportId,
					status.CaptureId,
					status.BundleId,
					status.Phase,
					status.Progress,
					status.BytesProcessed,
					status.TotalBytes,
					status.CommittedRelativePath,
					status.LegacyStatus,
					status.Success,
					status.CancellationRequested,
					status.IsTerminal,
					status.CanRetry,
					status.Error,
					PerfMeterCaptureBundleCoordinator.CombineWarnings(status.Warning, warning),
					status.StartedUtc,
					status.CompletedUtc,
					status.ExternalArtifact);
			}
		}

		internal bool Cancel(string exportId)
		{
			lock (_sync)
			{
				if (_operation == null ||
					_operation.Status.IsTerminal ||
					!string.Equals(_operation.ExportId, exportId, StringComparison.Ordinal))
				{
					return false;
				}

				_operation.Cancellation.Cancel();
				_operation.Status = WithCancellationRequested(_operation.Status);
				return true;
			}
		}

		internal bool TryConsumeCompletion(out PerfMeterCaptureBundleExportCompletion completion)
		{
			lock (_sync)
			{
				completion = _pendingCompletion;
				if (completion == null)
				{
					return false;
				}

				_pendingCompletion = null;
				return true;
			}
		}

		internal bool IsBusy
		{
			get
			{
				lock (_sync)
				{
					return _blockingExportActive || _pendingCompletion != null || (_operation != null && !_operation.Status.IsTerminal);
				}
			}
		}

		internal PerfMeterCaptureBundleExportResult ExportBlocking(
			PerfMeterCaptureBundleExportData data,
			string path,
			string externalArtifactPath,
			bool requireAuthoritativeExternalArtifact,
			PerfMeterCaptureBundleExportEnvironment environment)
		{
			lock (_sync)
			{
				if (_blockingExportActive || _pendingCompletion != null || (_operation != null && !_operation.Status.IsTerminal))
				{
					return new PerfMeterCaptureBundleExportResult(
						false,
						PerfMeterCaptureBundleExportStatus.Conflict,
						string.Empty,
						"export_already_active",
						data == null ? PerfMeterCaptureBundleStatusSnapshot.None : data.Status);
				}

				_blockingExportActive = true;
			}

			try
			{
				PerfMeterCaptureBundleExportResult result = default;
				Exception failure = null;
				using (ManualResetEventSlim completed = new ManualResetEventSlim(false))
				{
					bool queued = _queueWorkItem(_ =>
					{
						try
						{
							result = PerfMeterCaptureBundleExporter.Export(
								data,
								path,
								externalArtifactPath,
								requireAuthoritativeExternalArtifact,
								environment,
								CancellationToken.None,
								null);
						}
						catch (Exception exception)
						{
							failure = exception;
						}
						finally
						{
							completed.Set();
						}
					}, null);
					if (!queued)
					{
						return new PerfMeterCaptureBundleExportResult(
							false,
							PerfMeterCaptureBundleExportStatus.IoError,
							string.Empty,
							"export_queue_rejected",
							data == null ? PerfMeterCaptureBundleStatusSnapshot.None : data.Status);
					}

					completed.Wait();
				}

				return failure == null
					? result
					: new PerfMeterCaptureBundleExportResult(
						false,
						PerfMeterCaptureBundleExportStatus.IoError,
						string.Empty,
						"bundle_io_error: " + PerfMeterCaptureBundleExporter.RedactSensitivePaths(failure.Message, environment),
						data == null ? PerfMeterCaptureBundleStatusSnapshot.None : data.Status);
			}
			catch (Exception exception)
			{
				return new PerfMeterCaptureBundleExportResult(
					false,
					PerfMeterCaptureBundleExportStatus.IoError,
					string.Empty,
					"bundle_io_error: " + PerfMeterCaptureBundleExporter.RedactSensitivePaths(exception.Message, environment),
					data == null ? PerfMeterCaptureBundleStatusSnapshot.None : data.Status);
			}
			finally
			{
				lock (_sync)
				{
					_blockingExportActive = false;
				}
			}
		}

		internal void ResetForTests()
		{
			lock (_sync)
			{
				if (_operation != null && !_operation.Status.IsTerminal)
				{
					_operation.Cancellation.Cancel();
				}

				_operation = null;
				_pendingCompletion = null;
				_blockingExportActive = false;
			}
		}

		private void WorkItem(object state)
		{
			ExportOperation operation = (ExportOperation)state;
			PerfMeterCaptureBundleExportResult result;
			try
			{
				result = PerfMeterCaptureBundleExporter.Export(
					operation.Data,
					operation.Path,
					operation.ExternalArtifactPath,
					operation.RequireAuthoritativeExternalArtifact,
					operation.Environment,
					operation.Cancellation.Token,
					(phase, progress, bytesProcessed, totalBytes) => UpdateProgress(operation, phase, progress, bytesProcessed, totalBytes));
			}
			catch (Exception exception)
			{
				result = new PerfMeterCaptureBundleExportResult(
					false,
					PerfMeterCaptureBundleExportStatus.IoError,
					string.Empty,
					"bundle_io_error: " + PerfMeterCaptureBundleExporter.RedactSensitivePaths(exception.Message, operation.Environment),
					operation.Data.Status);
			}

			bool canceled = !result.Success && (operation.Cancellation.IsCancellationRequested || result.Status == PerfMeterCaptureBundleExportStatus.Canceled);
			SetTerminal(operation, result, canceled ? PerfMeterCaptureBundleExportPhase.Canceled : result.Success ? PerfMeterCaptureBundleExportPhase.Completed : PerfMeterCaptureBundleExportPhase.Failed, canceled);
		}

		private void UpdateProgress(ExportOperation operation, PerfMeterCaptureBundleExportPhase phase, float progress, long bytesProcessed, long totalBytes)
		{
			lock (_sync)
			{
				if (!ReferenceEquals(_operation, operation) || operation.Status.IsTerminal)
				{
					return;
				}

				operation.Status = new PerfMeterCaptureBundleExportStatusSnapshot(
					operation.Status.ExportId,
					operation.Status.CaptureId,
					operation.Status.BundleId,
					phase,
					progress,
					bytesProcessed,
					totalBytes,
					operation.Status.CommittedRelativePath,
					operation.Status.LegacyStatus,
					false,
					operation.Cancellation.IsCancellationRequested,
					false,
					false,
					operation.Status.Error,
					operation.Status.Warning,
					operation.Status.StartedUtc,
					operation.Status.CompletedUtc,
					operation.Status.ExternalArtifact);
			}
		}

		private void SetTerminal(
			ExportOperation operation,
			PerfMeterCaptureBundleExportResult result,
			PerfMeterCaptureBundleExportPhase phase,
			bool canceled)
		{
			lock (_sync)
			{
				if (!ReferenceEquals(_operation, operation))
				{
					return;
				}

				bool success = result.Success && !canceled;
				PerfMeterCaptureBundleExportStatus legacyStatus = canceled
					? PerfMeterCaptureBundleExportStatus.Canceled
					: result.Status;
				string error = canceled
					? "export_canceled"
					: success ? string.Empty : result.Error;
				string warning = success ? result.Error : string.Empty;
				bool canRetry = !success && operation.Data.Status.IsExportReady;
				operation.Status = new PerfMeterCaptureBundleExportStatusSnapshot(
					operation.ExportId,
					operation.Data.Status.CaptureId,
					operation.Data.Status.BundleId,
					phase,
					success ? 1f : operation.Status.Progress,
					Math.Max(operation.Status.BytesProcessed, success ? operation.Status.TotalBytes : 0L),
					operation.Status.TotalBytes,
					success ? result.RelativePath : string.Empty,
					legacyStatus,
					success,
					operation.Cancellation.IsCancellationRequested || canceled,
					true,
					canRetry,
					error,
					warning,
					operation.Status.StartedUtc,
					DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
					result.ExternalArtifact);
				_pendingCompletion = new PerfMeterCaptureBundleExportCompletion(operation.ExportId, operation.Data, result, canceled);
			}
		}

		private static PerfMeterCaptureBundleExportStatusSnapshot CreateQueuedStatus(ExportOperation operation)
		{
			return new PerfMeterCaptureBundleExportStatusSnapshot(
				operation.ExportId,
				operation.Data.Status.CaptureId,
				operation.Data.Status.BundleId,
				PerfMeterCaptureBundleExportPhase.Queued,
				0f,
				0L,
				0L,
				string.Empty,
				PerfMeterCaptureBundleExportStatus.NotReady,
				false,
				false,
				false,
				false,
				string.Empty,
				string.Empty,
				DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
				string.Empty,
				operation.Data.Status.ExternalArtifact);
		}

		private static PerfMeterCaptureBundleExportStatusSnapshot WithCancellationRequested(PerfMeterCaptureBundleExportStatusSnapshot status)
		{
			return new PerfMeterCaptureBundleExportStatusSnapshot(
				status.ExportId,
				status.CaptureId,
				status.BundleId,
				status.Phase,
				status.Progress,
				status.BytesProcessed,
				status.TotalBytes,
				status.CommittedRelativePath,
				status.LegacyStatus,
				status.Success,
				true,
				status.IsTerminal,
				status.CanRetry,
				status.Error,
				status.Warning,
				status.StartedUtc,
				status.CompletedUtc,
				status.ExternalArtifact);
		}

		private sealed class ExportOperation
		{
			internal ExportOperation(
				string exportId,
				PerfMeterCaptureBundleExportData data,
				string path,
				string externalArtifactPath,
				bool requireAuthoritativeExternalArtifact,
				PerfMeterCaptureBundleExportEnvironment environment)
			{
				ExportId = exportId;
				Data = data;
				Path = path;
				ExternalArtifactPath = externalArtifactPath;
				RequireAuthoritativeExternalArtifact = requireAuthoritativeExternalArtifact;
				Environment = environment;
				Cancellation = new CancellationTokenSource();
			}

			internal string ExportId { get; }
			internal PerfMeterCaptureBundleExportData Data { get; }
			internal string Path { get; }
			internal string ExternalArtifactPath { get; }
			internal bool RequireAuthoritativeExternalArtifact { get; }
			internal PerfMeterCaptureBundleExportEnvironment Environment { get; }
			internal CancellationTokenSource Cancellation { get; }
			internal PerfMeterCaptureBundleExportStatusSnapshot Status { get; set; }
		}
	}

	internal sealed class PerfMeterCaptureBundleExportCompletion
	{
		internal PerfMeterCaptureBundleExportCompletion(
			string exportId,
			PerfMeterCaptureBundleExportData data,
			PerfMeterCaptureBundleExportResult result,
			bool canceled)
		{
			ExportId = exportId ?? string.Empty;
			Data = data;
			Result = result;
			Canceled = canceled;
		}

		internal string ExportId { get; }
		internal PerfMeterCaptureBundleExportData Data { get; }
		internal PerfMeterCaptureBundleExportResult Result { get; }
		internal bool Canceled { get; }
	}
}
