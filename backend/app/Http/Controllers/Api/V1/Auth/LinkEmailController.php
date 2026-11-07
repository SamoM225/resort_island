<?php

namespace App\Http\Controllers\Api\V1\Auth;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\V1\LinkEmailRequest;
use App\Http\Resources\PlayerResource;

class LinkEmailController extends Controller
{
    /**
     * Turn the anonymous player into an account with email and password, keeping its progress.
     */
    public function __invoke(LinkEmailRequest $request): PlayerResource
    {
        $player = $request->user();

        abort_unless($player->isAnonymous(), 409, 'This account already has an email.');

        $player->update($request->validated());

        return PlayerResource::make($player);
    }
}
