<?php

namespace Database\Seeders;

use App\Models\GameConfig;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\File;
use RuntimeException;

/**
 * Publishes every config in database/game-configs/{key}.json. Safe to run on every deploy:
 * a new version is inserted, an already published version is left untouched.
 */
class GameConfigSeeder extends Seeder
{
    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        foreach (File::glob(database_path('game-configs/*.json')) as $path) {
            $key = pathinfo($path, PATHINFO_FILENAME);
            $file = File::json($path, JSON_THROW_ON_ERROR);

            if (! is_int($file['version'] ?? null) || $file['version'] < 1 || ! is_array($file['data'] ?? null)) {
                throw new RuntimeException("Game config [{$key}] must contain a positive integer \"version\" and a \"data\" object.");
            }

            $config = GameConfig::firstOrCreate(
                ['key' => $key, 'version' => $file['version']],
                ['data' => $file['data']],
            );

            if (! $config->wasRecentlyCreated && $config->data != $file['data']) {
                throw new RuntimeException("Game config [{$key}] v{$file['version']} is already published with different data. Bump its version instead of editing it.");
            }
        }
    }
}
