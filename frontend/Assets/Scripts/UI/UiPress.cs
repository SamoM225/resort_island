using UnityEngine;
using UnityEngine.EventSystems;

namespace ResortIsland.UI
{
    /// Squeezes a button a little while it is held down.
    public class UiPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.93f;
        private const float Speed = 18f;

        private bool pressed;

        /// Extra scale on top of the press (e.g. a selected tool is shown slightly larger).
        public float BaseScale { get; set; } = 1f;

        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnPointerExit(PointerEventData eventData) => pressed = false;

        private void OnDisable()
        {
            pressed = false;
            transform.localScale = Vector3.one * BaseScale;
        }

        private void Update()
        {
            float target = BaseScale * (pressed ? PressedScale : 1f);
            float current = transform.localScale.x;
            if (Mathf.Approximately(current, target)) return;
            float next = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(next - target) < 0.001f) next = target;
            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
