<?php

namespace App\Filament\Resources\GameConfigs\Pages;

use App\Filament\Resources\GameConfigs\Actions\DownloadConfigAction;
use App\Filament\Resources\GameConfigs\GameConfigResource;
use Filament\Actions\EditAction;
use Filament\Resources\Pages\ViewRecord;

class ViewGameConfig extends ViewRecord
{
    protected static string $resource = GameConfigResource::class;

    protected function getHeaderActions(): array
    {
        return [
            EditAction::make(),
            DownloadConfigAction::make(),
        ];
    }
}
