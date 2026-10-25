using Echo.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// ECHO > Presentation menu: one click sets the presentation phase stored in
    /// Resources/PresentationConfig and runs it from the Boot scene, restarting Play mode if it is
    /// already running. Switching never removes anything; it only changes which features are on
    /// at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class PresentationMenu
    {
        const string Root = "ECHO/Presentation/";
        const string Everything = Root + "Show Everything - Entire Game";
        const string Phase1 = Root + "Phase 1 - Conception (pitch slides)";
        const string Phase2 = Root + "Phase 2 - Diagrams (pitch and diagram slides)";
        const string Phase3 = Root + "Phase 3 - Design (design slides)";
        const string Phase4 = Root + "Phase 4 - Development (Levels 1-2, plain look)";
        const string Phase5 = Root + "Phase 5 - 2D Animation (Levels 1-3)";
        const string Phase6 = Root + "Phase 6 - Testing (Levels 1-3, Boss, debug overlay)";
        const string Phase7 = Root + "Phase 7 - Final Product (Levels 1-3, Boss, Ending)";
        const string Phase8 = Root + "Phase 8 - GamePix (entire game with ads)";
        const string Play = Root + "Play From Start";

        static readonly (string path, PresentationPhase phase)[] Items =
        {
            (Everything, PresentationPhase.Everything),
            (Phase1, PresentationPhase.Conception),
            (Phase2, PresentationPhase.Diagrams),
            (Phase3, PresentationPhase.Design),
            (Phase4, PresentationPhase.Development),
            (Phase5, PresentationPhase.Animation2D),
            (Phase6, PresentationPhase.Testing),
            (Phase7, PresentationPhase.FinalProduct),
            (Phase8, PresentationPhase.GamePix),
        };

        // Set while Play mode is being stopped so that it starts again from Boot.
        static bool restartPending;

        static PresentationMenu()
        {
            EditorApplication.delayCall += RefreshChecks;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem(Everything, priority = 0)] static void ShowEverything() => Show(PresentationPhase.Everything);
        [MenuItem(Phase1, priority = 11)] static void ShowPhase1() => Show(PresentationPhase.Conception);
        [MenuItem(Phase2, priority = 12)] static void ShowPhase2() => Show(PresentationPhase.Diagrams);
        [MenuItem(Phase3, priority = 13)] static void ShowPhase3() => Show(PresentationPhase.Design);
        [MenuItem(Phase4, priority = 14)] static void ShowPhase4() => Show(PresentationPhase.Development);
        [MenuItem(Phase5, priority = 15)] static void ShowPhase5() => Show(PresentationPhase.Animation2D);
        [MenuItem(Phase6, priority = 16)] static void ShowPhase6() => Show(PresentationPhase.Testing);
        [MenuItem(Phase7, priority = 17)] static void ShowPhase7() => Show(PresentationPhase.FinalProduct);
        [MenuItem(Phase8, priority = 18)] static void ShowPhase8() => Show(PresentationPhase.GamePix);

        static void Show(PresentationPhase phase)
        {
            if (SetPhase(phase))
                PlayFromStart();
        }

        /// <summary>Runs the active phase from the Boot scene, whatever scene is open.</summary>
        [MenuItem(Play, priority = 40)]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying)
            {
                // Already running: stop first, then OnPlayModeChanged starts again from Boot.
                restartPending = true;
                EditorApplication.isPlaying = false;
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StartFromBoot();
            }
        }

        static void StartFromBoot()
        {
            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(EchoSceneFactory.BootPath);
            if (boot == null)
            {
                Debug.LogError($"[ECHO] Boot scene not found at {EchoSceneFactory.BootPath}. Run ECHO > Setup > Run Project Setup first.");
                return;
            }

            // The phase picks its first scene in Boot, so a demo has to start there, not in the open scene.
            EditorSceneManager.playModeStartScene = boot;
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                // Only this menu starts from Boot; the Play button and the test runner keep the open scene.
                EditorSceneManager.playModeStartScene = null;
            }
            else if (change == PlayModeStateChange.EnteredEditMode && restartPending)
            {
                restartPending = false;
                EditorApplication.delayCall += StartFromBoot;
            }
        }

        /// <summary>Stores the phase without running it. False when the config asset is missing.</summary>
        public static bool SetPhase(PresentationPhase phase)
        {
            var config = AssetDatabase.LoadAssetAtPath<PresentationConfig>(EchoProjectSetup.PresentationConfigPath);
            if (config == null)
            {
                Debug.LogError("[ECHO] PresentationConfig not found. Run ECHO > Setup > Run Project Setup first.");
                return false;
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("phase").intValue = (int)phase;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            RefreshChecks();
            Debug.Log($"[ECHO] Presentation phase set to {phase}.");
            return true;
        }

        static void RefreshChecks()
        {
            var config = AssetDatabase.LoadAssetAtPath<PresentationConfig>(EchoProjectSetup.PresentationConfigPath);
            if (config == null)
                return;

            foreach (var item in Items)
                Menu.SetChecked(item.path, item.phase == config.Phase);
        }
    }
}
