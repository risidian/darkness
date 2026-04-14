using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class SheetModelTests
{
    [Fact]
    public void SheetLayer_GetPath_ReturnsCorrectGenderPath()
    {
        // Arrange
        var layer = new SheetLayer();
        layer.Paths["male"] = "path/to/male.png";
        layer.Paths["female"] = "path/to/female.png";

        // Act
        var malePath = layer.GetPath("male");
        var femalePath = layer.GetPath("female");
        var upperMalePath = layer.GetPath("MALE");

        // Assert
        Assert.Equal("path/to/male.png", malePath);
        Assert.Equal("path/to/female.png", femalePath);
        Assert.Equal("path/to/male.png", upperMalePath);
    }

    [Fact]
    public void SheetLayer_GetPath_FallsBackToMale()
    {
        // Arrange
        var layer = new SheetLayer();
        layer.Paths["male"] = "path/to/male.png";

        // Act
        var femalePath = layer.GetPath("female");

        // Assert
        Assert.Equal("path/to/male.png", femalePath);
    }

    [Fact]
    public void SheetConstants_AnimationRows_ContainsExpectedKeys()
    {
        // Assert
        Assert.True(SheetConstants.AnimationRows.ContainsKey("walk"));
        Assert.True(SheetConstants.AnimationRows.ContainsKey("slash"));
        Assert.Equal(8, SheetConstants.AnimationRows["walk"]);
    }
}
