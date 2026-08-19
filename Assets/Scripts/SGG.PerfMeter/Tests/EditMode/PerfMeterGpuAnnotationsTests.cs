using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace SGG.PerfMeter.Tests.EditMode
{
	public sealed class PerfMeterGpuAnnotationsTests
	{
		[SetUp]
		public void SetUp()
		{
			PerfMeterGpuAnnotationProviderRegistry.Reset();
			PerfMeterGpuAnnotationContextRegistry.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			PerfMeterGpuAnnotationProviderRegistry.Reset();
			PerfMeterGpuAnnotationContextRegistry.Reset();
		}

		[Test]
		public void BatchIsBoundedTypedAndReplacesDuplicateKeys()
		{
			PerfMeterGpuAnnotationBatch batch = new PerfMeterGpuAnnotationBatch(2);
			Assert.That(batch.TryAdd("SGG.Module", "first"), Is.True);
			Assert.That(batch.TryAdd("SGG.Module", "second"), Is.True);
			Assert.That(batch.Count, Is.EqualTo(1));
			Assert.That(batch.GetEntry(0).Value.StringValue, Is.EqualTo("second"));
			Assert.That(batch.TryAdd("SGG.Value", PerfMeterGpuAnnotationValue.Float(1f, 2f, 3f, 4f)), Is.True);
			Assert.That(batch.GetEntry(1).Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.Float));
			Assert.That(batch.GetEntry(1).Value.VectorWidth, Is.EqualTo(4));
			Assert.That(batch.TryAdd("SGG.Overflow", 1u), Is.False);

			batch.Reset();
			Assert.That(batch.Count, Is.Zero);
		}

		[Test]
		public void BatchRejectsMalformedKeysAndOversizedStrings()
		{
			PerfMeterGpuAnnotationBatch batch = new PerfMeterGpuAnnotationBatch();
			Assert.That(batch.TryAdd(null, 1u), Is.False);
			Assert.That(batch.TryAdd(".SGG", 1u), Is.False);
			Assert.That(batch.TryAdd("SGG.", 1u), Is.False);
			Assert.That(batch.TryAdd("SGG..Value", 1u), Is.False);
			Assert.That(batch.TryAdd("SGG Bad", 1u), Is.False);
			Assert.That(batch.TryAdd(new string('K', 128), 1u), Is.False);
			Assert.That(batch.TryAdd("SGG.String", new string('x', 256)), Is.False);
			Assert.That(batch.TryAdd("SGG.String", "valid"), Is.True);
		}

		[Test]
		public void ValueFactoriesCoverEveryScalarAndVectorWidth()
		{
			PerfMeterGpuAnnotationBatch batch = new PerfMeterGpuAnnotationBatch(8);
			Assert.That(batch.TryAdd("SGG.Bool", PerfMeterGpuAnnotationValue.Boolean(true, false, true, false)), Is.True);
			Assert.That(batch.TryAdd("SGG.Int32", PerfMeterGpuAnnotationValue.Int32(-1, 0, 1)), Is.True);
			Assert.That(batch.TryAdd("SGG.UInt32", PerfMeterGpuAnnotationValue.UInt32(1u, 2u)), Is.True);
			Assert.That(batch.TryAdd("SGG.Int64", PerfMeterGpuAnnotationValue.Int64(long.MinValue)), Is.True);
			Assert.That(batch.TryAdd("SGG.UInt64", PerfMeterGpuAnnotationValue.UInt64(ulong.MaxValue)), Is.True);
			Assert.That(batch.TryAdd("SGG.Float", PerfMeterGpuAnnotationValue.Float(-1f, 0f, 1f, 2f)), Is.True);
			Assert.That(batch.TryAdd("SGG.Double", PerfMeterGpuAnnotationValue.Double(-1d, 1d)), Is.True);
			Assert.That(batch.TryAdd("SGG.String", PerfMeterGpuAnnotationValue.String("typed")), Is.True);

			Assert.That(batch.GetEntry(0).Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.Bool));
			Assert.That(batch.GetEntry(0).Value.VectorWidth, Is.EqualTo(4));
			Assert.That(batch.GetEntry(1).Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.Int32));
			Assert.That(batch.GetEntry(1).Value.VectorWidth, Is.EqualTo(3));
			Assert.That(batch.GetEntry(2).Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.UInt32));
			Assert.That(batch.GetEntry(3).Value.Raw0, Is.EqualTo(unchecked((ulong)long.MinValue)));
			Assert.That(batch.GetEntry(4).Value.Raw0, Is.EqualTo(ulong.MaxValue));
			Assert.That(batch.GetEntry(5).Value.VectorWidth, Is.EqualTo(4));
			Assert.That(batch.GetEntry(6).Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.Double));
			Assert.That(batch.GetEntry(7).Value.StringValue, Is.EqualTo("typed"));
		}

		[Test]
		public void ContextGenerationPreventsLateClearAndCrossOwnerKeyCollision()
		{
			PerfMeterGpuAnnotationBatch first = new PerfMeterGpuAnnotationBatch();
			Assert.That(first.TryAdd("SGG.Weather.Command.Sequence", 10uL), Is.True);
			Assert.That(PerfMeterGpuAnnotations.TryPublishContext("weather.main", 1uL, first), Is.True);

			PerfMeterGpuAnnotationBatch replacement = new PerfMeterGpuAnnotationBatch();
			Assert.That(replacement.TryAdd("SGG.Weather.Command.Sequence", 11uL), Is.True);
			Assert.That(PerfMeterGpuAnnotations.TryPublishContext("weather.main", 2uL, replacement), Is.True);
			Assert.That(PerfMeterGpuAnnotations.TryClearContext("weather.main", 1uL), Is.False);

			PerfMeterGpuAnnotationBatch conflicting = new PerfMeterGpuAnnotationBatch();
			Assert.That(conflicting.TryAdd("SGG.Weather.Command.Sequence", 12uL), Is.True);
			Assert.That(PerfMeterGpuAnnotations.TryPublishContext("other.owner", 1uL, conflicting), Is.False);
			Assert.That(PerfMeterGpuAnnotations.TryClearContext("weather.main", 2uL), Is.True);
		}

		[Test]
		public void ScopeMergesAmbientLocalAndSchemaThenClearsEveryOwnedKey()
		{
			FakeProvider provider = new FakeProvider();
			PerfMeterGpuAnnotationProviderRegistry.Register(provider);
			PerfMeterGpuAnnotationBatch context = new PerfMeterGpuAnnotationBatch();
			Assert.That(context.TryAdd("SGG.Weather.Command.Sequence", 42uL), Is.True);
			Assert.That(PerfMeterGpuAnnotations.TryPublishContext("weather.main", 1uL, context), Is.True);

			PerfMeterGpuAnnotationBatch local = new PerfMeterGpuAnnotationBatch();
			Assert.That(local.TryAdd(PerfMeterGpuAnnotationKeys.Module, "com.sungeargames.sky"), Is.True);
			Assert.That(local.TryAdd(PerfMeterGpuAnnotationKeys.RenderGraphPass, "sky.volumetric_clouds.raymarch"), Is.True);
			FakeSink sink = new FakeSink();

			PerfMeterGpuAnnotationScope scope = PerfMeterGpuAnnotations.BeginScope(sink, local);
			Assert.That(scope, Is.Not.Null);
			Assert.That(provider.Created.Count, Is.EqualTo(2));
			Assert.That(sink.Events.Count, Is.EqualTo(1));
			AssertEntry(provider.Created[0], PerfMeterGpuAnnotationKeys.SchemaVersionKey, PerfMeterGpuAnnotationValueType.UInt32, 1uL);
			AssertEntry(provider.Created[0], "SGG.Weather.Command.Sequence", PerfMeterGpuAnnotationValueType.UInt64, 42uL);
			AssertEntry(provider.Created[0], PerfMeterGpuAnnotationKeys.Module, PerfMeterGpuAnnotationValueType.String, 0uL);
			Assert.That(Find(provider.Created[0], PerfMeterGpuAnnotationKeys.Module).Value.StringValue, Is.EqualTo("com.sungeargames.sky"));

			foreach (PerfMeterGpuAnnotationEntry entry in provider.Created[1])
			{
				Assert.That(entry.Value.Type, Is.EqualTo(PerfMeterGpuAnnotationValueType.Empty), entry.Key);
			}

			scope.Dispose();
			Assert.That(sink.Events.Count, Is.EqualTo(2));
			Assert.That(provider.Released.Count, Is.Zero);
		}

		[Test]
		public void FailedEndRecordingReleasesPreparedNativePacket()
		{
			FakeProvider provider = new FakeProvider();
			PerfMeterGpuAnnotationProviderRegistry.Register(provider);
			PerfMeterGpuAnnotationBatch local = new PerfMeterGpuAnnotationBatch();
			Assert.That(local.TryAdd("SGG.Module", "test"), Is.True);
			FakeSink sink = new FakeSink { ThrowOnIssueNumber = 2 };
			PerfMeterGpuAnnotationScope scope = PerfMeterGpuAnnotations.BeginScope(sink, local);

			Assert.That(scope, Is.Not.Null);
			Assert.Throws<InvalidOperationException>(() => scope.Dispose());
			Assert.That(provider.Released, Is.EquivalentTo(new[] { new IntPtr(2) }));
		}

		[Test]
		public void ProviderUnavailableIsAnExplicitNoOpState()
		{
			Assert.That(PerfMeterGpuAnnotations.Capabilities.Availability, Is.EqualTo(PerfMeterGpuAnnotationAvailability.ProviderUnavailable));
			Assert.That(PerfMeterGpuAnnotations.ShouldRecord, Is.False);
		}

		[Test]
		public void InactiveCommandBufferPathAllocatesNoManagedMemoryAfterWarmup()
		{
			PerfMeterGpuAnnotationBatch batch = new PerfMeterGpuAnnotationBatch();
			Assert.That(batch.TryAdd(PerfMeterGpuAnnotationKeys.Module, "test"), Is.True);
			CommandBuffer commandBuffer = new CommandBuffer();
			try
			{
				PerfMeterGpuAnnotations.BeginScope(commandBuffer, batch);
				long before = GC.GetAllocatedBytesForCurrentThread();
				bool unexpectedScope = false;
				for (int index = 0; index < 128; index++)
				{
					unexpectedScope |= PerfMeterGpuAnnotations.BeginScope(commandBuffer, batch) != null;
				}
				long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

				Assert.That(unexpectedScope, Is.False);
				Assert.That(allocated, Is.Zero);
			}
			finally
			{
				commandBuffer.Release();
			}
		}

		[Test]
		public void AnnotationInteropLayoutsAndAvailabilityMappingAreStable()
		{
			Assert.That(Marshal.SizeOf<SggRdAnnotationCapabilitiesV1>(), Is.EqualTo(PerfMeterRenderDocAnnotationAbiV1.CapabilitiesSize));
			Assert.That(Marshal.SizeOf<SggRdAnnotationEntryV1>(), Is.EqualTo(PerfMeterRenderDocAnnotationAbiV1.EntrySize));
			Assert.That(Marshal.OffsetOf<SggRdAnnotationEntryV1>(nameof(SggRdAnnotationEntryV1.ValueData)).ToInt32(), Is.EqualTo(24));
			Assert.That(Marshal.OffsetOf<SggRdAnnotationEntryV1>(nameof(SggRdAnnotationEntryV1.Key)).ToInt32(), Is.EqualTo(56));
			Assert.That(Marshal.OffsetOf<SggRdAnnotationEntryV1>(nameof(SggRdAnnotationEntryV1.StringValue)).ToInt32(), Is.EqualTo(184));

			SggRdAnnotationCapabilitiesV1 ready = new SggRdAnnotationCapabilitiesV1
			{
				AnnotationAbiMajor = 1u,
				SupportsAnnotations = 1u,
				IsCapturing = 1u,
				BackendSupported = 1u,
				EventIdValid = 1u
			};
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.Ok, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.Ready));
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.NotLoaded, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.RenderDocNotLoaded));
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.ExportMissing, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.ApiUnsupported));
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.CaptureInactive, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.CaptureInactive));
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.BackendUnsupported, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.BackendUnsupported));
			Assert.That(PerfMeterRenderDocGpuAnnotationProvider.MapAvailability(SggRdResult.PacketPoolExhausted, ready), Is.EqualTo(PerfMeterGpuAnnotationAvailability.PacketBudgetExceeded));
			Assert.That((uint)SggRdResult.AnnotationsUnavailable, Is.EqualTo(12u));
			Assert.That((uint)SggRdResult.CaptureInactive, Is.EqualTo(13u));
			Assert.That((uint)SggRdResult.BackendUnsupported, Is.EqualTo(14u));
			Assert.That((uint)SggRdResult.PacketPoolExhausted, Is.EqualTo(15u));
			Assert.That((uint)SggRdResult.AnnotationRejected, Is.EqualTo(16u));
		}

