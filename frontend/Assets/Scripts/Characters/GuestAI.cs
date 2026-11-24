using System;
using System.Collections.Generic;
using UnityEngine;
using ResortIsland.Config;
using ResortIsland.Resort;

namespace ResortIsland.Characters
{
    public enum GuestState
    {
        Arriving,
        EvaluatingNeeds,
        SelectingBuilding,
        MovingToBuilding,
        WaitingForService,
        ReceivingService,
        CheckingRemainingNeeds,
        MovingToExit,
        Leaving
    }

    [RequireComponent(typeof(CharacterView))]
    public sealed class GuestAI : MonoBehaviour
    {
        private const int OffPathCost = 60;
        private const int OnPathCost = 10;
        private const int TurnCost = 2;

        [SerializeField, Min(0.05f)] private float speed = 0.6f;
        [SerializeField, Min(0.1f)] private float serviceSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float retrySeconds = 0.75f;
        [SerializeField, Min(1)] private int maxSelectionRetries = 8;

        private readonly List<Vector3Int> route = new List<Vector3Int>();
        private readonly GuestNeedManager needs = new GuestNeedManager();
        private ResortManager resort;
        private Pier pier;
        private CharacterView view;
        private SpriteRenderer spriteRenderer;
        private PlacedBuilding target;
        private BuildingOccupancy targetOccupancy;
        private Vector3Int cell;
        private int routeIndex;
        private int selectionRetries;
        private float timer;
        private bool initialized;
        private GuestState state = GuestState.Arriving;

        public GuestState State => state;
        public IReadOnlyList<string> RemainingNeeds => needs.Remaining;

        public void Initialize(ResortManager resortManager, Pier arrivalPier, string primaryNeedId)
        {
            resort = resortManager;
            pier = arrivalPier;
            view = GetComponent<CharacterView>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (pier != null && pier.HasDock)
                cell = pier.Dock.LandingCell;
            else
                cell = resort != null ? resort.WorldToCell(transform.position) : Vector3Int.zero;
            if (resort != null)
            {
                transform.position = resort.CellToWorld(cell);
                ResortSorting.Apply(gameObject);
            }

            string secondaryNeedId = null;
            if (!string.IsNullOrEmpty(primaryNeedId) && resort != null &&
                UnityEngine.Random.value < 0.35f)
            {
                var alternatives = new List<string>();
                foreach (NeedConfig need in resort.ActiveNeeds)
                    if (need != null && need.Id != primaryNeedId)
                        alternatives.Add(need.Id);
                if (alternatives.Count > 0)
                    secondaryNeedId = alternatives[UnityEngine.Random.Range(0, alternatives.Count)];
            }

            needs.Set(primaryNeedId, secondaryNeedId);
            if (needs.IsEmpty && resort != null)
            {
                var defaults = new List<string>();
                foreach (NeedConfig need in resort.ActiveNeeds)
                    if (need != null && !string.IsNullOrEmpty(need.Id))
                        defaults.Add(need.Id);
                if (defaults.Count > 0)
                {
                    string selected = defaults[UnityEngine.Random.Range(0, defaults.Count)];
                    needs.Set(selected, secondaryNeedId);
                }
            }

            state = GuestState.Arriving;
            initialized = resort != null;
            if (initialized)
            {
                view.SetState(CharacterState.Rest);
                timer = Time.time + 0.25f;
            }
            else
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!initialized || resort == null || !resort.IsReady) return;

            switch (State)
            {
                case GuestState.Arriving:
                    if (Time.time >= timer) ChangeState(GuestState.EvaluatingNeeds);
                    break;
                case GuestState.EvaluatingNeeds:
                    ChangeState(needs.IsEmpty ? GuestState.MovingToExit : GuestState.SelectingBuilding);
                    break;
                case GuestState.SelectingBuilding:
                    SelectBuilding();
                    break;
                case GuestState.MovingToBuilding:
                    WalkToBuilding();
                    break;
                case GuestState.WaitingForService:
                    if (Time.time >= timer) TryOccupyTarget();
                    break;
                case GuestState.ReceivingService:
                    if (Time.time >= timer) CompleteService();
                    break;
                case GuestState.CheckingRemainingNeeds:
                    ChangeState(needs.IsEmpty ? GuestState.MovingToExit : GuestState.SelectingBuilding);
                    break;
                case GuestState.MovingToExit:
                    WalkToExit();
                    break;
                case GuestState.Leaving:
                    break;
            }
        }

        private void ChangeState(GuestState newState)
        {
            state = newState;
            switch (newState)
            {
                case GuestState.MovingToBuilding:
                case GuestState.MovingToExit:
                    view.SetState(CharacterState.Walk);
                    break;
                case GuestState.ReceivingService:
                    view.SetState(CharacterState.Action);
                    break;
                default:
                    view.SetState(CharacterState.Rest);
                    break;
            }
        }

