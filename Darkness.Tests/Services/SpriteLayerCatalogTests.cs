using Darkness.Core.Models;
using Darkness.Core.Services;

namespace Darkness.Tests.Services
{
    public class SpriteLayerCatalogTests
    {
        private readonly SpriteLayerCatalog _catalog = new();

        // ─── GetLayersForAppearance ───────────────────────────────────

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

        [Fact]
        public void GetLayersForAppearance_RespectsFaceSelection()
        {
            var appearance = new CharacterAppearance { Face = "Female" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var faceLayer = layers.First(l => l.ResourcePath.Contains("face/"));
            Assert.Contains("female", faceLayer.ResourcePath);
        }

        [Fact]
        public void GetLayersForAppearance_RespectsEyesSelection()
        {
            var appearance = new CharacterAppearance { Eyes = "Anger" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var eyesLayer = layers.First(l => l.ResourcePath.Contains("eyes/"));
            Assert.Contains("anger", eyesLayer.ResourcePath);
        }

        [Theory]
        [InlineData("Arming Sword (Steel)", "arming_sword_steel")]
        [InlineData("Arming Sword (Iron)", "arming_sword_iron")]
        [InlineData("Arming Sword (Gold)", "arming_sword_gold")]
        public void GetLayersForAppearance_ArmingSword_GeneratesWeaponLayer(string weapon, string expectedFile)
        {
            var appearance = new CharacterAppearance { WeaponType = weapon };
            var layers = _catalog.GetLayersForAppearance(appearance);

            Assert.Contains(layers, l => l.ResourcePath.Contains(expectedFile));
        }

        // ─── GetStitchLayers ─────────────────────────────────────────

        [Fact]
        public void GetStitchLayers_ReturnsBodyHeadFaceEyesHair_AtMinimum()
        {
            var appearance = new CharacterAppearance();
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.Contains(layers, l => l.RootPath.Contains("body/"));
            Assert.Contains(layers, l => l.RootPath.Contains("head/"));
            Assert.Contains(layers, l => l.RootPath.Contains("face/"));
            Assert.Contains(layers, l => l.RootPath.Contains("eyes/"));
            Assert.Contains(layers, l => l.RootPath.Contains("hair/"));
        }

        [Theory]
        [InlineData("Arming Sword (Steel)", "steel")]
        [InlineData("Arming Sword (Iron)", "iron")]
        [InlineData("Arming Sword (Gold)", "gold")]
        public void GetStitchLayers_ArmingSword_GeneratesWeaponLayer(string weapon, string expectedMaterial)
        {
            var appearance = new CharacterAppearance { WeaponType = weapon };
            var layers = _catalog.GetStitchLayers(appearance);

            var weaponLayer = layers.FirstOrDefault(l => l.RootPath.Contains("weapons/sword/arming"));
            Assert.NotNull(weaponLayer);
            Assert.Contains(expectedMaterial, weaponLayer!.FileNameTemplate);
            Assert.Contains("{action}", weaponLayer.FileNameTemplate);
        }

        [Fact]
        public void GetStitchLayers_Dagger_GeneratesWeaponLayerWithActionTemplate()
        {
            var appearance = new CharacterAppearance { WeaponType = "Dagger (Steel)" };
            var layers = _catalog.GetStitchLayers(appearance);

            var weaponLayer = layers.FirstOrDefault(l => l.RootPath.Contains("weapons/sword/dagger"));
            Assert.NotNull(weaponLayer);
            Assert.Contains("{action}", weaponLayer!.FileNameTemplate);
        }

        [Fact]
        public void GetStitchLayers_Wand_UsesGenderVariable()
        {
            var maleLayers = _catalog.GetStitchLayers(new CharacterAppearance { WeaponType = "Mage Wand", Head = "Human Male" });
            var femaleLayers = _catalog.GetStitchLayers(new CharacterAppearance { WeaponType = "Mage Wand", Head = "Human Female" });

            var maleWand = maleLayers.First(l => l.RootPath.Contains("wand"));
            var femaleWand = femaleLayers.First(l => l.RootPath.Contains("wand"));

            Assert.Contains("/male/", maleWand.RootPath);
            Assert.Contains("/female/", femaleWand.RootPath);
        }

        [Fact]
        public void GetStitchLayers_NoneWeapon_ExcludesWeaponLayer()
        {
            var appearance = new CharacterAppearance { WeaponType = "None" };
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.DoesNotContain(layers, l => l.RootPath.Contains("weapons/"));
        }

        [Theory]
        [InlineData("Longsleeve (White)")]
        [InlineData("Longsleeve (Blue)")]
        [InlineData("Longsleeve (Brown)")]
        public void GetStitchLayers_Longsleeve_GeneratesArmorLayer(string armor)
        {
            var appearance = new CharacterAppearance { ArmorType = armor };
            var layers = _catalog.GetStitchLayers(appearance);

            // Longsleeves map to closest robe assets since no dedicated longsleeve full-sprites exist
            var armorLayer = layers.FirstOrDefault(l => l.RootPath.Contains("torso/robes"));
            Assert.NotNull(armorLayer);
            Assert.Contains("{action}", armorLayer!.FileNameTemplate);
        }

        [Theory]
        [InlineData("Mage Robes (Blue)", "blue")]
        [InlineData("Mage Robes (Red)", "red")]
        [InlineData("Mage Robes (White)", "white")]
        public void GetStitchLayers_MageRobes_GeneratesArmorLayer(string armor, string expectedColor)
        {
            var appearance = new CharacterAppearance { ArmorType = armor };
            var layers = _catalog.GetStitchLayers(appearance);

            var armorLayer = layers.FirstOrDefault(l => l.RootPath.Contains("torso/robes"));
            Assert.NotNull(armorLayer);
            Assert.Contains(expectedColor, armorLayer!.RootPath);
        }

        [Theory]
        [InlineData("Plate (Steel)", "plate")]
        [InlineData("Plate (Iron)", "plate")]
        [InlineData("Plate (Gold)", "plate")]
        [InlineData("Leather", "leather")]
        [InlineData("Leather (Black)", "leather")]
        [InlineData("Leather (Brown)", "leather")]
        public void GetStitchLayers_StandardArmor_GeneratesArmorLayer(string armor, string expectedDir)
        {
            var appearance = new CharacterAppearance { ArmorType = armor };
            var layers = _catalog.GetStitchLayers(appearance);

            var armorLayer = layers.FirstOrDefault(l => l.RootPath.Contains($"armor/{expectedDir}"));
            Assert.NotNull(armorLayer);
            Assert.Contains("{action}", armorLayer!.FileNameTemplate);
        }

        [Theory]
        [InlineData("Crusader")]
        [InlineData("Spartan")]
        public void GetStitchLayers_Shield_GeneratesLayerWithActionTemplate(string shield)
        {
            var appearance = new CharacterAppearance { ShieldType = shield };
            var layers = _catalog.GetStitchLayers(appearance);

            var shieldLayer = layers.FirstOrDefault(l => l.RootPath.Contains("shields/"));
            Assert.NotNull(shieldLayer);
            Assert.Contains("{action}", shieldLayer!.FileNameTemplate);
            Assert.Contains(shield.ToLower(), shieldLayer.RootPath);
        }

        [Fact]
        public void GetStitchLayers_NoneShield_ExcludesShieldLayer()
        {
            var appearance = new CharacterAppearance { ShieldType = "None" };
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.DoesNotContain(layers, l => l.RootPath.Contains("shields/"));
        }

        [Fact]
        public void GetStitchLayers_HumanFemale_UsesGenderInPaths()
        {
            var appearance = new CharacterAppearance { Head = "Human Female", ArmorType = "Plate (Steel)" };
            var layers = _catalog.GetStitchLayers(appearance);

            var bodyLayer = layers.First(l => l.RootPath.Contains("body/"));
            var armorLayer = layers.First(l => l.RootPath.Contains("armor/"));

            Assert.Contains("/female", bodyLayer.RootPath);
            Assert.Contains("/female", armorLayer.RootPath);
        }

        [Theory]
        [InlineData("Leggings", "male")]
        [InlineData("Cuffed", "male")]
        [InlineData("Pantaloons", "male")]
        public void GetStitchLayers_MaleOnlyLegs_FallsBackToMale_ForFemaleCharacters(string legs, string expectedGender)
        {
            var appearance = new CharacterAppearance { Head = "Human Female", Legs = legs };
            var layers = _catalog.GetStitchLayers(appearance);

            var legsLayer = layers.FirstOrDefault(l => l.RootPath.Contains("legs/"));
            Assert.NotNull(legsLayer);
            Assert.Contains($"/{expectedGender}", legsLayer!.RootPath);
        }

        [Theory]
        [InlineData("Slacks")]
        [InlineData("Formal")]
        public void GetStitchLayers_GenderNeutralLegs_UsesSelectedGender(string legs)
        {
            var appearance = new CharacterAppearance { Head = "Human Female", Legs = legs };
            var layers = _catalog.GetStitchLayers(appearance);

            var legsLayer = layers.FirstOrDefault(l => l.RootPath.Contains("legs/"));
            Assert.NotNull(legsLayer);
            Assert.Contains("/female", legsLayer!.RootPath);
        }

        [Fact]
        public void GetStitchLayers_NoneLegs_ExcludesLegsLayer()
        {
            var appearance = new CharacterAppearance { Legs = "None" };
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.DoesNotContain(layers, l => l.RootPath.Contains("legs/"));
        }

        [Fact]
        public void GetStitchLayers_NoneFeet_ExcludesFeetLayer()
        {
            var appearance = new CharacterAppearance { Feet = "None" };
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.DoesNotContain(layers, l => l.RootPath.Contains("feet/"));
        }

        [Fact]
        public void GetStitchLayers_NoneArms_ExcludesArmsLayer()
        {
            var appearance = new CharacterAppearance { Arms = "None" };
            var layers = _catalog.GetStitchLayers(appearance);

            Assert.DoesNotContain(layers, l => l.RootPath.Contains("arms/"));
        }

        [Fact]
        public void GetStitchLayers_SkinColor_AppliesTintHex()
        {
            var appearance = new CharacterAppearance { SkinColor = "Bronze" };
            var layers = _catalog.GetStitchLayers(appearance);

            var bodyLayer = layers.First(l => l.RootPath.Contains("body/"));
            Assert.NotEqual("#FFFFFF", bodyLayer.TintHex);
        }

        [Fact]
        public void GetStitchLayers_HairColor_AppliesTintHex()
        {
            var appearance = new CharacterAppearance { HairColor = "Redhead" };
            var layers = _catalog.GetStitchLayers(appearance);

            var hairLayer = layers.First(l => l.RootPath.Contains("hair/"));
            Assert.NotEqual("#FFFFFF", hairLayer.TintHex);
        }

        // ─── GetDefaultAppearanceForClass ────────────────────────────

        [Theory]
        [InlineData("Warrior", "Plate (Steel)", "Arming Sword (Steel)")]
        [InlineData("Mage", "Mage Robes (Blue)", "Mage Wand")]
        [InlineData("Rogue", "Leather (Black)", "Dagger (Steel)")]
        [InlineData("Knight", "Plate (Steel)", "Arming Sword (Steel)")]
        [InlineData("Cleric", "Longsleeve (White)", "Arming Sword (Iron)")]
        public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment(string className, string expectedArmor, string expectedWeapon)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);

            Assert.Equal(expectedArmor, appearance.ArmorType);
            Assert.Equal(expectedWeapon, appearance.WeaponType);
        }

