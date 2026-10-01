using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Draws ECHO's placeholder and UI sprites procedurally (anti-aliased shapes saved as PNG),
    /// so the graybox and touch controls need no external art. All generated sprites are white;
    /// color comes from the SpriteRenderer/Image tint (see EchoPalette).
    /// </summary>
    public static class EchoSpriteFactory
    {
        const string ShapesFolder = "Assets/Art/Sprites/Generated";
        const string TouchFolder = "Assets/Art/UI/Touch";
        const int Supersampling = 4;

        public static string Square => $"{ShapesFolder}/Square.png";
        public static string RoundedRect => $"{ShapesFolder}/RoundedRect.png";
        public static string Circle => $"{ShapesFolder}/Circle.png";
        public static string Diamond => $"{ShapesFolder}/Diamond.png";
        public static string DiamondOutline => $"{ShapesFolder}/DiamondOutline.png";
        public static string TouchRing => $"{TouchFolder}/TouchRing.png";
        public static string IconArrow => $"{TouchFolder}/IconArrow.png";
        public static string IconJump => $"{TouchFolder}/IconJump.png";
        public static string IconDash => $"{TouchFolder}/IconDash.png";
        public static string IconPause => $"{TouchFolder}/IconPause.png";

        public static void EnsureAll(bool regenerate = false)
        {
            EchoProjectSetup.EnsureFolderPath(ShapesFolder);
            EchoProjectSetup.EnsureFolderPath(TouchFolder);

            // Coordinates below are normalized to [-1, 1] across the texture.
            Make(Square, 16, 16, (x, y) => true, regenerate);
            Make(RoundedRect, 64, 128, (x, y) => RoundedBox(x, y, 1f, 1f, 0.45f), regenerate, border: 16);
            Make(Circle, 64, 64, (x, y) => x * x + y * y <= 0.97f, regenerate);
            Make(Diamond, 64, 64, (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 0.95f, regenerate);
            Make(DiamondOutline, 64, 64, (x, y) =>
            {
                float d = Mathf.Abs(x) + Mathf.Abs(y);
                return d <= 0.95f && d >= 0.72f;
            }, regenerate);

            Make(TouchRing, 128, 128, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                return r <= 0.97f && r >= 0.88f;
            }, regenerate);
            Make(IconArrow, 128, 128, (x, y) => InPolygon(x, y, ArrowRight), regenerate);
            Make(IconJump, 128, 128, (x, y) => InPolygon(x, y, ChevronUp), regenerate);
            Make(IconDash, 128, 128, (x, y) => InPolygon(x, y, ChevronRight) || InPolygon(x - 0.42f, y, ChevronRight), regenerate);
            Make(IconPause, 128, 128, (x, y) => Mathf.Abs(y) <= 0.5f && (Mathf.Abs(x - 0.25f) <= 0.12f || Mathf.Abs(x + 0.25f) <= 0.12f), regenerate);
        }

        static readonly Vector2[] ArrowRight = { new Vector2(-0.3f, -0.45f), new Vector2(0.45f, 0f), new Vector2(-0.3f, 0.45f) };

        static readonly Vector2[] ChevronUp =
        {
            new Vector2(-0.5f, -0.1f), new Vector2(0f, 0.4f), new Vector2(0.5f, -0.1f),
            new Vector2(0.32f, -0.28f), new Vector2(0f, 0.04f), new Vector2(-0.32f, -0.28f),
        };

        static readonly Vector2[] ChevronRight =
        {
            new Vector2(-0.15f, 0.48f), new Vector2(0.33f, 0f), new Vector2(-0.15f, -0.48f),
            new Vector2(-0.33f, -0.3f), new Vector2(-0.03f, 0f), new Vector2(-0.33f, 0.3f),
        };

        static bool RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - radius <= 0f;
        }

        // Even-odd ray casting.
        static bool InPolygon(float x, float y, Vector2[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        static void Make(string path, int size, float pixelsPerUnit, Func<float, float, bool> inside, bool regenerate, int border = 0)
        {
            if (!regenerate && File.Exists(path))
                return;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float subSamples = Supersampling * Supersampling;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    int covered = 0;
                    for (int sy = 0; sy < Supersampling; sy++)
                    {
                        for (int sx = 0; sx < Supersampling; sx++)
                        {
                            float x = (px + (sx + 0.5f) / Supersampling) / size * 2f - 1f;
                            float y = (py + (sy + 0.5f) / Supersampling) / size * 2f - 1f;
                            if (inside(x, y))
                                covered++;
                        }
                    }
                    pixels[py * size + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * covered / subSamples));
                }
            }

            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = new Vector4(border, border, border, border);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        public static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
