using UnityEngine;
using ResortIsland.Economy;

namespace ResortIsland.UI
{
    public class HomeScreenController : MonoBehaviour
    {
        private int displayedCoins;
        private int displayedGems;
        private int displayedEnergy;
        private int displayedMaxEnergy;
        private int displayedResortValue;
        private int displayedReputation;
        private Sprite islandSprite;
        private Sprite[] buildingSprites;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle titleStyle;

        private void Awake()
        {
            CreateIsland();
            CreateBuildings();
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
        }

        private void UpdateCoins(int value) => displayedCoins = value;
        private void UpdateGems(int value) => displayedGems = value;

        private void OnDestroy()
        {
            PlayerEconomy economy = FindFirstObjectByType<PlayerEconomy>();
            if (economy == null) return;

            economy.OnCoinsChanged -= UpdateCoins;
            economy.OnGemsChanged -= UpdateGems;
        }

        private void CreateIsland()
        {
            islandSprite = Sprite.Create(CreateIslandTexture(), new Rect(0, 0, 512, 300), new Vector2(0.5f, 0.5f), 100);
            GameObject island = new GameObject("Island");
            island.transform.position = new Vector3(0f, -0.25f, 1f);
            SpriteRenderer renderer = island.AddComponent<SpriteRenderer>();
            renderer.sprite = islandSprite;
            renderer.sortingLayerName = "Ground";
        }

        private void CreateBuildings()
        {
            buildingSprites = new[]
            {
                Sprite.Create(CreateBuildingTexture(new Color(0.95f, 0.64f, 0.25f), new Color(0.83f, 0.28f, 0.25f)), new Rect(0, 0, 128, 160), new Vector2(0.5f, 0.12f), 100),
                Sprite.Create(CreateBuildingTexture(new Color(0.37f, 0.75f, 0.84f), new Color(0.16f, 0.37f, 0.58f)), new Rect(0, 0, 128, 160), new Vector2(0.5f, 0.12f), 100),
                Sprite.Create(CreateBuildingTexture(new Color(0.96f, 0.82f, 0.36f), new Color(0.28f, 0.58f, 0.38f)), new Rect(0, 0, 128, 160), new Vector2(0.5f, 0.12f), 100)
            };

            CreateBuilding("Beach Villas", buildingSprites[0], new Vector3(-1.2f, 0.25f, 0f), 4);
            CreateBuilding("Lagoon Hotel", buildingSprites[1], new Vector3(0.15f, 0.72f, -0.5f), 5);
            CreateBuilding("Palm Club", buildingSprites[2], new Vector3(1.15f, -0.05f, -1f), 6);
        }

        private static void CreateBuilding(string name, Sprite sprite, Vector3 position, int order)
        {
            GameObject building = new GameObject(name);
            building.transform.position = position;
            SpriteRenderer renderer = building.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "World";
            renderer.sortingOrder = order;
        }

        private static Texture2D CreateIslandTexture()
        {
            Texture2D texture = NewTexture(512, 300, new Color(0, 0, 0, 0));
            for (int y = 0; y < 300; y++)
            {
                float halfWidth = 16f + (y < 150 ? y : 299 - y) * 1.65f;
                int center = 256;
                for (int x = Mathf.Max(0, Mathf.FloorToInt(center - halfWidth)); x <= Mathf.Min(511, Mathf.CeilToInt(center + halfWidth)); x++)
                {
                    float edge = Mathf.Abs(x - center) / halfWidth;
                    Color color = edge > 0.9f ? new Color(0.92f, 0.72f, 0.3f) : new Color(0.27f, 0.67f, 0.38f);
                    if (y < 14 || y > 285) color = new Color(0.12f, 0.45f, 0.55f);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateBuildingTexture(Color wall, Color roof)
        {
            Texture2D texture = NewTexture(128, 160, new Color(0, 0, 0, 0));
            FillRect(texture, 35, 18, 58, 75, wall);
            FillRect(texture, 43, 94, 42, 42, roof);
            FillRect(texture, 48, 45, 13, 22, new Color(0.15f, 0.48f, 0.6f));
            FillRect(texture, 68, 45, 13, 22, new Color(0.15f, 0.48f, 0.6f));
            FillRect(texture, 58, 18, 12, 28, new Color(0.42f, 0.25f, 0.14f));
            FillRect(texture, 28, 12, 72, 7, new Color(0.12f, 0.36f, 0.3f));
            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int width, int height, Color clear)
        {
            Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            texture.SetPixels(pixels);
            return texture;
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width; px++)
                for (int py = y; py < y + height; py++)
                    texture.SetPixel(px, py, color);
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.width / 1280f, 0.75f, 1.35f);
            float margin = 18f * scale;
            float panelHeight = 78f * scale;
            GUI.Box(new Rect(margin, margin, Screen.width - margin * 2f, panelHeight), GUIContent.none, panelStyle);
            GUI.Label(new Rect(margin + 18f * scale, margin + 8f * scale, 210f * scale, 24f * scale), "ISLAND RESORT", titleStyle);

            string[] labels = { "COINS", "GEMS", "ENERGY", "RESORT VALUE", "REPUTATION" };
            string[] values = { displayedCoins.ToString("N0"), displayedGems.ToString("N0"), displayedEnergy + " / " + displayedMaxEnergy, displayedResortValue.ToString("N0"), displayedReputation + " / 5" };
            Color[] colors = { new Color(1f, 0.73f, 0.22f), new Color(0.48f, 0.86f, 1f), new Color(0.35f, 0.9f, 0.57f), new Color(1f, 0.86f, 0.42f), new Color(1f, 0.55f, 0.4f) };
            float startX = margin + 240f * scale;
            float itemWidth = (Screen.width - startX - margin) / labels.Length;
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
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.72f, 0.94f, 0.96f) } };
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
