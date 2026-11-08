<?php

namespace App\Filament\Resources\AnalyticsEvents\Tables;

use App\Filament\Resources\Users\UserResource;
use App\Models\AnalyticsEvent;
use Filament\Actions\ViewAction;
use Filament\Forms\Components\DatePicker;
use Filament\Support\Enums\FontFamily;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\Filter;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;
use Illuminate\Database\Eloquent\Builder;

class AnalyticsEventsTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->modifyQueryUsing(fn (Builder $query) => $query->with('user'))
            ->defaultSort('occurred_at', 'desc')
            ->columns([
                TextColumn::make('occurred_at')
                    ->label('Occurred')
                    ->dateTime()
                    ->sortable(),
                TextColumn::make('name')
                    ->badge()
                    ->searchable(),
                TextColumn::make('user_id')
                    ->label('Player')
                    ->state(fn (AnalyticsEvent $record): string => $record->user?->display_name ?? 'Deleted player')
                    ->url(fn (AnalyticsEvent $record): ?string => $record->user_id ? UserResource::getUrl('view', ['record' => $record->user_id]) : null),
                TextColumn::make('payload')
                    ->state(fn (AnalyticsEvent $record): string => json_encode($record->payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES))
                    ->fontFamily(FontFamily::Mono)
                    ->limit(60),
                TextColumn::make('platform')
                    ->toggleable(isToggledHiddenByDefault: true),
                TextColumn::make('app_version')
                    ->toggleable(isToggledHiddenByDefault: true),
            ])
            ->filters([
                SelectFilter::make('name')
                    ->options(fn (): array => AnalyticsEvent::query()->distinct()->orderBy('name')->pluck('name', 'name')->all())
                    ->multiple(),
                Filter::make('occurred_at')
                    ->schema([
                        DatePicker::make('from'),
                        DatePicker::make('until'),
                    ])
                    ->query(fn (Builder $query, array $data): Builder => $query
                        ->when($data['from'] ?? null, fn (Builder $query, string $date) => $query->whereDate('occurred_at', '>=', $date))
                        ->when($data['until'] ?? null, fn (Builder $query, string $date) => $query->whereDate('occurred_at', '<=', $date))),
            ])
            ->recordActions([
                ViewAction::make(),
            ]);
    }
}
