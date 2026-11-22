using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Characters
{
    [RequireComponent(typeof(CharacterView))]
    public sealed class WorkerAI : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float speed = 0.65f;

        private readonly List<Vector3Int> route = new List<Vector3Int>();
        private ResortManager resort;
        private CharacterView view;
        private PlacedBuilding assignedBuilding;
        private Vector3Int assignedSlot;
        private Vector3Int cell;
        private int routeIndex;
        private bool initialized;
        private SpriteRenderer selectionOutline;
        private BoxCollider2D workerCollider;
        private bool selected;

        public PlacedBuilding AssignedBuilding => assignedBuilding;
        public Vector3Int AssignedSlot => assignedSlot;

        public void Initialize(ResortManager manager)
        {
            resort = manager;
            view = GetComponent<CharacterView>();
            cell = resort != null ? resort.WorldToCell(transform.position) : Vector3Int.zero;
            CharacterWander wander = GetComponent<CharacterWander>();
            if (wander != null) wander.enabled = false;
            EnsureSelectionOutline();
            EnsureWorkerCollider();
            initialized = resort != null;
        }

        private void OnMouseDown()
        {
            if (resort == null || !resort.IsReady) return;
            if (resort.SelectedWorker == this)
                resort.ClearWorkerSelection();
            else
                resort.SelectWorker(this);
        }

        public void Assign(PlacedBuilding building, Vector3Int slot)
        {
            assignedBuilding = building;
            assignedSlot = slot;
            cell = resort != null ? resort.WorldToCell(transform.position) : slot;
            routeIndex = route.Count;
            if (view != null) view.SetState(CharacterState.Rest);
            gameObject.name = building != null && building.Config != null
                ? "Worker -> " + building.Config.DisplayName
                : "Worker (unassigned)";
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selectionOutline != null)
                selectionOutline.enabled = selected;
        }

        private void Update()
        {
            if (!initialized || resort == null || !resort.IsReady) return;

            Vector3Int goal = assignedSlot;
            if (assignedBuilding != null)
            {
                Vector3Int expected = resort.FindWorkerSlot(assignedBuilding, this);
                if (expected != assignedSlot)
                    assignedSlot = expected;
                goal = assignedSlot;
            }
            else
            {
                view.SetState(CharacterState.Rest);
                return;
            }

            if (cell == goal)
            {
                view.SetState(CharacterState.Rest);
                return;
            }

            if (routeIndex >= route.Count)
            {
                if (!TryFindRoute(goal))
                {
                    view.SetState(CharacterState.Rest);
                    return;
                }
                routeIndex = 1;
            }

            if (routeIndex >= route.Count) return;
            Vector3Int next = route[routeIndex];
            if (next != cell && !resort.IsFree(next) && (resort.Paths == null || !resort.Paths.Contains(next)))
            {
                route.Clear();
                routeIndex = 0;
                return;
            }

            Vector3 worldGoal = resort.CellToWorld(next);
            view.FaceDelta(worldGoal - transform.position);
            view.SetState(CharacterState.Walk);
            transform.position = Vector3.MoveTowards(transform.position, worldGoal, speed * Time.deltaTime);
            ResortSorting.Apply(gameObject);

            if ((transform.position - worldGoal).sqrMagnitude < 0.0001f)
            {
                cell = next;
                routeIndex++;
            }
        }

        private bool TryFindRoute(Vector3Int goal)
        {
            PathNetwork paths = resort.Paths;
            bool OnPath(Vector3Int c) => paths != null && paths.Contains(c);
            bool Walkable(Vector3Int c) => c == cell || OnPath(c) || resort.IsFree(c);
            route.Clear();
            return PathFinder.TryFind(new[] { cell }, Walkable, c => c == goal, route,
                OnPath, 60, 10, 2) && route.Count > 1;
        }

        private void EnsureWorkerCollider()
        {
            workerCollider = GetComponent<BoxCollider2D>();
            if (workerCollider == null)
                workerCollider = gameObject.AddComponent<BoxCollider2D>();

            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite != null && sprite.sprite != null)
            {
                workerCollider.size = sprite.sprite.bounds.size;
                workerCollider.offset = sprite.sprite.bounds.center;
            }
            else
            {
                workerCollider.size = new Vector2(0.5f, 0.8f);
                workerCollider.offset = new Vector2(0f, 0.4f);
            }
        }

        private void EnsureSelectionOutline()
        {
            Transform existing = transform.Find("WorkerSelectionOutline");
            if (existing != null)
            {
                selectionOutline = existing.GetComponent<SpriteRenderer>();
                if (selectionOutline != null)
                {
                    selectionOutline.enabled = selected;
                    return;
                }
            }

            GameObject outlineObject = new GameObject("WorkerSelectionOutline");
            outlineObject.transform.SetParent(transform, false);
            outlineObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            outlineObject.transform.localScale = Vector3.one * 0.7f;
            selectionOutline = outlineObject.AddComponent<SpriteRenderer>();
            selectionOutline.sprite = CreateSelectionRingSprite();
            selectionOutline.color = new Color(1f, 0.75f, 0.2f, 1f);
            selectionOutline.sortingLayerName = "Ground";
            selectionOutline.sortingOrder = 501;
            selectionOutline.enabled = false;
        }

        private static Sprite SelectionRingSprite;

        private static Sprite CreateSelectionRingSprite()
        {
            if (SelectionRingSprite != null) return SelectionRingSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "WorkerSelectionRing";
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float inner = size * 0.32f;
            float outer = size * 0.44f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
                pixels[y * size + x] = distance >= inner && distance <= outer
                    ? new Color32(255, 191, 51, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            SelectionRingSprite = Sprite.Create(texture,
                new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            SelectionRingSprite.name = "WorkerSelectionRing";
            return SelectionRingSprite;
        }
    }
}
