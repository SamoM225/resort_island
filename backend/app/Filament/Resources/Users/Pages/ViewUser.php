<?php

namespace App\Filament\Resources\Users\Pages;

use App\Filament\Resources\Users\Actions\BanPlayerAction;
use App\Filament\Resources\Users\Actions\UnbanPlayerAction;
use App\Filament\Resources\Users\UserResource;
use Filament\Resources\Pages\ViewRecord;

class ViewUser extends ViewRecord
{
    protected static string $resource = UserResource::class;

    protected function getHeaderActions(): array
    {
        return [
            BanPlayerAction::make(),
            UnbanPlayerAction::make(),
        ];
    }
}
