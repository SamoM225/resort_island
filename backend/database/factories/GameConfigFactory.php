<?php

namespace Database\Factories;

use App\Models\GameConfig;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<GameConfig>
 */
class GameConfigFactory extends Factory
{
    /**
     * Define the model's default state.
     *
     * @return array<string, mixed>
     */
    public function definition(): array
    {
        return [
            'key' => fake()->unique()->slug(2),
            'version' => 1,
            'data' => ['value' => fake()->numberBetween(1, 100)],
        ];
    }
}