#if UNITY_EDITOR_WIN && UNITY_64
		[Test]
		public void OptionalInstalledWindowsBridgeAlwaysReportsADefinedState()
		{
			PerfMeterGpuAnnotationAvailability availability =
				new PerfMeterRenderDocGpuAnnotationProvider().GetCapabilities().Availability;

			Assert.That(Enum.IsDefined(typeof(PerfMeterGpuAnnotationAvailability), availability), Is.True);
		}
#endif

		private static void AssertEntry(
			IReadOnlyList<PerfMeterGpuAnnotationEntry> entries,
			string key,
			PerfMeterGpuAnnotationValueType type,
			ulong raw0)
		{
			PerfMeterGpuAnnotationEntry entry = Find(entries, key);
			Assert.That(entry.Key, Is.EqualTo(key));
			Assert.That(entry.Value.Type, Is.EqualTo(type));
			Assert.That(entry.Value.Raw0, Is.EqualTo(raw0));
		}

		private static PerfMeterGpuAnnotationEntry Find(IReadOnlyList<PerfMeterGpuAnnotationEntry> entries, string key)
		{
			for (int index = 0; index < entries.Count; index++)
			{
				if (string.Equals(entries[index].Key, key, StringComparison.Ordinal))
				{
					return entries[index];
				}
			}
			return default;
		}

		private sealed class FakeProvider : IPerfMeterGpuAnnotationProvider
		{
			internal readonly List<PerfMeterGpuAnnotationEntry[]> Created = new List<PerfMeterGpuAnnotationEntry[]>();
			internal readonly List<IntPtr> Released = new List<IntPtr>();

			public PerfMeterGpuAnnotationCapabilities GetCapabilities()
			{
				return new PerfMeterGpuAnnotationCapabilities(PerfMeterGpuAnnotationAvailability.Ready);
			}

			public bool TryCreateEvent(PerfMeterGpuAnnotationEntry[] entries, int count, out PerfMeterGpuAnnotationPreparedEvent preparedEvent)
			{
				PerfMeterGpuAnnotationEntry[] copy = new PerfMeterGpuAnnotationEntry[count];
				Array.Copy(entries, copy, count);
				Created.Add(copy);
				preparedEvent = new PerfMeterGpuAnnotationPreparedEvent
				{
					Provider = this,
					Callback = new IntPtr(123),
					EventId = 7,
					EventData = new IntPtr(Created.Count)
				};
				return true;
			}

			public void ReleaseEvent(IntPtr eventData)
			{
				Released.Add(eventData);
			}
		}

		private sealed class FakeSink : IPerfMeterGpuAnnotationCommandSink
		{
			internal readonly List<IntPtr> Events = new List<IntPtr>();
			internal int ThrowOnIssueNumber;

			public void Issue(IntPtr callback, int eventId, IntPtr eventData)
			{
				int issueNumber = Events.Count + 1;
				if (ThrowOnIssueNumber == issueNumber)
				{
					throw new InvalidOperationException("synthetic command recording failure");
				}
				Assert.That(callback, Is.Not.EqualTo(IntPtr.Zero));
				Assert.That(eventId, Is.EqualTo(7));
				Events.Add(eventData);
			}
		}
	}
}
