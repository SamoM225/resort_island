<?php

namespace App\Filament\Resources\GameConfigs\Tables;

use App\Filament\Resources\GameConfigs\Actions\DownloadConfigAction;
use Filament\Actions\EditAction;
use Filament\Actions\ViewAction;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;
use Illuminate\Database\Eloquent\Builder;

class GameConfigsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->modifyQueryUsing(fn (Builder $query) => $query->latestVersions())
            ->defaultSort('key')
            ->paginated(false)
            ->columns([
                TextColumn::make('key')
                    ->searchable()
                    ->sortable()
                    ->weight('medium'),
                TextColumn::make('version')
                    ->badge()
                    ->formatStateUsing(fn (int $state): string => "v{$state}"),
                TextColumn::make('updated_at')
                    ->label('Last change')
                    ->since()
                    ->sortable(),
                TextColumn::make('created_at')
                    ->label('Version published')
                    ->dateTime()
                    ->toggleable(isToggledHiddenByDefault: true),
            ])
            ->recordActions([
                ViewAction::make(),
                EditAction::make(),
                DownloadConfigAction::make(),
            ]);
    }
}
