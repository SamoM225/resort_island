<?php

namespace App\Http\Requests\Api\V1;

use App\Services\WaveRating;
use Closure;
use Illuminate\Contracts\Validation\ValidationRule;
use Illuminate\Foundation\Http\FormRequest;

/**
 * A finished wave: the wait of every unit by need, and the result the game computed from it.
 */
class StoreWaveResultRequest extends FormRequest
{
    public const MAX_UNITS = 2000;

    public const MAX_WAIT_SECONDS = 3600;

    /**
     * Get the validation rules that apply to the request.
     *
     * @return array<string, ValidationRule|array<mixed>|string>
     */
    public function rules(): array
    {
        return [
            'wave' => ['required', 'integer', 'min:1'],
            'waits' => [
                'present',
                'array',
                function (string $attribute, mixed $value, Closure $fail): void {
                    if (! is_array($value)) {
                        return;
                    }

                    $rating = $this->rating();
                    foreach (array_keys($value) as $need) {
                        if (! is_string($need) || ! $rating->hasNeed($need)) {
                            $fail("Unknown need [{$need}].");
                        }
                    }

                    if (array_sum(array_map(fn ($waits) => is_array($waits) ? count($waits) : 0, $value)) > self::MAX_UNITS) {
                        $fail('A wave may not have more than '.self::MAX_UNITS.' units.');
                    }
                },
            ],
            'waits.*' => ['array', 'list'],
            'waits.*.*' => ['numeric', 'min:0', 'max:'.self::MAX_WAIT_SECONDS],
            'client' => ['nullable', 'array'],
            'client.coins' => ['nullable', 'integer', 'min:0'],
            'client.rating' => ['nullable', 'integer', 'between:1,'.WaveRating::PERFECT],
            'client.satisfaction' => ['nullable', 'numeric', 'between:0,1'],
        ];
    }

    public function rating(): WaveRating
    {
        return once(fn () => WaveRating::fromCurrentConfigs());
    }
}
