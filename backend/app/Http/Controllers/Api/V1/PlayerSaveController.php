<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\V1\UpdatePlayerSaveRequest;
use App\Http\Resources\PlayerSaveResource;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Carbon;

class PlayerSaveController extends Controller
{
    /**
     * The backed up save of the signed-in player, used to restore progress on a new device.
     */
    public function show(Request $request): PlayerSaveResource
    {
        $save = $request->user()->gameSave()->first();

        abort_if($save === null, 404, 'No save backed up yet.');

        return PlayerSaveResource::make($save);
    }

    /**
     * Back up the game's save. A save older than the stored one (another device played since) is refused
     * with 409 and the stored save, so a stale device never overwrites newer progress.
     */
    public function update(UpdatePlayerSaveRequest $request): PlayerSaveResource|JsonResponse
    {
        $player = $request->user();
        $savedAt = Carbon::parse($request->validated('saved_at'))->utc();

        if ($player->gameSave && $player->gameSave->saved_at->greaterThan($savedAt)) {
            return PlayerSaveResource::make($player->gameSave)
                ->additional(['message' => 'A newer save from another device is already backed up.'])
                ->response()
                ->setStatusCode(409);
        }

        $save = $player->gameSave()->updateOrCreate([], [
            'save_version' => $request->validated('save_version'),
            'data' => $request->validated('save'),
            'saved_at' => $savedAt,
        ]);

        return PlayerSaveResource::make($save);
    }
}
