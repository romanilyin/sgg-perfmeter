#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterSessionStreamExportTests
	{
		private static PerfMeterSessionSampleSnapshot[] Samples()
		{
			string unicode = new string('x', 4095) + "\U0001D11E Кириллица 漢字 \"quote\"\r\n\ud800";
			return new[] { new PerfMeterSessionSampleSnapshot(17, 1.125d, unicode, default,
				new[] { new PerfMeterCustomMetricSnapshot("unicode.id", unicode, "GPU", "ms", 1.125d, false, "n/a\n" + unicode) }) };
		}

		[TestCase(PerfMeterSessionJsonFormat.Full, "en-US")]
		[TestCase(PerfMeterSessionJsonFormat.Full, "fr-FR")]
		[TestCase(PerfMeterSessionJsonFormat.Compact, "en-US")]
		[TestCase(PerfMeterSessionJsonFormat.Compact, "ja-JP")]
		public void JsonStreamMatchesExistingPathBytesWithUnicodeAndCulture(PerfMeterSessionJsonFormat format, string culture)
		{
			WithCulture(culture, () => WithPath(path =>
			{
				var samples = Samples();
				Assert.That(PerfMeterSessionExporter.ExportJson(path, PerfMeterSessionSummarySnapshot.Empty, samples,
					PerfMeterSessionTimelineSnapshot.Empty, default, true, PerfMeterSessionExporter.RuntimePackageIdentity,
					PerfMeterSelfOverheadWindowSnapshot.Unavailable, format).Success, Is.True);
				using (var destination = new NonSeekingStream())
				{
					Assert.That(WriteJson(destination, samples, format), Is.True);
					Assert.That(destination.Bytes, Is.EqualTo(File.ReadAllBytes(path)));
					Assert.That(destination.Disposed, Is.False);
					Assert.That(destination.Bytes[0], Is.EqualTo((byte)'{'), "No UTF-8 BOM.");
				}
			}));
		}

		[TestCase("en-US")]
		[TestCase("fr-FR")]
		public void CsvStreamMatchesExistingPathBytesAndNewlines(string culture)
		{
			WithCulture(culture, () => WithPath(path =>
			{
				var samples = Samples();
				Assert.That(PerfMeterSessionExporter.ExportCsv(path, PerfMeterSessionSummarySnapshot.Empty, samples, default, true).Success, Is.True);
				using (var destination = new NonSeekingStream())
				{
					Assert.That(PerfMeterSessionExporter.WriteCsv(destination, PerfMeterSessionSummarySnapshot.Empty, samples, default), Is.True);
					Assert.That(destination.Bytes, Is.EqualTo(File.ReadAllBytes(path)));
					Assert.That(destination.Disposed, Is.False);
				}
			}));
		}

		[TestCase(0)] [TestCase(1)] [TestCase(2)]
		public void CallerOwnedNonSeekingGzipRoundTripsExactFullCompactAndCsvBytes(int kind)
		{
			var samples = Samples();
			byte[] expected = Encoding.UTF8.GetBytes(kind == 2
				? PerfMeterSessionExporter.BuildCsv(PerfMeterSessionSummarySnapshot.Empty, samples, default)
				: PerfMeterSessionExporter.BuildJson(PerfMeterSessionSummarySnapshot.Empty, samples, default, (PerfMeterSessionJsonFormat)kind));
			using (var destination = new NonSeekingStream())
			{
				using (var gzip = new GZipStream(destination, CompressionLevel.Fastest, true))
					Assert.That(kind == 2 ? PerfMeterSessionExporter.WriteCsv(gzip, PerfMeterSessionSummarySnapshot.Empty, samples, default)
						: WriteJson(gzip, samples, (PerfMeterSessionJsonFormat)kind), Is.True);
				Assert.That(destination.Disposed, Is.False, "SDK and compressor both leave caller stream open.");
				using (var source = new MemoryStream(destination.Bytes))
				using (var gzip = new GZipStream(source, CompressionMode.Decompress))
				using (var decoded = new MemoryStream())
				{
					gzip.CopyTo(decoded);
					Assert.That(decoded.ToArray(), Is.EqualTo(expected));
				}
			}
		}

		[TestCase(false)] [TestCase(true)]
		public void FailedWriterReturnsFalseLeavesPartialOutputAndDestinationOwned(bool csv)
		{
			using (var destination = new NonSeekingStream { FailAfterBytes = 17 })
			{
				Assert.That(csv ? PerfMeterSessionExporter.WriteCsv(destination, PerfMeterSessionSummarySnapshot.Empty, Samples(), default)
					: WriteJson(destination, Samples(), PerfMeterSessionJsonFormat.Compact), Is.False);
				Assert.That(destination.Bytes.Length, Is.EqualTo(17));
				Assert.That(destination.Disposed, Is.False);
			}
		}

		[TestCase(false)] [TestCase(true)]
		public void FlushFailureCannotReportSuccessOrCloseCallerStream(bool csv)
		{
			using (var destination = new NonSeekingStream { FailFlush = true })
			{
				Assert.That(csv ? PerfMeterSessionExporter.WriteCsv(destination, PerfMeterSessionSummarySnapshot.Empty, Samples(), default)
					: WriteJson(destination, Samples(), PerfMeterSessionJsonFormat.Full), Is.False);
				Assert.That(destination.Bytes.Length, Is.GreaterThan(0));
				Assert.That(destination.Disposed, Is.False);
			}
		}

		[Test]
		public void InvalidDestinationAndFormatRejectWithoutTakingOwnership()
		{
			Assert.Throws<ArgumentNullException>(() => PerformanceMeter.WriteSessionJson(null));
			Assert.Throws<ArgumentNullException>(() => PerformanceMeter.WriteSessionCsv(null));
			using (var destination = new NonSeekingStream { Writable = false })
			{
				Assert.Throws<ArgumentException>(() => PerformanceMeter.WriteSessionJson(destination));
				Assert.Throws<ArgumentException>(() => PerformanceMeter.WriteSessionCsv(destination));
				Assert.That(destination.Bytes, Is.Empty);
				Assert.That(destination.Disposed, Is.False);
			}
			using (var destination = new NonSeekingStream())
			{
				Assert.Throws<ArgumentOutOfRangeException>(() => WriteJson(destination, Samples(), (PerfMeterSessionJsonFormat)99));
				Assert.That(destination.Bytes, Is.Empty);
				Assert.That(destination.Disposed, Is.False);
			}
		}

		[Test]
		public void PublicFacadeUsesDefaultFullExplicitCompactAndCsvWithoutClosingStreams()
		{
			using (var full = new NonSeekingStream())
			using (var compact = new NonSeekingStream())
			using (var csv = new NonSeekingStream())
			{
				Assert.That(PerformanceMeter.WriteSessionJson(full), Is.True);
				Assert.That(PerformanceMeter.WriteSessionJson(compact, PerfMeterSessionJsonFormat.Compact), Is.True);
				Assert.That(PerformanceMeter.WriteSessionCsv(csv), Is.True);
				Assert.That(Encoding.UTF8.GetString(full.Bytes), Does.StartWith("{\"schema_version\":2"));
				Assert.That(Encoding.UTF8.GetString(compact.Bytes), Does.StartWith("{\"schema_version\":3"));
				Assert.That(csv.Bytes.Length, Is.GreaterThan(0));
				Assert.That(full.Disposed || compact.Disposed || csv.Disposed, Is.False);
			}
		}

		[Test]
		public void OccupiedParityPathPreservesForeignBytesAndNeverInvokesAction()
		{
			string path = Path.Combine(Path.GetTempPath(), "perfmeter-stream-foreign-" + Guid.NewGuid().ToString("N") + ".dat");
			byte[] original = Encoding.UTF8.GetBytes("foreign sentinel Кириллица");
			using (var claim = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) claim.Write(original, 0, original.Length);
			try {
				bool invoked = false;
				Assert.Throws<IOException>(() => WithOwnedPath(path, _ => invoked = true));
				Assert.That(invoked, Is.False); Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
			} finally { File.Delete(path); } // This control owns the original sentinel through CreateNew above.
		}

		private static bool WriteJson(Stream destination, PerfMeterSessionSampleSnapshot[] samples, PerfMeterSessionJsonFormat format)
			=> PerfMeterSessionExporter.WriteJson(destination, PerfMeterSessionSummarySnapshot.Empty, samples,
				PerfMeterSessionTimelineSnapshot.Empty, default, PerfMeterSessionExporter.RuntimePackageIdentity,
				PerfMeterSelfOverheadWindowSnapshot.Unavailable, format);

		private static void WithCulture(string name, Action action)
		{
			CultureInfo previous = CultureInfo.CurrentCulture;
			try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name); action(); }
			finally { CultureInfo.CurrentCulture = previous; }
		}

		private static void WithPath(Action<string> action)
			=> WithOwnedPath(Path.Combine(Path.GetTempPath(), "perfmeter-stream-" + Guid.NewGuid().ToString("N") + ".dat"), action);

		private static void WithOwnedPath(string path, Action<string> action)
		{
			// Admission is before try: an occupied path is foreign and must never reach cleanup.
			using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
			try { action(path); }
			finally { if (File.Exists(path)) File.Delete(path); }
		}

		private sealed class NonSeekingStream : Stream
		{
			private readonly MemoryStream data = new MemoryStream();
			internal bool Disposed, FailFlush, Writable = true;
			internal int FailAfterBytes = int.MaxValue;
			internal byte[] Bytes => data.ToArray();
			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => Writable && !Disposed;
			public override long Length => throw new NotSupportedException();
			public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
			public override void Flush() { if (FailFlush) throw new IOException("Injected flush failure."); }
			public override void Write(byte[] buffer, int offset, int count)
			{
				int permitted = Math.Min(count, Math.Max(0, FailAfterBytes - (int)data.Length));
				data.Write(buffer, offset, permitted);
				if (permitted != count) throw new IOException("Injected partial writer failure.");
			}
			protected override void Dispose(bool disposing) { Disposed = true; if (disposing) data.Dispose(); base.Dispose(disposing); }
			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
		}
	}
}
#endif
