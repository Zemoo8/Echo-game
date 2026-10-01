using Echo.Presentation;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Boot scene entry point. Hands off to the first real scene for the current presentation
    /// phase, falling back to the main menu if that scene has not been built yet.
    /// </summary>
    public class BootSceneController : MonoBehaviour
    {
        void Start()
        {
            string target = PresentationMode.StartScene;
            if (!SceneLoader.IsInBuild(target))
            {
                Debug.LogWarning($"[ECHO] Start scene '{target}' is not in the build yet; loading '{SceneNames.MainMenu}'.");
                target = SceneNames.MainMenu;
            }
            SceneLoader.Instance.Load(target);
        }
    }
}
