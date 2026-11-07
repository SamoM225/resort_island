<?php

namespace Database\Factories;

use App\Models\AnalyticsEvent;
use App\Models\User;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<AnalyticsEvent>
 */
class AnalyticsEventFactory extends Factory
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
            'name' => 'wave_complete',
            'payload' => ['wave' => fake()->numberBetween(1, 8), 'rating' => fake()->numberBetween(1, 5)],
            'session_id' => fake()->uuid(),
            'app_version' => '0.1.0',
            'platform' => 'android',
            'occurred_at' => now(),
        ];
    }
}
