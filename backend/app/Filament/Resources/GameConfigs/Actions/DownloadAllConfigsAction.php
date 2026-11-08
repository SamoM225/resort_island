<?php

namespace App\Filament\Resources\GameConfigs\Actions;

use App\Http\Resources\GameConfigResource;
use App\Models\GameConfig;
use Filament\Actions\Action;
use Filament\Support\Icons\Heroicon;
use Symfony\Component\HttpFoundation\StreamedResponse;

/**
 * Downloads the latest version of every config in the format of GET /api/v1/configs.
 * The file can be checked with `wave_sim.py --configs <file>` and uploaded back.
 */
class DownloadAllConfigsAction extends Action
{
    public static function getDefaultName(): ?string
    {
        return 'downloadAll';
    }

    protected function setUp(): void
    {
        parent::setUp();

        $this->label('Download all');
        $this->icon(Heroicon::OutlinedArrowDownTray);
        $this->color('gray');
        $this->action(fn (): StreamedResponse => response()->streamDownload(
            function (): void {
                $configs = GameConfigResource::collection(GameConfig::query()->latestVersions()->orderBy('key')->get())->resolve();

                echo json_encode(
                    ['data' => $configs],
                    JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PRESERVE_ZERO_FRACTION,
                );
            },
            'game-configs-'.app()->environment().'-'.now()->format('Y-m-d').'.json',
            ['Content-Type' => 'application/json'],
        ));
    }
}
