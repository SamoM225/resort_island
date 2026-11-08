<?php

namespace App\Filament\Resources\Users\Actions;

use App\Models\User;
use Filament\Actions\Action;
use Filament\Support\Icons\Heroicon;

class UnbanPlayerAction extends Action
{
    public static function getDefaultName(): ?string
    {
        return 'unban';
    }

    protected function setUp(): void
    {
        parent::setUp();

        $this->label('Unban');
        $this->color('success');
        $this->icon(Heroicon::OutlinedCheckCircle);
        $this->requiresConfirmation();
        $this->modalHeading(fn (User $record): string => "Unban {$record->display_name}");
        $this->modalDescription('The player can use the game again with their existing devices.');
        $this->visible(fn (User $record): bool => $record->isBanned());
        $this->successNotificationTitle('Player unbanned');
        $this->action(function (User $record): void {
            $record->unban();
            $this->success();
        });
    }
}
