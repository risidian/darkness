using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class QuestService : IQuestService
{
    private readonly List<QuestNode> _quests = new();

    public QuestService()
    {
        // Sample Story Beat 1
        _quests.Add(new QuestNode 
        { 
            Id = "main_1", 
            Title = "The Beginning", 
            IsMainStory = true,
            Encounter = new EncounterData 
            {
                Enemies = new List<Enemy> { new Enemy { Name = "Shadow Minion", MaxHP = 50, CurrentHP = 50, Attack = 10 } }
            }
        });

        // Sample Side Quest 1
        _quests.Add(new QuestNode 
        { 
            Id = "side_1", 
            Title = "Creek Monster", 
            IsMainStory = false,
            Prerequisites = new List<string> { "main_1" },
            Encounter = new EncounterData 
            {
                Enemies = new List<Enemy> { new Enemy { Name = "Water Drake", MaxHP = 120, CurrentHP = 120, Attack = 25 } }
            }
        });
    }

    public List<QuestNode> GetAvailableQuests(Character character)
    {
        return _quests.Where(q => 
            !character.CompletedQuestIds.Contains(q.Id) && 
            q.Prerequisites.All(p => character.CompletedQuestIds.Contains(p))).ToList();
    }

    public QuestNode? GetQuestById(string id) => _quests.FirstOrDefault(q => q.Id == id);

    public void CompleteQuest(Character character, string questId)
    {
        if (!character.CompletedQuestIds.Contains(questId))
            character.CompletedQuestIds.Add(questId);
    }

    public QuestNode? GetQuestByLocation(string locationKey)
    {
        // Simple mapping for demo
        if (locationKey == "SandyShore_East") return _quests.FirstOrDefault(q => q.Id == "main_1");
        return null;
    }
}
