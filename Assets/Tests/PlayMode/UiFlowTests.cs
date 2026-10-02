using System.Collections;
using System.Linq;
using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Presentation;
using Echo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Echo.Tests
{
    /// <summary>
    /// Loads the movement sandbox with the real Systems prefab and checks the interface around a
    /// level: the title card, the energy meter, the pause menu and the sound switches.
    /// </summary>
    public class UiFlowTests
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
            // Leave an empty scene behind so other tests don't run inside the sandbox.
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

        static IEnumerator LoadSandbox()
        {
            SceneManager.LoadScene(SceneNames.DevSandbox);
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.IsNotNull(LevelController.Current, "the sandbox should start a level");
        }

        [UnityTest]
        public IEnumerator TitleCardNamesTheLevelWhenItStarts()
        {
            yield return LoadSandbox();
            yield return new WaitForSecondsRealtime(1f);

            var card = Object.FindFirstObjectByType<LevelIntroCard>();
            Assert.IsNotNull(card, "the Systems UI should have a level title card");
            Assert.Greater(card.GetComponent<CanvasGroup>().alpha, 0.5f, "the title card should be showing a second in");
            var title = card.GetComponentsInChildren<Text>().First(t => t.name == "Title");
            Assert.AreEqual(LevelController.Current.LevelTitle, title.text);
        }

        [UnityTest]
        public IEnumerator PlayFromTheMainMenuStartsTheLevelWithItsTitleCard()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
            yield return null;
            yield return null;
            var play = Object.FindObjectsByType<MenuButton>(FindObjectsSortMode.None).First(b => b.name == "PLAY");
            play.Button.onClick.Invoke();

            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.IsNotNull(LevelController.Current, "Play should load a level");

            yield return new WaitForSecondsRealtime(1f);
            var card = Object.FindFirstObjectByType<LevelIntroCard>();
            Assert.Greater(card.GetComponent<CanvasGroup>().alpha, 0.5f, "the title card should be showing a second in");
            Assert.Less(ScreenFader.Instance.Alpha, 0.01f, "the screen should have faded back in");
        }

        [UnityTest]
        public IEnumerator EnergyMeterHasOneCellPerCharge()
        {
            yield return LoadSandbox();
            yield return null;

            var energy = LevelController.Current.Player.Energy;
            var cells = Object.FindObjectsByType<EnergyCellView>(FindObjectsSortMode.None);
            Assert.AreEqual(energy.Max, cells.Length, "one visible cell per energy charge");
        }

        [UnityTest]
        public IEnumerator PauseOpensTheMenuAndResumeClosesIt()
        {
            yield return LoadSandbox();
            var game = GameManager.Instance;
            var menu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            var root = menu.transform.Find("Root").gameObject;
            Assert.IsFalse(root.activeSelf, "the pause menu starts hidden");

            game.Pause();
            yield return null;
            Assert.IsTrue(root.activeSelf, "pausing shows the menu");
            Assert.AreEqual(0f, Time.timeScale, "the game is frozen while paused");

            game.Resume();
            yield return null;
            Assert.IsFalse(root.activeSelf, "resuming hides the menu");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator DyingFlashesTheScreenAndRespawnsTheHero()
        {
            yield return LoadSandbox();
            var level = LevelController.Current;
            var vignette = Object.FindFirstObjectByType<DamageFlash>().GetComponent<Image>();

            level.KillPlayer();
            yield return null;
            Assert.IsTrue(vignette.enabled, "dying flashes the red vignette");
            Assert.IsFalse(level.Player.IsAlive);

            yield return new WaitForSeconds(1f);
            Assert.IsTrue(level.Player.IsAlive, "the hero respawns");
            Assert.AreEqual(1, level.Deaths);
            Assert.Less(ScreenFader.Instance.Alpha, 0.01f, "the respawn fade has cleared");
        }

        [UnityTest]
        public IEnumerator CompletingTheLevelShowsTheCardThenMovesOn()
        {
            yield return LoadSandbox();
            var level = LevelController.Current;
            int levelId = level.GetInstanceID();

            level.CompleteLevel();
            yield return null;
            var card = Object.FindFirstObjectByType<LevelCompleteCard>().transform.Find("Card").gameObject;
            Assert.IsTrue(card.activeSelf, "the level-complete card shows");
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State);

            // The sandbox has no next scene, so it replays itself when the countdown ends.
            float timeout = Time.realtimeSinceStartup + 7f;
            while ((LevelController.Current == null || LevelController.Current.GetInstanceID() == levelId)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.IsNotNull(LevelController.Current, "the next level should load");
            Assert.AreNotEqual(levelId, LevelController.Current.GetInstanceID());
            Assert.IsFalse(card.activeSelf, "the card hides when the next level starts");
        }

        [Test]
        public void SoundSwitchesAreSavedInPlayerPrefs()
        {
            bool music = SoundSettings.MusicOn;
            try
            {
                SoundSettings.MusicOn = false;
                Assert.AreEqual(0, PlayerPrefs.GetInt("echo.music", 1));
                SoundSettings.MusicOn = true;
                Assert.AreEqual(1, PlayerPrefs.GetInt("echo.music", 0));
            }
            finally
            {
                SoundSettings.MusicOn = music;
            }
        }
    }
}
