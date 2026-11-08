<?php

use App\Models\AnalyticsEvent;
use App\Models\User;
use Laravel\Sanctum\Sanctum;

/**
 * @param  array<string, mixed>  $overrides
 * @return array<string, mixed>
 */
function analyticsEvent(array $overrides = []): array
{
    return [
        'name' => 'wave_complete',
        'occurred_at' => '2026-11-07T10:15:00+01:00',
        'session_id' => '7f3c2a8e-1d4b-4c6a-9e2f-0a1b2c3d4e5f',
        'payload' => ['wave' => 3, 'rating' => 4, 'drain_duration_s' => 21.5],
        ...$overrides,
    ];
}

test('game sends a batch of analytics events of the signed-in player', function () {
    $player = User::factory()->anonymous()->create();
    Sanctum::actingAs($player);

    $this->postJson(route('api.v1.analytics.events.store'), [
        'app_version' => '0.3.0',
        'platform' => 'android',
        'events' => [
            analyticsEvent(),
            analyticsEvent(['name' => 'worker_moved', 'payload' => ['phase' => 'in_wave', 'from' => 'restaurant', 'to' => 'beachbar']]),
        ],
    ])
        ->assertAccepted()
        ->assertExactJson(['accepted' => 2]);

    $event = AnalyticsEvent::query()->where('name', 'wave_complete')->sole();

    expect(AnalyticsEvent::count())->toBe(2)
        ->and($event->user_id)->toBe($player->id)
        ->and($event->payload)->toBe(['wave' => 3, 'rating' => 4, 'drain_duration_s' => 21.5])
        ->and($event->occurred_at->toIso8601String())->toBe('2026-11-07T09:15:00+00:00')
        ->and($event->app_version)->toBe('0.3.0')
        ->and($event->platform)->toBe('android');
});

test('event without payload is stored with an empty object', function () {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->postJson(route('api.v1.analytics.events.store'), [
        'events' => [analyticsEvent(['name' => 'session_start', 'payload' => null])],
    ])->assertAccepted();

    expect(AnalyticsEvent::sole()->getRawOriginal('payload'))->toBe('{}');
});

test('analytics need a player token', function () {
    $this->postJson(route('api.v1.analytics.events.store'), ['events' => [analyticsEvent()]])
        ->assertUnauthorized();

    expect(AnalyticsEvent::count())->toBe(0);
});

test('banned player cannot send analytics', function () {
    Sanctum::actingAs(User::factory()->banned()->create());

    $this->postJson(route('api.v1.analytics.events.store'), ['events' => [analyticsEvent()]])
        ->assertForbidden();

    expect(AnalyticsEvent::count())->toBe(0);
});

test('invalid event rejects the whole batch', function (array $event, string $error) {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->postJson(route('api.v1.analytics.events.store'), ['events' => [analyticsEvent(), $event]])
        ->assertJsonValidationErrorFor($error);

    expect(AnalyticsEvent::count())->toBe(0);
})->with([
    'name not snake_case' => [analyticsEvent(['name' => 'Wave Complete']), 'events.1.name'],
    'missing time' => [analyticsEvent(['occurred_at' => null]), 'events.1.occurred_at'],
    'session not a uuid' => [analyticsEvent(['session_id' => 'abc']), 'events.1.session_id'],
    'payload too large' => [analyticsEvent(['payload' => ['blob' => str_repeat('x', 5000)]]), 'events.1.payload'],
]);

test('batch is limited to 100 events', function () {
    Sanctum::actingAs(User::factory()->anonymous()->create());

    $this->postJson(route('api.v1.analytics.events.store'), ['events' => array_fill(0, 101, analyticsEvent())])
        ->assertJsonValidationErrorFor('events');
});
