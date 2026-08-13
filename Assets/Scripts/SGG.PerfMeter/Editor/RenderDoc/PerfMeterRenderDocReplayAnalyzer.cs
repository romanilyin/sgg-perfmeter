using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace SGG.PerfMeter.Editor
{
	internal enum PerfMeterRenderDocAnalyzerExecutionStatus
	{
		Completed = 0,
		Failed = 1,
		TimedOut = 2,
		Canceled = 3
	}

	internal sealed class PerfMeterRenderDocAnalyzerExecution
	{
		internal PerfMeterRenderDocAnalyzerExecution(
			PerfMeterRenderDocAnalyzerExecutionStatus status,
			string errorCode,
			PerfMeterRenderDocAnalysisResult result,
			PerfMeterRenderDocAnalysisError failure,
			string standardOutput,
			string standardError,
			bool logsTruncated)
		{
			Status = status;
			ErrorCode = errorCode ?? string.Empty;
			Result = result;
			Failure = failure;
			StandardOutput = standardOutput ?? string.Empty;
			StandardError = standardError ?? string.Empty;
			LogsTruncated = logsTruncated;
		}

		internal PerfMeterRenderDocAnalyzerExecutionStatus Status { get; }
		internal string ErrorCode { get; }
		internal PerfMeterRenderDocAnalysisResult Result { get; }
		internal PerfMeterRenderDocAnalysisError Failure { get; }
		internal string StandardOutput { get; }
		internal string StandardError { get; }
		internal bool LogsTruncated { get; }
	}

	internal enum PerfMeterRenderDocAnalyzerProcessStatus
	{
		Completed = 0,
		StartFailed = 1,
		TimedOut = 2,
		Canceled = 3,
		TerminationFailed = 4
	}

	internal sealed class PerfMeterRenderDocAnalyzerProcessResult
	{
		internal PerfMeterRenderDocAnalyzerProcessResult(
			PerfMeterRenderDocAnalyzerProcessStatus status,
			int exitCode,
			string standardOutput,
			string standardError,
			bool logsTruncated)
		{
			Status = status;
			ExitCode = exitCode;
			StandardOutput = standardOutput ?? string.Empty;
			StandardError = standardError ?? string.Empty;
			LogsTruncated = logsTruncated;
		}

		internal PerfMeterRenderDocAnalyzerProcessStatus Status { get; }
		internal int ExitCode { get; }
		internal string StandardOutput { get; }
		internal string StandardError { get; }
		internal bool LogsTruncated { get; }
	}

	internal sealed class PerfMeterRenderDocAnalyzerProcessStart
	{
		internal string FileName;
		internal string Arguments;
		internal string WorkingDirectory;
		internal Dictionary<string, string> Environment;
	}

	internal interface IPerfMeterRenderDocAnalyzerProcessRunner
	{
		PerfMeterRenderDocAnalyzerProcessResult Run(
			PerfMeterRenderDocAnalyzerProcessStart start,
			int timeoutMilliseconds,
			Func<bool> shouldCancel);
	}

	internal sealed class PerfMeterRenderDocAnalyzerProcessRunner : IPerfMeterRenderDocAnalyzerProcessRunner
	{
		internal const int MaximumLogCharacters = 64 * 1024;
		private const int PollMilliseconds = 100;
		private const int TerminationWaitMilliseconds = 5000;

		public PerfMeterRenderDocAnalyzerProcessResult Run(
			PerfMeterRenderDocAnalyzerProcessStart start,
			int timeoutMilliseconds,
			Func<bool> shouldCancel)
		{
			if (IsCancellationRequested(shouldCancel))
			{
				return CreateResult(PerfMeterRenderDocAnalyzerProcessStatus.Canceled, -1, null, null);
			}

			BoundedLog output = new BoundedLog(MaximumLogCharacters);
			BoundedLog error = new BoundedLog(MaximumLogCharacters);
			using (Process process = new Process())
			{
				try
				{
					if (start == null ||
						string.IsNullOrEmpty(start.FileName) ||
						string.IsNullOrEmpty(start.WorkingDirectory) ||
						start.Environment == null ||
						timeoutMilliseconds < 1)
					{
						return CreateResult(PerfMeterRenderDocAnalyzerProcessStatus.StartFailed, -1, output, error);
					}

					ProcessStartInfo info = process.StartInfo;
					info.FileName = start.FileName;
					info.Arguments = start.Arguments;
					info.WorkingDirectory = start.WorkingDirectory;
					info.UseShellExecute = false;
					info.CreateNoWindow = true;
					info.RedirectStandardOutput = true;
					info.RedirectStandardError = true;
					info.EnvironmentVariables.Clear();
					foreach (KeyValuePair<string, string> entry in start.Environment)
					{
						info.EnvironmentVariables[entry.Key] = entry.Value;
					}

					if (!process.Start())
					{
						return CreateResult(PerfMeterRenderDocAnalyzerProcessStatus.StartFailed, -1, output, error);
					}
				}
				catch (Exception)
				{
					return CreateResult(PerfMeterRenderDocAnalyzerProcessStatus.StartFailed, -1, output, error);
				}

				Thread outputPump = StartPump(process.StandardOutput, output);
				Thread errorPump = StartPump(process.StandardError, error);
				Stopwatch stopwatch = Stopwatch.StartNew();
				PerfMeterRenderDocAnalyzerProcessStatus status = PerfMeterRenderDocAnalyzerProcessStatus.Completed;

				while (!process.WaitForExit(PollMilliseconds))
				{
					if (IsCancellationRequested(shouldCancel))
					{
						status = PerfMeterRenderDocAnalyzerProcessStatus.Canceled;
						break;
					}
					if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
					{
						status = PerfMeterRenderDocAnalyzerProcessStatus.TimedOut;
						break;
					}
				}

				if (status != PerfMeterRenderDocAnalyzerProcessStatus.Completed && !TryTerminate(process))
				{
					status = PerfMeterRenderDocAnalyzerProcessStatus.TerminationFailed;
				}

				if (status == PerfMeterRenderDocAnalyzerProcessStatus.Completed)
				{
					process.WaitForExit();
				}

				outputPump.Join(TerminationWaitMilliseconds);
				errorPump.Join(TerminationWaitMilliseconds);
				int exitCode = process.HasExited ? process.ExitCode : -1;
				return CreateResult(status, exitCode, output, error);
			}
		}

		private static Thread StartPump(StreamReader reader, BoundedLog destination)
		{
			Thread thread = new Thread(() =>
			{
				char[] buffer = new char[4096];
				try
				{
					while (true)
					{
						int read = reader.Read(buffer, 0, buffer.Length);
						if (read == 0)
						{
							break;
						}
						destination.Append(buffer, read);
					}
				}
				catch (Exception)
				{
				}
			});
			thread.IsBackground = true;
			thread.Start();
			return thread;
		}

		private static bool TryTerminate(Process process)
		{
			try
			{
				if (process.HasExited)
				{
					return true;
				}

				string taskKillPath = Path.Combine(Environment.SystemDirectory, "taskkill.exe");
				using (Process taskKill = Process.Start(new ProcessStartInfo
				{
					FileName = taskKillPath,
					Arguments = "/PID " + process.Id.ToString(CultureInfo.InvariantCulture) + " /T /F",
					UseShellExecute = false,
					CreateNoWindow = true
				}))
				{
					if (taskKill != null && taskKill.WaitForExit(TerminationWaitMilliseconds) && process.WaitForExit(TerminationWaitMilliseconds))
					{
						return true;
					}
				}

				if (!process.HasExited)
				{
					process.Kill();
				}
				return process.WaitForExit(TerminationWaitMilliseconds);
			}
			catch (InvalidOperationException)
			{
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool IsCancellationRequested(Func<bool> shouldCancel)
		{
			if (shouldCancel == null)
			{
				return false;
			}
			try
			{
				return shouldCancel();
			}
			catch (Exception)
			{
				return true;
			}
		}

		private static PerfMeterRenderDocAnalyzerProcessResult CreateResult(
			PerfMeterRenderDocAnalyzerProcessStatus status,
			int exitCode,
			BoundedLog output,
			BoundedLog error)
		{
			return new PerfMeterRenderDocAnalyzerProcessResult(
				status,
				exitCode,
				output != null ? output.Value : string.Empty,
				error != null ? error.Value : string.Empty,
				(output != null && output.Truncated) || (error != null && error.Truncated));
		}

		private sealed class BoundedLog
		{
			private readonly int _maximumCharacters;
			private readonly StringBuilder _builder;

			internal BoundedLog(int maximumCharacters)
			{
				_maximumCharacters = maximumCharacters;
				_builder = new StringBuilder(Math.Min(maximumCharacters, 4096));
			}

			internal bool Truncated { get; private set; }
			internal string Value => _builder.ToString();

			internal void Append(char[] value, int count)
			{
				int remaining = _maximumCharacters - _builder.Length;
				if (remaining > 0)
				{
					_builder.Append(value, 0, Math.Min(remaining, count));
				}
				if (count > remaining)
				{
					Truncated = true;
				}
			}
		}
	}

	internal sealed class PerfMeterRenderDocReplayAnalyzer
	{
		internal const string RelativeWorkspaceRoot = "Temp/PerfMeter/RenderDocAnalyzer";
		internal const string WorkspaceMarkerFileName = ".sgg-perfmeter-renderdoc-analyzer";
		internal const string RequestFileName = "request.json";
		internal const string ResponseFileName = "response.json";
		internal const string StageFileName = "stage.txt";
		internal const string AnalyzerScriptRelativePath = "Tools~/RenderDocAnalyzer/perfmeter_renderdoc_analyzer.py";
		internal const string RenderDocExecutableName = "qrenderdoc.exe";

		private const string WorkspaceMarkerSchema = "sgg.perfmeter.renderdoc-analyzer-workspace";
		private const int MaximumRequestBytes = 64 * 1024 * 1024;
		private const int CleanupAttempts = 10;
		private const int CleanupRetryMilliseconds = 100;
		private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
		private readonly string _projectRoot;
		private readonly string _packageRoot;
		private readonly IPerfMeterRenderDocAnalyzerProcessRunner _processRunner;

		internal PerfMeterRenderDocReplayAnalyzer(
			string projectRoot,
			string packageRoot,
			IPerfMeterRenderDocAnalyzerProcessRunner processRunner = null)
		{
			_projectRoot = NormalizeDirectory(projectRoot);
			_packageRoot = NormalizeDirectory(packageRoot);
			_processRunner = processRunner ?? new PerfMeterRenderDocAnalyzerProcessRunner();
		}

		internal static PerfMeterRenderDocReplayAnalyzer CreateDefault()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			PackageInfo package = PackageInfo.FindForAssembly(typeof(PerfMeterRenderDocReplayAnalyzer).Assembly);
			string packageRoot = package != null && !string.IsNullOrEmpty(package.resolvedPath)
				? package.resolvedPath
				: Path.Combine(Application.dataPath, "Scripts", "SGG.PerfMeter");
			return new PerfMeterRenderDocReplayAnalyzer(projectRoot, packageRoot);
		}

		internal PerfMeterRenderDocAnalyzerExecution Analyze(
			string approvedRenderDocRoot,
			string requestJson,
			Func<bool> shouldCancel = null)
		{
			if (IsCancellationRequested(shouldCancel))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Canceled, "analysis_canceled");
			}

			if (!PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(requestJson, out PerfMeterRenderDocAnalysisRequest request, out _))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "invalid_request");
			}

			if (Application.platform != RuntimePlatform.WindowsEditor || IntPtr.Size != 8)
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "unsupported_host");
			}

			if (!TryResolveInputs(approvedRenderDocRoot, request, out string executablePath, out string scriptPath, out _))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "invalid_analyzer_inputs");
			}

			if (!TryCreateWorkspace(requestJson, out string workspacePath, out string nonce))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "workspace_create_failed");
			}

			PerfMeterRenderDocAnalyzerExecution execution;
			bool processMayBeRunning = false;
			try
			{
				if (!TryBuildEnvironment(workspacePath, approvedRenderDocRoot, out Dictionary<string, string> environment))
				{
					execution = Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "environment_create_failed");
				}
				else
				{
					PerfMeterRenderDocAnalyzerProcessStart start = new PerfMeterRenderDocAnalyzerProcessStart
					{
						FileName = executablePath,
						Arguments = "--python " + QuoteWindowsArgument(scriptPath),
						WorkingDirectory = workspacePath,
						Environment = environment
					};
					int timeoutMilliseconds = checked(request.options.timeout_seconds * 1000);
					PerfMeterRenderDocAnalyzerProcessResult process = SanitizeProcessResult(
						_processRunner.Run(start, timeoutMilliseconds, shouldCancel),
						approvedRenderDocRoot,
						workspacePath);
					processMayBeRunning = process.Status == PerfMeterRenderDocAnalyzerProcessStatus.TerminationFailed;
					execution = ReadProcessResult(workspacePath, nonce, request, process);
				}
			}
			catch (Exception)
			{
				execution = Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "analyzer_execution_failed");
			}

			if (processMayBeRunning)
			{
				return execution;
			}

			if (!TryDeleteWorkspace(workspacePath, nonce) && execution.Status == PerfMeterRenderDocAnalyzerExecutionStatus.Completed)
			{
				return new PerfMeterRenderDocAnalyzerExecution(
					PerfMeterRenderDocAnalyzerExecutionStatus.Failed,
					"workspace_cleanup_failed",
					null,
					null,
					execution.StandardOutput,
					execution.StandardError,
					execution.LogsTruncated);
			}
			return execution;
		}

		private PerfMeterRenderDocAnalyzerExecution ReadProcessResult(
			string workspacePath,
			string nonce,
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalyzerProcessResult process)
		{
			string standardError = AppendStage(process.StandardError, workspacePath);
			process = new PerfMeterRenderDocAnalyzerProcessResult(
				process.Status,
				process.ExitCode,
				process.StandardOutput,
				standardError,
				process.LogsTruncated);
			switch (process.Status)
			{
				case PerfMeterRenderDocAnalyzerProcessStatus.StartFailed:
					return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "process_start_failed", process);
				case PerfMeterRenderDocAnalyzerProcessStatus.TimedOut:
					return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.TimedOut, "analysis_timed_out", process);
				case PerfMeterRenderDocAnalyzerProcessStatus.Canceled:
					return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Canceled, "analysis_canceled", process);
				case PerfMeterRenderDocAnalyzerProcessStatus.TerminationFailed:
					return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "process_termination_failed", process);
			}

			if (process.ExitCode != 0)
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "process_exit_nonzero", process);
			}
			if (!ValidateWorkspaceMarker(workspacePath, nonce))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "workspace_compromised", process);
			}

			string responsePath = Path.Combine(workspacePath, ResponseFileName);
			if (!TryReadBoundedRegularFile(responsePath, request.options.max_output_bytes, out string json))
			{
				return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "invalid_response_file", process);
			}

			if (PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, json, out PerfMeterRenderDocAnalysisResult result, out _))
			{
				return new PerfMeterRenderDocAnalyzerExecution(
					PerfMeterRenderDocAnalyzerExecutionStatus.Completed,
					string.Empty,
					result,
					null,
					process.StandardOutput,
					process.StandardError,
					process.LogsTruncated);
			}
			if (PerfMeterRenderDocAnalyzerProtocol.TryReadError(request, json, out PerfMeterRenderDocAnalysisError failure, out _))
			{
				return new PerfMeterRenderDocAnalyzerExecution(
					PerfMeterRenderDocAnalyzerExecutionStatus.Failed,
					failure.error.code,
					null,
					failure,
					process.StandardOutput,
					process.StandardError,
					process.LogsTruncated);
			}

			return Failure(PerfMeterRenderDocAnalyzerExecutionStatus.Failed, "invalid_response", process);
		}

		private static string AppendStage(string standardError, string workspacePath)
		{
			if (!TryReadBoundedRegularFile(Path.Combine(workspacePath, StageFileName), 128, out string stage))
			{
				return standardError;
			}
			stage = stage.Trim();
			if (!IsStageToken(stage))
			{
				return standardError;
			}
			return string.IsNullOrEmpty(standardError)
				? "stage:" + stage
				: standardError + "\nstage:" + stage;
		}

		private static bool IsStageToken(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length > 64)
			{
				return false;
			}
			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				if (!((character >= 'a' && character <= 'z') || character == '_'))
				{
					return false;
				}
			}
			return true;
		}

		private bool TryResolveInputs(
			string approvedRenderDocRoot,
			PerfMeterRenderDocAnalysisRequest request,
			out string executablePath,
			out string scriptPath,
			out string capturePath)
		{
			executablePath = string.Empty;
			scriptPath = string.Empty;
			capturePath = string.Empty;
			try
			{
				string renderDocRoot = NormalizeDirectory(approvedRenderDocRoot);
				if (!IsRegularDirectory(renderDocRoot))
				{
					return false;
				}

				executablePath = Path.GetFullPath(Path.Combine(renderDocRoot, RenderDocExecutableName));
				if (!string.Equals(Path.GetDirectoryName(executablePath), renderDocRoot, StringComparison.OrdinalIgnoreCase) ||
					!IsRegularFile(executablePath))
				{
					return false;
				}

				scriptPath = Path.GetFullPath(Path.Combine(_packageRoot, AnalyzerScriptRelativePath.Replace('/', Path.DirectorySeparatorChar)));
				if (!IsContainedPath(_packageRoot, scriptPath) ||
					!ValidatePathFromRoot(_packageRoot, scriptPath, false))
				{
					return false;
				}

				capturePath = Path.GetFullPath(Path.Combine(_projectRoot, request.capture.path.Replace('/', Path.DirectorySeparatorChar)));
				FileInfo captureInfo = new FileInfo(capturePath);
				if (!IsContainedPath(_projectRoot, capturePath) ||
					!ValidatePathFromRoot(_projectRoot, capturePath, false) ||
					!long.TryParse(request.capture.size_bytes, NumberStyles.None, CultureInfo.InvariantCulture, out long expectedBytes) ||
					(captureInfo.Attributes & FileAttributes.Offline) != 0 ||
					captureInfo.Length != expectedBytes)
				{
					return false;
				}

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private bool TryCreateWorkspace(string requestJson, out string workspacePath, out string nonce)
		{
			workspacePath = string.Empty;
			nonce = string.Empty;
			try
			{
				string workspaceRoot = Path.GetFullPath(Path.Combine(_projectRoot, RelativeWorkspaceRoot.Replace('/', Path.DirectorySeparatorChar)));
				if (!EnsureOwnedDirectoryPath(workspaceRoot))
				{
					return false;
				}

				for (int attempt = 0; attempt < 4; attempt++)
				{
					nonce = Guid.NewGuid().ToString("N");
					workspacePath = Path.Combine(workspaceRoot, nonce);
					if (Directory.Exists(workspacePath) || File.Exists(workspacePath))
					{
						continue;
					}

					Directory.CreateDirectory(workspacePath);
					string marker = "schema=" + WorkspaceMarkerSchema + "\nversion=1\nnonce=" + nonce + "\n";
					WriteNewFile(Path.Combine(workspacePath, WorkspaceMarkerFileName), StrictUtf8.GetBytes(marker));
					WriteNewFile(Path.Combine(workspacePath, RequestFileName), StrictUtf8.GetBytes(requestJson));

					string profileRoot = Path.Combine(workspacePath, "profile");
					string roamingRoot = Path.Combine(profileRoot, "AppData", "Roaming");
					string configRoot = Path.Combine(roamingRoot, "qrenderdoc");
					Directory.CreateDirectory(configRoot);
					string config = "{\"rdocConfigData\":1,\"Analytics_TotalOptOut\":true,\"Analytics_ManualCheck\":false,\"CheckUpdate_AllowChecks\":false,\"AlwaysLoad_Extensions\":[],\"ShaderProcessors\":[],\"UIStyle\":\"RDLight\"}";
					WriteNewFile(Path.Combine(configRoot, "UI.config"), StrictUtf8.GetBytes(config));
					Directory.CreateDirectory(Path.Combine(profileRoot, "AppData", "Local"));
					Directory.CreateDirectory(Path.Combine(roamingRoot, "renderdoc"));
					Directory.CreateDirectory(Path.Combine(workspacePath, "tmp"));
					return true;
				}
			}
			catch (Exception)
			{
			}

			if (!string.IsNullOrEmpty(workspacePath) && !string.IsNullOrEmpty(nonce))
			{
				TryDeleteWorkspace(workspacePath, nonce);
			}
			workspacePath = string.Empty;
			nonce = string.Empty;
			return false;
		}

		private static bool TryBuildEnvironment(
			string workspacePath,
			string approvedRenderDocRoot,
			out Dictionary<string, string> environment)
		{
			environment = null;
			try
			{
				string windowsRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string systemDirectory = Environment.SystemDirectory;
				if (string.IsNullOrEmpty(windowsRoot) || string.IsNullOrEmpty(systemDirectory))
				{
					return false;
				}

				string profileRoot = Path.Combine(workspacePath, "profile");
				environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ "APPDATA", Path.Combine(profileRoot, "AppData", "Roaming") },
					{ "LOCALAPPDATA", Path.Combine(profileRoot, "AppData", "Local") },
					{ "USERPROFILE", profileRoot },
					{ "HOME", profileRoot },
					{ "TEMP", Path.Combine(workspacePath, "tmp") },
					{ "TMP", Path.Combine(workspacePath, "tmp") },
					{ "PATH", NormalizeDirectory(approvedRenderDocRoot) + Path.PathSeparator + systemDirectory },
					{ "SYSTEMROOT", windowsRoot },
					{ "WINDIR", windowsRoot },
					{ "COMSPEC", Path.Combine(systemDirectory, "cmd.exe") },
					{ "PYTHONNOUSERSITE", "1" },
					{ "PYTHONDONTWRITEBYTECODE", "1" },
					{ "QT_LOGGING_TO_CONSOLE", "1" },
					{ "SGG_PERFMETER_RENDERDOC_ANALYZER", "1" }
				};
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private bool EnsureOwnedDirectoryPath(string path)
		{
			try
			{
				if (!IsRegularDirectory(_projectRoot) || !IsContainedPath(_projectRoot, path))
				{
					return false;
				}

				string relative = path.Substring(_projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string current = _projectRoot;
				string[] segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < segments.Length; i++)
				{
					current = Path.Combine(current, segments[i]);
					Directory.CreateDirectory(current);
					if (!IsRegularDirectory(current))
					{
						return false;
					}
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool ValidatePathFromRoot(string root, string path, bool directory)
		{
			try
			{
				if (!IsRegularDirectory(root) || !IsContainedPath(root, path))
				{
					return false;
				}

				string current = Path.GetDirectoryName(path);
				while (!string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
				{
					if (string.IsNullOrEmpty(current) || !IsRegularDirectory(current))
					{
						return false;
					}
					current = Path.GetDirectoryName(current);
				}
				return directory ? IsRegularDirectory(path) : IsRegularFile(path);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool TryReadBoundedRegularFile(string path, int maximumBytes, out string value)
		{
			value = string.Empty;
			try
			{
				if (!IsRegularFile(path))
				{
					return false;
				}
				FileInfo info = new FileInfo(path);
				if (info.Length <= 0L || info.Length > maximumBytes || info.Length > MaximumRequestBytes)
				{
					return false;
				}

				byte[] bytes = new byte[(int)info.Length];
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan))
				{
					int offset = 0;
					while (offset < bytes.Length)
					{
						int read = stream.Read(bytes, offset, bytes.Length - offset);
						if (read == 0)
						{
							return false;
						}
						offset += read;
					}
					if (stream.ReadByte() != -1)
					{
						return false;
					}
				}
				value = StrictUtf8.GetString(bytes);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static void WriteNewFile(string path, byte[] contents)
		{
			using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				stream.Write(contents, 0, contents.Length);
				stream.Flush(true);
			}
		}

		private static bool ValidateWorkspaceMarker(string workspacePath, string nonce)
		{
			string expected = "schema=" + WorkspaceMarkerSchema + "\nversion=1\nnonce=" + nonce + "\n";
			return TryReadBoundedRegularFile(Path.Combine(workspacePath, WorkspaceMarkerFileName), 512, out string marker) &&
				string.Equals(marker, expected, StringComparison.Ordinal);
		}

		private static bool TryDeleteWorkspace(string workspacePath, string nonce)
		{
			for (int attempt = 0; attempt < CleanupAttempts; attempt++)
			{
				try
				{
					if (!string.Equals(Path.GetFileName(workspacePath), nonce, StringComparison.Ordinal) ||
						!ValidateWorkspaceMarker(workspacePath, nonce) ||
						ContainsReparsePoint(workspacePath))
					{
						return false;
					}
					DeleteWorkspaceContents(workspacePath);
					File.Delete(Path.Combine(workspacePath, WorkspaceMarkerFileName));
					Directory.Delete(workspacePath, false);
					return !Directory.Exists(workspacePath);
				}
				catch (Exception)
				{
					if (attempt + 1 < CleanupAttempts)
					{
						Thread.Sleep(CleanupRetryMilliseconds);
					}
				}
			}
			return false;
		}

		private static void DeleteWorkspaceContents(string directory)
		{
			if (!IsRegularDirectory(directory))
			{
				throw new IOException();
			}
			foreach (string file in Directory.GetFiles(directory))
			{
				if (!IsRegularFile(file))
				{
					throw new IOException();
				}
				if (!string.Equals(Path.GetFileName(file), WorkspaceMarkerFileName, StringComparison.Ordinal))
				{
					File.Delete(file);
				}
			}
			foreach (string child in Directory.GetDirectories(directory))
			{
				if (!IsRegularDirectory(child))
				{
					throw new IOException();
				}
				DeleteWorkspaceContents(child);
				Directory.Delete(child, false);
			}
		}

		private static bool ContainsReparsePoint(string directory)
		{
			if (!IsRegularDirectory(directory))
			{
				return true;
			}
			foreach (string child in Directory.GetFileSystemEntries(directory))
			{
				FileAttributes attributes = File.GetAttributes(child);
				if ((attributes & FileAttributes.ReparsePoint) != 0)
				{
					return true;
				}
				if ((attributes & FileAttributes.Directory) != 0 && ContainsReparsePoint(child))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsRegularDirectory(string path)
		{
			FileAttributes attributes = File.GetAttributes(path);
			return (attributes & FileAttributes.Directory) != 0 && (attributes & FileAttributes.ReparsePoint) == 0;
		}

		private static bool IsRegularFile(string path)
		{
			FileAttributes attributes = File.GetAttributes(path);
			return (attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
		}

		private static bool IsContainedPath(string root, string path)
		{
			string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizeDirectory(string path)
		{
			return Path.GetFullPath(path ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		private PerfMeterRenderDocAnalyzerProcessResult SanitizeProcessResult(
			PerfMeterRenderDocAnalyzerProcessResult process,
			string renderDocRoot,
			string workspacePath)
		{
			return new PerfMeterRenderDocAnalyzerProcessResult(
				process.Status,
				process.ExitCode,
				SanitizeLog(process.StandardOutput, renderDocRoot, workspacePath),
				SanitizeLog(process.StandardError, renderDocRoot, workspacePath),
				process.LogsTruncated);
		}

		private string SanitizeLog(string value, string renderDocRoot, string workspacePath)
		{
			string result = value ?? string.Empty;
			result = ReplacePath(result, workspacePath, "<workspace>");
			result = ReplacePath(result, _projectRoot, "<project>");
			result = ReplacePath(result, _packageRoot, "<package>");
			return ReplacePath(result, NormalizeDirectory(renderDocRoot), "<renderdoc>");
		}

		private static string ReplacePath(string value, string path, string replacement)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(path))
			{
				return value;
			}
			string result = ReplaceOrdinalIgnoreCase(value, path, replacement);
			return ReplaceOrdinalIgnoreCase(result, path.Replace('\\', '/'), replacement);
		}

		private static string ReplaceOrdinalIgnoreCase(string value, string search, string replacement)
		{
			int index = value.IndexOf(search, StringComparison.OrdinalIgnoreCase);
			if (index < 0)
			{
				return value;
			}
			StringBuilder builder = new StringBuilder(value.Length);
			int offset = 0;
			while (index >= 0)
			{
				builder.Append(value, offset, index - offset);
				builder.Append(replacement);
				offset = index + search.Length;
				index = value.IndexOf(search, offset, StringComparison.OrdinalIgnoreCase);
			}
			builder.Append(value, offset, value.Length - offset);
			return builder.ToString();
		}

		internal static string QuoteWindowsArgument(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "\"\"";
			}
			if (value.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '\"' }) < 0)
			{
				return value;
			}

			StringBuilder builder = new StringBuilder(value.Length + 2);
			builder.Append('\"');
			int backslashes = 0;
			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				if (character == '\\')
				{
					backslashes++;
					continue;
				}
				if (character == '\"')
				{
					builder.Append('\\', backslashes * 2 + 1);
					builder.Append('\"');
					backslashes = 0;
					continue;
				}
				builder.Append('\\', backslashes);
				backslashes = 0;
				builder.Append(character);
			}
			builder.Append('\\', backslashes * 2);
			builder.Append('\"');
			return builder.ToString();
		}

		private static bool IsCancellationRequested(Func<bool> shouldCancel)
		{
			if (shouldCancel == null)
			{
				return false;
			}
			try
			{
				return shouldCancel();
			}
			catch (Exception)
			{
				return true;
			}
		}

		private static PerfMeterRenderDocAnalyzerExecution Failure(
			PerfMeterRenderDocAnalyzerExecutionStatus status,
			string errorCode,
			PerfMeterRenderDocAnalyzerProcessResult process = null)
		{
			return new PerfMeterRenderDocAnalyzerExecution(
				status,
				errorCode,
				null,
				null,
				process != null ? process.StandardOutput : string.Empty,
				process != null ? process.StandardError : string.Empty,
				process != null && process.LogsTruncated);
		}
	}
}
