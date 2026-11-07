using UnityEngine;

namespace ResortIsland.Resort
{

    public class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private ResortManager resort;

        private PlacedBuilding dragged;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (resort == null || !resort.IsReady) return;

            if (!TryGetMouseCell(out var cell))
            {
                if (dragged != null && !Input.GetMouseButton(0))
                    EndDrag(false, dragged.Cell);
                return;
            }

            if (dragged != null) UpdateDragging(cell);
            else TryStartDrag(cell);
        }

        private void TryStartDrag(Vector3Int cell)
        {
            if (!Input.GetMouseButtonDown(0)) return;

            if (resort.TryGetBuilding(cell, out var building))
                dragged = building;
        }

        private void UpdateDragging(Vector3Int cell)
        {
            bool valid = cell == dragged.Cell || resort.IsFree(cell);

            dragged.transform.position = resort.CellToWorld(cell);
            ResortSorting.Apply(dragged.gameObject);
            SetAlpha(dragged.gameObject, valid ? 0.7f : 0.25f);

            if (Input.GetMouseButtonDown(1))
                EndDrag(false, cell);
            else if (!Input.GetMouseButton(0))
                EndDrag(valid, cell);
        }

        private void EndDrag(bool commit, Vector3Int cell)
        {
            SetAlpha(dragged.gameObject, 1f);

            if (commit && cell != dragged.Cell)
                resort.MoveBuilding(dragged, cell);
            else
                dragged.SetCell(dragged.Cell, resort.CellToWorld(dragged.Cell));   

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