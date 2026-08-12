using NUnit.Framework;
using SGG.PerfMeter.Editor;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterRenderDocAnalyzerProtocolTests
	{
		private const string FixtureRoot = "Tests/EditMode/Fixtures/RenderDocAnalyzer/";
		private const string SchemaRoot = "Editor/RenderDoc/Schemas/";

		[Test]
		public void ProtocolSchemasKeepIndependentStableV1Identities()
		{
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.CurrentSchemaVersion, Is.EqualTo(1));
			AssertSchema("renderdoc-analysis-request-v1.schema.json", PerfMeterRenderDocAnalyzerProtocol.RequestSchema);
			AssertSchema("renderdoc-analysis-result-v1.schema.json", PerfMeterRenderDocAnalyzerProtocol.ResultSchema);
			AssertSchema("renderdoc-analysis-error-v1.schema.json", PerfMeterRenderDocAnalyzerProtocol.ErrorSchema);
		}

		[Test]
		public void GoldenRequestResultAndErrorRoundTrip()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string requestError = string.Empty;
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(request, out string requestJson, out requestError), Is.True, requestError);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(requestJson, out _, out requestError), Is.True, requestError);

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, ReadFixture("result-v1.json"), out PerfMeterRenderDocAnalysisResult result, out string resultError), Is.True, resultError);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(request, result, out string resultJson, out resultError), Is.True, resultError);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, resultJson, out _, out resultError), Is.True, resultError);

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadError(request, ReadFixture("error-v1.json"), out PerfMeterRenderDocAnalysisError failure, out string failureError), Is.True, failureError);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(request, failure, out string failureJson, out failureError), Is.True, failureError);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadError(request, failureJson, out _, out failureError), Is.True, failureError);
		}

		[Test]
		public void UInt64MaxValueAndAvailableZeroRoundTripExactly()
		{
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(ReadRequest(), ReadFixture("result-v1.json"), out PerfMeterRenderDocAnalysisResult result, out string error), Is.True, error);

			Assert.That(result.capture.size_bytes, Is.EqualTo("536870912"));
			Assert.That(result.results[0].raw_value.value, Is.EqualTo("18446744073709551615"));
			Assert.That(result.results[2].availability, Is.EqualTo("available"));
			Assert.That(result.results[2].raw_value.value, Is.EqualTo("0"));

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(ReadRequest(), result, out string json, out error), Is.True, error);
			Assert.That(json, Does.Contain("\"value\":\"18446744073709551615\""));
			Assert.That(json, Does.Contain("\"value\":\"0\""));
			Assert.That(json, Does.Not.Contain("1.8446744073709552E"));
		}

		[Test]
		public void UnknownUnitAndResultTypeRoundTripWithoutNormalization()
		{
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(ReadRequest(), ReadFixture("result-v1.json"), out PerfMeterRenderDocAnalysisResult result, out string error), Is.True, error);

			Assert.That(result.counter_catalog[1].unit, Is.EqualTo("VendorFutureUnit"));
			Assert.That(result.counter_catalog[1].result_type, Is.EqualTo("VendorFutureType"));
			Assert.That(result.results[1].raw_value.encoding, Is.EqualTo("opaque_hex"));

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(ReadRequest(), result, out string json, out error), Is.True, error);
			Assert.That(json, Does.Contain("\"unit\":\"VendorFutureUnit\""));
			Assert.That(json, Does.Contain("\"result_type\":\"VendorFutureType\""));
			Assert.That(json, Does.Contain("\"encoding\":\"opaque_hex\""));
		}

		[Test]
		public void AvailabilityDistinctionsRemainExplicit()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, ReadFixture("result-v1.json"), out PerfMeterRenderDocAnalysisResult result, out string error), Is.True, error);

			Assert.That(result.counter_catalog[4].availability, Is.EqualTo("not_requested"));
			Assert.That(result.counter_catalog[5].availability, Is.EqualTo("unsupported"));
			Assert.That(result.counter_catalog[6].availability, Is.EqualTo("fetch_failed"));
			Assert.That(result.counter_catalog[7].availability, Is.EqualTo("not_applicable"));
			Assert.That(result.results[4].raw_value, Is.Null);
			Assert.That(result.summary.fetched_counter_count, Is.EqualTo(4));

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(request, result, out string json, out error), Is.True, error);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, json, out PerfMeterRenderDocAnalysisResult roundTrip, out error), Is.True, error);
			Assert.That(roundTrip.results[4].raw_value, Is.Null);
		}

		[Test]
		public void AdditiveUnknownFieldsAreAcceptedButFutureSchemaFailsClosed()
		{
			string request = ReadFixture("request-v1.json");
			string additive = request.Replace(
				"\"schema_version\": 1,",
				"\"schema_version\": 1, \"future_field\": { \"nested\": true },");
			string future = request.Replace("\"schema_version\": 1", "\"schema_version\": 99");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(additive, out _, out string error), Is.True, error);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(future, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("unsupported_schema_version"));
		}

		[Test]
		public void RequiredBooleanPresenceIsEnforced()
		{
			string request = ReadFixture("request-v1.json");
			string missingRequestBoolean = request.Replace("    \"include_action_tree\": true,\n", string.Empty);
			string duplicateRequestBoolean = request.Replace(
				"    \"include_action_tree\": true,",
				"    \"include_action_tree\": true, \"include_action_tree\": false,");
			PerfMeterRenderDocAnalysisRequest parsedRequest = ReadRequest();
			string failure = ReadFixture("error-v1.json");
			string missingErrorBoolean = failure.Replace("    \"retryable\": false\n", string.Empty);

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(missingRequestBoolean, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_boolean"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(duplicateRequestBoolean, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_boolean"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadError(parsedRequest, missingErrorBoolean, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_boolean"));
		}

		[Test]
		public void RequiredFieldsMustAppearAtCanonicalPathsAndOnce()
		{
			string request = ReadFixture("request-v1.json");
			string movedBoolean = request.Replace("    \"include_action_tree\": true,\n", string.Empty).Replace(
				"\"schema_version\": 1,",
				"\"schema_version\": 1, \"include_action_tree\": true,");
			string result = ReadFixture("result-v1.json");
			string missingRequested = result.Replace(" \"requested\": false,", string.Empty);
			string nullRawValue = result.Replace(
				"\"availability\": \"unsupported\", \"reason\": \"unsupported on gpu\"",
				"\"availability\": \"unsupported\", \"reason\": \"unsupported on gpu\", \"raw_value\": null");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(movedBoolean, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_boolean"));
			PerfMeterRenderDocAnalysisRequest parsedRequest = ReadRequest();
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(parsedRequest, missingRequested, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_field"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(parsedRequest, nullRawValue, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_field"));
		}

		[Test]
		public void EscapedKnownPropertyNamesAreCanonicalAndCannotDuplicate()
		{
			string request = ReadFixture("request-v1.json");
			string escapedOnly = request.Replace("include_action_tree", "include_\\u0061ction_tree");
			string escapedDuplicate = request.Replace(
				"\"include_action_tree\": true,",
				"\"include_action_tree\": true, \"include_\\u0061ction_tree\": false,");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(escapedOnly, out _, out string error), Is.True, error);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(escapedDuplicate, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("missing_required_boolean"));
		}

		[Test]
		public void InvalidTreePathHashAndKnownNumericValuesFailClosed()
		{
			string requestJson = ReadFixture("request-v1.json");
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string traversal = requestJson.Replace("Temp/PerfMeter/RenderDocCopies/golden/capture.rdc", "../capture.rdc");
			string result = ReadFixture("result-v1.json");
			string badParent = result.Replace("\"parent_event_id\": 1", "\"parent_event_id\": 999");
			string selfParent = result.Replace("\"parent_event_id\": 1", "\"parent_event_id\": 42");
			string hashMismatch = result.Replace(
				"\"observed_sha256\": \"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"",
				"\"observed_sha256\": \"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc\"");
			string invalidUInt64 = result.Replace(
				"\"value\": \"18446744073709551615\"",
				"\"value\": \"18446744073709551616\"");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(traversal, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_capture"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, badParent, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_action_tree"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, selfParent, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_action_tree"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, hashMismatch, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_capture"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, invalidUInt64, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_counter_result"));
		}

		[Test]
		public void SchemasForbidNumericUInt64AndReserveEmptyDiagnostics()
		{
			string requestSchema = ReadSchema("renderdoc-analysis-request-v1.schema.json");
			string resultSchema = ReadSchema("renderdoc-analysis-result-v1.schema.json");
			string errorSchema = ReadSchema("renderdoc-analysis-error-v1.schema.json");

			Assert.That(requestSchema, Does.Contain("\"uint64\": { \"type\": \"string\""));
			Assert.That(resultSchema, Does.Contain("\"uint64\": { \"type\": \"string\""));
			Assert.That(errorSchema, Does.Contain("\"uint64\": { \"type\": \"string\""));
			Assert.That(resultSchema, Does.Contain("\"diagnostics\": { \"type\": \"array\", \"maxItems\": 0 }"));
			Assert.That(errorSchema, Does.Contain("\"diagnostics\": { \"type\": \"array\", \"maxItems\": 0 }"));
		}

		[Test]
		public void CaptureAdmissionIsBoundedToNativeRenderDocArtifactLimit()
		{
			string accepted = ReadFixture("request-v1.json");
			string overflow = accepted.Replace("\"size_bytes\": \"536870912\"", "\"size_bytes\": \"536870913\"");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(accepted, out _, out string error), Is.True, error);
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(overflow, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_capture"));
		}

		[Test]
		public void ResponsesAreBoundToRequestIdentityHashApiAndLimits()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string result = ReadFixture("result-v1.json");
			string staleRequest = result.Replace("rd-analysis-golden", "rd-analysis-stale");
			string staleIdentity = result.Replace(
				"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
				"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc");
			string wrongApi = result.Replace("\"capture_app_api\": \"1.7.0\"", "\"capture_app_api\": \"1.6.0\"");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, staleRequest, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, staleIdentity, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, wrongApi, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));

			request.options.max_actions = 1;
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, result, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));

			request = ReadRequest();
			request.options.max_output_bytes = 1024;
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, result, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_document_size"));
		}

		[Test]
		public void ActionTreeAndDescriptionOptionsBindResultContents()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string result = ReadFixture("result-v1.json");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, result, out PerfMeterRenderDocAnalysisResult parsed, out string error), Is.True, error);
			parsed.actions = System.Array.Empty<PerfMeterRenderDocAction>();
			parsed.action_total_count = 0;

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryWrite(request, parsed, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));

			request.options.include_raw_counter_descriptions = false;
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, result, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("response_binding_mismatch"));
		}

		[Test]
		public void ActualReplayApiIsIndependentFromCapturedAppApi()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string newerReplay = ReadFixture("result-v1.json").Replace("\"replay_api\": \"1.7.0\"", "\"replay_api\": \"1.8.0\"");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, newerReplay, out PerfMeterRenderDocAnalysisResult result, out string error), Is.True, error);
			Assert.That(result.capture.capture_app_api, Is.EqualTo("1.7.0"));
			Assert.That(result.analyzer.replay_api, Is.EqualTo("1.8.0"));
		}

		[Test]
		public void FloatUsesRenderDocByteWidthAndRejectsNonFiniteOrUnknownWidths()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string result = ReadFixture("result-v1.json");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, result, out PerfMeterRenderDocAnalysisResult parsed, out string error), Is.True, error);
			Assert.That(parsed.results[3].raw_value.result_type, Is.EqualTo("Float"));
			Assert.That(parsed.results[3].raw_value.byte_width, Is.EqualTo(8));

			string nonFinite = result.Replace("\"value\": \"0.00042\"", "\"value\": \"NaN\"");
			string unknownWidth = result.Replace("\"byte_width\": 8, \"encoding\": \"decimal\", \"value\": \"0.00042\"", "\"byte_width\": 16, \"encoding\": \"decimal\", \"value\": \"0.00042\"");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, nonFinite, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_counter_result"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, unknownWidth, out _, out error), Is.False);
		}

		[Test]
		public void CompletedResultsRequireFullSectionsAndConsistentPassProvenance()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string result = ReadFixture("result-v1.json");
			string truncated = result.Replace("\"actions_complete\": true", "\"actions_complete\": false");
			string badPass = result.Replace("\"replay_pass_count\": 2", "\"replay_pass_count\": 1");
			string badStatus = result.Replace("\"status\": \"completed_with_errors\"", "\"status\": \"completed\"");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, truncated, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("incomplete_action_tree"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, badPass, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_counter_pass"));
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, badStatus, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("inconsistent_result_status"));

			string eventFailureWithoutSummary = result.Replace(
				"\"counter_id\": \"renderdoc:zero\", \"availability\": \"available\", \"reason\": \"\", \"raw_value\": { \"result_type\": \"UInt\", \"byte_width\": 8, \"encoding\": \"decimal\", \"value\": \"0\" }",
				"\"counter_id\": \"renderdoc:zero\", \"availability\": \"fetch_failed\", \"reason\": \"event failed\"");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, eventFailureWithoutSummary, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("inconsistent_result_status"));
		}

		[Test]
		public void AnalyzerMessagesRejectAbsoluteHostPaths()
		{
			PerfMeterRenderDocAnalysisRequest request = ReadRequest();
			string failure = ReadFixture("error-v1.json").Replace(
				"Observed capture hash does not match the request.",
				"Capture C:\\\\Users\\\\roman\\\\capture.rdc failed.");

			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadError(request, failure, out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_error"));

			string quotedPosix = ReadFixture("error-v1.json").Replace(
				"Observed capture hash does not match the request.",
				"Capture at '/home/user/capture.rdc' failed.");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadError(request, quotedPosix, out _, out error), Is.False);

			string provenancePath = ReadFixture("result-v1.json").Replace("\"replay_host\": \"windows-x64\"", "\"replay_host\": \"file:///home/user/tool\"");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, provenancePath, out _, out error), Is.False);
			Assert.That(error, Is.EqualTo("invalid_analyzer_provenance"));

			string resultReasonPath = ReadFixture("result-v1.json").Replace(
				"\"reason\": \"counter pass failed\"",
				"\"reason\": \"file:/home/user/counter.log\"");
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadResult(request, resultReasonPath, out _, out error), Is.False);
		}

		private static void AssertSchema(string fileName, string identity)
		{
			string schema = ReadSchema(fileName);
			Assert.That(schema, Does.Contain("\"$schema\": \"https://json-schema.org/draft/2020-12/schema\""));
			Assert.That(schema, Does.Contain("\"const\": \"" + identity + "\""));
			Assert.That(schema, Does.Contain("\"schema_version\": { \"const\": 1 }"));
			Assert.That(schema, Does.Contain("\"additionalProperties\": true"));
		}

		private static string ReadFixture(string fileName)
		{
			return PerfMeterTestAssets.ReadRenderDocAnalyzerAsset(FixtureRoot + fileName);
		}

		private static string ReadSchema(string fileName)
		{
			return PerfMeterTestAssets.ReadRenderDocAnalyzerAsset(SchemaRoot + fileName);
		}

		private static PerfMeterRenderDocAnalysisRequest ReadRequest()
		{
			Assert.That(PerfMeterRenderDocAnalyzerProtocol.TryReadRequest(ReadFixture("request-v1.json"), out PerfMeterRenderDocAnalysisRequest request, out string error), Is.True, error);
			return request;
		}
	}
}
