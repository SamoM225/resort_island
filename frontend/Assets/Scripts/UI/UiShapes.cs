using System.Collections.Generic;
using UnityEngine;

namespace ResortIsland.UI
{
    public enum UiGlyph
    {
        Pencil,
        ArrowUp,
        Roller,
        Eraser,
        Close,
        Rotate,
        Plus
    }

    /// Procedural UI sprites in the style of the ResortEmpire UI art (dark outline, flat fill,
    /// darker bottom band, light highlight). Generated once and cached.
    public static class UiShapes
    {
        public static readonly Color Outline = new Color32(0x26, 0x30, 0x3F, 0xFF);

        private const float PixelsPerUnit = 100f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// Flat rounded rectangle, 9-sliced. outlineWidth 0 = no outline.
        public static Sprite Panel(Color fill, Color outline, int radius = 24, int outlineWidth = 5)
            => RoundedRect(fill, outline, radius, outlineWidth, false);

        /// Rounded button like Button_*.png: darker bottom band and a highlight stripe on top.
        public static Sprite Button(Color fill, int radius = 24, int outlineWidth = 5)
            => RoundedRect(fill, Outline, radius, outlineWidth, true);

        private static Sprite RoundedRect(Color fill, Color outline, int radius, int outlineWidth, bool bevel)
        {
            string key = "rect" + ColorUtility.ToHtmlStringRGBA(fill) + ColorUtility.ToHtmlStringRGBA(outline) + radius + "_" + outlineWidth + bevel;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int border = radius + outlineWidth + 2;
            int size = border * 2 + 4;
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            float shadeTop = outlineWidth + radius * 0.75f;       // height of the darker bottom band
            float highlightY = size - outlineWidth - radius * 0.45f; // centre line of the top stripe
            Color shade = Color.Lerp(fill, Color.black, 0.22f);
            Color light = Color.Lerp(fill, Color.white, 0.4f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = RoundedRectDistance(px - half, py - half, half - 1f, half - 1f, radius + outlineWidth);
                float coverage = Mathf.Clamp01(0.5f - d);
                if (coverage <= 0f) { pixels[y * size + x] = Color.clear; continue; }

                Color inner = fill;
                if (bevel)
                {
                    if (py < shadeTop) inner = shade;
                    float stripe = CapsuleDistance(px, py, border, size - border, highlightY, radius * 0.16f);
                    inner = Color.Lerp(inner, light, Mathf.Clamp01(0.5f - stripe) * 0.85f);
                }

                // outline band near the edge, fill inside
                float innerBlend = outlineWidth > 0 ? Mathf.Clamp01(-d - outlineWidth + 0.5f) : 1f;
                Color c = Color.Lerp(outline, inner, innerBlend);
                c.a *= coverage;
                pixels[y * size + x] = c;
            }

            var sprite = CreateSprite("UiRect", size, size, pixels, new Vector4(border, border, border, border), FilterMode.Bilinear);
            Cache[key] = sprite;
            return sprite;
        }

        /// White glyph on transparent background (tint it with Image.color).
        public static Sprite Glyph(UiGlyph glyph)
        {
            string key = "glyph" + glyph;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int size = 96;
            const int samples = 4;
            List<Vector2[]> shapes = GlyphShapes(glyph);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    var p = new Vector2((x + (sx + 0.5f) / samples) / size, (y + (sy + 0.5f) / samples) / size);
                    foreach (Vector2[] shape in shapes)
                        if (InsideConvex(shape, p)) { hits++; break; }
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, hits / (float)(samples * samples));
            }

