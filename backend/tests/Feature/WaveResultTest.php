<?php

use App\Filament\Resources\Users\Pages\ViewUser;
use App\Models\Admin;
use App\Models\PlayerSave;
use App\Models\User;
use App\Models\WaveResult;
use Database\Seeders\GameConfigSeeder;
use Laravel\Sanctum\Sanctum;
use Livewire\Livewire;

beforeEach(function () {
    $this->seed(GameConfigSeeder::class);
});

test('backend rates a finished wave from its waits and keeps the result', function () {
    $player = User::factory()->anonymous()->create();
    Sanctum::actingAs($player);

    $this->postJson(route('api.v1.me.waves.store'), [
        'wave' => 2,
        'waits' => ['rooms' => [0, 20], 'drinks' => [5, 45]],
        'client' => ['satisfaction' => 0.71875, 'rating' => 3, 'coins' => 51],
    ])
        ->assertCreated()
        ->assertExactJson(['data' => [
            'wave' => 2,
            'units' => 4,
            'satisfaction' => 0.71875,
            'rating' => 3,
            'coins' => 51,
            'matches_client' => true,
        ]]);

    expect($player->waveResults()->sole()->only('wave', 'units', 'rating', 'coins', 'client_coins'))
        ->toBe(['wave' => 2, 'units' => 4, 'rating' => 3, 'coins' => 51, 'client_coins' => 51]);
});

test('the backend coins win over what the game claims', function () {
    $player = User::factory()->anonymous()->create();
    Sanctum::actingAs($player);

    $this->postJson(route('api.v1.me.waves.store'), [
        'wave' => 1,
        'waits' => ['rooms' => [60, 60]],
        'client' => ['rating' => 6, 'coins' => 5000],
    ])
        ->assertCreated()
        ->assertJsonPath('data.rating', 1)
        ->assertJsonPath('data.coins', 13)
        ->assertJsonPath('data.matches_client', false);
});

test('invalid wave reports are rejected', function (array $body, string $error) {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->postJson(route('api.v1.me.waves.store'), $body)->assertJsonValidationErrorFor($error);

    expect(WaveResult::count())->toBe(0);
})->with([
    'unknown need' => [['wave' => 1, 'waits' => ['spa' => [1]]], 'waits'],
    'negative wait' => [['wave' => 1, 'waits' => ['rooms' => [-1]]], 'waits.rooms.0'],
    'wait is not a number' => [['wave' => 1, 'waits' => ['rooms' => ['soon']]], 'waits.rooms.0'],
    'missing wave' => [['waits' => ['rooms' => [1]]], 'wave'],
    'too many units' => [['wave' => 1, 'waits' => ['rooms' => array_fill(0, 2001, 1)]], 'waits'],
]);

test('wave reports need a player token', function () {
    $this->postJson(route('api.v1.me.waves.store'), ['wave' => 1, 'waits' => ['rooms' => [1]]])->assertUnauthorized();
});

test('saved coins are checked against the coins the player earned', function () {
    $player = User::factory()->anonymous()->create();
    WaveResult::factory()->for($player)->create(['coins' => 768]);
    $save = PlayerSave::factory()->for($player)->create(['data' => ['economy' => ['coins' => 1768]]]);

    expect($save->earnedCoins())->toBe(1768)
        ->and($save->coinsExceedEarned())->toBeFalse();

    $save->update(['data' => ['economy' => ['coins' => 1769]]]);

    expect($save->coinsExceedEarned())->toBeTrue();
});

test('admin sees the rated waves and the coins check of a player', function () {
    $player = User::factory()->anonymous()->create();
    WaveResult::factory()->for($player)->create(['wave' => 3, 'rating' => 4, 'satisfaction' => 0.8584, 'coins' => 768, 'client_coins' => 900]);
    PlayerSave::factory()->for($player)->create(['data' => ['economy' => ['coins' => 99999]]]);
    $this->actingAs(Admin::factory()->create(), 'admin');

    Livewire::test(ViewUser::class, ['record' => $player->getRouteKey()])
        ->assertSee('Wave 3 · 4★ · 85.8 % · 768 coins (game said 900)')
        ->assertSee('More coins than earned (at most 1,768');
});
