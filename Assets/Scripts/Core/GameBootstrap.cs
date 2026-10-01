using Echo.Presentation;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Spawns the persistent Systems prefab (Resources/Systems) before the first scene loads,
    /// so every scene has its managers, including a level opened directly in the editor.
    /// </summary>
    public static class GameBootstrap
    {
        const string SystemsResourcePath = "Systems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SpawnSystems()
        {
            var prefab = Resources.Load<GameObject>(SystemsResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[ECHO] Resources/Systems prefab is missing. Run ECHO > Setup > Run Project Setup.");
                return;
            }

            var systems = Object.Instantiate(prefab);
            systems.name = prefab.name;
            Object.DontDestroyOnLoad(systems);
            Debug.Log($"[ECHO] Systems ready. Presentation phase: {PresentationMode.Phase}");
        }
    }
}
