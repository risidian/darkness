using System.Collections.Generic;
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class QuestServiceTests
{
    private readonly QuestService _questService;
    private readonly Mock<IFileSystemService> _mockFileSystem;

    public QuestServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        
        // Setup mock quest data
        var quests = new List<QuestNode>
        {
            new QuestNode { Id = "beat_1", Title = "The Beginning", IsMainStory = true, Prerequisites = new List<string>() },
            new QuestNode { Id = "side_1", Title = "A Small Favor", IsMainStory = false, Prerequisites = new List<string> { "beat_1" } }
        };
        
        string json = JsonSerializer.Serialize(quests);
        _mockFileSystem.Setup(f => f.ReadAllText("assets/data/quests.json")).Returns(json);

        _questService = new QuestService(_mockFileSystem.Object);
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
        Assert.Equal("beat_1", availableQuests[0].Id);
    }

    [Fact]
    public void GetAvailableQuests_ShouldReturnSideQuest_WhenMainQuestCompleted()
    {
        // Arrange
        var character = new Character { Name = "Test" };
        _questService.CompleteQuest(character, "beat_1");

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
        var quest = _questService.GetQuestById("beat_1");

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
        Assert.Equal("beat_1", quest.Id);
    }

    [Fact]
    public void CompleteQuest_ShouldAddIdToCompletedQuests()
    {
        // Arrange
        var character = new Character { Name = "Test" };

        // Act
        _questService.CompleteQuest(character, "beat_1");

        // Assert
        Assert.Contains("beat_1", character.CompletedQuestIds);
    }
}
