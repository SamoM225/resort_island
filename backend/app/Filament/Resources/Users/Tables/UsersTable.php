<?php

namespace App\Filament\Resources\Users\Tables;

use App\Filament\Resources\Users\Actions\BanPlayerAction;
use App\Filament\Resources\Users\Actions\UnbanPlayerAction;
use App\Models\User;
use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\TernaryFilter;
use Filament\Tables\Table;
use Illuminate\Database\Eloquent\Builder;

class UsersTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->modifyQueryUsing(fn (Builder $query) => $query->withMax('tokens', 'last_used_at'))
            ->defaultSort('created_at', 'desc')
            ->columns([
                TextColumn::make('id')
                    ->label('ID')
                    ->sortable(),
                TextColumn::make('name')
                    ->label('Player')
                    ->state(fn (User $record): string => $record->display_name)
                    ->searchable(),
                TextColumn::make('email')
                    ->placeholder('—')
                    ->searchable(),
                TextColumn::make('account')
                    ->state(fn (User $record): string => $record->isAnonymous() ? 'Anonymous' : 'Email linked')
                    ->badge()
                    ->color(fn (string $state): string => $state === 'Anonymous' ? 'gray' : 'info'),
                TextColumn::make('status')
                    ->state(fn (User $record): string => $record->isBanned() ? 'Banned' : 'Active')
                    ->badge()
                    ->color(fn (string $state): string => $state === 'Banned' ? 'danger' : 'success'),
                TextColumn::make('tokens_max_last_used_at')
                    ->label('Last active')
                    ->since()
                    ->placeholder('Never')
                    ->sortable(),
                TextColumn::make('created_at')
                    ->label('Registered')
                    ->dateTime()
                    ->sortable(),
            ])
            ->filters([
                TernaryFilter::make('banned_at')
                    ->label('Banned')
                    ->nullable(),
                TernaryFilter::make('email')
                    ->label('Email linked')
                    ->nullable(),
            ])
            ->recordActions([
                ViewAction::make(),
                BanPlayerAction::make(),
                UnbanPlayerAction::make(),
            ]);
    }
}
