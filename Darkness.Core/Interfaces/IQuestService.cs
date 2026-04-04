using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IQuestService
{
    List<QuestNode> GetAvailableQuests(Character character);
    QuestNode? GetQuestById(string id);
    void CompleteQuest(Character character, string questId);
    QuestNode? GetQuestByLocation(Character character, string locationKey);
    QuestNode? GetNextAvailableMainStoryQuest(Character character);
}
