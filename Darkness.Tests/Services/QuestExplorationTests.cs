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
        _questService.SetWorldFlag(1, "test_flag", true);
        Assert.True(_questService.GetWorldFlag(1, "test_flag"));

        _questService.SetWorldFlag(1, "test_flag", false);
        Assert.False(_questService.GetWorldFlag(1, "test_flag"));
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
        _questService.SetWorldFlag(1, "can_pass", true);
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
                        new() { Type = QuestRewardType.Experience, Value = "100" },
                        new() { Type = QuestRewardType.Gold, Value = "50" }
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
                        new() { Type = QuestRewardType.AttributePoint, Value = "2" }
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
        items.Insert(new Item { Name = "Iron Sword", Type = "Weapon" });
        var seedItem = items.FindOne(i => i.Name == "Iron Sword");

        var rewards = new List<QuestReward>
        {
            new() { Type = QuestRewardType.Item, Value = "Iron Sword", Amount = 1 }
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
        Assert.Equal("Iron Sword", character.Inventory[0].Name);
        // Inventory item must be a clone, not a shared seed reference.
        Assert.NotSame(seedItem, character.Inventory[0]);
    }

    [Fact]
    public void GrantRewards_StackableItem_RespectsAmount()
    {
        // Regression for the Item.Clone() Quantity-propagation bug:
        // a seed item with Quantity=5 + reward Amount=3 must yield exactly 3 stacks of 1
        // (consolidated to a single stack of 3), not 15.
        var items = _db.GetCollection<Item>("items");
        items.Insert(new Item { Name = "Health Potion", Type = "Consumable", Quantity = 5 });

        var character = new Character { Id = 42 };

        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "potion_chain",
            Steps = new List<QuestStep>
            {
                new()
                {
                    Id = "step_1",
                    Rewards = new List<QuestReward>
                    {
                        new() { Type = QuestRewardType.Item, Value = "Health Potion", Amount = 3 }
                    }
                }
            }
        });

        _questService.AdvanceStep(character, "potion_chain");

        // After consolidation, all matching items merge into one entry.
        // (ConsolidateInventory is called by callers, but here we just count units.)
        int totalPotions = character.Inventory.Where(i => i.Name == "Health Potion").Sum(i => i.Quantity);
        Assert.Equal(3, totalPotions);
    }

    [Fact]
    public void AdvanceStep_BrokenNextStepId_MarksChainBlocked()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "broken_chain",
            Steps = new List<QuestStep>
            {
                new() { Id = "step_1", NextStepId = "missing_step" }
            }
        });

        var character = new Character { Id = 1 };

        var result = _questService.AdvanceStep(character, "broken_chain");
        Assert.Null(result);

        // State should be marked blocked, not absent — otherwise a retry would soft-loop step_1.
        var state = _questService.GetQuestState(1, "broken_chain");
        Assert.NotNull(state);
        Assert.Equal("blocked", state.Status);

        // A second attempt should not replay step_1.
        var retry = _questService.AdvanceStep(character, "broken_chain");
        Assert.Null(retry);
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
                        new() { Type = QuestRewardType.WorldFlag, Value = "unlocked_gate" }
                    }
                }
            }
        });

        var character = new Character { Id = 1 };
        _questService.AdvanceStep(character, "flag_chain");

        Assert.True(_questService.GetWorldFlag(1, "unlocked_gate"));
    }
}
