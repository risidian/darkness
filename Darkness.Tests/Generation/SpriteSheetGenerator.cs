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
    private readonly SpriteLayerCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;

    public SpriteSheetGenerator()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteSheetGen_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _fsMock = new Mock<IFileSystemService>();

        var json = File.ReadAllText(FindSeedFile());
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

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
    public void GenerateSpriteSheets(string className, string head)
    {
        var appearance = _catalog.GetDefaultAppearanceForClass(className);
        appearance.Head = head;
        
        var layers = _catalog.GetStitchLayers(appearance).Where(l => !l.RootPath.Contains("weapons/") && !l.RootPath.Contains("shields/")).ToList();
        
        var root = GetProjectRoot();
        var outDir = Path.Combine(root, "GeneratedSpriteSheets");
        Directory.CreateDirectory(outDir);

        using var compositeBitmap = new SKBitmap(832, 1344);
        using var canvas = new SKCanvas(compositeBitmap);
        canvas.Clear(SKColors.Transparent);

        var animMap = new Dictionary<string, int>
        {
            { "spellcast", 0 },
            { "thrust", 4 },
            { "walk", 8 },
            { "slash", 12 },
            { "shoot", 16 },
            { "hurt", 20 }
        };

        foreach (var layer in layers)
        {
            using var layerBitmap = new SKBitmap(832, 1344);
            using var layerCanvas = new SKCanvas(layerBitmap);
            layerCanvas.Clear(SKColors.Transparent);

            bool layerHasContent = false;

            using var paint = new SKPaint();
            if (SKColor.TryParse(layer.TintHex, out var tintColor) && tintColor != SKColors.White)
            {
                paint.ColorFilter = SKColorFilter.CreateBlendMode(tintColor, SKBlendMode.Multiply);
            }

            foreach (var anim in animMap)
            {
                byte[] data = null;
                string assetPath = layer.RootPath + "/" + layer.FileNameTemplate.Replace("{action}", anim.Key);
                string fullPath = Path.Combine(root, "Darkness.Godot", assetPath);

                if (!File.Exists(fullPath))
                {
                    assetPath = layer.RootPath + "/" + layer.FileNameTemplate.Replace("{action}/", "");
                    fullPath = Path.Combine(root, "Darkness.Godot", assetPath);
                    if (!File.Exists(fullPath))
                    {
                        assetPath = layer.RootPath + "/" + layer.FileNameTemplate;
                        fullPath = Path.Combine(root, "Darkness.Godot", assetPath);
                    }
                }

                if (File.Exists(fullPath))
                {
                    data = File.ReadAllBytes(fullPath);
                }
                else
                {
                    // Fallback 1: Try "attack_" prefix (for LPC weapons)
                    if (anim.Key == "slash" || anim.Key == "thrust" || anim.Key == "shoot")
                    {
                        string altPath = layer.RootPath + "/" + layer.FileNameTemplate.Replace("{action}", "attack_" + anim.Key);
                        string altFullPath = Path.Combine(root, "Darkness.Godot", altPath);
                        if (File.Exists(altFullPath))
                        {
                            data = File.ReadAllBytes(altFullPath);
                        }
                    }

                    // Fallback 2: walk
                    if (data == null && anim.Key != "walk")
                    {
                        assetPath = layer.RootPath + "/" + layer.FileNameTemplate.Replace("{action}", "walk");
                        fullPath = Path.Combine(root, "Darkness.Godot", assetPath);
                        if (File.Exists(fullPath))
                        {
                            data = File.ReadAllBytes(fullPath);
                        }
                    }
                }

                if (data != null)
                {
                    using var stream = new MemoryStream(data);
                    using var img = SKBitmap.Decode(stream);
                    if (img != null)
                    {
                        layerHasContent = true;
                        
                        SKBitmap sourceImg = img;
                        if (layer.IsFlipped)
                        {
                            var flippedBitmap = new SKBitmap(img.Width, img.Height);
                            using var flipCanvas = new SKCanvas(flippedBitmap);
                            flipCanvas.Scale(-1, 1, img.Width / 2.0f, 0);
                            flipCanvas.DrawBitmap(img, 0, 0);
                            sourceImg = flippedBitmap;
                        }

                        if (sourceImg.Width == 96 && sourceImg.Height == 128)
                        {
                            // Face asset logic: blit the standing frames (col 1) to the 9 columns of this animation block
                            int destRowBase = anim.Value;
                            int[] faceYOffset = { 0, 96, 64, 32 }; // Up, Left, Down, Right

                            for (int dir = 0; dir < 4; dir++)
                            {
                                var faceRect = new SKRect(32, faceYOffset[dir], 64, faceYOffset[dir] + 32);
                                for (int col = 0; col < 9; col++)
                                {
                                    var destRect = new SKRect(col * 64 + 16, (destRowBase + dir) * 64 + 16, col * 64 + 16 + 32, (destRowBase + dir) * 64 + 16 + 32);
                                    layerCanvas.DrawBitmap(sourceImg, faceRect, destRect, paint);
                                }
                            }
                        }
                        else
                        {
                            layerCanvas.DrawBitmap(sourceImg, 0, anim.Value * 64, paint);
                        }

                        if (layer.IsFlipped)
                        {
                            sourceImg.Dispose();
                        }
                    }
                }
            }

            if (layerHasContent)
            {
                canvas.DrawBitmap(layerBitmap, 0, 0);
            }
        }

        string gender = head.Contains("Female") ? "Female" : "Male";
        string outPath = Path.Combine(outDir, $"{className}_{gender}.png");
        
        using var image = SKImage.FromBitmap(compositeBitmap);
        using var dataOut = image.Encode(SKEncodedImageFormat.Png, 100);
        using var outStream = File.OpenWrite(outPath);
        dataOut.SaveTo(outStream);
    }
}
