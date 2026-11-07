<?php

namespace App\Filament\Resources\GameConfigs\Pages;

use App\Filament\Resources\GameConfigs\Actions\DownloadAllConfigsAction;
use App\Filament\Resources\GameConfigs\Actions\UploadConfigsAction;
use App\Filament\Resources\GameConfigs\GameConfigResource;
use Filament\Resources\Pages\ListRecords;

class ListGameConfigs extends ListRecords
{
    protected static string $resource = GameConfigResource::class;

    public function getSubheading(): string
    {
        return GameConfigResource::modeDescription();
    }

    protected function getHeaderActions(): array
    {
        return [
            DownloadAllConfigsAction::make(),
            UploadConfigsAction::make(),
        ];
    }
}
