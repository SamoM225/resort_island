using System;
using UnityEngine;
using UnityEngine.EventSystems;
using ResortIsland.Config;
using ResortIsland.UI;
using ResortIsland.Characters;
using System.Collections.Generic;

namespace ResortIsland.Resort
{

    public enum PathTool
    {
        None,
        /// lays new path cells of PathStyle / PathLevel
        Draw,
        /// sets PathLevel on cells of PathStyle
        Upgrade,
        /// turns any path into PathStyle
        Restyle,
        Erase
    }

    /// Mouse / touch input on the island: placing, moving, turning and selecting buildings, and the path
    /// tools. R (or the build menu's Rotate button) turns the building being placed, dragged or selected;
    /// a right click on a building turns it too.
    public class BuildingPlacer : MonoBehaviour
    {
        private static readonly Color ValidCell = new Color(0.2f, 1f, 0.2f, 0.55f);
        private static readonly Color InvalidCell = new Color(1f, 0.15f, 0.15f, 0.65f);
        private static readonly Color SelectedCell = new Color(0.35f, 0.75f, 1f, 0.45f);
        private static readonly Color EntranceCell = new Color(1f, 0.82f, 0.2f, 0.95f);

        [SerializeField] private Camera cam;
        [SerializeField] private ResortManager resort;

        private BuildingConfig selectedConfig;
        private int placeRotation;
        // until the player turns it, a building faces the nearest path by itself
        private bool placeRotationManual;
        private Vector3Int lastAutoOrigin = new Vector3Int(int.MinValue, 0, 0);
        private readonly List<SpriteRenderer> previewCells = new List<SpriteRenderer>();
        private SpriteRenderer entranceMarker;
        private SpriteRenderer ghost;
        private Sprite previewSprite;
        private Texture2D previewTexture;

        // the current left press started on the island (not on the UI / not on a building)
        private bool pressOnIsland;

        private PlacedBuilding dragged;
        private int dragRotation;
        private bool dragMoved;
        private bool dragRotationManual;
        private Vector3 dragStartMouse;

        private PathTool pathTool;
        private bool painting;
        private bool paintChanged;
        private Vector3Int lastPaintCell;

        /// A building was placed through SelectBuilding (the build menu charges for it).
        public event Action<BuildingConfig> BuildingPlaced;

        public PathTool ActivePathTool => pathTool;
        public BuildingConfig SelectedBuilding => selectedConfig;
        /// Building on the island picked with a tap (shown with its footprint and entrance).
        public PlacedBuilding SelectedPlaced { get; private set; }
        public bool CanRotate => selectedConfig != null || dragged != null || SelectedPlaced != null;
        /// The tapped building shows its footprint and entrance only while this is on (the build menu is open).
        public bool HighlightSelection { get; set; }
        /// Style and level the path tools work with.
        public string PathStyle { get; set; }
        public int PathLevel { get; set; } = 1;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (resort == null || !resort.IsReady) return;
            if (SelectedPlaced == null) SelectedPlaced = null; // the building was destroyed (new game)

            if (painting && !Input.GetMouseButton(0)) EndPaint();
            if (Input.GetKeyDown(KeyCode.Escape) && resort.SelectedWorker != null)
            {
                resort.ClearWorkerSelection();
                HidePreview();
            }
            if (Input.GetKeyDown(KeyCode.R) && pathTool == PathTool.None) RotateSelection();

            if (!TryGetMouseCell(out var cell))
            {
                HidePreview();
                if (dragged != null && !Input.GetMouseButton(0))
                    EndDrag(false, dragged.Cell);
                return;
            }

            if (pathTool != PathTool.None)
            {
                UpdatePathTool(cell);
                return;
            }

            if (selectedConfig != null)
            {
                UpdatePlacing(cell);
                return;
            }

            if (dragged != null)
            {
                UpdateDragging(cell);
                return;
            }

            ShowSelection();
            TryStartDrag(cell);
        }

        public bool SelectBuilding(BuildingConfig config)
        {
            if (resort == null || !resort.CanBuild(config)) return false;
            CancelPlacement();
            selectedConfig = config;
            pressOnIsland = false;
            placeRotationManual = false;
            lastAutoOrigin = new Vector3Int(int.MinValue, 0, 0);
            return true;
        }

