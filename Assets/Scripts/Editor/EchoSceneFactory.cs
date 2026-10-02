using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.UI;
using Echo.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.EditorTools
{
    /// <summary>
    /// Creates ECHO's code-generated scenes. Existing scenes are never overwritten unless a
    /// rebuild is asked for explicitly (only safe for scenes nobody has hand-edited).
    /// </summary>
    public static class EchoSceneFactory
    {
        public const string ScenesFolder = "Assets/Scenes";
        public static string BootPath => $"{ScenesFolder}/{SceneNames.Boot}.unity";
        public static string MainMenuPath => $"{ScenesFolder}/{SceneNames.MainMenu}.unity";
        public static string SandboxPath => $"{ScenesFolder}/Dev/{SceneNames.DevSandbox}.unity";

        public static void EnsureAll()
        {
            EnsureScene(BootPath, BuildBoot, false);
            EnsureScene(MainMenuPath, BuildMainMenu, false);
            EnsureScene(SandboxPath, BuildSandbox, false);
        }

        [MenuItem("ECHO/Setup/Rebuild Movement Sandbox")]
        public static void RebuildSandbox()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureScene(SandboxPath, BuildSandbox, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        /// <summary>Gives an existing scene its music (adds a SceneAudio object if it has none).</summary>
        public static void EnsureSceneAudio(string path, MusicId music)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                return;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var sceneAudio = Object.FindFirstObjectByType<SceneAudio>();
            if (sceneAudio == null)
                sceneAudio = new GameObject("SceneAudio").AddComponent<SceneAudio>();

            var settings = new SerializedObject(sceneAudio);
            settings.FindProperty("music").enumValueIndex = (int)music;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        static void EnsureScene(string path, System.Action populate, bool rebuild)
        {
            if (!rebuild && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return;

            EchoProjectSetup.EnsureFolderPath(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ECHO Setup] Built {path}");
        }

        static void BuildBoot()
        {
            CreateCamera();
            new GameObject("Boot").AddComponent<BootSceneController>();
        }

        static void BuildMainMenu()
        {
            CreateCamera();
            EchoLevelArt.CreateGlobalLight(1f, Color.white);

            var icon = new GameObject("PlayIcon");
            icon.transform.localScale = Vector3.one * 2.5f;
            EchoPrefabFactory.AddSprite(icon, EchoSpriteFactory.IconArrow, EchoPalette.Energy, "Default", 0);

            var menu = new GameObject("MainMenu").AddComponent<MainMenuPlaceholder>();
            EchoPrefabFactory.SetReference(menu, "pulsingIcon", icon.transform);
        }

        // Test chamber covering every M1 mechanic, left to right:
        // jump steps, a jumpable gap, a gap that needs jump + dash, a wall only a dash breaks,
        // and the exit door (also dash-only). Energy: start 2 + three pickups; the route needs three dashes.
        // Dressed as biome 1, the abandoned research lab (see Docs/VISUAL_OVERHAUL_PLAN.md).
        static void BuildSandbox()
        {
            var camera = CreateCamera(EchoLevelArt.LabVoid);
            DisableCameraRoll(camera.gameObject.AddComponent<CameraFeedback>());
            EchoLevelArt.DustMotes(camera.transform);
            EchoLevelArt.CreateGlobalLight(0.85f, new Color(0.85f, 0.93f, 1f));
            new GameObject("SceneAudio").AddComponent<SceneAudio>();

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var pickups = Child("Pickups", level.transform);
            var barriers = Child("Barriers", level.transform);
            var backdrop = Child("Backdrop", level.transform);
            var props = Child("Props", level.transform);
            var lights = Child("Lights", level.transform);

            // Floors and walls (floor tops at y = 0).
            Block("Wall_Left", geometry, -10f, -4f, -9f, 12f);
            Block("Floor_Start", geometry, -9f, -4f, 14f, 0f);
            Block("Step_1", geometry, 4f, 0f, 6f, 1f);
            Block("Step_2", geometry, 8f, 0f, 10f, 2.2f);
            Block("Step_3", geometry, 11.5f, 0f, 13.5f, 3f);
            Block("Floor_AfterGap", geometry, 18f, -4f, 30f, 0f);
            Block("Floor_AfterDashGap", geometry, 38f, -4f, 57f, 0f);
            Block("Ceiling", geometry, 41f, 5f, 57f, 6f);
            Block("Wall_Right", geometry, 57f, -4f, 58f, 12f);
            EchoLevelArt.DepthShade(geometry, -10f, 14f, -0.6f);
            EchoLevelArt.DepthShade(geometry, 18f, 30f, -0.6f);
            EchoLevelArt.DepthShade(geometry, 38f, 58f, -0.6f);

            Pickup(pickups, 22f, 1f);
            Pickup(pickups, 27f, 1f);
            Pickup(pickups, 48f, 1f);

            Barrier("BreakableWall", barriers, 44f, 44.6f, false);
            Barrier("ExitDoor", barriers, 53f, 53.6f, true);

            // Backdrop: lab corridor along the floor, the city through its windows.
            const float corridorFloor = -0.25f;
            EchoLevelArt.FarCity(backdrop, -10f, 58f, corridorFloor);
            EchoLevelArt.Corridor(backdrop, -10f, 53.3f, corridorFloor);
            EchoLevelArt.UpperMachinery(backdrop, -10f, 58f, corridorFloor + 5.5f);

            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), -7.6f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("ServerCabinets"), 1.5f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("SmallTerminal"), 19.6f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("CryoPod"), 24.5f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.WarpedProp("ScreenPod"), 40f, 0f);
            EchoLevelArt.Prop(props, EchoLevelArt.LabProp("ServerCabinets"), 46.4f, 0f);

            EchoLevelArt.CeilingLamp(lights, -4f, 4.2f, false);
            EchoLevelArt.CeilingLamp(lights, 6f, 4.2f, false);
            EchoLevelArt.CeilingLamp(lights, 25f, 4.2f, true);
            EchoLevelArt.CeilingLamp(lights, 34f, 4.2f, false);
            EchoLevelArt.CeilingLamp(lights, 41.5f, 4.2f, true);
            EchoLevelArt.CeilingLamp(lights, 50f, 4.2f, false);

            EchoLevelArt.Pit(level.transform, 14f, 18f, 0f, -4f);
            EchoLevelArt.Pit(level.transform, 30f, 38f, 0f, -4f);

            var killZone = new GameObject("KillZone") { layer = LayerMask.NameToLayer(Layers.Hazard) };
            killZone.transform.SetParent(level.transform, false);
            killZone.transform.position = new Vector3(24f, -10f, 0f);
            var killCollider = killZone.AddComponent<BoxCollider2D>();
            killCollider.isTrigger = true;
            killCollider.size = new Vector2(100f, 4f);
            killZone.AddComponent<KillZone>();

            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(level.transform, false);
            spawn.transform.position = new Vector3(-6f, 1f, 0f);

            var playerPrefab = EchoPrefabFactory.EnsurePlayerPrefab();
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = spawn.transform.position;

            var follow = camera.gameObject.AddComponent<CameraFollow>();
            EchoPrefabFactory.SetReference(follow, "target", player.transform);
            var followSettings = new SerializedObject(follow);
            followSettings.FindProperty("bounds").rectValue = new Rect(-10f, -4f, 68f, 16f);
            followSettings.ApplyModifiedPropertiesWithoutUndo();

            var controller = level.AddComponent<LevelController>();
            EchoPrefabFactory.SetReference(controller, "player", player.GetComponent<PlayerController>());
            EchoPrefabFactory.SetReference(controller, "spawnPoint", spawn.transform);
            EchoPrefabFactory.SetReference(controller, "cameraFollow", follow);
            var levelSettings = new SerializedObject(controller);
            levelSettings.FindProperty("startingEnergy").intValue = 2;
            levelSettings.FindProperty("maxEnergy").intValue = 5;
            levelSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Rolling a pixel-perfect view makes the art shimmer; the shake keeps its offset only.
        static void DisableCameraRoll(CameraFeedback feedback)
        {
            var settings = new SerializedObject(feedback);
            settings.FindProperty("maxRoll").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Block(string name, Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            var size = new Vector2(xMax - xMin, yMax - yMin);
            EchoLevelArt.DressBlock(go, size);
            go.AddComponent<BoxCollider2D>().size = size;
        }

        static void Pickup(Transform parent, float x, float y)
        {
            var go = new GameObject("EnergyPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.36f; // same reach as the graybox pickup (0.6 radius at 0.6 scale)
            go.AddComponent<EnergyPickup>();
            EchoLevelArt.DressEnergyCell(go);
        }

        // Floor-to-ceiling barrier (ceiling at y = 5) so it can't be jumped over.
        static void Barrier(string name, Transform parent, float xMin, float xMax, bool isExit)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Breakable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, 2.5f, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(xMax - xMin, 5f);

            var field = EchoLevelArt.DressBarrier(go, 5f, isExit);

            var barrier = go.AddComponent<BreakableBarrier>();
            EchoPrefabFactory.SetReference(barrier, "intactVisual", field);
            var settings = new SerializedObject(barrier);
            settings.FindProperty("isLevelExit").boolValue = isExit;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Camera CreateCamera() => CreateCamera(EchoPalette.Void);

        /// <summary>
        /// Pixel-perfect camera: the world renders at 640x360 (32 px per unit) and is scaled up to
        /// the screen; UI stays at native resolution. Post-processing is on for bloom.
        /// </summary>
        public static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;

            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            go.AddComponent<AudioListener>();

            var pixelPerfect = go.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = (int)EchoImportRules.PixelsPerUnit;
            pixelPerfect.refResolutionX = 640;
            pixelPerfect.refResolutionY = 360;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            return camera;
        }
    }
}
