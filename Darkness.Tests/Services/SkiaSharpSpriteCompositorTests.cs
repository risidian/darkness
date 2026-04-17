using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Moq;
using Xunit;
using SkiaSharp;

namespace Darkness.Tests.Services;

public class SkiaSharpSpriteCompositorTests
{
    [Fact]
    public async Task CompositeFullSheet_ProducesCorrectDimensions()
    {
        var fileSystem = new Mock<IFileSystemService>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false); // No assets, just testing canvas
        
        var compositor = new SkiaSharpSpriteCompositor();
        var definitions = new List<SheetDefinition>();
        var appearance = new CharacterAppearance();
        
        var sheet = await compositor.CompositeFullSheet(definitions, appearance, fileSystem.Object);
        
        // Final height = 3456 (Standard) + (12 oversize animations * 192) = 3456 + 2304 = 5760
        
        using var stream = new MemoryStream(sheet);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);
        Assert.Equal(SheetConstants.SHEET_WIDTH, bitmap.Width);
        Assert.Equal(5760, bitmap.Height);
    }

    [Fact]
    public async Task CompositePreviewFrame_ProducesCorrectDimensions()
    {
        var fileSystem = new Mock<IFileSystemService>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        
        var compositor = new SkiaSharpSpriteCompositor();
        var definitions = new List<SheetDefinition>();
        var appearance = new CharacterAppearance();
        
        var frame = await compositor.CompositePreviewFrame(definitions, appearance, fileSystem.Object);
        
        using var stream = new MemoryStream(frame);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);
        Assert.Equal(64, bitmap.Width);
        Assert.Equal(64, bitmap.Height);
    }
}
