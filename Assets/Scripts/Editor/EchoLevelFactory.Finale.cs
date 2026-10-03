using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.UI;
using Echo.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static Echo.EditorTools.EchoSceneFactory;

namespace Echo.EditorTools
{
    /// <summary>The finale: the Guardian's arena and the ending.</summary>
    public static partial class EchoLevelFactory
    {
        public static string BossPath => $"{ScenesFolder}/Levels/{SceneNames.Boss}.unity";
        public static string EndingPath => $"{ScenesFolder}/Levels/{SceneNames.Ending}.unity";

        // The arena is one fixed screen: x -10..10, floor at 0, ceiling at 9.6.
        const float ArenaCeiling = 9.6f;
        const string Core = "Assets/Art/Pixel/Core";

        [MenuItem("ECHO/Setup/Rebuild Boss Arena")]
        public static void RebuildBoss()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsureScene(BossPath, BuildBoss, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        [MenuItem("ECHO/Setup/Rebuild Ending")]
        public static void RebuildEnding()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsureScene(EndingPath, BuildEnding, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        // The Guardian's reactor. The player enters on the left. Two steel platforms at height
        // 2.6 give a place to dash from; three cells come back 4 s after they are taken. Bolt
        // ports sit low and high on the left wall and on the blast door at the right, behind
        // which the exit waits. Start with 1 charge, max 3.
        static void BuildBoss()
        {
            var kit = EchoArtFactory.LoadKit(Biome.Reactor);
            var camera = CreateCamera(kit.voidColor);
            DisableCameraRoll(camera.gameObject.AddComponent<CameraFeedback>());
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(kit.ambientIntensity, kit.ambientColor);
            var sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();
            SetEnum(sceneAudio, "music", (int)MusicId.Boss);

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var pickups = Child("Cells", level.transform);
            var barriers = Child("Barriers", level.transform);
            var doors = Child("Doors", level.transform);
            var attacks = Child("Attacks", level.transform);
            var hints = Child("Hints", level.transform);
            var backdrop = Child("Backdrop", level.transform);
            var lights = Child("Lights", level.transform);

            // --- arena -----------------------------------------------------------------------
            KitBlock(geometry, kit, "Wall_Left", -11f, Bottom, -10f, WallTop);
            KitGround(geometry, kit, "Floor", -10f, 10f, 0f);
            KitBlock(geometry, kit, "Ceiling", -10f, ArenaCeiling, 10f, WallTop);
            KitBlock(geometry, kit, "Doorway_Exit", 8.6f, CeilingY, 10f, ArenaCeiling);
            KitBlock(geometry, kit, "Wall_Right", 10f, Bottom, 11f, WallTop);
            Platform(geometry, "Platform_Left", -7f, -4f, 2.6f);
            Platform(geometry, "Platform_Right", 4f, 7f, 2.6f);

            var exitShutter = Shutter(doors, 9f, 0f, CeilingY);
            Barrier("ExitDoor", barriers, 9.45f, 9.95f, true);

            CellSpot(pickups, -5.5f, 3.6f);
            CellSpot(pickups, 5.5f, 3.6f);
            CellSpot(pickups, 0f, 1f);

            Hint(hints, HintId.Guardian, -10f, -6.5f, 0f);

            // --- dressing --------------------------------------------------------------------
            ArenaBackdrop(kit, backdrop, -11f, 11f);
            var reactorObject = EchoLevelArt.Reactor(backdrop, new Vector2(0f, 5.4f), false);
            var reactor = reactorObject.AddComponent<ReactorMood>();
            EchoPrefabFactory.SetReference(reactor, "reactor", reactorObject.GetComponent<SpriteRenderer>());
            EchoPrefabFactory.SetReference(reactor, "calm", EchoArtFactory.LoadSprite($"{Core}/Reactor_Calm.png"));
            EchoPrefabFactory.SetReference(reactor, "overload", EchoArtFactory.LoadSprite($"{Core}/Reactor_Overload.png"));
            EchoPrefabFactory.SetReference(reactor, "halo", reactorObject.transform.Find("Halo").GetComponent<SpriteRenderer>());
            EchoPrefabFactory.SetReference(reactor, "reactorLight", reactorObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>());
            foreach (float x in new[] { -8f, 8f })
                EchoLevelArt.CeilingLamp(lights, x, ArenaCeiling - 0.4f, false, kit.lampColor);

            // --- the Guardian and its attacks ------------------------------------------------
            var leftPorts = new[] { WallPort(attacks, -9.8f, 0.55f), WallPort(attacks, -9.8f, 3.25f) };
            var rightPorts = new[] { WallPort(attacks, 8.45f, 0.55f), WallPort(attacks, 8.45f, 3.25f) };
            var waves = new[] { ShockwaveAt(attacks), ShockwaveAt(attacks) };
            var strikes = new GuardianStrike[4];
            for (int i = 0; i < strikes.Length; i++)
                strikes[i] = StrikeAt(attacks);
            BuildGuardian(level.transform, attacks, reactor, exitShutter, leftPorts, rightPorts, waves, strikes);

            Finish(level, camera, new Vector2(-8.5f, 1f), new Rect(-10f, -1.125f, 20f, 11.25f),
                   "THE REACTOR", "THE GUARDIAN", 1, 3, SceneNames.Ending);
        }

        static void BuildGuardian(Transform parent, Transform attacks, ReactorMood reactor, SecurityDoor exitShutter,
                                  SpriteRenderer[] leftPorts, SpriteRenderer[] rightPorts,
                                  GuardianShockwave[] waves, GuardianStrike[] strikes)
        {
            var go = new GameObject("Guardian") { layer = LayerMask.NameToLayer(Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 7f, 0f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.95f;

            var coreCalm = EchoArtFactory.LoadSprite($"{Core}/Guardian_CoreCalm.png");
            var coreOverload = EchoArtFactory.LoadSprite($"{Core}/Guardian_CoreOverload.png");
            var core = Part(go.transform, "Core", coreCalm, Vector2.zero, 1);
            var top = EchoArtFactory.LoadSprite($"{Core}/Guardian_ShellTop.png");
            var shellTop = Part(go.transform, "ShellTop", top, new Vector2(0f, top.bounds.size.y * 0.5f), 2);
            var bottom = EchoArtFactory.LoadSprite($"{Core}/Guardian_ShellBottom.png");
            var shellBottom = Part(go.transform, "ShellBottom", bottom, new Vector2(0f, -bottom.bounds.size.y * 0.5f), 2);
            var seam = Part(go.transform, "Seam", EchoArtFactory.LoadSprite($"{Core}/Guardian_Seam.png"), Vector2.zero, 3);
            var glow = EchoLevelArt.AddPointLight(go, new Color(1f, 0.35f, 0.25f), 1f, 3.5f);

            var guardian = go.AddComponent<Guardian>();
            EchoPrefabFactory.SetReference(guardian, "shellTop", shellTop.transform);
            EchoPrefabFactory.SetReference(guardian, "shellBottom", shellBottom.transform);
            EchoPrefabFactory.SetReference(guardian, "core", core);
            EchoPrefabFactory.SetReference(guardian, "coreCalm", coreCalm);
            EchoPrefabFactory.SetReference(guardian, "coreOverload", coreOverload);
            EchoPrefabFactory.SetReference(guardian, "seam", seam);
            EchoPrefabFactory.SetReference(guardian, "glow", glow);
            EchoPrefabFactory.SetReference(guardian, "reactor", reactor);
            EchoPrefabFactory.SetReference(guardian, "attackRoot", attacks);
            EchoPrefabFactory.SetReference(guardian, "boltSprite", EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/TurretBolt.png"));
            var settings = new SerializedObject(guardian);
            SetArray(settings.FindProperty("leftPorts"), leftPorts);
            SetArray(settings.FindProperty("rightPorts"), rightPorts);
            SetArray(settings.FindProperty("shockwaves"), waves);
            SetArray(settings.FindProperty("strikes"), strikes);
            SetArray(settings.FindProperty("onDefeat"), new Object[] { exitShutter });
            settings.FindProperty("arenaLeft").floatValue = -9.6f;
            settings.FindProperty("arenaRight").floatValue = 8.3f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static SpriteRenderer Part(Transform parent, string name, Sprite sprite, Vector2 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = order;
            return renderer;
        }

        static SpriteRenderer WallPort(Transform parent, float x, float y)
        {
            var go = new GameObject("BoltPort");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite($"{Core}/WallPort.png");
            renderer.sortingLayerName = "Interactables";
            renderer.sortingOrder = 4;
            return renderer;
        }

        // A floor wave, waiting off (it switches itself off when the scene starts).
        static GuardianShockwave ShockwaveAt(Transform parent)
        {
            var frames = EchoArtFactory.LoadSprites($"{Core}/Shockwave_{{0:00}}.png", 3);
            float height = frames[0].bounds.size.y;
            var go = new GameObject("Shockwave") { layer = LayerMask.NameToLayer(Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, height * 0.5f, 0f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.45f, height * 0.9f);
            trigger.offset = new Vector2(0f, -height * 0.05f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = "Hazards";
            Frames(visual.AddComponent<SpriteFlipbook>(), frames, 12f);
            EchoLevelArt.AddPointLight(visual, new Color(1f, 0.35f, 0.2f), 0.7f, 1.8f);

            var wave = go.AddComponent<GuardianShockwave>();
            EchoPrefabFactory.SetReference(wave, "visual", visual.transform);
            go.SetActive(false); // until the Guardian launches it
            return wave;
        }

        // A ceiling-to-floor beam the Guardian aims at the player; hidden until it strikes.
        static GuardianStrike StrikeAt(Transform parent)
        {
            var frames = EchoArtFactory.LoadSprites("Assets/Art/Pixel/Gameplay/LaserBeam_{0:00}.png", 3);
            var go = new GameObject("Strike") { layer = LayerMask.NameToLayer(Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, ArenaCeiling * 0.5f, 0f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.4f, ArenaCeiling);

            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            var renderer = beam.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(0.5f, ArenaCeiling);
            renderer.sortingLayerName = "Hazards";
            renderer.enabled = false; // until the Guardian strikes
            trigger.enabled = false;
            Frames(beam.AddComponent<SpriteFlipbook>(), frames, 16f);

            var strike = go.AddComponent<GuardianStrike>();
            EchoPrefabFactory.SetReference(strike, "beam", renderer);
            EchoPrefabFactory.SetReference(strike, "hurtZone", trigger);
            return strike;
        }

        // The reactor chamber is taller than a corridor: three rows of machinery up to the
        // ceiling, with conduits running the full height.
        static void ArenaBackdrop(EnvironmentKit kit, Transform parent, float xMin, float xMax)
        {
            const float floorY = -0.25f;
            float row = kit.backdrop[0].bounds.size.y;
            for (int i = 0; i < 3; i++)
                EchoLevelArt.WallStrip(kit, parent, xMin, xMax, floorY + i * row);
            for (float x = xMin + 2.5f; x < xMax; x += 6.5f)
                EchoLevelArt.Conduit(parent, x, floorY, row * 3f);
        }

        // An energy cell that comes back 4 s after it is taken.
        static void CellSpot(Transform parent, float x, float y)
        {
            var spot = new GameObject("CellSpot");
            spot.transform.SetParent(parent, false);
            spot.transform.position = new Vector3(x, y, 0f);
            Pickup(spot.transform, x, y);
            var respawner = spot.AddComponent<CellRespawner>();
            EchoPrefabFactory.SetReference(respawner, "pickup", spot.GetComponentInChildren<EnergyPickup>());
        }

        static void SetArray(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // --- the ending ------------------------------------------------------------------------

        // The calm reactor and the hero beside it; captions, the rescue ship's answer, THE END.
        static void BuildEnding()
        {
            var kit = EchoArtFactory.LoadKit(Biome.Reactor);
            var camera = CreateCamera(kit.voidColor);
            camera.transform.position = new Vector3(0f, 4.5f, -10f);
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(kit.ambientIntensity, kit.ambientColor);
            var sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();
            SetEnum(sceneAudio, "music", (int)MusicId.Ending);

            var set = new GameObject("Set");
            var geometry = Child("Geometry", set.transform);
            var backdrop = Child("Backdrop", set.transform);
            KitGround(geometry, kit, "Floor", -12f, 12f, 0f);
            KitBlock(geometry, kit, "Ceiling", -12f, ArenaCeiling, 12f, WallTop);
            ArenaBackdrop(kit, backdrop, -12f, 12f);
            EchoLevelArt.Reactor(backdrop, new Vector2(3.5f, 5.4f), false);
            EchoLevelArt.Hero(set.transform, new Vector2(-3f, 0f), false);
            foreach (float x in new[] { -7f, 7f })
                EchoLevelArt.CeilingLamp(set.transform, x, ArenaCeiling - 0.4f, false, kit.lampColor);

            BuildEndingUI();
        }

        static void BuildEndingUI()
        {
            var plain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // full alphabet: Kenney Future's K reads as H
            var canvas = EchoUiFactory.CreateCanvas("EndingCanvas", null, 20);

            var captionRect = EchoUiFactory.NewRect("Caption", canvas);
            EchoUiFactory.Place(captionRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(820f, 90f));
            var captionGroup = captionRect.gameObject.AddComponent<CanvasGroup>();
            var caption = EchoUiFactory.NewLabel("Text", captionRect, string.Empty, 26, new Color(0.92f, 0.95f, 1f), TextAnchor.MiddleCenter);
            Wrap(caption, plain);
            EchoUiFactory.Stretch(caption.rectTransform);

            var panel = EchoUiFactory.NewRect("Transmission", canvas);
            EchoUiFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 200f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = EchoUiFactory.UiSprite("Panel");
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;
            var transmission = panel.gameObject.AddComponent<CanvasGroup>();
            var header = EchoUiFactory.NewLabel("Header", panel, "INCOMING TRANSMISSION", 22, EchoPalette.Energy, TextAnchor.UpperCenter);
            EchoUiFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(640f, 30f));
            var body = EchoUiFactory.NewLabel("Body", panel, string.Empty, 24, new Color(0.92f, 0.95f, 1f), TextAnchor.UpperLeft);
            EchoUiFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(620f, 120f));
            Wrap(body, plain);

            var endRect = EchoUiFactory.NewRect("TheEnd", canvas);
            EchoUiFactory.Stretch(endRect);
            var theEnd = endRect.gameObject.AddComponent<CanvasGroup>();
            var title = EchoUiFactory.NewLabel("Title", endRect, "THE END", 72, Color.white, TextAnchor.MiddleCenter);
            EchoUiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(700f, 90f));
            var thanks = EchoUiFactory.NewLabel("Thanks", endRect, "Thanks for playing ECHO.", 28, EchoPalette.Energy, TextAnchor.MiddleCenter);
            thanks.font = plain;
            EchoUiFactory.Place(thanks.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(700f, 40f));

            var promptRect = EchoUiFactory.NewRect("Prompt", endRect);
            EchoUiFactory.Place(promptRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(700f, 40f));
            var prompt = promptRect.gameObject.AddComponent<CanvasGroup>();
            var promptText = EchoUiFactory.NewLabel("Text", promptRect, "Press Jump or tap to return to the menu", 22,
                                                    new Color(0.75f, 0.82f, 0.9f), TextAnchor.MiddleCenter);
            promptText.font = plain;
            EchoUiFactory.Stretch(promptText.rectTransform);

            var sequence = canvas.gameObject.AddComponent<EndingSequence>();
            EchoPrefabFactory.SetReference(sequence, "captionGroup", captionGroup);
            EchoPrefabFactory.SetReference(sequence, "caption", caption);
            EchoPrefabFactory.SetReference(sequence, "transmission", transmission);
            EchoPrefabFactory.SetReference(sequence, "transmissionBody", body);
            EchoPrefabFactory.SetReference(sequence, "theEnd", theEnd);
            EchoPrefabFactory.SetReference(sequence, "prompt", prompt);
            var settings = new SerializedObject(sequence);
            var captions = settings.FindProperty("captions");
            string[] lines =
            {
                "The reactor settles. For the first time since she woke, the facility is quiet.",
                "Its power reaches the antenna on the roof, and a distress beacon goes out into the dark.",
            };
            captions.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
                captions.GetArrayElementAtIndex(i).stringValue = lines[i];
            settings.FindProperty("message").stringValue =
                "...survivor, this is the rescue ship. We hear your beacon loud and clear.\nStay where you are. We're coming to get you.";
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Wrap(Text label, Font font)
        {
            label.font = font;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }
}
