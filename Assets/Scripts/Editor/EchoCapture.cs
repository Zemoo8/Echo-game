using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.EditorTools
{
    /// <summary>
    /// Renders still screenshots of the movement sandbox for docs and presentations (no play mode,
    /// so effects and animation are not shown). Output: Docs/Screenshots/.
    /// Headless (needs a graphics device, so no -nographics):
    /// -batchmode -quit -executeMethod Echo.EditorTools.EchoCapture.CaptureSandbox
    /// </summary>
    public static class EchoCapture
    {
        const string OutputFolder = "Docs/Screenshots";
        const int Width = 1280;
        const int Height = 720;

        // Camera centres along the sandbox route: start, first pit (city window), second pit, barrier and exit.
        static readonly Vector2[] Shots =
        {
            new Vector2(0f, 2.5f), new Vector2(16f, 2.5f), new Vector2(31f, 2.5f), new Vector2(48f, 2.5f),
        };

        [MenuItem("ECHO/Tools/Capture Sandbox Screenshots")]
        public static void CaptureSandbox()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(EchoSceneFactory.SandboxPath, OpenSceneMode.Single);
            var camera = Camera.main;
            Directory.CreateDirectory(OutputFolder);

            // The pixel-perfect upscale pass assumes it draws to the screen. 1280x720 is already an
            // exact 2x of the 640x360 world view, so the plain camera gives the same crisp image.
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect != null)
                pixelPerfect.enabled = false;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = target;

            for (int i = 0; i < Shots.Length; i++)
            {
                camera.transform.position = new Vector3(Shots[i].x, Shots[i].y, -10f);
                camera.Render();

                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = null;

                string path = $"{OutputFolder}/sandbox_{i + 1}.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log($"[ECHO Capture] {path}");
            }

            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(target);
            if (pixelPerfect != null)
                pixelPerfect.enabled = true;
        }
    }
}
