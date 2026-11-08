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

test('seeding keeps a config changed in the admin panel', function () {
    $edited = GameConfig::factory()->create([
        'key' => 'economy',
        'version' => gameConfigFileVersions()['economy'],
        'data' => ['starting_coins' => 1500],
    ]);

    $this->seed(GameConfigSeeder::class);

    expect(GameConfig::query()->where('key', 'economy')->sole()->data)->toBe($edited->data);
});

test('seeding publishes a config file with a newer version', function () {
    $directory = storage_path('framework/testing/game-configs-'.uniqid());
    File::ensureDirectoryExists($directory);
    File::put("{$directory}/economy.json", json_encode(['version' => 2, 'data' => ['starting_coins' => 2000]]));
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);

    $seeder = new GameConfigSeeder;
    $seeder->directory = $directory;
    $seeder->run();
    File::deleteDirectory($directory);

    expect(GameConfig::query()->where('key', 'economy')->orderBy('version')->pluck('data', 'version')->all())
        ->toBe([1 => ['starting_coins' => 1000], 2 => ['starting_coins' => 2000]]);
});

test('servers get configs but no test user', function () {
    app()->detectEnvironment(fn () => 'production');

    $this->artisan('db:seed', ['--force' => true])->assertSuccessful();

    expect(GameConfig::count())->toBe(count(gameConfigFileVersions()))
        ->and(User::count())->toBe(0);
});
