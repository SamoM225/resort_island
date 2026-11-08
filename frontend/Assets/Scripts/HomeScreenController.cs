using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Economy;

namespace ResortIsland.UI
{
    /// Home screen: the top toolbar (coins, gems, energy, resort value, reputation)
    /// built from the ResortEmpire UI sprites.
    public class HomeScreenController : MonoBehaviour
    {
        // Landscape layout, reference resolution = 844x390 design * 2.
        private static readonly Vector2 ReferenceResolution = new Vector2(1688f, 780f);

        // Toolbar_Plank.png is a sign of three stacked planks (1600x360). The toolbar uses one of them,
        // cut out of the texture: pixel rect measured from the art (outline included, bottom-left origin).
        private static readonly Vector2Int PlankSheetSize = new Vector2Int(1600, 360);
        private static readonly Rect PlankCrop = new Rect(56f, 229f, 1488f, 106f);
        // 9-slice borders (left, bottom, right, top); the rivets sit inside the 90 px side borders.
        private static readonly Vector4 PlankBorder = new Vector4(90f, 34f, 90f, 34f);

        private const float PlankHeight = 100f;
        private const float PlankMarginTop = 8f;
        private const float PlankMarginSide = 20f;
        private const float PlankPaddingSide = 100f;   // keeps content clear of the rivets
        private const float IconSize = 58f;
        private const float LabelSize = 17f;
        private const float ValueSize = 30f;

        private static readonly Color LabelColor = new Color32(0xF6, 0xE6, 0xC6, 0xFF);
        private static readonly Color ValueColor = Color.white;
        private static readonly Color OutlineColor = new Color32(0x26, 0x30, 0x3F, 0xFF);   // outline of the art
        private static readonly Color DividerColor = new Color32(0x26, 0x30, 0x3F, 0x66);

        [Header("Toolbar sprites (ResortEmpire/UI)")]
        [SerializeField] private Sprite plankSprite;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite gemIcon;
        [SerializeField] private Sprite energyIcon;
        [SerializeField] private Sprite resortValueIcon;
        [SerializeField] private Sprite reputationIcon;

        private Sprite croppedPlank;
        private TMP_Text coinsText;
        private TMP_Text gemsText;
        private TMP_Text energyText;
        private TMP_Text resortValueText;
        private TMP_Text reputationText;

        private int displayedCoins;
        private int displayedGems;
        private int displayedEnergy;
        private int displayedMaxEnergy;
        private int displayedResortValue;
        private int displayedReputation;

        private void Awake() => BuildToolbar();

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
            RefreshTexts();
        }

        private void UpdateCoins(int value) { displayedCoins = value; RefreshTexts(); }
        private void UpdateGems(int value) { displayedGems = value; RefreshTexts(); }
        private void UpdateEnergy(int value) { displayedEnergy = value; RefreshTexts(); }
        private void UpdateResortValue(int value) { displayedResortValue = value; RefreshTexts(); }
        private void UpdateReputation(int value) { displayedReputation = value; RefreshTexts(); }

        private void OnDestroy()
        {
            if (croppedPlank != null) Destroy(croppedPlank);

            PlayerEconomy economy = FindFirstObjectByType<PlayerEconomy>();
            if (economy == null) return;

            economy.OnCoinsChanged -= UpdateCoins;
            economy.OnGemsChanged -= UpdateGems;
            economy.OnEnergyChanged -= UpdateEnergy;
            economy.OnResortValueChanged -= UpdateResortValue;
            economy.OnReputationChanged -= UpdateReputation;
        }

        private void RefreshTexts()
        {
            if (coinsText != null) coinsText.text = displayedCoins.ToString("N0");
            if (gemsText != null) gemsText.text = displayedGems.ToString("N0");
            if (energyText != null) energyText.text = displayedEnergy + " / " + displayedMaxEnergy;
            if (resortValueText != null) resortValueText.text = displayedResortValue.ToString("N0");
            if (reputationText != null) reputationText.text = displayedReputation + " / 5";
        }

        // ---- toolbar ---------------------------------------------------------------------------

