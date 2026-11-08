<?php

use App\Models\GameConfig;
use Illuminate\Support\Facades\DB;

test('configs endpoint returns the latest version of every config', function () {
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);
    GameConfig::factory()->create(['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1500]]);
    GameConfig::factory()->create(['key' => 'waves', 'version' => 1, 'data' => ['waves' => [['wave' => 1]]]]);

    $this->getJson(route('api.v1.configs.index'))
        ->assertOk()
        ->assertExactJson(['data' => [
            ['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1500]],
            ['key' => 'waves', 'version' => 1, 'data' => ['waves' => [['wave' => 1]]]],
        ]]);
});

test('configs are served from the cache', function () {
    GameConfig::factory()->create(['key' => 'economy', 'data' => ['starting_coins' => 1000]]);
    $this->getJson(route('api.v1.configs.index'))->assertJsonPath('data.0.data.starting_coins', 1000);

    DB::table('game_configs')->update(['data' => json_encode(['starting_coins' => 9999])]);

    $this->getJson(route('api.v1.configs.index'))->assertJsonPath('data.0.data.starting_coins', 1000);
});

test('publishing a new config version refreshes the cached configs', function () {
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1, 'data' => ['starting_coins' => 1000]]);
    $this->getJson(route('api.v1.configs.index'))->assertJsonPath('data.0.version', 1);

    GameConfig::factory()->create(['key' => 'economy', 'version' => 2, 'data' => ['starting_coins' => 1500]]);

    $this->getJson(route('api.v1.configs.index'))
        ->assertJsonPath('data.0.version', 2)
        ->assertJsonPath('data.0.data.starting_coins', 1500);
});

test('configs response is cacheable by the client and revalidated with its etag', function () {
    GameConfig::factory()->create();

    $response = $this->getJson(route('api.v1.configs.index'))
        ->assertOk()
        ->assertHeader('Cache-Control', 'max-age=60, public');
    $etag = $response->headers->get('ETag');

    expect($etag)->not->toBeEmpty();

    $this->getJson(route('api.v1.configs.index'), ['If-None-Match' => $etag])
        ->assertNoContent(304);
});

test('stale etag gets the new configs after a config is published', function () {
    GameConfig::factory()->create(['key' => 'economy', 'version' => 1]);
    $etag = $this->getJson(route('api.v1.configs.index'))->headers->get('ETag');

    GameConfig::factory()->create(['key' => 'economy', 'version' => 2]);

    $this->getJson(route('api.v1.configs.index'), ['If-None-Match' => $etag])
        ->assertOk()
        ->assertJsonPath('data.0.version', 2);
});
