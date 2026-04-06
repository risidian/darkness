using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class QuestProgressionTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestProgressionTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestProgressionTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _questService = new QuestService(_db);

        SeedData();
    }

    private void SeedData()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        
        // beat_1
        chains.Insert(new QuestChain
        {
            Id = "beat_1",
            Title = "The Awakening",
            IsMainStory = true,
            SortOrder = 1,
            Prerequisites = new(),
            Steps = new List<QuestStep>
            {
                new() {
                    Id = "beat_1_intro",
                    Type = "branch",
                    Branch = new BranchData {
                        Options = new() {
                            new BranchOption { Text = "Fight", NextStepId = "beat_1_combat_dialogue", MoralityImpact = 5 },
                            new BranchOption { Text = "Sneak", NextStepId = "beat_1_stealth_dialogue", MoralityImpact = -5 }
                        }
                    }
                },
                new() {
                    Id = "beat_1_combat_dialogue",
                    Type = "dialogue",
                    NextStepId = "beat_1_combat"
                },
                new() {
                    Id = "beat_1_combat",
                    Type = "combat"
                }
            }
        });

        // beat_2
        chains.Insert(new QuestChain
        {
            Id = "beat_2",
            Title = "Dark Warrior",
            IsMainStory = true,
            SortOrder = 2,
            Prerequisites = new() { "beat_1" },
            Steps = new List<QuestStep>
            {
                new() { Id = "beat_2_dialogue", Type = "dialogue" }
            }
        });
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void FullProgression_FromBeat1ToBeat2_Works()
    {
        var character = new Character { Id = 1, Name = "Test" };
        
        // 1. Check initial state
        var available = _questService.GetAvailableChains(character);
        Assert.Contains(available, c => c.Id == "beat_1");
        Assert.DoesNotContain(available, c => c.Id == "beat_2");

        // 2. Start beat_1_intro and choose Fight
        var step1 = _questService.AdvanceStep(character, "beat_1", "beat_1_combat_dialogue");
        Assert.NotNull(step1);
        Assert.Equal("beat_1_combat_dialogue", step1.Id);

        // 3. Complete dialogue, move to combat
        var step2 = _questService.AdvanceStep(character, "beat_1");
        Assert.NotNull(step2);
        Assert.Equal("beat_1_combat", step2.Id);

        // 4. Complete combat (victory)
        var step3 = _questService.AdvanceStep(character, "beat_1");
        Assert.Null(step3); // Should return null when chain completes

        // 5. Verify beat_1 is completed
        var completed = _questService.GetCompletedChainIds(character.Id);
        Assert.Contains("beat_1", completed);

        // 6. Verify beat_2 is now available
        var availableAfter = _questService.GetAvailableChains(character);
        Assert.DoesNotContain(availableAfter, c => c.Id == "beat_1");
        Assert.Contains(availableAfter, c => c.Id == "beat_2");
    }
}
