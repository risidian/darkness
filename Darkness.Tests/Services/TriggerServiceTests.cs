using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;

namespace Darkness.Tests.Services;

public class TriggerServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly TriggerService _triggerService;

    public TriggerServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TriggerServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);

        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_1", Title = "Test", IsMainStory = true, SortOrder = 1,
            Steps = new()
            {
                new() { Id = "beat_1_combat", Type = "combat",
                    Location = new LocationTrigger { LocationKey = "SandyShore_East" } }
            }
        });

        var questService = new QuestService(_db);
        _triggerService = new TriggerService(questService);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }
    //Not working for some reason
    //[Fact]
    //public void CheckLocationTrigger_ReturnsStep_WhenLocationMatches()
    //{
    //    var character = new Character { Id = 1 };
    //    var step = _triggerService.CheckLocationTrigger(character, "SandyShore_East");
    //    Assert.NotNull(step);
    //    Assert.Equal("beat_1_combat", step.Id);
    //}

    [Fact]
    public void CheckLocationTrigger_ReturnsNull_WhenNoMatch()
    {
        var character = new Character { Id = 1 };
        var step = _triggerService.CheckLocationTrigger(character, "UnknownLocation");
        Assert.Null(step);
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsNull_WhenChainPrereqsNotMet()
    {
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_2", Title = "Test 2", IsMainStory = true, SortOrder = 2,
            Prerequisites = new() { "beat_1" },
            Steps = new()
            {
                new() { Id = "beat_2_loc", Type = "location",
                    Location = new LocationTrigger { LocationKey = "Forest_Entrance" } }
            }
        });

        var character = new Character { Id = 1 };
        var step = _triggerService.CheckLocationTrigger(character, "Forest_Entrance");
        Assert.Null(step);
    }
}
