<?php

namespace App\Services;

use App\Models\GameConfig;
use InvalidArgumentException;

/**
 * How a wave is rated (build document 4.4 and 4.8), the same model as tools/wave_sim/wave_sim.py and the game:
 * every unit of a need contributes by how long it waited until it was seated, weighted by its need;
 * the weighted average (at least wave_floor) is the satisfaction, the highest reached threshold the rating
 * (1-5 stars, 6 = Perfect), and the coins are units x coins_per_unit x the rating's multiplier.
 */
class WaveRating
{
    public const PERFECT = 6;

    private const EPSILON = 1e-9;

    /**
     * @param  array{patience_grace_s: int|float, patience_floor_after_s: int|float, unit_floor: int|float, wave_floor: int|float, rating_thresholds: list<int|float>}  $waveSystem
     * @param  array{coins_per_unit: int|float, coins_rating_mult: list<int|float>}  $economy
     * @param  array<string, int|float>  $weights  need id => satisfaction weight
     */
    public function __construct(
        private readonly array $waveSystem,
        private readonly array $economy,
        private readonly array $weights,
    ) {
        if (count($waveSystem['rating_thresholds']) !== self::PERFECT || count($economy['coins_rating_mult']) !== self::PERFECT) {
            throw new InvalidArgumentException('Configs need 6 rating thresholds and coin multipliers (1-5 stars and Perfect).');
        }
    }

    /**
     * @param  list<array{id: string, satisfaction_weight: int|float}>  $needs
     */
    public static function fromRules(array $waveSystem, array $economy, array $needs): self
    {
        return new self($waveSystem, $economy, array_column($needs, 'satisfaction_weight', 'id'));
    }

    /**
     * Rating with the configs the game currently gets from GET /api/v1/configs.
     */
    public static function fromCurrentConfigs(): self
    {
        $configs = GameConfig::query()->latestVersions()->whereIn('key', ['wave_system', 'economy', 'needs'])->pluck('data', 'key');

        return self::fromRules($configs['wave_system'], $configs['economy'], $configs['needs']['needs']);
    }

    public function hasNeed(string $need): bool
    {
        return array_key_exists($need, $this->weights);
    }

    /**
     * 100 % within patience_grace, then linearly down to unit_floor at patience_floor_after.
     */
    public function contribution(float $wait): float
    {
        $grace = $this->waveSystem['patience_grace_s'];
        $floorAfter = $this->waveSystem['patience_floor_after_s'];

        if ($wait <= $grace) {
            return 1.0;
        }

        if ($wait >= $floorAfter) {
            return (float) $this->waveSystem['unit_floor'];
        }

        $progress = ($wait - $grace) / ($floorAfter - $grace);

        return 1.0 - $progress * (1.0 - $this->waveSystem['unit_floor']);
    }

    /**
     * @param  array<string, list<int|float>>  $waitsByNeed
     */
    public function satisfaction(array $waitsByNeed): float
    {
        $weighted = 0.0;
        $totalWeight = 0.0;

        foreach ($waitsByNeed as $need => $waits) {
            $weight = $this->weights[$need] ?? throw new InvalidArgumentException("Need [{$need}] is not in the needs config.");

            foreach ($waits as $wait) {
                $weighted += $this->contribution((float) $wait) * $weight;
                $totalWeight += $weight;
            }
        }

        return $totalWeight == 0.0 ? 1.0 : max((float) $this->waveSystem['wave_floor'], $weighted / $totalWeight);
    }

    public function rating(float $satisfaction): int
    {
        $rating = 1;

        foreach ($this->waveSystem['rating_thresholds'] as $index => $threshold) {
            if ($satisfaction + self::EPSILON >= $threshold) {
                $rating = $index + 1;
            }
        }

        return $rating;
    }

    public function coins(int $units, int $rating): int
    {
        return (int) floor($units * $this->economy['coins_per_unit'] * $this->economy['coins_rating_mult'][$rating - 1] + 0.5);
    }

    /**
     * @param  array<string, list<int|float>>  $waitsByNeed
     * @return array{units: int, satisfaction: float, rating: int, coins: int}
     */
    public function evaluate(array $waitsByNeed): array
    {
        $units = array_sum(array_map(count(...), $waitsByNeed));
        $satisfaction = $this->satisfaction($waitsByNeed);
        $rating = $this->rating($satisfaction);

        return [
            'units' => $units,
            'satisfaction' => $satisfaction,
            'rating' => $rating,
            'coins' => $this->coins($units, $rating),
        ];
    }
}
