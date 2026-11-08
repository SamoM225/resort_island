<?php

namespace App\Filament\Resources\GameConfigs\Schemas;

use App\Models\GameConfig;
use App\Rules\SafeConfigChange;
use Closure;
use Filament\Forms\Components\CodeEditor;
use Filament\Forms\Components\CodeEditor\Enums\Language;
use Filament\Schemas\Schema;

class GameConfigForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->columns(1)
            ->components([
                CodeEditor::make('data')
                    ->label(fn (GameConfig $record): string => "{$record->key} (v{$record->version})")
                    ->helperText(fn (): string => GameConfig::freeEditing()
                        ? 'Saving overwrites the current version.'
                        : 'Only numbers can change. Saving publishes a new version.')
                    ->language(Language::Json)
                    ->required()
                    ->rules(fn (GameConfig $record): array => [
                        'bail',
                        'json',
                        function (string $attribute, mixed $value, Closure $fail): void {
                            $decoded = json_decode($value, true);

                            if (! is_array($decoded) || ($decoded !== [] && array_is_list($decoded))) {
                                $fail('The config must be a JSON object.');
                            }
                        },
                        ...(GameConfig::freeEditing() ? [] : [new SafeConfigChange($record->data)]),
                    ]),
            ]);
    }
}
