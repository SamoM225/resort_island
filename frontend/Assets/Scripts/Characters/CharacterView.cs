using UnityEngine;

namespace ResortIsland.Characters
{
    public enum CharacterState
    {
        Rest,
        Walk,
        Action   // Cheer (guests), Work (workers), Wave (VIP)
    }

    /// Plays the ResortEmpire character sheets: 4 directions x (Rest 2, Walk 4, Action 2) frames.
    /// Directions are screen compass directions (the sheet rows): SE, SW, NW, NE.
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterView : MonoBehaviour
    {
        private const int FramesPerDirection = 8;   // Rest 0-1, Walk 0-3, Action 0-1
        private const int RestOffset = 0, WalkOffset = 2, ActionOffset = 6;

        [Tooltip("32 sprites, rows in the order SE, SW, NW, NE; per row 2 rest frames, Walk_0-3, Action_0, Action_1.")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float restFps = 2f;
        [SerializeField] private float walkFps = 8f;
        [SerializeField] private float actionFps = 4f;

        private SpriteRenderer spriteRenderer;
        private int direction;          // 0 SE, 1 SW, 2 NW, 3 NE
        private CharacterState state;
        private float clock;

        public CharacterState State => state;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            clock = Random.value * 4f;   // desynchronise characters
            Apply();
        }

        public void SetState(CharacterState newState)
        {
            if (state == newState) return;
            state = newState;
            clock = 0f;
            Apply();
        }

        /// Faces the way the character moves on screen (world-space delta).
        public void FaceDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return;
            int newDirection = delta.x >= 0f ? (delta.y < 0f ? 0 : 3) : (delta.y < 0f ? 1 : 2);
            if (newDirection == direction) return;
            direction = newDirection;
            Apply();
        }

        private void Update()
        {
            clock += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            if (frames == null || frames.Length < FramesPerDirection * 4 || spriteRenderer == null) return;

            int offset, count;
            float fps;
            switch (state)
            {
                case CharacterState.Walk: offset = WalkOffset; count = 4; fps = walkFps; break;
                case CharacterState.Action: offset = ActionOffset; count = 2; fps = actionFps; break;
                default: offset = RestOffset; count = 2; fps = restFps; break;
            }

            int frame = (int)(clock * fps) % count;
            spriteRenderer.sprite = frames[direction * FramesPerDirection + offset + frame];
        }
    }
}
