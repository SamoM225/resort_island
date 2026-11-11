<?php

namespace Database\Factories;

use App\Models\User;
use App\Models\WaveResult;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<WaveResult>
 */
class WaveResultFactory extends Factory
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
            'wave' => 3,
            'units' => 48,
            'satisfaction' => 0.858,
            'rating' => 4,
            'coins' => 768,
            'client_coins' => 768,
        ];
    }
}
