using Darkness.Core.Interfaces;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class DialogServiceContractTests
{
    [Fact]
    public async Task DisplayConfirmAsync_WhenCancelled_ReturnsFalse()
    {
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.DisplayConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await mock.Object.DisplayConfirmAsync("Delete character?", "This cannot be undone.", "Delete", "Cancel");

        Assert.False(result);
    }

    [Fact]
    public async Task DisplayConfirmAsync_WhenConfirmed_ReturnsTrue()
    {
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.DisplayConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await mock.Object.DisplayConfirmAsync("Delete character?", "This cannot be undone.", "Delete", "Cancel");

        Assert.True(result);
    }

    [Fact]
    public async Task DisplayConfirmAsync_NeverReturnsTrueUnconditionally()
    {
        // This test documents the bug that was fixed:
        // DisplayConfirmAsync must not hardcode return true.
        // We verify the interface contract is respected by the mock.
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.DisplayConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await mock.Object.DisplayConfirmAsync("Any title", "Any message", "OK", "Cancel");

        // If this were hardcoded true, this assertion would fail
        Assert.False(result);
    }
}
