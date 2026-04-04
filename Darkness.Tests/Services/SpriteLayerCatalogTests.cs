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
                ArmorType = "Plate (Steel)",
                WeaponType = "Arming Sword (Steel)"
            };

            var layers = _catalog.GetLayersForAppearance(appearance);

            Assert.True(layers.Count >= 4);
            Assert.Contains(layers, l => l.ResourcePath.Contains("body/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("hair/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("armor/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("weapons/"));
        }

        [Fact]
        public void GetLayersForAppearance_NoneWeapon_ExcludesWeaponLayer()
        {
            var appearance = new CharacterAppearance { WeaponType = "None" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            Assert.DoesNotContain(layers, l => l.ResourcePath.Contains("weapons/"));
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
            var appearance = new CharacterAppearance { SkinColor = "Amber" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Equal(layers.Min(l => l.ZOrder), bodyLayer.ZOrder);
        }

        [Fact]
        public void GetLayersForAppearance_SkinColorMapsToCorrectFile()
        {
            var appearance = new CharacterAppearance { SkinColor = "Bronze" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Contains("bronze", bodyLayer.ResourcePath);
        }

        [Fact]
        public void GetLayersForAppearance_HairMapsToStyleAndColor()
        {
            var appearance = new CharacterAppearance { HairStyle = "Spiked", HairColor = "Blonde" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var hairLayer = layers.First(l => l.ResourcePath.Contains("hair/"));
            Assert.Contains("spiked", hairLayer.ResourcePath);
            Assert.Contains("blonde", hairLayer.ResourcePath);
        }

        [Theory]
        [InlineData("Warrior", "Plate (Steel)", "Arming Sword (Steel)")]
        [InlineData("Mage", "Mage Robes (Blue)", "Mage Wand")]
        [InlineData("Rogue", "Leather (Black)", "Dagger (Steel)")]
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
            Assert.Contains("Plain", _catalog.HairStyles);
            Assert.Contains("Spiked", _catalog.HairStyles);
            Assert.Contains("Bob", _catalog.HairStyles);
        }

        [Fact]
        public void SkinColors_ContainsRealisticTones()
        {
            Assert.Contains("Light", _catalog.SkinColors);
            Assert.Contains("Amber", _catalog.SkinColors);
            Assert.Contains("Bronze", _catalog.SkinColors);
            Assert.Equal(7, _catalog.SkinColors.Count);
        }

        [Fact]
        public void HairColors_ContainsExpectedOptions()
        {
            Assert.Contains("Blonde", _catalog.HairColors);
            Assert.Contains("Dark Brown", _catalog.HairColors);
            Assert.Contains("Redhead", _catalog.HairColors);
            Assert.Equal(11, _catalog.HairColors.Count);
        }
    }
}
