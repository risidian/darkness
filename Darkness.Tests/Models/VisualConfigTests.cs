using Darkness.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Darkness.Tests.Models;

public class VisualConfigTests
{
    [Fact]
    public void VisualConfig_Initialization_InitializesZonesList()
    {
        var config = new VisualConfig();
        Assert.NotNull(config.Zones);
        Assert.Empty(config.Zones);
        Assert.IsType<List<ZoneConfig>>(config.Zones);
    }
}
