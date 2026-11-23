using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Config;
using ResortIsland.Waves;

namespace ResortIsland.UI
{
    /// The game HUD after predloha.html (designed at 844 x 390, built here at 2x = the 1688 x 780 reference):
    /// - bottom bar: the wooden plank with Build · Workers · Start Wave · Forecast · Settings,
    /// - next wave card (top right): guests and one tile per need, "NEW" on needs this wave adds,
    /// - stats (top left): coins (+), energy, resort value.
    public partial class BuildModeHud
    {
        /// The wave card shows the forecast through this (WaveForecastPresenter drives it).
        private sealed class ForecastView : IWaveForecastView
        {
            private readonly BuildModeHud hud;
            public ForecastView(BuildModeHud hud) => this.hud = hud;
            public void Show(WavePlan plan) => hud.ShowForecast(plan);
            public void Hide() => hud.ShowForecast(null);
        }

        private const float BarWidth = 1040f;
        private const float MainBarHeight = 216f;
        private const float PlankHeight = 152f;
        private const float BarItemWidth = 144f;
        private const float StartItemWidth = 208f;
        private const float BarItemSquare = 116f;
        private const float StartCircle = 168f;
        private const float ToastSeconds = 2.5f;

        private static readonly Color Grey = new Color32(0xA9, 0xB1, 0xBA, 0xFF);
        private static readonly Color PillFill = new Color32(0x26, 0x30, 0x3E, 0xFF);
        private static readonly Color CardCream = new Color32(0xFF, 0xF8, 0xEA, 0xFF);
        private static readonly Color CaptionBlue = new Color32(0x4D, 0x64, 0x70, 0xFF);
        private static readonly Color NewFill = new Color32(0xE7, 0xF1, 0xF8, 0xFF);
        private static readonly Color NewOutline = new Color32(0x1F, 0x6F, 0xA6, 0xFF);
        private static readonly Color BadgeRed = new Color32(0xE2, 0x57, 0x4C, 0xFF);
        private static readonly Color PlusGreen = new Color32(0x3F, 0xB9, 0x4A, 0xFF);

        // bottom bar
        private RectTransform mainBar;
        private Button startWave;
        private Image startWaveImage;
        private Image startWaveShip;
        private bool startWaveReady = true;
        private float barAmount = 1f;
        private RectTransform buildBadge;
        private TMP_Text buildBadgeText;

        // stats
        private RectTransform statsRow;
        private TMP_Text coinsText;
        private TMP_Text energyText;
        private TMP_Text valueText;

        // next wave card
        private RectTransform waveCard;
        private TMP_Text waveCaption;
        private TMP_Text waveTitle;
        private RectTransform waveGrid;
        private bool waveCardHidden;

        // small message above the bar
        private RectTransform toast;
        private TMP_Text toastText;
        private float toastUntil;
        private RectTransform layoutActions;
        private bool layoutOperationRunning;
        private RectTransform layoutDialogBackdrop;
        private RectTransform layoutDialog;
        private TMP_Text layoutDialogText;
        private RectTransform layoutDialogConfirm;
        private RectTransform layoutDialogCancel;
        private TMP_Text layoutDialogCancelText;
        private Action layoutDialogAction;

        private void BuildGameHud(RectTransform parent)
        {
            BuildStats(parent);
            BuildWaveCard(parent);
            BuildBottomBar(parent);
            BuildToast(parent);
            BuildLayoutActions(parent);
            BuildLayoutDialog(parent);
        }

        /// Start Wave and the forecast move from the old scene UI here; the Workers button replaces the old
        /// Needs button; the stats replace the plank toolbar of HomeScreenController.
        private void TakeOverSceneHud()
        {
            if (waves != null) waves.UseViews(startWave.GetComponent<StartWaveButton>(), new ForecastView(this));
            if (home != null) home.SetToolbarVisible(false);

            if (needsPanel == null) return;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentTarget(i) == needsPanel)
                        button.gameObject.SetActive(false);
            // the panel slides over the bar
            Canvas needsCanvas = needsPanel.GetComponentInParent<Canvas>(true);
            if (needsCanvas != null) needsCanvas.rootCanvas.sortingOrder = 40;
        }

