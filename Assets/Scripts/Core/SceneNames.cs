namespace Echo.Core
{
    /// <summary>Scene names as listed in the build. EchoProjectSetup keeps the build list in this order.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";

        public const string Level1 = "Level1_Awakening";
        public const string Level2 = "Level2_Facility";
        public const string Level3 = "Level3_Core";
        public const string Boss = "Boss_Guardian";
        public const string Ending = "Ending";

        public const string ConceptShowcase = "Showcase_Concept";
        public const string DesignShowcase = "Showcase_Design";

        /// <summary>Graybox test chamber for movement and dash tuning.</summary>
        public const string DevSandbox = "Sandbox_Movement";
    }
}
