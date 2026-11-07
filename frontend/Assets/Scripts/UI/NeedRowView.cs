using TMPro;
using UnityEngine;

namespace ResortIsland.UI
{
    public class NeedRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text queueText;
        [SerializeField] private RectTransform fill;     
        [SerializeField] private GameObject warningIcon;

        public void Set(string needName, int served, int total, int queue, bool warning)
        {
            ApplyLayout();

            if (label != null) label.text = $"{needName}  {served}/{total}";
            if (queueText != null) queueText.text = $"Queue {queue}";

            if (fill != null)
            {
                float ratio = total > 0 ? Mathf.Clamp01((float)served / total) : 0f;
                fill.anchorMin = new Vector2(0f, 0f);
                fill.anchorMax = new Vector2(ratio, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }

            if (warningIcon != null) warningIcon.SetActive(warning);
        }

        private void ApplyLayout()
        {
            SetRect(label, 0f, 0.55f, 20f);
            SetRect(queueText, 0.55f, 0.80f, 0f);

            ConfigureText(label, TextAlignmentOptions.MidlineLeft, 14f, 40f);
            ConfigureText(queueText, TextAlignmentOptions.MidlineRight, 14f, 32f);
        }

        private static void SetRect(TMP_Text text, float xMin, float xMax, float left)
        {
            if (text == null) return;

            var r = text.rectTransform;
            r.anchorMin = new Vector2(xMin, 0f);
            r.anchorMax = new Vector2(xMax, 1f);
            r.offsetMin = new Vector2(left, 0f);
            r.offsetMax = Vector2.zero;
        }

        private static void ConfigureText(TMP_Text text, TextAlignmentOptions alignment, float minSize, float maxSize)
        {
            if (text == null) return;

            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
        }
    }
}