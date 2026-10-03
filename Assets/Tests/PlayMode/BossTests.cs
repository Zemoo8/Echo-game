using System;
using System.Collections;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echo.Tests
{
    /// <summary>
    /// The Guardian's arena with the real scene: the fight's rules (open shell takes hits, closed
    /// shell deflects, a death restarts only the current phase, six hits win and open the exit),
    /// a bot that wins the fight, and the ending that follows.
    /// </summary>
    public class BossTests
    {
        LevelController level;
        PlayerController player;
        Guardian guardian;

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

        IEnumerator LoadArena()
        {
            SceneManager.LoadScene(SceneNames.Boss);
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            level = LevelController.Current;
            Assert.IsNotNull(level, "the arena should start");
            player = level.Player;
            guardian = Guardian.Active;
            Assert.IsNotNull(guardian, "the arena has a Guardian");
            yield return new WaitForFixedUpdate();
        }

        // Takes the player out of the physics world, so the rules can be tested without dodging.
        void Shelter(bool sheltered) => player.GetComponent<Rigidbody2D>().simulated = !sheltered;

        static IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return null;
            Assert.IsTrue(condition(), $"timed out waiting for {what}");
        }

        IEnumerator LandHit()
        {
            yield return WaitFor(() => guardian.IsVulnerable, 15f, "the shell to open");
            int before = guardian.Hits;
            player.Energy.Initialize(1, 3);
            guardian.OnDashHit(player.Dash);
            Assert.AreEqual(before + 1, guardian.Hits, "a dash into the open shell is a hit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArenaHasItsPieces()
        {
            yield return LoadArena();
            Assert.AreEqual("THE GUARDIAN", level.LevelTitle);
            Assert.AreEqual(1, player.Energy.Current, "the fight starts with one charge");
            Assert.AreEqual(3, Object.FindObjectsByType<CellRespawner>(FindObjectsSortMode.None).Length);
            Assert.IsFalse(Object.FindFirstObjectByType<SecurityDoor>().IsOpen, "the exit is shut while the Guardian stands");
            Assert.IsNotNull(Object.FindFirstObjectByType<BreakableBarrier>());
            Assert.AreEqual(0, guardian.Hits);
            Assert.IsFalse(guardian.IsDefeated);
        }

        [UnityTest]
        public IEnumerator ClosedShellDeflectsAndOpenShellTakesTheHit()
        {
            yield return LoadArena();
            Shelter(true);
            yield return WaitFor(() => guardian.IsAwake, 6f, "the Guardian to wake");

            guardian.OnDashHit(player.Dash);
            Assert.AreEqual(0, guardian.Hits, "the closed shell takes no damage");

            yield return LandHit();
            Assert.IsFalse(guardian.IsVulnerable, "one hit per opening");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DeathRestartsOnlyTheCurrentPhase()
        {
            yield return LoadArena();
            Shelter(true);
            for (int i = 0; i < 3; i++)
                yield return LandHit();
            Assert.AreEqual(1, guardian.Phase);

            level.KillPlayer();
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(2, guardian.Hits, "the hit in phase 2 is undone; phase 1's stay");
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator SixHitsDefeatTheGuardianAndOpenTheWayOut()
        {
            yield return LoadArena();
            Shelter(true);
            for (int i = 0; i < Guardian.HitsToDefeat; i++)
                yield return LandHit();
            Assert.IsTrue(guardian.IsDefeated);

            var shutter = Object.FindFirstObjectByType<SecurityDoor>();
            yield return WaitFor(() => shutter.IsOpen, 5f, "the exit shutter to open");

            // Walk up to the exit and dash through it.
            Shelter(false);
            player.enabled = false;
            player.Movement.Teleport(new Vector2(6.5f, 1f));
            player.Energy.Initialize(1, 3);
            yield return new WaitForSeconds(0.3f);
            player.Movement.SetMoveInput(1f);
            player.Dash.RequestDash();
            yield return WaitFor(() => GameManager.Instance.State == GameState.LevelComplete, 3f, "the level to complete");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator BotDefeatsTheGuardian()
        {
            yield return LoadArena();
            var bot = new BossBot(player);
            player.enabled = false;
            int logged = 0;
            level.PlayerDied += () =>
            {
                if (logged++ < 5)
                    Debug.Log($"[ECHO Test] Boss death at x = {player.transform.position.x:0.0}, y = {player.transform.position.y:0.0}, " +
                              $"phase {guardian.Phase + 1}, hits {guardian.Hits}");
            };

            float end = Time.time + 240f;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                bot.Step();
                yield return new WaitForFixedUpdate();
            }

            Debug.Log($"[ECHO Test] Boss bot: {(GameManager.Instance.State == GameState.LevelComplete ? "won" : "stuck")} " +
                      $"after {level.ElapsedTime:0.0} s with {level.Deaths} deaths (Guardian hits {guardian.Hits}).");
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State,
                $"the bot should beat the Guardian and leave (hits {guardian.Hits}, deaths {level.Deaths})");
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator EndingPlaysAndReturnsToTheMenu()
        {
            SaveData.SetContinue(SceneNames.Boss);
            SceneManager.LoadScene(SceneNames.Ending);
            yield return null;
            yield return null;
            Assert.AreEqual(GameState.Ending, GameManager.Instance.State);
            Assert.IsFalse(SaveData.HasProgress, "finishing the story clears Continue");

            // Nobody presses anything: the captions and the message play out, then it waits on THE END.
            var sequence = Object.FindFirstObjectByType<UI.EndingSequence>();
            Assert.IsNotNull(sequence);
            yield return WaitFor(() => sequence.AtTheEnd, 45f, "THE END");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(SceneNames.Ending, SceneManager.GetActiveScene().name, "it waits for a press before leaving");
        }
    }
}
