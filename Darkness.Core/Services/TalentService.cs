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
        var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll();
        return allTrees.Where(tree => IsTreeAvailable(character, tree)).ToList();
    }

    private bool IsTreeAvailable(Character character, TalentTree tree)
    {
        foreach (var prereq in tree.Prerequisites)
        {
            if (prereq.Key == "Level" && character.Level < prereq.Value) return false;
            if (prereq.Key == "Strength" && character.Strength < prereq.Value) return false;
            if (prereq.Key == "Dexterity" && character.Dexterity < prereq.Value) return false;
            if (prereq.Key == "Constitution" && character.Constitution < prereq.Value) return false;
            if (prereq.Key == "Intelligence" && character.Intelligence < prereq.Value) return false;
            if (prereq.Key == "Wisdom" && character.Wisdom < prereq.Value) return false;
            if (prereq.Key == "Charisma" && character.Charisma < prereq.Value) return false;

            if (prereq.Key.StartsWith("SpentInTree:"))
            {
                var targetTreeId = prereq.Key.Replace("SpentInTree:", "");
                var targetTree = _db.GetCollection<TalentTree>("talent_trees").FindOne(t => t.Id == targetTreeId);
                if (targetTree == null) return false;

                var spent = character.UnlockedTalentIds.Count(id => targetTree.Nodes.Any(n => n.Id == id));
                if (spent < prereq.Value) return false;
            }
        }
        return true;
    }

    public bool CanPurchaseTalent(Character character, string treeId, string nodeId)
    {
        var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
        if (tree == null) return false;

        var node = tree.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return false;

        if (character.TalentPoints < node.PointsRequired) return false;
        if (character.UnlockedTalentIds.Contains(nodeId)) return false;
        if (!IsTreeAvailable(character, tree)) return false;

        if (!string.IsNullOrEmpty(node.PrerequisiteNodeId) && !character.UnlockedTalentIds.Contains(node.PrerequisiteNodeId))
            return false;

        return true;
    }

    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        if (CanPurchaseTalent(character, treeId, nodeId))
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

        // Clear existing bonuses
        character.StatBonuses.Clear();

        foreach (var talentId in character.UnlockedTalentIds)
        {
            var node = allNodes.FirstOrDefault(n => n.Id == talentId);
            if (node?.Effect != null && !string.IsNullOrEmpty(node.Effect.Stat))
            {
                if (!character.StatBonuses.ContainsKey(node.Effect.Stat))
                {
                    character.StatBonuses[node.Effect.Stat] = 0;
                }
                character.StatBonuses[node.Effect.Stat] += node.Effect.Value;
            }
        }
        
        character.RecalculateDerivedStats();
    }
}
