using System.Collections.Generic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class TalentModelTests
{
    [Fact]
    public void TalentEffect_InitializesCorrectly()
    {
        var effect = new TalentEffect
        {
            Stat = "Strength",
            Value = 5,
            Skill = "Heavy Strike"
        };

        Assert.Equal("Strength", effect.Stat);
        Assert.Equal(5, effect.Value);
        Assert.Equal("Heavy Strike", effect.Skill);
    }

    [Fact]
    public void TalentNode_InitializesCorrectly()
    {
        var node = new TalentNode
        {
            Id = "node_1",
            Name = "Strength I",
            Description = "Increases Strength by 5",
            PointsRequired = 1,
            Row = 2,
            Column = 3,
            PrerequisiteNodeId = null,
            Effect = new TalentEffect { Stat = "Strength", Value = 5 }
        };

        Assert.Equal("node_1", node.Id);
        Assert.Equal("Strength I", node.Name);
        Assert.Equal("Increases Strength by 5", node.Description);
        Assert.Equal(1, node.PointsRequired);
        Assert.Equal(2, node.Row);
        Assert.Equal(3, node.Column);
        Assert.Null(node.PrerequisiteNodeId);
        Assert.NotNull(node.Effect);
        Assert.Equal("Strength", node.Effect.Stat);
    }

    [Fact]
    public void TalentTree_InitializesCorrectly()
    {
        var tree = new TalentTree
        {
            Id = "tree_1",
            Name = "Warrior Tree",
            Tier = 1,
            RequiredClass = "Warrior",
            ExclusiveGroupId = "core_combat",
            IsHidden = false,
            Prerequisites = new Dictionary<string, int> { { "tree_0", 5 } },
            Nodes = new List<TalentNode> { new TalentNode { Id = "node_1", Name = "Strength I" } }
        };

        Assert.Equal("tree_1", tree.Id);
        Assert.Equal("Warrior Tree", tree.Name);
        Assert.Equal(1, tree.Tier);
        Assert.Equal("Warrior", tree.RequiredClass);
        Assert.Equal("core_combat", tree.ExclusiveGroupId);
        Assert.False(tree.IsHidden);
        Assert.Single(tree.Prerequisites);
        Assert.Equal(5, tree.Prerequisites["tree_0"]);
        Assert.Single(tree.Nodes);
        Assert.Equal("node_1", tree.Nodes[0].Id);
    }
}
