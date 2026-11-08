<?php

use App\Filament\Resources\AnalyticsEvents\Pages\ListAnalyticsEvents;
use App\Filament\Resources\AnalyticsEvents\Pages\ViewAnalyticsEvent;
use App\Models\Admin;
use App\Models\AnalyticsEvent;
use Livewire\Livewire;

beforeEach(function () {
    $this->actingAs(Admin::factory()->create(), 'admin');
});

test('admin filters analytics events by name', function () {
    $waveComplete = AnalyticsEvent::factory()->create(['name' => 'wave_complete']);
    $workerMoved = AnalyticsEvent::factory()->create(['name' => 'worker_moved']);

    Livewire::test(ListAnalyticsEvents::class)
        ->assertCanSeeTableRecords([$waveComplete, $workerMoved])
        ->filterTable('name', ['worker_moved'])
        ->assertCanSeeTableRecords([$workerMoved])
        ->assertCanNotSeeTableRecords([$waveComplete]);
});

test('admin filters analytics events by day', function () {
    $older = AnalyticsEvent::factory()->create(['occurred_at' => '2026-11-01 12:00:00']);
    $newer = AnalyticsEvent::factory()->create(['occurred_at' => '2026-11-06 12:00:00']);

    Livewire::test(ListAnalyticsEvents::class)
        ->filterTable('occurred_at', ['from' => '2026-11-05', 'until' => null])
        ->assertCanSeeTableRecords([$newer])
        ->assertCanNotSeeTableRecords([$older]);
});

test('admin sees the payload of an event', function () {
    $event = AnalyticsEvent::factory()->create([
        'payload' => ['phase' => 'in_wave', 'from' => 'restaurant', 'to' => '<b>beachbar</b>'],
    ]);

    Livewire::test(ViewAnalyticsEvent::class, ['record' => $event->getRouteKey()])
        ->assertSee('in_wave')
        ->assertSee('restaurant')
        ->assertSee('&lt;b&gt;beachbar&lt;/b&gt;', escape: false);
});
