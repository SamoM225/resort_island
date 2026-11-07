<?php

use App\Http\Controllers\Api\V1\AnalyticsEventController;
use App\Http\Controllers\Api\V1\Auth\AnonymousRegistrationController;
use App\Http\Controllers\Api\V1\Auth\CurrentPlayerController;
use App\Http\Controllers\Api\V1\Auth\LinkEmailController;
use App\Http\Controllers\Api\V1\Auth\LoginController;
use App\Http\Controllers\Api\V1\Auth\LogoutController;
use App\Http\Controllers\Api\V1\GameConfigController;
use Illuminate\Support\Facades\Route;

Route::prefix('v1')->name('api.v1.')->group(function () {
    Route::middleware('cache.headers:public;max_age=60;etag')->group(function () {
        Route::get('configs', GameConfigController::class)->name('configs.index');
    });

    Route::post('auth/anonymous', AnonymousRegistrationController::class)
        ->middleware('throttle:player-registration')
        ->name('auth.anonymous');
    Route::post('auth/login', LoginController::class)
        ->middleware('throttle:player-login')
        ->name('auth.login');

    Route::middleware(['auth:sanctum', 'not-banned'])->group(function () {
        Route::post('auth/logout', LogoutController::class)->name('auth.logout');
        Route::get('me', CurrentPlayerController::class)->name('me');
        Route::post('me/email', LinkEmailController::class)->name('me.email');

        Route::post('analytics/events', AnalyticsEventController::class)
            ->middleware('throttle:analytics')
            ->name('analytics.events.store');
    });
});
