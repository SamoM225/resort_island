# Resort Island

Monorepo pre hru Resort Island.

- `frontend/` – Unity klient (URP 2D)
- `backend/` – server (zatiaľ prázdny)

## Procedural island

`frontend/Assets/Scripts/Resort/IslandGenerator.cs` generates the island when the `Island` scene
starts. It is already attached to the `ResortManager` object in `frontend/Assets/Scenes/Island.unity`.
Change its serialized map, seed, coastline, noise, and terrain threshold fields in the Inspector,
or use **Generate island** from the component context menu to regenerate it.

`TileData` is the authoritative per-cell data source. Use `IslandGenerator.TryGetTile(x, y, out tile)`
for 0-based map coordinates, `IsBuildableArea` for multi-tile validation, and
`TrySetOccupiedArea` to reserve a rectangular area. Terrain-specific lists (`GrassLayer`,
`MountainsLayer`, etc.) provide logical layer views. Existing centered resort `Vector3Int`
coordinates are accepted by the `TryGetTile(Vector3Int, ...)` overload.
