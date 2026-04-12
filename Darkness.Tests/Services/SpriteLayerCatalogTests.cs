using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;

namespace Darkness.Tests.Services;

public class SpriteLayerCatalogTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SpriteLayerCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;

    public SpriteLayerCatalogTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteLayerCatalogTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _fsMock = new Mock<IFileSystemService>();

        // Find and load the actual seed file
        var json = File.ReadAllText(FindSeedFile());
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        // Seed DB
        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);

        _catalog = new SpriteLayerCatalog(_db, _fsMock.Object);
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
    public void GetOptionNames_ReturnsArmorTypes_Male()
    {
        var armors = _catalog.GetOptionNames("Armor", "male");
        Assert.Contains("Plate (Steel)", armors);
        Assert.Contains("Leather", armors);
        // Mage Robes are now "gendered" and should appear for both genders
        Assert.Contains("Mage Robes (Blue)", armors);
    }

    [Fact]
    public void GetOptionNames_ReturnsArmorTypes_Female()
    {
        var armors = _catalog.GetOptionNames("Armor", "female");
        Assert.Contains("Plate (Steel)", armors);
        Assert.Contains("Leather", armors);
        Assert.Contains("Mage Robes (Blue)", armors);
    }

    [Fact]
    public void GetOptionNames_ReturnsWeaponTypesWithNone()
    {
        var weapons = _catalog.GetOptionNames("Weapon", "male");
        Assert.Contains("Arming Sword (Steel)", weapons);
        Assert.Contains("None", weapons);
    }

    [Fact]
    public void GetOptionNames_ReturnsHairStyles()
    {
        var styles = _catalog.GetOptionNames("Hair", "male");
        Assert.Contains("Long", styles);
        Assert.Contains("Afro", styles);
    }

    [Fact]
    public void GetStitchLayers_DefaultAppearance_ReturnsLayersInZOrder()
    {
        var appearance = new CharacterAppearance();
        var layers = _catalog.GetStitchLayers(appearance);
        Assert.NotEmpty(layers);
        Assert.Contains("body/male", layers[0].RootPath);
    }

    [Fact]
    public void GetStitchLayers_FemaleHead_UsesFemaleGenderPaths()
    {
        var appearance = new CharacterAppearance { Head = "Human Female" };
        var layers = _catalog.GetStitchLayers(appearance);
        Assert.Contains(layers, l => l.RootPath.Contains("female"));
    }

    [Fact]
    public void GetStitchLayers_MaleOnlyLegs_FallsBackToMale()
    {
        var appearance = new CharacterAppearance { Head = "Human Female", Legs = "Leggings" };
        var layers = _catalog.GetStitchLayers(appearance);
        var legsLayer = layers.FirstOrDefault(l => l.RootPath.Contains("legs"));
        Assert.NotNull(legsLayer);
        Assert.Contains("male", legsLayer.RootPath);
    }

    [Fact]
    public void GetStitchLayers_NoneWeapon_ExcludesWeaponLayer()
    {
        var appearance = new CharacterAppearance { WeaponType = "None" };
        var layers = _catalog.GetStitchLayers(appearance);
        Assert.DoesNotContain(layers, l => l.RootPath.Contains("weapon"));
    }

    [Fact]
    public void GetStitchLayers_SkinColor_AppliesTintHex()
    {
        var appearance = new CharacterAppearance { SkinColor = "Amber" };
        var layers = _catalog.GetStitchLayers(appearance);
        var bodyLayer = layers.First(l => l.RootPath.Contains("body"));
        Assert.Equal("#E0AC69", bodyLayer.TintHex);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_Warrior_ReturnsPlateAndAxe()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("Warrior");
        Assert.Equal("Plate (Steel)", appearance.ArmorType);
        Assert.Equal("Waraxe", appearance.WeaponType);
        Assert.Equal("None", appearance.ShieldType);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_Mage_ReturnsRobesAndWand()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("Mage");
        Assert.Equal("Mage Robes (Blue)", appearance.ArmorType);
        Assert.Equal("Mage Wand", appearance.WeaponType);
    }

    [Fact]
    public void GetStitchLayers_MageWithOffHand_ReturnsMirroredDagger()
    {
        var appearance = new CharacterAppearance 
        { 
            WeaponType = "Mage Wand",
            OffHandType = "Dagger (Steel)"
        };
        var layers = _catalog.GetStitchLayers(appearance);
        
        // Find dagger layer
        var daggerLayer = layers.FirstOrDefault(l => l.RootPath.Contains("dagger"));
        Assert.NotNull(daggerLayer);
        Assert.True(daggerLayer.IsFlipped);
    }

    [Fact]
    public void GetStitchLayers_MaleMageRobes_RedirectsToTabard()
    {
        var appearance = new CharacterAppearance 
        { 
            Head = "Human Male",
            ArmorType = "Mage Robes (Blue)"
        };
        var layers = _catalog.GetStitchLayers(appearance);
        
        var armorLayer = layers.FirstOrDefault(l => l.RootPath.Contains("torso"));
        Assert.NotNull(armorLayer);
        Assert.Contains("jacket/tabard/male", armorLayer.RootPath);
    }

    [Fact]
    public void GetStitchLayers_Shield_ReturnsShieldLayer()
    {
        var appearance = new CharacterAppearance { ShieldType = "Crusader" };
        var layers = _catalog.GetStitchLayers(appearance);
        Assert.Contains(layers, l => l.RootPath.Contains("crusader"));
    }
}
