using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ITriggerService
{
    QuestStep? CheckLocationTrigger(Character character, string locationKey);
}
