using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Echo.EditorTools.EchoSceneFactory;

namespace Echo.EditorTools
{
    /// <summary>
    /// Builds ECHO's story levels in code (see Docs/LEVEL_DESIGN.md for the intent of every
    /// section). Spacing reuses what the movement sandbox proved: 4-unit gaps for a running
    /// jump, 8-unit gaps for a jump plus an air dash, steps of 1, 1.2 and 0.8 units.
    /// Rebuilt from scratch each time; never hand-edit the scenes.
    /// </summary>
    public static class EchoLevelFactory
    {
        public static string Level1Path => $"{ScenesFolder}/Levels/{SceneNames.Level1}.unity";

        const float WallTop = 14f;
        const float Bottom = -4f;
        const float CeilingY = 5f;

        [MenuItem("ECHO/Setup/Rebuild Level 1")]
        public static void RebuildLevel1()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsureScene(Level1Path, BuildLevel1, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        public static void EnsureAll() => EnsureScene(Level1Path, BuildLevel1, false);

        // Level 1 · Awakening (abandoned lab). Floor tops: 0 unless noted. Left to right:
        //   wake by the cryo pod (move) · stairs up (jump; the last stair is 6 deep so a full jump
        //   from the one below can't overshoot into the pit) · 4-unit pit at height 3 · drop down ·
        //   energy cell then a cyan barrier in a doorway (energy, dash) · checkpoint · cell, then an
        //   8-unit pit (air dash) · two cells · checkpoint · two timed lasers · checkpoint · cell ·
        //   final 8-unit pit · exit door. Start with no energy; max 3. The route needs 4 dashes and
        //   offers 5 cells, so arriving at the exit with a charge is easy if the cells are taken.
        static void BuildLevel1()
        {
            var lab = EchoArtFactory.LoadKit(Biome.Lab);
            var camera = CreateCamera(lab.voidColor);
            DisableCameraRoll(camera.gameObject.AddComponent<CameraFeedback>());
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(lab.ambientIntensity, lab.ambientColor);
            var sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();
            SetEnum(sceneAudio, "music", (int)MusicId.Lab);

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var pickups = Child("Pickups", level.transform);
            var barriers = Child("Barriers", level.transform);
            var hazards = Child("Hazards", level.transform);
            var checkpoints = Child("Checkpoints", level.transform);
            var hints = Child("Hints", level.transform);
            var backdrop = Child("Backdrop", level.transform);
            var props = Child("Props", level.transform);
            var lights = Child("Lights", level.transform);

            // --- ground ----------------------------------------------------------------------
            Block("Wall_Left", geometry, -4f, Bottom, -3f, WallTop);
            Ground(geometry, "Floor_Wake", -3f, 22f, 0f);
            Ground(geometry, "Stair_1", 22f, 26f, 1f);
            Ground(geometry, "Stair_2", 26f, 30f, 2.2f);
            Ground(geometry, "Stair_3", 30f, 36f, 3f);
            Ground(geometry, "Ledge", 40f, 48f, 3f);           // pit 36..40 at height 3
            Ground(geometry, "Floor_Barrier", 48f, 76f, 0f);
            Block("Doorway_Barrier", geometry, 57f, CeilingY, 63f, WallTop);
            Ground(geometry, "Floor_Lasers", 84f, 128f, 0f);   // pit 76..84
            Block("Doorway_Laser_1", geometry, 103.4f, CeilingY, 104.6f, WallTop);
            Block("Doorway_Laser_2", geometry, 110.4f, CeilingY, 111.6f, WallTop);
            Ground(geometry, "Floor_Exit", 136f, 162f, 0f);    // pit 128..136
            Block("Doorway_Exit", geometry, 151f, CeilingY, 157f, WallTop);
            Block("Wall_Right", geometry, 162f, Bottom, 163f, WallTop);

            EchoLevelArt.Pit(level.transform, 36f, 40f, 3f, Bottom);
            EchoLevelArt.Pit(level.transform, 76f, 84f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 128f, 136f, 0f, Bottom);
            KillZone(level.transform, 80f, -9f, 200f);

            // --- energy, barriers, hazards, checkpoints --------------------------------------
            Pickup(pickups, 50f, 1f);
            Pickup(pickups, 72f, 1f);
            Pickup(pickups, 88f, 1f);
            Pickup(pickups, 92f, 1f);
            Pickup(pickups, 122f, 1f);

            Barrier("Barrier", barriers, 59.7f, 60.3f, false);
            Barrier("ExitDoor", barriers, 153.7f, 154.3f, true);

            Laser(hazards, 104f, 0f, CeilingY, 1.6f, 1.4f, 0f);
            Laser(hazards, 111f, 0f, CeilingY, 1.6f, 1.4f, 1f);

            Checkpoint(checkpoints, 67f, 0f);
            Checkpoint(checkpoints, 97f, 0f);
            Checkpoint(checkpoints, 118f, 0f);

            Hint(hints, HintId.Move, -3f, 12f, 0f);
            Hint(hints, HintId.Jump, 16f, 24f, 0f);
            Hint(hints, HintId.Energy, 48.5f, 53f, 0f);
            Hint(hints, HintId.Dash, 53f, 59.5f, 0f);
            Hint(hints, HintId.AirDash, 69f, 76f, 0f);
            Hint(hints, HintId.Lasers, 98f, 103.5f, 0f);
            Hint(hints, HintId.KeepCharge, 120f, 128f, 0f);
            Hint(hints, HintId.OutOfEnergy, 120f, 128f, 0f, onlyWhenEmpty: true);
            Hint(hints, HintId.OutOfEnergy, 136f, 153.5f, 0f, onlyWhenEmpty: true);

            // --- dressing --------------------------------------------------------------------
            const float corridorFloor = -0.25f;
            EchoLevelArt.FarCity(backdrop, -4f, 163f, corridorFloor);
            EchoLevelArt.Corridor(backdrop, -4f, 154f, corridorFloor);
            EchoLevelArt.UpperMachinery(backdrop, -4f, 163f, corridorFloor + 5.5f);

            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), 0.6f, 0f);   // where she wakes
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), 6.5f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("ServerCabinets"), 13f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("SmallTerminal"), 44f, 3f);
            EchoLevelArt.Prop(props, EchoLevelArt.WarpedProp("ScreenPod"), 52.5f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("ServerCabinets"), 70f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), 90f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("SmallTerminal"), 115f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.WarpedProp("ScreenPod"), 124.5f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("ServerCabinets"), 141f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), 147f, 0f);

            foreach (float x in new[] { 3f, 12f, 20f, 50f, 68f, 79f, 89f, 99f, 117f, 125f, 140f, 148f, 159f })
                EchoLevelArt.CeilingLamp(lights, x, 4.6f, x == 68f || x == 125f, lab.lampColor);

            // --- player, camera, level -------------------------------------------------------
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(level.transform, false);
            spawn.transform.position = new Vector3(2.5f, 1f, 0f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(EchoPrefabFactory.EnsurePlayerPrefab());
            player.transform.position = spawn.transform.position;

            var follow = camera.gameObject.AddComponent<CameraFollow>();
            EchoPrefabFactory.SetReference(follow, "target", player.transform);
            var followSettings = new SerializedObject(follow);
            followSettings.FindProperty("bounds").rectValue = new Rect(-4f, Bottom, 167f, 16f);
            followSettings.ApplyModifiedPropertiesWithoutUndo();

            var controller = level.AddComponent<LevelController>();
            EchoPrefabFactory.SetReference(controller, "player", player.GetComponent<PlayerController>());
            EchoPrefabFactory.SetReference(controller, "spawnPoint", spawn.transform);
            EchoPrefabFactory.SetReference(controller, "cameraFollow", follow);
            var settings = new SerializedObject(controller);
            settings.FindProperty("sectorLabel").stringValue = "SECTOR 01";
            settings.FindProperty("levelTitle").stringValue = "ABANDONED LAB";
            settings.FindProperty("startingEnergy").intValue = 0;
            settings.FindProperty("maxEnergy").intValue = 3;
            settings.FindProperty("nextScene").stringValue = SceneNames.Level2;
            settings.FindProperty("saveProgress").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- building blocks ---------------------------------------------------------------------

        /// <summary>Walkable ground from xMin to xMax with its top at <paramref name="top"/>.</summary>
        static void Ground(Transform parent, string name, float xMin, float xMax, float top)
        {
            Block(name, parent, xMin, Bottom, xMax, top);
            EchoLevelArt.DepthShade(parent, xMin, xMax, top - 0.6f);
        }

        static void KillZone(Transform parent, float x, float y, float width)
        {
            var go = new GameObject("KillZone") { layer = LayerMask.NameToLayer(Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(width, 4f);
            go.AddComponent<Gameplay.KillZone>();
        }

        // Player-only triggers live on the Pickup layer, which only collides with the player.
        static BoxCollider2D Trigger(GameObject go, Vector2 size, Vector2 offset)
        {
            go.layer = LayerMask.NameToLayer(Layers.Pickup);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = size;
            trigger.offset = offset;
            return trigger;
        }

        static void Checkpoint(Transform parent, float x, float floorY)
        {
            var go = new GameObject("Checkpoint");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY, 0f);
            Trigger(go, new Vector2(1.2f, 3f), new Vector2(0f, 1.5f));

            var offSprite = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/Checkpoint_Off.png");
            var onSprite = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/Checkpoint_On.png");
            var lamp = new GameObject("Beacon");
            lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(0f, offSprite.bounds.size.y * 0.5f, 0f);
            var renderer = lamp.AddComponent<SpriteRenderer>();
            renderer.sprite = offSprite;
            renderer.sortingLayerName = "Interactables";

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, offSprite.bounds.size.y - 0.15f, 0f);
            var light = EchoLevelArt.AddPointLight(lightObject, new Color(0.9f, 0.95f, 1f), 0.9f, 2.4f);

            var respawn = new GameObject("RespawnPoint");
            respawn.transform.SetParent(go.transform, false);
            respawn.transform.localPosition = new Vector3(0f, 1f, 0f);

            var checkpoint = go.AddComponent<Gameplay.Checkpoint>();
            EchoPrefabFactory.SetReference(checkpoint, "lamp", renderer);
            EchoPrefabFactory.SetReference(checkpoint, "offSprite", offSprite);
            EchoPrefabFactory.SetReference(checkpoint, "onSprite", onSprite);
            EchoPrefabFactory.SetReference(checkpoint, "lampLight", light);
            EchoPrefabFactory.SetReference(checkpoint, "respawnPoint", respawn.transform);
        }

        static void Laser(Transform parent, float x, float floorY, float ceilingY, float onTime, float offTime, float offset)
        {
            float height = ceilingY - floorY;
            var go = new GameObject("Laser") { layer = LayerMask.NameToLayer(Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY + height * 0.5f, 0f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.3f, height);

            var frames = EchoArtFactory.LoadSprites("Assets/Art/Pixel/Gameplay/LaserBeam_{0:00}.png", 3);
            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            var beamRenderer = beam.AddComponent<SpriteRenderer>();
            beamRenderer.sprite = frames[0];
            beamRenderer.drawMode = SpriteDrawMode.Tiled;
            beamRenderer.size = new Vector2(frames[0].bounds.size.x, height - 0.4f);
            beamRenderer.sortingLayerName = "Hazards";
            var flipbook = beam.AddComponent<SpriteFlipbook>();
            var flipSettings = new SerializedObject(flipbook);
            var array = flipSettings.FindProperty("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            flipSettings.FindProperty("framesPerSecond").floatValue = 14f;
            flipSettings.ApplyModifiedPropertiesWithoutUndo();
            var beamLight = EchoLevelArt.AddPointLight(beam, new Color(1f, 0.25f, 0.3f), 0.9f, 2.2f);

            var emitter = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/LaserEmitter.png");
            float half = emitter.bounds.size.y * 0.5f;
            foreach (bool top in new[] { true, false })
            {
                var cap = new GameObject(top ? "Emitter_Top" : "Emitter_Bottom");
                cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(0f, top ? height * 0.5f - half : -height * 0.5f + half, 0f);
                cap.transform.localScale = new Vector3(1f, top ? 1f : -1f, 1f);
                var capRenderer = cap.AddComponent<SpriteRenderer>();
                capRenderer.sprite = emitter;
                capRenderer.sortingLayerName = "Hazards";
                capRenderer.sortingOrder = 1;
            }

            var gate = go.AddComponent<LaserGate>();
            EchoPrefabFactory.SetReference(gate, "beam", beamRenderer);
            EchoPrefabFactory.SetReference(gate, "beamLight", beamLight);
            var gateSettings = new SerializedObject(gate);
            gateSettings.FindProperty("onTime").floatValue = onTime;
            gateSettings.FindProperty("offTime").floatValue = offTime;
            gateSettings.FindProperty("offset").floatValue = offset;
            gateSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Hint(Transform parent, HintId hint, float xMin, float xMax, float floorY, bool onlyWhenEmpty = false)
        {
            var go = new GameObject("Hint_" + hint);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, floorY, 0f);
            Trigger(go, new Vector2(xMax - xMin, 8f), new Vector2(0f, 4f));
            var zone = go.AddComponent<HintZone>();
            SetEnum(zone, "hint", (int)hint);
            var settings = new SerializedObject(zone);
            settings.FindProperty("onlyWhenEmpty").boolValue = onlyWhenEmpty;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int value)
        {
            var settings = new SerializedObject(target);
            settings.FindProperty(field).enumValueIndex = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
