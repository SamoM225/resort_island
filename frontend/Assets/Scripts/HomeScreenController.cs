using UnityEngine;
using ResortIsland.Economy;

namespace ResortIsland.UI
{
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Optional artwork")]
        [SerializeField] private Texture2D backgroundArt;

        private int displayedCoins;
        private int displayedGems;
        private int displayedEnergy;
        private int displayedMaxEnergy;
        private int displayedResortValue;
        private int displayedReputation;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;

        private void Awake()
        {
            if (backgroundArt != null)
                CreateBackground();
        }

        private void CreateBackground()
        {
            Sprite backgroundSprite = Sprite.Create(
                backgroundArt,
                new Rect(0, 0, backgroundArt.width, backgroundArt.height),
                new Vector2(0.5f, 0.5f),
                100f);
            GameObject background = new GameObject("Start Screen Background");
            background.transform.position = new Vector3(0f, 0f, 5f);
            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = backgroundSprite;
            renderer.sortingOrder = -100;

            float targetHeight = Camera.main.orthographicSize * 2f;
            float targetWidth = targetHeight * Camera.main.aspect;
            float scale = Mathf.Max(targetWidth / renderer.bounds.size.x, targetHeight / renderer.bounds.size.y);
            background.transform.localScale = Vector3.one * scale;
        }

        private void Start()
        {
            PlayerEconomy economy = FindFirstObjectByType<PlayerEconomy>();
            if (economy == null) return;

            displayedCoins = economy.Coins;
            displayedGems = economy.Gems;
            displayedEnergy = economy.Energy;
            displayedMaxEnergy = economy.MaxEnergy;
            displayedResortValue = economy.ResortValue;
            displayedReputation = economy.Reputation;
            economy.OnCoinsChanged += UpdateCoins;
            economy.OnGemsChanged += UpdateGems;
            economy.OnEnergyChanged += UpdateEnergy;
            economy.OnResortValueChanged += UpdateResortValue;
            economy.OnReputationChanged += UpdateReputation;
        }

        private void UpdateCoins(int value) => displayedCoins = value;
        private void UpdateGems(int value) => displayedGems = value;
        private void UpdateEnergy(int value) => displayedEnergy = value;
        private void UpdateResortValue(int value) => displayedResortValue = value;
        private void UpdateReputation(int value) => displayedReputation = value;

        private void OnDestroy()
        {
            PlayerEconomy economy = FindFirstObjectByType<PlayerEconomy>();
            if (economy == null) return;

            economy.OnCoinsChanged -= UpdateCoins;
            economy.OnGemsChanged -= UpdateGems;
            economy.OnEnergyChanged -= UpdateEnergy;
            economy.OnResortValueChanged -= UpdateResortValue;
            economy.OnReputationChanged -= UpdateReputation;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.width / 1280f, 0.75f, 1.35f);
            float margin = 18f * scale;
            float panelHeight = 78f * scale;
            GUI.Box(new Rect(margin, margin, Screen.width - margin * 2f, panelHeight), GUIContent.none, panelStyle);

            string[] labels = { "COINS", "GEMS", "ENERGY", "RESORT VALUE", "REPUTATION" };
            string[] values = { displayedCoins.ToString("N0"), displayedGems.ToString("N0"), displayedEnergy + " / " + displayedMaxEnergy, displayedResortValue.ToString("N0"), displayedReputation + " / 5" };
            Color[] colors = { new Color(1f, 0.73f, 0.22f), new Color(0.48f, 0.86f, 1f), new Color(0.35f, 0.9f, 0.57f), new Color(1f, 0.86f, 0.42f), new Color(1f, 0.55f, 0.4f) };
            float contentPadding = 36f * scale;
            float startX = margin + contentPadding;
            float itemWidth = (Screen.width - margin * 2f - contentPadding * 2f) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect item = new Rect(startX + itemWidth * i, margin + 12f * scale, itemWidth - 8f * scale, panelHeight - 20f * scale);
                GUI.color = colors[i];
                GUI.Label(new Rect(item.x, item.y, 22f * scale, 28f * scale), i == 0 ? "●" : i == 1 ? "◆" : "●", valueStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(item.x + 25f * scale, item.y, item.width - 25f * scale, 18f * scale), labels[i], labelStyle);
                GUI.Label(new Rect(item.x + 25f * scale, item.y + 19f * scale, item.width - 25f * scale, 30f * scale), values[i], valueStyle);
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeBackground(new Color(0.04f, 0.13f, 0.2f, 0.92f)) } };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.63f, 0.77f, 0.82f) } };
            valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private static Texture2D MakeBackground(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
