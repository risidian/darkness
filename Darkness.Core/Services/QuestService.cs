using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class QuestService : IQuestService
{
    private readonly IFileSystemService _fileSystem;
    private List<QuestNode> _quests = new();

    public QuestService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
        LoadQuests();
    }

    private void LoadQuests()
    {
        try 
        {
            string json = _fileSystem.ReadAllText("assets/data/quests.json");
            _quests = JsonSerializer.Deserialize<List<QuestNode>>(json) ?? new();
        }
        catch (System.Exception ex)
        {
            // Fallback or log error
            System.Console.WriteLine($"[QuestService] Failed to load quests: {ex.Message}");
        }
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
