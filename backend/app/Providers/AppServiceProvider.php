<?php

namespace App\Providers;

use Illuminate\Cache\RateLimiting\Limit;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\RateLimiter;
use Illuminate\Support\ServiceProvider;

class AppServiceProvider extends ServiceProvider
{
    /**
     * Register any application services.
     */
    public function register(): void
    {
        //
    }

    /**
     * Bootstrap any application services.
     */
    public function boot(): void
    {
        RateLimiter::for('player-registration', fn (Request $request) => Limit::perMinute(10)->by($request->ip()));

        RateLimiter::for('player-login', fn (Request $request) => Limit::perMinute(5)->by(
            $request->string('email')->lower()->append('|', $request->ip())->value()
        ));
    }
}
