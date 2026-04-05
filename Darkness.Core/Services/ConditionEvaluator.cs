using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public static class ConditionEvaluator
{
    public static bool EvaluateAll(List<BranchCondition>? conditions, Character character, List<string> completedChainIds)
    {
        if (conditions == null || conditions.Count == 0)
            return true;
        return conditions.All(c => Evaluate(c, character, completedChainIds));
    }

    public static bool Evaluate(BranchCondition condition, Character character, List<string> completedChainIds)
    {
        return condition.Type switch
        {
            "morality" => EvaluateNumeric(character.Morality, condition.Operator, condition.Value),
            "class" => condition.Operator == "==" && string.Equals(character.Class, condition.Value, StringComparison.OrdinalIgnoreCase),
            "has_item" => character.Inventory.Any(i => string.Equals(i.Name, condition.Value, StringComparison.OrdinalIgnoreCase)),
            "quest_completed" => completedChainIds.Contains(condition.Value),
            _ => false
        };
    }

    private static bool EvaluateNumeric(int actual, string op, string valueStr)
    {
        if (!int.TryParse(valueStr, out var value))
            return false;
        return op switch
        {
            ">=" => actual >= value,
            "<=" => actual <= value,
            "==" => actual == value,
            ">" => actual > value,
            "<" => actual < value,
            _ => false
        };
    }
}
