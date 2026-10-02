using System.Collections.Generic;
using Echo.Audio;
using Echo.Core;
using Echo.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Builds ECHO's art data from the imported packs: effect materials, the player's animation
    /// set, every particle effect (the VFX library) and the audio library, then plugs the
    /// libraries into the Systems prefab. Re-runnable: existing assets are refreshed in place,
    /// so their GUIDs and every reference to them survive.
    /// </summary>
    public static class EchoArtFactory
    {
        public const string PlayerVisualsPath = "Assets/Settings/Visuals/PlayerVisuals.asset";
        public const string VfxLibraryPath = "Assets/Settings/Visuals/VfxLibrary.asset";
        public const string AudioLibraryPath = "Assets/Settings/Audio/AudioLibrary.asset";
        public const string KitsFolder = "Assets/Settings/Visuals/Kits";

        const string MaterialsFolder = "Assets/Art/Materials";
        const string VfxFolder = "Assets/Prefabs/VFX";
        const string PlayerFrames = "Assets/ThirdParty/Ansimuz/WarpedCity/Player";
        const string PixelArt = "Assets/Art/Pixel";
        const string Particles = "Assets/ThirdParty/Kenney/Particles";
        const string Sfx = "Assets/ThirdParty/Kenney/Audio";
        const string Music = "Assets/ThirdParty/SRG774/DarkSciFiAudio";

        const string AdditiveShader = "Echo/Additive";
        const string UnlitSpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        /// <summary>Additive white glow (dash trail, afterimages). Bright enough to bloom.</summary>
        public static Material GlowMaterial => EnsureMaterial("FX_Glow", AdditiveShader, null, 2.2f);

        /// <summary>Additive pixel squares: sparks, shards, energy motes.</summary>
        public static Material PixelMaterial => EnsureMaterial("FX_Pixel", AdditiveShader, $"{PixelArt}/FX/PixelSquare.png", 2.5f);

        /// <summary>Additive pixel ring: shockwaves.</summary>
        public static Material RingMaterial => EnsureMaterial("FX_Ring", AdditiveShader, $"{PixelArt}/FX/Ring.png", 2f);

        /// <summary>Additive soft round glow: flashes and halos around light sources.</summary>
        public static Material SoftGlowMaterial => EnsureMaterial("FX_SoftGlow", AdditiveShader, $"{Particles}/light_01.png", 1.4f);

        /// <summary>Alpha-blended, unlit pixel squares: dust and ambient motes.</summary>
        public static Material DustMaterial => EnsureMaterial("FX_Dust", UnlitSpriteShader, $"{PixelArt}/FX/PixelSquare.png", 1f);

        /// <summary>
        /// Builds the art data and rebuilds everything that shows it: the player prefab, the
        /// persistent UI, the main menu, the movement sandbox and the design showcase.
        /// Headless: -batchmode -quit -executeMethod Echo.EditorTools.EchoArtFactory.ApplyVisualOverhaul
        /// </summary>
        [MenuItem("ECHO/Setup/Apply Visual Overhaul")]
        public static void ApplyVisualOverhaul()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EchoSpriteFactory.EnsureAll();
            EnsureAll();
            EchoPrefabFactory.EnsurePlayerPrefab(rebuild: true);
            EchoUiFactory.RebuildSystemsUI();
            EchoSceneFactory.RebuildMainMenu();
            EchoSceneFactory.RebuildSandbox();
            EchoShowcaseFactory.Rebuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[ECHO Art] Visual overhaul applied.");
        }

        public static void EnsureAll()
        {
            EnsurePlayerVisuals();
            EnsureEnvironmentKits();
            var vfx = EnsureVfxLibrary();
            var audio = EnsureAudioLibrary();
            WireSystems(vfx, audio);
            AssetDatabase.SaveAssets();
        }

        // --- materials --------------------------------------------------------------------------

        static Material EnsureMaterial(string name, string shaderName, string texturePath, float intensity)
        {
            EchoProjectSetup.EnsureFolderPath(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";

            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"[ECHO Art] Shader '{shaderName}' not found.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.mainTexture = texturePath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) : null;
            if (material.HasProperty("_Intensity"))
                material.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(material);
            return material;
        }

        // --- player -----------------------------------------------------------------------------

        public static PlayerVisuals EnsurePlayerVisuals()
        {
            var visuals = LoadOrCreate<PlayerVisuals>(PlayerVisualsPath);

            // Frame choices from the Warped City hero: see Docs/VISUAL_OVERHAUL_PLAN.md.
            Set(visuals.idle, 6f, true, Frames("idle", 1, 2, 3, 4));
            Set(visuals.run, 14f, true, Frames("run", 1, 2, 3, 4, 5, 6, 7, 8));
            Set(visuals.rise, 10f, false, Frames("run", 5));
            Set(visuals.apexFlip, 14f, false, Frames("jump", 1, 2, 3));
            Set(visuals.fall, 10f, false, Frames("jump", 4));
            Set(visuals.land, 10f, false, Frames("back-jump", 1));
            Set(visuals.dash, 10f, false, Frames("run", 2));
            Set(visuals.hurt, 10f, false, Frames("hurt", 1));
            Set(visuals.victory, 12f, false, Frames("back-jump", 1, 2, 3, 4, 5, 6, 7));

            EditorUtility.SetDirty(visuals);
            return visuals;
        }

        static void Set(PlayerVisuals.Clip clip, float fps, bool loop, Sprite[] frames)
        {
            clip.framesPerSecond = fps;
            clip.loop = loop;
            clip.frames = frames;
        }

        static Sprite[] Frames(string animation, params int[] numbers)
        {
            var frames = new Sprite[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
                frames[i] = LoadSprite($"{PlayerFrames}/{animation}/{animation}_{numbers[i]:00}.png");
            return frames;
        }

        public static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new System.InvalidOperationException($"[ECHO Art] Missing sprite {path}. Run Tools/import_assets.py and let Unity import it.");
            return sprite;
        }

        public static Sprite[] LoadSprites(string pathFormat, int count)
        {
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = LoadSprite(string.Format(pathFormat, i));
            return sprites;
        }

        // --- biomes -----------------------------------------------------------------------------

        /// <summary>One kit per biome (Assets/Settings/Visuals/Kits). Refreshed in place each run.</summary>
        public static void EnsureEnvironmentKits()
        {
            const string lab = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Lab";
            const string security = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Security";
            const string core = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Core";
            const string props = "Assets/ThirdParty/Ansimuz/WarpedCity/Props";

            Kit(Biome.Lab, "ABANDONED LAB",
                "Dark teal corridors, cryo pods and dead monitors under dim cyan ceiling lights. The city glows through the windows.",
                $"{PixelArt}/Lab/LabBlock.png",
                new[] { $"{lab}/Corridor_A.png", $"{lab}/Corridor_Machinery.png", $"{lab}/Corridor_Terminal.png", $"{lab}/Corridor_A.png" },
                Color.white, $"{lab}/Corridor_Back.png", new Color(0.6f, 0.8f, 0.95f),
                new Color32(0x00, 0x04, 0x10, 0xFF), new Color(0.85f, 0.93f, 1f), 0.85f, new Color(0.55f, 0.85f, 0.95f),
                new[] { $"{lab}/CryoPod.png", $"{lab}/ServerCabinets.png", $"{lab}/SmallTerminal.png", $"{props}/ScreenPod.png" },
                MusicId.Lab);

            Kit(Biome.Security, "SECURITY WING",
                "Cold steel bulkheads and hard white light. Red scanners sweep the halls and turrets guard the doors.",
                $"{PixelArt}/Security/SecurityBlock.png",
                new[] { $"{security}/Bulkhead_bg-wall-with-supports.png", $"{security}/Bulkhead_bg-wall.png" },
                Color.white, null, Color.white,
                new Color32(0x07, 0x09, 0x0A, 0xFF), new Color(0.8f, 0.88f, 1f), 0.8f, new Color(0.85f, 0.92f, 1f),
                new[] { $"{props}/LabDoor.png", $"{props}/ServerRack.png", $"{props}/Terminal.png", $"{props}/WallPanelTall.png" },
                MusicId.Security);

            Kit(Biome.Core, "ENERGY CORE",
                "Deep navy machinery wrapped in cyan conduits. Everything here hums with the energy ECHO runs on.",
                $"{PixelArt}/Core/CoreBlock.png",
                new[] { $"{core}/Machinery_Back.png" },
                new Color(0.7f, 0.82f, 1f), null, Color.white,
                new Color32(0x04, 0x06, 0x0F, 0xFF), new Color(0.75f, 0.85f, 1f), 0.75f, new Color(0.4f, 0.9f, 1f),
                new[] { $"{props}/EnergyPod.png", $"{props}/PillarMachine.png", $"{props}/MachineryPipes.png" },
                MusicId.Core);

            Kit(Biome.Reactor, "THE GUARDIAN'S REACTOR",
                "The central reactor. Calm, it glows energy cyan; when the Guardian attacks it overloads to danger red.",
                $"{PixelArt}/Core/CoreBlock.png",
                new[] { $"{core}/Machinery_Back.png" },
                new Color(0.45f, 0.55f, 0.75f), null, Color.white,
                new Color32(0x03, 0x04, 0x0B, 0xFF), new Color(0.7f, 0.78f, 1f), 0.6f, new Color(0.4f, 0.9f, 1f),
                new[] { $"{PixelArt}/Core/Reactor_Calm.png", $"{PixelArt}/Core/Reactor_Overload.png" },
                MusicId.Boss);
        }

        public static EnvironmentKit LoadKit(Biome biome) =>
            AssetDatabase.LoadAssetAtPath<EnvironmentKit>($"{KitsFolder}/{biome}.asset");

        static void Kit(Biome biome, string name, string description, string block, string[] backdrop, Color backdropTint,
                        string farLayer, Color farTint, Color voidColor, Color ambient, float ambientIntensity, Color lamp,
                        string[] props, MusicId music)
        {
            var kit = LoadOrCreate<EnvironmentKit>($"{KitsFolder}/{biome}.asset");
            kit.biome = biome;
            kit.displayName = name;
            kit.description = description;
            kit.block = LoadSprite(block);
            kit.backdrop = System.Array.ConvertAll(backdrop, LoadSprite);
            kit.backdropTint = backdropTint;
            kit.farLayer = farLayer != null ? LoadSprite(farLayer) : null;
            kit.farTint = farTint;
            kit.voidColor = voidColor;
            kit.ambientColor = ambient;
            kit.ambientIntensity = ambientIntensity;
            kit.lampColor = lamp;
            kit.props = System.Array.ConvertAll(props, LoadSprite);
            kit.music = music;
            EditorUtility.SetDirty(kit);
        }

        // --- effects ----------------------------------------------------------------------------

        static VfxLibrary EnsureVfxLibrary()
        {
            Color energy = EchoPalette.Energy;
            Color energyLight = new Color(0.7f, 0.96f, 1f);
            Color exit = EchoPalette.Exit;
            Color danger = EchoPalette.Hazard;
            Color dust = new Color(0.45f, 0.62f, 0.66f, 0.85f);
            Color dustLight = new Color(0.62f, 0.78f, 0.8f, 0.7f);
            Color hair = new Color(0.82f, 0.2f, 0.85f);
            Color fizzle = new Color(0.45f, 0.55f, 0.65f, 0.8f);

            var pixel = PixelMaterial;
            var ring = RingMaterial;
            var soft = SoftGlowMaterial;
            var dustMaterial = DustMaterial;

            var entries = new List<VfxLibrary.Entry>
            {
                Effect(VfxId.DashBurst, root =>
                {
                    // Sparks spray out behind the player (local -X; mirrored for left dashes).
                    root.Burst(16).Life(0.15f, 0.3f).Speed(4f, 9f).Size(0.0625f, 0.125f).Colors(energy, energyLight)
                        .Arc(0.25f, 70f, 145f).Drag(4f).Shrink();
                    new EchoFx("SpeedLines", root.Transform, pixel).Burst(6).Life(0.1f, 0.18f).Speed(10f, 15f).Size(0.05f, 0.07f)
                        .Colors(energy, energyLight).Arc(0.45f, 24f, 168f).Stretch(4f).Fade();
                    new EchoFx("Ring", root.Transform, ring).Burst(1).Life(0.18f, 0.18f).Size(1.6f, 1.6f)
                        .Colors(Alpha(energy, 0.8f), Alpha(energy, 0.8f)).Grow(0.25f).Fade();
                }, pixel),

                Effect(VfxId.JumpDust, root =>
                {
                    root.Burst(6).Life(0.25f, 0.4f).Speed(0.8f, 2f).Size(0.0625f, 0.15f).Colors(dust, dustLight)
                        .Arc(0.2f, 180f, 0f, 0.35f).Drag(3f).Shrink();
                }, dustMaterial, "VFX"),

                Effect(VfxId.LandDust, root =>
                {
                    root.Burst(10).Life(0.3f, 0.45f).Speed(1.5f, 3.5f).Size(0.0625f, 0.19f).Colors(dust, dustLight)
                        .Arc(0.3f, 180f, 0f, 0.2f).Drag(5f).Shrink();
                }, dustMaterial),

                Effect(VfxId.StepDust, root =>
                {
                    root.Burst(3).Life(0.2f, 0.3f).Speed(0.6f, 1.4f).Size(0.0625f, 0.125f).Colors(dust, dustLight)
                        .Arc(0.1f, 40f, 140f).Drag(3f).Shrink();
                }, dustMaterial),

                Effect(VfxId.PickupBurst, root =>
                {
                    root.Burst(14).Life(0.25f, 0.45f).Speed(2f, 5f).Size(0.0625f, 0.125f).Colors(energy, energyLight)
                        .Arc(0.2f, 360f).Drag(3f).Shrink();
                    new EchoFx("Ring", root.Transform, ring).Burst(1).Life(0.22f, 0.22f).Size(1.8f, 1.8f)
                        .Colors(Alpha(energy, 0.9f), Alpha(energy, 0.9f)).Grow(0.2f).Fade();
                    new EchoFx("Flash", root.Transform, soft).Burst(1).Life(0.18f, 0.18f).Size(2.4f, 2.4f)
                        .Colors(Alpha(energy, 0.7f), Alpha(energy, 0.7f)).Fade();
                }, pixel),

                Effect(VfxId.DashDenied, root =>
                {
                    // No energy: a dim fizzle in front of the player, no glow.
                    root.Burst(6).Life(0.15f, 0.25f).Speed(1f, 2.5f).Size(0.0625f, 0.094f).Colors(fizzle, Alpha(fizzle, 0.4f))
                        .Arc(0.2f, 90f, -45f).Gravity(0.6f).Shrink();
                }, dustMaterial),

                Effect(VfxId.BarrierShatter, root =>
                {
                    // Shards fly the way the dash was going.
                    root.Burst(40).Life(0.45f, 0.9f).Size(0.0625f, 0.19f).Colors(energy, energyLight)
                        .Box(new Vector2(0.5f, 4.6f)).Velocity(new Vector2(2f, -2f), new Vector2(9f, 5f)).Gravity(1.6f).Drag(1f).Shrink();
                    new EchoFx("Flash", root.Transform, soft).Burst(1).Life(0.15f, 0.15f).Size(5f, 5f)
                        .Colors(Alpha(energy, 0.8f), Alpha(energy, 0.8f)).Fade();
                }, pixel),

                Effect(VfxId.ExitShatter, root =>
                {
                    root.Burst(60).Life(0.6f, 1.1f).Size(0.0625f, 0.22f).Colors(exit, Color.white)
                        .Box(new Vector2(0.5f, 4.6f)).Velocity(new Vector2(1f, -2f), new Vector2(10f, 7f)).Gravity(1.2f).Drag(1f).Shrink();
                    new EchoFx("Flash", root.Transform, soft).Burst(1).Life(0.25f, 0.25f).Size(8f, 8f)
                        .Colors(Alpha(exit, 0.9f), Alpha(exit, 0.9f)).Fade();
                    new EchoFx("Ring", root.Transform, ring).Burst(1).Life(0.35f, 0.35f).Size(6f, 6f)
                        .Colors(Alpha(exit, 0.9f), Alpha(exit, 0.9f)).Grow(0.15f).Fade();
                }, pixel),

                Effect(VfxId.PlayerDeath, root =>
                {
                    // The hero breaks into her own colors; the ring is warm because danger did this.
                    root.Burst(28).Life(0.5f, 0.85f).Speed(3f, 8f).Size(0.0625f, 0.156f).Colors(Color.white, hair)
                        .Arc(0.3f, 360f).Gravity(1.2f).Drag(1.5f).Shrink();
                    new EchoFx("Ring", root.Transform, ring).Burst(1).Life(0.3f, 0.3f).Size(3f, 3f)
                        .Colors(Alpha(danger, 0.9f), Alpha(danger, 0.9f)).Grow(0.1f).Fade();
                }, pixel),

                Effect(VfxId.PlayerRespawn, root =>
                {
                    root.Burst(18).Life(0.3f, 0.55f).Size(0.0625f, 0.125f).Colors(energy, energyLight)
                        .Box(new Vector2(0.7f, 0.1f)).Velocity(new Vector2(-0.3f, 2f), new Vector2(0.3f, 5f)).Shrink();
                    new EchoFx("Flash", root.Transform, soft).Burst(1).Life(0.25f, 0.25f).Size(3f, 3f)
                        .Colors(Alpha(energy, 0.6f), Alpha(energy, 0.6f)).Fade();
                }, pixel),

                Effect(VfxId.Victory, root =>
                {
                    root.Burst(30).Life(0.8f, 1.2f).Speed(3f, 7f).Size(0.0625f, 0.156f).Colors(exit, energyLight)
                        .Arc(0.3f, 120f, 30f).Gravity(0.8f).Drag(1.5f).Fade(0.5f);
                    new EchoFx("Ring", root.Transform, ring).Burst(1).Life(0.4f, 0.4f).Size(4f, 4f)
                        .Colors(Alpha(exit, 0.8f), Alpha(exit, 0.8f)).Grow(0.1f).Fade();
                }, pixel),
            };

            var library = LoadOrCreate<VfxLibrary>(VfxLibraryPath);
            library.entries = entries.ToArray();
            EditorUtility.SetDirty(library);
            return library;
        }

        static VfxLibrary.Entry Effect(VfxId id, System.Action<EchoFx> build, Material rootMaterial, string sortingLayer = "VFX")
        {
            EchoProjectSetup.EnsureFolderPath(VfxFolder);
            var root = new EchoFx(id.ToString(), null, rootMaterial, sortingLayer);
            build(root);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.System.gameObject, $"{VfxFolder}/{id}.prefab");
            Object.DestroyImmediate(root.System.gameObject);
            return new VfxLibrary.Entry { id = id, prefab = prefab.GetComponent<ParticleSystem>() };
        }

        static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        // --- audio ------------------------------------------------------------------------------

        static AudioLibrary EnsureAudioLibrary()
        {
            var library = LoadOrCreate<AudioLibrary>(AudioLibraryPath);

            library.sounds = new[]
            {
                Sound(SfxId.Footstep, 0.3f, 0.08f, 0.08f, "footstep_concrete_000", "footstep_concrete_001", "footstep_concrete_002", "footstep_concrete_003", "footstep_concrete_004"),
                Sound(SfxId.Jump, 0.35f, 0.06f, 0f, "phaseJump2"),
                Sound(SfxId.Land, 0.5f, 0.08f, 0.05f, "impactSoft_medium_000"),
                Sound(SfxId.Dash, 0.55f, 0.06f, 0f, "forceField_000"),
                Sound(SfxId.DashDenied, 0.45f, 0.03f, 0.15f, "error_004"),
                Sound(SfxId.EnergyPickup, 0.55f, 0.04f, 0f, "powerUp2"),
                Sound(SfxId.BarrierBreak, 0.7f, 0.05f, 0f, "impactGlass_heavy_002", "impactGlass_medium_001"),
                Sound(SfxId.ExitBreak, 0.8f, 0.02f, 0f, "lowFrequency_explosion_000"),
                Sound(SfxId.PlayerDeath, 0.7f, 0.05f, 0f, "explosionCrunch_000"),
                Sound(SfxId.PlayerRespawn, 0.45f, 0.03f, 0f, "powerUp5"),
                MusicSound(SfxId.LevelComplete, 0.7f, "victory"),
                Sound(SfxId.UiHover, 0.35f, 0.04f, 0.05f, "select_002"),
                Sound(SfxId.UiConfirm, 0.5f, 0f, 0f, "confirmation_002"),
                Sound(SfxId.UiBack, 0.5f, 0f, 0f, "back_002"),
                Sound(SfxId.PauseOpen, 0.5f, 0f, 0f, "open_002"),
                Sound(SfxId.PauseClose, 0.5f, 0f, 0f, "close_002"),
            };

            library.tracks = new[]
            {
                Track(MusicId.Menu, "title", 0.55f, null, 0f),
                Track(MusicId.Lab, "sector", 0.45f, "spaceEngineLow_000", 0.18f),
                Track(MusicId.Security, "pulse", 0.45f, "computerNoise_000", 0.12f),
                Track(MusicId.Core, "airy", 0.45f, "spaceEngineLow_000", 0.2f),
                Track(MusicId.Boss, "urgent", 0.55f, null, 0f),
                Track(MusicId.Ending, "transmission", 0.5f, null, 0f),
            };

            EditorUtility.SetDirty(library);
            return library;
        }

        static AudioLibrary.Sound Sound(SfxId id, float volume, float pitchJitter, float cooldown, params string[] clips)
        {
            var variations = new AudioClip[clips.Length];
            for (int i = 0; i < clips.Length; i++)
                variations[i] = LoadClip($"{Sfx}/{clips[i]}.ogg");
            return new AudioLibrary.Sound { id = id, variations = variations, volume = volume, pitchJitter = pitchJitter, cooldown = cooldown };
        }

        static AudioLibrary.Sound MusicSound(SfxId id, float volume, string clip) =>
            new AudioLibrary.Sound { id = id, variations = new[] { LoadClip($"{Music}/{clip}.ogg") }, volume = volume, pitchJitter = 0f };

        static AudioLibrary.Track Track(MusicId id, string music, float musicVolume, string ambience, float ambienceVolume) =>
            new AudioLibrary.Track
            {
                id = id,
                music = LoadClip($"{Music}/{music}.ogg"),
                musicVolume = musicVolume,
                ambience = ambience != null ? LoadClip($"{Sfx}/{ambience}.ogg") : null,
                ambienceVolume = ambienceVolume,
            };

        static AudioClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                throw new System.InvalidOperationException($"[ECHO Art] Missing audio clip {path}.");
            return clip;
        }

        // --- systems ----------------------------------------------------------------------------

        static void WireSystems(VfxLibrary vfx, AudioLibrary audio)
        {
            string path = EchoProjectSetup.SystemsPrefabPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            EchoPrefabFactory.SetReference(GetOrAdd<VfxManager>(root), "library", vfx);
            EchoPrefabFactory.SetReference(GetOrAdd<AudioManager>(root), "library", audio);
            GetOrAdd<WorldFeedback>(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EchoProjectSetup.EnsureFolderPath(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
