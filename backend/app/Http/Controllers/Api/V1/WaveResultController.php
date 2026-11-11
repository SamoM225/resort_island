<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\V1\StoreWaveResultRequest;
use App\Http\Resources\WaveResultResource;
use Illuminate\Http\JsonResponse;

class WaveResultController extends Controller
{
    /**
     * Rate a finished wave again from its waits with the current configs and keep the result.
     * The backend's coins are authoritative; the game's own result is kept to spot mismatches.
     */
    public function __invoke(StoreWaveResultRequest $request): JsonResponse
    {
        $result = $request->rating()->evaluate($request->validated('waits'));

        $waveResult = $request->user()->waveResults()->create([
            ...$result,
            'wave' => $request->validated('wave'),
            'client_coins' => $request->validated('client.coins'),
        ]);

        return WaveResultResource::make($waveResult)->response()->setStatusCode(201);
    }
}
