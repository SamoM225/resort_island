<?php

use App\Models\GameConfig;
use App\Models\User;
use Database\Seeders\GameConfigSeeder;
use Illuminate\Support\Facades\File;

/**
 * @return array<string, int> config key => version of every file in database/game-configs
 */
function gameConfigFileVersions(): array
{
    return collect(File::glob(database_path('game-configs/*.json')))
        ->mapWithKeys(fn (string $path) => [pathinfo($path, PATHINFO_FILENAME) => File::json($path)['version']])
        ->all();
}

test('seeding publishes every config file in its version', function () {
    $this->seed();

    expect(gameConfigFileVersions())->toHaveKeys(['economy', 'wave_system', 'energy', 'needs', 'buildings', 'unlocks', 'waves'])
        ->and(GameConfig::query()->pluck('version', 'key')->all())->toEqual(gameConfigFileVersions());
});

test('seeding can run on every deploy without duplicating data', function () {
    $this->seed();
    $this->seed();

    expect(GameConfig::count())->toBe(count(gameConfigFileVersions()))
        ->and(User::count())->toBe(1);
});

test('seeding fails when a published config version was edited in place', function () {
    GameConfig::factory()->create([
        'key' => 'economy',
        'version' => gameConfigFileVersions()['economy'],
        'data' => ['starting_coins' => 1],
    ]);

    $this->seed(GameConfigSeeder::class);
})->throws(RuntimeException::class, 'Bump its version');

test('servers get configs but no test user', function () {
    app()->detectEnvironment(fn () => 'production');

    $this->artisan('db:seed', ['--force' => true])->assertSuccessful();

    expect(GameConfig::count())->toBe(count(gameConfigFileVersions()))
        ->and(User::count())->toBe(0);
});
