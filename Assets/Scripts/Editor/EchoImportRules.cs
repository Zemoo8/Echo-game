using UnityEditor;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Import settings for ECHO's art and audio, applied automatically by folder so every file
    /// imports the same way: pixel art at 32 px per unit with point filtering, effect textures
    /// smooth, sound effects small and music streamed from compressed memory.
    /// </summary>
    public class EchoImportRules : AssetPostprocessor
    {
        public const float PixelsPerUnit = 32f;

        // The hero's frames are 71x67 with the feet at the bottom, body centred at x = 38.5.
        static readonly Vector2 PlayerPivot = new Vector2(38.5f / 71f, 0f);

        // Bump when a rule changes so Unity reimports the affected files.
        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool pixelArt = path.StartsWith("Assets/ThirdParty/Ansimuz/") || path.StartsWith("Assets/Art/Pixel/");
            bool effects = path.StartsWith("Assets/ThirdParty/Kenney/Particles/");
            if (!pixelArt && !effects)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelArt ? PixelsPerUnit : 128f;
            importer.filterMode = pixelArt ? FilterMode.Point : FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = Vector4.zero;

            if (path.Contains("/WarpedCity/Player/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = PlayerPivot;
            }
            else if (path.StartsWith("Assets/ThirdParty/Ansimuz/") &&
                     (path.Contains("/Props/") || path.Contains("/Lab/") || path.Contains("/Security/") || path.Contains("/Core/")))
            {
                // Backdrops and props stand on the floor.
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }

            // Tiles stretch to any size with fixed 16 px borders (drawn Tiled in the scene).
            if (path.EndsWith("/LabBlock.png"))
                settings.spriteBorder = new Vector4(16, 16, 16, 16);
            else if (path.EndsWith("/LabPlatform.png"))
                settings.spriteBorder = new Vector4(16, 0, 16, 0);

            importer.SetTextureSettings(settings);
        }

        void OnPreprocessAudio()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/ThirdParty/"))
                return;

            bool music = path.Contains("/SRG774/");
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = !music;
            importer.loadInBackground = music;

            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.45f : 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
