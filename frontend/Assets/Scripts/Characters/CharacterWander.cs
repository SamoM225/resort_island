using UnityEngine;
using ResortIsland.Resort;

namespace ResortIsland.Characters
{
    /// Test behaviour: walks between free cells of the island and rests in between.
    /// Lets you check the sprites, animations and sorting of workers and guests.
    [RequireComponent(typeof(CharacterView))]
    public class CharacterWander : MonoBehaviour
    {
        private static readonly Vector3Int[] Steps =
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        };

        [SerializeField, Min(0.05f)] private float speed = 0.6f;
        [SerializeField] private Vector2 restSeconds = new Vector2(0.5f, 2.5f);

        private ResortManager resort;
        private CharacterView view;
        private Vector3Int cell;
        private Vector3 target;
        private bool moving;
        private float restUntil;

        private void Start()
        {
            view = GetComponent<CharacterView>();
            resort = FindFirstObjectByType<ResortManager>();
            if (resort == null) { enabled = false; return; }

            cell = resort.WorldToCell(transform.position);
            Rest();
        }

        private void Update()
        {
            if (!resort.IsReady) return;

            if (!moving)
            {
                if (Time.time < restUntil) return;
                if (!TryPickStep()) { Rest(); return; }
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            ResortSorting.Apply(gameObject);

            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                moving = false;
                Rest();
            }
        }

        private bool TryPickStep()
        {
            int start = Random.Range(0, Steps.Length);
            for (int i = 0; i < Steps.Length; i++)
            {
                Vector3Int next = cell + Steps[(start + i) % Steps.Length];
                if (!resort.IsFree(next)) continue;

                target = resort.CellToWorld(next);
                view.FaceDelta(target - transform.position);
                view.SetState(CharacterState.Walk);
                cell = next;
                moving = true;
                return true;
            }
            return false;
        }

        private void Rest()
        {
            view.SetState(CharacterState.Idle);
            restUntil = Time.time + Random.Range(restSeconds.x, restSeconds.y);
        }
    }
}
