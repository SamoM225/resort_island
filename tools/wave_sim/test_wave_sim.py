import contextlib
import copy
import io
import json
import tempfile
import unittest
from pathlib import Path

import wave_sim

FIXTURE = Path(__file__).resolve().parents[2] / "frontend" / "Assets" / "Tests" / "EditMode" / "Fixtures" / "configs_response.json"


def fixture_configs():
    return wave_sim.load_configs(FIXTURE)


def write_config_directory(configs, directory):
    for key, data in configs.items():
        (Path(directory) / f"{key}.json").write_text(json.dumps({"version": 1, "data": data}), encoding="utf-8")


def run_cli(configs):
    with tempfile.TemporaryDirectory() as directory:
        write_config_directory(configs, directory)
        with contextlib.redirect_stdout(io.StringIO()):
            return wave_sim.main(["--configs", directory])


class WaveRulesTests(unittest.TestCase):
    def setUp(self):
        self.rules = wave_sim.WaveRules(fixture_configs()["wave_system"])

    def test_contribution_falls_linearly_from_grace_to_unit_floor(self):
        self.assertEqual(1.0, self.rules.contribution(0))
        self.assertEqual(1.0, self.rules.contribution(10))
        self.assertAlmostEqual(0.625, self.rules.contribution(20))
        self.assertEqual(0.25, self.rules.contribution(30))
        self.assertEqual(0.25, self.rules.contribution(90))

    def test_rating_is_highest_reached_threshold(self):
        self.assertEqual("1*", wave_sim.RATINGS[self.rules.rating(0.4)])
        self.assertEqual("3*", wave_sim.RATINGS[self.rules.rating(0.799)])
        self.assertEqual("5*", wave_sim.RATINGS[self.rules.rating(0.9)])
        self.assertEqual("5*", wave_sim.RATINGS[self.rules.rating(0.999)])
        self.assertEqual("Perfect", wave_sim.RATINGS[self.rules.rating(1.0)])

    def test_empty_building_serves_slower(self):
        self.assertEqual(12, self.rules.service_duration(staffed=True))
        self.assertAlmostEqual(12 / 0.33, self.rules.service_duration(staffed=False))


class QueueTests(unittest.TestCase):
    def test_arrivals_are_uniform_until_the_end_of_arrival(self):
        self.assertEqual([22.5, 45.0, 67.5, 90.0], wave_sim.arrivals(4, 0.0, 90.0))

    def test_unit_waits_only_until_a_seat_is_free(self):
        self.assertEqual([0, 0, 0, 10], wave_sim.waits([0, 0, 0, 0], 3, 10))
        self.assertEqual([0, 5], wave_sim.waits([0, 5], 1, 10))


class ResortTests(unittest.TestCase):
    def setUp(self):
        self.resort = wave_sim.Resort(fixture_configs())

    def test_needs_of_unbought_buildings_are_left_out(self):
        self.assertEqual({"rooms", "food", "drinks"}, set(self.resort.demand_in(3)))
        self.assertEqual({"rooms", "food", "drinks", "relax"}, set(self.resort.demand_in(4)))

    def test_debut_waves_are_waves_with_a_new_need(self):
        self.assertEqual({4, 7}, self.resort.debut_waves())


class SimulationTests(unittest.TestCase):
    def test_matches_results_in_build_document(self):
        reports = wave_sim.evaluate(fixture_configs())

        self.assertEqual(
            ["Perfect", "4*", "4*", "Perfect", "4*", "4*", "5*", "3*"],
            [wave_sim.RATINGS[report.no_intervention.rating] for report in reports],
        )
        self.assertTrue(all(report.fully_upgraded.rating >= wave_sim.FIVE_STARS for report in reports))

    def test_wr7_skips_first_two_waves_and_debut_waves(self):
        reports = {report.wave: report for report in wave_sim.evaluate(fixture_configs())}

        self.assertEqual([3, 5, 6, 8], [wave for wave, report in reports.items() if report.wr7_applies])

    def test_too_fast_empty_building_violates_wr7(self):
        configs = copy.deepcopy(fixture_configs())
        configs["wave_system"]["utilization_empty"] = 0.9

        self.assertTrue(any(report.wr7_violated for report in wave_sim.evaluate(configs)))

    def test_too_slow_service_violates_wr8(self):
        configs = copy.deepcopy(fixture_configs())
        configs["wave_system"]["service_time_s"] = 30

        self.assertTrue(any(report.wr8_violated for report in wave_sim.evaluate(configs)))


class CommandLineTests(unittest.TestCase):
    def test_config_directory_and_api_response_load_the_same(self):
        with tempfile.TemporaryDirectory() as directory:
            write_config_directory(fixture_configs(), directory)

            self.assertEqual(fixture_configs(), wave_sim.load_configs(directory))

    def test_exits_with_success_when_rules_hold(self):
        self.assertEqual(0, run_cli(fixture_configs()))

    def test_exits_with_failure_on_violation(self):
        configs = copy.deepcopy(fixture_configs())
        configs["wave_system"]["utilization_empty"] = 0.9

        self.assertEqual(1, run_cli(configs))


if __name__ == "__main__":
    unittest.main()
