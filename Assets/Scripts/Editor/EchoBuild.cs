using GamePix.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Builds the GamePix package exactly like GamePix > Build and Run, but without the plugin
    /// update check or auto-launching a browser, so it also works headless:
    /// -batchmode -quit -executeMethod Echo.EditorTools.EchoBuild.BuildGamePix
    /// Output: html5/ (playable WebGL build + the .gpx archive to upload).
    /// </summary>
    public static class EchoBuild
    {
        const string OutputFolder = "html5";

        [MenuItem("ECHO/Build/GamePix Package (no auto-run)")]
        public static void BuildGamePix()
        {
            if (Emscripten.editorVersion != Emscripten.pluginVersion)
            {
                throw new BuildFailedException(
                    $"GamePix plugin targets emscripten {Emscripten.pluginVersion} but this editor uses {Emscripten.editorVersion}.");
            }

            UnityConfigurator.SetWebGLSettings(true, AssetsCompression.Uncompressed);
            bool built = Builder.BuildArchive(OutputFolder, AssetsCompression.Uncompressed, DataFileCompression.Uncompressed, false);
            AssetDatabase.SaveAssets();

            if (!built)
                throw new BuildFailedException("GamePix build failed. See the Unity log for details.");

            Debug.Log($"[ECHO Build] GamePix package ready in {OutputFolder}/");
        }
    }
}
