using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;

namespace Darkness.Tests.Services;

public class QuestServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);

        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_1", Title = "The Awakening", IsMainStory = true, SortOrder = 1,
            Prerequisites = new(),
            Steps = new List<QuestStep>
            {
                new() { Id = "beat_1_intro", Type = "branch",
                    Branch = new BranchData { Options = new()
                    {
                        new BranchOption { Text = "Fight", NextStepId = "beat_1_combat", MoralityImpact = 5 },
                        new BranchOption { Text = "Sneak", NextStepId = "beat_1_stealth", MoralityImpact = -5 }
                    }}},
                new() { Id = "beat_1_combat", Type = "combat" },
                new() { Id = "beat_1_stealth", Type = "location" }
            }
        });
        chains.Insert(new QuestChain
        {
            Id = "beat_2", Title = "Dark Warrior", IsMainStory = true, SortOrder = 2,
            Prerequisites = new() { "beat_1" },
            Steps = new List<QuestStep>
            {
                new() { Id = "beat_2_combat", Type = "combat" }
            }
        });

        _questService = new QuestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void GetAvailableChains_ReturnsChainWithNoPrereqs()
    {
        var character = new Character { Id = 1 };
        var chains = _questService.GetAvailableChains(character);
        Assert.Single(chains);
        Assert.Equal("beat_1", chains[0].Id);
    }

    [Fact]
    public void GetAvailableChains_ExcludesChainWithUnmetPrereqs()
    {
        var character = new Character { Id = 1 };
        var chains = _questService.GetAvailableChains(character);
        Assert.DoesNotContain(chains, c => c.Id == "beat_2");
    }

    [Fact]
    public void GetAvailableChains_IncludesChainWhenPrereqsComplete()
    {
        var character = new Character { Id = 1 };
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_1", Status = "completed" });

        var chains = _questService.GetAvailableChains(character);
        Assert.Contains(chains, c => c.Id == "beat_2");
    }

    [Fact]
    public void GetCurrentStep_ReturnsFirstStep_WhenNoState()
    {
        var character = new Character { Id = 1 };
        var step = _questService.GetCurrentStep(character, "beat_1");
        Assert.NotNull(step);
        Assert.Equal("beat_1_intro", step.Id);
    }

    [Fact]
    public void AdvanceStep_WithBranchChoice_SetsCorrectStep()
    {
        var character = new Character { Id = 1 };
        var step = _questService.AdvanceStep(character, "beat_1", "beat_1_combat");

        Assert.NotNull(step);
        Assert.Equal("beat_1_combat", step.Id);

        var state = _questService.GetQuestState(1, "beat_1");
        Assert.NotNull(state);
        Assert.Equal("beat_1_combat", state.CurrentStepId);
        Assert.Equal("in_progress", state.Status);
    }

    [Fact]
    public void AdvanceStep_OnLastStep_CompletesChain()
    {
        var character = new Character { Id = 1 };
        _questService.AdvanceStep(character, "beat_1", "beat_1_combat");
        _questService.AdvanceStep(character, "beat_1");

        var state = _questService.GetQuestState(1, "beat_1");
        Assert.NotNull(state);
        Assert.Equal("completed", state.Status);
    }

    [Fact]
    public void AdvanceStep_AppliesMoralityImpact()
    {
        var character = new Character { Id = 1, Morality = 0 };
        _questService.AdvanceStep(character, "beat_1", "beat_1_combat");
        Assert.Equal(5, character.Morality);
    }

    [Fact]
    public void IsMainStoryComplete_ReturnsFalse_WhenChainsIncomplete()
    {
        var character = new Character { Id = 1 };
        Assert.False(_questService.IsMainStoryComplete(character));
    }

    [Fact]
    public void IsMainStoryComplete_ReturnsTrue_WhenAllMainChainsComplete()
    {
        var character = new Character { Id = 1 };
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_1", Status = "completed" });
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_2", Status = "completed" });
        Assert.True(_questService.IsMainStoryComplete(character));
    }

    [Fact]
    public void GetChainById_ReturnsCorrectChain()
    {
        var chain = _questService.GetChainById("beat_1");
        Assert.NotNull(chain);
        Assert.Equal("The Awakening", chain.Title);
    }

    [Fact]
    public void GetChainById_ReturnsNull_ForUnknownId()
    {
        Assert.Null(_questService.GetChainById("nonexistent"));
    }
}
