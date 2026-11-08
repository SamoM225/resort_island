<?php

use App\Filament\Resources\Users\Pages\ListUsers;
use App\Filament\Resources\Users\Pages\ViewUser;
use App\Models\Admin;
use App\Models\User;
use Filament\Actions\Testing\TestAction;
use Livewire\Livewire;

beforeEach(function () {
    $this->actingAs(Admin::factory()->create(), 'admin');
});

test('admin sees anonymous and linked players', function () {
    $anonymous = User::factory()->anonymous()->create();
    $linked = User::factory()->create(['email' => 'player@resort.test']);

    Livewire::test(ListUsers::class)
        ->assertCanSeeTableRecords([$anonymous, $linked])
        ->assertSee("Player #{$anonymous->id}")
        ->assertSee('player@resort.test');
});

test('admin finds a player by email', function () {
    $wanted = User::factory()->create(['email' => 'wanted@resort.test']);
    $other = User::factory()->create(['email' => 'other@resort.test']);

    Livewire::test(ListUsers::class)
        ->searchTable('wanted@')
        ->assertCanSeeTableRecords([$wanted])
        ->assertCanNotSeeTableRecords([$other]);
});

test('admin bans a player with a reason', function () {
    $player = User::factory()->anonymous()->create();

    Livewire::test(ListUsers::class)
        ->callAction(TestAction::make('ban')->table($player), data: ['reason' => 'Cheating'])
        ->assertHasNoFormErrors();

    expect($player->refresh()->isBanned())->toBeTrue()
        ->and($player->ban_reason)->toBe('Cheating');
});

test('ban needs a reason', function () {
    $player = User::factory()->anonymous()->create();

    Livewire::test(ListUsers::class)
        ->callAction(TestAction::make('ban')->table($player), data: ['reason' => ''])
        ->assertHasFormErrors(['reason' => 'required']);

    expect($player->refresh()->isBanned())->toBeFalse();
});

test('admin unbans a player from the detail page', function () {
    $player = User::factory()->banned()->create();

    Livewire::test(ViewUser::class, ['record' => $player->getRouteKey()])
        ->assertSee('Cheating')
        ->callAction('unban');

    expect($player->refresh()->isBanned())->toBeFalse()
        ->and($player->ban_reason)->toBeNull();
});

test('admin filters banned players', function () {
    $banned = User::factory()->banned()->create();
    $active = User::factory()->create();

    Livewire::test(ListUsers::class)
        ->filterTable('banned_at', true)
        ->assertCanSeeTableRecords([$banned])
        ->assertCanNotSeeTableRecords([$active]);
});
