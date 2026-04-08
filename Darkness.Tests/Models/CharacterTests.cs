using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class CharacterTests
{
    [Fact]
    public void Character_InitializesWithZeroGoldAndEmptyHotbar()
    {
        var character = new Character();
        Assert.Equal(0, character.Gold);
        Assert.NotNull(character.Hotbar);
        Assert.Equal(5, character.Hotbar.Length);
    }
}
