using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ResortIsland.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        public static ToastView Instance { get; private set; }

        [Header("Odkazy")]
        [SerializeField] private TMP_Text label;

        [Header("Časovanie (sekundy)")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float visibleDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Header("Posun pri zobrazení (v pixeloch)")]
        [SerializeField] private float slideDistance = 60f;

        [Header("Fronta")]
        [Tooltip("Zapnuté: správy čakajú v rade. Vypnuté: nová správa nahradí aktuálnu.")]
        [SerializeField] private bool useQueue = true;

        public float FadeInDuration => fadeInDuration;
        public float VisibleDuration => visibleDuration;
        public float FadeOutDuration => fadeOutDuration;
        public bool UseQueue => useQueue;

        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 shownPosition;
        private readonly Queue<string> queue = new Queue<string>();
        private Coroutine running;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            shownPosition = rect.anchoredPosition;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            rect.anchoredPosition = shownPosition + Vector2.up * slideDistance;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (running != null)
            {
                if (useQueue)
                {
                    queue.Enqueue(text);
                    return;
                }

                StopCoroutine(running);
            }

            running = StartCoroutine(Play(text));
        }

        private IEnumerator Play(string text)
        {
            label.text = text;
            yield return Animate(0f, 1f, fadeInDuration);
            yield return new WaitForSecondsRealtime(visibleDuration);
            yield return Animate(1f, 0f, fadeOutDuration);

            running = null;
            if (queue.Count > 0)
            {
                Show(queue.Dequeue());
            }
        }

        private IEnumerator Animate(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = k * k * (3f - 2f * k); 
                Apply(Mathf.Lerp(from, to, k));
                yield return null;
            }

            Apply(to);
        }

        private void Apply(float alpha)
        {
            group.alpha = alpha;
            rect.anchoredPosition = shownPosition + Vector2.up * slideDistance * (1f - alpha);
        }

        [ContextMenu("Test: ukáž toast")]
        private void TestShow()
        {
            Show("Testovacia správa");
        }

        [ContextMenu("Test: 3 toasty za sebou")]
        private void TestShowMany()
        {
            Show("Prvá správa");
            Show("Druhá správa");
            Show("Tretia správa");
        }
    }
}