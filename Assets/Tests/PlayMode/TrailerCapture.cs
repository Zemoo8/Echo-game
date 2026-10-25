using System;
using System.Collections;
using System.IO;
using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echo.Tests
{
    /// <summary>
    /// Records the presentation video's footage: the level and boss bots play each part of the
    /// game while every frame (world and interface) is saved as a JPEG in Recordings/&lt;part&gt;/.
    /// Game time runs at a fixed 30 frames per second (60 for the boss fight) through
    /// Time.captureFramerate, so the footage is smooth however slow the machine is. Explicit:
    /// never part of the normal test run.
    /// Headless (needs a graphics device, so no -nographics):
    /// -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Echo.Tests.TrailerCapture
    /// </summary>
    [Explicit, Category("Capture")]
    public class TrailerCapture
    {
        const int FrameRate = 30;
        FrameRecorder recorder;

        [SetUp]
        public void SetUp()
        {
            PresentationMode.Override(PresentationConfig.ForPhase(PresentationPhase.FinalProduct)); // no debug overlay
            AudioListener.volume = 0f;
            SaveData.Clear();
            SoundSettings.MusicOn = SoundSettings.SfxOn = true; // the menu shows the switches; earlier tests may have left them off
            Time.captureFramerate = FrameRate;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (recorder != null)
                Object.Destroy(recorder.gameObject);
            Time.captureFramerate = 0;

            var empty = SceneManager.CreateScene("Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Boot);
            PresentationMode.Override(null);
            AudioListener.volume = 1f;
        }

        void StartRecording(string part)
        {
            string folder = Path.Combine(Environment.GetEnvironmentVariable("ECHO_CAPTURE_DIR") ?? "Recordings", part);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            recorder = new GameObject("FrameRecorder").AddComponent<FrameRecorder>();
            Object.DontDestroyOnLoad(recorder.gameObject);
            recorder.Folder = folder;
        }

        static IEnumerator Seconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        static IEnumerator WaitForLevel(string scene)
        {
            float end = Time.realtimeSinceStartup + 60f;
            while (!(SceneManager.GetActiveScene().name == scene && LevelController.Current != null
                     && GameManager.Instance.State == GameState.Playing) && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State, scene + " should start");
        }

        /// <summary>Steps the bot until the level is complete, then keeps filming the level-complete card.</summary>
        static IEnumerator Play(Action step, float timeout)
        {
            float end = Time.time + timeout;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                step();
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State, "the bot should clear the level");
            yield return Seconds(2.5f);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Menu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
            yield return Seconds(0.5f);
            StartRecording("menu");
            yield return Seconds(8f);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Level1() => PlayLevel(SceneNames.Level1, "level1");

        [UnityTest, Timeout(300000)]
        public IEnumerator Level2() => PlayLevel(SceneNames.Level2, "level2");

        [UnityTest, Timeout(300000)]
        public IEnumerator Level3() => PlayLevel(SceneNames.Level3, "level3");

        IEnumerator PlayLevel(string scene, string part)
        {
            SceneManager.LoadScene(scene);
            yield return WaitForLevel(scene);
            StartRecording(part);
            var player = LevelController.Current.Player;
            player.enabled = false; // the bot drives movement and dash directly
            using (var human = new HumanTouch(player))
                yield return Play(human.Step, 150f);
        }

        /// <summary>
        /// The first 30 s of the fight: the Guardian wakes, the bot lands the first phase's two
        /// hits and the second phase begins. Under a fixed capture rate the bot loses to the
        /// second phase's ceiling beams (at 30 or 60 frames a second), so the whole fight isn't
        /// filmed; the video cuts from here to the ending.
        /// </summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator Boss()
        {
            Time.captureFramerate = 60; // the boss bot was tuned at 60 updates a second
            SceneManager.LoadScene(SceneNames.Boss);
            yield return WaitForLevel(SceneNames.Boss);
            StartRecording("boss");
            var player = LevelController.Current.Player;
            player.enabled = false;
            var bot = new BossBot(player);
            float end = Time.time + 30f;
            while (Time.time < end)
            {
                bot.Step();
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Ending()
        {
            SceneManager.LoadScene(SceneNames.Ending);
            yield return null;
            StartRecording("ending");
            var sequence = Object.FindFirstObjectByType<UI.EndingSequence>();
            Assert.IsNotNull(sequence, "the ending plays");
            float end = Time.time + 60f;
            while (!sequence.AtTheEnd && Time.time < end)
                yield return null;
            yield return Seconds(3f);
        }

        /// <summary>
        /// Wraps the level bot so the footage plays like a person rather than a machine: it gets
        /// its bearings before setting off, stops for a breath at checkpoints, reacts a moment
        /// after a laser switches off instead of on the exact frame, and spends a full meter's
        /// spare charge dashing through a machine (a full meter would skip the next cell anyway).
        /// </summary>
        sealed class HumanTouch : IDisposable
        {
            readonly LevelBot bot;
            readonly PlayerController player;
            readonly float startedAt = Time.time;
            float stillUntil;
            float pauseAt = float.PositiveInfinity; // when to stop for a breath (after a checkpoint)
            LaserGate watching;
            bool watchedOn;

            public HumanTouch(PlayerController player)
            {
                this.player = player;
                bot = new LevelBot(player);
                Checkpoint.AnyActivated += OnCheckpoint;
            }

            public void Dispose() => Checkpoint.AnyActivated -= OnCheckpoint;

            void OnCheckpoint(Checkpoint _) => pauseAt = Time.time + 0.2f; // a step past the light, then stop

            float X => player.transform.position.x;
            float FeetY => player.GetComponent<Collider2D>().bounds.min.y;
            bool Grounded => player.Movement.IsGrounded;

            public void Step()
            {
                var movement = player.Movement;
                float t = Time.time - startedAt;

                // Getting one's bearings: stand, glance back, then set off.
                if (t < 1.6f)
                {
                    movement.SetMoveInput(t > 0.9f && t < 1.0f ? -1f : 0f);
                    return;
                }

                if (Time.time >= pauseAt && Grounded && Safe())
                {
                    stillUntil = Time.time + 0.55f;
                    pauseAt = float.PositiveInfinity;
                }

                // A person sees the laser go off and only then starts to move.
                var gate = LaserAhead();
                if (gate != watching)
                {
                    watching = gate;
                    watchedOn = gate != null && gate.IsOn;
                }
                else if (gate != null)
                {
                    if (watchedOn && !gate.IsOn && Grounded && Mathf.Abs(movement.Velocity.x) < 0.5f)
                        stillUntil = Mathf.Max(stillUntil, Time.time + 0.22f);
                    watchedOn = gate.IsOn;
                }

                if (Time.time < stillUntil && player.IsAlive)
                {
                    movement.SetMoveInput(0f);
                    movement.SetJumpHeld(false);
                    return;
                }

                if (player.IsAlive && Grounded && player.Energy.IsFull && MachineInDashReach())
                {
                    movement.SetMoveInput(1f);
                    player.Dash.RequestDash();
                    return;
                }

                bot.Step();
            }

            // Somewhere a pause can't hurt: solid ground, no machine close by.
            bool Safe()
            {
                foreach (var machine in Object.FindObjectsByType<Machine>(FindObjectsSortMode.None))
                {
                    if (!machine.IsDestroyed && Mathf.Abs(machine.transform.position.x - X) < 6f)
                        return false;
                }
                foreach (var plate in Object.FindObjectsByType<CollapsingPlatform>(FindObjectsSortMode.None))
                {
                    if (Mathf.Abs(plate.transform.position.x - X) < 2.5f)
                        return false;
                }
                return true;
            }

            LaserGate LaserAhead()
            {
                foreach (var gate in Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None))
                {
                    float distance = gate.transform.position.x - X;
                    if (!gate.IsSwitchedOff && distance > 0.4f && distance < 2.6f)
                        return gate;
                }
                return null;
            }

            bool MachineInDashReach()
            {
                foreach (var machine in Object.FindObjectsByType<Machine>(FindObjectsSortMode.None))
                {
                    var p = machine.transform.position;
                    float distance = p.x - X;
                    if (!machine.IsDestroyed && distance > 1.4f && distance < 3.2f && p.y > FeetY - 0.5f && p.y < FeetY + 1.6f)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Points the main camera at a 1280x720 texture, with the overlay canvases drawn through the
        /// camera (overlays never reach a render texture), and saves each frame the game renders
        /// into it. The texture is read one frame later, once the frame has been drawn.
        /// </summary>
        [DefaultExecutionOrder(int.MaxValue)]
        sealed class FrameRecorder : MonoBehaviour
        {
            const int Width = 1280;
            const int Height = 720;

            public string Folder;
            RenderTexture target;
            Texture2D image;
            Camera lastCamera;
            int frame;
            readonly System.Collections.Generic.HashSet<Canvas> lifted = new System.Collections.Generic.HashSet<Canvas>();

            void Awake()
            {
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            }

            void OnDestroy()
            {
                if (lastCamera != null && lastCamera.targetTexture == target)
                    lastCamera.targetTexture = null;
                foreach (var canvas in lifted)
                {
                    if (canvas == null)
                        continue; // went with its scene
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingLayerName = "Default";
                    canvas.sortingOrder -= 1000;
                }
                Destroy(target);
                Destroy(image);
            }

            void LateUpdate()
            {
                // Camera.main skips a camera that draws into a texture, so keep hold of ours.
                bool hadCamera = !ReferenceEquals(lastCamera, null);
                var camera = lastCamera != null ? lastCamera : Camera.main;
                if (hadCamera && camera != lastCamera)
                {
                    enabled = false; // the game moved on to the next scene: this part is done
                    return;
                }
                if (camera == null)
                    return;

                bool fresh = lastCamera == null;
                if (fresh)
                {
                    // The pixel-perfect pass assumes it draws to the screen; 1280x720 is an exact 2x
                    // of its 640x360 view, so the plain camera with the same size gives the same image.
                    var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
                    if (pixelPerfect != null)
                    {
                        pixelPerfect.enabled = false;
                        camera.orthographicSize = pixelPerfect.refResolutionY / (2f * pixelPerfect.assetsPPU);
                    }
                    camera.targetTexture = target;
                    lastCamera = camera;
                    Debug.Log($"[ECHO Capture] {Folder}: camera {camera.name} in {camera.gameObject.scene.name}, size {camera.orthographicSize}");
                }

                // A camera-mode canvas without a camera reports itself as an overlay, so the camera is
                // set together with the mode; each canvas is lifted above the sprites only once.
                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                    if (lifted.Add(canvas))
                    {
                        canvas.sortingLayerName = "Foreground"; // above every sprite, as the overlay is in game
                        canvas.sortingOrder += 1000;
                    }
                    fresh = true;
                }
                if (fresh)
                    return; // the texture still holds a frame drawn before this setup

                // The texture holds the frame the game drew last time round.
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(Folder, $"f_{++frame:00000}.jpg"), image.EncodeToJPG(92));
            }
        }
    }
}
