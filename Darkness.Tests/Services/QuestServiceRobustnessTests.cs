using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class QuestServiceRobustnessTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestServiceRobustnessTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestServiceRobustnessTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());

        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "valid_chain", Title = "Valid Chain",
            Steps = new List<QuestStep>
            {
                new() { Id = "step_1", NextStepId = "invalid_step_id" },
                new() { Id = "step_branch", Type = "branch", Branch = new BranchData { Options = new() {
                    new BranchOption { NextStepId = "step_2" }
                }}},
                new() { Id = "step_2" }
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
    public void AdvanceStep_ReturnsNull_WhenChainNotFound()
    {
        var character = new Character { Id = 1 };
        var step = _questService.AdvanceStep(character, "nonexistent_chain");
        Assert.Null(step);
    }

    [Fact]
    public void AdvanceStep_ReturnsNull_WhenNextStepIdIsInvalid()
    {
        var character = new Character { Id = 1 };
        // step_1 has NextStepId = "invalid_step_id" which doesn't exist in the chain
        var step = _questService.AdvanceStep(character, "valid_chain");
        Assert.Null(step);

        var state = _questService.GetQuestState(1, "valid_chain");
        // It should NOT be completed if the next step was missing
        Assert.NotEqual("completed", state?.Status);
    }

    [Fact]
    public void AdvanceStep_ReturnsNull_WhenChoiceProvidedButStepHasNoBranch()
    {
        var character = new Character { Id = 1 };
        // Advance from step_1 (which is not a branch) with a choice
        var step = _questService.AdvanceStep(character, "valid_chain", "some_choice");
        Assert.Null(step);

        var state = _questService.GetQuestState(1, "valid_chain");
        Assert.NotEqual("completed", state?.Status);
    }

    [Fact]
    public void AdvanceStep_ReturnsNull_WhenChoiceIsInvalid()
    {
        var character = new Character { Id = 1 };
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "valid_chain", CurrentStepId = "step_branch", Status = "in_progress" });

        var step = _questService.AdvanceStep(character, "valid_chain", "invalid_choice");
        Assert.Null(step);

        var state = _questService.GetQuestState(1, "valid_chain");
        Assert.Equal("in_progress", state?.Status);
        Assert.Equal("step_branch", state?.CurrentStepId);
    }
}
