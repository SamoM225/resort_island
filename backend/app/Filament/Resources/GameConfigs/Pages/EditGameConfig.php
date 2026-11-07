<?php

namespace App\Filament\Resources\GameConfigs\Pages;

use App\Filament\Resources\GameConfigs\GameConfigResource;
use App\Models\GameConfig;
use Filament\Actions\ViewAction;
use Filament\Resources\Pages\EditRecord;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Support\Arr;
use Illuminate\Validation\ValidationException;

/**
 * Edits the config as JSON. Saving goes through GameConfig::publish(), which either overwrites
 * the version (dev) or publishes a new one (prod).
 */
class EditGameConfig extends EditRecord
{
    protected static string $resource = GameConfigResource::class;

    private ?GameConfig $published = null;

    public function getSubheading(): string
    {
        return GameConfigResource::modeDescription();
    }

    protected function getHeaderActions(): array
    {
        return [
            ViewAction::make(),
        ];
    }

    /**
     * @param  array<string, mixed>  $data
     * @return array<string, mixed>
     */
    protected function mutateFormDataBeforeFill(array $data): array
    {
        return ['data' => json_encode($data['data'], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PRESERVE_ZERO_FRACTION)];
    }

    /**
     * @param  GameConfig  $record
     * @param  array{data: string}  $data
     */
    protected function handleRecordUpdate(Model $record, array $data): Model
    {
        try {
            $this->published = GameConfig::publish($record->key, json_decode($data['data'], true));
        } catch (ValidationException $exception) {
            throw ValidationException::withMessages(['data.data' => Arr::flatten($exception->errors())]);
        }

        return $this->published;
    }

    protected function getSavedNotificationTitle(): string
    {
        return match (true) {
            $this->published?->wasRecentlyCreated => "Published as v{$this->published->version}",
            $this->published?->wasChanged() => "Saved v{$this->published->version}",
            default => 'No changes',
        };
    }

    protected function getRedirectUrl(): string
    {
        return GameConfigResource::getUrl('view', ['record' => $this->published ?? $this->getRecord()]);
    }
}
