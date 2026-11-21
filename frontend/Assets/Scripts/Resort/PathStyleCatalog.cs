using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.Resort
{
    /// One look of a path style. The sheet is a ResortEmpire path sheet: 8 x 2 cells, cell index = the
    /// neighbour mask (see PathFinder.Directions). Sprites are cut out of it at runtime.
    [Serializable]
    public class PathStyleLevel
    {
        public const float PixelsPerUnit = 128f;

        [SerializeField] private string name;
        [SerializeField] private Texture2D sheet;
        [Tooltip("Centre of the tile surface in the first cell, in pixels from the cell's top-left corner.")]
        [SerializeField] private Vector2 faceCenter = new Vector2(74f, 38f);
        [SerializeField, Min(0)] private int costPerTile;
        [Header("Shown in the level panel (not applied to gameplay yet)")]
        [SerializeField, Range(0, 100)] private int guestSpeedPercent;
        [SerializeField, Min(0)] private int valuePerTile;

        [NonSerialized] private Sprite[] sprites;

        public string Name => name;
        public Texture2D Sheet => sheet;
        public int CostPerTile => costPerTile;
        public int GuestSpeedPercent => guestSpeedPercent;
        public int ValuePerTile => valuePerTile;

        public string Description
        {
            get
            {
                var parts = new List<string>();
                if (guestSpeedPercent > 0) parts.Add("guests " + guestSpeedPercent + "% faster");
                if (valuePerTile > 0) parts.Add("+" + valuePerTile + " Value/tile");
                return parts.Count > 0 ? string.Join(" · ", parts) : "basic path";
            }
        }

        /// Sprite for a neighbour mask 0…15 (null without a sheet).
        public Sprite GetSprite(int mask)
        {
            if (sheet == null || mask < 0 || mask > 15) return null;
            if (sprites == null) sprites = new Sprite[16];

            Sprite sprite = sprites[mask];
            if (sprite != null) return sprite;

            float cellWidth = sheet.width / 8f;
            float cellHeight = sheet.height / 2f;
            int column = mask % 8;
            int row = mask / 8; // row 0 is the top row of the image
            var rect = new Rect(column * cellWidth, (1 - row) * cellHeight, cellWidth, cellHeight);
            var pivot = new Vector2(faceCenter.x / cellWidth, 1f - faceCenter.y / cellHeight);

            sprite = Sprite.Create(sheet, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = sheet.name + "_" + mask.ToString("00");
            sprites[mask] = sprite;
            return sprite;
        }
    }

    [Serializable]
    public class PathStyle
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, Range(1, 4)] private int tier = 1;
        [Tooltip("Can be built from this wave on (1 = from the start).")]
        [SerializeField, Min(1)] private int unlockWave = 1;
        [SerializeField] private PathStyleLevel[] levels = new PathStyleLevel[0];

        public string Id => id;
        public string DisplayName => displayName;
        public int Tier => tier;
        public int UnlockWave => unlockWave;
        public int LevelCount => levels != null ? levels.Length : 0;

        /// level is 1-based and clamped to the levels the style has.
        public PathStyleLevel Level(int level)
            => LevelCount == 0 ? null : levels[Mathf.Clamp(level, 1, LevelCount) - 1];

        public int ClampLevel(int level) => Mathf.Clamp(level, 1, Mathf.Max(1, LevelCount));
        public int CostPerTile(int level) => Level(level)?.CostPerTile ?? 0;
        public bool IsUnlocked(int reachedWave) => reachedWave >= unlockWave;
    }

    [CreateAssetMenu(fileName = "PathStyles", menuName = "ResortIsland/Config/PathStyleCatalog")]
    public class PathStyleCatalog : ScriptableObject
    {
        [SerializeField] private PathStyle[] styles = new PathStyle[0];
        [Tooltip("Style of generated paths and of paths from saves made before styles existed.")]
        [SerializeField] private string defaultStyleId;

        public IReadOnlyList<PathStyle> Styles => styles;
        public string DefaultStyleId => !string.IsNullOrEmpty(defaultStyleId) ? defaultStyleId : styles.Length > 0 ? styles[0].Id : null;

        public PathStyle Find(string id)
        {
            foreach (PathStyle style in styles)
                if (style != null && style.Id == id) return style;
            return null;
        }

        /// The style, or the default one when the id is unknown.
        public PathStyle FindOrDefault(string id) => Find(id) ?? Find(DefaultStyleId) ?? (styles.Length > 0 ? styles[0] : null);

        public Sprite GetSprite(string styleId, int level, int mask)
            => FindOrDefault(styleId)?.Level(level)?.GetSprite(mask);

        public int CostPerTile(string styleId, int level) => FindOrDefault(styleId)?.CostPerTile(level) ?? 0;
    }
}
