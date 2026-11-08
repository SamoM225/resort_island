<?php

namespace App\Models;

use Database\Factories\GameConfigFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Attributes\Scope;
use Illuminate\Database\Eloquent\Builder;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Query\Builder as QueryBuilder;
use Illuminate\Support\Facades\Cache;

/**
 * One published version of a tunable game config (economy, waves, buildings…).
 * Versions are append-only: a change is published as a new version, the client always gets the latest one.
 */
#[Fillable(['key', 'version', 'data'])]
class GameConfig extends Model
{
    /** @use HasFactory<GameConfigFactory> */
    use HasFactory;

    /**
     * Cache key of the latest-versions response, cleared whenever a config is written.
     */
    public const CACHE_KEY = 'game-configs.latest';

    /**
     * Get the attributes that should be cast.
     *
     * @return array<string, string>
     */
    protected function casts(): array
    {
        return [
            'version' => 'integer',
            'data' => 'array',
        ];
    }

    protected static function booted(): void
    {
        static::saved(fn () => Cache::forget(self::CACHE_KEY));
        static::deleted(fn () => Cache::forget(self::CACHE_KEY));
    }

    /**
     * Keep only the highest version of each config key.
     *
     * @param  Builder<GameConfig>  $query
     */
    #[Scope]
    protected function latestVersions(Builder $query): void
    {
        $query->where('version', function (QueryBuilder $latest) {
            $latest->selectRaw('max(version)')
                ->from('game_configs as latest')
                ->whereColumn('latest.key', 'game_configs.key');
        });
    }
}
