using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using Xunit;
using System;
using System.IO;
using System.Linq;

namespace Darkness.Tests.Scenes;

public class CharacterGenWizardTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SheetDefinitionCatalog _catalog;
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
        var appSeeder = new AppearanceSeeder(_fsMock.Object);
        appSeeder.Seed(_db);

        // Seed SheetDefinitions from actual JSON files
        var root = FindProjectRoot();
        var sheetDefDir = Path.Combine(root, "Darkness.Godot", "assets", "data", "sheet_definitions");
        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        foreach (var file in Directory.GetFiles(sheetDefDir, "*.json", SearchOption.AllDirectories))
        {
            var defJson = File.ReadAllText(file);
            var def = System.Text.Json.JsonSerializer.Deserialize<SheetDefinition>(defJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (def != null) col.Insert(def);
        }

        _catalog = new SheetDefinitionCatalog(_db);
    }

    private static string FindSeedFile()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return Path.Combine(dir!, "Darkness.Godot", "assets", "data", "sprite-catalog.json");
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir!;
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
        
        // 3. Get sheet definitions
        var definitions = _catalog.GetSheetDefinitions(appearance);
        
        var errors = new System.Collections.Generic.List<string>();

        // Check body exists
        if (!definitions.Any(d => d.Slot == "Body")) errors.Add($"Missing Body definition for {className}");

        // Check equipment
        if (appearance.ArmorType != "None" && !string.IsNullOrEmpty(appearance.ArmorType))
            if (!definitions.Any(d => d.Slot == "Armor")) errors.Add($"Missing Armor definition for {className} ({appearance.ArmorType})");

        if (appearance.WeaponType != "None" && !string.IsNullOrEmpty(appearance.WeaponType))
            if (!definitions.Any(d => d.Slot == "Weapon")) errors.Add($"Missing Weapon definition for {className} ({appearance.WeaponType})");

        if (appearance.ShieldType != "None" && !string.IsNullOrEmpty(appearance.ShieldType))
            if (!definitions.Any(d => d.Slot == "Shield")) errors.Add($"Missing Shield definition for {className} ({appearance.ShieldType})");

        if (appearance.OffHandType != "None" && !string.IsNullOrEmpty(appearance.OffHandType))
            if (!definitions.Any(d => d.Slot == "OffHand")) errors.Add($"Missing OffHand definition for {className} ({appearance.OffHandType})");

        if (appearance.Legs != "None" && !string.IsNullOrEmpty(appearance.Legs))
            if (!definitions.Any(d => d.Slot == "Legs")) errors.Add($"Missing Legs definition for {className} ({appearance.Legs})");

        if (appearance.Feet != "None" && !string.IsNullOrEmpty(appearance.Feet))
            if (!definitions.Any(d => d.Slot == "Feet")) errors.Add($"Missing Feet definition for {className} ({appearance.Feet})");

        if (appearance.Arms != "None" && !string.IsNullOrEmpty(appearance.Arms))
            if (!definitions.Any(d => d.Slot == "Arms")) errors.Add($"Missing Arms definition for {className} ({appearance.Arms})");

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
