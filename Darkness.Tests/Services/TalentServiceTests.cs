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
                new TalentNode { Id = "node2", PrerequisiteNodeIds = { "node1" } }
 
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

    [Fact]
    public void PurchaseTalent_DeductsPoint_And_AddsId()
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
        service.PurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.Contains("node1", character.UnlockedTalentIds);
        Assert.Equal(0, character.TalentPoints);
    }

    [Fact]
    public void ApplyTalentPassives_AddsStats_And_UpdatesDerived()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> 
            { 
                new TalentNode 
                { 
                    Id = "node1", 
                    Effect = new TalentEffect { Stat = "Strength", Value = 5 } 
                } 
            } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character 
        { 
            Strength = 10, 
            Constitution = 10,
            UnlockedTalentIds = new List<string> { "node1" } 
        };
        character.RecalculateDerivedStats(); 

        // Act
        service.ApplyTalentPassives(character);

        // Assert
        Assert.Equal(15, character.Strength);
    }

    [Fact]
    public void GetAvailableTrees_FiltersByClass() {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        col.Insert(new TalentTree { Id = "mage_tree", RequiredClass = "Mage" });
        col.Insert(new TalentTree { Id = "knight_tree", RequiredClass = "Knight" });
        
        var service = new TalentService(db);
        var knight = new Character { Class = "Knight" };

        // Act
        var result = service.GetAvailableTrees(knight);

        // Assert
        Assert.Contains(result, t => t.Id == "knight_tree");
        Assert.DoesNotContain(result, t => t.Id == "mage_tree");
    }

    [Fact]
    public void GetAvailableTrees_RespectsExclusivityGroup() {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        
        var tree1 = new TalentTree { Id = "tree1", ExclusiveGroupId = "groupA", Nodes = new List<TalentNode> { new TalentNode { Id = "node1" } } };
        var tree2 = new TalentTree { Id = "tree2", ExclusiveGroupId = "groupA", Nodes = new List<TalentNode> { new TalentNode { Id = "node2" } } };
        col.Insert(tree1);
        col.Insert(tree2);

        var service = new TalentService(db);
        var character = new Character { UnlockedTalentIds = new List<string> { "node1" } };

        // Act
        var result = service.GetAvailableTrees(character);

        // Assert
        Assert.Contains(result, t => t.Id == "tree1");
        Assert.DoesNotContain(result, t => t.Id == "tree2");
    }

    [Fact]
    public void GetAvailableTrees_HandlesHiddenTrees() {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        
        // Add a prerequisite (Level 10) so it's hidden if level < 10 AND no points spent
        col.Insert(new TalentTree 
        { 
            Id = "hidden_tree", 
            IsHidden = true, 
            Prerequisites = new Dictionary<string, int> { { "Level", 10 } },
            Nodes = new List<TalentNode> { new TalentNode { Id = "nodeH" } } 
        });
        col.Insert(new TalentTree { Id = "visible_tree", IsHidden = false });

        var service = new TalentService(db);
        var character = new Character { Level = 1 };

        // Act 1: Prereqs not met AND no points spent
        var result1 = service.GetAvailableTrees(character);

        // Act 2: Prereqs not met BUT points spent (manually added to simulate unlocking)
        character.UnlockedTalentIds.Add("nodeH");
        var result2 = service.GetAvailableTrees(character);

        // Act 3: Prereqs met BUT no points spent
        var character2 = new Character { Level = 10 };
        var result3 = service.GetAvailableTrees(character2);

        // Assert
        Assert.DoesNotContain(result1, t => t.Id == "hidden_tree");
        Assert.Contains(result1, t => t.Id == "visible_tree");
        
        Assert.Contains(result2, t => t.Id == "hidden_tree");
        Assert.Contains(result3, t => t.Id == "hidden_tree");
    }

    [Fact]
    public void GetAvailableTrees_ShouldShowHiddenTree_WhenPrereqsMetButNoPointsSpent()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        
        var tree = new TalentTree 
        { 
            Id = "hidden_tree", 
            IsHidden = true, 
            RequiredClass = "Knight",
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1" } }
        };
        col.Insert(tree);

        var service = new TalentService(db);
        var character = new Character { Class = "Knight" };

        // Act
        var available = service.GetAvailableTrees(character);

        // Assert
        Assert.Contains(available, t => t.Id == "hidden_tree");
    }
}
