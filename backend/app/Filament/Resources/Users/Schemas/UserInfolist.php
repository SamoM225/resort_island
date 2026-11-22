<?php

namespace App\Filament\Resources\Users\Schemas;

use App\Models\User;
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
                        ViewEntry::make('gameSave.data')
                            ->label('Save')
                            ->columnSpanFull()
                            ->view('filament.entries.json-tree'),
                    ]),
            ]);
    }
}
