<?php

namespace App\Rules;

use Closure;
use Illuminate\Contracts\Validation\ValidationRule;

/**
 * A config change that cannot break the game: the structure stays (same keys, same list lengths),
 * texts, booleans and nulls stay, and only numbers change — a whole number stays whole,
 * a non-negative one stays non-negative. Used for configs on production.
 */
class SafeConfigChange implements ValidationRule
{
    /**
     * @param  array<array-key, mixed>  $current  data of the published version
     */
    public function __construct(private array $current) {}

    /**
     * Run the validation rule. The value is the new data, as an array or a JSON string.
     */
    public function validate(string $attribute, mixed $value, Closure $fail): void
    {
        $new = is_string($value) ? json_decode($value, true) : $value;

        if (! is_array($new)) {
            return;
        }

        foreach (array_slice(self::violations($this->current, $new), 0, 5) as $violation) {
            $fail($violation);
        }
    }

    /**
     * @return list<string> one message per breaking difference, prefixed with its path (e.g. "waves.3.demand.relax")
     */
    public static function violations(mixed $current, mixed $new, string $path = ''): array
    {
        $at = $path === '' ? 'Config' : $path;

        if (is_array($current)) {
            return self::arrayViolations($current, $new, $path, $at);
        }

        if (is_int($current) || is_float($current)) {
            if (! is_int($new) && ! is_float($new)) {
                return ["{$at}: must stay a number."];
            }

            if (is_int($current) && ! is_int($new)) {
                return ["{$at}: must stay a whole number."];
            }

            if ($current >= 0 && $new < 0) {
                return ["{$at}: cannot be negative."];
            }

            return [];
        }

        if ($current !== $new) {
            return ["{$at}: only numbers can change on production, this value must stay ".json_encode($current).'.'];
        }

        return [];
    }

    /**
     * @param  array<array-key, mixed>  $current
     * @return list<string>
     */
    private static function arrayViolations(array $current, mixed $new, string $path, string $at): array
    {
        if (! is_array($new) || array_is_list($current) !== array_is_list($new)) {
            return ["{$at}: the structure cannot change on production."];
        }

        if (array_is_list($current) && count($current) !== count($new)) {
            return ["{$at}: the list must keep its ".count($current).' items on production.'];
        }

        $added = array_diff_key($new, $current);
        $removed = array_diff_key($current, $new);

        if ($added !== [] || $removed !== []) {
            $changes = [
                ...array_map(fn (string|int $key): string => "+{$key}", array_keys($added)),
                ...array_map(fn (string|int $key): string => "-{$key}", array_keys($removed)),
            ];

            return ["{$at}: keys cannot be added or removed on production (".implode(', ', $changes).').'];
        }

        $violations = [];

        foreach ($current as $key => $value) {
            array_push($violations, ...self::violations($value, $new[$key], $path === '' ? (string) $key : "{$path}.{$key}"));
        }

        return $violations;
    }
}
