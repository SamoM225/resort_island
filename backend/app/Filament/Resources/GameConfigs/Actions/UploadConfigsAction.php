<?php

namespace App\Filament\Resources\GameConfigs\Actions;

use App\Filament\Resources\GameConfigs\GameConfigResource;
use App\Models\GameConfig;
use Filament\Actions\Action;
use Filament\Forms\Components\FileUpload;
use Filament\Notifications\Notification;
use Filament\Support\Icons\Heroicon;
use Illuminate\Http\UploadedFile;
use Illuminate\Support\Arr;
use Illuminate\Support\HtmlString;
use Illuminate\Validation\ValidationException;

/**
 * Publishes uploaded configs: {key}.json files from "Download" or one file from "Download all".
 * The upload is all or nothing — if any config is rejected, none is published.
 */
class UploadConfigsAction extends Action
{
    public static function getDefaultName(): ?string
    {
        return 'upload';
    }

    protected function setUp(): void
    {
        parent::setUp();

        $this->label('Upload');
        $this->icon(Heroicon::OutlinedArrowUpTray);
        $this->modalHeading('Upload configs');
        $this->modalDescription(fn (): string => GameConfigResource::modeDescription().' Nothing is published unless every config is accepted.');
        $this->modalSubmitActionLabel('Publish');
        $this->schema([
            FileUpload::make('files')
                ->label('Config files')
                ->helperText('{key}.json files ("version" and "data", as from Download) or the file from "Download all".')
                ->multiple()
                ->acceptedFileTypes(['application/json', 'text/plain'])
                ->maxSize(1024)
                ->storeFiles(false)
                ->required(),
        ]);
        $this->action(function (array $data): void {
            try {
                $results = GameConfig::publishMany(static::configsFromFiles($data['files']));
            } catch (ValidationException $exception) {
                Notification::make()
                    ->danger()
                    ->title('Nothing was published')
                    ->body(static::lines(Arr::flatten($exception->errors())))
                    ->persistent()
                    ->send();

                $this->halt();
            }

            Notification::make()
                ->success()
                ->title('Configs uploaded')
                ->body(static::lines(collect($results)->map(fn (array $result, string $key): string => match ($result['status']) {
                    'created' => "{$key}: published v{$result['version']}",
                    'updated' => "{$key}: v{$result['version']} overwritten",
                    default => "{$key}: unchanged",
                })->all()))
                ->send();
        });
    }

    /**
     * @param  array<UploadedFile>  $files
     * @return array<string, array<array-key, mixed>> config key => data
     *
     * @throws ValidationException messages keyed by the file name
     */
    public static function configsFromFiles(array $files): array
    {
        $configs = [];
        $errors = [];

        foreach ($files as $file) {
            $name = $file->getClientOriginalName();
            $contents = json_decode($file->get(), true);

            if (! is_array($contents) || ! is_array($contents['data'] ?? null)) {
                $errors[$name] = "{$name}: expected {\"version\": …, \"data\": {…}} or {\"data\": [{\"key\": …, \"data\": {…}}]}.";

                continue;
            }

            if (! array_is_list($contents['data'])) {
                $configs[pathinfo($name, PATHINFO_FILENAME)] = $contents['data'];

                continue;
            }

            foreach ($contents['data'] as $index => $config) {
                if (! is_string($config['key'] ?? null) || ! is_array($config['data'] ?? null)) {
                    $errors[$name] = "{$name}: item {$index} needs a \"key\" and a \"data\" object.";

                    continue 2;
                }

                $configs[$config['key']] = $config['data'];
            }
        }

        if ($errors !== []) {
            throw ValidationException::withMessages($errors);
        }

        return $configs;
    }

    /**
     * @param  list<string>  $lines
     */
    private static function lines(array $lines): HtmlString
    {
        return new HtmlString(implode('<br>', array_map(e(...), $lines)));
    }
}
