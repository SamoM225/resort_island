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

        /*
        | Free editing (local and dev): an admin change overwrites the current
        | version and may change anything. Otherwise (prod) every change is a
        | new version and only numbers may change, so the game cannot break.
        */
        'free_editing' => (bool) env('GAME_CONFIGS_FREE_EDITING', in_array(env('APP_ENV', 'production'), ['local', 'development'], true)),
    ],

];
