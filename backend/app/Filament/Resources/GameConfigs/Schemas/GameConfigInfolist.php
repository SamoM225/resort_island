<?php

namespace App\Filament\Resources\GameConfigs\Schemas;

use App\Models\GameConfig;
use Filament\Infolists\Components\TextEntry;
use Filament\Infolists\Components\ViewEntry;
use Filament\Schemas\Components\Section;
use Filament\Schemas\Schema;

class GameConfigInfolist
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->columns(3)
            ->components([
                Section::make('Data')
                    ->description('Click a key to expand it.')
                    ->columnSpan(2)
                    ->schema([
                        ViewEntry::make('data')
                            ->hiddenLabel()
                            ->view('filament.entries.json-tree'),
                    ]),
                Section::make('Version')
                    ->columnSpan(1)
                    ->schema([
                        TextEntry::make('key'),
                        TextEntry::make('version')->formatStateUsing(fn (int $state): string => "v{$state}"),
                        TextEntry::make('created_at')->label('Published')->dateTime(),
                        TextEntry::make('updated_at')->label('Last change')->dateTime(),
                        TextEntry::make('history')
                            ->state(fn (GameConfig $record): array => $record->versions
                                ->map(fn (GameConfig $version): string => "v{$version->version} · {$version->created_at->format('Y-m-d H:i')}")
                                ->all())
                            ->listWithLineBreaks()
                            ->bulleted(),
                    ]),
            ]);
    }
}
