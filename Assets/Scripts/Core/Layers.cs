using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Physics layer names (set up by EchoProjectSetup) and the masks gameplay code queries with.
    /// </summary>
    public static class Layers
    {
        public const string Ground = "Ground";
        public const string OneWayPlatform = "OneWayPlatform";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Hazard = "Hazard";
        public const string Pickup = "Pickup";
        public const string Interactable = "Interactable";
        public const string Breakable = "Breakable";
        public const string Projectile = "Projectile";

        static int groundMask = -1;
        static int dashTargetMask = -1;

        /// <summary>Surfaces the player can stand on.</summary>
        public static int GroundMask => groundMask >= 0 ? groundMask : groundMask = LayerMask.GetMask(Ground, OneWayPlatform, Breakable);

        /// <summary>Things a dash can hit (breakable barriers, enemies, switches).</summary>
        public static int DashTargetMask => dashTargetMask >= 0 ? dashTargetMask : dashTargetMask = LayerMask.GetMask(Breakable, Enemy, Interactable);
    }
}
