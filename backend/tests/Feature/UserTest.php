<?php

use App\Models\User;
use Illuminate\Database\UniqueConstraintViolationException;
use Illuminate\Support\Facades\Hash;

test('user password is stored hashed', function () {
    $user = User::factory()->create(['password' => 'secret-password']);

    expect($user->getRawOriginal('password'))->not->toBe('secret-password')
        ->and(Hash::check('secret-password', $user->getRawOriginal('password')))->toBeTrue();
});

test('two users cannot share an email', function () {
    User::factory()->create(['email' => 'player@example.com']);
    User::factory()->create(['email' => 'player@example.com']);
})->throws(UniqueConstraintViolationException::class);
