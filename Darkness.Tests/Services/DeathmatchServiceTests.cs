using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services;

public class DeathmatchServiceTests
{
    [Fact]
    public async Task GetEncountersAsync_ReturnsNonEmptyList()
    {
        var service = new DeathmatchService();
        var encounters = await service.GetEncountersAsync();
        Assert.NotEmpty(encounters);
    }

    [Fact]
    public async Task GetEncountersAsync_AllEncountersHaveEnemies()
    {
        var service = new DeathmatchService();
        var encounters = await service.GetEncountersAsync();
        Assert.All(encounters, e => Assert.NotEmpty(e.Enemies));
    }

    [Fact]
    public async Task GetEncountersAsync_AllEncountersHaveTitle()
    {
        var service = new DeathmatchService();
        var encounters = await service.GetEncountersAsync();
        Assert.All(encounters, e => Assert.False(string.IsNullOrWhiteSpace(e.Name)));
    }
}
