using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.UI;
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
            EnsureScene(SandboxPath, BuildSandbox, true);
            EchoProjectSetup.SetupBuildScenes();
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
            CreateGlobalLight();

            var icon = new GameObject("PlayIcon");
            icon.transform.localScale = Vector3.one * 2.5f;
            EchoPrefabFactory.AddSprite(icon, EchoSpriteFactory.IconArrow, EchoPalette.Energy, "Default", 0);

            var menu = new GameObject("MainMenu").AddComponent<MainMenuPlaceholder>();
            EchoPrefabFactory.SetReference(menu, "pulsingIcon", icon.transform);
        }

        // Graybox chamber covering every M1 mechanic, left to right:
        // jump steps, a jumpable gap, a gap that needs jump + dash, a wall only a dash breaks,
        // and the exit door (also dash-only). Energy: start 2 + three pickups; the route needs three dashes.
        static void BuildSandbox()
        {
            var camera = CreateCamera();
            CreateGlobalLight();

            var level = new GameObject("Level");
            var geometry = Child("Geometry", level.transform);
            var pickups = Child("Pickups", level.transform);
            var barriers = Child("Barriers", level.transform);

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

            Pickup(pickups, 22f, 1f);
            Pickup(pickups, 27f, 1f);
            Pickup(pickups, 48f, 1f);

            Barrier("BreakableWall", barriers, 44f, 44.6f, false);
            Barrier("ExitDoor", barriers, 53f, 53.6f, true);

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
            go.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);
            EchoPrefabFactory.AddSprite(go, EchoSpriteFactory.Square, EchoPalette.Structure, "Level", 0);
            go.AddComponent<BoxCollider2D>();
        }

        static void Pickup(Transform parent, float x, float y)
        {
            var go = new GameObject("EnergyPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = Vector3.one * 0.6f;
            EchoPrefabFactory.AddSprite(go, EchoSpriteFactory.Diamond, EchoPalette.Energy, "Interactables", 0);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            go.AddComponent<EnergyPickup>();
        }

        // Floor-to-ceiling barrier (ceiling at y = 5) so it can't be jumped over.
        static void Barrier(string name, Transform parent, float xMin, float xMax, bool isExit)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Breakable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, 2.5f, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(xMax - xMin, 5f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(xMax - xMin, 5f, 1f);
            var color = isExit ? EchoPalette.Exit : EchoPalette.Energy;
            color.a = 0.75f;
            EchoPrefabFactory.AddSprite(visual, EchoSpriteFactory.Square, color, "Interactables", 0);

            var barrier = go.AddComponent<BreakableBarrier>();
            EchoPrefabFactory.SetReference(barrier, "intactVisual", visual);
            var settings = new SerializedObject(barrier);
            settings.FindProperty("isLevelExit").boolValue = isExit;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = EchoPalette.Void;

            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>();
            return camera;
        }

        static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;

            // Light every sorting layer (Light2D only fills this in Awake, which edit-time AddComponent may skip).
            var layers = SortingLayer.layers;
            var serialized = new SerializedObject(light);
            var targets = serialized.FindProperty("m_ApplyToSortingLayers");
            targets.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
                targets.GetArrayElementAtIndex(i).intValue = layers[i].id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
