using System;

namespace SGG.PerfMeter.Editor
{
	internal static class PerfMeterRenderDocAnalyzerJsonShape
	{
		private const int MaximumNesting = 128;
		private static readonly string[] RequestRoot = { "schema", "schema_version", "request_id", "capture", "counter_selection", "options" };
		private static readonly string[] RequestCapture = { "artifact_id", "capture_id", "bundle_id", "path", "sha256", "size_bytes", "association_state", "finalization_state", "authority_state", "content_state", "source_file_identity_sha256", "capture_app_api" };
		private static readonly string[] CounterSelection = { "mode", "packs", "explicit_counter_ids" };
		private static readonly string[] RequestOptions = { "include_action_tree", "include_raw_counter_descriptions", "timeout_seconds", "max_actions", "max_counter_results", "max_output_bytes" };
		private static readonly string[] ResultRoot = { "schema", "schema_version", "request_id", "status", "capture", "analyzer", "actions_complete", "action_total_count", "actions", "counter_catalog_complete", "counter_total_count", "counter_catalog", "results_complete", "result_total_count", "results", "summary", "warnings", "diagnostics" };
		private static readonly string[] ResultCapture = { "artifact_id", "capture_id", "bundle_id", "expected_sha256", "observed_sha256", "hash_verified", "size_bytes", "association_state", "finalization_state", "authority_state", "content_state", "source_file_identity_sha256", "capture_app_api" };
		private static readonly string[] Analyzer = { "analyzer_version", "renderdoc_build", "replay_api", "graphics_api", "gpu", "driver", "replay_host", "started_utc", "completed_utc", "duration_milliseconds" };
		private static readonly string[] Action = { "event_id", "parent_event_id", "name", "flags" };
		private static readonly string[] CounterMetadata = { "id", "native_id", "name", "description", "unit", "result_type", "result_byte_width", "aggregation", "availability", "reason", "requested", "fetched", "pass_index", "provenance" };
		private static readonly string[] CounterResult = { "event_id", "counter_id", "availability", "reason", "raw_value" };
		private static readonly string[] RawValue = { "result_type", "byte_width", "encoding", "value" };
		private static readonly string[] Summary = { "requested_counter_count", "described_counter_count", "fetched_counter_count", "unsupported_counter_count", "failed_counter_count", "failed_result_count", "replay_pass_count", "duration_milliseconds" };
		private static readonly string[] ErrorRoot = { "schema", "schema_version", "request_id", "status", "capture", "analyzer", "error", "warnings", "diagnostics" };
		private static readonly string[] ErrorCapture = { "expected_sha256", "observed_sha256", "hash_verified" };
		private static readonly string[] ErrorDetail = { "code", "stage", "message", "retryable" };

