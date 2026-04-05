using Darkness.Core.Models;
using Darkness.Core.Services;

namespace Darkness.Tests.Services;

public class ConditionEvaluatorTests
{
    [Fact]
    public void Evaluate_MoralityGreaterOrEqual_ReturnsTrue_WhenMet()
    {
        var character = new Character { Morality = 10 };
        var condition = new BranchCondition { Type = "morality", Operator = ">=", Value = "5" };
        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_MoralityGreaterOrEqual_ReturnsFalse_WhenNotMet()
    {
        var character = new Character { Morality = 3 };
        var condition = new BranchCondition { Type = "morality", Operator = ">=", Value = "5" };
        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_ClassEquals_ReturnsTrue_WhenMatches()
    {
        var character = new Character { Class = "Mage" };
        var condition = new BranchCondition { Type = "class", Operator = "==", Value = "Mage" };
        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_ClassEquals_ReturnsFalse_WhenDifferent()
    {
        var character = new Character { Class = "Warrior" };
        var condition = new BranchCondition { Type = "class", Operator = "==", Value = "Mage" };
        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_HasItem_ReturnsTrue_WhenInventoryContains()
    {
        var character = new Character { Inventory = new List<Item> { new() { Name = "Iron Key" } } };
        var condition = new BranchCondition { Type = "has_item", Operator = "contains", Value = "Iron Key" };
        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_HasItem_ReturnsFalse_WhenMissing()
    {
        var character = new Character { Inventory = new List<Item>() };
        var condition = new BranchCondition { Type = "has_item", Operator = "contains", Value = "Iron Key" };
        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_QuestCompleted_ReturnsTrue_WhenCompleted()
    {
        var character = new Character();
        var completedChainIds = new List<string> { "beat_1" };
        var condition = new BranchCondition { Type = "quest_completed", Operator = "==", Value = "beat_1" };
        Assert.True(ConditionEvaluator.Evaluate(condition, character, completedChainIds));
    }

    [Fact]
    public void EvaluateAll_NullConditions_ReturnsTrue()
    {
        var character = new Character();
        Assert.True(ConditionEvaluator.EvaluateAll(null, character, new List<string>()));
    }

    [Fact]
    public void EvaluateAll_EmptyConditions_ReturnsTrue()
    {
        var character = new Character();
        Assert.True(ConditionEvaluator.EvaluateAll(new List<BranchCondition>(), character, new List<string>()));
    }

    [Fact]
    public void Evaluate_UnknownType_ReturnsFalse()
    {
        var character = new Character();
        var condition = new BranchCondition { Type = "unknown", Operator = "==", Value = "x" };
        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }
}
