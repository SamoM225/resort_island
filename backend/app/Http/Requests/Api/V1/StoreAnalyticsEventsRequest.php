<?php

namespace App\Http\Requests\Api\V1;

use Closure;
use Illuminate\Contracts\Validation\ValidationRule;
use Illuminate\Foundation\Http\FormRequest;

/**
 * A batch of analytics events the game collected since its last send.
 */
class StoreAnalyticsEventsRequest extends FormRequest
{
    public const MAX_EVENTS = 100;

    public const MAX_PAYLOAD_BYTES = 4096;

    /**
     * Get the validation rules that apply to the request.
     *
     * @return array<string, ValidationRule|array<mixed>|string>
     */
    public function rules(): array
    {
        return [
            'app_version' => ['nullable', 'string', 'max:32'],
            'platform' => ['nullable', 'string', 'max:16'],
            'events' => ['required', 'array', 'min:1', 'max:'.self::MAX_EVENTS],
            'events.*.name' => ['required', 'string', 'max:64', 'regex:/^[a-z][a-z0-9_]*$/'],
            'events.*.occurred_at' => ['required', 'date'],
            'events.*.session_id' => ['nullable', 'uuid'],
            'events.*.payload' => [
                'nullable',
                'array',
                function (string $attribute, mixed $value, Closure $fail): void {
                    if (strlen(json_encode($value)) > self::MAX_PAYLOAD_BYTES) {
                        $fail('The :attribute may not be larger than '.self::MAX_PAYLOAD_BYTES.' bytes.');
                    }
                },
            ],
        ];
    }

    /**
     * @return array<string, string>
     */
    public function messages(): array
    {
        return [
            'events.*.name.regex' => 'Event names are snake_case, e.g. wave_complete.',
        ];
    }
}
