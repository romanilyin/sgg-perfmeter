using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SGG.PerfMeter.Editor
{
	internal static class PerfMeterRenderDocAnalyzerProtocol
	{
		internal const string RequestSchema = "sgg.perfmeter.renderdoc-analysis-request";
		internal const string ResultSchema = "sgg.perfmeter.renderdoc-analysis";
		internal const string ErrorSchema = "sgg.perfmeter.renderdoc-analysis-error";
		internal const int CurrentSchemaVersion = 1;
		internal const int MaximumDocumentBytes = 64 * 1024 * 1024;
		internal const int MaximumActions = 1000000;
		internal const int MaximumCounters = 4096;
		internal const int MaximumResults = 1000000;
		internal const ulong MaximumCaptureBytes = 512uL * 1024uL * 1024uL;

		internal static bool TryReadRequest(string json, out PerfMeterRenderDocAnalysisRequest document, out string error)
		{
			if (!ValidateInputSize(json, MaximumDocumentBytes, out error) ||
				!PerfMeterRenderDocAnalyzerJsonShape.ValidateRequest(json, out error))
			{
				document = null;
				return false;
			}

			return TryDeserialize(json, out document, out error) && ValidateRequest(document, out error);
		}

		internal static bool TryReadResult(
			PerfMeterRenderDocAnalysisRequest request,
			string json,
			out PerfMeterRenderDocAnalysisResult document,
			out string error)
		{
			document = null;
			if (!ValidateRequest(request, out error))
			{
				error = "invalid_request_binding";
				return false;
			}

			if (!ValidateInputSize(json, request.options.max_output_bytes, out error) ||
				!PerfMeterRenderDocAnalyzerJsonShape.ValidateResult(json, out error))
			{
				return false;
			}

			if (!TryDeserialize(json, request.options.max_output_bytes, out document, out error))
			{
				return false;
			}

			NormalizeAbsentRawValues(document);
			return ValidateResult(document, out error) && ValidateResultBinding(request, document, out error);
		}

		internal static bool TryReadError(
			PerfMeterRenderDocAnalysisRequest request,
			string json,
			out PerfMeterRenderDocAnalysisError document,
			out string error)
		{
			document = null;
			if (!ValidateRequest(request, out error))
			{
				error = "invalid_request_binding";
				return false;
			}

			if (!ValidateInputSize(json, request.options.max_output_bytes, out error) ||
				!PerfMeterRenderDocAnalyzerJsonShape.ValidateError(json, out error))
			{
				return false;
			}

			return TryDeserialize(json, request.options.max_output_bytes, out document, out error) &&
				ValidateError(document, out error) &&
				ValidateErrorBinding(request, document, out error);
		}

		internal static bool TryWrite(PerfMeterRenderDocAnalysisRequest document, out string json, out string error)
		{
			if (!ValidateRequest(document, out error))
			{
				json = string.Empty;
				return false;
			}

			json = JsonUtility.ToJson(document);
			return ValidateOutputSize(json, out error);
		}

		internal static bool TryWrite(
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalysisResult document,
			out string json,
			out string error)
		{
			if (!ValidateRequest(request, out error) ||
				!ValidateResult(document, out error) ||
				!ValidateResultBinding(request, document, out error))
			{
				json = string.Empty;
				return false;
			}

			json = JsonUtility.ToJson(document);
			return ValidateOutputSize(json, request.options.max_output_bytes, out error);
		}

		internal static bool TryWrite(
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalysisError document,
			out string json,
			out string error)
		{
			if (!ValidateRequest(request, out error) ||
				!ValidateError(document, out error) ||
				!ValidateErrorBinding(request, document, out error))
			{
				json = string.Empty;
				return false;
			}

			json = JsonUtility.ToJson(document);
			return ValidateOutputSize(json, request.options.max_output_bytes, out error);
		}

		private static bool ValidateRequest(PerfMeterRenderDocAnalysisRequest document, out string error)
		{
			if (!ValidateEnvelope(document, RequestSchema, out error))
			{
				return false;
			}

			PerfMeterRenderDocAnalysisCaptureRequest capture = document.capture;
			if (capture == null ||
				!IsToken(capture.artifact_id, 128) ||
				!IsToken(capture.capture_id, 128) ||
				!IsToken(capture.bundle_id, 128) ||
				!IsSafeRelativeCapturePath(capture.path) ||
				!IsSha256(capture.sha256) ||
				!TryParseCanonicalUInt64(capture.size_bytes, out ulong sizeBytes) ||
				sizeBytes == 0u || sizeBytes > MaximumCaptureBytes ||
				capture.association_state != "bridge_authenticated" ||
				capture.finalization_state != "finalized" ||
				capture.authority_state != "authenticated" ||
				capture.content_state != "present" ||
				!IsSha256(capture.source_file_identity_sha256) ||
				!IsBoundedText(capture.capture_app_api, 64, false))
			{
				error = "invalid_capture";
				return false;
			}

			PerfMeterRenderDocCounterSelection selection = document.counter_selection;
			if (selection == null ||
				!ValidateUniqueTokens(selection.packs, 64, 128) ||
				!ValidateUniqueTokens(selection.explicit_counter_ids, MaximumCounters, 128) ||
				!ValidateSelectionContents(selection))
			{
				error = "invalid_counter_selection";
				return false;
			}

			PerfMeterRenderDocAnalysisOptions options = document.options;
			if (options == null ||
				options.timeout_seconds < 1 || options.timeout_seconds > 1800 ||
				options.max_actions < 1 || options.max_actions > MaximumActions ||
				options.max_counter_results < 1 || options.max_counter_results > MaximumResults ||
				options.max_output_bytes < 1024 || options.max_output_bytes > MaximumDocumentBytes)
			{
				error = "invalid_options";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateResult(PerfMeterRenderDocAnalysisResult document, out string error)
		{
			if (!ValidateEnvelope(document, ResultSchema, out error) ||
				(document.status != "completed" && document.status != "completed_with_errors"))
			{
				error = string.IsNullOrEmpty(error) ? "invalid_status" : error;
				return false;
			}

			if (!ValidateCompletedCapture(document.capture, out error) ||
				!ValidateAnalyzerProvenance(document.analyzer, false, out error) ||
				!ValidateWarningsAndDiagnostics(document.warnings, document.diagnostics, out error))
			{
				return false;
			}

			PerfMeterRenderDocAction[] actions = document.actions;
			if (!document.actions_complete ||
				actions == null ||
				document.action_total_count != actions.Length ||
				actions.Length > MaximumActions ||
				!ValidateActionTree(actions, out error))
			{
				error = string.IsNullOrEmpty(error) ? "incomplete_action_tree" : error;
				return false;
			}

			PerfMeterRenderDocCounterMetadata[] catalog = document.counter_catalog;
			if (!document.counter_catalog_complete ||
				catalog == null ||
				document.counter_total_count != catalog.Length ||
				catalog.Length > MaximumCounters ||
				!ValidateCounterCatalog(catalog, document.summary, out error))
			{
				error = string.IsNullOrEmpty(error) ? "incomplete_counter_catalog" : error;
				return false;
			}

			PerfMeterRenderDocCounterResult[] results = document.results;
			if (!document.results_complete ||
				results == null ||
				document.result_total_count != results.Length ||
				results.Length > MaximumResults ||
				!ValidateCounterResults(results, actions, catalog, out int failedResultCount, out error))
			{
				error = string.IsNullOrEmpty(error) ? "incomplete_counter_results" : error;
				return false;
			}

			if (document.summary.failed_result_count != failedResultCount ||
				(document.summary.failed_counter_count + failedResultCount == 0 && document.status != "completed") ||
				(document.summary.failed_counter_count + failedResultCount > 0 && document.status != "completed_with_errors") ||
				!string.Equals(document.summary.duration_milliseconds, document.analyzer.duration_milliseconds, StringComparison.Ordinal))
			{
				error = "inconsistent_result_status";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateError(PerfMeterRenderDocAnalysisError document, out string error)
		{
			if (!ValidateEnvelope(document, ErrorSchema, out error) || document.status != "failed")
			{
				error = string.IsNullOrEmpty(error) ? "invalid_status" : error;
				return false;
			}

			PerfMeterRenderDocAnalysisErrorCapture capture = document.capture;
			if (capture == null ||
				!IsSha256(capture.expected_sha256) ||
				(!string.IsNullOrEmpty(capture.observed_sha256) && !IsSha256(capture.observed_sha256)) ||
				(capture.hash_verified && !string.Equals(capture.expected_sha256, capture.observed_sha256, StringComparison.Ordinal)))
			{
				error = "invalid_capture";
				return false;
			}

			PerfMeterRenderDocAnalysisErrorDetail detail = document.error;
			if (detail == null ||
				!IsToken(detail.code, 128) ||
				!IsToken(detail.stage, 128) ||
				!IsBoundedText(detail.message, 4096, false) ||
				ContainsAbsolutePath(detail.message))
			{
				error = "invalid_error";
				return false;
			}

			if (!ValidateAnalyzerProvenance(document.analyzer, true, out error) ||
				!ValidateWarningsAndDiagnostics(document.warnings, document.diagnostics, out error))
			{
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateEnvelope(PerfMeterRenderDocAnalysisDocument document, string schema, out string error)
		{
			if (document == null)
			{
				error = "document_required";
				return false;
			}

			if (!string.Equals(document.schema, schema, StringComparison.Ordinal))
			{
				error = "schema_mismatch";
				return false;
			}

			if (document.schema_version != CurrentSchemaVersion)
			{
				error = "unsupported_schema_version";
				return false;
			}

			if (!IsToken(document.request_id, 128))
			{
				error = "invalid_request_id";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateCompletedCapture(PerfMeterRenderDocAnalysisCaptureResult capture, out string error)
		{
			if (capture == null ||
				!IsToken(capture.artifact_id, 128) ||
				!IsToken(capture.capture_id, 128) ||
				!IsToken(capture.bundle_id, 128) ||
				!IsSha256(capture.expected_sha256) ||
				!IsSha256(capture.observed_sha256) ||
				!capture.hash_verified ||
				!string.Equals(capture.expected_sha256, capture.observed_sha256, StringComparison.Ordinal) ||
				!TryParseCanonicalUInt64(capture.size_bytes, out ulong sizeBytes) ||
				sizeBytes == 0u || sizeBytes > MaximumCaptureBytes ||
				capture.association_state != "bridge_authenticated" ||
				capture.finalization_state != "finalized" ||
				capture.authority_state != "authenticated" ||
				capture.content_state != "present" ||
				!IsSha256(capture.source_file_identity_sha256) ||
				!IsBoundedText(capture.capture_app_api, 64, false))
			{
				error = "invalid_capture";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateResultBinding(
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalysisResult result,
			out string error)
		{
			PerfMeterRenderDocAnalysisCaptureRequest expected = request.capture;
			PerfMeterRenderDocAnalysisCaptureResult observed = result.capture;
			if (!string.Equals(result.request_id, request.request_id, StringComparison.Ordinal) ||
				!string.Equals(observed.artifact_id, expected.artifact_id, StringComparison.Ordinal) ||
				!string.Equals(observed.capture_id, expected.capture_id, StringComparison.Ordinal) ||
				!string.Equals(observed.bundle_id, expected.bundle_id, StringComparison.Ordinal) ||
				!string.Equals(observed.expected_sha256, expected.sha256, StringComparison.Ordinal) ||
				!string.Equals(observed.observed_sha256, expected.sha256, StringComparison.Ordinal) ||
				!string.Equals(observed.size_bytes, expected.size_bytes, StringComparison.Ordinal) ||
				!string.Equals(observed.source_file_identity_sha256, expected.source_file_identity_sha256, StringComparison.Ordinal) ||
				!string.Equals(observed.capture_app_api, expected.capture_app_api, StringComparison.Ordinal) ||
				result.actions.Length > request.options.max_actions ||
				result.results.Length > request.options.max_counter_results ||
				(!request.options.include_action_tree && result.actions.Length != 0) ||
				(!request.options.include_raw_counter_descriptions && HasCounterDescriptions(result.counter_catalog)) ||
				!ValidateRequestedActionReferences(request, result))
			{
				error = "response_binding_mismatch";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateErrorBinding(
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalysisError failure,
			out string error)
		{
			if (!string.Equals(failure.request_id, request.request_id, StringComparison.Ordinal) ||
				!string.Equals(failure.capture.expected_sha256, request.capture.sha256, StringComparison.Ordinal) ||
				(failure.capture.hash_verified && !string.Equals(failure.capture.observed_sha256, request.capture.sha256, StringComparison.Ordinal)))
			{
				error = "response_binding_mismatch";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateRequestedActionReferences(
			PerfMeterRenderDocAnalysisRequest request,
			PerfMeterRenderDocAnalysisResult result)
		{
			if (!request.options.include_action_tree)
			{
				return true;
			}

			HashSet<long> eventIds = new HashSet<long>();
			for (int i = 0; i < result.actions.Length; i++)
			{
				eventIds.Add(result.actions[i].event_id);
			}

			for (int i = 0; i < result.results.Length; i++)
			{
				if (!eventIds.Contains(result.results[i].event_id))
				{
					return false;
				}
			}

			return true;
		}

		private static bool ValidateAnalyzerProvenance(PerfMeterRenderDocAnalyzerProvenance analyzer, bool allowEmptyRenderDoc, out string error)
		{
			if (analyzer == null ||
				!IsBoundedText(analyzer.analyzer_version, 128, false) ||
				!IsBoundedText(analyzer.renderdoc_build, 256, allowEmptyRenderDoc) ||
				!IsBoundedText(analyzer.replay_api, 128, allowEmptyRenderDoc) ||
				!IsBoundedText(analyzer.graphics_api, 128, allowEmptyRenderDoc) ||
				!IsBoundedText(analyzer.gpu, 512, true) ||
				!IsBoundedText(analyzer.driver, 512, true) ||
				!IsBoundedText(analyzer.replay_host, 128, false) ||
				ContainsAbsolutePath(analyzer.analyzer_version) ||
				ContainsAbsolutePath(analyzer.renderdoc_build) ||
				ContainsAbsolutePath(analyzer.replay_api) ||
				ContainsAbsolutePath(analyzer.graphics_api) ||
				ContainsAbsolutePath(analyzer.gpu) ||
				ContainsAbsolutePath(analyzer.driver) ||
				ContainsAbsolutePath(analyzer.replay_host) ||
				!TryParseUtc(analyzer.started_utc, out DateTimeOffset started) ||
				!TryParseUtc(analyzer.completed_utc, out DateTimeOffset completed) ||
				completed < started ||
				!TryParseCanonicalUInt64(analyzer.duration_milliseconds, out ulong durationMilliseconds) ||
				Math.Abs((completed - started).TotalMilliseconds - durationMilliseconds) > 1d)
			{
				error = "invalid_analyzer_provenance";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateActionTree(PerfMeterRenderDocAction[] actions, out string error)
		{
			HashSet<long> eventIds = new HashSet<long>();
			for (int i = 0; i < actions.Length; i++)
			{
				PerfMeterRenderDocAction action = actions[i];
				if (action == null ||
					action.event_id <= 0L || action.event_id > uint.MaxValue ||
					action.parent_event_id < 0L || action.parent_event_id > uint.MaxValue ||
					action.parent_event_id == action.event_id ||
					!IsBoundedText(action.name, 1024, true) ||
					!ValidateTokens(action.flags, 128, 128) ||
					!eventIds.Add(action.event_id) ||
					(action.parent_event_id != 0L && !eventIds.Contains(action.parent_event_id)))
				{
					error = "invalid_action_tree";
					return false;
				}
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateCounterCatalog(
			PerfMeterRenderDocCounterMetadata[] catalog,
			PerfMeterRenderDocAnalysisSummary summary,
			out string error)
		{
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			int requestedCount = 0;
			int fetchedCount = 0;
			int unsupportedCount = 0;
			int failedCount = 0;
			for (int i = 0; i < catalog.Length; i++)
			{
				PerfMeterRenderDocCounterMetadata metadata = catalog[i];
				if (metadata == null ||
					!IsToken(metadata.id, 128) ||
					metadata.native_id < 0L || metadata.native_id > uint.MaxValue ||
					!IsBoundedText(metadata.name, 512, false) ||
					!IsBoundedText(metadata.description, 4096, true) ||
					!IsBoundedText(metadata.unit, 128, false) ||
					!IsBoundedText(metadata.result_type, 128, false) ||
					metadata.result_byte_width < 1 || metadata.result_byte_width > 32 ||
					!IsAggregation(metadata.aggregation) ||
					!IsAvailability(metadata.availability) ||
					!IsBoundedText(metadata.reason, 1024, true) ||
					ContainsAbsolutePath(metadata.reason) ||
					!IsToken(metadata.provenance, 128) ||
					!ids.Add(metadata.id) ||
					!ValidateMetadataState(metadata))
				{
					error = "invalid_counter_catalog";
					return false;
				}

				requestedCount += metadata.requested ? 1 : 0;
				fetchedCount += metadata.fetched ? 1 : 0;
				unsupportedCount += metadata.availability == "unsupported" ? 1 : 0;
				failedCount += metadata.availability == "fetch_failed" ? 1 : 0;
			}

			if (summary == null ||
				summary.requested_counter_count != requestedCount ||
				summary.described_counter_count != catalog.Length ||
				summary.fetched_counter_count != fetchedCount ||
				summary.unsupported_counter_count != unsupportedCount ||
				summary.failed_counter_count != failedCount ||
				summary.replay_pass_count < 0 ||
				!TryParseCanonicalUInt64(summary.duration_milliseconds, out _))
			{
				error = "invalid_summary";
				return false;
			}

			for (int i = 0; i < catalog.Length; i++)
			{
				if (catalog[i].pass_index >= summary.replay_pass_count)
				{
					error = "invalid_counter_pass";
					return false;
				}
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateCounterResults(
			PerfMeterRenderDocCounterResult[] results,
			PerfMeterRenderDocAction[] actions,
			PerfMeterRenderDocCounterMetadata[] catalog,
			out int failedResultCount,
			out string error)
		{
			failedResultCount = 0;
			HashSet<long> eventIds = new HashSet<long>();
			for (int i = 0; i < actions.Length; i++)
			{
				eventIds.Add(actions[i].event_id);
			}

			Dictionary<string, PerfMeterRenderDocCounterMetadata> metadataById = new Dictionary<string, PerfMeterRenderDocCounterMetadata>(StringComparer.Ordinal);
			for (int i = 0; i < catalog.Length; i++)
			{
				metadataById.Add(catalog[i].id, catalog[i]);
			}

			HashSet<string> resultKeys = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < results.Length; i++)
			{
				PerfMeterRenderDocCounterResult result = results[i];
				if (result == null ||
					result.event_id <= 0L || result.event_id > uint.MaxValue ||
					(actions.Length > 0 && !eventIds.Contains(result.event_id)) ||
					!metadataById.TryGetValue(result.counter_id ?? string.Empty, out PerfMeterRenderDocCounterMetadata metadata) ||
					!IsAvailability(result.availability) ||
					!IsBoundedText(result.reason, 1024, true) ||
					ContainsAbsolutePath(result.reason) ||
					!resultKeys.Add(result.event_id.ToString(CultureInfo.InvariantCulture) + "\n" + result.counter_id) ||
					!ValidateCounterResultValue(result, metadata))
				{
					error = "invalid_counter_result";
					return false;
				}

				failedResultCount += result.availability == "fetch_failed" ? 1 : 0;
			}

			error = string.Empty;
			return true;
		}

		private static bool HasCounterDescriptions(PerfMeterRenderDocCounterMetadata[] catalog)
		{
			for (int i = 0; i < catalog.Length; i++)
			{
				if (!string.IsNullOrEmpty(catalog[i].description))
				{
					return true;
				}
			}

			return false;
		}

		private static bool ValidateCounterResultValue(PerfMeterRenderDocCounterResult result, PerfMeterRenderDocCounterMetadata metadata)
		{
			if (metadata.availability == "not_requested" || metadata.availability == "unsupported")
			{
				return result.availability == metadata.availability && IsAbsentRawValue(result.raw_value);
			}

			if (metadata.availability == "fetch_failed")
			{
				return result.availability == "fetch_failed" && IsAbsentRawValue(result.raw_value);
			}

			if (metadata.availability == "not_applicable")
			{
				return result.availability == "not_applicable" && IsAbsentRawValue(result.raw_value);
			}

			if (result.availability != "available")
			{
				return (result.availability == "not_applicable" || result.availability == "fetch_failed") &&
					IsAbsentRawValue(result.raw_value);
			}

			PerfMeterRenderDocRawValue raw = result.raw_value;
			if (raw == null ||
				!string.Equals(raw.result_type, metadata.result_type, StringComparison.Ordinal) ||
				raw.byte_width != metadata.result_byte_width ||
				!IsToken(raw.encoding, 64) ||
				!IsBoundedText(raw.value, 512, false))
			{
				return false;
			}

			switch (raw.result_type)
			{
				case "UInt":
					return raw.encoding == "decimal" && ValidateUnsignedValue(raw.value, raw.byte_width);
				case "SInt":
					return raw.encoding == "decimal" && ValidateSignedValue(raw.value, raw.byte_width);
				case "Float":
					return raw.encoding == "decimal" &&
						((raw.byte_width == 4 && TryParseFiniteFloat(raw.value)) ||
						(raw.byte_width == 8 && TryParseFiniteDouble(raw.value)));
				default:
					return true;
			}
		}

		private static bool IsAbsentRawValue(PerfMeterRenderDocRawValue raw)
		{
			return raw == null ||
				(string.IsNullOrEmpty(raw.result_type) &&
				raw.byte_width == 0 &&
				string.IsNullOrEmpty(raw.encoding) &&
				string.IsNullOrEmpty(raw.value));
		}

		private static void NormalizeAbsentRawValues(PerfMeterRenderDocAnalysisResult document)
		{
			if (document == null || document.results == null)
			{
				return;
			}

			for (int i = 0; i < document.results.Length; i++)
			{
				PerfMeterRenderDocCounterResult result = document.results[i];
				if (result != null && result.availability != "available" && IsAbsentRawValue(result.raw_value))
				{
					result.raw_value = null;
				}
			}
		}

		private static bool ValidateMetadataState(PerfMeterRenderDocCounterMetadata metadata)
		{
			if (metadata.availability == "not_requested")
			{
				return !metadata.requested && !metadata.fetched && metadata.pass_index == -1;
			}

			if (!metadata.requested)
			{
				return false;
			}

			if (metadata.availability == "available")
			{
				return metadata.fetched && metadata.pass_index >= 0;
			}

			if (metadata.availability == "fetch_failed")
			{
				return !metadata.fetched && metadata.pass_index >= 0;
			}

			return !metadata.fetched && metadata.pass_index == -1;
		}

		private static bool ValidateWarningsAndDiagnostics(string[] warnings, string[] diagnostics, out string error)
		{
			if (warnings == null || warnings.Length > 128 || diagnostics == null || diagnostics.Length != 0)
			{
				error = "invalid_diagnostics";
				return false;
			}

			for (int i = 0; i < warnings.Length; i++)
			{
				if (!IsBoundedText(warnings[i], 2048, false) || ContainsAbsolutePath(warnings[i]))
				{
					error = "invalid_warning";
					return false;
				}
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateSelectionContents(PerfMeterRenderDocCounterSelection selection)
		{
			switch (selection.mode)
			{
				case "none":
					return selection.packs.Length == 0 && selection.explicit_counter_ids.Length == 0;
				case "semantic_pack":
					return selection.packs.Length > 0 && selection.explicit_counter_ids.Length == 0;
				case "explicit":
					return selection.packs.Length == 0 && selection.explicit_counter_ids.Length > 0;
				case "semantic_pack_and_explicit":
					return selection.packs.Length > 0 && selection.explicit_counter_ids.Length > 0;
				default:
					return false;
			}
		}

		private static bool IsSafeRelativeCapturePath(string value)
		{
			if (!IsPathText(value, 1024) ||
				value[0] == '/' ||
				value.IndexOf('\\') >= 0 ||
				value.IndexOf(':') >= 0 ||
				!value.EndsWith(".rdc", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			string[] segments = value.Split('/');
			for (int i = 0; i < segments.Length; i++)
			{
				if (string.IsNullOrEmpty(segments[i]) || segments[i] == "." || segments[i] == "..")
				{
					return false;
				}
			}

			return true;
		}

		private static bool ValidateUnsignedValue(string value, int byteWidth)
		{
			if (!TryParseCanonicalUInt64(value, out ulong parsed))
			{
				return false;
			}

			switch (byteWidth)
			{
				case 1: return parsed <= byte.MaxValue;
				case 2: return parsed <= ushort.MaxValue;
				case 4: return parsed <= uint.MaxValue;
				case 8: return true;
				default: return false;
			}
		}

		private static bool ValidateSignedValue(string value, int byteWidth)
		{
			if (!long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long parsed) ||
				!string.Equals(parsed.ToString(CultureInfo.InvariantCulture), value, StringComparison.Ordinal))
			{
				return false;
			}

			switch (byteWidth)
			{
				case 1: return parsed >= sbyte.MinValue && parsed <= sbyte.MaxValue;
				case 2: return parsed >= short.MinValue && parsed <= short.MaxValue;
				case 4: return parsed >= int.MinValue && parsed <= int.MaxValue;
				case 8: return true;
				default: return false;
			}
		}

		private static bool TryParseCanonicalUInt64(string value, out ulong parsed)
		{
			return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) &&
				string.Equals(parsed.ToString(CultureInfo.InvariantCulture), value, StringComparison.Ordinal);
		}

		private static bool TryParseFiniteFloat(string value)
		{
			return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
				!float.IsNaN(parsed) && !float.IsInfinity(parsed);
		}

		private static bool TryParseFiniteDouble(string value)
		{
			return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
				!double.IsNaN(parsed) && !double.IsInfinity(parsed);
		}

		private static bool TryParseUtc(string value, out DateTimeOffset parsed)
		{
			return DateTimeOffset.TryParseExact(
				value,
				"yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out parsed);
		}

		private static bool IsSha256(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length != 64)
			{
				return false;
			}

			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsToken(string value, int maximumLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length > maximumLength)
			{
				return false;
			}

			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				if (!((character >= 'a' && character <= 'z') ||
					(character >= 'A' && character <= 'Z') ||
					(character >= '0' && character <= '9') ||
					character == '-' || character == '_' || character == '.' || character == ':' || character == '/'))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsBoundedText(string value, int maximumLength, bool allowEmpty)
		{
			if (value == null || value.Length > maximumLength || (!allowEmpty && value.Length == 0))
			{
				return false;
			}

			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				if (char.IsControl(character) && character != '\r' && character != '\n' && character != '\t')
				{
					return false;
				}
			}

			return true;
		}

		private static bool ContainsAbsolutePath(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			if (value.IndexOf("file:/", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			for (int i = 0; i < value.Length; i++)
			{
				if (i + 2 < value.Length &&
					((value[i] >= 'a' && value[i] <= 'z') || (value[i] >= 'A' && value[i] <= 'Z')) &&
					value[i + 1] == ':' &&
					(value[i + 2] == '\\' || value[i + 2] == '/'))
				{
					return true;
				}

				if (i + 1 < value.Length && value[i] == '\\' && value[i + 1] == '\\')
				{
					return true;
				}

				if (value[i] == '/' &&
					(i == 0 || char.IsWhiteSpace(value[i - 1]) || value[i - 1] == '(' || value[i - 1] == '[' ||
					value[i - 1] == '"' || value[i - 1] == '\'' || value[i - 1] == '=') &&
					i + 1 < value.Length && !char.IsWhiteSpace(value[i + 1]) && value[i + 1] != '/')
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsPathText(string value, int maximumLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length > maximumLength)
			{
				return false;
			}

			for (int i = 0; i < value.Length; i++)
			{
				if (char.IsControl(value[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static bool ValidateUniqueTokens(string[] values, int maximumCount, int maximumLength)
		{
			if (values == null || values.Length > maximumCount)
			{
				return false;
			}

			HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < values.Length; i++)
			{
				if (!IsToken(values[i], maximumLength) || !unique.Add(values[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static bool ValidateTokens(string[] values, int maximumCount, int maximumLength)
		{
			if (values == null || values.Length > maximumCount)
			{
				return false;
			}

			for (int i = 0; i < values.Length; i++)
			{
				if (!IsToken(values[i], maximumLength))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsAvailability(string value)
		{
			return value == "not_requested" || value == "unsupported" || value == "available" || value == "fetch_failed" || value == "not_applicable";
		}

		private static bool IsAggregation(string value)
		{
			return value == "sum" || value == "weighted_mean" || value == "mean" || value == "min" || value == "max" || value == "ratio_of_sums" || value == "last" || value == "non_aggregatable";
		}

		private static bool ValidateOutputSize(string json, out string error)
		{
			return ValidateOutputSize(json, MaximumDocumentBytes, out error);
		}

		private static bool ValidateOutputSize(string json, int maximumBytes, out string error)
		{
			if (maximumBytes < 1 || maximumBytes > MaximumDocumentBytes || Encoding.UTF8.GetByteCount(json) > maximumBytes)
			{
				error = "output_too_large";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool ValidateInputSize(string json, int maximumBytes, out string error)
		{
			if (string.IsNullOrWhiteSpace(json) ||
				maximumBytes < 1 || maximumBytes > MaximumDocumentBytes ||
				json.Length > maximumBytes ||
				Encoding.UTF8.GetByteCount(json) > maximumBytes)
			{
				error = "invalid_document_size";
				return false;
			}

			error = string.Empty;
			return true;
		}

		private static bool TryDeserialize<T>(string json, out T document, out string error) where T : class
		{
			return TryDeserialize(json, MaximumDocumentBytes, out document, out error);
		}

		private static bool TryDeserialize<T>(string json, int maximumBytes, out T document, out string error) where T : class
		{
			document = null;
			if (!ValidateInputSize(json, maximumBytes, out error))
			{
				return false;
			}

			try
			{
				document = JsonUtility.FromJson<T>(json);
			}
			catch (Exception)
			{
				error = "invalid_json";
				return false;
			}

			if (document == null)
			{
				error = "invalid_json";
				return false;
			}

			error = string.Empty;
			return true;
		}
	}
}
