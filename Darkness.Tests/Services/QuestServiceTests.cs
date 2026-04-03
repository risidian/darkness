using System.Collections.Generic;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services;

public class QuestServiceTests
{
    private readonly QuestService _questService;

    public QuestServiceTests()
    {
        _questService = new QuestService();
    }

    [Fact]
    public void GetAvailableQuests_ShouldReturnInitialMainQuest_WhenNoQuestsCompleted()
    {
        // Arrange
        var character = new Character { Name = "Test" };

        // Act
        var availableQuests = _questService.GetAvailableQuests(character);

        // Assert
        Assert.Single(availableQuests);
        Assert.Equal("main_1", availableQuests[0].Id);
    }

    [Fact]
    public void GetAvailableQuests_ShouldReturnSideQuest_WhenMainQuestCompleted()
    {
        // Arrange
        var character = new Character { Name = "Test" };
        _questService.CompleteQuest(character, "main_1");

        // Act
        var availableQuests = _questService.GetAvailableQuests(character);

        // Assert
        Assert.Single(availableQuests);
        Assert.Equal("side_1", availableQuests[0].Id);
    }

    [Fact]
    public void GetQuestById_ShouldReturnCorrectQuest()
    {
        // Act
        var quest = _questService.GetQuestById("main_1");

        // Assert
        Assert.NotNull(quest);
        Assert.Equal("The Beginning", quest.Title);
    }

    [Fact]
    public void GetQuestByLocation_ShouldReturnMainQuest_ForSandyShoreEast()
    {
        // Act
        var quest = _questService.GetQuestByLocation("SandyShore_East");

        // Assert
        Assert.NotNull(quest);
        Assert.Equal("main_1", quest.Id);
    }

    [Fact]
    public void CompleteQuest_ShouldAddIdToCompletedQuests()
    {
        // Arrange
        var character = new Character { Name = "Test" };

        // Act
        _questService.CompleteQuest(character, "main_1");

        // Assert
        Assert.Contains("main_1", character.CompletedQuestIds);
    }
}
