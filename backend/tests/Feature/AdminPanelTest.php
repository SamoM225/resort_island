<?php

use App\Models\Admin;
use App\Models\User;
use Filament\Auth\Pages\Login;
use Livewire\Livewire;

test('guest is sent to the admin login', function () {
    $this->get('/admin')->assertRedirect(route('filament.admin.auth.login'));
});

test('admin can open the dashboard', function () {
    $this->actingAs(Admin::factory()->create(), 'admin')
        ->get('/admin')
        ->assertOk()
        ->assertSee('Resort Island');
});

test('signed in player cannot open the admin panel', function () {
    $this->actingAs(User::factory()->create())
        ->get('/admin')
        ->assertRedirect(route('filament.admin.auth.login'));
});

test('admin signs in with email and password', function () {
    $admin = Admin::factory()->create(['email' => 'admin@resort.test']);

    Livewire::test(Login::class)
        ->fillForm(['email' => 'admin@resort.test', 'password' => 'password'])
        ->call('authenticate')
        ->assertHasNoFormErrors();

    $this->assertAuthenticatedAs($admin, 'admin');
});

test('player credentials do not sign in to the admin panel', function () {
    User::factory()->create(['email' => 'player@resort.test']);

    Livewire::test(Login::class)
        ->fillForm(['email' => 'player@resort.test', 'password' => 'password'])
        ->call('authenticate')
        ->assertHasFormErrors(['email']);

    $this->assertGuest('admin');
});
