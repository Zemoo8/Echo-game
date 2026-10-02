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
        public static string Level2Path => $"{ScenesFolder}/Levels/{SceneNames.Level2}.unity";
        public static string Level3Path => $"{ScenesFolder}/Levels/{SceneNames.Level3}.unity";

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

        [MenuItem("ECHO/Setup/Rebuild Level 2")]
        public static void RebuildLevel2()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsureScene(Level2Path, BuildLevel2, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        [MenuItem("ECHO/Setup/Rebuild Level 3")]
        public static void RebuildLevel3()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EnsureScene(Level3Path, BuildLevel3, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        public static void EnsureAll()
        {
            EnsureScene(Level1Path, BuildLevel1, false);
            EnsureScene(Level2Path, BuildLevel2, false);
            EnsureScene(Level3Path, BuildLevel3, false);
        }

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

            Finish(level, camera, new Vector2(2.5f, 1f), new Rect(-4f, Bottom, 167f, 16f),
                   "SECTOR 01", "ABANDONED LAB", 0, 3, SceneNames.Level2);
        }

        // Level 2 · Security Wing. Floor tops 0 unless noted. Left to right:
        //   a patrolling drone (jump it or dash it) · cell · checkpoint · a turret behind a low
        //   cover wall · 8-unit pit · cell · a console on a ledge that opens the shutter beyond it ·
        //   a second drone · checkpoint · a console on a floating platform that switches off the
        //   three lasers ahead (or time them) · checkpoint · cell · 8-unit pit · a cover wall (so
        //   the exit turret's bolts never cross the pit) · a turret guarding the exit door. Start with 1 energy, max 3. The route needs 3 dashes (two pits, the
        //   exit) and offers 3 cells, so there is one charge to spare: spend it on a machine, or keep it.
        static void BuildLevel2()
        {
            var kit = EchoArtFactory.LoadKit(Biome.Security);
            var camera = CreateCamera(kit.voidColor);
            DisableCameraRoll(camera.gameObject.AddComponent<CameraFeedback>());
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(kit.ambientIntensity, kit.ambientColor);
            var sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();
            SetEnum(sceneAudio, "music", (int)MusicId.Security);

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var pickups = Child("Pickups", level.transform);
            var barriers = Child("Barriers", level.transform);
            var hazards = Child("Hazards", level.transform);
            var machines = Child("Machines", level.transform);
            var devices = Child("Consoles", level.transform);
            var checkpoints = Child("Checkpoints", level.transform);
            var hints = Child("Hints", level.transform);
            var backdrop = Child("Backdrop", level.transform);
            var props = Child("Props", level.transform);
            var lights = Child("Lights", level.transform);

            // --- ground ----------------------------------------------------------------------
            // The turret stands 8 units before the first pit so jumping it lands on the floor,
            // with room to run up to the pit.
            KitBlock(geometry, kit, "Wall_Left", -4f, Bottom, -3f, WallTop);
            KitGround(geometry, kit, "Floor_Entry", -3f, 46f, 0f);
            KitBlock(geometry, kit, "Cover", 31f, 0f, 32f, 1.6f);
            KitGround(geometry, kit, "Floor_Shutter", 54f, 120f, 0f);          // pit 46..54
            KitBlock(geometry, kit, "Console_Ledge", 62f, 0f, 68f, 2f);
            KitBlock(geometry, kit, "Doorway_Shutter", 71f, CeilingY, 73f, WallTop);
            KitBlock(geometry, kit, "Step", 90f, 0f, 92f, 1.2f);
            Platform(geometry, "Console_Platform", 92f, 98f, 2.6f);
            KitBlock(geometry, kit, "Doorway_Laser_1", 101.4f, CeilingY, 102.6f, WallTop);
            KitBlock(geometry, kit, "Doorway_Laser_2", 105.4f, CeilingY, 106.6f, WallTop);
            KitBlock(geometry, kit, "Doorway_Laser_3", 109.4f, CeilingY, 110.6f, WallTop);
            KitGround(geometry, kit, "Floor_Exit", 128f, 148f, 0f);            // pit 120..128
            KitBlock(geometry, kit, "Cover_Exit", 130f, 0f, 131f, 1.6f);       // stops the exit turret's bolts before the pit
            KitBlock(geometry, kit, "Doorway_Exit", 139f, CeilingY, 145f, WallTop);
            KitBlock(geometry, kit, "Wall_Right", 148f, Bottom, 149f, WallTop);

            EchoLevelArt.Pit(level.transform, 46f, 54f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 120f, 128f, 0f, Bottom);
            KillZone(level.transform, 74f, -9f, 180f);

            // --- the run ---------------------------------------------------------------------
            Pickup(pickups, 27f, 1f);
            Pickup(pickups, 57f, 1f);
            Pickup(pickups, 117f, 1f);
            Barrier("ExitDoor", barriers, 141.7f, 142.3f, true);

            DroneOn(machines, 14f, 24f, 1.4f, 2.5f, 0f);
            DroneOn(machines, 76f, 86f, 1.4f, 3f, 0.5f);
            TurretOn(machines, 38f, 0f, -1, 2f, 0f);
            TurretOn(machines, 136f, 0f, -1, 1.6f, 0.5f);

            var shutter = Shutter(hazards, 72f, 0f, CeilingY);
            var laser1 = Laser(hazards, 102f, 0f, CeilingY, 1.2f, 1f, 0f);
            var laser2 = Laser(hazards, 106f, 0f, CeilingY, 1.2f, 1f, 0.4f);
            var laser3 = Laser(hazards, 110f, 0f, CeilingY, 1.2f, 1f, 0.8f);
            ConsoleAt(devices, 65f, 2f, shutter);
            ConsoleAt(devices, 96.5f, 2.6f, laser1, laser2, laser3);

            Checkpoint(checkpoints, 29f, 0f);
            Checkpoint(checkpoints, 88.5f, 0f);
            Checkpoint(checkpoints, 114f, 0f);

            Hint(hints, HintId.Drones, 5f, 13f, 0f);
            Hint(hints, HintId.Turrets, 28f, 31f, 0f);
            Hint(hints, HintId.Consoles, 58f, 62f, 0f);
            Hint(hints, HintId.Lasers, 98.5f, 101.4f, 0f);
            Hint(hints, HintId.KeepCharge, 114f, 120f, 0f);
            Hint(hints, HintId.OutOfEnergy, 114f, 120f, 0f, onlyWhenEmpty: true);
            Hint(hints, HintId.OutOfEnergy, 128f, 141.5f, 0f, onlyWhenEmpty: true);

            // --- dressing --------------------------------------------------------------------
            EchoLevelArt.SecurityHall(kit, backdrop, -4f, 149f, -0.25f);
            EchoLevelArt.Prop(props, Path(kit.props[0]), 9f, 0f);    // a sealed door
            EchoLevelArt.Prop(props, Path(kit.props[1]), 20f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[2]), 42f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[3]), 81f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[1]), 115.5f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[0]), 131f, 0f);
            foreach (float x in new[] { 4f, 18f, 33f, 59f, 78f, 94f, 115f, 132f, 146f })
                EchoLevelArt.CeilingLamp(lights, x, 4.6f, x == 78f || x == 132f, kit.lampColor);

            Finish(level, camera, new Vector2(2f, 1f), new Rect(-4f, Bottom, 153f, 16f),
                   "SECTOR 02", "SECURITY WING", 1, 3, SceneNames.Level3);
        }

        // Level 3 · Energy Core. Floor tops 0 unless noted. Left to right:
        //   a bridge of cracked plates over the abyss · checkpoint · a core lock that needs 2 held
        //   charges, with a drone between its two cells (jump it: a dash spends a charge the lock
        //   needs) · 8-unit pit · checkpoint · two lasers · a rising bridge of plates · checkpoint ·
        //   cell · 8-unit pit · three cells (one on a ledge) · a core lock that needs a full meter ·
        //   8-unit pit · cover · a turret guarding the exit door. Start with 0 energy, max 3.
        // Every lock can be filled from the checkpoint before it, so returning there (R) never
        // leaves the player stuck.
        static void BuildLevel3()
        {
            var kit = EchoArtFactory.LoadKit(Biome.Core);
            var camera = CreateCamera(kit.voidColor);
            DisableCameraRoll(camera.gameObject.AddComponent<CameraFeedback>());
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(kit.ambientIntensity, kit.ambientColor);
            var sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();
            SetEnum(sceneAudio, "music", (int)MusicId.Core);

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var plates = Child("Plates", level.transform);
            var pickups = Child("Pickups", level.transform);
            var barriers = Child("Barriers", level.transform);
            var hazards = Child("Hazards", level.transform);
            var machines = Child("Machines", level.transform);
            var locks = Child("Locks", level.transform);
            var checkpoints = Child("Checkpoints", level.transform);
            var hints = Child("Hints", level.transform);
            var backdrop = Child("Backdrop", level.transform);
            var props = Child("Props", level.transform);
            var lights = Child("Lights", level.transform);

            // --- ground ----------------------------------------------------------------------
            KitBlock(geometry, kit, "Wall_Left", -4f, Bottom, -3f, WallTop);
            KitGround(geometry, kit, "Floor_Entry", -3f, 18f, 0f);
            KitGround(geometry, kit, "Floor_Lock1", 39f, 70f, 0f);             // abyss 18..39
            KitBlock(geometry, kit, "Doorway_Lock1", 63.4f, CeilingY, 64.6f, WallTop);
            KitGround(geometry, kit, "Floor_Lasers", 78f, 112f, 0f);           // pit 70..78
            KitBlock(geometry, kit, "Doorway_Laser_1", 89.4f, CeilingY, 90.6f, WallTop);
            KitBlock(geometry, kit, "Doorway_Laser_2", 94.4f, CeilingY, 95.6f, WallTop);
            KitGround(geometry, kit, "Floor_Reactor", 140f, 150f, 0f);         // abyss 112..140
            KitGround(geometry, kit, "Floor_Lock2", 158f, 179f, 0f);           // pit 150..158
            KitBlock(geometry, kit, "Ledge", 164f, 0f, 167f, 1.6f);
            KitBlock(geometry, kit, "Doorway_Lock2", 174.4f, CeilingY, 175.6f, WallTop);
            KitGround(geometry, kit, "Floor_Exit", 187f, 208f, 0f);            // pit 179..187
            KitBlock(geometry, kit, "Cover_Exit", 190f, 0f, 191f, 1.6f);
            KitBlock(geometry, kit, "Doorway_Exit", 199f, CeilingY, 205f, WallTop);
            KitBlock(geometry, kit, "Wall_Right", 208f, Bottom, 209f, WallTop);

            // Cracked plates, 3 wide with 3-unit gaps: an easy hop each, no time to stand still.
            UnstablePlate(plates, 21f, 24f, 0f);
            UnstablePlate(plates, 27f, 30f, 0f);
            UnstablePlate(plates, 33f, 36f, 0f);
            UnstablePlate(plates, 115f, 118f, 0.8f);
            UnstablePlate(plates, 121f, 124f, 1.6f);
            UnstablePlate(plates, 127f, 130f, 1.6f);
            UnstablePlate(plates, 133f, 136f, 0.8f);

            EchoLevelArt.Pit(level.transform, 18f, 39f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 70f, 78f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 112f, 140f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 150f, 158f, 0f, Bottom);
            EchoLevelArt.Pit(level.transform, 179f, 187f, 0f, Bottom);
            KillZone(level.transform, 102f, -9f, 230f);

            // --- the run ---------------------------------------------------------------------
            Pickup(pickups, 9f, 1f);
            Pickup(pickups, 28.5f, 2.4f);      // over the middle plate, taken mid-jump
            Pickup(pickups, 46f, 1f);
            Pickup(pickups, 60f, 1f);
            Pickup(pickups, 100f, 1f);
            Pickup(pickups, 146f, 1f);         // so the pit after checkpoint 3 can always be crossed
            Pickup(pickups, 161f, 1f);
            Pickup(pickups, 165.5f, 2.6f);
            Pickup(pickups, 170f, 1f);
            Barrier("ExitDoor", barriers, 201.7f, 202.3f, true);

            DroneOn(machines, 49f, 57f, 1.4f, 2.5f, 0f);
            TurretOn(machines, 196f, 0f, -1, 1.8f, 0.3f);

            Laser(hazards, 90f, 0f, CeilingY, 1.4f, 1.2f, 0f);
            Laser(hazards, 95f, 0f, CeilingY, 1.4f, 1.2f, 0.6f);
            LockAt(locks, 64f, 0f, CeilingY, 2);
            LockAt(locks, 175f, 0f, CeilingY, 3);

            Checkpoint(checkpoints, 42f, 0f);
            Checkpoint(checkpoints, 81f, 0f);
            Checkpoint(checkpoints, 143f, 0f);

            Hint(hints, HintId.Plates, 11f, 18f, 0f);
            Hint(hints, HintId.ChargeLocks, 58f, 63.4f, 0f);
            Hint(hints, HintId.KeepCharge, 176f, 179f, 0f);
            Hint(hints, HintId.OutOfEnergy, 187f, 201.5f, 0f, onlyWhenEmpty: true);

            // --- dressing --------------------------------------------------------------------
            EchoLevelArt.CoreMachinery(kit, backdrop, -4f, 209f, -0.25f);
            EchoLevelArt.Reactor(backdrop, new Vector2(125.5f, 6.2f), false);
            EchoLevelArt.Prop(props, Path(kit.props[0]), 6f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[1]), 13.5f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[2]), 52f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[0]), 84f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[1]), 105f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[2]), 147.5f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[0]), 171.5f, 0f);
            EchoLevelArt.Prop(props, Path(kit.props[1]), 193.5f, 0f);
            foreach (float x in new[] { 4f, 44f, 58f, 84f, 100f, 145f, 162f, 171f, 192f, 206f })
                EchoLevelArt.CeilingLamp(lights, x, 4.6f, x == 58f || x == 171f, kit.lampColor);

            Finish(level, camera, new Vector2(2f, 1f), new Rect(-4f, Bottom, 213f, 16f),
                   "SECTOR 03", "ENERGY CORE", 0, 3, SceneNames.Boss);
        }

        /// <summary>A cracked core plate over the abyss that gives way a moment after it is stood on.</summary>
        static void UnstablePlate(Transform parent, float xMin, float xMax, float top)
        {
            const float thickness = 0.5f;
            float width = xMax - xMin;
            var go = new GameObject("UnstablePlate") { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, top - thickness * 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(width, thickness);

            var calm = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Core/UnstablePlatform.png");
            var visual = new GameObject("Plate");
            visual.transform.SetParent(go.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = calm;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width, thickness);
            renderer.sortingLayerName = "Level";

            var plate = go.AddComponent<CollapsingPlatform>();
            EchoPrefabFactory.SetReference(plate, "plate", renderer);
            EchoPrefabFactory.SetReference(plate, "calmSprite", calm);
            EchoPrefabFactory.SetReference(plate, "hotSprite", EchoArtFactory.LoadSprite("Assets/Art/Pixel/Core/UnstableGlowPlatform.png"));
        }

        /// <summary>
        /// A core lock from the floor to the ceiling: a steel-blue field between two emitters, and a
        /// panel of charge lights on the approach side (one per charge it needs).
        /// </summary>
        static ChargeLock LockAt(Transform parent, float x, float floorY, float ceilingY, int required)
        {
            float height = ceilingY - floorY;
            var go = new GameObject($"ChargeLock_{required}") { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY + height * 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, height);

            var frames = EchoArtFactory.LoadSprites("Assets/Art/Pixel/Energy/LockField_{0:00}.png", 4);
            var field = new GameObject("Field");
            field.transform.SetParent(go.transform, false);
            var fieldRenderer = field.AddComponent<SpriteRenderer>();
            fieldRenderer.sprite = frames[0];
            fieldRenderer.drawMode = SpriteDrawMode.Tiled;
            fieldRenderer.size = new Vector2(frames[0].bounds.size.x, height);
            fieldRenderer.sortingLayerName = "Interactables";
            Frames(field.AddComponent<SpriteFlipbook>(), frames, 8f);
            var glow = EchoLevelArt.AddPointLight(field, new Color(0.55f, 0.63f, 1f), 0.8f, 2.5f);

            var emitter = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Energy/LockEmitter.png");
            float emitterHalf = emitter.bounds.size.y * 0.5f;
            foreach (bool top in new[] { true, false })
            {
                var cap = new GameObject(top ? "Emitter_Top" : "Emitter_Bottom");
                cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(0f, top ? height * 0.5f - emitterHalf : -height * 0.5f + emitterHalf, 0f);
                cap.transform.localScale = new Vector3(1f, top ? 1f : -1f, 1f);
                var capRenderer = cap.AddComponent<SpriteRenderer>();
                capRenderer.sprite = emitter;
                capRenderer.sortingLayerName = "Interactables";
                capRenderer.sortingOrder = 1;
            }

            // The light panel stands on the approach (left) side at eye level, filling from the bottom.
            const float pixel = 1f / 32f;
            var panelSprite = EchoArtFactory.LoadSprite($"Assets/Art/Pixel/Gameplay/LockPanel_{required}.png");
            float panelHeight = panelSprite.bounds.size.y;
            var panel = new GameObject("LightPanel");
            panel.transform.SetParent(go.transform, false);
            panel.transform.localPosition = new Vector3(-0.62f, -height * 0.5f + 1.5f + panelHeight * 0.5f, 0f);
            var panelRenderer = panel.AddComponent<SpriteRenderer>();
            panelRenderer.sprite = panelSprite;
            panelRenderer.sortingLayerName = "Interactables";
            panelRenderer.sortingOrder = 2;

            var off = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/LockLight_Off.png");
            var renderers = new SpriteRenderer[required];
            for (int k = 0; k < required; k++)
            {
                int slotFromTop = required - 1 - k;
                var light = new GameObject($"Light_{k + 1}");
                light.transform.SetParent(panel.transform, false);
                light.transform.localPosition = new Vector3(0f, panelHeight * 0.5f - (6f + 10f * slotFromTop) * pixel, 0f);
                renderers[k] = light.AddComponent<SpriteRenderer>();
                renderers[k].sprite = off;
                renderers[k].sortingLayerName = "Interactables";
                renderers[k].sortingOrder = 3;
            }

            var chargeLock = go.AddComponent<ChargeLock>();
            EchoPrefabFactory.SetReference(chargeLock, "field", fieldRenderer);
            EchoPrefabFactory.SetReference(chargeLock, "lightOff", off);
            EchoPrefabFactory.SetReference(chargeLock, "lightOn", EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/LockLight_On.png"));
            EchoPrefabFactory.SetReference(chargeLock, "glow", glow);
            var settings = new SerializedObject(chargeLock);
            settings.FindProperty("required").intValue = required;
            var array = settings.FindProperty("lights");
            array.arraySize = required;
            for (int k = 0; k < required; k++)
                array.GetArrayElementAtIndex(k).objectReferenceValue = renderers[k];
            settings.ApplyModifiedPropertiesWithoutUndo();
            return chargeLock;
        }

        static void Finish(GameObject level, Camera camera, Vector2 spawnAt, Rect bounds,
                           string sector, string title, int startingEnergy, int maxEnergy, string nextScene)
        {
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(level.transform, false);
            spawn.transform.position = spawnAt;

            var player = (GameObject)PrefabUtility.InstantiatePrefab(EchoPrefabFactory.EnsurePlayerPrefab());
            player.transform.position = spawn.transform.position;

            var follow = camera.gameObject.AddComponent<CameraFollow>();
            EchoPrefabFactory.SetReference(follow, "target", player.transform);
            var followSettings = new SerializedObject(follow);
            followSettings.FindProperty("bounds").rectValue = bounds;
            followSettings.ApplyModifiedPropertiesWithoutUndo();

            var controller = level.AddComponent<LevelController>();
            EchoPrefabFactory.SetReference(controller, "player", player.GetComponent<PlayerController>());
            EchoPrefabFactory.SetReference(controller, "spawnPoint", spawn.transform);
            EchoPrefabFactory.SetReference(controller, "cameraFollow", follow);
            var settings = new SerializedObject(controller);
            settings.FindProperty("sectorLabel").stringValue = sector;
            settings.FindProperty("levelTitle").stringValue = title;
            settings.FindProperty("startingEnergy").intValue = startingEnergy;
            settings.FindProperty("maxEnergy").intValue = maxEnergy;
            settings.FindProperty("nextScene").stringValue = nextScene;
            settings.FindProperty("saveProgress").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- kit-dressed ground ------------------------------------------------------------------

        static void KitBlock(Transform parent, EnvironmentKit kit, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            var size = new Vector2(xMax - xMin, yMax - yMin);
            EchoLevelArt.DressBlock(go, size, kit);
            go.AddComponent<BoxCollider2D>().size = size;
        }

        static void KitGround(Transform parent, EnvironmentKit kit, string name, float xMin, float xMax, float top)
        {
            KitBlock(parent, kit, name, xMin, Bottom, xMax, top);
            EchoLevelArt.DepthShade(parent, xMin, xMax, top - 0.6f);
        }

        /// <summary>A thin steel girder the player can stand on and walk under.</summary>
        static void Platform(Transform parent, string name, float xMin, float xMax, float top)
        {
            const float thickness = 0.5f;
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, top - thickness * 0.5f, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Security/SecurityPlatform.png");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(xMax - xMin, thickness);
            renderer.sortingLayerName = "Level";
            go.AddComponent<BoxCollider2D>().size = new Vector2(xMax - xMin, thickness);
        }

        // --- machines and devices ----------------------------------------------------------------

        static void DroneOn(Transform parent, float xA, float xB, float y, float speed, float phase)
        {
            var go = new GameObject("Drone") { layer = LayerMask.NameToLayer(Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(xA, y, 0f);
            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.useFullKinematicContacts = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;

            var frames = NumberedFromOne("Assets/ThirdParty/Ansimuz/WarpedCity/Enemies/drone/drone_{0:00}.png", 4);
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = "Characters";
            Frames(body.AddComponent<SpriteFlipbook>(), frames, 10f);
            EchoLevelArt.AddPointLight(body, new Color(1f, 0.3f, 0.25f), 0.5f, 1.4f);

            var drone = go.AddComponent<Drone>();
            EchoPrefabFactory.SetReference(drone, "body", body);
            EchoPrefabFactory.SetReference(drone, "sprite", renderer);
            var settings = new SerializedObject(drone);
            settings.FindProperty("pointA").vector2Value = new Vector2(xA, y);
            settings.FindProperty("pointB").vector2Value = new Vector2(xB, y);
            settings.FindProperty("speed").floatValue = speed;
            settings.FindProperty("phase").floatValue = phase;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TurretOn(Transform parent, float x, float floorY, int facing, float interval, float offset)
        {
            var frames = NumberedFromOne("Assets/ThirdParty/Ansimuz/WarpedCity/Enemies/turret/turret_{0:00}.png", 6);
            float half = frames[0].bounds.size.y * 0.5f;
            var go = new GameObject("Turret") { layer = LayerMask.NameToLayer(Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY + half, 0f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.7f, half * 2f);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = "Characters";
            EchoLevelArt.AddPointLight(body, new Color(1f, 0.45f, 0.2f), 0.4f, 1.2f);

            var turret = go.AddComponent<Turret>();
            EchoPrefabFactory.SetReference(turret, "body", body);
            EchoPrefabFactory.SetReference(turret, "sprite", renderer);
            EchoPrefabFactory.SetReference(turret, "boltSprite", EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/TurretBolt.png"));
            var settings = new SerializedObject(turret);
            settings.FindProperty("facing").intValue = facing;
            settings.FindProperty("interval").floatValue = interval;
            settings.FindProperty("offset").floatValue = offset;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A security shutter from the floor to the ceiling, opened by a console.</summary>
        static SecurityDoor Shutter(Transform parent, float x, float floorY, float ceilingY)
        {
            float height = ceilingY - floorY;
            var go = new GameObject("SecurityDoor") { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY, 0f);
            var blocker = go.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(0.8f, height);
            blocker.offset = new Vector2(0f, height * 0.5f);

            var slats = new GameObject("Shutter");
            slats.transform.SetParent(go.transform, false);
            slats.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            var renderer = slats.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/Shutter.png");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(0.75f, height);
            renderer.sortingLayerName = "Interactables";

            var door = go.AddComponent<SecurityDoor>();
            EchoPrefabFactory.SetReference(door, "shutter", renderer);
            return door;
        }

        static void ConsoleAt(Transform parent, float x, float floorY, params MonoBehaviour[] targets)
        {
            var go = new GameObject("Console");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY, 0f);
            Trigger(go, new Vector2(1f, 2f), new Vector2(0f, 1f));

            var locked = EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/Console_Locked.png");
            var screen = new GameObject("Screen");
            screen.transform.SetParent(go.transform, false);
            screen.transform.localPosition = new Vector3(0f, locked.bounds.size.y * 0.5f, 0f);
            var renderer = screen.AddComponent<SpriteRenderer>();
            renderer.sprite = locked;
            renderer.sortingLayerName = "Interactables";
            var light = EchoLevelArt.AddPointLight(screen, new Color(1f, 0.3f, 0.25f), 0.7f, 1.8f);

            var console = go.AddComponent<ControlConsole>();
            EchoPrefabFactory.SetReference(console, "screen", renderer);
            EchoPrefabFactory.SetReference(console, "lockedSprite", locked);
            EchoPrefabFactory.SetReference(console, "usedSprite", EchoArtFactory.LoadSprite("Assets/Art/Pixel/Gameplay/Console_Open.png"));
            EchoPrefabFactory.SetReference(console, "screenLight", light);
            var settings = new SerializedObject(console);
            var array = settings.FindProperty("targets");
            array.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite[] NumberedFromOne(string format, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = EchoArtFactory.LoadSprite(string.Format(format, i + 1));
            return frames;
        }

        static void Frames(SpriteFlipbook flipbook, Sprite[] frames, float fps)
        {
            var settings = new SerializedObject(flipbook);
            var array = settings.FindProperty("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            settings.FindProperty("framesPerSecond").floatValue = fps;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static string Path(Sprite sprite) => AssetDatabase.GetAssetPath(sprite);

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

        static LaserGate Laser(Transform parent, float x, float floorY, float ceilingY, float onTime, float offTime, float offset)
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
            return gate;
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
