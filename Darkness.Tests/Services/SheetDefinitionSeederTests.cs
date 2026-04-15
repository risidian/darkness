using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using Xunit;
using System.IO;

namespace Darkness.Tests.Services;

public class SheetDefinitionSeederTests
{
    [Fact]
    public void Seed_ShouldDeserializeSnakeCaseFields()
    {
        // Arrange
        var mockFs = new Mock<IFileSystemService>();
        mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        mockFs.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new[] { "assets/data/sheet_definitions/weapons/sword/test_weapon.json" });

        var json = @"{
            ""name"": ""Test Weapon"",
            ""slot"": ""Weapon"",
            ""layers"": {
                ""slash_behind"": {
                    ""custom_animation"": ""slash_oversize"",
                    ""zPos"": 9,
                    ""paths"": {
                        ""male"": ""weapons/sword/bg/"",
                        ""female"": ""weapons/sword/bg/""
                    }
                }
            },
            ""variants"": [""steel""],
            ""animations"": [""walk"", ""slash_oversize""],
            ""preview_row"": 10,
            ""preview_column"": 1
        }";
        mockFs.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(json);

        var seeder = new SheetDefinitionSeeder(mockFs.Object);
        using var db = new LiteDatabase(new MemoryStream());

        // Act
        seeder.Seed(db);

        // Assert
        var col = db.GetCollection<SheetDefinition>("sheet_definitions");
        var def = col.FindOne(d => d.Name == "Test Weapon");
        Assert.NotNull(def);
        Assert.Equal(10, def.PreviewRow);
        Assert.Equal(1, def.PreviewColumn);

        var layer = def.Layers["slash_behind"];
        Assert.Equal("slash_oversize", layer.CustomAnimation);
    }
}
