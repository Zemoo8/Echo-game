using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// The player's progress: which level Continue starts. Kept in PlayerPrefs, which GamePix
    /// stores in its localStorage. Callers check the SaveSystem presentation feature.
    /// </summary>
    public static class SaveData
    {
        const string ContinueKey = "echo.continue";

        /// <summary>The level Continue loads, or empty when there's nothing to continue.</summary>
        public static string ContinueScene => PlayerPrefs.GetString(ContinueKey, string.Empty);

        /// <summary>True when there's a saved level that exists in this build.</summary>
        public static bool HasProgress
        {
            get
            {
                string scene = ContinueScene;
                return !string.IsNullOrEmpty(scene) && SceneLoader.IsInBuild(scene);
            }
        }

        public static void SetContinue(string scene)
        {
            if (ContinueScene == scene)
                return;
            PlayerPrefs.SetString(ContinueKey, scene);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(ContinueKey);
            PlayerPrefs.Save();
        }
    }
}
