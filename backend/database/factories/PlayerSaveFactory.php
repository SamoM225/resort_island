<?php

namespace Database\Factories;

use App\Models\PlayerSave;
use App\Models\User;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<PlayerSave>
 */
class PlayerSaveFactory extends Factory
{
    /**
     * Define the model's default state.
     *
     * @return array<string, mixed>
     */
    public function definition(): array
    {
        return [
            'user_id' => User::factory()->anonymous(),
            'save_version' => 1,
            'data' => [
                'saveVersion' => 1,
                'economy' => ['coins' => 1000, 'gems' => 10, 'energy' => 82, 'resortValue' => 24500, 'reputation' => 4],
                'resort' => [
                    'buildings' => [['buildingId' => 'reception', 'cellX' => -2, 'cellY' => 2]],
                    'unlocks' => [],
                ],
            ],
            'saved_at' => now(),
        ];
    }
}
