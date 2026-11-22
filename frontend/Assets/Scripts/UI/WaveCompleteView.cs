using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    public class WaveCompleteView : MonoBehaviour, IWaveCompleteView
    {
        [Header("Panel")]
        [SerializeField] private SlidePanel panel;

        [Header("Hviezdičky")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFull;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private GameObject perfectBadge;

        [Header("Texty")]
        [SerializeField] private TMP_Text satisfactionText;
        [SerializeField] private TMP_Text coinsText;

        [Header("Tlačidlo")]
        [SerializeField] private Button claimButton;

        public event Action Claimed;

        private void OnEnable()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);
        }

        private void OnDisable()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimClicked);
        }

        public void Show(int stars, bool perfect, int satisfactionPercent, int coins)
        {
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? starFull : starEmpty;

            if (perfectBadge != null)
                perfectBadge.SetActive(perfect);

            satisfactionText.text = $"{satisfactionPercent}%";
            coinsText.text = $"+{coins} Coins";

            panel.Open();
        }

        public void Hide() => panel.Close();

        private void OnClaimClicked() => Claimed?.Invoke();

        [ContextMenu("Preview: ukáž výsledok")]
        private void PreviewShow() => Show(4, false, 88, 120);

        [ContextMenu("Preview: ukáž Perfect")]
        private void PreviewPerfect() => Show(5, true, 100, 200);
    }
}