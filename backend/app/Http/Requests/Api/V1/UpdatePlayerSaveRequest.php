<?php

namespace App\Http\Requests\Api\V1;

use Closure;
use Illuminate\Contracts\Validation\ValidationRule;
use Illuminate\Foundation\Http\FormRequest;

/**
 * The game's local save as JSON, with its format version and the time the game saved it.
 */
class UpdatePlayerSaveRequest extends FormRequest
{
    public const MAX_DATA_BYTES = 256 * 1024;

    /**
     * Get the validation rules that apply to the request.
     *
     * @return array<string, ValidationRule|array<mixed>|string>
     */
    public function rules(): array
    {
        return [
            'save_version' => ['required', 'integer', 'min:1'],
            'saved_at' => ['required', 'date'],
            'save' => [
                'required',
                'array',
                function (string $attribute, mixed $value, Closure $fail): void {
                    if (strlen(json_encode($value)) > self::MAX_DATA_BYTES) {
                        $fail('The save may not be larger than 256 KB.');
                    }
                },
            ],
        ];
    }
}
