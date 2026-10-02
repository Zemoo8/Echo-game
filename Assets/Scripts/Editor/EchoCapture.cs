using System.IO;
using Echo.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.EditorTools
{
    /// <summary>
    /// Renders still screenshots of the movement sandbox and the design showcase for docs and
    /// presentations (no play mode, so effects and animation are not shown). Output: Docs/Screenshots/.
    /// Headless (needs a graphics device, so no -nographics):
    /// -batchmode -quit -executeMethod Echo.EditorTools.EchoCapture.CaptureSandbox
    /// -batchmode -quit -executeMethod Echo.EditorTools.EchoCapture.CaptureShowcase
    /// -batchmode -quit -executeMethod Echo.EditorTools.EchoCapture.CaptureLevel1
    /// </summary>
    public static class EchoCapture
    {
        const string OutputFolder = "Docs/Screenshots";
        const int Width = 1280;
        const int Height = 720;

        // Camera centres along the sandbox route: start, first pit (city window), second pit, barrier and exit.
        static readonly Vector2[] SandboxShots =
        {
            new Vector2(0f, 2.5f), new Vector2(16f, 2.5f), new Vector2(31f, 2.5f), new Vector2(48f, 2.5f),
        };

        // Level 1, section by section: wake, stairs and pit, barrier, air-dash pit, lasers, final gap, exit.
        static readonly Vector2[] Level1Shots =
        {
            new Vector2(6f, 2.5f), new Vector2(33f, 4f), new Vector2(55f, 2.5f), new Vector2(78f, 2.5f),
            new Vector2(106f, 2.5f), new Vector2(128f, 2.5f), new Vector2(150f, 2.5f),
        };

        [MenuItem("ECHO/Tools/Capture Sandbox Screenshots")]
        public static void CaptureSandbox() => CaptureWorld(EchoSceneFactory.SandboxPath, SandboxShots, "sandbox_");

        [MenuItem("ECHO/Tools/Capture Level 1 Screenshots")]
        public static void CaptureLevel1() => CaptureWorld(EchoLevelFactory.Level1Path, Level1Shots, "level1_");

        static void CaptureWorld(string scenePath, Vector2[] shots, string prefix)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
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

            for (int i = 0; i < shots.Length; i++)
            {
                camera.transform.position = new Vector3(shots[i].x, shots[i].y, -10f);
                camera.Render();

                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = null;

                string path = $"{OutputFolder}/{prefix}{i + 1}.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log($"[ECHO Capture] {path}");
            }

            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(target);
            if (pixelPerfect != null)
                pixelPerfect.enabled = true;
        }

        /// <summary>One still per design-showcase slide (world and interface): design_01.png onward.</summary>
        [MenuItem("ECHO/Tools/Capture Design Showcase Slides")]
        public static void CaptureShowcase()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(EchoShowcaseFactory.ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            var showcase = Object.FindFirstObjectByType<DesignShowcase>();
            Directory.CreateDirectory(OutputFolder);

            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect != null)
                pixelPerfect.enabled = false;

            // Overlay canvases never reach a render texture, so the interface is drawn through the
            // camera for the capture (the scene is not saved).
            var canvas = showcase.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingLayerName = "Foreground"; // above every sprite, as the overlay is in game
            canvas.sortingOrder = 1000;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            for (int i = 0; i < showcase.SlideCount; i++)
            {
                showcase.Jump(i);
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = null;

                string path = $"{OutputFolder}/design_{i + 1:00}.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log($"[ECHO Capture] {path}");
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(target);
        }
    }
}