		internal static bool ValidateRequest(string json, out string error)
		{
			JsonSpan[] rootValues = new JsonSpan[RequestRoot.Length];
			JsonSpan[] captureValues = new JsonSpan[RequestCapture.Length];
			JsonSpan[] selectionValues = new JsonSpan[CounterSelection.Length];
			JsonSpan[] optionValues = new JsonSpan[RequestOptions.Length];
			if (!TryGetRoot(json, out JsonSpan root) ||
				!TryReadObjectFields(json, root, RequestRoot, FullMask(RequestRoot), rootValues, out _) ||
				rootValues[3].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[3], RequestCapture, FullMask(RequestCapture), captureValues, out _) ||
				rootValues[4].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[4], CounterSelection, FullMask(CounterSelection), selectionValues, out _) ||
				rootValues[5].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[5], RequestOptions, FullMask(RequestOptions), optionValues, out _) ||
				optionValues[0].Kind != 'b' || optionValues[1].Kind != 'b')
			{
				error = "missing_required_boolean";
				return false;
			}

			error = string.Empty;
			return true;
		}

		internal static bool ValidateResult(string json, out string error)
		{
			JsonSpan[] rootValues = new JsonSpan[ResultRoot.Length];
			JsonSpan[] captureValues = new JsonSpan[ResultCapture.Length];
			JsonSpan[] analyzerValues = new JsonSpan[Analyzer.Length];
			JsonSpan[] summaryValues = new JsonSpan[Summary.Length];
			if (!TryGetRoot(json, out JsonSpan root) ||
				!TryReadObjectFields(json, root, ResultRoot, FullMask(ResultRoot), rootValues, out _) ||
				rootValues[4].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[4], ResultCapture, FullMask(ResultCapture), captureValues, out _) ||
				captureValues[5].Kind != 'b' ||
				rootValues[5].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[5], Analyzer, FullMask(Analyzer), analyzerValues, out _) ||
				rootValues[6].Kind != 'b' || rootValues[9].Kind != 'b' || rootValues[12].Kind != 'b' ||
				rootValues[8].Kind != '[' || !ValidateObjectArray(json, rootValues[8], Action) ||
				rootValues[11].Kind != '[' || !ValidateCatalog(json, rootValues[11]) ||
				rootValues[14].Kind != '[' || !ValidateResults(json, rootValues[14]) ||
				rootValues[15].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[15], Summary, FullMask(Summary), summaryValues, out _))
			{
				error = "missing_required_field";
				return false;
			}

			error = string.Empty;
			return true;
		}

		internal static bool ValidateError(string json, out string error)
		{
			JsonSpan[] rootValues = new JsonSpan[ErrorRoot.Length];
			JsonSpan[] captureValues = new JsonSpan[ErrorCapture.Length];
			JsonSpan[] analyzerValues = new JsonSpan[Analyzer.Length];
			JsonSpan[] detailValues = new JsonSpan[ErrorDetail.Length];
			if (!TryGetRoot(json, out JsonSpan root) ||
				!TryReadObjectFields(json, root, ErrorRoot, FullMask(ErrorRoot), rootValues, out _) ||
				rootValues[4].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[4], ErrorCapture, FullMask(ErrorCapture), captureValues, out _) ||
				captureValues[2].Kind != 'b' ||
				rootValues[5].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[5], Analyzer, FullMask(Analyzer), analyzerValues, out _) ||
				rootValues[6].Kind != '{' ||
				!TryReadObjectFields(json, rootValues[6], ErrorDetail, FullMask(ErrorDetail), detailValues, out _) ||
				detailValues[3].Kind != 'b')
			{
				error = "missing_required_boolean";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateCatalog(string json, JsonSpan array)
		{
			int index = array.Start + 1;
			JsonSpan[] values = new JsonSpan[CounterMetadata.Length];
			while (true)
			{
				SkipWhitespace(json, ref index, array.End);
				if (index >= array.End || json[index] == ']')
				{
					return index < array.End;
				}

				if (!TryParseValue(json, ref index, array.End, 0, out JsonSpan item) ||
					item.Kind != '{' ||
					!TryReadObjectFields(json, item, CounterMetadata, FullMask(CounterMetadata), values, out _) ||
					values[10].Kind != 'b' || values[11].Kind != 'b')
				{
					return false;
				}

				SkipWhitespace(json, ref index, array.End);
				if (index < array.End && json[index] == ',')
				{
					index++;
					continue;
				}

				return index < array.End && json[index] == ']';
			}
		}

		private static bool ValidateResults(string json, JsonSpan array)
		{
			int index = array.Start + 1;
			JsonSpan[] values = new JsonSpan[CounterResult.Length];
			JsonSpan[] rawValues = new JsonSpan[RawValue.Length];
			while (true)
			{
				SkipWhitespace(json, ref index, array.End);
				if (index >= array.End || json[index] == ']')
				{
					return index < array.End;
				}

				if (!TryParseValue(json, ref index, array.End, 0, out JsonSpan item) ||
					item.Kind != '{' ||
					!TryReadObjectFields(json, item, CounterResult, 0x0fuL, values, out ulong present))
				{
					return false;
				}

				if ((present & 0x10uL) != 0uL)
				{
					JsonSpan rawValue = values[4];
					if (rawValue.Kind != '{' ||
						!TryReadObjectFields(json, rawValue, RawValue, FullMask(RawValue), rawValues, out _))
					{
						return false;
					}
				}

				SkipWhitespace(json, ref index, array.End);
				if (index < array.End && json[index] == ',')
				{
					index++;
					continue;
				}

				return index < array.End && json[index] == ']';
			}
		}

		private static bool ValidateObjectArray(string json, JsonSpan array, string[] requiredProperties)
		{
			int index = array.Start + 1;
			JsonSpan[] values = new JsonSpan[requiredProperties.Length];
			while (true)
			{
				SkipWhitespace(json, ref index, array.End);
				if (index >= array.End || json[index] == ']')
				{
					return index < array.End;
				}

				if (!TryParseValue(json, ref index, array.End, 0, out JsonSpan item) ||
					item.Kind != '{' ||
					!TryReadObjectFields(json, item, requiredProperties, FullMask(requiredProperties), values, out _))
				{
					return false;
				}

				SkipWhitespace(json, ref index, array.End);
				if (index < array.End && json[index] == ',')
				{
					index++;
					continue;
				}

				return index < array.End && json[index] == ']';
			}
		}

		private static bool TryReadObjectFields(
			string json,
			JsonSpan parent,
			string[] knownProperties,
			ulong requiredMask,
			JsonSpan[] values,
			out ulong presentMask)
		{
			presentMask = 0uL;
			if (parent.Kind != '{' || knownProperties.Length > 64 || values == null || values.Length < knownProperties.Length)
			{
				return false;
			}

			int index = parent.Start + 1;
			while (true)
			{
				SkipWhitespace(json, ref index, parent.End);
				if (index >= parent.End || json[index] == '}')
				{
					return index < parent.End && (presentMask & requiredMask) == requiredMask;
				}

				if (!TryParseString(json, ref index, parent.End, out int nameStart, out int nameLength, out _))
				{
					return false;
				}

				SkipWhitespace(json, ref index, parent.End);
				if (index >= parent.End || json[index++] != ':')
				{
					return false;
				}

				SkipWhitespace(json, ref index, parent.End);
				if (!TryParseValue(json, ref index, parent.End, 0, out JsonSpan current))
				{
					return false;
				}

				int propertyIndex = MatchKnownProperty(json, nameStart, nameLength, knownProperties);
				if (propertyIndex >= 0)
				{
					ulong bit = 1uL << propertyIndex;
					if ((presentMask & bit) != 0uL)
					{
						return false;
					}

					presentMask |= bit;
					values[propertyIndex] = current;
				}

				SkipWhitespace(json, ref index, parent.End);
				if (index < parent.End && json[index] == ',')
				{
					index++;
					continue;
				}

				return index < parent.End && json[index] == '}' && (presentMask & requiredMask) == requiredMask;
			}
		}

		private static ulong FullMask(string[] properties)
		{
			return properties.Length == 64 ? ulong.MaxValue : (1uL << properties.Length) - 1uL;
		}

		private static int MatchKnownProperty(string json, int start, int length, string[] knownProperties)
		{
			for (int i = 0; i < knownProperties.Length; i++)
			{
				if (DecodedNameEquals(json, start, length, knownProperties[i]))
				{
					return i;
				}
			}

			return -1;
		}

		private static bool TryGetRoot(string json, out JsonSpan root)
		{
			root = default;
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}

			int index = 0;
			SkipWhitespace(json, ref index, json.Length);
			if (!TryParseValue(json, ref index, json.Length, 0, out root) || root.Kind != '{')
			{
				return false;
			}

			SkipWhitespace(json, ref index, json.Length);
			return index == json.Length;
		}

		private static bool TryParseValue(string json, ref int index, int limit, int depth, out JsonSpan value)
		{
			value = default;
			if (depth > MaximumNesting || index >= limit)
			{
				return false;
			}

			int start = index;
			char kind = json[index];
			if (kind == '"')
			{
				if (!TryParseString(json, ref index, limit, out _, out _, out _))
				{
					return false;
				}

				value = new JsonSpan(start, index, '"');
				return true;
			}

			if (kind == '{' || kind == '[')
			{
				char close = kind == '{' ? '}' : ']';
				index++;
				while (true)
				{
					SkipWhitespace(json, ref index, limit);
					if (index >= limit)
					{
						return false;
					}

					if (json[index] == close)
					{
						index++;
						value = new JsonSpan(start, index, kind);
						return true;
					}

					if (kind == '{')
					{
						if (!TryParseString(json, ref index, limit, out _, out _, out _))
						{
							return false;
						}

						SkipWhitespace(json, ref index, limit);
						if (index >= limit || json[index++] != ':')
						{
							return false;
						}
					}

					SkipWhitespace(json, ref index, limit);
					if (!TryParseValue(json, ref index, limit, depth + 1, out _))
					{
						return false;
					}

					SkipWhitespace(json, ref index, limit);
					if (index < limit && json[index] == ',')
					{
						index++;
						continue;
					}

					if (index >= limit || json[index] != close)
					{
						return false;
					}
				}
			}

			while (index < limit && json[index] != ',' && json[index] != '}' && json[index] != ']' && !char.IsWhiteSpace(json[index]))
			{
				index++;
			}

			if (index == start)
			{
				return false;
			}

			kind = SpanEquals(json, start, index - start, "true") || SpanEquals(json, start, index - start, "false")
				? 'b'
				: SpanEquals(json, start, index - start, "null") ? 'n' : '#';
			value = new JsonSpan(start, index, kind);
			return true;
		}

		private static bool TryParseString(
			string json,
			ref int index,
			int limit,
			out int contentStart,
			out int contentLength,
			out bool escaped)
		{
			contentStart = 0;
			contentLength = 0;
			escaped = false;
			if (index >= limit || json[index] != '"')
			{
				return false;
			}

			contentStart = ++index;
			while (index < limit)
			{
				char character = json[index++];
				if (character == '"')
				{
					contentLength = index - contentStart - 1;
					return true;
				}

				if (character == '\\')
				{
					escaped = true;
					if (index >= limit)
					{
						return false;
					}

					index++;
				}
				else if (character < 0x20)
				{
					return false;
				}
			}

			return false;
		}

		private static bool DecodedNameEquals(string json, int start, int length, string expected)
		{
			int source = start;
			int sourceEnd = start + length;
			int target = 0;
			while (source < sourceEnd && target < expected.Length)
			{
				char character = json[source++];
				if (character == '\\')
				{
					if (source >= sourceEnd)
					{
						return false;
					}

					char escape = json[source++];
					switch (escape)
					{
						case '"': character = '"'; break;
						case '\\': character = '\\'; break;
						case '/': character = '/'; break;
						case 'b': character = '\b'; break;
						case 'f': character = '\f'; break;
						case 'n': character = '\n'; break;
						case 'r': character = '\r'; break;
						case 't': character = '\t'; break;
						case 'u':
							if (source + 4 > sourceEnd || !TryParseHex4(json, source, out character))
							{
								return false;
							}

							source += 4;
							break;
						default:
							return false;
					}
				}

				if (character != expected[target++])
				{
					return false;
				}
			}

			return source == sourceEnd && target == expected.Length;
		}

		private static bool TryParseHex4(string json, int start, out char value)
		{
			int parsed = 0;
			for (int i = 0; i < 4; i++)
			{
				char character = json[start + i];
				int digit = character >= '0' && character <= '9'
					? character - '0'
					: character >= 'a' && character <= 'f'
						? character - 'a' + 10
						: character >= 'A' && character <= 'F' ? character - 'A' + 10 : -1;
				if (digit < 0)
				{
					value = default;
					return false;
				}

				parsed = (parsed << 4) | digit;
			}

			value = (char)parsed;
			return true;
		}

		private static bool SpanEquals(string json, int start, int length, string expected)
		{
			if (length != expected.Length)
			{
				return false;
			}

			for (int i = 0; i < length; i++)
			{
				if (json[start + i] != expected[i])
				{
					return false;
				}
			}

			return true;
		}

		private static void SkipWhitespace(string json, ref int index, int limit)
		{
			while (index < limit && char.IsWhiteSpace(json[index]))
			{
				index++;
			}
		}

		private readonly struct JsonSpan
		{
			internal JsonSpan(int start, int end, char kind)
			{
				Start = start;
				End = end;
				Kind = kind;
			}

			internal int Start { get; }
			internal int End { get; }
			internal char Kind { get; }
		}
	}
}
