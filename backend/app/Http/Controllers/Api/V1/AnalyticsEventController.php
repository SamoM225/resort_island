<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\V1\StoreAnalyticsEventsRequest;
use App\Models\AnalyticsEvent;
use Illuminate\Http\JsonResponse;
use Illuminate\Support\Carbon;
use stdClass;

class AnalyticsEventController extends Controller
{
    /**
     * Store a batch of analytics events of the signed-in player in one insert.
     */
    public function __invoke(StoreAnalyticsEventsRequest $request): JsonResponse
    {
        $receivedAt = now();

        $rows = collect($request->validated('events'))->map(fn (array $event): array => [
            'user_id' => $request->user()->id,
            'name' => $event['name'],
            'payload' => json_encode(($event['payload'] ?? []) ?: new stdClass),
            'session_id' => $event['session_id'] ?? null,
            'app_version' => $request->validated('app_version'),
            'platform' => $request->validated('platform'),
            'occurred_at' => Carbon::parse($event['occurred_at'])->utc(),
            'created_at' => $receivedAt,
        ]);

        AnalyticsEvent::insert($rows->all());

        return response()->json(['accepted' => $rows->count()], 202);
    }
}
