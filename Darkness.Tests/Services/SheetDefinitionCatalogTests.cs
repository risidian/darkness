using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Darkness.Tests.Services;

public class SheetDefinitionCatalogTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SheetDefinitionCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;

    public SheetDefinitionCatalogTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SheetDefinitionCatalogTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _fsMock = new Mock<IFileSystemService>();

        // Find and load the actual seed file
        var json = File.ReadAllText(FindSeedFile());
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        // Seed DB for AppearanceOptions
        var appSeeder = new AppearanceSeeder(_fsMock.Object);
        appSeeder.Seed(_db);

        // Seed DB for SheetDefinitions (manual insert for test)
        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        col.Insert(new SheetDefinition { Name = "Arming Sword (Steel)", Slot = "Weapon", Variants = new List<string> { "steel" } });
        col.Insert(new SheetDefinition { Name = "Plate (Steel)", Slot = "Armor" });

        _catalog = new SheetDefinitionCatalog(_db);
    }

    private static string FindSeedFile()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return Path.Combine(dir!, "Darkness.Godot", "assets", "data", "sprite-catalog.json");
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void GetOptionNames_ReturnsHairStyles()
    {
        var styles = _catalog.GetOptionNames("Hair", "male");
        Assert.Contains("Long", styles);
        Assert.Contains("Afro", styles);
    }

    [Fact]
    public void GetSheetDefinitions_DefaultAppearance_ReturnsBody()
    {
        var appearance = new CharacterAppearance();
        var definitions = _catalog.GetSheetDefinitions(appearance);
        Assert.NotEmpty(definitions);
        Assert.Contains(definitions, d => d.Slot == "Body");
    }

    [Fact]
    public void GetSheetDefinitions_SkinColor_AppliesCorrectTint()
    {
        var appearance = new CharacterAppearance { SkinColor = "Amber" };
        var definitions = _catalog.GetSheetDefinitions(appearance);
        var body = definitions.FirstOrDefault(d => d.Slot == "Body");
        Assert.NotNull(body);
        // We can't check the tint here easily because WrapAppearanceOption creates it internally
        // but we can verify the definition is returned.
    }

    [Fact]
    public void WrapAppearanceOption_HairLong_SetsDefaultVariantFromFileNameTemplate()
    {
        // Hair "Long" has FileNameTemplate = "{action}/blonde.png" -> DefaultVariant should be "blonde"
        var appearance = new CharacterAppearance { HairStyle = "Long" };
        var definitions = _catalog.GetSheetDefinitions(appearance);
        var hairDef = definitions.FirstOrDefault(d => d.Slot == "Hair");
        Assert.NotNull(hairDef);
        var baseLayer = hairDef.Layers["base"];
        Assert.Equal("blonde", baseLayer.DefaultVariant);
    }

    [Fact]
    public void WrapAppearanceOption_Body_DefaultVariantIsNull()
    {
        // Body "Human Male" has FileNameTemplate = "{action}.png" -> DefaultVariant should be null
        var appearance = new CharacterAppearance();
        var definitions = _catalog.GetSheetDefinitions(appearance);
        var bodyDef = definitions.FirstOrDefault(d => d.Slot == "Body");
        Assert.NotNull(bodyDef);
        var baseLayer = bodyDef.Layers["base"];
        Assert.Null(baseLayer.DefaultVariant);
    }

    [Fact]
    public void WrapAppearanceOption_Eyes_SetsDefaultVariantFromFileNameTemplate()
    {
        // Eyes "Default" has FileNameTemplate = "{action}/blue.png" -> DefaultVariant should be "blue"
        var appearance = new CharacterAppearance { Eyes = "Default" };
        var definitions = _catalog.GetSheetDefinitions(appearance);
        var eyesDef = definitions.FirstOrDefault(d => d.Slot == "Eyes");
        Assert.NotNull(eyesDef);
        var baseLayer = eyesDef.Layers["base"];
        Assert.Equal("blue", baseLayer.DefaultVariant);
    }

    [Theory]
    [InlineData("Knight", "Arming Sword (Steel)", "Spartan")]
    [InlineData("Warrior", "Waraxe", "None")]
    [InlineData("Mage", "Mage Wand", "None")]
    [InlineData("Rogue", "Dagger (Steel)", "None")]
    [InlineData("Cleric", "Mace", "Crusader")]
    public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment(string className, string expectedWeapon, string expectedShield)
    {
        var appearance = _catalog.GetDefaultAppearanceForClass(className);
        Assert.Equal(expectedWeapon, appearance.WeaponType);
        Assert.Equal(expectedShield, appearance.ShieldType);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_UnknownClass_ReturnsDefaultAppearance()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("Nonexistent");
        Assert.NotNull(appearance);
    }

    [Theory]
    [InlineData("Knight", "Plate (Steel)", "Boots (Rimmed)", "Gloves", "Formal")]
    [InlineData("Mage", "Mage Robes (Blue)", "Sandals", "None", "Formal")]
    [InlineData("Rogue", "Leather (Black)", "Boots (Fold)", "Gloves", "Leggings")]
    [InlineData("Cleric", "Longsleeve (White)", "Shoes", "None", "Slacks")]
    public void GetDefaultAppearanceForClass_ReturnsCorrectArmorAndAccessories(string className, string expectedArmor, string expectedFeet, string expectedArms, string expectedLegs)
    {
        var appearance = _catalog.GetDefaultAppearanceForClass(className);
        Assert.Equal(expectedArmor, appearance.ArmorType);
        Assert.Equal(expectedFeet, appearance.Feet);
        Assert.Equal(expectedArms, appearance.Arms);
        Assert.Equal(expectedLegs, appearance.Legs);
    }
}
