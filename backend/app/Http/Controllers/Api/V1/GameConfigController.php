<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Http\Resources\GameConfigResource;
use App\Models\GameConfig;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Cache;

class GameConfigController extends Controller
{
    /**
     * Latest version of every game config. The payload is cached server-side;
     * ETag / 304 handling comes from the route's cache.headers middleware.
     */
    public function __invoke(Request $request): JsonResponse
    {
        $configs = Cache::remember(
            GameConfig::CACHE_KEY,
            config('game.configs.cache_ttl'),
            fn (): array => GameConfigResource::collection(
                GameConfig::query()->latestVersions()->orderBy('key')->get()
            )->resolve($request),
        );

        return response()->json(['data' => $configs]);
    }
}
