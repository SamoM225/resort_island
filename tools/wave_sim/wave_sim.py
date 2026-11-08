import argparse
import heapq
import json
import sys
from dataclasses import dataclass
from pathlib import Path

DEFAULT_CONFIGS = Path(__file__).resolve().parents[2] / "backend" / "database" / "game-configs"
RATINGS = ("1*", "2*", "3*", "4*", "5*", "Perfect")
FIVE_STARS = RATINGS.index("5*")
WR7_FROM_WAVE = 3
EPSILON = 1e-9


def load_configs(path):
    path = Path(path)
    if path.is_dir():
        return {file.stem: json.loads(file.read_text(encoding="utf-8"))["data"] for file in sorted(path.glob("*.json"))}
    response = json.loads(path.read_text(encoding="utf-8"))
    return {item["key"]: item["data"] for item in response["data"]}


def arrivals(units, start, duration):
    return [start + duration * k / units for k in range(1, units + 1)]


def waits(arrival_times, seats, service_duration):
    free_at = [0.0] * seats
    result = []
    for arrival in sorted(arrival_times):
        start = max(arrival, heapq.heappop(free_at))
        heapq.heappush(free_at, start + service_duration)
        result.append(start - arrival)
    return result


@dataclass(frozen=True)
class Building:
    id: str
    need: str
    unlocked_after_wave: int


@dataclass(frozen=True)
class WaveResult:
    satisfaction: float
    rating: int
    empty_building: str


@dataclass(frozen=True)
class WaveReport:
    wave: int
    demand: dict
    no_intervention: WaveResult
    fully_upgraded: WaveResult
    wr7_applies: bool

    @property
    def wr7_violated(self):
        return self.wr7_applies and self.no_intervention.rating >= FIVE_STARS

    @property
    def wr8_violated(self):
        return self.fully_upgraded.rating < FIVE_STARS


class WaveRules:
    def __init__(self, wave_system):
        self.wave_timer = wave_system["wave_timer_s"]
        self.service_time = wave_system["service_time_s"]
        self.utilization_staffed = wave_system["utilization_staffed"]
        self.utilization_empty = wave_system["utilization_empty"]
        self.patience_grace = wave_system["patience_grace_s"]
        self.patience_floor_after = wave_system["patience_floor_after_s"]
        self.unit_floor = wave_system["unit_floor"]
        self.wave_floor = wave_system["wave_floor"]
        self.rating_thresholds = wave_system["rating_thresholds"]
        rush = wave_system["rush"]
        self.rush_wave = rush["wave"]
        self.rush_time = rush["time_s"]
        self.rush_units = rush["units"]
        self.rush_duration = rush["duration_s"]

    def contribution(self, wait):
        if wait <= self.patience_grace:
            return 1.0
        if wait >= self.patience_floor_after:
            return self.unit_floor
        progress = (wait - self.patience_grace) / (self.patience_floor_after - self.patience_grace)
        return 1.0 - progress * (1.0 - self.unit_floor)

    def service_duration(self, staffed):
        utilization = self.utilization_staffed if staffed else self.utilization_empty
        if utilization <= 0:
            raise ValueError("Utilization must be above 0, otherwise a wave never ends.")
        return self.service_time / utilization

    def rating(self, satisfaction):
        reached = [index for index, threshold in enumerate(self.rating_thresholds) if satisfaction + EPSILON >= threshold]
        return max(reached, default=0)


class Resort:
    def __init__(self, configs):
        unlock_waves = {unlock["id"]: unlock["after_wave"] for unlock in configs["unlocks"]["unlocks"]}
        self.buildings = [
            Building(building["id"], building["need"], unlock_waves[building["unlock"]] if building["unlock"] else 0)
            for building in configs["buildings"]["buildings"]
        ]
        self.capacities = [level["capacity"] for level in configs["buildings"]["levels"]]
        self.weights = {need["id"]: need["satisfaction_weight"] for need in configs["needs"]["needs"]}
        self.table = {wave["wave"]: wave["demand"] for wave in configs["waves"]["waves"]}

    @property
    def waves(self):
        return sorted(self.table)

    def buildings_in(self, wave):
        return [building for building in self.buildings if building.unlocked_after_wave < wave]

    def demand_in(self, wave):
        return {building.need: self.table[wave].get(building.need, 0) for building in self.buildings_in(wave)}

    def debut_waves(self):
        debuts = set()
        seen = set()
        for wave in self.waves:
            needs = {need for need, units in self.demand_in(wave).items() if units > 0}
            if seen and needs - seen:
                debuts.add(wave)
            seen |= needs
        return debuts