        private void SelectBuilding()
        {
            if (needs.IsEmpty)
            {
                ChangeState(GuestState.MovingToExit);
                return;
            }

            string needId = needs.Remaining[0];
            PlacedBuilding best = null;
            int bestDistance = int.MaxValue;
            foreach (PlacedBuilding building in resort.Buildings)
            {
                if (building == null || building.Config == null || building.Config.ServedNeed == null)
                    continue;
                if (building.Config.ServedNeed.Id != needId)
                    continue;

                BuildingOccupancy occupancy = building.GetComponent<BuildingOccupancy>();
                if (occupancy == null)
                {
                    occupancy = building.gameObject.AddComponent<BuildingOccupancy>();
                    occupancy.Init(building.Config);
                }
                if (occupancy.Available <= 0)
                    continue;
                if (!resort.IsUsableEntrance(building.EntranceCell))
                    continue;
                if (!TryFindRoute(building.EntranceCell))
                    continue;

                int distance = Mathf.Abs(building.EntranceCell.x - cell.x) + Mathf.Abs(building.EntranceCell.y - cell.y);
                if (best == null || distance < bestDistance)
                {
                    best = building;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                selectionRetries++;
                if (selectionRetries >= maxSelectionRetries)
                {
                    needs.Fulfill(needId);
                    selectionRetries = 0;
                    ChangeState(needs.IsEmpty ? GuestState.MovingToExit : GuestState.SelectingBuilding);
                }
                else
                {
                    ChangeState(GuestState.WaitingForService);
                    timer = Time.time + retrySeconds;
                }
                return;
            }

            selectionRetries = 0;
            target = best;
            targetOccupancy = target.GetComponent<BuildingOccupancy>();
            if (!TryFindRoute(target.EntranceCell))
            {
                target = null;
                selectionRetries++;
                timer = Time.time + retrySeconds;
                ChangeState(GuestState.WaitingForService);
                return;
            }
            routeIndex = 1;
            ChangeState(GuestState.MovingToBuilding);
        }

        private void TryOccupyTarget()
        {
            if (target == null)
            {
                ChangeState(GuestState.SelectingBuilding);
                return;
            }
            if (targetOccupancy == null)
                targetOccupancy = target.GetComponent<BuildingOccupancy>();
            if (targetOccupancy != null && targetOccupancy.TryOccupy(this))
            {
                timer = Time.time + serviceSeconds;
                ChangeState(GuestState.ReceivingService);
            }
            else
            {
                timer = Time.time + retrySeconds;
                state = GuestState.WaitingForService;
            }
        }

        private void CompleteService()
        {
            if (targetOccupancy != null)
                targetOccupancy.Release(this);
            if (needs.Remaining.Count > 0)
                needs.Fulfill(needs.Remaining[0]);
            target = null;
            targetOccupancy = null;
            ChangeState(GuestState.CheckingRemainingNeeds);
        }

        private void WalkToBuilding()
        {
            if (target == null)
            {
                ChangeState(GuestState.SelectingBuilding);
                return;
            }

            if (WalkRoute())
                return;
            if (target.EntranceCell != cell)
            {
                if (TryFindRoute(target.EntranceCell))
                {
                    routeIndex = 1;
                    return;
                }
                target = null;
                timer = Time.time + retrySeconds;
                ChangeState(GuestState.WaitingForService);
                return;
            }

            TryOccupyTarget();
        }

        private void WalkToExit()
        {
            Vector3Int exit = pier != null && pier.HasDock ? pier.Dock.LandingCell : cell;
            if (TryWalkOneStep(exit))
            {
                ChangeState(GuestState.Leaving);
                Destroy(gameObject);
                return;
            }

            if (!TryFindRoute(exit))
            {
                ChangeState(GuestState.Leaving);
                Destroy(gameObject);
                return;
            }
            routeIndex = 1;
        }

        private bool TryWalkOneStep(Vector3Int goal)
        {
            if (cell == goal)
                return true;
            if (routeIndex >= route.Count)
            {
                if (!TryFindRoute(goal)) return false;
                routeIndex = 1;
            }
            WalkRoute();
            return cell == goal;
        }

        private bool WalkRoute()
        {
            if (routeIndex >= route.Count) return false;

            Vector3Int next = route[routeIndex];
            PathNetwork paths = resort.Paths;
            if (next != cell &&
                !resort.IsFree(next) &&
                (paths == null || !paths.Contains(next)))
            {
                return false;
            }

            Vector3 worldGoal = resort.CellToWorld(next);
            view.FaceDelta(worldGoal - transform.position);
            transform.position = Vector3.MoveTowards(transform.position, worldGoal, speed * Time.deltaTime);
            ResortSorting.Apply(gameObject);
            if ((transform.position - worldGoal).sqrMagnitude < 0.0001f)
            {
                cell = next;
                routeIndex++;
            }
            return true;
        }

        private bool TryFindRoute(Vector3Int goal)
        {
            PathNetwork paths = resort.Paths;
            bool OnPath(Vector3Int c) => paths != null && paths.Contains(c);
            bool Walkable(Vector3Int c) => c == cell || OnPath(c) || resort.IsFree(c);
            return PathFinder.TryFind(new[] { cell }, Walkable, c => c == goal, route,
                OnPath, OffPathCost, OnPathCost, TurnCost) && route.Count > 1;
        }
    }
}
