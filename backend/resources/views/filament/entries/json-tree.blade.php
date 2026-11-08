{{-- Collapsible view of a JSON value (array state of the entry). Click a key to expand it. --}}
<div style="font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8125rem; line-height: 1.6;">
    @include('filament.entries.json-node', ['value' => $getState(), 'label' => null, 'depth' => 0])
</div>
