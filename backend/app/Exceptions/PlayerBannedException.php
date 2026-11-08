<?php

namespace App\Exceptions;

use Exception;
use Illuminate\Contracts\Debug\ShouldntReport;
use Illuminate\Http\JsonResponse;

/**
 * A banned player called the game API. The game shows the reason instead of retrying.
 */
class PlayerBannedException extends Exception implements ShouldntReport
{
    public function __construct(public readonly ?string $reason)
    {
        parent::__construct('This account is banned.');
    }

    /**
     * Render the exception as an HTTP response.
     */
    public function render(): JsonResponse
    {
        return response()->json([
            'message' => $this->getMessage(),
            'reason' => $this->reason,
        ], 403);
    }
}
