using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class QuestExplorationTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestExplorationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestExplorationTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _questService = new QuestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void SetAndGetWorldFlag_WorksCorrectly()
    {
        _questService.SetWorldFlag("test_flag", true);
        Assert.True(_questService.GetWorldFlag("test_flag"));

        _questService.SetWorldFlag("test_flag", false);
        Assert.False(_questService.GetWorldFlag("test_flag"));
    }

    [Fact]
    public void AdvanceStep_ChecksNextStepRequirements()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "test_chain",
            Steps = new List<QuestStep>
            {
                new() { Id = "step_1", NextStepId = "step_2" },
                new() { Id = "step_2", Requirements = new List<BranchCondition> 
                    { new() { Type = "world_flag", Operator = "==", Value = "can_pass" } } }
            }
        });

        var character = new Character { Id = 1 };
        
        // Try to advance without the flag
        var nextStep = _questService.AdvanceStep(character, "test_chain");
        Assert.Null(nextStep);

        // Set the flag and try again
        _questService.SetWorldFlag("can_pass", true);
        nextStep = _questService.AdvanceStep(character, "test_chain");
        Assert.NotNull(nextStep);
        Assert.Equal("step_2", nextStep.Id);
    }

    [Fact]
    public void AdvanceStep_GrantsRewardsOfCompletedStep()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "reward_chain",
            Steps = new List<QuestStep>
            {
                new() { 
                    Id = "step_1", 
                    NextStepId = "step_2",
                    Rewards = new List<QuestReward> 
                    { 
                        new() { Type = "Experience", Value = "100" },
                        new() { Type = "Gold", Value = "50" }
                    }
                },
                new() { Id = "step_2" }
            }
        });

        var character = new Character { Id = 1, Experience = 0, Gold = 0 };
        
        _questService.AdvanceStep(character, "reward_chain");

        Assert.Equal(100, character.Experience);
        Assert.Equal(50, character.Gold);
    }

    [Fact]
    public void AdvanceStep_GrantsRewardsOnCompletion()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "final_reward_chain",
            Steps = new List<QuestStep>
            {
                new() { 
                    Id = "only_step", 
                    Rewards = new List<QuestReward> 
                    { 
                        new() { Type = "AttributePoint", Value = "2" }
                    }
                }
            }
        });

        var character = new Character { Id = 1, AttributePoints = 0 };
        
        _questService.AdvanceStep(character, "final_reward_chain");

        Assert.Equal(2, character.AttributePoints);
        var state = _questService.GetQuestState(1, "final_reward_chain");
        Assert.Equal("completed", state?.Status);
    }

    [Fact]
    public void GrantRewards_ItemReward_WorksCorrectly()
    {
        // Seed an item
        var items = _db.GetCollection<Item>("items");
        items.Insert(new Item { Name = "Steel Sword", Type = "Weapon" });

        var rewards = new List<QuestReward>
        {
            new() { Type = "Item", Value = "Steel Sword", Amount = 1 }
        };

        var character = new Character { Id = 1 };
        
        // Manually call AdvanceStep with a chain that has this reward
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "item_chain",
            Steps = new List<QuestStep>
            {
                new() { Id = "step_1", Rewards = rewards }
            }
        });

        _questService.AdvanceStep(character, "item_chain");

        Assert.Single(character.Inventory);
        Assert.Equal("Steel Sword", character.Inventory[0].Name);
    }

    [Fact]
    public void AdvanceStep_GrantsWorldFlagReward()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "flag_chain",
            Steps = new List<QuestStep>
            {
                new() { 
                    Id = "step_1", 
                    Rewards = new List<QuestReward> 
                    { 
                        new() { Type = "WorldFlag", Value = "unlocked_gate" }
                    }
                }
            }
        });

        var character = new Character { Id = 1 };
        _questService.AdvanceStep(character, "flag_chain");

        Assert.True(_questService.GetWorldFlag("unlocked_gate"));
    }
}