            var sprite = CreateSprite("UiGlyph" + glyph, size, size, pixels, Vector4.zero, FilterMode.Bilinear);
            Cache[key] = sprite;
            return sprite;
        }

        // ---- geometry ---------------------------------------------------------------------------

        private static List<Vector2[]> GlyphShapes(UiGlyph glyph)
        {
            var shapes = new List<Vector2[]>();
            switch (glyph)
            {
                case UiGlyph.Pencil:
                    // drawn upright (tip down), then tilted so the tip points down-left
                    shapes.Add(Transform(Rect(-0.11f, -0.2f, 0.11f, 0.3f), -45f, 0.5f, 0.5f));
                    shapes.Add(Transform(new[] { new Vector2(-0.11f, -0.23f), new Vector2(0.11f, -0.23f), new Vector2(0f, -0.43f) }, -45f, 0.5f, 0.5f));
                    shapes.Add(Transform(Rect(-0.11f, 0.34f, 0.11f, 0.44f), -45f, 0.5f, 0.5f));
                    break;
                case UiGlyph.ArrowUp:
                    shapes.Add(new[] { new Vector2(0.5f, 0.9f), new Vector2(0.13f, 0.52f), new Vector2(0.87f, 0.52f) });
                    shapes.Add(Rect(0.37f, 0.1f, 0.63f, 0.55f));
                    break;
                case UiGlyph.Roller:
                    shapes.Add(Rect(0.1f, 0.62f, 0.74f, 0.9f));
                    shapes.Add(Rect(0.74f, 0.71f, 0.9f, 0.79f));
                    shapes.Add(Rect(0.82f, 0.5f, 0.9f, 0.79f));
                    shapes.Add(Rect(0.44f, 0.46f, 0.9f, 0.55f));
                    shapes.Add(Rect(0.42f, 0.08f, 0.56f, 0.5f));
                    break;
                case UiGlyph.Eraser:
                    shapes.Add(Transform(Rect(-0.33f, -0.16f, 0.33f, 0.16f), 35f, 0.5f, 0.58f));
                    shapes.Add(Rect(0.16f, 0.1f, 0.84f, 0.17f));
                    break;
                case UiGlyph.Close:
                    shapes.Add(Transform(Rect(-0.38f, -0.09f, 0.38f, 0.09f), 45f, 0.5f, 0.5f));
                    shapes.Add(Transform(Rect(-0.38f, -0.09f, 0.38f, 0.09f), -45f, 0.5f, 0.5f));
                    break;
                case UiGlyph.Plus:
                    shapes.Add(Rect(0.38f, 0.14f, 0.62f, 0.86f));
                    shapes.Add(Rect(0.14f, 0.38f, 0.86f, 0.62f));
                    break;
                case UiGlyph.Rotate:
                {
                    // ring (thin quads) open at the top right, with an arrow head at its end
                    const float inner = 0.24f, outer = 0.36f, start = 70f, end = 340f, step = 15f;
                    var centre = new Vector2(0.5f, 0.5f);
                    for (float a = start; a < end; a += step)
                    {
                        float b = Mathf.Min(a + step, end);
                        shapes.Add(new[] { centre + Polar(inner, a), centre + Polar(outer, a), centre + Polar(outer, b), centre + Polar(inner, b) });
                    }
                    Vector2 tangent = Polar(1f, end + 90f);
                    shapes.Add(new[]
                    {
                        centre + Polar(inner - 0.09f, end),
                        centre + Polar(outer + 0.09f, end),
                        centre + Polar((inner + outer) * 0.5f, end) + tangent * 0.2f
                    });
                    break;
                }
            }
            return shapes;
        }

        private static Vector2 Polar(float radius, float degrees)
            => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * radius;

        private static Vector2[] Rect(float x0, float y0, float x1, float y1)
            => new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };

        private static Vector2[] Transform(Vector2[] points, float degrees, float dx, float dy)
        {
            float r = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                result[i] = new Vector2(points[i].x * cos - points[i].y * sin + dx, points[i].x * sin + points[i].y * cos + dy);
            return result;
        }

        private static bool InsideConvex(Vector2[] polygon, Vector2 p)
        {
            bool? positive = null;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (Mathf.Approximately(cross, 0f)) continue;
                bool side = cross > 0f;
                if (positive == null) positive = side;
                else if (positive != side) return false;
            }
            return true;
        }

        // signed distance from a rounded rectangle centred at the origin
        private static float RoundedRectDistance(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static float CapsuleDistance(float x, float y, float x0, float x1, float cy, float halfThickness)
        {
            float cx = Mathf.Clamp(x, x0, x1);
            return new Vector2(x - cx, y - cy).magnitude - halfThickness;
        }

        private static Sprite CreateSprite(string name, int width, int height, Color[] pixels, Vector4 border, FilterMode filter)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit,
                0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