        [Theory]
        [InlineData("Warrior", "Crusader")]
        [InlineData("Knight", "Spartan")]
        [InlineData("Cleric", "Crusader")]
        [InlineData("Mage", "None")]
        [InlineData("Rogue", "None")]
        public void GetDefaultAppearanceForClass_ReturnsCorrectShield(string className, string expectedShield)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);
            Assert.Equal(expectedShield, appearance.ShieldType);
        }

        [Theory]
        [InlineData("Warrior")]
        [InlineData("Mage")]
        [InlineData("Rogue")]
        [InlineData("Knight")]
        [InlineData("Cleric")]
        public void GetDefaultAppearanceForClass_AllFieldsPopulated(string className)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);

            Assert.False(string.IsNullOrEmpty(appearance.ArmorType), $"{className} missing ArmorType");
            Assert.False(string.IsNullOrEmpty(appearance.WeaponType), $"{className} missing WeaponType");
            Assert.False(string.IsNullOrEmpty(appearance.ShieldType), $"{className} missing ShieldType");
            Assert.False(string.IsNullOrEmpty(appearance.Feet), $"{className} missing Feet");
            Assert.False(string.IsNullOrEmpty(appearance.Arms), $"{className} missing Arms");
            Assert.False(string.IsNullOrEmpty(appearance.Legs), $"{className} missing Legs");
            Assert.False(string.IsNullOrEmpty(appearance.Head), $"{className} missing Head");
            Assert.False(string.IsNullOrEmpty(appearance.Face), $"{className} missing Face");
        }

        // ─── Catalog Lists ──────────────────────────────────────────

        [Fact]
        public void HairStyles_ContainsExpectedOptions()
        {
            Assert.Contains("Long", _catalog.HairStyles);
            Assert.Contains("Plain", _catalog.HairStyles);
            Assert.Contains("Spiked", _catalog.HairStyles);
            Assert.Contains("Bob", _catalog.HairStyles);
            Assert.Contains("Afro", _catalog.HairStyles);
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

        // ─── Regression: Every catalog option produces valid stitch layers ───

        [Fact]
        public void GetStitchLayers_AllArmorTypes_ProduceArmorLayer()
        {
            foreach (var armor in _catalog.ArmorTypes)
            {
                var appearance = new CharacterAppearance { ArmorType = armor };
                var layers = _catalog.GetStitchLayers(appearance);

                bool hasArmor = layers.Any(l =>
                    l.RootPath.Contains("armor/") ||
                    l.RootPath.Contains("torso/"));
                Assert.True(hasArmor, $"Armor type '{armor}' did not produce an armor/torso layer");
            }
        }

        [Fact]
        public void GetStitchLayers_AllWeaponTypes_ProduceCorrectLayers()
        {
            foreach (var weapon in _catalog.WeaponTypes)
            {
                var appearance = new CharacterAppearance { WeaponType = weapon };
                var layers = _catalog.GetStitchLayers(appearance);

                if (weapon == "None")
                {
                    Assert.DoesNotContain(layers, l => l.RootPath.Contains("weapons/"));
                }
                else
                {
                    Assert.Contains(layers, l => l.RootPath.Contains("weapons/"));
                }
            }
        }

        [Fact]
        public void GetStitchLayers_AllShieldTypes_ProduceCorrectLayers()
        {
            foreach (var shield in _catalog.ShieldTypes)
            {
                var appearance = new CharacterAppearance { ShieldType = shield };
                var layers = _catalog.GetStitchLayers(appearance);

                if (shield == "None")
                {
                    Assert.DoesNotContain(layers, l => l.RootPath.Contains("shields/"));
                }
                else
                {
                    Assert.Contains(layers, l => l.RootPath.Contains("shields/"));
                }
            }
        }

        [Fact]
        public void GetStitchLayers_AllLegsTypes_ProduceCorrectLayers()
        {
            foreach (var legs in _catalog.LegsTypes)
            {
                var appearance = new CharacterAppearance { Legs = legs };
                var layers = _catalog.GetStitchLayers(appearance);

                if (legs == "None")
                {
                    Assert.DoesNotContain(layers, l => l.RootPath.Contains("legs/"));
                }
                else
                {
                    Assert.Contains(layers, l => l.RootPath.Contains("legs/"));
                }
            }
        }

        [Fact]
        public void GetStitchLayers_AllFeetTypes_ProduceCorrectLayers()
        {
            foreach (var feet in _catalog.FeetTypes)
            {
                var appearance = new CharacterAppearance { Feet = feet };
                var layers = _catalog.GetStitchLayers(appearance);

                if (feet == "None")
                {
                    Assert.DoesNotContain(layers, l => l.RootPath.Contains("feet/"));
                }
                else
                {
                    Assert.Contains(layers, l => l.RootPath.Contains("feet/"));
                }
            }
        }

        [Fact]
        public void GetStitchLayers_AllArmsTypes_ProduceCorrectLayers()
        {
            foreach (var arms in _catalog.ArmsTypes)
            {
                var appearance = new CharacterAppearance { Arms = arms };
                var layers = _catalog.GetStitchLayers(appearance);

                if (arms == "None")
                {
                    Assert.DoesNotContain(layers, l => l.RootPath.Contains("arms/"));
                }
                else
                {
                    Assert.Contains(layers, l => l.RootPath.Contains("arms/"));
                }
            }
        }

        [Fact]
        public void GetStitchLayers_AllHairStyles_ProduceHairLayer()
        {
            foreach (var style in _catalog.HairStyles)
            {
                var appearance = new CharacterAppearance { HairStyle = style };
                var layers = _catalog.GetStitchLayers(appearance);

                Assert.Contains(layers, l => l.RootPath.Contains("hair/"));
            }
        }

        [Fact]
        public void GetStitchLayers_AllSkinColors_ProduceTintedBodyLayer()
        {
            foreach (var skin in _catalog.SkinColors)
            {
                var appearance = new CharacterAppearance { SkinColor = skin };
                var layers = _catalog.GetStitchLayers(appearance);

                var bodyLayer = layers.First(l => l.RootPath.Contains("body/"));
                Assert.False(string.IsNullOrEmpty(bodyLayer.TintHex), $"Skin color '{skin}' has no tint hex");
            }
        }

        [Fact]
        public void GetStitchLayers_AllHairColors_ProduceTintedHairLayer()
        {
            foreach (var color in _catalog.HairColors)
            {
                var appearance = new CharacterAppearance { HairColor = color };
                var layers = _catalog.GetStitchLayers(appearance);

                var hairLayer = layers.First(l => l.RootPath.Contains("hair/"));
                Assert.False(string.IsNullOrEmpty(hairLayer.TintHex), $"Hair color '{color}' has no tint hex");
            }
        }

        // ─── Regression: Every class default generates valid stitch layers ───

        [Theory]
        [InlineData("Warrior")]
        [InlineData("Mage")]
        [InlineData("Rogue")]
        [InlineData("Knight")]
        [InlineData("Cleric")]
        public void GetStitchLayers_ClassDefaults_ProduceValidLayerSet(string className)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);
            var layers = _catalog.GetStitchLayers(appearance);

            // Every class should produce at minimum: body, head, face, eyes, hair, armor
            Assert.True(layers.Count >= 6, $"{className} defaults produced only {layers.Count} layers");
            Assert.Contains(layers, l => l.RootPath.Contains("body/"));
            Assert.Contains(layers, l => l.RootPath.Contains("head/"));
            Assert.Contains(layers, l => l.RootPath.Contains("face/"));
            Assert.Contains(layers, l => l.RootPath.Contains("eyes/"));
            Assert.Contains(layers, l => l.RootPath.Contains("hair/"));

            // Armor layer must exist (either armor/ or torso/)
            bool hasArmor = layers.Any(l => l.RootPath.Contains("armor/") || l.RootPath.Contains("torso/"));
            Assert.True(hasArmor, $"{className} defaults missing armor layer");

            // Weapon layer must exist for classes that have a weapon
            if (appearance.WeaponType != "None")
            {
                Assert.Contains(layers, l => l.RootPath.Contains("weapons/"));
            }

            // Shield layer must exist for classes that have a shield
            if (appearance.ShieldType != "None")
            {
                Assert.Contains(layers, l => l.RootPath.Contains("shields/"));
            }
        }
    }
}
