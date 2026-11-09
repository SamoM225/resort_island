using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ResortIsland.Config;

namespace ResortIsland.Resort
{
    public sealed class BuildingBuildMenu : MonoBehaviour
    {
        private ResortManager resort;
        private BuildingPlacer placer;
        private Text status;
        private readonly List<Entry> entries = new List<Entry>();

        private struct Entry
        {
            public BuildingConfig Config;
            public Button Button;
        }

        public static BuildingBuildMenu Create(ResortManager manager, BuildingPlacer buildingPlacer)
        {
            if (manager == null || buildingPlacer == null || manager.Catalog == null) return null;

            var go = new GameObject("Building Build Menu");
            var menu = go.AddComponent<BuildingBuildMenu>();
            menu.resort = manager;
            menu.placer = buildingPlacer;
            menu.Build();
            return menu;
        }

        private void OnDestroy() => Unlocks.OnChanged -= Refresh;

        private void Build()
        {
            Unlocks.OnChanged += Refresh;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var panel = NewRect("Buildings", transform);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 0f);
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(20f, 20f);
            panel.sizeDelta = new Vector2(420f, 74f);
            panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.1f, 0.9f);

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 5f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (var config in resort.Catalog)
            {
                if (config == null) continue;
                var button = NewButton(config.DisplayName, panel);
                var captured = config;
                button.onClick.AddListener(() => Select(captured));
                entries.Add(new Entry { Config = captured, Button = button });
            }

            var cancel = NewButton("Cancel", panel);
            cancel.onClick.AddListener(Cancel);

            status = NewText("Выберите здание", transform);
            var statusRect = (RectTransform)status.transform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(0f, 0f);
            statusRect.pivot = Vector2.zero;
            statusRect.anchoredPosition = new Vector2(20f, 100f);
            statusRect.sizeDelta = new Vector2(500f, 28f);
            Refresh();
        }

        private void Select(BuildingConfig config)
        {
            if (placer.SelectBuilding(config))
                status.text = "Кликните по карте: " + config.DisplayName;
            else
                status.text = "Здание недоступно";
        }

        private void Cancel()
        {
            placer.CancelPlacement();
            status.text = "Выберите здание";
        }

        private void Refresh()
        {
            if (resort == null) return;
            foreach (var entry in entries)
                entry.Button.interactable = resort.CanBuild(entry.Config);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Button NewButton(string label, Transform parent)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.45f);

            var text = NewText(label, go.transform);
            text.alignment = TextAnchor.MiddleCenter;
            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 2f);
            rect.offsetMax = new Vector2(-3f, -2f);
            return go.GetComponent<Button>();
        }

        private static Text NewText(string value, Transform parent)
        {
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(parent, false);
            var text = textGo.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            return text;
        }
    }
}
