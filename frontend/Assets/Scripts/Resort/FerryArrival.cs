using System;
using UnityEngine;
using ResortIsland.Waves;

namespace ResortIsland.Resort
{
    public class FerryArrival : MonoBehaviour, IArrivalView
    {
        private enum Phase
        {
            Hidden,
            Arriving,
            Docked,
            Departing
        }

        [SerializeField] private Pier pier;
        [SerializeField] private ResortManager resort;
        [Tooltip("Ferry_R0 to Ferry_R3: bow to the bottom left, top left, top right and bottom right of the screen.")]
        [SerializeField] private Sprite[] sprites = new Sprite[4];
        [SerializeField, Min(0.1f)] private float sailSeconds = 2.5f;
        [SerializeField, Min(0f)] private float dockedSeconds = 1f;
        [SerializeField, Min(1f)] private float sailDistanceCells = 10f;

        private SpriteRenderer ferry;
        private Phase phase;
        private Vector3 sea;
        private Vector3 berth;
        private Vector3 start;
        private Vector3 end;
        private float elapsed;
        private Action docked;

        public void PlayArrival(Action onDocked)
        {
            if (pier == null || resort == null || !pier.HasDock || sprites == null || sprites.Length < 4)
            {
                onDocked?.Invoke();
                return;
            }

            Dock dock = pier.Dock;
            berth = resort.FootprintToWorld(dock.FerryOrigin, dock.FerrySize);
            Vector3 seaward = resort.CellToWorld(dock.LandingCell + dock.Seaward * 2) - resort.CellToWorld(dock.LandingCell + dock.Seaward);
            sea = berth + seaward * sailDistanceCells;
            docked = onDocked;

            EnsureFerry();
            ferry.gameObject.SetActive(true);
            Sail(Phase.Arriving, sea, berth);
        }

        private void Update()
        {
            if (phase == Phase.Hidden)
                return;

            elapsed += Time.deltaTime;
            float duration = phase == Phase.Docked ? dockedSeconds : sailSeconds;
            float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            Place(progress);

            if (progress < 1f)
                return;

            switch (phase)
            {
                case Phase.Arriving:
                    Sail(Phase.Docked, berth, berth);
                    NotifyDocked();
                    break;
                case Phase.Docked:
                    Sail(Phase.Departing, berth, sea);
                    break;
                default:
                    phase = Phase.Hidden;
                    ferry.gameObject.SetActive(false);
                    break;
            }
        }

        private void Sail(Phase next, Vector3 from, Vector3 to)
        {
            phase = next;
            elapsed = 0f;
            start = from;
            end = to;
            if (to != from)
                ferry.sprite = SpriteFacing(to - from);
            Place(0f);
        }

        private void Place(float progress)
        {
            ferry.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
            ResortSorting.Apply(ferry.gameObject);
        }

        private void NotifyDocked()
        {
            Action callback = docked;
            docked = null;
            callback?.Invoke();
        }

        private Sprite SpriteFacing(Vector3 heading)
        {
            if (heading.x < 0f)
                return heading.y < 0f ? sprites[0] : sprites[1];
            return heading.y >= 0f ? sprites[2] : sprites[3];
        }

        private void EnsureFerry()
        {
            if (ferry != null)
                return;

            GameObject go = new GameObject("Ferry", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            ferry = go.GetComponent<SpriteRenderer>();
        }
    }
}
