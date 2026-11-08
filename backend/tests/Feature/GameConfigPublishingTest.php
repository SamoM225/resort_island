<?php

use App\Models\GameConfig;
use Illuminate\Support\Arr;
use Illuminate\Validation\ValidationException;

/**
 * @return array<string, mixed>
 */
function economyData(array $overrides = []): array
{
    return [
        'starting_coins' => 1000,
        'coins_rating_mult' => [0.4, 0.6, 0.8, 1.0, 1.2, 1.3],
        'rush' => ['wave' => 2, 'units' => 6],
        'currency' => 'coins',
        ...$overrides,
    ];
}

function publishingErrors(Closure $publish): array
{
    try {
        $publish();
    } catch (ValidationException $exception) {
        return Arr::flatten($exception->errors());
    }

    return [];
}

describe('dev', function () {
    beforeEach(fn () => config(['game.configs.free_editing' => true]));

    test('a change overwrites the current version, structure included', function () {
        $config = GameConfig::factory()->create(['key' => 'economy', 'version' => 3, 'data' => economyData()]);

        $published = GameConfig::publish('economy', ['starting_coins' => 50, 'new_flag' => true]);

        expect($published->is($config))->toBeTrue()
            ->and($published->version)->toBe(3)
            ->and(GameConfig::count())->toBe(1)
            ->and($published->refresh()->data)->toEqual(['starting_coins' => 50, 'new_flag' => true]);
    });

    test('a new config key starts at version 1', function () {
        $published = GameConfig::publish('seasons', ['peak_waves' => [5, 6]]);

        expect($published->version)->toBe(1)
            ->and($published->wasRecentlyCreated)->toBeTrue();
    });

    test('a new config key must be snake_case', function () {
        expect(publishingErrors(fn () => GameConfig::publish('Seasons 2', ['a' => 1])))
            ->toContain('Seasons 2: config keys are snake_case, e.g. wave_system.');
    });

    test('the game gets the overwritten config right away', function () {
        GameConfig::factory()->create(['key' => 'economy', 'data' => economyData()]);
        $this->getJson(route('api.v1.configs.index'))->assertJsonPath('data.0.data.starting_coins', 1000);

        GameConfig::publish('economy', economyData(['starting_coins' => 1500]));

        $this->getJson(route('api.v1.configs.index'))->assertJsonPath('data.0.data.starting_coins', 1500);
    });
});

describe('prod', function () {
    beforeEach(function () {
        config(['game.configs.free_editing' => false]);
        GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => economyData()]);
    });

    test('changed numbers are published as a new version', function () {
        $published = GameConfig::publish('economy', economyData([
            'starting_coins' => 1500,
            'coins_rating_mult' => [0.5, 0.6, 0.8, 1.1, 1.2, 1.3],
        ]));

        expect($published->version)->toBe(2)
            ->and($published->data['starting_coins'])->toBe(1500)
            ->and(GameConfig::query()->where('version', 1)->sole()->data)->toEqual(economyData());
    });

    test('a decimal value stays editable even when it is a round number', function () {
        $published = GameConfig::publish('economy', economyData(['coins_rating_mult' => [0.4, 0.6, 0.8, 1.15, 1.2, 1.3]]));

        expect($published->version)->toBe(2);
    });

    test('unchanged data publishes nothing', function () {
        $published = GameConfig::publish('economy', economyData());

        expect($published->version)->toBe(1)
            ->and(GameConfig::count())->toBe(1);
    });

    test('a change that could break the game is rejected', function (array $data, string $error) {
        expect(publishingErrors(fn () => GameConfig::publish('economy', $data)))->toContain($error)
            ->and(GameConfig::count())->toBe(1);
    })->with([
        'text changed' => [economyData(['currency' => 'gems']), 'currency: only numbers can change on production, this value must stay "coins".'],
        'key added' => [economyData(['bonus' => 5]), 'Config: keys cannot be added or removed on production (+bonus).'],
        'key removed' => [Arr::except(economyData(), 'rush'), 'Config: keys cannot be added or removed on production (-rush).'],
        'list resized' => [economyData(['coins_rating_mult' => [0.4, 0.6]]), 'coins_rating_mult: the list must keep its 6 items on production.'],
        'whole number became decimal' => [economyData(['starting_coins' => 1000.5]), 'starting_coins: must stay a whole number.'],
        'negative number' => [economyData(['rush' => ['wave' => 2, 'units' => -1]]), 'rush.units: cannot be negative.'],
        'number became text' => [economyData(['starting_coins' => '1000']), 'starting_coins: must stay a number.'],
    ]);

    test('a new config key is rejected', function () {
        expect(publishingErrors(fn () => GameConfig::publish('seasons', ['peak_waves' => [5, 6]])))
            ->toContain('seasons: new configs can only be added on dev, the game does not know them yet.');
    });

    test('an upload publishes all configs or none', function () {
        GameConfig::factory()->create(['key' => 'energy', 'version' => 1, 'data' => ['boost_cost' => 3]]);

        $errors = publishingErrors(fn () => GameConfig::publishMany([
            'energy' => ['boost_cost' => 4],
            'economy' => economyData(['currency' => 'gems']),
        ]));

        expect($errors)->not->toBeEmpty()
            ->and(GameConfig::query()->where('key', 'energy')->sole()->data)->toBe(['boost_cost' => 3]);
    });
});
