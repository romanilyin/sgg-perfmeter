import os
import sys
import unittest


ANALYZER_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if ANALYZER_ROOT not in sys.path:
    sys.path.insert(0, ANALYZER_ROOT)

import perfmeter_renderdoc_analyzer as analyzer


class FakeDescription:
    def __init__(self, native_id):
        self.name = "Counter %d" % native_id
        self.description = "Synthetic counter"
        self.resultType = "CompType.UInt"
        self.resultByteWidth = 8
        self.unit = "CounterUnit.Absolute"


class FakeValue:
    def __init__(self, value):
        self.u32 = value
        self.u64 = value
        self.f = float(value)
        self.d = float(value)


class FakeResult:
    def __init__(self, native_id, event_id=42):
        self.counter = native_id
        self.eventId = event_id
        self.value = FakeValue(native_id)


class FakeController:
    def __init__(self, available, failed_passes=(), returned_counters=None, fatal_status=None):
        self.available = list(available)
        self.failed_passes = set(tuple(values) for values in failed_passes)
        self.returned_counters = dict((tuple(key), tuple(value)) for key, value in (returned_counters or {}).items())
        self.fatal_status = fatal_status
        self.calls = []

    def EnumerateCounters(self):
        return list(reversed(self.available))

    def DescribeCounter(self, native_id):
        return FakeDescription(native_id)

    def FetchCounters(self, native_ids):
        key = tuple(int(value) for value in native_ids)
        self.calls.append(key)
        if key in self.failed_passes:
            raise RuntimeError("synthetic pass failure")
        return [FakeResult(native_id) for native_id in self.returned_counters.get(key, key)]

    def GetFatalErrorStatus(self):
        return self.fatal_status


def explicit_selection(native_ids):
    return {
        "mode": "explicit",
        "packs": [],
        "explicit_counter_ids": ["renderdoc:%d" % native_id for native_id in native_ids],
    }


def read_counters(controller, selection, vendor="nVidia", graphics_api="D3D12"):
    return analyzer.read_counters(
        controller,
        selection,
        True,
        1000000,
        analyzer.MAX_DOCUMENT_BYTES,
        {42},
        vendor,
        graphics_api,
    )


