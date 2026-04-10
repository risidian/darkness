using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class NavigationArgsTests
{
    [Fact]
    public void BattleArgs_HasReturnPositionProperties()
    {
        var args = new BattleArgs
        {
            ReturnPositionX = 100.5f,
            ReturnPositionY = 200.5f
        };

        Assert.Equal(100.5f, args.ReturnPositionX);
        Assert.Equal(200.5f, args.ReturnPositionY);
    }

    [Fact]
    public void StealthArgs_HasReturnPositionProperties()
    {
        var args = new StealthArgs
        {
            ReturnPositionX = 300.5f,
            ReturnPositionY = 400.5f
        };

        Assert.Equal(300.5f, args.ReturnPositionX);
        Assert.Equal(400.5f, args.ReturnPositionY);
    }
}
