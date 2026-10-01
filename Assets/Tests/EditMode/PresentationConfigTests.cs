using System;
using Echo.Core;
using Echo.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Echo.Tests
{
    public class PresentationConfigTests
    {
        PresentationConfig config;

        [SetUp]
        public void SetUp() => config = ScriptableObject.CreateInstance<PresentationConfig>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(config);

        static bool Has(Feature set, Feature feature) => (set & feature) == feature;

        [Test]
        public void EverythingEnablesEveryFeature()
        {
            Feature everything = PresentationConfig.FeaturesFor(PresentationPhase.Everything);
            foreach (Feature feature in Enum.GetValues(typeof(Feature)))
                Assert.IsTrue(Has(everything, feature), $"{feature} should be on in Everything");
        }

        [Test]
        public void DevelopmentShowsCoreGameplayButNotLaterWork()
        {
            Feature development = PresentationConfig.FeaturesFor(PresentationPhase.Development);
            Assert.IsTrue(Has(development, Feature.Dash));
            Assert.IsTrue(Has(development, Feature.Energy));
            Assert.IsTrue(Has(development, Feature.TouchControls));
            Assert.IsTrue(Has(development, Feature.Level1));
            Assert.IsFalse(Has(development, Feature.Boss));
            Assert.IsFalse(Has(development, Feature.Animation));
            Assert.IsFalse(Has(development, Feature.GamePixAds));
        }

        [Test]
        public void OnlyTheGamePixPhaseShowsAds()
        {
            Assert.IsFalse(Has(PresentationConfig.FeaturesFor(PresentationPhase.FinalProduct), Feature.GamePixAds));
            Assert.IsTrue(Has(PresentationConfig.FeaturesFor(PresentationPhase.GamePix), Feature.GamePixAds));
            Assert.IsFalse(Has(PresentationConfig.FeaturesFor(PresentationPhase.GamePix), Feature.DevOverlay));
        }

        [Test]
        public void EarlyPhasesStartInTheirShowcaseScenes()
        {
            Assert.AreEqual(SceneNames.ConceptShowcase, PresentationConfig.StartSceneFor(PresentationPhase.Conception));
            Assert.AreEqual(SceneNames.DesignShowcase, PresentationConfig.StartSceneFor(PresentationPhase.Design));
            Assert.AreEqual(SceneNames.MainMenu, PresentationConfig.StartSceneFor(PresentationPhase.Development));
            Assert.AreEqual(SceneNames.MainMenu, PresentationConfig.StartSceneFor(PresentationPhase.Everything));
        }

        [Test]
        public void ForceOverridesWinOverThePhase()
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("phase").intValue = (int)PresentationPhase.Development;
            serialized.FindProperty("forceEnabled").intValue = (int)Feature.Boss;
            serialized.FindProperty("forceDisabled").intValue = (int)Feature.Dash;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(Has(config.EnabledFeatures, Feature.Boss));
            Assert.IsFalse(Has(config.EnabledFeatures, Feature.Dash));
            Assert.IsTrue(Has(config.EnabledFeatures, Feature.Energy));
        }
    }
}
