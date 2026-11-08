<?php

namespace App\Filament\Resources\GameConfigs;

use App\Filament\Resources\GameConfigs\Pages\EditGameConfig;
use App\Filament\Resources\GameConfigs\Pages\ListGameConfigs;
use App\Filament\Resources\GameConfigs\Pages\ViewGameConfig;
use App\Filament\Resources\GameConfigs\Schemas\GameConfigForm;
use App\Filament\Resources\GameConfigs\Schemas\GameConfigInfolist;
use App\Filament\Resources\GameConfigs\Tables\GameConfigsTable;
use App\Models\GameConfig;
use BackedEnum;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Table;
use Illuminate\Database\Eloquent\Model;

/**
 * Game configs served to the game. The list shows the latest version of each config;
 * how a change is published depends on the environment (GameConfig::freeEditing()).
 */
class GameConfigResource extends Resource
{
    protected static ?string $model = GameConfig::class;

    protected static string|BackedEnum|null $navigationIcon = Heroicon::OutlinedCog6Tooth;

    protected static ?int $navigationSort = 3;

    public static function getRecordTitle(?Model $record): string
    {
        return $record ? "{$record->key} v{$record->version}" : static::getModelLabel();
    }

    public static function modeDescription(): string
    {
        return GameConfig::freeEditing()
            ? 'Development: a change overwrites the current version and anything can change.'
            : 'Production: only numbers can change, and every change publishes a new version.';
    }

    /**
     * Only the latest version of a config can be changed; older ones are history.
     */
    public static function canEdit(Model $record): bool
    {
        return ! GameConfig::query()
            ->where('key', $record->key)
            ->where('version', '>', $record->version)
            ->exists();
    }

    public static function form(Schema $schema): Schema
    {
        return GameConfigForm::configure($schema);
    }

    public static function infolist(Schema $schema): Schema
    {
        return GameConfigInfolist::configure($schema);
    }

    public static function table(Table $table): Table
    {
        return GameConfigsTable::configure($table);
    }

    public static function getPages(): array
    {
        return [
            'index' => ListGameConfigs::route('/'),
            'view' => ViewGameConfig::route('/{record}'),
            'edit' => EditGameConfig::route('/{record}/edit'),
        ];
    }
}
