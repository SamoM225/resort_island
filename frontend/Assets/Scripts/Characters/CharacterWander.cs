using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Characters
{
    /// Guests and workers walk along the paths from building to building: to the cell in front of the
    /// door, in through the door (they disappear for a while) and out again. They leave the paths only
    /// where no path leads (e.g. from where they were spawned). Without buildings to visit they stroll
    /// along the paths, without paths they wander over free land.
    [RequireComponent(typeof(CharacterView))]
    public class CharacterWander : MonoBehaviour
    {
        // walking costs for PathFinder: stay on the paths, cut across land only when there is no other way
        private const int OffPathCost = 60;
        private const int OnPathCost = 10;
        private const int TurnCost = 2;
        private const float DoorDepth = 0.28f;   // how far towards the building the door is (world units)
        private const float DoorSeconds = 0.35f;

        [SerializeField, Min(0.05f)] private float speed = 0.6f;
        [SerializeField] private Vector2 restSeconds = new Vector2(0.5f, 2.5f);
        [Tooltip("How long a visit inside a building takes.")]
        [SerializeField] private Vector2 visitSeconds = new Vector2(2f, 5f);

        private enum Mode { Resting, Walking, Entering, Inside, Leaving }

        private ResortManager resort;
        private CharacterView view;
        private SpriteRenderer spriteRenderer;
        private readonly List<Vector3Int> route = new List<Vector3Int>();
        private int routeIndex;
        private Vector3Int cell;
        private Mode mode;
        private float timer;
        private PlacedBuilding target;
        private PlacedBuilding lastVisited;
        private Vector3 doorFrom;
        private Vector3 doorTo;

        private void Start()
        {
            view = GetComponent<CharacterView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            resort = FindFirstObjectByType<ResortManager>();
            if (resort == null) { enabled = false; return; }

            cell = resort.WorldToCell(transform.position);
            Rest();
        }

        private void Update()
        {
            if (!resort.IsReady) return;

            switch (mode)
            {
                case Mode.Resting:
                    if (Time.time >= timer) PlanNext();
                    break;
                case Mode.Walking:
                    Walk();
                    break;
                case Mode.Entering:
                    UpdateDoor(true);
                    break;
                case Mode.Inside:
                    if (Time.time >= timer) Leave();
                    break;
                case Mode.Leaving:
                    UpdateDoor(false);
                    break;
            }
        }

        // ---- planning ---------------------------------------------------------------------------

        private void PlanNext()
        {
            if (TryRouteToBuilding() || TryStroll() || TryRandomStep()) return;
            Rest();
        }

        /// A random building (not the one just visited) whose door the character can reach.
        private bool TryRouteToBuilding()
        {
            IReadOnlyList<PlacedBuilding> buildings = resort.Buildings;
            if (buildings.Count == 0) return false;

            int start = Random.Range(0, buildings.Count);
            for (int i = 0; i < buildings.Count; i++)
            {
                PlacedBuilding building = buildings[(start + i) % buildings.Count];
                if (building == null || (building == lastVisited && buildings.Count > 1)) continue;

                Vector3Int door = building.EntranceCell;
                if (!resort.IsUsableEntrance(door)) continue;
                if (!FindRoute(c => c == door)) continue;

                target = building;
                StartWalking();
                return true;
            }
            return false;
        }

        /// A few cells along the paths to a random path cell.
        private bool TryStroll()
        {
            PathNetwork paths = resort.Paths;
            if (paths == null || paths.Cells.Count == 0) return false;

            var candidates = new List<Vector3Int>();
            foreach (Vector3Int pathCell in paths.Cells.Keys)
                if (pathCell != cell && (pathCell - cell).sqrMagnitude <= 64) candidates.Add(pathCell);
            if (candidates.Count == 0) return false;

            Vector3Int goal = candidates[Random.Range(0, candidates.Count)];
            if (!FindRoute(c => c == goal)) return false;
            target = null;
            StartWalking();
            return true;
        }

        private bool TryRandomStep()
        {
            int first = Random.Range(0, PathFinder.Directions.Length);
            for (int i = 0; i < PathFinder.Directions.Length; i++)
            {
                Vector3Int next = cell + PathFinder.Directions[(first + i) % PathFinder.Directions.Length];
                if (!resort.IsFree(next)) continue;
                route.Clear();
                route.Add(cell);
                route.Add(next);
                target = null;
                StartWalking();
                return true;
            }
            return false;
        }

        private bool FindRoute(System.Func<Vector3Int, bool> isGoal)
        {
            PathNetwork paths = resort.Paths;
            bool OnPath(Vector3Int c) => paths != null && paths.Contains(c);
            bool Walkable(Vector3Int c) => c == cell || OnPath(c) || resort.IsFree(c);

            return PathFinder.TryFind(new[] { cell }, Walkable, isGoal, route, OnPath, OffPathCost, OnPathCost, TurnCost)
                   && route.Count > 1;
        }

        // ---- walking ----------------------------------------------------------------------------

        private void StartWalking()
        {
            routeIndex = 1;
            mode = Mode.Walking;
            view.SetState(CharacterState.Walk);
        }

        private void Walk()
        {
            if (routeIndex >= route.Count)
            {
                Arrive();
                return;
            }

            Vector3Int next = route[routeIndex];
            // a building was put on the way: plan again from here
            if (next != cell && !resort.IsFree(next) && (resort.Paths == null || !resort.Paths.Contains(next)))
            {
                Rest(0.2f);
                return;
            }

            Vector3 goal = resort.CellToWorld(next);
            view.FaceDelta(goal - transform.position);
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
            ResortSorting.Apply(gameObject);

            if ((transform.position - goal).sqrMagnitude < 0.0001f)
            {
                cell = next;
                routeIndex++;
            }
        }

        private void Arrive()
        {
            // the building moved or was turned meanwhile: walk to its door again
            if (target != null && target.EntranceCell != cell)
            {
                Rest(0.1f);
                return;
            }
            if (target == null)
            {
                Rest();
                return;
            }

            doorFrom = resort.CellToWorld(cell);
            Vector3 centre = resort.FootprintToWorld(target.Cell, target.Footprint);
            doorTo = doorFrom + (centre - doorFrom).normalized * DoorDepth;
            view.FaceDelta(doorTo - doorFrom);
            view.SetState(CharacterState.Walk);
            mode = Mode.Entering;
            timer = 0f;
        }

        // ---- going in and out of the door ---------------------------------------------------------

        private void UpdateDoor(bool entering)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / DoorSeconds);
            transform.position = entering ? Vector3.Lerp(doorFrom, doorTo, t) : Vector3.Lerp(doorTo, doorFrom, t);
            SetAlpha(entering ? 1f - t : t);
            ResortSorting.Apply(gameObject);
            if (t < 1f) return;

            if (entering)
            {
                spriteRenderer.enabled = false;
                lastVisited = target;
                mode = Mode.Inside;
                timer = Time.time + Random.Range(visitSeconds.x, visitSeconds.y);
            }
            else
            {
                target = null;
                Rest();
            }
        }

        private void Leave()
        {
            // come out of the door where it is now (the building may have been moved meanwhile)
            if (lastVisited != null)
            {
                cell = lastVisited.EntranceCell;
                doorFrom = resort.CellToWorld(cell);
                Vector3 centre = resort.FootprintToWorld(lastVisited.Cell, lastVisited.Footprint);
                doorTo = doorFrom + (centre - doorFrom).normalized * DoorDepth;
            }
            spriteRenderer.enabled = true;
            view.FaceDelta(doorFrom - doorTo);
            view.SetState(CharacterState.Walk);
            mode = Mode.Leaving;
            timer = 0f;
        }

        private void Rest(float? seconds = null)
        {
            mode = Mode.Resting;
            view.SetState(CharacterState.Rest);
            timer = Time.time + (seconds ?? Random.Range(restSeconds.x, restSeconds.y));
        }

        private void SetAlpha(float alpha)
        {
            if (spriteRenderer == null) return;
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