class CounterPlannerTests(unittest.TestCase):
    def test_pack_expansion_filters_vendor_and_api_without_substitution(self):
        selection = {
            "mode": "semantic_pack_and_explicit",
            "packs": ["nvidia_basic", "vulkan_extended"],
            "explicit_counter_ids": ["renderdoc:3000002", "renderdoc:7000001"],
        }

        first = analyzer.plan_counter_selection(selection, [7000001, 4000007, 3000002], "AMD", "D3D12")
        second = analyzer.plan_counter_selection(selection, [3000002, 7000001, 4000007], "AMD", "D3D12")

        self.assertEqual(first, second)
        self.assertEqual(first["requested"], (3000002, 4000007, 7000001))
        self.assertEqual(first["selected"], (7000001,))
        self.assertEqual(set(first["not_applicable"]), {3000002, 4000007})
        self.assertEqual(first["batches"], (("explicit", (7000001,)),))

    def test_equivalent_explicit_sets_have_the_same_plan_and_arm_allows_opengl(self):
        first = analyzer.plan_counter_selection(explicit_selection([1, 3000001, 2]), [1, 2, 3000001], "nVidia", "D3D12")
        second = analyzer.plan_counter_selection(explicit_selection([2, 1, 3000001]), [3000001, 2, 1], "nVidia", "D3D12")
        arm = analyzer.plan_counter_selection(
            {"mode": "semantic_pack", "packs": ["arm_basic"], "explicit_counter_ids": []},
            [5000001],
            "ARM",
            "OpenGL",
        )

        self.assertEqual(first, second)
        self.assertEqual(first["batches"], (("generic", (1, 2)), ("nvidia", (3000001,))))
        self.assertEqual(arm["selected"], (5000001,))

    def test_large_explicit_selection_is_batched_and_reports_passes(self):
        native_ids = list(range(7000001, 7000019))
        controller = FakeController(native_ids)

        catalog, results, summary, warnings, _ = read_counters(controller, explicit_selection(native_ids))

        self.assertEqual(controller.calls, [tuple(native_ids[:16]), tuple(native_ids[16:])])
        self.assertEqual(summary["replay_pass_count"], 2)
        self.assertEqual(summary["requested_counter_count"], 18)
        self.assertEqual(summary["fetched_counter_count"], 18)
        self.assertEqual([item["pass_index"] for item in catalog], ([0] * 16) + ([1] * 2))
        self.assertEqual(len(results), 18)
        self.assertEqual(warnings, [])

    def test_failed_generic_batch_retries_each_counter_and_keeps_per_counter_error(self):
        controller = FakeController([1, 2], failed_passes=((1, 2), (2,)))

        catalog, results, summary, warnings, _ = read_counters(controller, explicit_selection([1, 2]))
        by_id = {item["native_id"]: item for item in catalog}

        self.assertEqual(controller.calls, [(1, 2), (1,), (2,)])
        self.assertTrue(by_id[1]["fetched"])
        self.assertEqual(by_id[1]["pass_index"], 1)
        self.assertEqual(by_id[2]["availability"], "fetch_failed")
        self.assertEqual(by_id[2]["reason"], "counter retry failed")
        self.assertEqual(by_id[2]["pass_index"], 2)
        self.assertEqual(summary["replay_pass_count"], 3)
        self.assertEqual(summary["failed_counter_count"], 1)
        self.assertEqual(len(results), 1)
        self.assertEqual(len(warnings), 1)

    def test_partial_nonthrowing_batch_retries_missing_generic_counter(self):
        controller = FakeController(
            [1, 2],
            returned_counters={(1, 2): (1,), (2,): ()},
        )

        catalog, results, summary, warnings, _ = read_counters(controller, explicit_selection([1, 2]))
        by_id = {item["native_id"]: item for item in catalog}

        self.assertEqual(controller.calls, [(1, 2), (2,)])
        self.assertTrue(by_id[1]["fetched"])
        self.assertEqual(by_id[2]["availability"], "fetch_failed")
        self.assertEqual(by_id[2]["reason"], "counter retry returned no results")
        self.assertEqual(summary["replay_pass_count"], 2)
        self.assertEqual(summary["failed_counter_count"], 1)
        self.assertEqual(len(results), 1)
        self.assertEqual(len(warnings), 1)

    def test_vendor_batch_failure_is_not_retried(self):
        native_ids = [3000001, 3000002]
        controller = FakeController(native_ids, failed_passes=(tuple(native_ids),))

        catalog, results, summary, warnings, _ = read_counters(controller, explicit_selection(native_ids))

        self.assertEqual(controller.calls, [tuple(native_ids)])
        self.assertEqual(summary["replay_pass_count"], 1)
        self.assertEqual(summary["failed_counter_count"], 2)
        self.assertEqual(results, [])
        self.assertTrue(all(item["availability"] == "fetch_failed" for item in catalog))
        self.assertTrue(all(item["reason"] == "counter batch failed; retry is not safe" for item in catalog))
        self.assertEqual(len(warnings), 1)

    def test_fatal_replay_status_fails_analysis(self):
        controller = FakeController([1], fatal_status="failed")

        with self.assertRaises(analyzer.AnalyzerFailure) as failure:
            analyzer.read_counters(
                controller,
                explicit_selection([1]),
                True,
                100,
                analyzer.MAX_DOCUMENT_BYTES,
                {42},
                "nVidia",
                "D3D12",
                succeeded_status="succeeded",
            )

        self.assertEqual(failure.exception.code, "replay_failed")
        self.assertEqual(controller.calls, [(1,)])

    def test_explicit_missing_counter_stays_unsupported_and_available_peer_is_not_requested(self):
        controller = FakeController([20, 21])

        catalog, _, summary, _, _ = read_counters(controller, explicit_selection([20, 22]))
        by_id = {item["native_id"]: item for item in catalog}

        self.assertTrue(by_id[20]["requested"])
        self.assertTrue(by_id[20]["fetched"])
        self.assertEqual(by_id[21]["availability"], "not_requested")
        self.assertFalse(by_id[21]["requested"])
        self.assertEqual(by_id[22]["availability"], "unsupported")
        self.assertTrue(by_id[22]["requested"])
        self.assertFalse(by_id[22]["fetched"])
        self.assertEqual(summary["requested_counter_count"], 2)
        self.assertEqual(controller.calls, [(20,)])

    def test_result_limit_stops_before_later_counter_passes(self):
        native_ids = list(range(7000001, 7000019))
        controller = FakeController(native_ids)

        with self.assertRaises(analyzer.AnalyzerFailure) as failure:
            analyzer.read_counters(
                controller,
                explicit_selection(native_ids),
                True,
                1,
                analyzer.MAX_DOCUMENT_BYTES,
                {42},
                "nVidia",
                "D3D12",
            )

        self.assertEqual(failure.exception.code, "counter_result_limit_exceeded")
        self.assertEqual(controller.calls, [tuple(native_ids[:16])])

        controller = FakeController(native_ids)
        with self.assertRaises(analyzer.AnalyzerFailure) as exact_failure:
            analyzer.read_counters(
                controller,
                explicit_selection(native_ids),
                True,
                16,
                analyzer.MAX_DOCUMENT_BYTES,
                {42},
                "nVidia",
                "D3D12",
            )

        self.assertEqual(exact_failure.exception.code, "counter_result_limit_exceeded")
        self.assertEqual(controller.calls, [tuple(native_ids[:16])])


if __name__ == "__main__":
    unittest.main()
