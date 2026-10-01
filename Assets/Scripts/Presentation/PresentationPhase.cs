using System;

namespace Echo.Presentation
{
    /// <summary>
    /// Professor presentation stages (see PRESENTATION_PHASES.md).
    /// Everything = the complete game and is the normal development setting.
    /// </summary>
    public enum PresentationPhase
    {
        Everything = 0,
        Conception = 1,
        Diagrams = 2,
        Design = 3,
        Development = 4,
        Animation2D = 5,
        Testing = 6,
        FinalProduct = 7,
        GamePix = 8,
    }

    /// <summary>
    /// Parts of the game a presentation phase can hide. The code for every feature always
    /// exists in the project; presentation mode only decides what is switched on at runtime.
    /// </summary>
    [Flags]
    public enum Feature
    {
        None = 0,

        // Core gameplay
        Dash = 1 << 0,
        Energy = 1 << 1,
        Hazards = 1 << 2,
        Enemies = 1 << 3,
        Switches = 1 << 4,
        BreakableBarriers = 1 << 5,
        Checkpoints = 1 << 6,

        // Content
        Level1 = 1 << 7,
        Level2 = 1 << 8,
        Level3 = 1 << 9,
        Boss = 1 << 10,
        Ending = 1 << 11,

        // Polish
        Animation = 1 << 12,
        VFX = 1 << 13,
        CameraFeedback = 1 << 14,
        Audio = 1 << 15,
        Music = 1 << 16,

        // UI and platform
        MainMenu = 1 << 17,
        Hud = 1 << 18,
        PauseMenu = 1 << 19,
        SaveSystem = 1 << 20,
        TouchControls = 1 << 21,
        DevOverlay = 1 << 22,
        GamePixAds = 1 << 23,

        // Presentation-only scenes
        ConceptShowcase = 1 << 24,
        DesignShowcase = 1 << 25,
    }
}
