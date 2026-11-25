<?php

namespace App\Models;

use Database\Factories\WaveResultFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

/**
 * A wave the game finished, rated again by the backend from the reported waits (App\Services\WaveRating).
 * Its coins and energy are what the player was entitled to; client_coins and client_energy are what
 * the game said it paid out.
 */
#[Fillable(['wave', 'units', 'satisfaction', 'rating', 'coins', 'energy', 'client_coins', 'client_energy'])]
class WaveResult extends Model
{
    /** @use HasFactory<WaveResultFactory> */
    use HasFactory;

    public const UPDATED_AT = null;

    /**
     * Get the attributes that should be cast.
     *
     * @return array<string, string>
     */
    protected function casts(): array
    {
        return [
            'wave' => 'integer',
            'units' => 'integer',
            'satisfaction' => 'float',
            'rating' => 'integer',
            'coins' => 'integer',
            'energy' => 'integer',
            'client_coins' => 'integer',
            'client_energy' => 'integer',
        ];
    }

    /**
     * Null when the game did not send its own result. Games from before the energy reward send no energy.
     */
    public function matchesClient(): ?bool
    {
        if ($this->client_coins === null) {
            return null;
        }

        return $this->client_coins === $this->coins
            && ($this->client_energy === null || $this->client_energy === $this->energy);
    }

    /**
     * @return BelongsTo<User, $this>
     */
    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }
}
