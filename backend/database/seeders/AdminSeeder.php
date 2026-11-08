<?php

namespace Database\Seeders;

use App\Models\Admin;
use Illuminate\Database\Seeder;

/**
 * Local admin panel account (admin@example.com / password). Never seeded on the dev or prod server —
 * admins there are created with `php artisan make:filament-user`.
 */
class AdminSeeder extends Seeder
{
    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        if (Admin::query()->where('email', 'admin@example.com')->exists()) {
            return;
        }

        Admin::factory()->create([
            'name' => 'Admin',
            'email' => 'admin@example.com',
        ]);
    }
}
