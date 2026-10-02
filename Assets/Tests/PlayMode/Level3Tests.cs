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
    /// Level 3 (energy core) with the real scene: cracked plates, core locks, and the bot playing
    /// it from the entrance to the exit door.
    /// </summary>
    public class Level3Tests
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

        IEnumerator LoadLevel3()
        {
            SceneManager.LoadScene(SceneNames.Level3);
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            level = LevelController.Current;
            Assert.IsNotNull(level, "Level 3 should start");
            player = level.Player;
            yield return Steps(5);
        }

        static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        static CollapsingPlatform FirstPlate() =>
            Object.FindObjectsByType<CollapsingPlatform>(FindObjectsSortMode.None).OrderBy(p => p.transform.position.x).First();

        static ChargeLock LockNeeding(int charges) =>
            Object.FindObjectsByType<ChargeLock>(FindObjectsSortMode.None).Single(l => l.Required == charges);

        [UnityTest]
        public IEnumerator Level3HasItsPieces()
        {
            yield return LoadLevel3();
            Assert.AreEqual("ENERGY CORE", level.LevelTitle);
            Assert.AreEqual(0, player.Energy.Current, "Level 3 starts empty");
            Assert.AreEqual(7, Object.FindObjectsByType<CollapsingPlatform>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(LockNeeding(2));
            Assert.IsNotNull(LockNeeding(3));
            Assert.AreEqual(3, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(9, Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator PlateGivesWayUnderThePlayerAndComesBack()
        {
            yield return LoadLevel3();
            var plate = FirstPlate();

            player.Movement.Teleport(plate.transform.position + new Vector3(0f, 1f, 0f));
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(plate.IsCracking, "standing on a plate starts it cracking");

            player.Movement.Teleport(new Vector2(10f, 1f)); // step off onto solid floor
            yield return new WaitForSeconds(0.45f);
            Assert.IsTrue(plate.IsBroken, "the plate gives way even after the player has left it");

            yield return new WaitForSeconds(2.6f);
            Assert.IsFalse(plate.IsBroken, "a broken plate re-forms");
        }

        [UnityTest]
        public IEnumerator FallingThroughAPlateRestoresItOnRespawn()
        {
            yield return LoadLevel3();
            var plate = FirstPlate();

            player.Movement.Teleport(plate.transform.position + new Vector3(0f, 1f, 0f));
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(plate.IsBroken);

            float end = Time.time + 3f;
            while (player.IsAlive && Time.time < end)
                yield return null;
            Assert.AreEqual(1, level.Deaths, "the abyss below kills");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(plate.IsBroken, "the respawn brings the plate back at once");
        }

        [UnityTest]
        public IEnumerator LockOpensOnlyWhileThePlayerHoldsEnoughCharge()
        {
            yield return LoadLevel3();
            var chargeLock = LockNeeding(2);
            Vector2 before = chargeLock.transform.position + new Vector3(-2.5f, -1.5f, 0f); // clear of the cell at x 60
            player.Movement.Teleport(before);

            player.Energy.Initialize(1, 3);
            yield return Steps(3);
            Assert.IsFalse(chargeLock.IsOpen, "one charge isn't enough");

            player.Energy.Initialize(2, 3);
            yield return Steps(3);
            Assert.IsTrue(chargeLock.IsOpen, "two charges open it");

            player.Movement.Teleport(new Vector2(chargeLock.transform.position.x, before.y));
            yield return Steps(3);
            player.Energy.TrySpend();
            yield return Steps(3);
            Assert.IsTrue(chargeLock.IsOpen, "it never closes on the player inside it");

            player.Movement.Teleport(before);
            yield return Steps(3);
            Assert.IsFalse(chargeLock.IsOpen, "out of the field with one charge, it closes again");
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator BotPlaysLevel3FromStartToExit()
        {
            yield return LoadLevel3();
            var bot = new LevelBot(player);
            player.enabled = false;
            int logged = 0;
            level.PlayerDied += () =>
            {
                if (logged++ < 3)
                    Debug.Log($"[ECHO Test] Level 3 death at x = {player.transform.position.x:0.0}, y = {player.transform.position.y:0.0}, " +
                              $"energy {player.Energy.Current}");
            };

            float end = Time.time + 120f;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                bot.Step();
                yield return new WaitForFixedUpdate();
            }

            Debug.Log($"[ECHO Test] Level 3 bot: {(GameManager.Instance.State == GameState.LevelComplete ? "finished" : "stuck")} " +
                      $"at x = {player.transform.position.x:0.0} after {level.ElapsedTime:0.0} s with {level.Deaths} deaths.");
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State,
                $"the bot should reach the exit (stopped at x = {player.transform.position.x:0.0}, deaths {level.Deaths})");
        }
    }
}
