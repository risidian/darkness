using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Darkness.Tests.Services;

public class TalentServiceTests
{
    [Fact]
    public void GetAvailableTrees_ReturnsOnlyTreesWithMetPrerequisites()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        col.Insert(new TalentTree { Id = "tree1", Prerequisites = new Dictionary<string, int> { { "Level", 10 } } });
        col.Insert(new TalentTree { Id = "tree2", Prerequisites = new Dictionary<string, int> { { "Level", 1 } } });
        
        var service = new TalentService(db);
        var character = new Character { Level = 5 };

        // Act
        var result = service.GetAvailableTrees(character);

        // Assert
        Assert.Single(result);
        Assert.Equal("tree2", result[0].Id);
    }

    [Fact]
    public void CanPurchaseTalent_ReturnsFalse_WhenPointsInsufficient()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1", PointsRequired = 1 } } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 0 };

        // Act
        var result = service.CanPurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanPurchaseTalent_ReturnsTrue_WhenAllConditionsMet()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1", PointsRequired = 1 } } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 1, Level = 1 };

        // Act
        var result = service.CanPurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanPurchaseTalent_ReturnsFalse_WhenAlreadyUnlocked()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1", PointsRequired = 1 } } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 1, UnlockedTalentIds = new List<string> { "node1" } };

        // Act
        var result = service.CanPurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanPurchaseTalent_ReturnsFalse_WhenPrerequisiteNotMet()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> 
            { 
                new TalentNode { Id = "node1" },
                new TalentNode { Id = "node2", PrerequisiteNodeId = "node1" } 
            } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 1 };

        // Act
        var result = service.CanPurchaseTalent(character, "tree1", "node2");

        // Assert
        Assert.False(result);
    }
}
