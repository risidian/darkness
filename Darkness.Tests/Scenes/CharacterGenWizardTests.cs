using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using Xunit;

namespace Darkness.Tests.Scenes;

public class CharacterGenWizardTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SpriteLayerCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;

    public CharacterGenWizardTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"CharacterGenWizardTests_{Guid.NewGuid()}.db");
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

    [Theory]
    [InlineData("Knight", "Human Male", "male")]
    [InlineData("Knight", "Human Female", "female")]
    [InlineData("Rogue", "Human Male", "male")]
    [InlineData("Rogue", "Human Female", "female")]
    [InlineData("Mage", "Human Male", "male")]
    [InlineData("Mage", "Human Female", "female")]
    [InlineData("Cleric", "Human Male", "male")]
    [InlineData("Cleric", "Human Female", "female")]
    [InlineData("Warrior", "Human Male", "male")]
    [InlineData("Warrior", "Human Female", "female")]
    public void ExhaustiveSpriteMappingTest(string className, string head, string expectedGender)
    {
        // 1. Get default appearance for class
        var appearance = _catalog.GetDefaultAppearanceForClass(className);
        
        // 2. Set head to match gender
        appearance.Head = head;
        
        // 3. Get stitch layers
        var layers = _catalog.GetStitchLayers(appearance);
        
        // 4. Assert basics
        Assert.NotEmpty(layers);
        
        // Check body path
        var bodyLayer = layers.FirstOrDefault(l => l.RootPath.Contains("body"));
        Assert.NotNull(bodyLayer);
        Assert.Contains(expectedGender, bodyLayer.RootPath);

        // 5. Check specific class gear and prevent path doubling (e.g. female/female)
        switch (className)
        {
            case "Knight":
                var plateLayer = layers.FirstOrDefault(l => l.RootPath.Contains("plate"));
                Assert.NotNull(plateLayer);
                Assert.Contains(expectedGender, plateLayer.RootPath);
                Assert.DoesNotContain($"{expectedGender}/{expectedGender}", plateLayer.RootPath);
                break;
            case "Mage":
                var mageArmor = layers.FirstOrDefault(l => l.RootPath.Contains("torso") && !l.RootPath.Contains("body"));
                Assert.NotNull(mageArmor);
                if (expectedGender == "male")
                {
                    Assert.Contains("jacket/tabard/male", mageArmor.RootPath);
                }
                else
                {
                    Assert.Contains("torso/robes/female", mageArmor.RootPath);
                    Assert.DoesNotContain("female/female", mageArmor.RootPath);
                }
                break;
            case "Cleric":
                var clericArmor = layers.FirstOrDefault(l => l.RootPath.Contains("torso") && !l.RootPath.Contains("body"));
                Assert.NotNull(clericArmor);
                if (expectedGender == "male")
                {
                    Assert.Contains("jacket/tabard/male", clericArmor.RootPath);
                }
                else
                {
                    Assert.Contains("torso/robes/female", clericArmor.RootPath);
                    Assert.DoesNotContain("female/female", clericArmor.RootPath);
                }
                break;
        }
    }

    [Fact]
    public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment()
    {
        var knight = _catalog.GetDefaultAppearanceForClass("Knight");
        Assert.Equal("Plate (Steel)", knight.ArmorType);
        Assert.Equal("Arming Sword (Steel)", knight.WeaponType);
        Assert.Equal("Spartan", knight.ShieldType);

        var mage = _catalog.GetDefaultAppearanceForClass("Mage");
        Assert.Equal("Mage Robes (Blue)", mage.ArmorType);
        Assert.Equal("Mage Wand", mage.WeaponType);
        Assert.Equal("Dagger (Steel)", mage.OffHandType);

        var rogue = _catalog.GetDefaultAppearanceForClass("Rogue");
        Assert.Equal("Leather (Black)", rogue.ArmorType);
        Assert.Equal("Dagger (Steel)", rogue.WeaponType);

        var warrior = _catalog.GetDefaultAppearanceForClass("Warrior");
        Assert.Equal("Plate (Steel)", warrior.ArmorType);
        Assert.Equal("Waraxe", warrior.WeaponType);

        var cleric = _catalog.GetDefaultAppearanceForClass("Cleric");
        Assert.Equal("Longsleeve (White)", cleric.ArmorType);
        Assert.Equal("Mace", cleric.WeaponType);
    }
}
