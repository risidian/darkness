using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class TriggerService : ITriggerService
{
    private readonly IQuestService _questService;
    private readonly Dictionary<string, DateTime> _lastTriggerActivation = new();
    private static readonly TimeSpan TriggerCooldown = TimeSpan.FromMilliseconds(500);

    public TriggerService(IQuestService questService)
    {
        _questService = questService;
    }

    public QuestStep? CheckLocationTrigger(Character character, string locationKey)
    {
        // Simple debounce to prevent "bouncing" when near trigger boundaries
        if (_lastTriggerActivation.TryGetValue(locationKey, out var lastTime))
        {
            if (DateTime.UtcNow - lastTime < TriggerCooldown)
            {
                return null;
            }
        }

        var availableChains = _questService.GetAvailableChains(character);

        foreach (var chain in availableChains)
        {
            var currentStep = _questService.GetCurrentStep(character, chain.Id);
            if (currentStep?.Location?.LocationKey == locationKey)
            {
                _lastTriggerActivation[locationKey] = DateTime.UtcNow;
                return currentStep;
            }
        }

        return null;
    }
}
