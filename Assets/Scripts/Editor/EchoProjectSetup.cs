using System.Collections.Generic;
using System.Linq;
using Echo.Core;
using Echo.Platform;
using Echo.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echo.EditorTools
{
    /// <summary>
    /// Re-runnable project setup for ECHO: 2D rendering, layers, core assets and the boot scenes.
    /// It only creates what is missing and re-applies settings, so it is safe to run any time.
    /// Menu: ECHO > Setup > Run Project Setup.
    /// Headless: -batchmode -quit -executeMethod Echo.EditorTools.EchoProjectSetup.Run
    /// </summary>
    public static class EchoProjectSetup
    {
        public const string PresentationConfigPath = "Assets/Resources/PresentationConfig.asset";
        public const string SystemsPrefabPath = "Assets/Resources/Systems.prefab";

        const string RendererPath = "Assets/Settings/Echo2DRenderer.asset";
        const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        static readonly string[] PipelineAssetPaths = { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" };

        const string ScenesFolder = "Assets/Scenes";

        // Scenes in build order. Scenes that do not exist yet are skipped.
        static readonly string[] BuildScenes =
        {
            SceneNames.Boot, SceneNames.MainMenu,
            "Levels/" + SceneNames.Level1, "Levels/" + SceneNames.Level2, "Levels/" + SceneNames.Level3,
            "Levels/" + SceneNames.Boss, "Levels/" + SceneNames.Ending,
            "Presentation/" + SceneNames.ConceptShowcase, "Presentation/" + SceneNames.DesignShowcase,
        };

        // Back-to-front draw order.
        static readonly string[] SortingLayers =
        {
            "Background", "Midground", "Default", "Level", "Interactables",
            "Hazards", "Characters", "Player", "VFX", "Foreground",
        };

        // User physics layers, starting at 6 (0-5 are Unity built-ins).
        static readonly string[] PhysicsLayers =
        {
            "Ground", "OneWayPlatform", "Player", "Enemy", "Hazard",
            "Pickup", "Interactable", "Breakable", "Projectile",
        };
        const int FirstUserLayer = 6;

        public static readonly Color BackgroundColor = new Color32(7, 10, 18, 255);

        [MenuItem("ECHO/Setup/Run Project Setup")]
        public static void Run()
        {
            Setup2DRenderer();
            SetupTagManager();
            SetupEditorAndPlayerSettings();
            EnsurePresentationConfig();
            EnsureSystemsPrefab();
            EnsureScenes();
            SetupBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[ECHO Setup] Project setup complete.");
        }

        static void Setup2DRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                // Mirrors URP's own "Create > Rendering > URP 2D Renderer" menu.
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);

                var rendererData = new SerializedObject(renderer);
                rendererData.FindProperty("m_PostProcessData").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
                rendererData.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[ECHO Setup] Created {RendererPath}");
            }

            foreach (string path in PipelineAssetPaths)
            {
                var pipeline = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path));
                var renderers = pipeline.FindProperty("m_RendererDataList");

                int index = -1;
                for (int i = 0; i < renderers.arraySize; i++)
                {
                    if (renderers.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                        index = i;
                }
                if (index < 0)
                {
                    // The original 3D renderer stays in the list, just no longer the default.
                    renderers.InsertArrayElementAtIndex(0);
                    renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    index = 0;
                }

                pipeline.FindProperty("m_DefaultRendererIndex").intValue = index;
                pipeline.FindProperty("m_MSAA").intValue = 1;          // off: sprites don't need it and it is costly on WebGL
                pipeline.FindProperty("m_RenderScale").floatValue = 1f; // full resolution keeps sprites crisp
                pipeline.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetupTagManager()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < PhysicsLayers.Length; i++)
            {
                var layer = layers.GetArrayElementAtIndex(FirstUserLayer + i);
                if (string.IsNullOrEmpty(layer.stringValue))
                    layer.stringValue = PhysicsLayers[i];
                else if (layer.stringValue != PhysicsLayers[i])
                    Debug.LogWarning($"[ECHO Setup] Layer {FirstUserLayer + i} is '{layer.stringValue}', expected '{PhysicsLayers[i]}'. Left unchanged.");
            }

            // Rebuild sorting layers in ECHO order, keeping the IDs of layers that already exist
            // (Default must keep ID 0). Any extra layers someone added are kept at the end.
            var sorting = tagManager.FindProperty("m_SortingLayers");
            var existing = new List<SortingLayerEntry>();
            for (int i = 0; i < sorting.arraySize; i++)
            {
                var entry = sorting.GetArrayElementAtIndex(i);
                existing.Add(new SortingLayerEntry
                {
                    Name = entry.FindPropertyRelative("name").stringValue,
                    Id = entry.FindPropertyRelative("uniqueID").uintValue,
                    Locked = entry.FindPropertyRelative("locked").boolValue,
                });
            }

            var ordered = new List<SortingLayerEntry>();
            foreach (string name in SortingLayers)
            {
                int found = existing.FindIndex(e => e.Name == name);
                ordered.Add(found >= 0 ? existing[found] : new SortingLayerEntry { Name = name, Id = StableId(name) });
            }
            ordered.AddRange(existing.Where(e => !SortingLayers.Contains(e.Name)));

            sorting.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                var entry = sorting.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("name").stringValue = ordered[i].Name;
                entry.FindPropertyRelative("uniqueID").uintValue = ordered[i].Id;
                entry.FindPropertyRelative("locked").boolValue = ordered[i].Locked;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        struct SortingLayerEntry
        {
            public string Name;
            public uint Id;
            public bool Locked;
        }

        static uint StableId(string name)
        {
            uint id = (uint)Animator.StringToHash("EchoSortingLayer_" + name);
            return id == 0 ? 1u : id;
        }

        static void SetupEditorAndPlayerSettings()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            PlayerSettings.productName = "ECHO";
            // 16:9. GamePix keeps this aspect ratio and scales the canvas to the screen.
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;
        }

        static void EnsurePresentationConfig()
        {
            EnsureFolder("Assets", "Resources");
            if (AssetDatabase.LoadAssetAtPath<PresentationConfig>(PresentationConfigPath) != null)
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PresentationConfig>(), PresentationConfigPath);
            Debug.Log($"[ECHO Setup] Created {PresentationConfigPath}");
        }

        static void EnsureSystemsPrefab()
        {
            EnsureFolder("Assets", "Resources");

            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(SystemsPrefabPath) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(SystemsPrefabPath) : new GameObject("Systems");

            EnsureComponent<GameManager>(root);
            EnsureComponent<SceneLoader>(root);
            EnsureComponent<GamePixManager>(root);
            EnsureComponent<DevOverlay>(root);

            PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            if (exists)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);
        }

        static void EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() == null)
                target.AddComponent<T>();
        }

        static void EnsureScenes()
        {
            EnsureScene(SceneNames.Boot, () =>
            {
                CreateCamera();
                new GameObject("Boot").AddComponent<BootSceneController>();
            });

            // Placeholder until the menu UI milestone.
            EnsureScene(SceneNames.MainMenu, () => CreateCamera());
        }

        static void EnsureScene(string sceneName, System.Action populate)
        {
            string path = $"{ScenesFolder}/{sceneName}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ECHO Setup] Created {path}");
        }

        static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>();
            return camera;
        }

        static void SetupBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string scene in BuildScenes)
            {
                string path = $"{ScenesFolder}/{scene}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
