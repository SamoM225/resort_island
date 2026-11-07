<?php

namespace App\Http\Controllers\Api\V1\Auth;

use App\Http\Controllers\Controller;
use App\Http\Resources\PlayerResource;
use App\Models\User;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class AnonymousRegistrationController extends Controller
{
    /**
     * Create an anonymous player on the game's first launch and return its API token.
     */
    public function __invoke(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'device_name' => ['nullable', 'string', 'max:100'],
        ]);

        $player = User::create();
        $token = $player->createToken($validated['device_name'] ?? 'game')->plainTextToken;

        return PlayerResource::make($player)
            ->additional(['token' => $token])
            ->response()
            ->setStatusCode(201);
    }
}
