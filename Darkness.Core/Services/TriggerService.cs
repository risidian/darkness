using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class TriggerService : ITriggerService
{
    private readonly IQuestService _questService;

    public TriggerService(IQuestService questService)
    {
        _questService = questService;
    }

    public QuestStep? CheckLocationTrigger(Character character, string locationKey)
    {
        var availableChains = _questService.GetAvailableChains(character);

        foreach (var chain in availableChains)
        {
            var currentStep = _questService.GetCurrentStep(character, chain.Id);
            if (currentStep?.Location?.LocationKey == locationKey)
                return currentStep;
        }

        return null;
    }
}