        /// While the build menu is open the bar slides away (Animate) and the wave card hides.
        private void SetGameHudForBuildMode(bool building)
        {
            waveCard.gameObject.SetActive(!building && !waveCardHidden);
            toast.gameObject.SetActive(false);
            if (building && layoutActions != null)
                layoutActions.gameObject.SetActive(false);
            if (building)
                HideLayoutDialog();
        }

        private void RefreshGameHud()
        {
            if (economy != null)
            {
                coinsText.text = economy.Coins.ToString("N0");
                energyText.text = economy.Energy.ToString("N0");
                valueText.text = economy.ResortValue.ToString("N0");
            }

            bool ready = startWave.interactable;
            if (ready != startWaveReady)
            {
                startWaveReady = ready;
                startWaveImage.color = ready ? Color.white : new Color(0.62f, 0.66f, 0.7f, 1f);
                startWaveShip.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }

            int fresh = NewBuildingCount();
            buildBadge.gameObject.SetActive(fresh > 0);
            buildBadgeText.text = fresh.ToString();

            if (toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false);
        }

        // building types the player can build now and has none of yet
        private int NewBuildingCount()
        {
            int coins = economy != null ? economy.Coins : int.MaxValue;
            int count = 0;
            foreach (BuildingConfig config in resort.Catalog)
                if (config != null && resort.CanBuild(config) && resort.CountOf(config) == 0 && config.Cost <= coins)
                    count++;
            return count;
        }

        private void ToggleNeeds()
        {
            if (needsPanel != null) needsPanel.Toggle();
            else ShowToast("There is no workers panel in this scene");
        }

        private void ToggleLayoutActions()
        {
            if (layoutActions == null) return;
            bool open = !layoutActions.gameObject.activeSelf;
            if (open)
            {
                layoutActions.gameObject.SetActive(true);
                StartCoroutine(AnimateLayoutPanel(open));
            }
            else
            {
                StartCoroutine(CloseLayoutPanel());
            }
        }

        private void BuildLayoutActions(RectTransform parent)
        {
            layoutActions = NewRect("BuildingLayoutActions", parent);
            Place(layoutActions, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, MainBarHeight + 30f), new Vector2(420f, 112f),
                new Vector2(0.5f, 0f));
            AddImage(layoutActions, UiShapes.Panel(CardCream, Outline, 18, 3), true).type = Image.Type.Sliced;

            RectTransform save = NewRect("SaveLayout", layoutActions);
            Place(save, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(20f, 0f), new Vector2(180f, 72f), new Vector2(0f, 0.5f));
            AddImage(save, UiShapes.Panel(PlusGreen, Outline, 14, 2), true).type = Image.Type.Sliced;
            AddButton(save, () =>
            {
                if (layoutOperationRunning) return;
                layoutOperationRunning = true;
                try
                {
                    if (resort == null)
                    {
                        ShowLayoutDialog("Save failed: Resort manager is not available.", null);
                        return;
                    }
                    if (resort.SaveBuildingLayout(out string error))
                        ShowLayoutDialog("Layout saved successfully.", null);
                    else
                        ShowLayoutDialog("Save failed: " + error, null);
                }
                finally
                {
                    layoutOperationRunning = false;
                }
            });
            NewText("SaveLabel", save, "SAVE LAYOUT", 24f, Color.white, TextAlignmentOptions.Center);

