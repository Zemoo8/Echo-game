using System.Collections;
using System.Linq;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echo.Tests
{
    /// <summary>
    /// Level 1 with the real scene: its pieces, checkpoints, lasers, and a bot that plays it from
    /// the cryo pod to the exit door using only what a player can do (run, jump, dash).
    /// </summary>
    public class Level1Tests
    {
        LevelController level;
        PlayerController player;

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

        IEnumerator LoadLevel1()
        {
            SceneManager.LoadScene(SceneNames.Level1);
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            level = LevelController.Current;
            Assert.IsNotNull(level, "Level 1 should start");
            player = level.Player;
            yield return Steps(5);
        }

        static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Level1HasItsPieces()
        {
            yield return LoadLevel1();
            Assert.AreEqual("ABANDONED LAB", level.LevelTitle);
            Assert.AreEqual(0, player.Energy.Current, "Level 1 starts with no energy");
            Assert.AreEqual(5, Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None).Length);
            var barriers = Object.FindObjectsByType<BreakableBarrier>(FindObjectsSortMode.None);
            Assert.AreEqual(1, barriers.Count(b => b.IsLevelExit), "one exit door");
        }

        [UnityTest]
        public IEnumerator FirstHintTeachesMovement()
        {
            yield return LoadLevel1();
            yield return new WaitForSecondsRealtime(0.8f);
            var hints = Object.FindFirstObjectByType<Echo.UI.HintDisplay>();
            Assert.IsNotNull(hints, "the HUD has a hint panel");
            Assert.Greater(hints.GetComponent<CanvasGroup>().alpha, 0.5f, "a hint shows at the start");
            StringAssert.Contains("move", hints.GetComponentInChildren<UnityEngine.UI.Text>().text);
        }

        [UnityTest]
        public IEnumerator CheckpointBringsBackItsEnergyAndLaterPickups()
        {
            yield return LoadLevel1();
            var checkpoint = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).First();
            var laterCell = Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None)
                .Where(p => p.transform.position.x > checkpoint.transform.position.x).OrderBy(p => p.transform.position.x).First();

            player.Energy.Initialize(2, 3);
            player.Movement.Teleport(checkpoint.RespawnPosition);
            yield return Steps(5);
            Assert.AreEqual(checkpoint, level.LastCheckpoint, "walking past the beacon activates it");

            player.Energy.TrySpend();
            player.Movement.Teleport(laterCell.transform.position);
            yield return Steps(5);
            Assert.IsTrue(laterCell.IsCollected);
            Assert.AreEqual(2, player.Energy.Current);

            level.KillPlayer();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(player.IsAlive, "the hero respawns");
            Assert.Less(Vector2.Distance(player.transform.position, checkpoint.RespawnPosition), 1.5f, "at the checkpoint");
            Assert.AreEqual(2, player.Energy.Current, "with the energy she had at the checkpoint");
            Assert.IsFalse(laterCell.IsCollected, "cells taken after the checkpoint come back");
        }

        [UnityTest]
        public IEnumerator LaserKillsOnlyWhileOn()
        {
            yield return LoadLevel1();
            var gate = Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None).OrderBy(g => g.transform.position.x).First();
            var spot = new Vector2(gate.transform.position.x, 1f);

            yield return WaitFor(() => gate.IsOn, 5f);
            yield return WaitFor(() => !gate.IsOn, 5f); // the start of an off window
            player.Movement.Teleport(spot);
            yield return Steps(6);
            Assert.IsTrue(player.IsAlive, "a switched-off laser is safe");

            yield return WaitFor(() => gate.IsOn, 5f);
            player.Movement.Teleport(spot + new Vector2(0f, 0.01f));
            yield return Steps(6);
            Assert.IsFalse(player.IsAlive, "a switched-on laser kills");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator BotPlaysLevel1FromStartToExit()
        {
            yield return LoadLevel1();
            var bot = new LevelBot(player);
            player.enabled = false; // the bot drives movement and dash directly instead of input devices

            float end = Time.time + 75f;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                bot.Step();
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State,
                $"the bot should reach the exit (stopped at x = {player.transform.position.x:0.0}, deaths {level.Deaths})");
            Assert.AreEqual(0, level.Deaths, "a clean run needs no deaths");
            Assert.GreaterOrEqual(player.Energy.Current, 0);
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end)
                yield return new WaitForFixedUpdate();
        }
    }
}
