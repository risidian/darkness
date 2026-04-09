using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class ZoneConfigTests
{
    [Fact]
    public void ZoneConfig_Initialization_SetsDefaults()
    {
        var zone = new ZoneConfig();
        Assert.Equal("Block", zone.Type);
        Assert.Equal(0f, zone.X);
        Assert.Equal(0f, zone.Y);
        Assert.Equal(0f, zone.Width);
        Assert.Equal(0f, zone.Height);
        Assert.Null(zone.ActionId);
        Assert.Null(zone.Message);
    }
}
