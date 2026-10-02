using System;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// The player's animation frames per state. Swapping this asset re-skins the character
    /// without touching any code.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerVisuals", menuName = "ECHO/Player Visuals")]
    public class PlayerVisuals : ScriptableObject
    {
        [Serializable]
        public class Clip
        {
            public Sprite[] frames = Array.Empty<Sprite>();
            [Min(0.1f)] public float framesPerSecond = 10f;
            public bool loop = true;

            public float Duration => frames.Length / framesPerSecond;

            public Sprite FrameAt(float time)
            {
                if (frames.Length == 0)
                    return null;

                int index = Mathf.FloorToInt(time * framesPerSecond);
                index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
                return frames[index];
            }
        }

        public Clip idle = new Clip { framesPerSecond = 6f };
        public Clip run = new Clip { framesPerSecond = 14f };
        public Clip rise = new Clip { loop = false };
        [Tooltip("Somersault played once at the top of a full jump.")]
        public Clip apexFlip = new Clip { framesPerSecond = 14f, loop = false };
        public Clip fall = new Clip { loop = false };
        public Clip land = new Clip { loop = false };
        public Clip dash = new Clip { loop = false };
        public Clip hurt = new Clip { loop = false };
        public Clip victory = new Clip { framesPerSecond = 12f, loop = false };
    }
}
