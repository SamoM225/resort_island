<?php

namespace App\Http\Controllers\Api\V1\Auth;

use App\Exceptions\PlayerBannedException;
use App\Http\Controllers\Controller;
use App\Http\Requests\Api\V1\LoginRequest;
use App\Http\Resources\PlayerResource;
use App\Models\User;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\ValidationException;

class LoginController extends Controller
{
    /**
     * Sign in to a player account with a linked email, e.g. on a new device, and return a new API token.
     */
    public function __invoke(LoginRequest $request): PlayerResource
    {
        $player = User::query()->where('email', $request->validated('email'))->first();

        if (! $player || ! Hash::check($request->validated('password'), $player->password)) {
            throw ValidationException::withMessages(['email' => __('auth.failed')]);
        }

        if ($player->isBanned()) {
            throw new PlayerBannedException($player->ban_reason);
        }

        $token = $player->createToken($request->validated('device_name') ?? 'game')->plainTextToken;

        return PlayerResource::make($player)->additional(['token' => $token]);
    }
}
