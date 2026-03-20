using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PeakClassMod.Tests
{
    [TestFixture]
    public class PeakClassModTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset state before each test
            ClassManager.ResetAllFlags();
        }

        [Test]
        public void BuildStartItemsByHatName_ShouldReturnCorrectItems_ForMushroomHat()
        {
            // Act
            List<string> items = PeakClassModPlugin.BuildStartItemsByHatName("Hat_Mushroom");

            // Assert
            Assert.That(items, Is.Not.Null);
            Assert.That(items.Count, Is.EqualTo(3));
            foreach (var item in items)
            {
                Assert.That(item, Does.StartWith("Shroomberry_"));
            }
        }

        [Test]
        public void BuildStartItemsByHatName_ShouldReturnCorrectItems_ForMedicHat()
        {
            // Act
            List<string> items = PeakClassModPlugin.BuildStartItemsByHatName("Hat_Medic");

            // Assert
            Assert.That(items, Is.Not.Null);
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items, Contains.Item("FirstAidKit"));
            Assert.That(items, Contains.Item("Cure-All"));
        }

        [Test]
        public void BuildStartItemsByHatName_ShouldReturnCorrectItems_ForAstronautHat()
        {
            // Act
            List<string> items = PeakClassModPlugin.BuildStartItemsByHatName("Hat_Astronaut");

            // Assert
            Assert.That(items, Is.Not.Null);
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items, Contains.Item("BalloonBunch"));
        }

        [Test]
        public void BuildStartItemsByHatName_ShouldReturnEmptyList_ForUnknownHat()
        {
            // Act
            List<string> items = PeakClassModPlugin.BuildStartItemsByHatName("Hat_Unknown");

            // Assert
            Assert.That(items, Is.Not.Null);
            Assert.That(items, Is.Empty);
        }

        [Test]
        public void BuildStartItemsByHatName_ShouldReturnEmptyList_ForNullHat()
        {
            // Act
            List<string> items = PeakClassModPlugin.BuildStartItemsByHatName(null);

            // Assert
            Assert.That(items, Is.Not.Null);
            Assert.That(items, Is.Empty);
        }

        [Test]
        public void ClassManager_ActivateScoutClass_ShouldSetActiveFlagAndResetOthers()
        {
            // Arrange
            ClassManager.ActivateGobelinClass(); // Set something else first
            Assert.That(ClassManager.IsGobelinActive(), Is.True);
            Assert.That(ClassManager.IsScoutActive(), Is.False);

            // Act
            ClassManager.ActivateScoutClass();

            // Assert
            Assert.That(ClassManager.IsScoutActive(), Is.True);
            Assert.That(ClassManager.IsGobelinActive(), Is.False);
            Assert.That(PeakClassModPlugin.currentClass, Is.EqualTo("Scout"));
        }

        [Test]
        public void ClassManager_DeactivateAllClasses_ShouldResetAllFlags()
        {
            // Arrange
            ClassManager.ActivateMedicClass();
            Assert.That(ClassManager.IsMedicActive(), Is.True);

            // Act
            ClassManager.DeactivateAllClasses();

            // Assert
            Assert.That(ClassManager.IsMedicActive(), Is.False);
            Assert.That(ClassManager.IsScoutActive(), Is.False);
            Assert.That(ClassManager.IsGobelinActive(), Is.False);
            Assert.That(ClassManager.IsTechPriestActive(), Is.False);
            Assert.That(PeakClassModPlugin.currentClass, Is.EqualTo("None"));
        }

        [Test]
        public void GobelinDiet_IsAllowedFood_ShouldReturnTrue_WhenItemIsNull()
        {
            // To be safe, we will just test that passing null to GobelinDiet.IsAllowedFood returns true based on source code logic
            bool result = GobelinDiet.IsAllowedFood(null);
            Assert.That(result, Is.True);
        }
    }
}
