<?php

namespace App\Http\Middleware;

use App\Exceptions\PlayerBannedException;
use App\Models\User;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

/**
 * Blocks the game API for banned players. Their tokens stay valid, so unbanning restores access.
 */
class EnsurePlayerIsNotBanned
{
    /**
     * Handle an incoming request.
     *
     * @param  Closure(Request): (Response)  $next
     */
    public function handle(Request $request, Closure $next): Response
    {
        $player = $request->user();

        if ($player instanceof User && $player->isBanned()) {
            throw new PlayerBannedException($player->ban_reason);
        }

        return $next($request);
    }
}
