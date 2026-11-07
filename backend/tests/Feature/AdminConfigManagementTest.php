<?php

use App\Filament\Resources\GameConfigs\Pages\EditGameConfig;
use App\Filament\Resources\GameConfigs\Pages\ListGameConfigs;
use App\Filament\Resources\GameConfigs\Pages\ViewGameConfig;
use App\Models\Admin;
use App\Models\GameConfig;
use Illuminate\Http\UploadedFile;
use Livewire\Livewire;

beforeEach(function () {
    $this->actingAs(Admin::factory()->create(), 'admin');
});

test('admin sees the latest version of every config', function () {
    $oldEconomy = GameConfig::factory()->create(['key' => 'economy', 'version' => 1]);
    $economy = GameConfig::factory()->create(['key' => 'economy', 'version' => 2]);
    $waves = GameConfig::factory()->create(['key' => 'waves', 'version' => 1]);

    Livewire::test(ListGameConfigs::class)
        ->assertCanSeeTableRecords([$economy, $waves])
        ->assertCanNotSeeTableRecords([$oldEconomy]);
});

test('admin expands a config and sees its versions', function () {
    GameConfig::factory()->create(['key' => 'waves', 'version' => 1]);
    $waves = GameConfig::factory()->create([
        'key' => 'waves',
        'version' => 2,
        'data' => ['waves' => [['wave' => 1, 'demand' => ['rooms' => 8, 'drinks' => 8]]]],
    ]);

    Livewire::test(ViewGameConfig::class, ['record' => $waves->getRouteKey()])
        ->assertSee('demand')
        ->assertSee('drinks')
        ->assertSee('v1 ·')
        ->assertSee('v2 ·');
});

test('on dev saving overwrites the current version', function () {
    config(['game.configs.free_editing' => true]);
    $economy = GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);

    Livewire::test(EditGameConfig::class, ['record' => $economy->getRouteKey()])
        ->assertSee('Development: a change overwrites the current version')
        ->fillForm(['data' => '{"starting_coins": 1500, "starting_gems": 20}'])
        ->call('save')
        ->assertHasNoFormErrors();

    expect(GameConfig::query()->sole()->only('version', 'data'))
        ->toEqual(['version' => 1, 'data' => ['starting_coins' => 1500, 'starting_gems' => 20]]);
});

test('on prod saving a number publishes a new version', function () {
    config(['game.configs.free_editing' => false]);
    $economy = GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000, 'weight' => 1.0]]);

    Livewire::test(EditGameConfig::class, ['record' => $economy->getRouteKey()])
        ->assertSee('Production: only numbers can change')
        ->fillForm(['data' => '{"starting_coins": 1500, "weight": 1.2}'])
        ->call('save')
        ->assertHasNoFormErrors()
        ->assertRedirect(ViewGameConfig::getUrl(['record' => GameConfig::query()->where('version', 2)->sole()]));

    $versions = GameConfig::query()->orderBy('version')->pluck('data', 'version')->all();

    expect($versions)->toEqual([
        1 => ['starting_coins' => 1000, 'weight' => 1.0],
        2 => ['starting_coins' => 1500, 'weight' => 1.2],
    ])
        ->and($versions[1]['weight'])->toBe(1.0);
});

test('on prod a breaking change is shown and nothing is saved', function () {
    config(['game.configs.free_editing' => false]);
    $buildings = GameConfig::factory()->create(['key' => 'buildings', 'data' => ['buildings' => [['id' => 'reception', 'capacity' => 3]]]]);

    Livewire::test(EditGameConfig::class, ['record' => $buildings->getRouteKey()])
        ->fillForm(['data' => '{"buildings": [{"id": "hotel", "capacity": 3}]}'])
        ->call('save')
        ->assertHasFormErrors(['data'])
        ->assertSee('buildings.0.id: only numbers can change on production');

    expect(GameConfig::query()->sole()->data)->toBe(['buildings' => [['id' => 'reception', 'capacity' => 3]]]);
});

test('invalid JSON is rejected', function () {
    $economy = GameConfig::factory()->create(['key' => 'economy']);

    Livewire::test(EditGameConfig::class, ['record' => $economy->getRouteKey()])
        ->fillForm(['data' => '{"starting_coins": '])
        ->call('save')
        ->assertHasFormErrors(['data' => 'json']);
});

test('admin downloads one config in the seed file format', function () {
    $economy = GameConfig::factory()->create(['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1000]]);

    Livewire::test(ListGameConfigs::class)
        ->callTableAction('download', $economy)
        ->assertFileDownloaded('economy.json', json_encode(['version' => 2, 'data' => ['starting_coins' => 1000]], JSON_PRETTY_PRINT));
});

test('admin downloads all configs in the API format', function () {
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);
    GameConfig::factory()->create(['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1500]]);

    Livewire::test(ListGameConfigs::class)
        ->callAction('downloadAll')
        ->assertFileDownloaded(
            'game-configs-testing-'.now()->format('Y-m-d').'.json',
            json_encode(['data' => [['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1500]]]], JSON_PRETTY_PRINT),
        );
});

test('admin uploads downloaded configs back', function () {
    config(['game.configs.free_editing' => false]);
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);
    GameConfig::factory()->create(['key' => 'energy', 'version' => 1, 'data' => ['boost_cost' => 3]]);

    $bundle = UploadedFile::fake()->createWithContent('game-configs-production.json', json_encode(['data' => [
        ['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1200]],
        ['key' => 'energy', 'version' => 1, 'data' => ['boost_cost' => 3]],
    ]]));

    Livewire::test(ListGameConfigs::class)
        ->callAction('upload', data: ['files' => [$bundle]])
        ->assertHasNoFormErrors()
        ->assertNotified('Configs uploaded');

    expect(GameConfig::query()->latestVersions()->orderBy('key')->get()->map->only('key', 'version', 'data')->all())->toBe([
        ['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1200]],
        ['key' => 'energy', 'version' => 1, 'data' => ['boost_cost' => 3]],
    ]);
});

test('admin uploads single config files on dev, a new one included', function () {
    config(['game.configs.free_editing' => true]);
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);

    Livewire::test(ListGameConfigs::class)
        ->callAction('upload', data: ['files' => [
            UploadedFile::fake()->createWithContent('economy.json', json_encode(['version' => 1, 'data' => ['starting_coins' => 800]])),
            UploadedFile::fake()->createWithContent('seasons.json', json_encode(['version' => 1, 'data' => ['peak_waves' => [5, 6]]])),
        ]])
        ->assertNotified('Configs uploaded');

    expect(GameConfig::query()->orderBy('key')->get()->map->only('key', 'version', 'data')->all())->toBe([
        ['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 800]],
        ['key' => 'seasons', 'version' => 1, 'data' => ['peak_waves' => [5, 6]]],
    ]);
});

test('a rejected upload publishes nothing', function () {
    config(['game.configs.free_editing' => false]);
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);

    Livewire::test(ListGameConfigs::class)
        ->callAction('upload', data: ['files' => [
            UploadedFile::fake()->createWithContent('economy.json', json_encode(['version' => 1, 'data' => ['starting_coins' => 1200]])),
            UploadedFile::fake()->createWithContent('broken.json', '{"no data here": true}'),
        ]])
        ->assertNotified('Nothing was published');

    expect(GameConfig::query()->sole()->version)->toBe(1);
});
