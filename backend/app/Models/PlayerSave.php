<?php

namespace App\Models;

use Database\Factories\PlayerSaveFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

/**
 * Backup of a player's local save (map, coins, gems…), uploaded by the game after it saves.
 * One per player; the game keeps playing offline and the backup restores progress on another device.
 */
#[Fillable(['save_version', 'data', 'saved_at'])]
class PlayerSave extends Model
{
    /** @use HasFactory<PlayerSaveFactory> */
    use HasFactory;

    /**
     * Get the attributes that should be cast.
     *
     * @return array<string, string>
     */
    protected function casts(): array
    {
        return [
            'save_version' => 'integer',
            'data' => 'array',
            'saved_at' => 'datetime',
        ];
    }

    /**
     * @return BelongsTo<User, $this>
     */
    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }
}
