using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.Core
{
    /// <summary>
    /// Loads scenes asynchronously, one at a time. Lives on the persistent Systems prefab.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public bool IsLoading { get; private set; }

        /// <summary>Raised with the scene name once a load has finished.</summary>
        public event Action<string> SceneLoaded;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[ECHO] Ignoring load of '{sceneName}': another scene is still loading.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
                yield return null;

            IsLoading = false;
            Debug.Log($"[ECHO] Loaded scene '{sceneName}'.");
            SceneLoaded?.Invoke(sceneName);
        }

        public static bool IsInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return true;
            }
            return false;
        }
    }
}
