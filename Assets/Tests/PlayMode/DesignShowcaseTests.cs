using System.Collections;
using Echo.Core;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echo.Tests
{
    /// <summary>The presentation phase 3 slide deck: browsing, wrap-around and camera moves.</summary>
    public class DesignShowcaseTests
    {
        [SetUp]
        public void SetUp() => PresentationMode.Override(ScriptableObject.CreateInstance<PresentationConfig>()); // Everything

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
    }
}
