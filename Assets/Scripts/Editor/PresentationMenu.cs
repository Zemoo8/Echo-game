using Echo.Presentation;
using UnityEditor;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// ECHO > Presentation menu: switches the presentation phase stored in Resources/PresentationConfig.
    /// Switching never removes anything; it only changes which features are on at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class PresentationMenu
    {
        const string Root = "ECHO/Presentation/";
        const string Everything = Root + "Show Everything";
        const string Phase1 = Root + "Phase 1 - Conception";
        const string Phase2 = Root + "Phase 2 - Diagrams";
        const string Phase3 = Root + "Phase 3 - Design";
        const string Phase4 = Root + "Phase 4 - Development";
        const string Phase5 = Root + "Phase 5 - 2D Animation";
        const string Phase6 = Root + "Phase 6 - Testing";
        const string Phase7 = Root + "Phase 7 - Final Product";
        const string Phase8 = Root + "Phase 8 - GamePix";

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

        static PresentationMenu() => EditorApplication.delayCall += RefreshChecks;

        [MenuItem(Everything, priority = 0)] static void ShowEverything() => SetPhase(PresentationPhase.Everything);
        [MenuItem(Phase1, priority = 11)] static void ShowPhase1() => SetPhase(PresentationPhase.Conception);
        [MenuItem(Phase2, priority = 12)] static void ShowPhase2() => SetPhase(PresentationPhase.Diagrams);
        [MenuItem(Phase3, priority = 13)] static void ShowPhase3() => SetPhase(PresentationPhase.Design);
        [MenuItem(Phase4, priority = 14)] static void ShowPhase4() => SetPhase(PresentationPhase.Development);
        [MenuItem(Phase5, priority = 15)] static void ShowPhase5() => SetPhase(PresentationPhase.Animation2D);
        [MenuItem(Phase6, priority = 16)] static void ShowPhase6() => SetPhase(PresentationPhase.Testing);
        [MenuItem(Phase7, priority = 17)] static void ShowPhase7() => SetPhase(PresentationPhase.FinalProduct);
        [MenuItem(Phase8, priority = 18)] static void ShowPhase8() => SetPhase(PresentationPhase.GamePix);

        public static void SetPhase(PresentationPhase phase)
        {
            var config = AssetDatabase.LoadAssetAtPath<PresentationConfig>(EchoProjectSetup.PresentationConfigPath);
            if (config == null)
            {
                Debug.LogError("[ECHO] PresentationConfig not found. Run ECHO > Setup > Run Project Setup first.");
                return;
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("phase").intValue = (int)phase;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            RefreshChecks();
            Debug.Log($"[ECHO] Presentation phase set to {phase}.");
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
