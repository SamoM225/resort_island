<?php

use App\Http\Controllers\Api\V1\GameConfigController;
use Illuminate\Support\Facades\Route;

Route::prefix('v1')->name('api.v1.')->group(function () {
    Route::middleware('cache.headers:public;max_age=60;etag')->group(function () {
        Route::get('configs', GameConfigController::class)->name('configs.index');
    });
});
