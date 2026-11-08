<?php

namespace App\Filament\Resources\AnalyticsEvents\Schemas;

use App\Filament\Resources\Users\UserResource;
use App\Models\AnalyticsEvent;
use Filament\Infolists\Components\TextEntry;
use Filament\Infolists\Components\ViewEntry;
use Filament\Schemas\Components\Section;
use Filament\Schemas\Schema;

class AnalyticsEventInfolist
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                Section::make('Event')
                    ->columns(2)
                    ->schema([
                        TextEntry::make('name')->badge(),
                        TextEntry::make('user_id')
                            ->label('Player')
                            ->state(fn (AnalyticsEvent $record): string => $record->user?->display_name ?? 'Deleted player')
                            ->url(fn (AnalyticsEvent $record): ?string => $record->user_id ? UserResource::getUrl('view', ['record' => $record->user_id]) : null),
                        TextEntry::make('occurred_at')->label('Occurred (game clock)')->dateTime(),
                        TextEntry::make('created_at')->label('Received')->dateTime(),
                        TextEntry::make('session_id')->placeholder('—'),
                        TextEntry::make('platform')->placeholder('—'),
                        TextEntry::make('app_version')->placeholder('—'),
                    ]),
                Section::make('Payload')
                    ->schema([
                        ViewEntry::make('payload')
                            ->hiddenLabel()
                            ->view('filament.entries.json-tree'),
                    ]),
            ]);
    }
}
