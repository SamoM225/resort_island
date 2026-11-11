<?php

namespace App\Filament\Resources\Users\Schemas;

use App\Models\User;
use App\Models\WaveResult;
use App\Services\WaveRating;
use Filament\Infolists\Components\TextEntry;
use Filament\Infolists\Components\ViewEntry;
use Filament\Schemas\Components\Section;
use Filament\Schemas\Schema;

class UserInfolist
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                Section::make('Player')
                    ->columns(2)
                    ->schema([
                        TextEntry::make('id')->label('ID'),
                        TextEntry::make('display_name')->label('Name'),
                        TextEntry::make('email')->placeholder('Not linked'),
                        TextEntry::make('account')
                            ->state(fn (User $record): string => $record->isAnonymous() ? 'Anonymous' : 'Email linked')
                            ->badge()
                            ->color(fn (string $state): string => $state === 'Anonymous' ? 'gray' : 'info'),
                        TextEntry::make('created_at')->label('Registered')->dateTime(),
                        TextEntry::make('last_active')
                            ->state(fn (User $record) => $record->tokens()->max('last_used_at'))
                            ->since()
                            ->placeholder('Never'),
                    ]),
                Section::make('Ban')
                    ->columns(2)
                    ->visible(fn (User $record): bool => $record->isBanned())
                    ->schema([
                        TextEntry::make('banned_at')->label('Banned')->dateTime(),
                        TextEntry::make('ban_reason')->label('Reason'),
                    ]),
                Section::make('Save backup')
                    ->description('The last save the game uploaded.')
                    ->columns(3)
                    ->visible(fn (User $record): bool => $record->gameSave !== null)
                    ->schema([
                        TextEntry::make('gameSave.saved_at')->label('Saved in the game')->dateTime(),
                        TextEntry::make('gameSave.updated_at')->label('Uploaded')->since(),
                        TextEntry::make('gameSave.save_version')->label('Save format')->formatStateUsing(fn (int $state): string => "v{$state}"),
                        TextEntry::make('save_coins')
                            ->label('Coins')
                            ->state(fn (User $record): mixed => data_get($record->gameSave?->data, 'economy.coins'))
                            ->numeric()
                            ->placeholder('—'),
                        TextEntry::make('save_gems')
                            ->label('Gems')
                            ->state(fn (User $record): mixed => data_get($record->gameSave?->data, 'economy.gems'))
                            ->numeric()
                            ->placeholder('—'),
                        TextEntry::make('save_buildings')
                            ->label('Buildings')
                            ->state(fn (User $record): int => count(data_get($record->gameSave?->data, 'resort.buildings', []))),
                        TextEntry::make('save_coins_check')
                            ->label('Coins check')
                            ->columnSpan(2)
                            ->state(fn (User $record): string => $record->gameSave->coinsExceedEarned()
                                ? 'More coins than earned (at most '.number_format($record->gameSave->earnedCoins()).': starting coins + rated waves)'
                                : 'OK (at most '.number_format($record->gameSave->earnedCoins()).' earned)')
                            ->badge()
                            ->color(fn (User $record): string => $record->gameSave->coinsExceedEarned() ? 'danger' : 'success'),
                        ViewEntry::make('gameSave.data')
                            ->label('Save')
                            ->columnSpanFull()
                            ->view('filament.entries.json-tree'),
                    ]),
                Section::make('Waves')
                    ->description('Finished waves, rated again by the backend from the reported waits.')
                    ->visible(fn (User $record): bool => $record->waveResults()->exists())
                    ->schema([
                        TextEntry::make('waves_played')
                            ->label('Last 10')
                            ->state(fn (User $record): array => $record->waveResults()->latest('id')->limit(10)->get()
                                ->map(fn (WaveResult $result): string => sprintf(
                                    'Wave %d · %s · %.1f %% · %s coins%s',
                                    $result->wave,
                                    $result->rating === WaveRating::PERFECT ? 'Perfect' : "{$result->rating}★",
                                    $result->satisfaction * 100,
                                    number_format($result->coins),
                                    $result->matchesClient() === false ? " (game said {$result->client_coins})" : '',
                                ))
                                ->all())
                            ->listWithLineBreaks()
                            ->bulleted(),
                    ]),
            ]);
    }
}
