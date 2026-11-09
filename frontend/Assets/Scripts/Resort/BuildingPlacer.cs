using UnityEngine;
using UnityEngine.EventSystems;
using ResortIsland.Config;
using System.Collections.Generic;

namespace ResortIsland.Resort
{

    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private ResortManager resort;

        private PlacedBuilding dragged;
        private BuildingConfig selectedConfig;
        private BuildingBuildMenu menu;
        private readonly List<SpriteRenderer> previewCells = new List<SpriteRenderer>();
        private Sprite previewSprite;
        private Texture2D previewTexture;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (resort == null || !resort.IsReady) return;

            if (menu == null)
            {
                menu = BuildingBuildMenu.Create(resort, this);
                if (menu == null) return;
            }

            if (!TryGetMouseCell(out var cell))
            {
                HidePreview();
                if (dragged != null && !Input.GetMouseButton(0))
                    EndDrag(false, dragged.Cell);
                return;
            }

            if (selectedConfig != null)
            {
                UpdatePreview(cell);

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    CancelPlacement();
                    return;
                }

                if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
                {
                    Vector3Int origin = resort.OriginFor(cell, selectedConfig.Footprint);
                    if (resort.TryPlaceBuilding(selectedConfig, origin))
                        CancelPlacement();
                }
                return;
            }

            HidePreview();
            if (dragged != null) UpdateDragging(cell);
            else TryStartDrag(cell);
        }

        public bool SelectBuilding(BuildingConfig config)
        {
            if (resort == null || !resort.CanBuild(config)) return false;
            selectedConfig = config;
            return true;
        }

        public void CancelPlacement()
        {
            selectedConfig = null;
            HidePreview();
        }

        private void TryStartDrag(Vector3Int cell)
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (IsPointerOverUi()) return;

            if (resort.TryGetBuilding(cell, out var building))
                dragged = building;
        }

        private static bool IsPointerOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void UpdatePreview(Vector3Int cell)
        {
            EnsurePreviewSprite();

            Vector2Int size = selectedConfig.Footprint;
            Vector3Int origin = resort.OriginFor(cell, size);
            int required = size.x * size.y;
            while (previewCells.Count < required)
            {
                var go = new GameObject("Building Placement Cell");
                go.transform.SetParent(transform, true);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = previewSprite;
                renderer.sortingLayerName = "Ground";
                renderer.sortingOrder = 50;
                previewCells.Add(renderer);
            }

            int index = 0;
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int previewCell = origin + new Vector3Int(x, y, 0);
                var renderer = previewCells[index++];
                renderer.transform.position = resort.CellToWorld(previewCell);
                renderer.color = resort.IsAreaFree(previewCell, Vector2Int.one)
                    ? new Color(0.2f, 1f, 0.2f, 0.55f)
                    : new Color(1f, 0.15f, 0.15f, 0.65f);
                renderer.enabled = true;
            }

            for (; index < previewCells.Count; index++)
                previewCells[index].enabled = false;
        }

        private void HidePreview()
        {
            foreach (var renderer in previewCells)
                renderer.enabled = false;
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

        private void OnDestroy()
        {
            if (previewSprite != null) Destroy(previewSprite);
            if (previewTexture != null) Destroy(previewTexture);
        }

        private void UpdateDragging(Vector3Int cell)
        {
            // the cell under the cursor is the middle of the building footprint
            Vector2Int size = dragged.Footprint;
            Vector3Int origin = resort.OriginFor(cell, size);
            bool valid = resort.IsAreaFree(origin, size, dragged);

            dragged.transform.position = resort.FootprintToWorld(origin, size);
            ResortSorting.Apply(dragged.gameObject);
            SetAlpha(dragged.gameObject, valid ? 0.7f : 0.25f);

            if (Input.GetMouseButtonDown(1))
                EndDrag(false, origin);
            else if (!Input.GetMouseButton(0))
                EndDrag(valid, origin);
        }

        private void EndDrag(bool commit, Vector3Int origin)
        {
            SetAlpha(dragged.gameObject, 1f);

            if (commit && origin != dragged.Cell)
                resort.MoveBuilding(dragged, origin);
            else
                resort.RefreshPosition(dragged);

            dragged = null;
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