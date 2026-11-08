<?php

namespace App\Filament\Resources\Users\Schemas;

use App\Models\User;
use Filament\Infolists\Components\TextEntry;
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
            ]);
    }
}
