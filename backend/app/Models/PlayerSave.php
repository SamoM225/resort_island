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

    public function savedCoins(): ?int
    {
        $coins = data_get($this->data, 'economy.coins');

        return is_int($coins) ? $coins : null;
    }

    /**
     * The most coins the player can have: starting coins plus the coins of every wave the backend rated.
     * Spending only lowers it, so a save with more coins was not earned by playing.
     */
    public function earnedCoins(): int
    {
        $startingCoins = (int) data_get(GameConfig::query()->latestVersions()->where('key', 'economy')->value('data'), 'starting_coins', 0);

        return $startingCoins + (int) $this->user->waveResults()->sum('coins');
    }

    public function coinsExceedEarned(): bool
    {
        return $this->savedCoins() !== null && $this->savedCoins() > $this->earnedCoins();
    }

    public function savedEnergy(): ?int
    {
        $energy = data_get($this->data, 'economy.energy');

        return is_int($energy) ? $energy : null;
    }

    /**
     * The most energy the player can have: starting energy plus the energy of every wave the backend rated,
     * never above max_energy (the game caps every reward). Boosts only lower it.
     */
    public function earnedEnergy(): int
    {
        $economy = GameConfig::query()->latestVersions()->where('key', 'economy')->value('data');
        $earned = (int) data_get($economy, 'starting_energy', 0) + (int) $this->user->waveResults()->sum('energy');
        $maxEnergy = data_get($economy, 'max_energy');

        return is_int($maxEnergy) ? min($earned, $maxEnergy) : $earned;
    }

    public function energyExceedsEarned(): bool
    {
        return $this->savedEnergy() !== null && $this->savedEnergy() > $this->earnedEnergy();
    }

    /**
     * @return BelongsTo<User, $this>
     */
    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }
}