            RectTransform load = NewRect("LoadLayout", layoutActions);
            Place(load, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(180f, 72f), new Vector2(1f, 0.5f));
            AddImage(load, UiShapes.Panel(NewOutline, Outline, 14, 2), true).type = Image.Type.Sliced;
            AddButton(load, () =>
            {
                if (layoutOperationRunning) return;
                layoutOperationRunning = true;
                try
                {
                    if (resort == null)
                    {
                        ShowLayoutDialog("Load failed: Resort manager is not available.", null);
                        return;
                    }
                    if (!resort.HasBuildingLayoutSave())
                    {
                        ShowLayoutDialog("No saved layout found.", null);
                    }
                    else
                    {
                        ShowLayoutDialog("Load the saved building layout?", () =>
                        {
                            if (resort.LoadBuildingLayout(out string error))
                            {
                                layoutActions.gameObject.SetActive(false);
                                ShowLayoutDialog("Layout loaded successfully.", null);
                            }
                            else
                            {
                                ShowLayoutDialog("Load failed: " + error, null);
                            }
                        });
                    }
                }
                finally
                {
                    layoutOperationRunning = false;
                }
            });
            NewText("LoadLabel", load, "LOAD LAYOUT", 24f, Color.white, TextAlignmentOptions.Center);
            layoutActions.gameObject.SetActive(false);
        }

        private void BuildLayoutDialog(RectTransform parent)
        {
            layoutDialogBackdrop = NewRect("LayoutDialogBackdrop", parent);
            Place(layoutDialogBackdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdropImage = AddImage(layoutDialogBackdrop,
                UiShapes.Panel(new Color(0f, 0f, 0f, 0.62f), new Color(0f, 0f, 0f, 0f), 0, 0),
                true);
            backdropImage.raycastTarget = true;
            layoutDialogBackdrop.gameObject.SetActive(false);

            layoutDialog = NewRect("LayoutDialog", layoutDialogBackdrop);
            Place(layoutDialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 260f), new Vector2(0.5f, 0.5f));
            AddImage(layoutDialog, UiShapes.Panel(CardCream, Outline, 22, 4), false).type = Image.Type.Sliced;

            layoutDialogText = NewText("Message", layoutDialog, "", 30f, TextDark,
                TextAlignmentOptions.Center);
            Place((RectTransform)layoutDialogText.transform,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 38f), new Vector2(-60f, 140f), new Vector2(0.5f, 0.5f));

            layoutDialogConfirm = NewRect("Confirm", layoutDialog);
            Place(layoutDialogConfirm, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(170f, 34f), new Vector2(190f, 72f), new Vector2(0.5f, 0f));
            AddImage(layoutDialogConfirm, UiShapes.Panel(PlusGreen, Outline, 14, 2), true).type = Image.Type.Sliced;
            AddButton(layoutDialogConfirm, () =>
            {
                Action action = layoutDialogAction;
                HideLayoutDialog();
                action?.Invoke();
            });
            TMP_Text confirmText = NewText("ConfirmLabel", layoutDialogConfirm, "CONFIRM",
                24f, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)confirmText.transform, 8f, 8f, 8f, 8f);

            layoutDialogCancel = NewRect("Cancel", layoutDialog);
            Place(layoutDialogCancel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-170f, 34f), new Vector2(190f, 72f), new Vector2(0.5f, 0f));
            AddImage(layoutDialogCancel, UiShapes.Panel(Grey, Outline, 14, 2), true).type = Image.Type.Sliced;
            AddButton(layoutDialogCancel, HideLayoutDialog);
            layoutDialogCancelText = NewText("CancelLabel", layoutDialogCancel, "CANCEL",
                24f, TextDark, TextAlignmentOptions.Center);
            Stretch((RectTransform)layoutDialogCancelText.transform, 8f, 8f, 8f, 8f);
        }

        private void ShowLayoutDialog(string message, Action confirmAction)
        {
            if (layoutDialog == null || layoutDialogBackdrop == null) return;
            layoutDialogAction = confirmAction;
            layoutDialogText.text = message;
            layoutDialogConfirm.gameObject.SetActive(confirmAction != null);
            layoutDialogCancelText.text = confirmAction != null ? "CANCEL" : "OK";
            layoutDialogBackdrop.gameObject.SetActive(true);
            layoutDialog.gameObject.SetActive(true);
            layoutDialog.localScale = Vector3.one * 0.86f;
            StartCoroutine(AnimateLayoutDialog(true));
        }

        private void HideLayoutDialog()
        {
            layoutDialogAction = null;
            if (layoutDialog == null || layoutDialogBackdrop == null) return;
            layoutDialog.localScale = Vector3.one;
            layoutDialog.gameObject.SetActive(false);
            layoutDialogBackdrop.gameObject.SetActive(false);
        }

        private IEnumerator CloseLayoutPanel()
        {
            yield return AnimateLayoutPanel(false);
            layoutActions.gameObject.SetActive(false);
            layoutActions.localScale = Vector3.one;
        }

        private IEnumerator AnimateLayoutPanel(bool opening)
        {
            float duration = 0.18f;
            float elapsed = 0f;
            layoutActions.localScale = opening ? Vector3.one * 0.86f : Vector3.one;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float scale = opening
                    ? Mathf.Lerp(0.86f, 1f, eased)
                    : Mathf.Lerp(1f, 0.86f, eased);
                layoutActions.localScale = Vector3.one * scale;
                yield return null;
            }
            layoutActions.localScale = Vector3.one;
        }

        private IEnumerator AnimateLayoutDialog(bool opening)
        {
            float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float scale = opening
                    ? Mathf.Lerp(0.86f, 1f, eased)
                    : Mathf.Lerp(1f, 0.86f, eased);
                layoutDialog.localScale = Vector3.one * scale;
                yield return null;
            }
            layoutDialog.localScale = Vector3.one;
        }

        private void ToggleWaveCard()
        {
            waveCardHidden = !waveCardHidden;
            waveCard.gameObject.SetActive(!waveCardHidden && !open);
        }

        private void ShowToast(string text)
        {
            toastText.text = text;
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + ToastSeconds;
        }

        // ---- next wave card -----------------------------------------------------------------------

        private void ShowForecast(WavePlan plan)
        {
            if (!built) return;
            for (int i = waveGrid.childCount - 1; i >= 0; i--)
                Destroy(waveGrid.GetChild(i).gameObject);

            if (plan == null)
            {
                waveCaption.text = "WAVE " + ReachedWave;
                waveTitle.text = StatusText();
                waveGrid.gameObject.SetActive(false);
                return;
            }

            waveCaption.text = "NEXT WAVE · " + plan.Wave;
            waveTitle.text = plan.Guests + " guests";
            waveGrid.gameObject.SetActive(true);

            // needs the previous wave did not have get a NEW badge
            var previous = new HashSet<string>();
            WavePlan before = plan.Wave > 1 && waves != null && waves.Planner != null ? waves.Planner.Plan(plan.Wave - 1) : null;
            if (before != null) foreach (NeedDemand need in before.Needs) previous.Add(need.NeedId);

            foreach (NeedDemand need in plan.Needs)
                NewNeedTile(need, before != null && !previous.Contains(need.NeedId));
        }

        private string StatusText()
        {
            GameState state = waves != null && waves.State != null ? waves.State.Current : GameState.AwaitingWave;
            switch (state)
            {
                case GameState.WaveIncoming: return "Guests arriving";
                case GameState.WaveActive: return "In progress";
                case GameState.WaveResolving: return "Wrapping up";
                case GameState.WaveComplete: return "Complete!";
                case GameState.Upgrade: return "Upgrade time";
                default: return "Get ready";
            }
        }

        private void BuildWaveCard(RectTransform parent)
        {
            waveCard = NewRect("Next Wave", parent);
            Place(waveCard, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-104f, -24f), new Vector2(520f, 296f));
            AddImage(waveCard, UiShapes.Panel(CardCream, Outline, 36, 6), true).type = Image.Type.Sliced;
            var column = waveCard.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 20, 26);
            column.spacing = 16f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            var fitter = waveCard.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform header = NewRect("Header", waveCard);
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.spacing = 16f;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = false;
            RectTransform ship = NewRect("Ship", header);
            var shipElement = ship.gameObject.AddComponent<LayoutElement>();
            shipElement.preferredWidth = 92f;
            shipElement.preferredHeight = 92f;
            AddImage(ship, skin != null ? skin.ShipIcon : null, false).preserveAspect = true;

            RectTransform titles = NewRect("Titles", header);
            var titleColumn = titles.gameObject.AddComponent<VerticalLayoutGroup>();
            titleColumn.childControlWidth = true;
            titleColumn.childControlHeight = true;
            titleColumn.childForceExpandWidth = false;
            titleColumn.childForceExpandHeight = false;
            titleColumn.spacing = 0f;
            waveCaption = NewText("Caption", titles, "NEXT WAVE", 22f, CaptionBlue, TextAlignmentOptions.MidlineLeft);
            waveCaption.characterSpacing = 6f;
            waveTitle = NewText("Title", titles, "Get ready", 36f, TextDark, TextAlignmentOptions.MidlineLeft);

            waveGrid = NewRect("Needs", waveCard);
            var grid = waveGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230f, 64f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
        }

        private void NewNeedTile(NeedDemand need, bool isNew)
        {
            RectTransform tile = NewRect(need.NeedId, waveGrid);
            AddImage(tile, isNew ? UiShapes.Panel(NewFill, NewOutline, 20, 4) : UiShapes.Panel(Color.white, Color.white, 20, 0), false)
                .type = Image.Type.Sliced;

            RectTransform icon = NewRect("Icon", tile);
            Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(60f, 60f), new Vector2(0f, 0.5f));
            AddImage(icon, skin != null ? skin.NeedIconFor(need.NeedId) : null, false).preserveAspect = true;
            TMP_Text units = NewText("Units", tile, need.Units.ToString(), 34f, TextDark, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)units.transform, 88f, 0f, 8f, 0f);

            if (!isNew) return;
            RectTransform badge = NewRect("New", tile);
            Place(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(12f, 16f), new Vector2(64f, 32f), new Vector2(1f, 1f));
            AddImage(badge, UiShapes.Panel(BadgeRed, Outline, 16, 3), false).type = Image.Type.Sliced;
            TMP_Text label = NewText("Label", badge, "NEW", 19f, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)label.transform);
        }

        // ---- stats (top left) ---------------------------------------------------------------------

        private void BuildStats(RectTransform parent)
        {
            statsRow = NewRect("Stats", parent);
            Place(statsRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -28f), new Vector2(900f, 76f));
            var row = statsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 44f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            coinsText = NewStat("Coins", coinIcon, 100f, true);
            energyText = NewStat("Energy", skin != null ? skin.EnergyIcon : null, 104f, false);
            valueText = NewStat("Resort Value", skin != null ? skin.CrownIcon : null, 104f, false);
        }

        private TMP_Text NewStat(string name, Sprite icon, float iconSize, bool plus)
        {
            RectTransform pill = NewRect(name, statsRow);
            AddImage(pill, UiShapes.Panel(PillFill, PillFill, 32, 0), false).type = Image.Type.Sliced;
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(80, plus ? 64 : 30, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var element = pill.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 64f;
            element.preferredHeight = 64f;

            TMP_Text value = NewText("Value", pill, "0", 36f, Color.white, TextAlignmentOptions.MidlineLeft);

            // the icon sticks out over the left end of the pill
            RectTransform iconRect = NewRect("Icon", pill);
            iconRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-32f, 0f), new Vector2(iconSize, iconSize), new Vector2(0f, 0.5f));
            AddImage(iconRect, icon, false).preserveAspect = true;

            if (plus)
            {
                RectTransform button = NewRect("Add", pill);
                button.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Place(button, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, 6f), new Vector2(64f, 64f), new Vector2(1f, 0.5f));
                AddImage(button, UiShapes.Button(PlusGreen, 20, 0), true).type = Image.Type.Sliced;
                RectTransform glyph = NewRect("Plus", button);
                Place(glyph, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(30f, 30f), new Vector2(0.5f, 0.5f));
                AddImage(glyph, UiShapes.Glyph(UiGlyph.Plus), false);
                AddButton(button, () => ShowToast("The coin shop is coming soon"));
            }
            return value;
        }

        // ---- bottom bar -----------------------------------------------------------------------------

        private void BuildBottomBar(RectTransform parent)
        {
            mainBar = NewRect("Bottom Bar", parent);
            Place(mainBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, TrayBottom), new Vector2(BarWidth, MainBarHeight), new Vector2(0.5f, 0f));

            // the plank: lower part of the bar, the buttons stick out above it
            RectTransform plank = NewRect("Plank", mainBar);
            plank.anchorMin = new Vector2(0f, 0f);
            plank.anchorMax = new Vector2(1f, 0f);
            plank.pivot = new Vector2(0.5f, 0f);
            plank.sizeDelta = new Vector2(0f, PlankHeight);
            plank.anchoredPosition = Vector2.zero;
            AddImage(plank, skin != null ? skin.ToolbarPlank : null, true);

            // space-between of 2 + 1 + 2 items over 952 px (the plank minus 44 px on each side)
            float gap = (BarWidth - 88f - 4f * BarItemWidth - StartItemWidth) / 4f;
            float x = 44f;
            RectTransform build = BarItem("Build", buttonOrange, hammerIcon, x, () => OpenMenu(Tab.Buildings));
            x += BarItemWidth + gap;
            BarItem("Workers", buttonBlue, skin != null ? skin.WorkersIcon : null, x, ToggleNeeds);
            x += BarItemWidth + gap;
            BuildStartItem(x);
            x += StartItemWidth + gap;
            BarItem("Forecast", skin != null ? skin.ButtonTeal : null, skin != null ? skin.SpyglassIcon : null, x, ToggleWaveCard);
            x += BarItemWidth + gap;
            BarItem("Layout", skin != null ? skin.ButtonGrey : null, skin != null ? skin.GearIcon : null, x,
                ToggleLayoutActions);

            // red count of buildings that are new to build
            buildBadge = NewRect("Badge", build);
            Place(buildBadge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, 12f), new Vector2(44f, 44f), new Vector2(0f, 1f));
            AddImage(buildBadge, UiShapes.Panel(BadgeRed, Outline, 22, 3), false).type = Image.Type.Sliced;
            buildBadgeText = NewText("Count", buildBadge, "1", 26f, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)buildBadgeText.transform, 0f, 2f, 0f, 0f);
        }

        // square ResortEmpire button with an icon, the label under it
        private RectTransform BarItem(string label, Sprite background, Sprite icon, float x, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform item = NewRect(label, mainBar);
            Place(item, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 12f), new Vector2(BarItemWidth, 150f));

            RectTransform square = NewRect("Button", item);
            Place(square, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(BarItemSquare, BarItemSquare), new Vector2(0.5f, 1f));
            Image body = AddImage(square, background != null ? background : UiShapes.Button(Grey, 22, 4), true);
            RectTransform iconRect = NewRect("Icon", square);
            Place(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(88f, 88f), new Vector2(0.5f, 1f));
            AddImage(iconRect, icon, false).preserveAspect = true;

            TMP_Text text = NewText("Label", item, label, 28f, Color.white, TextAlignmentOptions.Bottom);
            Place((RectTransform)text.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(20f, 36f), new Vector2(0.5f, 0f));
            Outlined(text);

            Button button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            item.gameObject.AddComponent<UiPress>();
            return item;
        }

        // the big round Start Wave button in the middle (StartWaveButton is the view WaveController drives)
        private void BuildStartItem(float x)
        {
            RectTransform item = NewRect("Start Wave", mainBar);
            Place(item, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 12f), new Vector2(StartItemWidth, 206f));

            RectTransform circle = NewRect("Button", item);
            Place(circle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(StartCircle, StartCircle), new Vector2(0.5f, 1f));
            Sprite round = skin != null ? skin.ButtonRoundGreen : null;
            startWaveImage = AddImage(circle, round != null ? round : UiShapes.Button(Green, 80, 6), true);
            startWaveImage.preserveAspect = true;
            RectTransform ship = NewRect("Ship", circle);
            Place(ship, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(108f, 108f), new Vector2(0.5f, 1f));
            startWaveShip = AddImage(ship, skin != null ? skin.ShipIcon : null, false);
            startWaveShip.preserveAspect = true;

            circle.gameObject.AddComponent<StartWaveButton>();
            startWave = circle.GetComponent<Button>();
            startWave.targetGraphic = startWaveImage;
            startWave.transition = Selectable.Transition.None;
            circle.gameObject.AddComponent<UiPress>();

            TMP_Text text = NewText("Label", item, "Start Wave", 30f, Color.white, TextAlignmentOptions.Bottom);
            Place((RectTransform)text.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(40f, 38f), new Vector2(0.5f, 0f));
            Outlined(text);
        }

        private void BuildToast(RectTransform parent)
        {
            toast = NewRect("Toast", parent);
            Place(toast, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, TrayBottom + MainBarHeight + 14f), new Vector2(720f, 64f), new Vector2(0.5f, 0f));
            AddImage(toast, UiShapes.Panel(HintFill, HintFill, 22, 0), false).type = Image.Type.Sliced;
            toastText = NewText("Text", toast, "", 26f, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)toastText.transform, 20f, 4f, 20f, 4f);
            toast.gameObject.SetActive(false);
        }
    }
}
