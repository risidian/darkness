using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Core.Services;

public class TalentService : ITalentService
{
    private readonly LiteDatabase _db;

    public TalentService(LiteDatabase db)
    {
        _db = db;
    }

    public List<TalentTree> GetAvailableTrees(Character character)
    {
        var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        return allTrees.Where(tree => 
        {
            var result = IsTreeAvailable(character, tree);
            if (tree.IsHidden)
            {
                // Hidden trees show up if prerequisites are met OR if points are already spent
                var metPrereqs = result.Success;
                var hasPoints = character.UnlockedTalentIds.Any(id => tree.Nodes.Any(n => n.Id == id));
                return metPrereqs || hasPoints;
            }
            return result.Success;
        }).ToList();
    }

    private TalentPurchaseResult IsTreeAvailable(Character character, TalentTree tree)
    {
        if (!string.IsNullOrEmpty(tree.RequiredClass) && character.Class != tree.RequiredClass)
        {
            return TalentPurchaseResult.Failed($"Requires Class: {tree.RequiredClass}");
        }

        if (!string.IsNullOrEmpty(tree.ExclusiveGroupId))
        {
            var otherTreesInGroup = _db.GetCollection<TalentTree>("talent_trees")
                .Find(t => t.ExclusiveGroupId == tree.ExclusiveGroupId && t.Id != tree.Id)
                .ToList();

            foreach (var otherTree in otherTreesInGroup)
            {
                if (character.UnlockedTalentIds.Any(id => otherTree.Nodes.Any(n => n.Id == id)))
                {
                    return TalentPurchaseResult.Failed($"Locked by Exclusive Group: {otherTree.Name}");
                }
            }
        }

        foreach (var prereq in tree.Prerequisites)
        {
            if (prereq.Key == "Level" && character.Level < prereq.Value) return TalentPurchaseResult.Failed($"Requires Level {prereq.Value}");
            if (prereq.Key == "Strength" && character.Strength < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Strength");
            if (prereq.Key == "Dexterity" && character.Dexterity < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Dexterity");
            if (prereq.Key == "Constitution" && character.Constitution < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Constitution");
            if (prereq.Key == "Intelligence" && character.Intelligence < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Intelligence");
            if (prereq.Key == "Wisdom" && character.Wisdom < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Wisdom");
            if (prereq.Key == "Charisma" && character.Charisma < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} Charisma");

            if (prereq.Key.StartsWith("SpentInTree:"))
            {
                var targetTreeId = prereq.Key.Replace("SpentInTree:", "");
                var targetTree = _db.GetCollection<TalentTree>("talent_trees").FindOne(t => t.Id == targetTreeId);
                if (targetTree == null) return TalentPurchaseResult.Failed($"Required tree '{targetTreeId}' not found");

                var spent = character.UnlockedTalentIds.Count(id => targetTree.Nodes.Any(n => n.Id == id));
                if (spent < prereq.Value) return TalentPurchaseResult.Failed($"Requires {prereq.Value} points in {targetTree.Name}");
            }
        }
        return TalentPurchaseResult.Succeeded();
    }

    public TalentPurchaseResult CanPurchaseTalent(Character character, string treeId, string nodeId)
    {
        var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
        if (tree == null) return TalentPurchaseResult.Failed("Talent tree not found.");

        var node = tree.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return TalentPurchaseResult.Failed("Talent node not found.");

        if (character.UnlockedTalentIds.Contains(nodeId)) return TalentPurchaseResult.Failed("Talent already unlocked.");

        var treeAvailable = IsTreeAvailable(character, tree);
        if (!treeAvailable.Success) return treeAvailable;

        foreach (var prereqId in node.PrerequisiteNodeIds)
        {
            if (!character.UnlockedTalentIds.Contains(prereqId))
            {
                var prereqNode = tree.Nodes.FirstOrDefault(n => n.Id == prereqId);
                return TalentPurchaseResult.Failed($"Requires {prereqNode?.Name ?? prereqId}");
            }
        }

        if (character.TalentPoints < node.PointsRequired) return TalentPurchaseResult.Failed($"Not enough talent points (Need {node.PointsRequired})");

        return TalentPurchaseResult.Succeeded();
    }

    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        var result = CanPurchaseTalent(character, treeId, nodeId);
        if (result.Success)
        {
            var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
            var node = tree.Nodes.First(n => n.Id == nodeId);
            
            character.UnlockedTalentIds.Add(nodeId);
            character.TalentPoints -= node.PointsRequired;
        }
    }

    public void ApplyTalentPassives(Character character)
    {
        var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        var allNodes = allTrees.SelectMany(t => t.Nodes).ToList();

        character.TalentStatBonuses.Clear();

        foreach (var talentId in character.UnlockedTalentIds)
        {
            var node = allNodes.FirstOrDefault(n => n.Id == talentId);
            if (node?.Effect != null && !string.IsNullOrEmpty(node.Effect.Stat))
            {
                if (!character.TalentStatBonuses.ContainsKey(node.Effect.Stat))
                {
                    character.TalentStatBonuses[node.Effect.Stat] = 0;
                }
                character.TalentStatBonuses[node.Effect.Stat] += node.Effect.Value;
            }
        }
        
        character.RecalculateDerivedStats();
    }
}