        /// Drag on the map to use the tool on the cells under the cursor.
        public bool SelectPathTool(PathTool tool)
        {
            if (resort == null || !resort.PathsReady) return false;
            CancelPlacement();
            pathTool = tool;
            return true;
        }

        public void CancelPlacement()
        {
            if (painting) EndPaint();
            selectedConfig = null;
            pathTool = PathTool.None;
            SelectedPlaced = null;
            HidePreview();
        }

        /// Turns the building that is being placed, dragged or is selected.
        public bool RotateSelection()
        {
            if (selectedConfig != null)
            {
                placeRotation = BuildingRotation.Normalize(placeRotation + 1);
                placeRotationManual = true;
                return true;
            }
            if (dragged != null)
            {
                dragRotation = BuildingRotation.Normalize(dragRotation + 1);
                dragRotationManual = true;
                dragged.ShowRotation(dragRotation);
                return true;
            }
            return SelectedPlaced != null && resort.TryRotateBuilding(SelectedPlaced);
        }

        // ---- placing a new building -----------------------------------------------------------

        private void UpdatePlacing(Vector3Int cell)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            Vector2Int size = BuildingRotation.Size(selectedConfig.Footprint, placeRotation);
            Vector3Int origin = resort.OriginFor(cell, size);
            if (!placeRotationManual && origin != lastAutoOrigin)
            {
                lastAutoOrigin = origin;
                placeRotation = resort.AutoRotation(selectedConfig, origin, null, placeRotation);
                size = BuildingRotation.Size(selectedConfig.Footprint, placeRotation);
                origin = resort.OriginFor(cell, size);
            }
            bool valid = resort.CanPlaceBuilding(selectedConfig, origin, placeRotation);
            ShowPreview(origin, size, c => resort.CanPlaceBuilding(c, Vector2Int.one));
            ShowEntrance(BuildingRotation.EntranceCell(origin, selectedConfig.Footprint, selectedConfig.Entrance, placeRotation), valid);
            ShowGhost(selectedConfig.SpriteFor(placeRotation), resort.FootprintToWorld(origin, size), valid);

            // placed on release, so dragging the view around while placing does not build anything
            if (Input.GetMouseButtonDown(0)) pressOnIsland = !IsPointerOverUi();
            if (!Input.GetMouseButtonUp(0) || !pressOnIsland || CameraController.DraggedThisPress) return;
            pressOnIsland = false;

