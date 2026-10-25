using System;
using System.Collections;
using Echo.Core;
using Echo.Gameplay;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echo.Tests
{
    /// <summary>
    /// The whole story in one run, through the real scene transitions: the level bots play
    /// Levels 1 to 3, the boss bot beats the Guardian, and the ending starts. Nothing is skipped
    /// or teleported; each level leads into the next exactly as it does for a player.
    /// </summary>
    public class FullGameTests
    {
        [SetUp]
        public void SetUp()
        {
            PresentationMode.Override(ScriptableObject.CreateInstance<PresentationConfig>()); // Everything
            AudioListener.volume = 0f; // the scenes play music and effects; keep test runs silent
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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

        static IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(condition(), $"timed out waiting for {what}");
        }

        static IEnumerator WaitForLevel(string scene) =>
            WaitFor(() => SceneManager.GetActiveScene().name == scene && LevelController.Current != null
                          && LevelController.Current.gameObject.scene.name == scene
                          && GameManager.Instance.State == GameState.Playing, 30f, scene + " to start");

        static IEnumerator Play(Action step, float timeout, string scene)
        {
            float end = Time.time + timeout;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                step();
                yield return new WaitForFixedUpdate();
            }
            var level = LevelController.Current;
            Debug.Log($"[ECHO Test] Full game: {scene} {(GameManager.Instance.State == GameState.LevelComplete ? "cleared" : "stuck")} " +
                      $"in {level.ElapsedTime:0.0} s with {level.Deaths} deaths.");
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State, $"the bot should clear {scene}");
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator BotsPlayTheWholeGame()
        {
            SaveData.Clear();
            SceneManager.LoadScene(SceneNames.Level1);

            foreach (string scene in new[] { SceneNames.Level1, SceneNames.Level2, SceneNames.Level3 })
            {
                yield return WaitForLevel(scene);
                var player = LevelController.Current.Player;
                player.enabled = false;
                var bot = new LevelBot(player);
                yield return Play(bot.Step, 150f, scene);
                Assert.IsTrue(SaveData.HasProgress, "progress is saved for Continue");
            }

            yield return WaitForLevel(SceneNames.Boss);
            var hero = LevelController.Current.Player;
            hero.enabled = false;
            var bossBot = new BossBot(hero);
            yield return Play(bossBot.Step, 240f, SceneNames.Boss);

            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Ending, 30f, "the ending");
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<UI.EndingSequence>(), "the ending plays");
            Assert.AreEqual(GameState.Ending, GameManager.Instance.State);
            Assert.IsFalse(SaveData.HasProgress, "finishing the story clears Continue");
        }
    }
}
