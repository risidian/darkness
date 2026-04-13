using Darkness.Core.Models;
using Xunit;
using System.Collections.Generic;

namespace Darkness.Tests.Models
{
    public class ItemRequirementsTests
    {
        [Fact]
        public void CanEquip_ReturnsTrue_WhenRequirementsMet()
        {
            // Arrange
            var character = new Character { BaseStrength = 15, Level = 5 };
            var item = new Item { RequiredStrength = 10, RequiredLevel = 1 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.True(result);
            Assert.Empty(missing);
        }

        [Fact]
        public void CanEquip_ReturnsFalse_WhenStrengthNotMet()
        {
            // Arrange
            var character = new Character { BaseStrength = 10 };
            var item = new Item { RequiredStrength = 15 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.False(result);
            Assert.Contains("Strength 15", missing);
        }

        [Fact]
        public void CanEquip_ReturnsFalse_WhenDexterityNotMet()
        {
            // Arrange
            var character = new Character { BaseDexterity = 10 };
            var item = new Item { RequiredDexterity = 15 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.False(result);
            Assert.Contains("Dexterity 15", missing);
        }

        [Fact]
        public void CanEquip_ReturnsFalse_WhenIntelligenceNotMet()
        {
            // Arrange
            var character = new Character { BaseIntelligence = 10 };
            var item = new Item { RequiredIntelligence = 15 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.False(result);
            Assert.Contains("Intelligence 15", missing);
        }

        [Fact]
        public void CanEquip_ReturnsFalse_WhenLevelNotMet()
        {
            // Arrange
            var character = new Character { Level = 5 };
            var item = new Item { RequiredLevel = 10 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.False(result);
            Assert.Contains("Level 10", missing);
        }

        [Fact]
        public void CanEquip_ReturnsMultipleMissing_WhenSeveralNotMet()
        {
            // Arrange
            var character = new Character { BaseStrength = 10, Level = 5 };
            var item = new Item { RequiredStrength = 15, RequiredLevel = 10 };

            // Act
            var result = item.CanEquip(character, out var missing);

            // Assert
            Assert.False(result);
            Assert.Equal(2, missing.Count);
            Assert.Contains("Strength 15", missing);
            Assert.Contains("Level 10", missing);
        }
    }
}
