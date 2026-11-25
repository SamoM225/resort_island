using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Config;
using ResortIsland.Economy;
using ResortIsland.Resort;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    /// Build mode: a "Build" button opens a bottom tray with two tabs.
    /// - Buildings: cards of the catalog buildings (count, price, lock); pick one and tap the island.
    /// - Paths: the path editor - tools on the left (Draw / Upgrade / Restyle / Erase), path style
    ///   cards in the tray, the level panel and X (undo everything) / Done (pay and keep) on top.
    /// Built in code like HomeScreenController, landscape reference 1688 x 780.
    public partial class BuildModeHud : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1688f, 780f);

        private static readonly Color Outline = UiShapes.Outline;
        // light, almost white panels: they stand out on the island (sand, grass and water)
        private static readonly Color Cream = new Color32(0xFF, 0xFD, 0xF7, 0xFF);
        private static readonly Color TabInactive = new Color32(0xEC, 0xE3, 0xD0, 0xFF);
        private static readonly Color CardFill = Color.white;
        private static readonly Color CardOutline = new Color32(0xE0, 0xD5, 0xBE, 0xFF);
        private static readonly Color SelectedFill = new Color32(0xEC, 0xF9, 0xE6, 0xFF);
        private static readonly Color SelectedOutline = new Color32(0x4C, 0xB0, 0x4A, 0xFF);
        private static readonly Color LockedFill = new Color32(0xF2, 0xEE, 0xE6, 0xFF);
        private static readonly Color TextDark = Outline;
        private static readonly Color TextMuted = new Color32(0x8A, 0x7E, 0x6B, 0xFF);
        private static readonly Color Green = new Color32(0x45, 0xB6, 0x4A, 0xFF);
        private static readonly Color GreenDark = new Color32(0x2F, 0x8C, 0x37, 0xFF);
        private static readonly Color Red = new Color32(0xE5, 0x53, 0x4B, 0xFF);
        private static readonly Color Purple = new Color32(0x8C, 0x5B, 0xD6, 0xFF);
        private static readonly Color HintFill = new Color(0.15f, 0.19f, 0.25f, 0.88f);
        private static readonly Color WarningText = new Color32(0xFF, 0xB4, 0xA8, 0xFF);
        private static readonly Color[] TierColors =
        {
            new Color32(0x8E, 0x9A, 0xA8, 0xFF), // T1
            new Color32(0x3B, 0x8F, 0xDB, 0xFF), // T2
            new Color32(0x8C, 0x5B, 0xD6, 0xFF), // T3
            new Color32(0xE8, 0xA3, 0x17, 0xFF)  // T4
        };

        private const float CardWidth = 196f;
        private const float CardHeight = 188f;
        private const float TrayHeight = 232f;
        private const float TrayBottom = 16f;
        private const float SideMargin = 100f;
        private const float ToolSize = 108f;
        private const float ToolStep = 122f;
        private const float TopRow = 30f;
        private const float SecondRow = 132f;
        private const float MessageSeconds = 2.5f;
        private const float OpenSeconds = 0.35f;

        private enum Tab { Buildings, Paths }

        [SerializeField] private ResortManager resort;
        [SerializeField] private BuildingPlacer placer;

        [Header("Sprites (ResortEmpire/UI)")]
        [SerializeField] private Sprite buttonOrange;
        [SerializeField] private Sprite buttonPurple;
        [SerializeField] private Sprite buttonBlue;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite hammerIcon;
        [SerializeField] private Sprite padlockIcon;

        private sealed class Card
        {
            public RectTransform Root;
            public Image Background;
            public Image Thumbnail;
            public TMP_Text Name;
            public Image PriceIcon;
            public TMP_Text Price;
            public Button Button;
            public int State = -1;
        }

        private sealed class ToolButton
        {
            public PathTool Tool;
            public RectTransform Root;
            public Image Ring;
            public UiPress Press;
        }


        private PlayerEconomy economy;
        private WaveController waves;
        private HomeScreenController home;
        private BuildMenuSkin skin;
        private SlidePanel needsPanel;

        private RectTransform openRoot;
        private RectTransform topGroup;
        private RectTransform toolsColumn;
        private RectTransform tray;
        private RectTransform pathContent;
        private RectTransform buildingContent;
        private ScrollRect scroll;
        private RectTransform doneButton;
        private RectTransform closeButton;
        private RectTransform rotateButton;
        private RectTransform levelPanel;
        private TMP_Text doneCostText;
        private TMP_Text hintText;
        private TMP_Text levelName;
        private TMP_Text levelDescription;
        private readonly Image[] levelButtons = new Image[3];
        private readonly TMP_Text[] levelLabels = new TMP_Text[3];
        private readonly Image[] tabImages = new Image[2];
        private readonly List<ToolButton> tools = new List<ToolButton>();
        private readonly List<KeyValuePair<PathStyle, Card>> pathCards = new List<KeyValuePair<PathStyle, Card>>();
        private readonly List<KeyValuePair<BuildingConfig, Card>> buildingCards = new List<KeyValuePair<BuildingConfig, Card>>();

        private bool built;
        private bool open;
        private Tab tab = Tab.Buildings;
        private string selectedStyle;
        private int selectedLevel = 1;
        private string message;
        private float messageUntil;
        private float openAmount;
        private float refreshTimer;

        private int ReachedWave => waves != null && waves.State != null ? waves.State.NextWave : 1;
        private PathStyleCatalog Styles => resort != null ? resort.PathStyles : null;

        // The menu must not depend on the scene being up to date: it is added at runtime when the scene
        // has none, and finds its references itself.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<BuildModeHud>() != null) return;
            BuildingPlacer scenePlacer = FindFirstObjectByType<BuildingPlacer>();
            if (scenePlacer != null) scenePlacer.gameObject.AddComponent<BuildModeHud>();
        }

        private void Awake()
        {
            // (no ?? here: a missing Unity component only compares equal to null with ==)
            if (resort == null) resort = GetComponent<ResortManager>();
            if (resort == null) resort = FindFirstObjectByType<ResortManager>();
            if (placer == null) placer = GetComponent<BuildingPlacer>();
            if (placer == null) placer = FindFirstObjectByType<BuildingPlacer>();

            skin = Resources.Load<BuildMenuSkin>("BuildMenuSkin");
            if (skin == null) return;
            if (buttonOrange == null) buttonOrange = skin.ButtonOrange;
            if (buttonPurple == null) buttonPurple = skin.ButtonPurple;
            if (buttonBlue == null) buttonBlue = skin.ButtonBlue;
            if (coinIcon == null) coinIcon = skin.CoinIcon;
            if (hammerIcon == null) hammerIcon = skin.HammerIcon;
            if (padlockIcon == null) padlockIcon = skin.PadlockIcon;
        }

        private void Start()
        {
            economy = FindFirstObjectByType<PlayerEconomy>();
            waves = FindFirstObjectByType<WaveController>();
            home = FindFirstObjectByType<HomeScreenController>();
            NeedsPanel needs = FindFirstObjectByType<NeedsPanel>(FindObjectsInactive.Include);
            if (needs != null) needsPanel = needs.GetComponent<SlidePanel>();
            if (placer != null) placer.BuildingPlaced += OnBuildingPlaced;
        }

        private void OnDestroy()
        {
            if (placer != null) placer.BuildingPlaced -= OnBuildingPlaced;
        }

        private void Update()
        {
            if (!built)
            {
                // the cards need the catalog, the placed buildings and the path styles
                if (resort == null || placer == null || !resort.IsReady) return;
                Build();
                built = true;
                TakeOverSceneHud(); // BuildModeHud.GameHud.cs
                Refresh();
            }

            Animate(Time.unscaledDeltaTime);

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.2f;
                Refresh();
            }
        }

        // ---- actions ------------------------------------------------------------------------


        private void OpenMenu(Tab start)
        {
            if (open) return;
            if (needsPanel != null) needsPanel.Close();
            open = true;
            openRoot.gameObject.SetActive(true);
            SetGameHudForBuildMode(true);
            if (!EnterTab(start)) EnterTab(Tab.Buildings);
        }

        private void CloseMenu()
        {
            placer.CancelPlacement();
            placer.HighlightSelection = false;
            open = false;
            openRoot.gameObject.SetActive(false);
            SetGameHudForBuildMode(false);
            openAmount = 0f;
        }


        private void OnClose()
        {
            if (tab == Tab.Paths) resort.CancelPathEdit();
            CloseMenu();
        }

        private void OnDone()
        {
            if (tab == Tab.Paths && !TryCommitPaths()) return;
            CloseMenu();
        }

        private bool TryCommitPaths()
        {
            if (!resort.IsEditingPaths) return true;
            int cost = resort.PendingPathCost;
            if (cost > 0 && economy != null && !economy.TrySpendCoins(cost))
            {
                ShowMessage("Not enough coins · remove some tiles or press X to undo");
                return false;
            }
            int value = resort.PendingPathValue;
            resort.CommitPathEdit();
            economy?.AddResortValue(value);
            return true;
        }

        private bool SwitchTab(Tab next)
        {
            if (next == tab) return true;
            if (next == Tab.Paths && !PathsAvailable()) return false;
            // leaving the path editor keeps (and pays for) the changes
            if (tab == Tab.Paths && !TryCommitPaths()) return false;
            return EnterTab(next);
        }

        private bool PathsAvailable()
        {
            if (resort.PathsReady && Styles != null) return true;
            ShowMessage("Paths are not ready yet");
            return false;
        }

        private bool EnterTab(Tab next)
        {
            if (next == Tab.Paths && !PathsAvailable()) return false;

            placer.CancelPlacement();
            tab = next;
            message = null;
            RefreshTabs();

            bool paths = tab == Tab.Paths;
            toolsColumn.gameObject.SetActive(paths);
            rotateButton.gameObject.SetActive(!paths);
            placer.HighlightSelection = !paths;
            levelPanel.gameObject.SetActive(paths);
            doneButton.gameObject.SetActive(paths);
            closeButton.anchoredPosition = new Vector2(paths ? -(SideMargin + 300f + 18f) : -SideMargin, -TopRow);
            pathContent.gameObject.SetActive(paths);
            buildingContent.gameObject.SetActive(!paths);
            scroll.content = paths ? pathContent : buildingContent;
            scroll.horizontalNormalizedPosition = 0f;
            tray.offsetMin = new Vector2(paths ? SideMargin + ToolSize + 32f : SideMargin, tray.offsetMin.y);

            if (paths)
            {
                resort.BeginPathEdit();
                PathStyle style = Styles.Find(selectedStyle);
                if (style == null || !IsUnlocked(style)) style = FirstUnlockedStyle();
                SelectStyle(style);
                SelectTool(PathTool.Draw);
            }

            Refresh();
            return true;
        }

        private bool IsUnlocked(PathStyle style) => resort.IsPathStyleUnlocked(style.Id, ReachedWave);

        private PathStyle FirstUnlockedStyle()
        {
            foreach (PathStyle style in Styles.Styles)
                if (style != null && IsUnlocked(style)) return style;
            return Styles.FindOrDefault(null);
        }

        private void SelectTool(PathTool tool)
        {
            placer.PathStyle = selectedStyle;
            placer.PathLevel = selectedLevel;
            placer.SelectPathTool(tool);
            message = null;
            Refresh();
        }

        private void SelectStyle(PathStyle style)
        {
            if (style == null) return;
            if (!IsUnlocked(style))
            {
                ShowMessage(style.DisplayName + " unlocks at wave " + resort.Tuning.PathUnlockWave(style.Id));
                return;
            }

            selectedStyle = style.Id;
            selectedLevel = 1;
            placer.PathStyle = selectedStyle;
            placer.PathLevel = selectedLevel;
            if (placer.ActivePathTool == PathTool.None || placer.ActivePathTool == PathTool.Erase)
                SelectTool(PathTool.Draw);
            Refresh();
        }

        private void SelectLevel(int level)
        {
            PathStyle style = Styles?.Find(selectedStyle);
            if (style == null || level > style.LevelCount) return;
            selectedLevel = level;
            placer.PathLevel = level;
            if (placer.ActivePathTool == PathTool.None || placer.ActivePathTool == PathTool.Erase)
                SelectTool(PathTool.Draw);
            Refresh();
        }

        private void SelectBuilding(BuildingConfig config)
        {
            if (!Unlocks.Has(config.UnlockId))
                ShowMessage(config.DisplayName + " is still locked");
            else if (resort.CountOf(config) >= resort.MaxCountOf(config))
                ShowMessage("All " + resort.MaxCountOf(config) + " are already built");
            else if (resort.CostOf(config) > 0 && economy != null && economy.Coins < resort.CostOf(config))
                ShowMessage("Not enough coins for " + config.DisplayName);
            else if (placer.SelectBuilding(config))
                message = null;
            Refresh();
        }

        private void OnBuildingPlaced(BuildingConfig config)
        {
            int cost = resort.CostOf(config);
            if (cost > 0 && economy != null && !economy.TrySpendCoins(cost))
                Debug.LogWarning("[BuildModeHud] " + config.Id + " was placed but the coins were spent meanwhile.");
            economy?.AddResortValue(resort.ValueOf(config));
            Refresh();
        }

        public void ShowMessage(string text)
        {
            if (!string.IsNullOrEmpty(text) && toast != null && toastText != null)
            {
                ShowToast(text);
                return;
            }
            message = text;
            messageUntil = Time.unscaledTime + MessageSeconds;
            Refresh();
        }

        // ---- refresh ------------------------------------------------------------------------

        private void Refresh()
        {
            if (!built) return;
            RefreshGameHud();
            if (!open) return;

            int coins = economy != null ? economy.Coins : 0;

            if (message != null && Time.unscaledTime > messageUntil) message = null;
            hintText.text = message ?? HintText();
            hintText.color = message != null ? WarningText : Color.white;

            if (tab == Tab.Paths) RefreshPaths(coins);
            else RefreshBuildings(coins);
        }


        private string HintText()
        {
            if (tab == Tab.Buildings)
            {
                BuildingConfig selected = placer.SelectedBuilding;
                if (selected != null)
                    return "Tap the island to place " + selected.DisplayName + " · R or Rotate turns it · the yellow tile is its entrance";
                if (placer.SelectedPlaced != null)
                    return "Drag to move · R, right click or Rotate turns it · paths lead to the yellow entrance";
                return "Pick a building · tap one on the island to select it, drag it to move it";
            }

            PathStyle style = Styles?.Find(selectedStyle);
            string name = style != null ? style.DisplayName : "path";
            switch (placer.ActivePathTool)
            {
                case PathTool.Draw: return "Drag across tiles to draw · corners and crossings join by themselves";
                case PathTool.Upgrade: return "Drag over " + name + " to raise it to Lv" + selectedLevel;
                case PathTool.Restyle: return "Drag over any path to turn it into " + name;
                case PathTool.Erase: return "Drag over a path to remove it";
                default: return "Pick a tool on the left";
            }
        }

        private void RefreshPaths(int coins)
        {
            int cost = resort.PendingPathCost;
            doneCostText.text = cost.ToString("N0");
            doneCostText.color = economy == null || cost <= coins ? Color.white : WarningText;

            int wave = ReachedWave;
            foreach (var pair in pathCards)
            {
                PathStyle style = pair.Key;
                bool locked = !resort.IsPathStyleUnlocked(style.Id, wave);
                int state = locked ? 2 : style.Id == selectedStyle ? 1 : 0;
                if (SetCardState(pair.Value, state))
                {
                    int price = resort.PathCostPerTile(style.Id, 1);
                    pair.Value.Price.text = locked ? "Wave " + resort.Tuning.PathUnlockWave(style.Id) : price == 0 ? "Free" : price + "/tile";
                    pair.Value.PriceIcon.sprite = locked ? padlockIcon : coinIcon;
                }
            }

            PathStyle selected = Styles.Find(selectedStyle);
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool exists = selected != null && i < selected.LevelCount;
                levelButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool on = i + 1 == selectedLevel;
                levelButtons[i].sprite = on ? UiShapes.Button(Purple, 18, 4) : UiShapes.Panel(CardFill, Outline, 18, 4);
                levelLabels[i].color = on ? Color.white : TextDark;
            }
            PathStyleLevel level = selected?.Level(selectedLevel);
            levelName.text = level != null ? level.Name : "";
            levelDescription.text = level != null
                ? PathStyleLevel.Describe(resort.Tuning.PathLevel(selected.Id, selected.ClampLevel(selectedLevel)))
                : "";

            foreach (ToolButton tool in tools)
            {
                bool active = placer.ActivePathTool == tool.Tool;
                tool.Ring.enabled = active;
                tool.Press.BaseScale = active ? 1.06f : 1f;
            }
        }

        private void RefreshBuildings(int coins)
        {
            rotateButton.gameObject.SetActive(true);

            foreach (var pair in buildingCards)
            {
                BuildingConfig config = pair.Key;
                bool locked = !Unlocks.Has(config.UnlockId);
                int count = resort.CountOf(config);
                int maxCount = resort.MaxCountOf(config);
                int cost = resort.CostOf(config);
                bool full = count >= maxCount;
                int state = locked || full ? 2 : placer.SelectedBuilding == config ? 1 : 0;
                SetCardState(pair.Value, state);

                pair.Value.PriceIcon.sprite = locked ? padlockIcon : coinIcon;
                pair.Value.PriceIcon.enabled = !full || locked;
                pair.Value.Price.text = locked ? "Locked"
                    : full ? count + "/" + maxCount + " built"
                    : (cost == 0 ? "Free" : cost.ToString("N0")) + "  ·  " + count + "/" + maxCount;
                pair.Value.Price.color = !locked && !full && cost > coins ? Red : TextDark;
            }
        }

        // 0 normal, 1 selected, 2 locked; true when the state changed
        private bool SetCardState(Card card, int state)
        {
            if (card.State == state) return false;
            card.State = state;
            card.Background.sprite = state == 1 ? UiShapes.Panel(SelectedFill, SelectedOutline, 20, 5)
                : state == 2 ? UiShapes.Panel(LockedFill, CardOutline, 20, 4)
                : UiShapes.Panel(CardFill, CardOutline, 20, 4);
            card.Thumbnail.color = state == 2 ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            card.Name.color = state == 2 ? TextMuted : TextDark;
            return true;
        }

        // ---- open / close animation ---------------------------------------------------------

        private void Animate(float dt)
        {
            if (!built) return;

            // the bottom bar goes down while the build menu is open and comes back after
            float barTarget = open ? 0f : 1f;
            if (!Mathf.Approximately(barAmount, barTarget))
            {
                barAmount = Mathf.MoveTowards(barAmount, barTarget, dt / OpenSeconds);
                float b = open ? barAmount * barAmount : EaseOutBack(barAmount);
                mainBar.anchoredPosition = new Vector2(mainBar.anchoredPosition.x,
                    Mathf.LerpUnclamped(-MainBarHeight - 60f, TrayBottom, b));
            }

            if (!open || openAmount >= 1f) return;
            openAmount = Mathf.Min(1f, openAmount + dt / OpenSeconds);
            float t = EaseOutBack(openAmount);
            tray.anchoredPosition = new Vector2(tray.anchoredPosition.x, Mathf.LerpUnclamped(-TrayHeight - 40f, TrayBottom, t));
            toolsColumn.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-ToolSize - 40f, SideMargin, t), -SecondRow);
            topGroup.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(160f, 0f, t));
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.4f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // ---- building the UI ----------------------------------------------------------------

        private void Build()
        {
            var canvasObject = new GameObject("Build Mode Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform safeArea = NewRect("Safe Area", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            BuildGameHud(safeArea);

            openRoot = NewRect("Build Mode", safeArea);
            Stretch(openRoot);
            BuildTray(openRoot);
            BuildTools(openRoot);
            BuildRotateButton(openRoot);
            BuildTop(openRoot);
            openRoot.gameObject.SetActive(false);
        }


        private void BuildTop(RectTransform parent)
        {
            topGroup = NewRect("Top", parent);
            Stretch(topGroup);


            // X and Done
            RectTransform close = closeButton = NewRect("Close", topGroup);
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(SideMargin + 300f + 18f), -TopRow), new Vector2(84f, 84f));
            AddImage(close, UiShapes.Button(Red), true).type = Image.Type.Sliced;
            RectTransform cross = NewRect("Icon", close);
            Place(cross, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));
            AddImage(cross, UiShapes.Glyph(UiGlyph.Close), false);
            AddButton(close, OnClose);

            doneButton = NewRect("Done", topGroup);
            Place(doneButton, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-SideMargin, -TopRow), new Vector2(300f, 84f));
            AddImage(doneButton, UiShapes.Button(Green), true).type = Image.Type.Sliced;
            TMP_Text done = NewText("Label", doneButton, "Done", 34f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)done.transform, 28f, 6f, 150f, 0f);
            Outlined(done);
            RectTransform costPill = NewRect("Cost", doneButton);
            Place(costPill, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 4f), new Vector2(150f, 54f), new Vector2(1f, 0.5f));
            AddImage(costPill, UiShapes.Panel(GreenDark, GreenDark, 26, 0), false).type = Image.Type.Sliced;
            RectTransform costCoin = NewRect("Coin", costPill);
            Place(costCoin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(44f, 44f), new Vector2(0f, 0.5f));
            AddImage(costCoin, coinIcon, false).preserveAspect = true;
            doneCostText = NewText("Value", costPill, "0", 30f, Color.white, TextAlignmentOptions.Midline);
            Stretch((RectTransform)doneCostText.transform, 50f, 0f, 8f, 0f);
            AddButton(doneButton, OnDone);

            // hint
            RectTransform hint = NewRect("Hint", topGroup);
            Place(hint, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-150f, -SecondRow), new Vector2(820f, 92f), new Vector2(0.5f, 1f));
            AddImage(hint, UiShapes.Panel(HintFill, HintFill, 22, 0), false).type = Image.Type.Sliced;
            hintText = NewText("Text", hint, "", 26f, Color.white, TextAlignmentOptions.Center);
            hintText.textWrappingMode = TextWrappingModes.Normal;
            Stretch((RectTransform)hintText.transform, 24f, 8f, 24f, 8f);

            BuildLevelPanel(topGroup);
        }

        private void BuildLevelPanel(RectTransform parent)
        {
            levelPanel = NewRect("Level", parent);
            Place(levelPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-SideMargin, -SecondRow), new Vector2(400f, 236f));
            AddImage(levelPanel, UiShapes.Panel(Cream, Outline, 26, 5), true).type = Image.Type.Sliced;

            TMP_Text caption = NewText("Caption", levelPanel, "LEVEL", 20f, TextMuted, TextAlignmentOptions.TopLeft);
            Place((RectTransform)caption.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -18f), new Vector2(-52f, 26f), new Vector2(0f, 1f));

            for (int i = 0; i < levelButtons.Length; i++)
            {
                int level = i + 1;
                RectTransform button = NewRect("Lv" + level, levelPanel);
                Place(button, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f + i * 120f, -52f), new Vector2(108f, 60f));
                levelButtons[i] = AddImage(button, UiShapes.Panel(CardFill, Outline, 18, 4), true);
                levelButtons[i].type = Image.Type.Sliced;
                levelLabels[i] = NewText("Label", button, "Lv" + level, 26f, TextDark, TextAlignmentOptions.Center);
                Stretch((RectTransform)levelLabels[i].transform, 0f, 4f, 0f, 0f);
                AddButton(button, () => SelectLevel(level));
            }

            levelName = NewText("Name", levelPanel, "", 32f, TextDark, TextAlignmentOptions.TopLeft);
            Place((RectTransform)levelName.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -128f), new Vector2(-52f, 40f), new Vector2(0f, 1f));
            levelDescription = NewText("Description", levelPanel, "", 22f, TextMuted, TextAlignmentOptions.TopLeft);
            levelDescription.fontStyle = FontStyles.Normal;
            levelDescription.textWrappingMode = TextWrappingModes.Normal;
            Place((RectTransform)levelDescription.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -172f), new Vector2(-52f, 56f), new Vector2(0f, 1f));
        }

        private void BuildTools(RectTransform parent)
        {
            toolsColumn = NewRect("Tools", parent);
            Place(toolsColumn, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(SideMargin, -SecondRow), new Vector2(ToolSize, ToolStep * 4f));

            AddTool(PathTool.Draw, "Draw", buttonOrange, UiGlyph.Pencil, Color.white);
            AddTool(PathTool.Upgrade, "Upgrade", buttonPurple, UiGlyph.ArrowUp, Color.white);
            AddTool(PathTool.Restyle, "Restyle", buttonBlue, UiGlyph.Roller, Color.white);
            AddTool(PathTool.Erase, "Erase", null, UiGlyph.Eraser, Color.white);
        }

        // Buildings tab: turns the building being placed or the selected one
        private void BuildRotateButton(RectTransform parent)
        {
            rotateButton = NewRect("Rotate", parent);
            Place(rotateButton, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(SideMargin, -SecondRow), new Vector2(ToolSize, ToolSize));
            BuildIconButton(rotateButton, buttonBlue, UiShapes.Glyph(UiGlyph.Rotate), Color.white, "Rotate", 56f);
            rotateButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!placer.CanRotate)
                    ShowMessage("Tap a building on the island first (or pick one below), then turn it");
                else if (!placer.RotateSelection())
                    ShowMessage("It does not fit turned this way here");
                else if (placer.SelectedPlaced != null && !resort.IsUsableEntrance(placer.SelectedPlaced.EntranceCell, placer.SelectedPlaced))
                    ShowMessage("Its entrance is blocked now (red) · paths and guests cannot reach it");
                Refresh();
            });
            rotateButton.gameObject.SetActive(false);
        }

        private void AddTool(PathTool tool, string label, Sprite background, UiGlyph glyph, Color tint)
        {
            RectTransform root = NewRect(label, toolsColumn);
            Place(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -tools.Count * ToolStep - ToolSize * 0.5f),
                new Vector2(ToolSize, ToolSize), new Vector2(0.5f, 0.5f));

            // white ring around the active tool
            RectTransform ring = NewRect("Selected", root);
            Stretch(ring, -9f, -9f, -9f, -9f);
            Image ringImage = AddImage(ring, UiShapes.Panel(Color.clear, Color.white, 32, 6), false);
            ringImage.type = Image.Type.Sliced;
            ringImage.enabled = false;

            RectTransform body = NewRect("Body", root);
            Stretch(body);
            if (background != null) AddImage(body, background, true);
            else AddImage(body, UiShapes.Button(Red, 22, 4), true).type = Image.Type.Sliced;

            RectTransform icon = NewRect("Icon", root);
            Place(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(52f, 52f), new Vector2(0.5f, 1f));
            AddImage(icon, UiShapes.Glyph(glyph), false).color = tint;
            TMP_Text text = NewText("Label", root, label, 20f, Color.white, TextAlignmentOptions.Midline);
            Place((RectTransform)text.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 26f), new Vector2(0.5f, 0f));
            Outlined(text);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectTool(tool));
            UiPress press = root.gameObject.AddComponent<UiPress>();

            tools.Add(new ToolButton { Tool = tool, Root = root, Ring = ringImage, Press = press });
        }

        private void BuildTray(RectTransform parent)
        {
            tray = NewRect("Tray", parent);
            tray.anchorMin = new Vector2(0f, 0f);
            tray.anchorMax = new Vector2(1f, 0f);
            tray.pivot = new Vector2(0.5f, 0f);
            tray.offsetMin = new Vector2(SideMargin, TrayBottom);
            tray.offsetMax = new Vector2(-SideMargin, TrayBottom + TrayHeight);

            BuildTab(tray, Tab.Buildings, "Buildings", hammerIcon, 0);
            BuildTab(tray, Tab.Paths, "Paths", PathTabIcon(), 1);

            RectTransform background = NewRect("Background", tray);
            Stretch(background);
            AddImage(background, UiShapes.Panel(Cream, Outline, 30, 5), true).type = Image.Type.Sliced;

            RectTransform viewport = NewRect("Viewport", tray);
            Stretch(viewport, 14f, 10f, 14f, 10f);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport, null, true).color = Color.clear;

            scroll = tray.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.viewport = viewport;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            pathContent = NewCardRow("Path Cards", viewport);
            buildingContent = NewCardRow("Building Cards", viewport);

            PathStyleCatalog styles = Styles;
            if (styles != null)
                foreach (PathStyle style in styles.Styles)
                {
                    if (style == null) continue;
                    PathStyle captured = style;
                    Card card = NewCard(pathContent, style.DisplayName, styles.GetSprite(style.Id, 1, 15), "T" + style.Tier,
                        TierColors[Mathf.Clamp(style.Tier, 1, 4) - 1], () => SelectStyle(captured));
                    pathCards.Add(new KeyValuePair<PathStyle, Card>(style, card));
                }

            foreach (BuildingConfig config in resort.Catalog)
            {
                if (config == null) continue;
                BuildingConfig captured = config;
                Card card = NewCard(buildingContent, config.DisplayName, BuildingThumbnail(config),
                    config.Footprint.x + "×" + config.Footprint.y, TierColors[0], () => SelectBuilding(captured));
                buildingCards.Add(new KeyValuePair<BuildingConfig, Card>(config, card));
            }

            pathContent.gameObject.SetActive(false);
            scroll.content = buildingContent;
        }

        private void BuildTab(RectTransform trayRect, Tab tabId, string label, Sprite icon, int index)
        {
            RectTransform tabRect = NewRect(label + " Tab", trayRect);
            Place(tabRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f + index * 222f, -6f), new Vector2(210f, 66f), new Vector2(0f, 0f));
            Image image = AddImage(tabRect, UiShapes.Panel(TabInactive, Outline, 22, 5), true);
            image.type = Image.Type.Sliced;
            tabImages[index] = image;

            RectTransform iconRect = NewRect("Icon", tabRect);
            Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 4f), new Vector2(48f, 48f), new Vector2(0f, 0.5f));
            AddImage(iconRect, icon, false).preserveAspect = true;
            TMP_Text text = NewText("Label", tabRect, label, 26f, TextDark, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)text.transform, 70f, 8f, 10f, 0f);

            AddButton(tabRect, () => SwitchTab(tabId));
        }

        private void RefreshTabs()
        {
            tabImages[0].sprite = UiShapes.Panel(tab == Tab.Buildings ? Cream : TabInactive, Outline, 22, 5);
            tabImages[1].sprite = UiShapes.Panel(tab == Tab.Paths ? Cream : TabInactive, Outline, 22, 5);
        }

        private Card NewCard(RectTransform parent, string title, Sprite thumbnail, string badge, Color badgeColor, UnityEngine.Events.UnityAction onClick)
        {
            var card = new Card();
            card.Root = NewRect(title, parent);
            var element = card.Root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;
            card.Background = AddImage(card.Root, UiShapes.Panel(CardFill, CardOutline, 20, 4), true);
            card.Background.type = Image.Type.Sliced;

            RectTransform thumb = NewRect("Thumbnail", card.Root);
            Place(thumb, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(CardWidth - 40f, 86f), new Vector2(0.5f, 1f));
            card.Thumbnail = AddImage(thumb, thumbnail, false);
            card.Thumbnail.preserveAspect = true;
            card.Thumbnail.enabled = thumbnail != null;

            card.Name = NewText("Name", card.Root, title, 24f, TextDark, TextAlignmentOptions.Midline);
            card.Name.enableAutoSizing = true;
            card.Name.fontSizeMin = 16f;
            card.Name.fontSizeMax = 24f;
            Place((RectTransform)card.Name.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 50f), new Vector2(-16f, 32f), new Vector2(0.5f, 0f));

            RectTransform priceRow = NewRect("Price", card.Root);
            Place(priceRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), new Vector2(-16f, 34f), new Vector2(0.5f, 0f));
            var row = priceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.spacing = 6f;
            RectTransform priceIcon = NewRect("Icon", priceRow);
            var iconElement = priceIcon.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = 32f;
            iconElement.preferredHeight = 32f;
            card.PriceIcon = AddImage(priceIcon, coinIcon, false);
            card.PriceIcon.preserveAspect = true;
            card.Price = NewText("Value", priceRow, "", 22f, TextDark, TextAlignmentOptions.MidlineLeft);

            // tier / size badge over the top-left corner
            RectTransform badgeRect = NewRect("Badge", card.Root);
            Place(badgeRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 8f), new Vector2(58f, 34f), new Vector2(0f, 1f));
            AddImage(badgeRect, UiShapes.Panel(badgeColor, Outline, 14, 3), false).type = Image.Type.Sliced;
            TMP_Text badgeText = NewText("Label", badgeRect, badge, 18f, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)badgeText.transform);

            card.Button = AddButton(card.Root, onClick);
            return card;
        }

        private static RectTransform NewCardRow(string name, RectTransform viewport)
        {
            RectTransform content = NewRect(name, viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 16f;
            layout.padding = new RectOffset(14, 14, 12, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        private Sprite PathTabIcon()
        {
            PathStyleCatalog styles = Styles;
            return styles != null ? styles.GetSprite(styles.DefaultStyleId, 1, 15) : null;
        }

        private static Sprite BuildingThumbnail(BuildingConfig config)
        {
            if (config.Prefab == null) return null;
            SpriteRenderer renderer = config.Prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }

        private void BuildIconButton(RectTransform root, Sprite background, Sprite icon, Color iconTint, string label, float iconSize)
        {
            Image body = AddImage(root, background, true);
            RectTransform iconRect = NewRect("Icon", root);
            Place(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(iconSize, iconSize), new Vector2(0.5f, 1f));
            Image iconImage = AddImage(iconRect, icon, false);
            iconImage.preserveAspect = true;
            iconImage.color = iconTint;
            TMP_Text text = NewText("Label", root, label, 24f, Color.white, TextAlignmentOptions.Midline);
            Place((RectTransform)text.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 30f), new Vector2(0.5f, 0f));
            Outlined(text);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<UiPress>();
        }

        // ---- small helpers ------------------------------------------------------------------

        private static Button AddButton(RectTransform rect, UnityEngine.Events.UnityAction onClick)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            rect.gameObject.AddComponent<UiPress>();
            return button;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, bool raycast)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        private static void Outlined(TMP_Text text)
        {
            text.outlineWidth = 0.22f;
            text.outlineColor = Outline;
        }

        private static TMP_Text NewText(string name, RectTransform parent, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
