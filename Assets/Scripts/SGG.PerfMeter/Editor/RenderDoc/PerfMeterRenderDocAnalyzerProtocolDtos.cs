using System;

namespace SGG.PerfMeter.Editor
{
	[Serializable]
	internal abstract class PerfMeterRenderDocAnalysisDocument
	{
		public string schema;
		public int schema_version;
		public string request_id;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisRequest : PerfMeterRenderDocAnalysisDocument
	{
		public PerfMeterRenderDocAnalysisCaptureRequest capture;
		public PerfMeterRenderDocCounterSelection counter_selection;
		public PerfMeterRenderDocAnalysisOptions options;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisCaptureRequest
	{
		public string artifact_id;
		public string capture_id;
		public string bundle_id;
		public string path;
		public string sha256;
		public string size_bytes;
		public string association_state;
		public string finalization_state;
		public string authority_state;
		public string content_state;
		public string source_file_identity_sha256;
		public string capture_app_api;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocCounterSelection
	{
		public string mode;
		public string[] packs;
		public string[] explicit_counter_ids;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisOptions
	{
		public bool include_action_tree;
		public bool include_raw_counter_descriptions;
		public int timeout_seconds;
		public int max_actions;
		public int max_counter_results;
		public int max_output_bytes;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisResult : PerfMeterRenderDocAnalysisDocument
	{
		public string status;
		public PerfMeterRenderDocAnalysisCaptureResult capture;
		public PerfMeterRenderDocAnalyzerProvenance analyzer;
		public bool actions_complete;
		public int action_total_count;
		public PerfMeterRenderDocAction[] actions;
		public bool counter_catalog_complete;
		public int counter_total_count;
		public PerfMeterRenderDocCounterMetadata[] counter_catalog;
		public bool results_complete;
		public int result_total_count;
		public PerfMeterRenderDocCounterResult[] results;
		public PerfMeterRenderDocAnalysisSummary summary;
		public string[] warnings;
		public string[] diagnostics;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisCaptureResult
	{
		public string artifact_id;
		public string capture_id;
		public string bundle_id;
		public string expected_sha256;
		public string observed_sha256;
		public bool hash_verified;
		public string size_bytes;
		public string association_state;
		public string finalization_state;
		public string authority_state;
		public string content_state;
		public string source_file_identity_sha256;
		public string capture_app_api;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalyzerProvenance
	{
		public string analyzer_version;
		public string renderdoc_build;
		public string replay_api;
		public string graphics_api;
		public string gpu;
		public string driver;
		public string replay_host;
		public string started_utc;
		public string completed_utc;
		public string duration_milliseconds;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAction
	{
		public long event_id;
		public long parent_event_id;
		public string name;
		public string[] flags;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocCounterMetadata
	{
		public string id;
		public long native_id;
		public string name;
		public string description;
		public string unit;
		public string result_type;
		public int result_byte_width;
		public string aggregation;
		public string availability;
		public string reason;
		public bool requested;
		public bool fetched;
		public int pass_index;
		public string provenance;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocCounterResult
	{
		public long event_id;
		public string counter_id;
		public string availability;
		public string reason;
		public PerfMeterRenderDocRawValue raw_value;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocRawValue
	{
		public string result_type;
		public int byte_width;
		public string encoding;
		public string value;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisSummary
	{
		public int requested_counter_count;
		public int described_counter_count;
		public int fetched_counter_count;
		public int unsupported_counter_count;
		public int failed_counter_count;
		public int failed_result_count;
		public int replay_pass_count;
		public string duration_milliseconds;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisError : PerfMeterRenderDocAnalysisDocument
	{
		public string status;
		public PerfMeterRenderDocAnalysisErrorCapture capture;
		public PerfMeterRenderDocAnalyzerProvenance analyzer;
		public PerfMeterRenderDocAnalysisErrorDetail error;
		public string[] warnings;
		public string[] diagnostics;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisErrorCapture
	{
		public string expected_sha256;
		public string observed_sha256;
		public bool hash_verified;
	}

	[Serializable]
	internal sealed class PerfMeterRenderDocAnalysisErrorDetail
	{
		public string code;
		public string stage;
		public string message;
		public bool retryable;
	}
}
