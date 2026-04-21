using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class TriggerServiceDebounceTests
{
    private readonly Mock<IQuestService> _questServiceMock;
    private readonly TriggerService _triggerService;

    public TriggerServiceDebounceTests()
    {
        _questServiceMock = new Mock<IQuestService>();
        _triggerService = new TriggerService(_questServiceMock.Object);
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsStep_FirstTime()
    {
        var character = new Character { Id = 1 };
        var locationKey = "TownGate";
        var expectedStep = new QuestStep { Id = "step_1" };
        var chain = new QuestChain { Id = "chain_1" };

        _questServiceMock.Setup(q => q.GetAvailableChains(character))
            .Returns(new List<QuestChain> { chain });
        _questServiceMock.Setup(q => q.GetCurrentStep(character, chain.Id))
            .Returns(new QuestStep { Id = "step_1", Location = new LocationTrigger { LocationKey = locationKey } });

        var step = _triggerService.CheckLocationTrigger(character, locationKey);
        
        Assert.NotNull(step);
        Assert.Equal("step_1", step.Id);
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsNull_WhenCalledTwiceTooFast()
    {
        var character = new Character { Id = 1 };
        var locationKey = "TownGate";
        var chain = new QuestChain { Id = "chain_1" };

        _questServiceMock.Setup(q => q.GetAvailableChains(character))
            .Returns(new List<QuestChain> { chain });
        _questServiceMock.Setup(q => q.GetCurrentStep(character, chain.Id))
            .Returns(new QuestStep { Id = "step_1", Location = new LocationTrigger { LocationKey = locationKey } });

        // First call
        var step1 = _triggerService.CheckLocationTrigger(character, locationKey);
        Assert.NotNull(step1);

        // Second call immediately
        var step2 = _triggerService.CheckLocationTrigger(character, locationKey);
        Assert.Null(step2);
    }

    [Fact]
    public async Task CheckLocationTrigger_ReturnsStep_AfterCooldown()
    {
        var character = new Character { Id = 1 };
        var locationKey = "TownGate";
        var chain = new QuestChain { Id = "chain_1" };

        _questServiceMock.Setup(q => q.GetAvailableChains(character))
            .Returns(new List<QuestChain> { chain });
        _questServiceMock.Setup(q => q.GetCurrentStep(character, chain.Id))
            .Returns(new QuestStep { Id = "step_1", Location = new LocationTrigger { LocationKey = locationKey } });

        // First call
        _triggerService.CheckLocationTrigger(character, locationKey);

        // Wait 600ms
        await Task.Delay(600);

        // Second call
        var step = _triggerService.CheckLocationTrigger(character, locationKey);
        Assert.NotNull(step);
        Assert.Equal("step_1", step.Id);
    }
}
