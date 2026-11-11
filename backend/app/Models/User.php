<?php

namespace App\Models;

// use Illuminate\Contracts\Auth\MustVerifyEmail;
use Database\Factories\UserFactory;
use Illuminate\Database\Eloquent\Attributes\Fillable;
use Illuminate\Database\Eloquent\Attributes\Hidden;
use Illuminate\Database\Eloquent\Casts\Attribute;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;
use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;
use Laravel\Sanctum\HasApiTokens;

/**
 * A player. The game registers it anonymously (no name, email or password);
 * linking an email later lets the player sign in to the same account on another device.
 */
#[Fillable(['name', 'email', 'password'])]
#[Hidden(['password', 'remember_token'])]
class User extends Authenticatable
{
    /** @use HasFactory<UserFactory> */
    use HasApiTokens, HasFactory, Notifiable;

    /**
     * Get the attributes that should be cast.
     *
     * @return array<string, string>
     */
    protected function casts(): array
    {
        return [
            'email_verified_at' => 'datetime',
            'password' => 'hashed',
            'banned_at' => 'datetime',
        ];
    }

    /**
     * Backup of the game's save. Not named save(), that is Eloquent's save.
     *
     * @return HasOne<PlayerSave, $this>
     */
    public function gameSave(): HasOne
    {
        return $this->hasOne(PlayerSave::class);
    }

    /**
     * Finished waves, rated by the backend.
     *
     * @return HasMany<WaveResult, $this>
     */
    public function waveResults(): HasMany
    {
        return $this->hasMany(WaveResult::class);
    }

    public function isAnonymous(): bool
    {
        return $this->email === null;
    }

    public function isBanned(): bool
    {
        return $this->banned_at !== null;
    }

    public function ban(string $reason): void
    {
        $this->forceFill(['banned_at' => now(), 'ban_reason' => $reason])->save();
    }

    public function unban(): void
    {
        $this->forceFill(['banned_at' => null, 'ban_reason' => null])->save();
    }

    /**
     * Name shown in the admin panel; anonymous players have none.
     */
    protected function displayName(): Attribute
    {
        return Attribute::get(fn (): string => $this->name ?? "Player #{$this->id}");
    }
}
