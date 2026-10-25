<?php

namespace App\Models;

use Database\Factories\GameConfigFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

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
}
