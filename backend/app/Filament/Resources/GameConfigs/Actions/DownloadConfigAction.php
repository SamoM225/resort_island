<?php

namespace App\Filament\Resources\GameConfigs\Actions;

use App\Models\GameConfig;
use Filament\Actions\Action;
use Filament\Support\Icons\Heroicon;
use Symfony\Component\HttpFoundation\StreamedResponse;

/**
 * Downloads one config as {key}.json in the format of database/game-configs, so it can go straight into the repo.
 */
class DownloadConfigAction extends Action
{
    public static function getDefaultName(): ?string
    {
        return 'download';
    }

    protected function setUp(): void
    {
        parent::setUp();

        $this->label('Download');
        $this->icon(Heroicon::OutlinedArrowDownTray);
        $this->color('gray');
        $this->action(fn (GameConfig $record): StreamedResponse => response()->streamDownload(
            function () use ($record): void {
                echo json_encode(
                    ['version' => $record->version, 'data' => $record->data],
                    JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PRESERVE_ZERO_FRACTION,
                );
            },
            "{$record->key}.json",
            ['Content-Type' => 'application/json'],
        ));
    }
}
