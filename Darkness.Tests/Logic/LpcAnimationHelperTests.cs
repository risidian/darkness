using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Logic;

public class LpcAnimationHelperTests
{
    [Fact]
    public void GetFrameRect_Walk_ReturnsCorrectRect()
    {
        var helper = new LpcAnimationHelper();
        var rect = helper.GetFrameRect("walk", 2, 0); // South (Row 10), Frame 0
        Assert.Equal(0, rect.X);
        Assert.Equal(10 * 64, rect.Y);
        Assert.Equal(64, rect.Width);
        Assert.Equal(64, rect.Height);
    }

    [Fact]
    public void GetOversizeFrameRect_Slash_ReturnsCorrectRect()
    {
        var helper = new LpcAnimationHelper();
        // Row offset for slash_oversize is fixed at 3456 (Row 54 start)
        // 4 directions (N, W, S, E)
        var rect = helper.GetOversizeFrameRect("slash_oversize", 2, 0); // South (Row 56), Frame 0
        Assert.Equal(0, rect.X);
        Assert.Equal(3456 + (2 * 192), rect.Y);
        Assert.Equal(192, rect.Width);
        Assert.Equal(192, rect.Height);
    }
}
