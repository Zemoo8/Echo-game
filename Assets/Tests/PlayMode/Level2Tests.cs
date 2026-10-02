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
    /// Level 2 (security wing) with the real scene: drones, turrets, consoles, the shutter, and
    /// the bot playing it from the entrance to the exit door.
    /// </summary>
    public class Level2Tests
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

        IEnumerator LoadLevel2()
        {
            SceneManager.LoadScene(SceneNames.Level2);
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((LevelController.Current == null || GameManager.Instance.State != GameState.Playing)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;
            level = LevelController.Current;
            Assert.IsNotNull(level, "Level 2 should start");
            player = level.Player;
            yield return Steps(5);
        }

        static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Level2HasItsPieces()
        {
            yield return LoadLevel2();
            Assert.AreEqual("SECURITY WING", level.LevelTitle);
            Assert.AreEqual(1, player.Energy.Current, "Level 2 starts with one charge");
            Assert.AreEqual(2, Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Object.FindObjectsByType<ControlConsole>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, Object.FindObjectsByType<SecurityDoor>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, Object.FindObjectsByType<EnergyPickup>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator DashDestroysADroneAndADeathBringsItBack()
        {
            yield return LoadLevel2();
            var drone = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).OrderBy(d => d.transform.position.x).First();

            drone.OnDashHit(player.Dash);
            Assert.IsTrue(drone.IsDestroyed, "a dash destroys a drone");

            level.KillPlayer(); // no checkpoint reached yet: back to the start, drone and all
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(drone.IsDestroyed, "the drone is back after the respawn");
        }

        [UnityTest]
        public IEnumerator TouchingADroneKills()
        {
            yield return LoadLevel2();
            var drone = Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).OrderBy(d => d.transform.position.x).First();
            player.Movement.Teleport(drone.transform.position);
            yield return Steps(4);
            Assert.IsFalse(player.IsAlive, "touching a drone without dashing kills");
        }

        [UnityTest]
        public IEnumerator ConsoleOpensTheShutterAndADeathClosesIt()
        {
            yield return LoadLevel2();
            var door = Object.FindFirstObjectByType<SecurityDoor>();
            var console = Object.FindObjectsByType<ControlConsole>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).First();
            Assert.IsFalse(door.IsOpen);

            player.Movement.Teleport(console.transform.position + new Vector3(0f, 1f, 0f));
            yield return Steps(5);
            Assert.IsTrue(console.IsUsed);
            Assert.IsTrue(door.IsOpen, "the console opens the shutter");

            level.KillPlayer(); // the console was used after the last checkpoint
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(console.IsUsed);
            Assert.IsFalse(door.IsOpen, "the shutter closes again on respawn");
        }

        [UnityTest]
        [Timeout(150000)]
        public IEnumerator BotPlaysLevel2FromStartToExit()
        {
            yield return LoadLevel2();
            var bot = new LevelBot(player);
            player.enabled = false;
            int logged = 0;
            level.PlayerDied += () =>
            {
                if (logged++ < 3)
                    Debug.Log($"[ECHO Test] death at x = {player.transform.position.x:0.0}, y = {player.transform.position.y:0.0}, " +
                              $"energy {player.Energy.Current}, dashing {player.Dash.IsDashing}");
            };

            float end = Time.time + 100f;
            while (GameManager.Instance.State != GameState.LevelComplete && Time.time < end)
            {
                bot.Step();
                yield return new WaitForFixedUpdate();
            }

            Debug.Log($"[ECHO Test] Level 2 bot: {(GameManager.Instance.State == GameState.LevelComplete ? "finished" : "stuck")} " +
                      $"at x = {player.transform.position.x:0.0} after {level.ElapsedTime:0.0} s with {level.Deaths} deaths.");
            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State,
                $"the bot should reach the exit (stopped at x = {player.transform.position.x:0.0}, deaths {level.Deaths})");
        }
    }
}
