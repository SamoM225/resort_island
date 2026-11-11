<?php

namespace App\Http\Resources;

use App\Models\WaveResult;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

/**
 * @mixin WaveResult
 */
class WaveResultResource extends JsonResource
{
    /**
     * Transform the resource into an array.
     *
     * @return array{wave: int, units: int, satisfaction: float, rating: int, coins: int, matches_client: bool|null}
     */
    public function toArray(Request $request): array
    {
        return [
            'wave' => $this->wave,
            'units' => $this->units,
            'satisfaction' => $this->satisfaction,
            'rating' => $this->rating,
            'coins' => $this->coins,
            'matches_client' => $this->matchesClient(),
        ];
    }
}
