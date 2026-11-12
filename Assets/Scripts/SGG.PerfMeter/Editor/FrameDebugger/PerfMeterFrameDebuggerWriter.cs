using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SGG.PerfMeter.Editor.FrameDebugger
{
	// Consumes detached CLR data only; safe to call from the export worker.
	internal static class PerfMeterFrameDebuggerWriter
	{
		internal const string Schema = "sgg.perfmeter.frame-debugger";
		internal const string MarkerFileName = ".sgg-perfmeter-frame-debugger";
		private const string Provenance = "observed_not_exact_frame";
		private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
		private static readonly string[] Columns = { "shader", "shader_pass", "draw_calls", "vertices", "indices", "instances", "render_target_name", "width", "height" };
		private static readonly string[] AllowedFiles = { "events.json", "events.csv", "properties.csv", "manifest.json", MarkerFileName };

		internal static PerfMeterFrameDebuggerWriteResult Write(string projectRoot, PerfMeterFrameDebuggerSnapshot snapshot, CancellationToken token)
		{
			if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
			ValidateId(snapshot.ExportId);
			string project = CanonicalProject(projectRoot);
			string root = Path.Combine(project, "Temp", "PerfMeter", "FrameDebuggerExports");
			string final = Path.Combine(root, snapshot.ExportId);
			string nonce = Guid.NewGuid().ToString("N");
			string stage = Path.Combine(root, ".staging-" + snapshot.ExportId + "-" + nonce);
			string stageMarker = Marker("staging", snapshot.ExportId, nonce);
			string committedMarker = Marker("committed", snapshot.ExportId, nonce);
			bool ownsStage = false;
			bool committed = false;
			using (Mutex mutex = new Mutex(false, "SGG.PerfMeter.FrameDebugger." + Hash(Utf8.GetBytes(NormalizeLockPath(project)))))
			{
				bool locked = false;
				try
				{
					while (!locked)
					{
						token.ThrowIfCancellationRequested();
						try { locked = mutex.WaitOne(50); }
						catch (AbandonedMutexException) { locked = true; }
					}
					ValidateSnapshot(snapshot, token);
					CheckAncestors(project);
					EnsureDirectory(root);
					CheckAncestors(final);
					if (Directory.Exists(final) || File.Exists(final)) throw new IOException("The export already exists; overwriting is refused.");
					token.ThrowIfCancellationRequested();
					if (Directory.Exists(stage) || File.Exists(stage)) throw new IOException("The export staging path already exists.");
					Directory.CreateDirectory(stage);
					CheckAncestors(stage);
					// Finish the tiny ownership marker before observing cancellation, so cancellation
					// always has an authenticated staging directory to clean up.
					WriteNew(stage, MarkerFileName, stageMarker, CancellationToken.None);
					ownsStage = true;
					List<OutputFile> files = new List<OutputFile>();
					long bytes = Utf8.GetByteCount(stageMarker);
					AddOutput(stage, "events.json", BuildJson(snapshot, token), files, ref bytes, token);
					AddOutput(stage, "events.csv", BuildCsv(snapshot, token), files, ref bytes, token);
					if (snapshot.IncludeProperties) AddOutput(stage, "properties.csv", BuildPropertiesCsv(snapshot, token), files, ref bytes, token);
					bytes += Utf8.GetByteCount(committedMarker) - Utf8.GetByteCount(stageMarker);
					files.Add(new OutputFile { Name = MarkerFileName, Bytes = Utf8.GetByteCount(committedMarker), Sha256 = Hash(Utf8.GetBytes(committedMarker)) });
					string manifest = BuildManifest(snapshot, files, bytes);
					long totalBytes = checked(bytes + Utf8.GetByteCount(manifest));
					if (totalBytes > PerfMeterFrameDebuggerExportOptions.MaxOutputBytes) throw new IOException("The export exceeds the 64 MiB output limit.");
					WriteNew(stage, "manifest.json", manifest, token);
					CheckAncestors(root);
					CheckAncestors(stage);
					EnforceRetention(root, totalBytes, snapshot.ExportId, token);
					token.ThrowIfCancellationRequested();
					ReplaceOwnMarker(stage, stageMarker, committedMarker);
					CheckAncestors(root);
					CheckAncestors(stage);
					CheckAncestors(final);
					token.ThrowIfCancellationRequested();
					Directory.Move(stage, final);
					committed = true; // A late cancellation cannot revoke a published, durable result.
					CheckAncestors(final);
					return new PerfMeterFrameDebuggerWriteResult
					{
						OutputDirectory = final,
						JsonSha256 = files[0].Sha256,
						CsvSha256 = files[1].Sha256,
						TotalBytes = totalBytes
					};
				}
				finally
				{
					if (ownsStage && !committed) CleanupOwnStage(stage, stageMarker, committedMarker);
					if (locked) mutex.ReleaseMutex();
				}
			}
		}

		internal static string BuildJson(PerfMeterFrameDebuggerSnapshot snapshot) => BuildJson(snapshot, CancellationToken.None);
		internal static string BuildCsv(PerfMeterFrameDebuggerSnapshot snapshot) => BuildCsv(snapshot, CancellationToken.None);

		private static string BuildJson(PerfMeterFrameDebuggerSnapshot snapshot, CancellationToken token)
		{
			ValidateSnapshot(snapshot, token);
			StringBuilder b = new StringBuilder(4096);
			b.Append("{\"schema\":"); JsonString(b, Schema);
			b.Append(",\"schema_version\":1,\"provenance\":"); JsonString(b, Provenance);
			b.Append(",\"metadata\":{");
			StringMember(b, "export_id", snapshot.ExportId, false);
			StringMember(b, "started_utc", snapshot.StartedUtc);
			StringMember(b, "completed_utc", snapshot.CompletedUtc);
			StringMember(b, "unity_version", snapshot.UnityVersion);
			StringMember(b, "render_pipeline", snapshot.RenderPipeline);
			StringMember(b, "graphics_api", snapshot.GraphicsApi);
			StringMember(b, "scene_name", snapshot.SceneName);
			StringMember(b, "session_id", snapshot.SessionId);
			StringMember(b, "diagnostic_interval_ended_utc", snapshot.DiagnosticIntervalEndedUtc);
			b.Append(",\"original_limit\":").Append(snapshot.OriginalLimit.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"include_properties\":").Append(snapshot.IncludeProperties ? "true" : "false");
			b.Append("},\"completeness\":{");
			StringMember(b, "state", snapshot.Completeness, false);
			StringMember(b, "warning", snapshot.Warning);
			b.Append(",\"total_event_count\":").Append(snapshot.TotalEventCount.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"requested_event_count\":").Append(snapshot.RequestedEventCount.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"exported_event_count\":").Append(snapshot.ExportedEventCount.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"missing_event_count\":").Append(snapshot.MissingEventCount.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"truncated_event_count\":").Append(snapshot.TruncatedEventCount.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"selection_restored\":").Append(snapshot.SelectionRestored ? "true" : "false");
			StringMember(b, "restoration_reason", snapshot.RestorationReason);
			b.Append("},\"events\":[");
			for (int i = 0; i < snapshot.Events.Length; i++)
			{
				token.ThrowIfCancellationRequested();
				PerfMeterFrameDebuggerEvent row = snapshot.Events[i];
				if (i != 0) b.Append(',');
				b.Append("{\"index\":").Append(row.Index.ToString(CultureInfo.InvariantCulture));
				b.Append(",\"raw_event_type\":").Append(row.RawEventType.ToString(CultureInfo.InvariantCulture));
				StringMember(b, "type", row.EventType);
				StringMember(b, "name", row.Name);
				StringMember(b, "status", row.Status);
				StringMember(b, "reason", row.Reason);
				b.Append(",\"truncated\":").Append(row.Truncated ? "true" : "false");
				b.Append(",\"fields\":[");
				bool first = true;
				foreach (PerfMeterFrameDebuggerField field in row.Fields)
				{
					if (!snapshot.IncludeProperties && IsProperty(field)) continue;
					if (!first) b.Append(',');
					first = false;
					bool available = IsAvailable(field, out string reason);
					b.Append('{'); StringMember(b, "name", field.Name, false);
					StringMember(b, "kind", field.Kind);
					b.Append(",\"available\":").Append(available ? "true" : "false").Append(",\"value\":");
					if (available) JsonValue(b, field.Value); else b.Append("null");
					StringMember(b, "reason", reason);
					b.Append(",\"truncated\":").Append(field.Truncated ? "true" : "false").Append('}');
					CheckBuilder(b);
				}
				b.Append("]}");
			}
			b.Append("]}\n");
			return Finish(b);
		}

		private static string BuildCsv(PerfMeterFrameDebuggerSnapshot snapshot, CancellationToken token)
		{
			ValidateSnapshot(snapshot, token);
			StringBuilder b = new StringBuilder("index,type,name,status,reason,shader,shader_pass,draw_calls,vertices,indices,instances,render_target_name,width,height\r\n");
			foreach (PerfMeterFrameDebuggerEvent row in snapshot.Events)
			{
				token.ThrowIfCancellationRequested();
				b.Append(row.Index.ToString(CultureInfo.InvariantCulture));
				CsvText(b, row.EventType); CsvText(b, row.Name); CsvText(b, row.Status); CsvText(b, row.Reason);
				foreach (string name in Columns)
				{
					PerfMeterFrameDebuggerField field = FindField(row, name);
					b.Append(',');
					if (field != null && IsAvailable(field, out _)) CsvValue(b, field.Value);
				}
				b.Append("\r\n");
				CheckBuilder(b);
			}
			return Finish(b);
		}

		private static string BuildPropertiesCsv(PerfMeterFrameDebuggerSnapshot snapshot, CancellationToken token)
		{
			StringBuilder b = new StringBuilder("event_index,name,kind,value,available,reason\r\n");
			foreach (PerfMeterFrameDebuggerEvent row in snapshot.Events)
			{
				token.ThrowIfCancellationRequested();
				foreach (PerfMeterFrameDebuggerField field in row.Fields)
				{
					if (!IsProperty(field)) continue;
					bool available = IsAvailable(field, out string reason);
					b.Append(row.Index.ToString(CultureInfo.InvariantCulture));
					CsvText(b, field.Name); CsvText(b, field.Kind); b.Append(',');
					if (available) CsvValue(b, field.Value);
					b.Append(',').Append(available ? "true" : "false"); CsvText(b, reason); b.Append("\r\n");
					CheckBuilder(b);
				}
			}
			return Finish(b);
		}

		private static bool IsAvailable(PerfMeterFrameDebuggerField field, out string reason)
		{
			reason = field.Reason ?? string.Empty;
			if (!field.Available) { if (reason.Length == 0) reason = "unavailable"; return false; }
			object value = field.Value;
			if (value == null) { reason = "value_missing"; return false; }
			if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)) || value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
			{ reason = "non_finite_value"; return false; }
			if (value is double[] numbers)
			{
				foreach (double number in numbers) if (double.IsNaN(number) || double.IsInfinity(number)) { reason = "non_finite_value"; return false; }
				return true;
			}
			if (value is string || value is bool || value is string[] || IsNumber(value)) return true;
			reason = "unsupported_value_type";
			return false;
		}

		private static bool IsNumber(object value) => value is byte || value is sbyte || value is short || value is ushort || value is int ||
			value is uint || value is long || value is ulong || value is float || value is double || value is decimal;

		private static void JsonValue(StringBuilder b, object value)
		{
			if (value is string text) { JsonString(b, text); return; }
			if (value is bool boolean) { b.Append(boolean ? "true" : "false"); return; }
			if (value is string[] strings)
			{
				b.Append('['); for (int i = 0; i < strings.Length; i++) { if (i > 0) b.Append(','); if (strings[i] == null) b.Append("null"); else JsonString(b, strings[i]); } b.Append(']'); return;
			}
			if (value is double[] numbers)
			{
				b.Append('['); for (int i = 0; i < numbers.Length; i++) { if (i > 0) b.Append(','); b.Append(numbers[i].ToString("R", CultureInfo.InvariantCulture)); } b.Append(']'); return;
			}
			b.Append(Number(value));
		}

		private static string Number(object value) => value is double d ? d.ToString("R", CultureInfo.InvariantCulture) :
			value is float f ? f.ToString("R", CultureInfo.InvariantCulture) : ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

		private static void JsonString(StringBuilder b, string value)
		{
			b.Append('"');
			if (value != null) foreach (char c in value)
			{
				switch (c)
				{
					case '"': b.Append("\\\""); break;
					case '\\': b.Append("\\\\"); break;
					case '\b': b.Append("\\b"); break;
					case '\f': b.Append("\\f"); break;
					case '\n': b.Append("\\n"); break;
					case '\r': b.Append("\\r"); break;
					case '\t': b.Append("\\t"); break;
					default:
						// Escaping UTF-16 surrogates also preserves unpaired inputs as valid JSON.
						if (c < 32 || char.IsSurrogate(c) || c == '\u2028' || c == '\u2029') b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else b.Append(c);
						break;
				}
			}
			b.Append('"');
		}

		private static void StringMember(StringBuilder b, string name, string value, bool comma = true)
		{
			if (comma) b.Append(','); JsonString(b, name); b.Append(':'); JsonString(b, value);
		}

		private static void CsvText(StringBuilder b, string value) { b.Append(','); CsvQuoted(b, value); }
		private static void CsvQuoted(StringBuilder b, string value)
		{
			value = value ?? string.Empty;
			b.Append('"');
			if (value.Length > 0 && (value[0] == '=' || value[0] == '+' || value[0] == '-' || value[0] == '@' || char.IsWhiteSpace(value[0]) || char.IsControl(value[0]))) b.Append('\'');
			foreach (char c in value) { if (c == '"') b.Append('"'); b.Append(c); }
			b.Append('"');
		}

		private static void CsvValue(StringBuilder b, object value)
		{
			if (value is string text) CsvQuoted(b, text);
			else if (value is bool boolean) b.Append(boolean ? "true" : "false");
			else if (value is Array) { StringBuilder json = new StringBuilder(); JsonValue(json, value); CsvQuoted(b, json.ToString()); }
			else b.Append(Number(value));
		}

		private static bool IsProperty(PerfMeterFrameDebuggerField field) => (field.Name ?? string.Empty).StartsWith("property.", StringComparison.Ordinal);
		private static PerfMeterFrameDebuggerField FindField(PerfMeterFrameDebuggerEvent row, string name)
		{
			foreach (PerfMeterFrameDebuggerField field in row.Fields) if (string.Equals(field.Name, name, StringComparison.Ordinal)) return field;
			return null;
		}

		private static void ValidateSnapshot(PerfMeterFrameDebuggerSnapshot snapshot, CancellationToken token)
		{
			if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
			if (snapshot.Events == null || snapshot.Events.Length > 8192) throw new InvalidDataException("Invalid frame event count.");
			long chars = 0;
			foreach (string text in new[] { snapshot.ExportId, snapshot.StartedUtc, snapshot.CompletedUtc, snapshot.UnityVersion, snapshot.RenderPipeline,
				snapshot.GraphicsApi, snapshot.SceneName, snapshot.SessionId, snapshot.DiagnosticIntervalEndedUtc, snapshot.RestorationReason, snapshot.Completeness, snapshot.Warning }) CountString(text, ref chars);
			foreach (PerfMeterFrameDebuggerEvent row in snapshot.Events)
			{
				token.ThrowIfCancellationRequested();
				if (row == null || row.Fields == null || row.Fields.Length > 384) throw new InvalidDataException("Invalid frame event fields.");
				CountString(row.EventType, ref chars); CountString(row.Name, ref chars); CountString(row.Status, ref chars); CountString(row.Reason, ref chars);
				int properties = 0;
				foreach (PerfMeterFrameDebuggerField field in row.Fields)
				{
					if (field == null) throw new InvalidDataException("Null frame field.");
					// Up to 128 native entries each carry a detached value and a flags field.
					if (IsProperty(field) && ++properties > 256) throw new InvalidDataException("Too many frame property fields.");
					CountString(field.Name, ref chars); CountString(field.Kind, ref chars); CountString(field.Reason, ref chars);
					if (field.Value is string text) CountString(text, ref chars);
					if (field.Value is Array array && array.Length > 4096) throw new InvalidDataException("Frame field array exceeds its limit.");
					if (field.Value is string[] strings) foreach (string item in strings) CountString(item, ref chars);
				}
				if (row.Fields.Length - properties > 256) throw new InvalidDataException("Too many frame fields.");
			}
		}

		private static void CountString(string text, ref long chars)
		{
			if (text == null) return;
			if (text.Length > 4096) throw new InvalidDataException("A captured string exceeds its limit.");
			chars += text.Length;
			if (chars > PerfMeterFrameDebuggerExportOptions.MaxCapturedCharacters) throw new InvalidDataException("The captured string budget was exceeded.");
		}

		private static string Finish(StringBuilder builder)
		{
			CheckBuilder(builder);
			string text = builder.ToString();
			if (Utf8.GetByteCount(text) > PerfMeterFrameDebuggerExportOptions.MaxOutputBytes) throw new IOException("The output limit was exceeded.");
			return text;
		}

		private static void CheckBuilder(StringBuilder builder)
		{
			if (builder.Length > PerfMeterFrameDebuggerExportOptions.MaxOutputBytes) throw new IOException("The output limit was exceeded.");
		}

		private sealed class OutputFile
		{
			internal string Name;
			internal long Bytes;
			internal string Sha256;
		}

		private static void AddOutput(string stage, string name, string text, List<OutputFile> files, ref long total, CancellationToken token)
		{
			long size = Utf8.GetByteCount(text);
			if (checked(total + size) > PerfMeterFrameDebuggerExportOptions.MaxOutputBytes) throw new IOException("The export exceeds the 64 MiB output limit.");
			WriteNew(stage, name, text, token);
			total += size;
			files.Add(new OutputFile { Name = name, Bytes = size, Sha256 = Hash(Utf8.GetBytes(text)) });
		}

		private static string BuildManifest(PerfMeterFrameDebuggerSnapshot snapshot, List<OutputFile> files, long payloadBytes)
		{
			StringBuilder b = new StringBuilder("{\"schema\":"); JsonString(b, Schema);
			b.Append(",\"schema_version\":1,\"provenance\":"); JsonString(b, Provenance);
			StringMember(b, "export_id", snapshot.ExportId); StringMember(b, "completeness", snapshot.Completeness);
			StringMember(b, "completed_utc", snapshot.CompletedUtc);
			b.Append(",\"files\":[");
			for (int i = 0; i < files.Count; i++)
			{
				if (i != 0) b.Append(','); b.Append('{'); StringMember(b, "name", files[i].Name, false);
				b.Append(",\"size_bytes\":").Append(files[i].Bytes.ToString(CultureInfo.InvariantCulture)); StringMember(b, "sha256", files[i].Sha256); b.Append('}');
			}
			// The manifest cannot hash itself; every other published file is listed above.
			b.Append("],\"payload_and_marker_bytes\":").Append(payloadBytes.ToString(CultureInfo.InvariantCulture)).Append("}\n");
			return b.ToString();
		}

		private static string Hash(byte[] bytes)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(bytes); StringBuilder b = new StringBuilder(64);
				foreach (byte item in hash) b.Append(item.ToString("x2", CultureInfo.InvariantCulture)); return b.ToString();
			}
		}

		private static void WriteNew(string directory, string name, string text, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			CheckAncestors(directory);
			string path = Path.Combine(directory, name);
			CheckAncestors(path);
			using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (StreamWriter writer = new StreamWriter(stream, Utf8, 4096, true))
			{
				for (int offset = 0; offset < text.Length; offset += 4096) { token.ThrowIfCancellationRequested(); writer.Write(text.ToCharArray(offset, Math.Min(4096, text.Length - offset))); }
				writer.Flush(); stream.Flush(true);
			}
			CheckAncestors(directory);
			CheckAncestors(path);
		}

		private static string CanonicalProject(string projectRoot)
		{
			if (string.IsNullOrWhiteSpace(projectRoot) || !Path.IsPathRooted(projectRoot)) throw new ArgumentException("An absolute project directory is required.", nameof(projectRoot));
			foreach (string part in projectRoot.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)) if (part == "..") throw new ArgumentException("Project traversal is refused.", nameof(projectRoot));
			string project = Path.GetFullPath(projectRoot);
			if (project.Length > Path.GetPathRoot(project).Length) project = project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!Directory.Exists(project)) throw new DirectoryNotFoundException("The project directory does not exist.");
			CheckAncestors(project); return project;
		}

		private static void ValidateId(string id)
		{
			if (string.IsNullOrEmpty(id) || id.Length > 128) throw new ArgumentException("Invalid export identifier.");
			foreach (char c in id) if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-' || c == '_')) throw new ArgumentException("Invalid export identifier.");
		}

		private static string NormalizeLockPath(string path) => Path.DirectorySeparatorChar == '\\' ? path.ToUpperInvariant() : path;
		private static void CheckAncestors(string path)
		{
			for (string cursor = Path.GetFullPath(path); !string.IsNullOrEmpty(cursor); cursor = Path.GetDirectoryName(cursor))
			{
				try { if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse points are refused in export paths."); }
				catch (FileNotFoundException) { }
				catch (DirectoryNotFoundException) { }
			}
		}

		private static void EnsureDirectory(string directory)
		{
			CheckAncestors(directory);
			Directory.CreateDirectory(directory);
			CheckAncestors(directory);
		}

		private static string Marker(string state, string id, string nonce) => Schema + "\n1\n" + state + "\n" + id + "\n" + nonce + "\n";
		private static bool ReadMarker(string directory, out string marker)
		{
			marker = null;
			string path = Path.Combine(directory, MarkerFileName);
			if (!File.Exists(path)) return false;
			CheckAncestors(path);
			if (new FileInfo(path).Length > 1024) return false;
			marker = File.ReadAllText(path, Utf8);
			return true;
		}

		private static void ReplaceOwnMarker(string stage, string expected, string replacement)
		{
			CheckAncestors(stage);
			if (!ReadMarker(stage, out string actual) || actual != expected) throw new IOException("The staging ownership marker changed.");
			string path = Path.Combine(stage, MarkerFileName);
			CheckAncestors(path);
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
			{
				byte[] bytes = Utf8.GetBytes(replacement); stream.SetLength(0); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
			}
			CheckAncestors(stage);
			CheckAncestors(path);
		}

		private static void CleanupOwnStage(string stage, string stagingMarker, string committedMarker)
		{
			try
			{
				CheckAncestors(stage);
				if (!Directory.Exists(stage) || !ReadMarker(stage, out string marker) || marker != stagingMarker && marker != committedMarker) return;
				DeleteKnownDirectory(stage, marker);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		private sealed class RetainedExport
		{
			internal string Directory;
			internal string Marker;
			internal long Bytes;
			internal DateTime Created;
			internal bool CanDelete;
		}

		private static void EnforceRetention(string root, long incomingBytes, string currentId, CancellationToken token)
		{
			CheckAncestors(root);
			List<RetainedExport> exports = new List<RetainedExport>();
			long retainedBytes = 0;
			foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
			{
				token.ThrowIfCancellationRequested();
				if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 || Path.GetFileName(directory).StartsWith(".", StringComparison.Ordinal)) continue;
				CheckAncestors(directory);
				if (!ReadMarker(directory, out string marker)) continue;
				string[] parts = marker.Split('\n');
				if (parts.Length != 6 || parts[0] != Schema || parts[1] != "1" || parts[2] != "committed" || parts[3] != Path.GetFileName(directory) || parts[4].Length != 32 || parts[5].Length != 0) continue;
				RetainedExport item = new RetainedExport { Directory = directory, Marker = marker, Created = Directory.GetCreationTimeUtc(directory), CanDelete = true };
				foreach (string entry in Directory.GetFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly))
				{
					FileAttributes attributes = File.GetAttributes(entry);
					if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
						throw new IOException("Retained export usage cannot be established safely because an owned export contains a directory or reparse point.");
					item.Bytes = checked(item.Bytes + new FileInfo(entry).Length);
					if (Array.IndexOf(AllowedFiles, Path.GetFileName(entry)) < 0) item.CanDelete = false;
				}
				if (!File.Exists(Path.Combine(directory, "manifest.json"))) item.CanDelete = false;
				retainedBytes = checked(retainedBytes + item.Bytes); exports.Add(item);
			}
			exports.Sort((left, right) => { int order = left.Created.CompareTo(right.Created); return order != 0 ? order : string.CompareOrdinal(left.Directory, right.Directory); });
			int count = exports.Count + 1;
			long bytes = checked(retainedBytes + incomingBytes);
			List<RetainedExport> remove = new List<RetainedExport>();
			foreach (RetainedExport item in exports)
			{
				if (count <= PerfMeterFrameDebuggerExportOptions.MaxRetainedExports && bytes <= PerfMeterFrameDebuggerExportOptions.MaxRetainedBytes) break;
				if (!item.CanDelete || string.Equals(Path.GetFileName(item.Directory), currentId, StringComparison.Ordinal)) continue;
				remove.Add(item); count--; bytes -= item.Bytes;
			}
			if (count > PerfMeterFrameDebuggerExportOptions.MaxRetainedExports || bytes > PerfMeterFrameDebuggerExportOptions.MaxRetainedBytes) throw new IOException("Export retention quota cannot be met without deleting unowned or unsafe content.");
			foreach (RetainedExport item in remove) { token.ThrowIfCancellationRequested(); CheckAncestors(root); DeleteKnownDirectory(item.Directory, item.Marker); }
			CheckAncestors(root);
		}

		private static void DeleteKnownDirectory(string directory, string expectedMarker)
		{
			CheckAncestors(directory);
			if (!ReadMarker(directory, out string actual) || actual != expectedMarker) throw new IOException("Export ownership changed during cleanup.");
			string[] entries = Directory.GetFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly);
			foreach (string entry in entries)
			{
				CheckAncestors(entry);
				if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0 || Array.IndexOf(AllowedFiles, Path.GetFileName(entry)) < 0) throw new IOException("Unknown content prevents export cleanup.");
			}
			foreach (string entry in entries)
			{
				if (Path.GetFileName(entry) == MarkerFileName) continue;
				CheckAncestors(entry); File.Delete(entry);
			}
			CheckAncestors(directory);
			if (!ReadMarker(directory, out actual) || actual != expectedMarker) throw new IOException("Export ownership changed during cleanup.");
			File.Delete(Path.Combine(directory, MarkerFileName));
			Directory.Delete(directory, false);
		}
	}
}