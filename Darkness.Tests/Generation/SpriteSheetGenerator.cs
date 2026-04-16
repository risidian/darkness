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
using SkiaSharp;
using System.Threading.Tasks;

namespace Darkness.Tests.Generation;

public class SpriteSheetGenerator : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SheetDefinitionCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly SkiaSharpSpriteCompositor _compositor;
    private static bool _directoryCleared = false;
    private static readonly object _directoryLock = new();

    public SpriteSheetGenerator()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteSheetGen_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _fsMock = new Mock<IFileSystemService>();

        lock (_directoryLock)
        {
            if (!_directoryCleared)
            {
                var root = GetProjectRoot();
                var outDir = Path.Combine(root, "GeneratedSpriteSheets");
                if (Directory.Exists(outDir))
                {
                    foreach (var f in Directory.GetFiles(outDir))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
                else
                {
                    Directory.CreateDirectory(outDir);
                }
                _directoryCleared = true;
            }
        }

        var json = File.ReadAllText(FindSeedFile());
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var appSeeder = new AppearanceSeeder(_fsMock.Object);
        appSeeder.Seed(_db);

        // Seed SheetDefinitions from actual JSON files
        var projectRoot = GetProjectRoot();
        var sheetDefDir = Path.Combine(projectRoot, "Darkness.Godot", "assets", "data", "sheet_definitions");
        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        foreach (var file in Directory.GetFiles(sheetDefDir, "*.json", SearchOption.AllDirectories))
        {
            var defJson = File.ReadAllText(file);
            var def = System.Text.Json.JsonSerializer.Deserialize<SheetDefinition>(defJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (def != null) col.Insert(def);
        }

        _catalog = new SheetDefinitionCatalog(_db);
        _compositor = new SkiaSharpSpriteCompositor();
    }

    private static string FindSeedFile()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return Path.Combine(dir!, "Darkness.Godot", "assets", "data", "sprite-catalog.json");
    }

    private static string GetProjectRoot()
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
    [InlineData("Knight", "Human Male")]
    [InlineData("Knight", "Human Female")]
    [InlineData("Rogue", "Human Male")]
    [InlineData("Rogue", "Human Female")]
    [InlineData("Mage", "Human Male")]
    [InlineData("Mage", "Human Female")]
    [InlineData("Cleric", "Human Male")]
    [InlineData("Cleric", "Human Female")]
    [InlineData("Warrior", "Human Male")]
    [InlineData("Warrior", "Human Female")]
    public async Task GenerateSpriteSheets(string className, string head)
    {
        var appearance = _catalog.GetDefaultAppearanceForClass(className);
        appearance.Head = head;
        
        var definitions = _catalog.GetSheetDefinitions(appearance);
        
        var root = GetProjectRoot();
        var outDir = Path.Combine(root, "GeneratedSpriteSheets");
        Directory.CreateDirectory(outDir);

        // Mock FileSystemService to read from local disk for the generator
        var localFs = new Mock<IFileSystemService>();
        localFs.Setup(f => f.FileExists(It.IsAny<string>())).Returns<string>(p => File.Exists(Path.Combine(root, "Darkness.Godot", p)));
        localFs.Setup(f => f.OpenAppPackageFileAsync(It.IsAny<string>())).Returns<string>(p => Task.FromResult<Stream>(File.OpenRead(Path.Combine(root, "Darkness.Godot", p))));

        var sheetData = await _compositor.CompositeFullSheet(definitions, appearance, localFs.Object);

        Assert.NotNull(sheetData);
        Assert.True(sheetData.Length > 20000, $"Generated sheet for {className}_{head} is too small ({sheetData.Length} bytes). Equipment likely failed to bake.");

        string gender = head.Contains("Female") ? "Female" : "Male";
        string outPath = Path.Combine(outDir, $"{className}_{gender}.png");
        
        File.WriteAllBytes(outPath, sheetData);
        Console.WriteLine($"[Generator] Generated {className}_{gender}: {sheetData.Length} bytes. Saved to {outPath}");

        // Verify Oversize Frames (Attack animations)
        // Oversize region starts at Y=SheetConstants.SHEET_HEIGHT (3456)
        using var bitmap = SKBitmap.Decode(sheetData);
        Assert.NotNull(bitmap);

        // Check a random frame in the slash_oversize animation (Direction: Down, Frame: 3)
        // Rect: X=3*192, Y=3456 + 2*192
        int oversizeY = SheetConstants.SHEET_HEIGHT + (2 * SheetConstants.OVERSIZE_FRAME_SIZE);
        int oversizeX = 3 * SheetConstants.OVERSIZE_FRAME_SIZE;

        bool hasPixels = false;
        for (int y = oversizeY; y < oversizeY + SheetConstants.OVERSIZE_FRAME_SIZE; y += 10)
        {
            for (int x = oversizeX; x < oversizeX + SheetConstants.OVERSIZE_FRAME_SIZE; x += 10)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    hasPixels = true;
                    break;
                }
            }
            if (hasPixels) break;
        }

        Assert.True(hasPixels, $"Oversize frame at ({oversizeX}, {oversizeY}) is empty for {className}. Weapon/Body failed to bake into attack frames.");
    }
}
