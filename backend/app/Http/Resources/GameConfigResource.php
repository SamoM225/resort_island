<?php

namespace App\Http\Resources;

use App\Models\GameConfig;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

/**
 * @mixin GameConfig
 */
class GameConfigResource extends JsonResource
{
    /**
     * Transform the resource into an array.
     *
     * @return array{key: string, version: int, data: array<string, mixed>}
     */
    public function toArray(Request $request): array
    {
        return [
            'key' => $this->key,
            'version' => $this->version,
            'data' => $this->data,
        ];
    }
}
