@if (is_array($value))
    <details @if ($depth === 0) open @endif style="margin-left: {{ $depth === 0 ? 0 : 1.25 }}rem">
        <summary style="cursor: pointer">
            @if ($label !== null)
                <span style="color: var(--primary-600)">{{ $label }}</span>:
            @endif
            <span style="opacity: 0.6">{{ array_is_list($value) ? '[ '.count($value).' ]' : '{ '.count($value).' }' }}</span>
        </summary>
        @foreach ($value as $childLabel => $child)
            @include('filament.entries.json-node', ['value' => $child, 'label' => $childLabel, 'depth' => $depth + 1])
        @endforeach
    </details>
@else
    <div style="margin-left: {{ $depth === 0 ? 0 : 1.25 }}rem">
        @if ($label !== null)
            <span style="color: var(--primary-600)">{{ $label }}</span>:
        @endif
        <span>{{ json_encode($value, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES) }}</span>
    </div>
@endif
