<?php

use App\Models\GameConfig;
use App\Services\WaveRating;
use Database\Seeders\GameConfigSeeder;
use Illuminate\Support\Arr;
use Illuminate\Support\Facades\File;

/**
 * @return array{rules: array<string, mixed>, cases: list<array<string, mixed>>}
 */
function waveRatingCases(): array
{
    return File::json(base_path('../tools/wave_sim/fixtures/wave_rating_cases.json'), JSON_THROW_ON_ERROR);
}

function waveRatingFromCases(): WaveRating
{
    $rules = waveRatingCases()['rules'];

    return WaveRating::fromRules($rules['wave_system'], $rules['economy'], $rules['needs']);
}

test('every case from wave_sim gets the same satisfaction, rating and coins', function () {
    $rating = waveRatingFromCases();
    $cases = waveRatingCases()['cases'];

    expect($cases)->not->toBeEmpty();

    foreach ($cases as $case) {
        $result = $rating->evaluate($case['waits']);

        expect($result['units'])->toBe($case['units'], $case['name'])
            ->and($result['satisfaction'])->toEqualWithDelta($case['satisfaction'], 1e-9, $case['name'])
            ->and($result['rating'])->toBe($case['rating'], $case['name'])
            ->and($result['coins'])->toBe($case['coins'], $case['name']);
    }
});

test('the backend rates with the configs it serves to the game', function () {
    $this->seed(GameConfigSeeder::class);
    $rules = waveRatingCases()['rules'];

    $served = GameConfig::query()->latestVersions()->pluck('data', 'key');

    expect(Arr::only($served['wave_system'], array_keys($rules['wave_system'])))->toEqual($rules['wave_system'])
        ->and(Arr::only($served['economy'], array_keys($rules['economy'])))->toEqual($rules['economy'])
        ->and(WaveRating::fromCurrentConfigs()->evaluate(['rooms' => [0, 20]])['rating'])->toBe(4);
});

test('contribution is full within the grace and falls to the unit floor', function () {
    $rating = waveRatingFromCases();

    expect($rating->contribution(0))->toBe(1.0)
        ->and($rating->contribution(10))->toBe(1.0)
        ->and($rating->contribution(20))->toEqualWithDelta(0.625, 1e-12)
        ->and($rating->contribution(30))->toBe(0.25)
        ->and($rating->contribution(300))->toBe(0.25);
});

test('an unknown need is rejected', function () {
    waveRatingFromCases()->satisfaction(['spa' => [3]]);
})->throws(InvalidArgumentException::class);
