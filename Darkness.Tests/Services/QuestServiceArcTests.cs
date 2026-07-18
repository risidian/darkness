using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;

namespace Darkness.Tests.Services;

public class QuestServiceArcTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestServiceArcTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestServiceArcTests_{Guid.NewGuid()}.db");
        // Each LiteDatabase gets its OWN BsonMapper (flaky-test fix — mandatory).
        _db = new LiteDatabase(_dbPath, new BsonMapper());

        var arcs = _db.GetCollection<Arc>("arcs");
        arcs.Insert(new Arc
        {
            Id = "arc_1", TitleKey = "arc.arc_1.title", Order = 1,
            Prerequisites = new(),
            MainChainIds = new() { "beat_1", "beat_2" }
        });
        arcs.Insert(new Arc
        {
            Id = "arc_2", TitleKey = "arc.arc_2.title", Order = 2,
            Prerequisites = new() { "arc_1" },
            MainChainIds = new() { "beat_3" }
        });

        _questService = new QuestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    private void MarkCompleted(int characterId, params string[] chainIds)
    {
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        foreach (var id in chainIds)
        {
            stateCol.Insert(new QuestState { CharacterId = characterId, ChainId = id, Status = "completed" });
        }
    }

    [Fact]
    public void GetAvailableArcs_ExcludesArcsWithUnmetPrerequisiteArcs()
    {
        var character = new Character { Id = 1 };
        var available = _questService.GetAvailableArcs(character);

        Assert.Contains(available, a => a.Id == "arc_1");
        Assert.DoesNotContain(available, a => a.Id == "arc_2");
    }

    [Fact]
    public void GetAvailableArcs_IncludesArcWhenPrerequisiteArcComplete()
    {
        var character = new Character { Id = 1 };
        MarkCompleted(1, "beat_1", "beat_2"); // completes arc_1

        var available = _questService.GetAvailableArcs(character);

        // arc_1 is now complete so it drops out; arc_2 becomes available.
        Assert.DoesNotContain(available, a => a.Id == "arc_1");
        Assert.Contains(available, a => a.Id == "arc_2");
    }

    [Fact]
    public void IsArcComplete_TrueOnlyWhenAllMainChainsCompleted()
    {
        var character = new Character { Id = 1 };

        Assert.False(_questService.IsArcComplete(character, "arc_1"));

        MarkCompleted(1, "beat_1");
        Assert.False(_questService.IsArcComplete(character, "arc_1")); // beat_2 still incomplete

        MarkCompleted(1, "beat_2");
        Assert.True(_questService.IsArcComplete(character, "arc_1"));
    }

    [Fact]
    public void IsArcComplete_UnknownArc_ReturnsFalse()
    {
        var character = new Character { Id = 1 };
        Assert.False(_questService.IsArcComplete(character, "arc_missing"));
    }

    [Fact]
    public void GetCurrentArc_ReturnsLowestOrderIncompleteAvailableArc()
    {
        var character = new Character { Id = 1 };

        var current = _questService.GetCurrentArc(character);
        Assert.NotNull(current);
        Assert.Equal("arc_1", current!.Id);

        // Complete arc_1 -> current advances to arc_2.
        MarkCompleted(1, "beat_1", "beat_2");
        current = _questService.GetCurrentArc(character);
        Assert.NotNull(current);
        Assert.Equal("arc_2", current!.Id);
    }

    [Fact]
    public void GetCurrentArc_NoArcs_ReturnsNull()
    {
        _db.GetCollection<Arc>("arcs").DeleteAll();
        var character = new Character { Id = 1 };
        Assert.Null(_questService.GetCurrentArc(character));
    }
}
