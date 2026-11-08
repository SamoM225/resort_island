<?php

namespace App\Http\Controllers\Api\V1\Auth;

use App\Http\Controllers\Controller;
use App\Http\Resources\PlayerResource;
use Illuminate\Http\Request;

class CurrentPlayerController extends Controller
{
    /**
     * The player the token belongs to.
     */
    public function __invoke(Request $request): PlayerResource
    {
        return PlayerResource::make($request->user());
    }
}
