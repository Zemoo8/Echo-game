using Echo.Core;
using UnityEngine;

namespace Echo.Presentation
{
    /// <summary>
    /// Chooses which presentation phase the game runs in. Lives at Resources/PresentationConfig.
    /// Switch phases from the ECHO > Presentation menu rather than editing this by hand.
    /// </summary>
    [CreateAssetMenu(fileName = "PresentationConfig", menuName = "ECHO/Presentation Config")]
    public class PresentationConfig : ScriptableObject
    {
        public const string ResourcePath = "PresentationConfig";

        const Feature CoreGameplay = Feature.Dash | Feature.Energy | Feature.Hazards | Feature.Enemies
                                     | Feature.Switches | Feature.BreakableBarriers | Feature.Checkpoints;
        const Feature GameShell = Feature.MainMenu | Feature.Hud | Feature.PauseMenu | Feature.SaveSystem
                                  | Feature.TouchControls;
        const Feature Polish = Feature.Animation | Feature.VFX | Feature.CameraFeedback | Feature.Audio | Feature.Music;
        const Feature AllContent = Feature.Level1 | Feature.Level2 | Feature.Level3 | Feature.Boss | Feature.Ending;

        [Tooltip("Which professor presentation stage to demonstrate. Everything = the complete game.")]
        [SerializeField] PresentationPhase phase = PresentationPhase.Everything;

        [Tooltip("Features to force on for this demo, on top of the phase defaults.")]
        [SerializeField] Feature forceEnabled;

        [Tooltip("Features to force off for this demo, even if the phase includes them.")]
        [SerializeField] Feature forceDisabled;

        public PresentationPhase Phase => phase;
        public Feature EnabledFeatures => (FeaturesFor(phase) | forceEnabled) & ~forceDisabled;

        /// <summary>A config for one phase, for tests and tools (the game reads the Resources asset).</summary>
        public static PresentationConfig ForPhase(PresentationPhase value)
        {
            var config = CreateInstance<PresentationConfig>();
            config.phase = value;
            return config;
        }

        /// <summary>Default feature set for each phase. Later phases build on earlier ones.</summary>
        public static Feature FeaturesFor(PresentationPhase phase)
        {
            const Feature development = CoreGameplay | GameShell | Feature.Level1 | Feature.Level2 | Feature.Audio;
            const Feature animation = development | Feature.Animation | Feature.VFX | Feature.CameraFeedback | Feature.Level3;
            const Feature testing = animation | Feature.Boss | Feature.Music | Feature.DevOverlay;
            const Feature final = CoreGameplay | GameShell | Polish | AllContent;

            switch (phase)
            {
                case PresentationPhase.Conception:
                case PresentationPhase.Diagrams:
                    return Feature.ConceptShowcase;
                case PresentationPhase.Design:
                    return Feature.ConceptShowcase | Feature.DesignShowcase | Feature.MainMenu;
                case PresentationPhase.Development:
                    return development;
                case PresentationPhase.Animation2D:
                    return animation;
                case PresentationPhase.Testing:
                    return testing;
                case PresentationPhase.FinalProduct:
                    return final;
                case PresentationPhase.GamePix:
                    return final | Feature.GamePixAds;
                default:
                    return ~Feature.None;
            }
        }

        /// <summary>First scene the Boot scene hands off to in each phase.</summary>
        public static string StartSceneFor(PresentationPhase phase)
        {
            switch (phase)
            {
                case PresentationPhase.Conception:
                case PresentationPhase.Diagrams:
                    return SceneNames.ConceptShowcase;
                case PresentationPhase.Design:
                    return SceneNames.DesignShowcase;
                default:
                    return SceneNames.MainMenu;
            }
        }
    }

    /// <summary>Runtime entry point: <c>PresentationMode.IsEnabled(Feature.Dash)</c>.</summary>
    public static class PresentationMode
    {
        static PresentationConfig config;

        public static PresentationConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = Resources.Load<PresentationConfig>(PresentationConfig.ResourcePath);
                    if (config == null)
                    {
                        Debug.LogWarning("[ECHO] Resources/PresentationConfig missing; running the complete game.");
                        config = ScriptableObject.CreateInstance<PresentationConfig>();
                    }
                }
                return config;
            }
        }

        public static PresentationPhase Phase => Config.Phase;
        public static string StartScene => PresentationConfig.StartSceneFor(Phase);

        public static bool IsEnabled(Feature feature) => (Config.EnabledFeatures & feature) == feature;

        /// <summary>The content feature a scene belongs to; None for scenes every phase may load.</summary>
        public static Feature FeatureForScene(string scene)
        {
            switch (scene)
            {
                case SceneNames.Level1: return Feature.Level1;
                case SceneNames.Level2: return Feature.Level2;
                case SceneNames.Level3: return Feature.Level3;
                case SceneNames.Boss: return Feature.Boss;
                case SceneNames.Ending: return Feature.Ending;
                default: return Feature.None;
            }
        }

        /// <summary>False for a level the active phase doesn't present yet (Phase 4 ends after Level 2).</summary>
        public static bool IsSceneEnabled(string scene)
        {
            var feature = FeatureForScene(scene);
            return feature == Feature.None || IsEnabled(feature);
        }

        /// <summary>Use a specific config instead of the Resources one (automated tests and tools).</summary>
        public static void Override(PresentationConfig overrideConfig) => config = overrideConfig;

        // Keeps the cache correct when domain reload is disabled for faster play mode.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => config = null;
    }
}
