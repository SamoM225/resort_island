<?php

namespace Database\Seeders;

use App\Models\GameConfig;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\File;
use RuntimeException;

/**
 * Publishes the configs in database/game-configs/{key}.json: a key the database does not have yet,
 * or a file version newer than the database's. Safe to run on every deploy — configs changed
 * in the admin panel are never overwritten.
 */
class GameConfigSeeder extends Seeder
{
    /**
     * Directory with the config files, database/game-configs by default.
     */
    public ?string $directory = null;

    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        foreach (File::glob(($this->directory ?? database_path('game-configs')).'/*.json') as $path) {
            $key = pathinfo($path, PATHINFO_FILENAME);
            $file = File::json($path, JSON_THROW_ON_ERROR);

            if (! is_int($file['version'] ?? null) || $file['version'] < 1 || ! is_array($file['data'] ?? null)) {
                throw new RuntimeException("Game config [{$key}] must contain a positive integer \"version\" and a \"data\" object.");
            }

            $latestVersion = GameConfig::query()->where('key', $key)->max('version');

            if ($latestVersion === null || $file['version'] > $latestVersion) {
                GameConfig::create(['key' => $key, 'version' => $file['version'], 'data' => $file['data']]);

                continue;
            }

            $latest = GameConfig::query()->where('key', $key)->where('version', $latestVersion)->sole();

            if ($latest->data != $file['data']) {
                $this->command?->line("  Game config [{$key}]: kept database v{$latestVersion} (changed in the admin panel). To publish the file, set its version above {$latestVersion}.");
            }
        }
    }
}
