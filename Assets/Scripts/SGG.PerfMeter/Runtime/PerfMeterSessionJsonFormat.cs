namespace SGG.PerfMeter
{
	/// <summary>Explicit session JSON contracts. Capture-samples JSON and CSV are independent.</summary>
	public enum PerfMeterSessionJsonFormat
	{
		/// <summary>Schema 2 with complete custom metric objects in every sample (the default).</summary>
		Full = 0,
		/// <summary>Schema 3 with session-local custom metric descriptor and warning tables.</summary>
		Compact = 1
	}
}
