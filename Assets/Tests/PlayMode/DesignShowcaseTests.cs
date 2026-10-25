using System.Collections;
using System.Linq;
using Echo.Core;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echo.Tests
{
    /// <summary>
    /// The presentation slide decks: the design deck (phase 3) browses, wraps and moves the camera;
    /// the concept deck shows the pitch in phase 1 and adds the diagrams in phase 2.
    /// </summary>
    public class DesignShowcaseTests
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

        [UnityTest]
        public IEnumerator SlidesBrowseWrapAndMoveTheCamera()
        {
            SceneManager.LoadScene(SceneNames.DesignShowcase);
            yield return null;
            yield return null;

            var showcase = Object.FindFirstObjectByType<DesignShowcase>();
            Assert.IsNotNull(showcase, "the showcase scene has its slide deck");
            Assert.GreaterOrEqual(showcase.SlideCount, 8, "palette, hero, threats, four biomes and the interface");
            Assert.AreEqual(0, showcase.Index);

            var camera = Camera.main;
            float startX = camera.transform.position.x;

            showcase.Go(2); // the hero studio, a short pan away
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(2, showcase.Index);
            Assert.Greater(Mathf.Abs(camera.transform.position.x - startX), 0.5f, "the camera moved to the hero studio");

            showcase.Go(-1);
            yield return null;
            Assert.AreEqual(showcase.SlideCount - 1, showcase.Index, "going back from the first slide wraps to the last");
            showcase.Go(showcase.SlideCount);
            yield return null;
            Assert.AreEqual(0, showcase.Index, "going past the last slide wraps to the first");
        }

        IEnumerator LoadConcept(PresentationPhase phase)
        {
            PresentationMode.Override(PresentationConfig.ForPhase(phase));
            SceneManager.LoadScene(SceneNames.ConceptShowcase);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConceptDeckShowsOnlyThePitchInPhase1()
        {
            yield return LoadConcept(PresentationPhase.Conception);
            var showcase = Object.FindFirstObjectByType<DesignShowcase>();
            Assert.IsNotNull(showcase, "the concept scene has its slide deck");
            Assert.AreEqual(6, showcase.SlideCount, "title, pitch, core loop, story, platform, scope");
            // GameObject.Find skips inactive objects, which is exactly what the button should be here.
            var menu = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "Menu");
            Assert.IsNotNull(menu, "the deck has its menu button");
            Assert.IsFalse(menu.gameObject.activeInHierarchy, "phase 1 has no main menu to go to");
        }

        [UnityTest]
        public IEnumerator ConceptDeckAddsTheDiagramsInPhase2()
        {
            yield return LoadConcept(PresentationPhase.Diagrams);
            var showcase = Object.FindFirstObjectByType<DesignShowcase>();
            Assert.AreEqual(12, showcase.SlideCount, "the pitch plus six diagrams");
            showcase.Go(9);
            yield return null;
            Assert.IsTrue(GameObject.Find("GuardianOverlay") != null, "the Guardian's state machine is on screen");
        }
    }
}
