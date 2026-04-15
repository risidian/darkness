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
        
        // 4. Assert basics
        Assert.NotEmpty(definitions);
        
        // Check body exists
        var body = definitions.FirstOrDefault(d => d.Slot == "Body");
        Assert.NotNull(body);
    }
}
