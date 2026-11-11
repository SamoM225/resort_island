<?php

use App\Filament\Resources\Users\Pages\ViewUser;
use App\Models\Admin;
use App\Models\PlayerSave;
use App\Models\User;
use Laravel\Sanctum\Sanctum;
use Livewire\Livewire;

/**
 * @return array<string, mixed>
 */
function gameSave(int $coins = 1250): array
{
    return [
        'saveVersion' => 1,
        'savedAtUtc' => '2026-11-11T10:00:00Z',
        'economy' => ['coins' => $coins, 'gems' => 12, 'energy' => 80, 'resortValue' => 24500, 'reputation' => 4],
        'resort' => [
            'buildings' => [
                ['buildingId' => 'reception', 'cellX' => -2, 'cellY' => 2],
                ['buildingId' => 'beachbar', 'cellX' => 2, 'cellY' => -2],
            ],
            'unlocks' => ['pool'],
        ],
    ];
}

test('game backs up its save and gets it back on another device', function () {
    $player = User::factory()->anonymous()->create();
    Sanctum::actingAs($player);

    $this->putJson(route('api.v1.me.save.update'), [
        'save_version' => 1,
        'saved_at' => '2026-11-11T11:00:00+01:00',
        'save' => gameSave(),
    ])
        ->assertCreated()
        ->assertJsonPath('data.save_version', 1)
        ->assertJsonPath('data.saved_at', '2026-11-11T10:00:00+00:00');

    $this->getJson(route('api.v1.me.save.show'))
        ->assertOk()
        ->assertJsonPath('data.save.economy.coins', 1250)
        ->assertJsonPath('data.save.resort.buildings.1.buildingId', 'beachbar');
});

test('a newer save replaces the backup', function () {
    $player = User::factory()->anonymous()->create();
    PlayerSave::factory()->for($player)->create(['saved_at' => '2026-11-11 10:00:00', 'data' => gameSave(1000)]);
    Sanctum::actingAs($player);

    $this->putJson(route('api.v1.me.save.update'), [
        'save_version' => 1,
        'saved_at' => '2026-11-11T10:05:00Z',
        'save' => gameSave(1400),
    ])->assertOk();

    expect(PlayerSave::count())->toBe(1)
        ->and($player->gameSave()->sole()->data['economy']['coins'])->toBe(1400);
});

test('a save older than the backup never overwrites it', function () {
    $player = User::factory()->anonymous()->create();
    PlayerSave::factory()->for($player)->create(['saved_at' => '2026-11-11 10:05:00', 'data' => gameSave(1400)]);
    Sanctum::actingAs($player);

    $this->putJson(route('api.v1.me.save.update'), [
        'save_version' => 1,
        'saved_at' => '2026-11-11T10:00:00Z',
        'save' => gameSave(1000),
    ])
        ->assertConflict()
        ->assertJsonPath('data.save.economy.coins', 1400);

    expect($player->gameSave()->sole()->data['economy']['coins'])->toBe(1400);
});

test('player without a backup gets 404', function () {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->getJson(route('api.v1.me.save.show'))->assertNotFound();
});

test('a player only ever sees their own save', function () {
    PlayerSave::factory()->create(['data' => gameSave(9999)]);
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->getJson(route('api.v1.me.save.show'))->assertNotFound();
});

test('save backup needs a token and a player who is not banned', function () {
    $this->getJson(route('api.v1.me.save.show'))->assertUnauthorized();

    Sanctum::actingAs(User::factory()->banned()->create());

    $this->putJson(route('api.v1.me.save.update'), ['save_version' => 1, 'saved_at' => now()->toIso8601String(), 'save' => gameSave()])
        ->assertForbidden();

    expect(PlayerSave::count())->toBe(0);
});

test('invalid save is rejected', function (array $body, string $error) {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->putJson(route('api.v1.me.save.update'), $body)->assertJsonValidationErrorFor($error);

    expect(PlayerSave::count())->toBe(0);
})->with([
    'missing version' => [['saved_at' => '2026-11-11T10:00:00Z', 'save' => ['economy' => []]], 'save_version'],
    'missing time' => [['save_version' => 1, 'save' => ['economy' => []]], 'saved_at'],
    'save not an object' => [['save_version' => 1, 'saved_at' => '2026-11-11T10:00:00Z', 'save' => 'save'], 'save'],
    'save too large' => [['save_version' => 1, 'saved_at' => '2026-11-11T10:00:00Z', 'save' => ['blob' => str_repeat('x', 270000)]], 'save'],
]);

test('admin sees the save backup of a player', function () {
    $player = User::factory()->anonymous()->create();
    PlayerSave::factory()->for($player)->create(['data' => gameSave(1250)]);
    $this->actingAs(Admin::factory()->create(), 'admin');

    Livewire::test(ViewUser::class, ['record' => $player->getRouteKey()])
        ->assertSee('Save backup')
        ->assertSee('1,250')
        ->assertSee('beachbar');
});
