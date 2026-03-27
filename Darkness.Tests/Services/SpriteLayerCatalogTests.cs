using Darkness.Core.Models;
using Darkness.Core.Services;

namespace Darkness.Tests.Services
{
    public class SpriteLayerCatalogTests
    {
        private readonly SpriteLayerCatalog _catalog = new();

        [Fact]
        public void GetLayersForAppearance_ReturnsBodyHairArmorWeapon()
        {
            var appearance = new CharacterAppearance
            {
                SkinColor = "Light",
                HairStyle = "Long",
                HairColor = "Black",
                ArmorType = "Plate",
                WeaponType = "Longsword"
            };

            var layers = _catalog.GetLayersForAppearance(appearance);

            Assert.True(layers.Count >= 4);
            Assert.Contains(layers, l => l.ResourcePath.Contains("body/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("hair/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("armor/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("weapons/"));
        }

        [Fact]
        public void GetLayersForAppearance_LayersAreSortedByZOrder()
        {
            var appearance = new CharacterAppearance();
            var layers = _catalog.GetLayersForAppearance(appearance);

            for (int i = 1; i < layers.Count; i++)
            {
                Assert.True(layers[i].ZOrder >= layers[i - 1].ZOrder,
                    $"Layer at index {i} (z={layers[i].ZOrder}) should be >= layer at index {i - 1} (z={layers[i - 1].ZOrder})");
            }
        }

        [Fact]
        public void GetLayersForAppearance_BodyLayerIsLowestZOrder()
        {
            var appearance = new CharacterAppearance { SkinColor = "Tan" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Equal(layers.Min(l => l.ZOrder), bodyLayer.ZOrder);
        }

        [Fact]
        public void GetLayersForAppearance_SkinColorMapsToCorrectFile()
        {
            var appearance = new CharacterAppearance { SkinColor = "Tan" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Contains("tan", bodyLayer.ResourcePath.ToLower());
        }

        [Fact]
        public void GetLayersForAppearance_HairMapsToStyleAndColor()
        {
            var appearance = new CharacterAppearance { HairStyle = "Short", HairColor = "Blonde" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var hairLayer = layers.First(l => l.ResourcePath.Contains("hair/"));
            Assert.Contains("short", hairLayer.ResourcePath.ToLower());
            Assert.Contains("blonde", hairLayer.ResourcePath.ToLower());
        }

        [Theory]
        [InlineData("Warrior", "Plate", "Longsword")]
        [InlineData("Mage", "Robe", "Staff")]
        [InlineData("Rogue", "Leather", "Daggers")]
        public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment(string className, string expectedArmor, string expectedWeapon)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);

            Assert.Equal(expectedArmor, appearance.ArmorType);
            Assert.Equal(expectedWeapon, appearance.WeaponType);
        }

        [Fact]
        public void HairStyles_ContainsExpectedOptions()
        {
            Assert.Contains("Long", _catalog.HairStyles);
            Assert.Contains("Short", _catalog.HairStyles);
            Assert.Contains("Mohawk", _catalog.HairStyles);
            Assert.Contains("Messy", _catalog.HairStyles);
        }
    }
}