class WaveSimulator:
    def __init__(self, resort, rules):
        self.resort = resort
        self.rules = rules

    def satisfaction(self, wave, capacity, empty_building):
        demand = self.resort.demand_in(wave)
        weighted = 0.0
        total_weight = 0.0
        for building in self.resort.buildings_in(wave):
            unit_arrivals = arrivals(demand[building.need], 0.0, self.rules.wave_timer)
            if wave == self.rules.rush_wave and building.id == empty_building:
                unit_arrivals += arrivals(self.rules.rush_units, self.rules.rush_time, self.rules.rush_duration)
            duration = self.rules.service_duration(staffed=building.id != empty_building)
            weight = self.resort.weights[building.need]
            for wait in waits(unit_arrivals, capacity, duration):
                weighted += self.rules.contribution(wait) * weight
                total_weight += weight
        if total_weight == 0:
            return 1.0
        return max(self.rules.wave_floor, weighted / total_weight)

    def best_static_placement(self, wave, capacity):
        satisfaction, empty_building = max(
            (self.satisfaction(wave, capacity, building.id), building.id) for building in self.resort.buildings_in(wave)
        )
        return WaveResult(satisfaction, self.rules.rating(satisfaction), empty_building)


def evaluate(configs):
    resort = Resort(configs)
    simulator = WaveSimulator(resort, WaveRules(configs["wave_system"]))
    debuts = resort.debut_waves()
    return [
        WaveReport(
            wave,
            resort.demand_in(wave),
            simulator.best_static_placement(wave, resort.capacities[0]),
            simulator.best_static_placement(wave, resort.capacities[-1]),
            wave >= WR7_FROM_WAVE and wave not in debuts,
        )
        for wave in resort.waves
    ]


def describe(result):
    return f"{RATINGS[result.rating]:<8}{result.satisfaction * 100:6.1f} %  ({result.empty_building} empty)"


def format_report(reports, source):
    demands = [", ".join(f"{need} {units}" for need, units in report.demand.items()) for report in reports]
    width = max(len("Demand"), *(len(demand) for demand in demands)) + 2
    lines = [
        f"Wave simulation, configs: {source}",
        "",
        f"{'Wave':<6}{'Demand':<{width}}{'No intervention, Lv1':<40}{'Fully upgraded':<40}{'WR7':<10}WR8",
    ]
    for report, demand in zip(reports, demands):
        wr7 = ("VIOLATED" if report.wr7_violated else "ok") if report.wr7_applies else "-"
        wr8 = "VIOLATED" if report.wr8_violated else "ok"
        lines.append(f"{report.wave:<6}{demand:<{width}}{describe(report.no_intervention):<40}{describe(report.fully_upgraded):<40}{wr7:<10}{wr8}")
    violations = [f"WR7 in wave {r.wave}" for r in reports if r.wr7_violated] + [f"WR8 in wave {r.wave}" for r in reports if r.wr8_violated]
    lines.append("")
    lines.append("Violations: " + ", ".join(violations) if violations else "WR7 and WR8 hold in every wave.")
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Simulates every scripted wave and checks the wave rules WR7 and WR8.")
    parser.add_argument(
        "--configs",
        default=str(DEFAULT_CONFIGS),
        help="directory with {key}.json game configs (default: backend/database/game-configs) or a saved GET /api/v1/configs response",
    )
    args = parser.parse_args(argv)
    reports = evaluate(load_configs(args.configs))
    print(format_report(reports, args.configs))
    return 1 if any(report.wr7_violated or report.wr8_violated for report in reports) else 0


if __name__ == "__main__":
    sys.exit(main())
