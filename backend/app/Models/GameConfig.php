<?php

namespace App\Models;

use App\Rules\SafeConfigChange;
use Database\Factories\GameConfigFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Attributes\Scope;
use Illuminate\Database\Eloquent\Builder;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Query\Builder as QueryBuilder;
use Illuminate\Support\Facades\Cache;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

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

    /**
     * Store 1.0 as 1.0, not 1: a decimal value has to stay decimal, SafeConfigChange relies on the type.
     */
    protected function getJsonCastFlags($key): int
    {
        return parent::getJsonCastFlags($key) | JSON_PRESERVE_ZERO_FRACTION;
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

    /**
     * All versions of this config, newest first.
     *
     * @return HasMany<GameConfig, $this>
     */
    public function versions(): HasMany
    {
        return $this->hasMany(self::class, 'key', 'key')->orderByDesc('version');
    }

    /**
     * Local and dev: changes overwrite the latest version and may change anything (config game.configs.free_editing).
     */
    public static function freeEditing(): bool
    {
        return (bool) config('game.configs.free_editing');
    }

    /**
     * Publish new data of a config from the admin panel. With free editing the latest version is overwritten;
     * otherwise only a SafeConfigChange is accepted and it becomes a new version. Unchanged data publishes nothing.
     *
     * @param  array<array-key, mixed>  $data
     *
     * @throws ValidationException messages keyed by the config key
     */
    public static function publish(string $key, array $data): self
    {
        return DB::transaction(function () use ($key, $data): self {
            $latest = static::query()->where('key', $key)->orderByDesc('version')->lockForUpdate()->first();

            if ($latest === null) {
                return static::publishNewKey($key, $data);
            }

            if (static::normalized($latest->data) === static::normalized($data)) {
                return $latest;
            }

            if (static::freeEditing()) {
                $latest->update(['data' => $data]);

                return $latest;
            }

            $violations = SafeConfigChange::violations($latest->data, $data);

            if ($violations !== []) {
                throw ValidationException::withMessages([$key => $violations]);
            }

            return static::create(['key' => $key, 'version' => $latest->version + 1, 'data' => $data]);
        });
    }

    /**
     * Publish several configs at once (an upload); if any of them is rejected, none is published.
     *
     * @param  array<string, array<array-key, mixed>>  $configs  config key => data
     * @return array<string, array{status: 'created'|'updated'|'unchanged', version: int}>
     *
     * @throws ValidationException messages keyed by the config key
     */
    public static function publishMany(array $configs): array
    {
        return DB::transaction(function () use ($configs): array {
            $results = [];
            $errors = [];

            foreach ($configs as $key => $data) {
                try {
                    $published = static::publish($key, $data);
                } catch (ValidationException $exception) {
                    $errors = [...$errors, ...$exception->errors()];

                    continue;
                }

                $results[$key] = [
                    'status' => match (true) {
                        $published->wasRecentlyCreated => 'created',
                        $published->wasChanged() => 'updated',
                        default => 'unchanged',
                    },
                    'version' => $published->version,
                ];
            }

            if ($errors !== []) {
                throw ValidationException::withMessages($errors);
            }

            return $results;
        });
    }

    /**
     * @param  array<array-key, mixed>  $data
     */
    private static function publishNewKey(string $key, array $data): self
    {
        if (! static::freeEditing()) {
            throw ValidationException::withMessages([$key => ["{$key}: new configs can only be added on dev, the game does not know them yet."]]);
        }

        if (! preg_match('/^[a-z][a-z0-9_]{0,63}$/', $key)) {
            throw ValidationException::withMessages([$key => ["{$key}: config keys are snake_case, e.g. wave_system."]]);
        }

        return static::create(['key' => $key, 'version' => 1, 'data' => $data]);
    }

    /**
     * Same data in a comparable form: object keys sorted, because the database may return them in another order.
     */
    private static function normalized(mixed $value): mixed
    {
        if (! is_array($value)) {
            return $value;
        }

        $value = array_map(static::normalized(...), $value);

        if (! array_is_list($value)) {
            ksort($value);
        }

        return $value;
    }
}
