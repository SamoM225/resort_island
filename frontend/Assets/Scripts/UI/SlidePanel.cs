using System;
using UnityEngine;
using UnityEngine.Events;

namespace ResortIsland.UI
{
    public enum PanelEdge { Bottom, Top, Left, Right }

    [Serializable]
    public class PanelLayout
    {
        [SerializeField] private PanelEdge edge = PanelEdge.Bottom;
        [SerializeField, Range(0.1f, 1f)] private float size = 0.5f;

        public PanelLayout() { }

        public PanelLayout(PanelEdge edge, float size)
        {
            this.edge = edge;
            this.size = size;
        }

        public PanelEdge Edge => edge;
        public float Size => size;
    }

    [RequireComponent(typeof(RectTransform))]
    public class SlidePanel : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private PanelLayout portrait = new PanelLayout(PanelEdge.Bottom, 0.5f);
        [SerializeField] private PanelLayout landscape = new PanelLayout(PanelEdge.Right, 0.4f);
        [SerializeField] private bool adaptToAspect = true;   

        [Header("Animation")]
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool startOpen;

        [Header("Optional")]
        [SerializeField] private CanvasGroup backdrop;   
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private RectTransform rect;
        private float amount;   
        private float target;
        private int lastWidth;
        private int lastHeight;

        private RectTransform Rect => rect != null ? rect : (rect = (RectTransform)transform);

        public bool IsOpen => target > 0.5f;

        private void Awake()
        {
            amount = target = startOpen ? 1f : 0f;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                lastWidth = Screen.width;
                lastHeight = Screen.height;
                Apply();
            }

            if (Mathf.Approximately(amount, target)) return;

            float step = duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            amount = Mathf.MoveTowards(amount, target, step);
            Apply();

            if (Mathf.Approximately(amount, target))
                (target > 0.5f ? onOpened : onClosed)?.Invoke();
        }

        public void Open() => target = 1f;

        public void Close() => target = 0f;

        public void Toggle() => target = IsOpen ? 0f : 1f;

        public void SetOpen(bool open) => target = open ? 1f : 0f;

        private PanelLayout CurrentLayout()
        {
            if (!adaptToAspect) return portrait;
            return Screen.width >= Screen.height ? landscape : portrait;
        }

        private void Apply()
        {
            var layout = CurrentLayout();
            float s = layout.Size;

            Vector2 shownMin, shownMax, hiddenMin, hiddenMax;

            switch (layout.Edge)
            {
                case PanelEdge.Top:
                    shownMin = new Vector2(0f, 1f - s); shownMax = new Vector2(1f, 1f);
                    hiddenMin = new Vector2(0f, 1f); hiddenMax = new Vector2(1f, 1f + s);
                    break;
                case PanelEdge.Left:
                    shownMin = new Vector2(0f, 0f); shownMax = new Vector2(s, 1f);
                    hiddenMin = new Vector2(-s, 0f); hiddenMax = new Vector2(0f, 1f);
                    break;
                case PanelEdge.Right:
                    shownMin = new Vector2(1f - s, 0f); shownMax = new Vector2(1f, 1f);
                    hiddenMin = new Vector2(1f, 0f); hiddenMax = new Vector2(1f + s, 1f);
                    break;
                default:   
                    shownMin = new Vector2(0f, 0f); shownMax = new Vector2(1f, s);
                    hiddenMin = new Vector2(0f, -s); hiddenMax = new Vector2(1f, 0f);
                    break;
            }

            float t = curve.Evaluate(amount);
            Rect.anchorMin = Vector2.LerpUnclamped(hiddenMin, shownMin, t);
            Rect.anchorMax = Vector2.LerpUnclamped(hiddenMax, shownMax, t);
            Rect.offsetMin = Vector2.zero;
            Rect.offsetMax = Vector2.zero;

            if (backdrop != null)
            {
                backdrop.alpha = amount;
                backdrop.blocksRaycasts = amount > 0.01f;
                backdrop.interactable = amount > 0.5f;
            }
        }

        [ContextMenu("Preview: open")]
        private void PreviewOpen()
        {
            amount = target = 1f;
            Apply();
        }

        [ContextMenu("Preview: closed")]
        private void PreviewClosed()
        {
            amount = target = 0f;
            Apply();
        }
    }
}