<?php

namespace App\Http\Resources;

use App\Models\PlayerSave;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

/**
 * @mixin PlayerSave
 */
class PlayerSaveResource extends JsonResource
{
    /**
     * Transform the resource into an array.
     *
     * @return array{save_version: int, saved_at: string, uploaded_at: string|null, save: array<string, mixed>}
     */
    public function toArray(Request $request): array
    {
        return [
            'save_version' => $this->save_version,
            'saved_at' => $this->saved_at->toIso8601String(),
            'uploaded_at' => $this->updated_at?->toIso8601String(),
            'save' => $this->data,
        ];
    }
}
