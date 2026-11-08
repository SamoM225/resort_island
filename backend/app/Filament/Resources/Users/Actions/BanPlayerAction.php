<?php

namespace App\Filament\Resources\Users\Actions;

use App\Models\User;
use Filament\Actions\Action;
use Filament\Forms\Components\Textarea;
use Filament\Support\Icons\Heroicon;

class BanPlayerAction extends Action
{
    public static function getDefaultName(): ?string
    {
        return 'ban';
    }

    protected function setUp(): void
    {
        parent::setUp();

        $this->label('Ban');
        $this->color('danger');
        $this->icon(Heroicon::OutlinedNoSymbol);
        $this->modalHeading(fn (User $record): string => "Ban {$record->display_name}");
        $this->modalDescription('The game API answers 403 with this reason until the player is unbanned. The account and its progress stay.');
        $this->modalSubmitActionLabel('Ban');
        $this->schema([
            Textarea::make('reason')
                ->label('Reason')
                ->helperText('The player sees it in the game.')
                ->required()
                ->maxLength(255),
        ]);
        $this->visible(fn (User $record): bool => ! $record->isBanned());
        $this->successNotificationTitle('Player banned');
        $this->action(function (User $record, array $data): void {
            $record->ban($data['reason']);
            $this->success();
        });
    }
}