        private void BuildToolbar()
        {
            var canvasObject = new GameObject("Top Toolbar Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // No GraphicRaycaster on purpose: the toolbar must not swallow clicks meant for the island.
            RectTransform safeArea = NewRect("Safe Area", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform plank = NewRect("Toolbar Plank", safeArea);
            plank.anchorMin = new Vector2(0f, 1f);
            plank.anchorMax = new Vector2(1f, 1f);
            plank.pivot = new Vector2(0.5f, 1f);
            plank.offsetMin = new Vector2(PlankMarginSide, -(PlankMarginTop + PlankHeight));
            plank.offsetMax = new Vector2(-PlankMarginSide, -PlankMarginTop);
            var plankImage = plank.gameObject.AddComponent<Image>();
            croppedPlank = CropPlank();
            plankImage.sprite = croppedPlank != null ? croppedPlank : plankSprite;
            plankImage.type = Image.Type.Sliced;
            plankImage.raycastTarget = false;

            // one row: five equal slots separated by thin dividers
            RectTransform content = NewRect("Content", plank);
            Stretch(content);
            content.offsetMin = new Vector2(PlankPaddingSide, 6f);
            content.offsetMax = new Vector2(-PlankPaddingSide, -6f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;   // dividers keep their own (shorter) height
            row.spacing = 0f;

            coinsText = BuildSlot(content, "Coins", coinIcon, first: true);
            gemsText = BuildSlot(content, "Gems", gemIcon);
            energyText = BuildSlot(content, "Energy", energyIcon);
            resortValueText = BuildSlot(content, "Resort Value", resortValueIcon);
            reputationText = BuildSlot(content, "Reputation", reputationIcon);
            RefreshTexts();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        // One plank cut out of the three-plank sheet; null (use the original sprite) if the art changed size.
        private Sprite CropPlank()
        {
            if (plankSprite == null) return null;
            Texture2D texture = plankSprite.texture;
            if (texture == null || texture.width != PlankSheetSize.x || texture.height != PlankSheetSize.y) return null;

            return Sprite.Create(texture, PlankCrop, new Vector2(0.5f, 0.5f), plankSprite.pixelsPerUnit,
                0, SpriteMeshType.FullRect, PlankBorder);
        }

        // One stat: icon with a small label above the value, centred in its slot.
        private TMP_Text BuildSlot(RectTransform parent, string label, Sprite icon, bool first = false)
        {
            if (!first) BuildDivider(parent);

            RectTransform slot = NewRect(label, parent);
            var slotElement = slot.gameObject.AddComponent<LayoutElement>();
            slotElement.flexibleWidth = 1f;
            slotElement.minWidth = 150f;
            var slotLayout = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlWidth = true;
            slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;
            slotLayout.spacing = 12f;
            slotLayout.padding = new RectOffset(8, 8, 0, 0);

            RectTransform iconRect = NewRect("Icon", slot);
            var iconElement = iconRect.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = IconSize;
            iconElement.preferredHeight = IconSize;
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform column = NewRect("Text", slot);
            var columnLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            columnLayout.childAlignment = TextAnchor.MiddleLeft;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = false;
            columnLayout.childForceExpandHeight = false;
            columnLayout.spacing = -2f;

            NewText("Label", column, label.ToUpperInvariant(), LabelSize, LabelColor);
            TMP_Text valueText = NewText("Value", column, "0", ValueSize, ValueColor);
            valueText.outlineWidth = 0.2f;
            valueText.outlineColor = OutlineColor;
            return valueText;
        }

        private static void BuildDivider(RectTransform parent)
        {
            RectTransform divider = NewRect("Divider", parent);
            var element = divider.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 3f;
            element.preferredWidth = 3f;
            element.flexibleWidth = 0f;
            element.minHeight = 48f;
            element.preferredHeight = 48f;
            element.flexibleHeight = 0f;
            var image = divider.gameObject.AddComponent<Image>();
            image.color = DividerColor;
            image.raycastTarget = false;
        }

        private static TMP_Text NewText(string name, RectTransform parent, string text, float size, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = size;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