            BuildingConfig placed = selectedConfig;
            if (resort.TryPlaceBuilding(placed, origin, placeRotation))
            {
                CancelPlacement();
                BuildingPlaced?.Invoke(placed);
            }
        }

        // ---- moving, turning and selecting placed buildings -----------------------------------

        private void TryStartDrag(Vector3Int cell)
        {
            if (IsPointerOverUi())
            {
                if (Input.GetMouseButtonDown(0)) pressOnIsland = false;
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
                world.z = 0f;
                Collider2D hit = Physics2D.OverlapPoint(world);
                if (hit != null && hit.GetComponentInParent<WorkerAI>() != null)
                {
                    pressOnIsland = false;
                    return;
                }
            }
            bool onBuilding = resort.TryGetBuilding(cell, out var building);

            if (onBuilding && resort.SelectedWorker != null)
            {
                bool occupied = resort.HasWorkerAtBuilding(building);
                ShowPreview(building.Cell, building.Footprint,
                    _ => !occupied, occupied ? InvalidCell : SelectedCell);
                ShowEntrance(building.EntranceCell, !occupied);
            }

            if (Input.GetMouseButtonDown(1) && onBuilding)
            {
                SelectedPlaced = building;
                resort.TryRotateBuilding(building);
                return;
            }
            // a tap on empty land (not a drag of the view) clears the selection
            if (Input.GetMouseButtonDown(0)) pressOnIsland = !onBuilding;
            if (Input.GetMouseButtonUp(0) && pressOnIsland && !CameraController.DraggedThisPress && !onBuilding)
            {
                resort.ClearWorkerSelection();
                SelectedPlaced = null;
            }
            if (!Input.GetMouseButtonDown(0) || !onBuilding) return;
            dragged = building;
            dragRotation = building.Rotation;
            dragMoved = false;
            dragRotationManual = false;
            lastAutoOrigin = building.Cell;
            dragStartMouse = Input.mousePosition;
        }

        private void UpdateDragging(Vector3Int cell)
        {
            if (!dragMoved && (Input.mousePosition - dragStartMouse).sqrMagnitude > 64f)
                dragMoved = true;

            // the cell under the cursor is the middle of the building footprint
            Vector2Int size = BuildingRotation.Size(dragged.Config.Footprint, dragRotation);
            Vector3Int origin = resort.OriginFor(cell, size);
            if (origin != dragged.Cell) dragMoved = true;
            if (dragMoved && !dragRotationManual && origin != lastAutoOrigin)
            {
                // face the nearest path at the new spot
                lastAutoOrigin = origin;
                int rotation = resort.AutoRotation(dragged.Config, origin, dragged, dragRotation);
                if (rotation != dragRotation)
                {
                    dragRotation = rotation;
                    dragged.ShowRotation(dragRotation);
                    size = BuildingRotation.Size(dragged.Config.Footprint, dragRotation);
                    origin = resort.OriginFor(cell, size);
                }
            }
            bool changed = dragMoved || dragRotation != dragged.Rotation;
            bool valid = resort.CanPlaceBuilding(dragged.Config, origin, dragRotation, dragged);

            if (changed)
            {
                dragged.transform.position = resort.FootprintToWorld(origin, size);
                ResortSorting.Apply(dragged.gameObject);
                SetAlpha(dragged.gameObject, valid ? 0.7f : 0.25f);
                ShowPreview(origin, size, c => resort.CanPlaceBuilding(c, Vector2Int.one, dragged));
                ShowEntrance(dragged.EntranceFor(origin, dragRotation), valid);
            }

            if (Input.GetMouseButtonDown(1))
                EndDrag(false, origin);
            else if (!Input.GetMouseButton(0))
                EndDrag(valid, origin);
        }

        private void EndDrag(bool commit, Vector3Int origin)
        {
            PlacedBuilding building = dragged;
            dragged = null;
            SetAlpha(building.gameObject, 1f);
            HidePreview();

            bool changed = origin != building.Cell || dragRotation != building.Rotation;
            if (!dragMoved && dragRotation == building.Rotation)
            {
                // Assignment starts only after the worker avatar itself was clicked.
                if (resort.SelectedWorker != null &&
                    !resort.ReassignSelectedWorker(building))
                    ShowWorkerError();
                SelectedPlaced = building;
                resort.RefreshPosition(building);
            }
            else if (commit && changed)
            {
                resort.MoveBuilding(building, origin, dragRotation);
                SelectedPlaced = building;
            }
            else
            {
                building.ShowRotation(building.Rotation);
                resort.RefreshPosition(building);
            }
        }

        private void ShowSelection()
        {
            if (SelectedPlaced == null || !HighlightSelection)
            {
                HidePreview();
                return;
            }
            ShowPreview(SelectedPlaced.Cell, SelectedPlaced.Footprint, _ => true, SelectedCell);
            ShowEntrance(SelectedPlaced.EntranceCell, resort.IsUsableEntrance(SelectedPlaced.EntranceCell, SelectedPlaced));
        }

        // ---- path tools ------------------------------------------------------------------------

        private void UpdatePathTool(Vector3Int cell)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            ShowPreview(cell, Vector2Int.one, CanPaint);

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                painting = true;
                paintChanged = false;
                lastPaintCell = cell;
                Paint(cell);
                return;
            }

            if (!painting || !Input.GetMouseButton(0)) return;

            // walk cell by cell, so a fast drag does not leave gaps in the path
            while (lastPaintCell != cell)
            {
                Vector3Int d = cell - lastPaintCell;
                lastPaintCell += Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                    ? new Vector3Int(Math.Sign(d.x), 0, 0)
                    : new Vector3Int(0, Math.Sign(d.y), 0);
                Paint(lastPaintCell);
            }
        }

        private bool CanPaint(Vector3Int cell)
        {
            switch (pathTool)
            {
                case PathTool.Draw: return resort.CanBuildPath(cell);
                case PathTool.Upgrade: return resort.CanUpgradePath(cell, PathStyle, PathLevel);
                case PathTool.Restyle: return resort.CanRestylePath(cell, PathStyle);
                case PathTool.Erase: return resort.CanRemovePath(cell);
                default: return false;
            }
        }

        private void Paint(Vector3Int cell)
        {
            bool changed;
            switch (pathTool)
            {
                case PathTool.Draw: changed = resort.TryBuildPath(cell, PathStyle, PathLevel); break;
                case PathTool.Upgrade: changed = resort.TryUpgradePath(cell, PathStyle, PathLevel); break;
                case PathTool.Restyle: changed = resort.TryRestylePath(cell, PathStyle); break;
                case PathTool.Erase: changed = resort.TryRemovePath(cell); break;
                default: changed = false; break;
            }
            paintChanged |= changed;
        }

        private void EndPaint()
        {
            painting = false;
            // inside the path editor the game is saved when the edit is confirmed
            if (paintChanged && !resort.IsEditingPaths) resort.SaveGame();
            paintChanged = false;
        }

        // ---- preview ---------------------------------------------------------------------------

        private static bool IsPointerOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void ShowPreview(Vector3Int origin, Vector2Int size, Func<Vector3Int, bool> isValid, Color? validColor = null)
        {
            EnsurePreviewSprite();

            int required = size.x * size.y;
            while (previewCells.Count < required)
                previewCells.Add(NewMarker("Building Placement Cell", 1000));

            int index = 0;
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int previewCell = origin + new Vector3Int(x, y, 0);
                var renderer = previewCells[index++];
                renderer.transform.position = resort.CellToWorld(previewCell);
                renderer.color = isValid(previewCell) ? validColor ?? ValidCell : InvalidCell;
                renderer.enabled = true;
            }

            for (; index < previewCells.Count; index++)
                previewCells[index].enabled = false;
            if (entranceMarker != null) entranceMarker.enabled = false;
            if (ghost != null) ghost.enabled = false;
        }

        // the cell in front of the door: yellow when guests can stand there, red when not
        private void ShowEntrance(Vector3Int cell, bool valid)
        {
            EnsurePreviewSprite();
            if (entranceMarker == null) entranceMarker = NewMarker("Building Entrance", 1001);
            entranceMarker.transform.position = resort.CellToWorld(cell);
            entranceMarker.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            entranceMarker.color = valid ? EntranceCell : InvalidCell;
            entranceMarker.enabled = true;
        }

        private void ShowGhost(Sprite sprite, Vector3 position, bool valid)
        {
            if (sprite == null) return;
            if (ghost == null)
            {
                var go = new GameObject("Building Ghost");
                go.transform.SetParent(transform, true);
                ghost = go.AddComponent<SpriteRenderer>();
                ghost.sortingLayerName = ResortSorting.LayerName;
            }
            ghost.sprite = sprite;
            ghost.transform.position = position;
            ghost.sortingOrder = -Mathf.RoundToInt(position.y * 100f);
            ghost.color = valid ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f, 0.45f);
            ghost.enabled = true;
        }

        private void HidePreview()
        {
            foreach (var renderer in previewCells)
                renderer.enabled = false;
            if (entranceMarker != null) entranceMarker.enabled = false;
            if (ghost != null) ghost.enabled = false;
        }

        private SpriteRenderer NewMarker(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, true);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = previewSprite;
            renderer.sortingLayerName = "Ground";
            // above the paths (PathRenderer)
            renderer.sortingOrder = order;
            return renderer;
        }

        private void EnsurePreviewSprite()
        {
            if (previewSprite != null) return;

            const int width = 32;
            const int height = 16;
            previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Building Placement Cell Texture"
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
                float ny = Mathf.Abs((y + 0.5f - height * 0.5f) / (height * 0.5f));
                pixels[y * width + x] = nx + ny <= 1f ? Color.white : Color.clear;
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
            previewSprite = Sprite.Create(previewTexture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), width);
            previewSprite.name = "Building Placement Cell";
        }

        private void ShowWorkerError()
        {
            var hud = FindFirstObjectByType<BuildModeHud>();
            if (hud != null)
                hud.ShowMessage("На этом здании уже есть работник");
            else
                Debug.LogWarning("На этом здании уже есть работник");
        }

        private void OnDestroy()
        {
            if (previewSprite != null) Destroy(previewSprite);
            if (previewTexture != null) Destroy(previewTexture);
        }

        private bool TryGetMouseCell(out Vector3Int cell)
        {
            cell = default;

            Vector3 mouse = Input.mousePosition;
            if (!new Rect(0f, 0f, Screen.width, Screen.height).Contains(mouse)) return false;

            Vector3 world = cam.ScreenToWorldPoint(mouse);
            world.z = 0f;
            cell = resort.WorldToCell(world);
            return true;
        }

        private static void SetAlpha(GameObject go, float alpha)
        {
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }
        }
    }
}
