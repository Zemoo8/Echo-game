using System;
using Echo.Audio;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>The four places ECHO happens, in story order.</summary>
    public enum Biome
    {
        Lab,
        Security,
        Core,
        Reactor,
    }

    /// <summary>
    /// One biome's look: its ground tiles, backdrop pieces, light and music. Levels are dressed
    /// from a kit, so a biome can be re-skinned by editing its asset instead of any code.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentKit", menuName = "ECHO/Environment Kit")]
    public class EnvironmentKit : ScriptableObject
    {
        public Biome biome;
        public string displayName = "BIOME";
        [TextArea] public string description;

        [Header("Ground")]
        [Tooltip("Solid ground, drawn tiled with fixed 16 px borders.")]
        public Sprite block;

        [Header("Backdrop")]
        [Tooltip("Wall pieces laid left to right behind the level, repeated as needed.")]
        public Sprite[] backdrop = Array.Empty<Sprite>();
        public Color backdropTint = Color.white;
        [Tooltip("Optional far layer (seen through windows or above the walls).")]
        public Sprite farLayer;
        public Color farTint = Color.white;

        [Header("Light")]
        [Tooltip("Camera clear color: the darkest tone of the backdrop, so the art has no edges.")]
        public Color voidColor = Color.black;
        public Color ambientColor = Color.white;
        [Range(0f, 2f)] public float ambientIntensity = 0.85f;
        public Color lampColor = Color.white;

        [Header("Dressing")]
        public Sprite[] props = Array.Empty<Sprite>();
        public MusicId music = MusicId.Lab;
    }
}
