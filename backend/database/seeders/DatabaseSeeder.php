<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;

class DatabaseSeeder extends Seeder
{
    /**
     * Seed the application's database. Runs on every deploy, so every seeder here must be idempotent.
     * Model events stay on: publishing a config has to clear the cached configs response.
     */
    public function run(): void
    {
        $this->call(GameConfigSeeder::class);

        if (app()->environment(['local', 'testing'])) {
            $this->call(UserSeeder::class);
        }
    }
}
