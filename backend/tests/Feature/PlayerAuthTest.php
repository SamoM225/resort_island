<?php

use App\Models\User;
use Illuminate\Support\Facades\Hash;
use Laravel\Sanctum\Sanctum;

test('game registers an anonymous player and gets a token', function () {
    $response = $this->postJson(route('api.v1.auth.anonymous'), ['device_name' => 'Pixel 8'])
        ->assertCreated()
        ->assertJsonPath('data.is_anonymous', true)
        ->assertJsonPath('data.email', null);

    $player = User::findOrFail($response->json('data.id'));

    expect($player->isAnonymous())->toBeTrue()
        ->and($player->tokens()->sole()->name)->toBe('Pixel 8');

    $this->withToken($response->json('token'))
        ->getJson(route('api.v1.me'))
        ->assertOk()
        ->assertJsonPath('data.id', $player->id);
});

test('game API rejects requests without a token', function () {
    $this->getJson(route('api.v1.me'))->assertUnauthorized();
});

test('anonymous player links an email and keeps the same account', function () {
    $player = User::factory()->anonymous()->create();
    Sanctum::actingAs($player);

    $this->postJson(route('api.v1.me.email'), [
        'email' => 'player@resort.test',
        'password' => 'island-password',
        'password_confirmation' => 'island-password',
        'name' => 'Tomas',
    ])
        ->assertOk()
        ->assertJsonPath('data.id', $player->id)
        ->assertJsonPath('data.is_anonymous', false);

    $player->refresh();

    expect($player->email)->toBe('player@resort.test')
        ->and($player->name)->toBe('Tomas')
        ->and(Hash::check('island-password', $player->password))->toBeTrue();
});

test('linking an email that another player uses is rejected', function () {
    User::factory()->create(['email' => 'taken@resort.test']);
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->postJson(route('api.v1.me.email'), [
        'email' => 'taken@resort.test',
        'password' => 'island-password',
        'password_confirmation' => 'island-password',
    ])->assertJsonValidationErrorFor('email');
});

test('player with a linked email cannot link another one', function () {
    Sanctum::actingAs(User::factory()->create(['email' => 'first@resort.test']));

    $this->postJson(route('api.v1.me.email'), [
        'email' => 'second@resort.test',
        'password' => 'island-password',
        'password_confirmation' => 'island-password',
    ])->assertConflict();

    expect(User::query()->where('email', 'second@resort.test')->exists())->toBeFalse();
});

test('player signs in with the linked email on another device', function () {
    $player = User::factory()->create(['email' => 'player@resort.test', 'password' => 'island-password']);

    $response = $this->postJson(route('api.v1.auth.login'), [
        'email' => 'player@resort.test',
        'password' => 'island-password',
        'device_name' => 'iPad',
    ])
        ->assertOk()
        ->assertJsonPath('data.id', $player->id);

    expect($response->json('token'))->toBeString()
        ->and($player->tokens()->pluck('name')->all())->toBe(['iPad']);
});

test('sign in with a wrong password fails', function () {
    User::factory()->create(['email' => 'player@resort.test', 'password' => 'island-password']);

    $this->postJson(route('api.v1.auth.login'), [
        'email' => 'player@resort.test',
        'password' => 'wrong-password',
    ])->assertJsonValidationErrorFor('email');
});

test('sign in attempts are rate limited', function () {
    User::factory()->create(['email' => 'player@resort.test', 'password' => 'island-password']);

    foreach (range(1, 5) as $attempt) {
        $this->postJson(route('api.v1.auth.login'), ['email' => 'player@resort.test', 'password' => 'wrong-password']);
    }

    $this->postJson(route('api.v1.auth.login'), ['email' => 'player@resort.test', 'password' => 'island-password'])
        ->assertTooManyRequests();
});

test('banned player cannot sign in', function () {
    User::factory()->banned('Cheating')->create(['email' => 'player@resort.test', 'password' => 'island-password']);

    $this->postJson(route('api.v1.auth.login'), ['email' => 'player@resort.test', 'password' => 'island-password'])
        ->assertForbidden()
        ->assertJson(['message' => 'This account is banned.', 'reason' => 'Cheating']);
});

test('banned player cannot use the game API until unbanned', function () {
    $player = User::factory()->anonymous()->create();
    $token = $player->createToken('game')->plainTextToken;
    $player->ban('Cheating');

    $this->withToken($token)->getJson(route('api.v1.me'))
        ->assertForbidden()
        ->assertJsonPath('reason', 'Cheating');

    $player->unban();
    app('auth')->forgetGuards();

    $this->withToken($token)->getJson(route('api.v1.me'))->assertOk();
});

test('logout revokes only the current token', function () {
    $player = User::factory()->anonymous()->create();
    $phoneToken = $player->createToken('phone')->plainTextToken;
    $player->createToken('tablet');

    $this->withToken($phoneToken)->postJson(route('api.v1.auth.logout'))->assertNoContent();

    expect($player->tokens()->pluck('name')->all())->toBe(['tablet']);
});
