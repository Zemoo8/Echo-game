using Echo.Player;
using NUnit.Framework;
using UnityEngine;

namespace Echo.Tests
{
    public class EnergySystemTests
    {
        GameObject owner;
        EnergySystem energy;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Energy");
            energy = owner.AddComponent<EnergySystem>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(owner);

        [Test]
        public void InitializeClampsToCapacity()
        {
            energy.Initialize(9, 5);
            Assert.AreEqual(5, energy.Current);
            Assert.IsTrue(energy.IsFull);
        }

        [Test]
        public void SpendingFailsWithoutEnoughEnergy()
        {
            energy.Initialize(1, 5);
            Assert.IsTrue(energy.TrySpend(1));
            Assert.IsFalse(energy.TrySpend(1));
            Assert.AreEqual(0, energy.Current);
        }

        [Test]
        public void AddingStopsAtCapacityAndReportsWhatWasAdded()
        {
            energy.Initialize(4, 5);
            Assert.AreEqual(1, energy.Add(3));
            Assert.AreEqual(5, energy.Current);
            Assert.AreEqual(0, energy.Add(1));
        }

        [Test]
        public void ChangedReportsCurrentAndMax()
        {
            energy.Initialize(2, 5);
            int reportedCurrent = -1, reportedMax = -1;
            energy.Changed += (current, max) => { reportedCurrent = current; reportedMax = max; };

            energy.TrySpend(1);

            Assert.AreEqual(1, reportedCurrent);
            Assert.AreEqual(5, reportedMax);
        }
    }
}
