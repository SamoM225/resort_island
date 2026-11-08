<?php

return [

    /*
    |--------------------------------------------------------------------------
    | Game Configs
    |--------------------------------------------------------------------------
    |
    | Tunable game configs are served from the database. The latest versions
    | are cached for this many seconds; writing a config clears the cache
    | right away, so the TTL is only a safety net for out-of-band changes.
    |
    */

    'configs' => [
        'cache_ttl' => (int) env('GAME_CONFIGS_CACHE_TTL', 3600),
    ],

];
