using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// ECHO's color language. Each gameplay role has one color so the player can read any
    /// screen at a glance: cyan always means energy and dashing, red always means danger.
    /// </summary>
    public static class EchoPalette
    {
        /// <summary>Background void of the facility.</summary>
        public static readonly Color Void = Hex(0x070A12);

        /// <summary>Platforms, walls and floors.</summary>
        public static readonly Color Structure = Hex(0x232C3F);
        public static readonly Color StructureEdge = Hex(0x3B4A66);

        /// <summary>The player's body; the brightest neutral on screen.</summary>
        public static readonly Color PlayerBody = Hex(0xEAF8FF);

        /// <summary>Energy: pickups, the HUD, the player's visor, dash effects, breakable barriers.</summary>
        public static readonly Color Energy = Hex(0x36E2FF);

        /// <summary>Anything that hurts: lasers, spikes, kill zones.</summary>
        public static readonly Color Hazard = Hex(0xFF3355);

        public static readonly Color Enemy = Hex(0xFF8A2A);
        public static readonly Color Switch = Hex(0xFFD23F);

        /// <summary>The way out: exit doors and the goal.</summary>
        public static readonly Color Exit = Hex(0x3DFF8F);

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
