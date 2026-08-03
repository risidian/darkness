using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using System;
using System.IO;
using Xunit;

namespace Darkness.Tests.UI;

public class TutorialFlagTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public TutorialFlagTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TutorialFlagTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _questService = new QuestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void TutorialDismissedFlag_IsFalseByDefault()
    {
        var character = new Character { Id = 1 };
        
        // Assert it is false by default
        bool isDismissed = _questService.GetWorldFlag(character.Id, "tutorial_dismissed");
        Assert.False(isDismissed);
    }

    [Fact]
    public void TutorialDismissedFlag_CanBeSetAndRetrieved()
    {
        var character = new Character { Id = 1 };
        
        // Set the flag
        _questService.SetWorldFlag(character.Id, "tutorial_dismissed", true);
        
        // Retrieve the flag
        bool isDismissed = _questService.GetWorldFlag(character.Id, "tutorial_dismissed");
        
        Assert.True(isDismissed);
    }
}
