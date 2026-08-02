using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class ItemConsumableTests
{
    [Fact]
    public void GetHealAmount_ExtractsAmountFromDescription()
    {
        var item = new Item { Description = "Restores 50 HP.", Value = 25 };
        Assert.Equal(50, item.GetHealAmount(100));
    }

    [Fact]
    public void GetHealAmount_FullRestore()
    {
        var item = new Item { Description = "Restores all HP.", Value = 250 };
        Assert.Equal(999, item.GetHealAmount(999));
    }

    [Fact]
    public void GetHealAmount_FallsBackToValue()
    {
        var item = new Item { Description = "A mysterious brew.", Value = 15 };
        Assert.Equal(15, item.GetHealAmount(100));
    }
}
