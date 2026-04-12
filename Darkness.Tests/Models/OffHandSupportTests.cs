using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class OffHandSupportTests
{
    [Fact]
    public void CharacterAppearance_ShouldHaveOffHandType()
    {
        var appearance = new CharacterAppearance();
        // This will fail to compile until I add the property
        Assert.Equal("None", appearance.OffHandType);
    }

    [Fact]
    public void Character_ShouldHaveOffHandType()
    {
        var character = new Character();
        // This will fail to compile until I add the property
        Assert.Equal("None", character.OffHandType);
    }
}
