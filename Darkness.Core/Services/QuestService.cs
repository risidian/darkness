using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class QuestService : IQuestService
{
    private readonly LiteDatabase _db;

    public QuestService(LiteDatabase db)
    {
        _db = db;
    }

    public List<QuestChain> GetAvailableChains(Character character)
    {
        var chainCol = _db.GetCollection<QuestChain>("quest_chains");
        var stateCol = _db.GetCollection<QuestState>("quest_states");

        var completedIds = GetCompletedChainIds(character.Id);
        var allChains = chainCol.FindAll().ToList();

        var available = allChains.Where(c =>
            !completedIds.Contains(c.Id) &&
            c.Prerequisites.All(p => completedIds.Contains(p))
        ).OrderBy(c => c.SortOrder).ToList();

        return available;
    }

    public QuestChain? GetChainById(string chainId)
    {
        var col = _db.GetCollection<QuestChain>("quest_chains");
        return col.FindOne(c => c.Id == chainId);
    }

    public QuestStep? GetCurrentStep(Character character, string chainId)
    {
        var chain = GetChainById(chainId);
        if (chain == null || chain.Steps.Count == 0) return null;

        var state = GetQuestState(character.Id, chainId);
        if (state == null)
        {
            return chain.Steps[0];
        }

        var step = chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId);
        return step;
    }

    public QuestStep? AdvanceStep(Character character, string chainId, string? choiceStepId = null)
    {
        var chain = GetChainById(chainId);
        if (chain == null)
        {
            Console.Error.WriteLine($"[QuestService] ERROR: Quest chain not found: {chainId}");
            return null;
        }

        var stateCol = _db.GetCollection<QuestState>("quest_states");
        var state = GetQuestState(character.Id, chainId);
        
        // If state is already completed, do nothing
        if (state?.Status == "completed")
        {
            Console.Error.WriteLine($"[QuestService] WARN: Attempted to advance a completed quest chain: {chainId}");
            return null;
        }

        var currentStepId = state?.CurrentStepId;
        var currentStep = currentStepId != null
            ? chain.Steps.FirstOrDefault(s => s.Id == currentStepId)
            : chain.Steps.FirstOrDefault();

        if (currentStep == null)
        {
            Console.Error.WriteLine($"[QuestService] ERROR: Current step '{currentStepId}' not found in chain '{chainId}'");
            return null;
        }

        string? nextStepId = null;
        int pendingMoralityImpact = 0;

        if (choiceStepId != null)
        {
            if (currentStep.Branch == null)
            {
                Console.Error.WriteLine($"[QuestService] ERROR: Choice '{choiceStepId}' provided but current step '{currentStep.Id}' has no branch data.");
                return null;
            }

            var completedChainIds = GetCompletedChainIds(character.Id);
            var option = currentStep.Branch.Options.FirstOrDefault(o =>
                o.NextStepId == choiceStepId &&
                ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds));

            if (option != null)
            {
                nextStepId = option.NextStepId;
                pendingMoralityImpact = option.MoralityImpact;
            }
            else
            {
                Console.Error.WriteLine($"[QuestService] ERROR: Invalid or unmet choice '{choiceStepId}' for step '{currentStep.Id}' in chain '{chainId}'");
                return null;
            }
        }
        else
        {
            nextStepId = currentStep.NextStepId;
        }

        if (!string.IsNullOrEmpty(nextStepId))
        {
            var nextStep = chain.Steps.FirstOrDefault(s => s.Id == nextStepId);
            if (nextStep != null)
            {
                if (state == null)
                {
                    state = new QuestState
                    {
                        CharacterId = character.Id,
                        ChainId = chainId,
                        CurrentStepId = nextStepId,
                        Status = "in_progress"
                    };
                    stateCol.Insert(state);
                }
                else
                {
                    state.CurrentStepId = nextStepId;
                    state.Status = "in_progress";
                    state.CurrentCombatSnapshot = null;
                    stateCol.Update(state);
                }
                // Apply morality only after state has been persisted
                character.Morality += pendingMoralityImpact;
                return nextStep;
            }
            else
            {
                Console.Error.WriteLine($"[QuestService] ERROR: Next step '{nextStepId}' defined in data but missing from chain '{chainId}' steps.");
                return null;
            }
        }

        // Truly no next step — chain is complete
        if (state == null)
        {
            state = new QuestState
            {
                CharacterId = character.Id,
                ChainId = chainId,
                CurrentStepId = currentStep.Id,
                Status = "completed"
            };
            stateCol.Insert(state);
        }
        else
        {
            state.Status = "completed";
            state.CurrentStepId = currentStep.Id;
            state.CurrentCombatSnapshot = null;
            stateCol.Update(state);
        }

        return null;
    }

    public void UpdateQuestState(QuestState state)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        col.Update(state);
    }

    public QuestState? GetQuestState(int characterId, string chainId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        return col.FindOne(s => s.CharacterId == characterId && s.ChainId == chainId);
    }

    public bool IsMainStoryComplete(Character character)
    {
        var chainCol = _db.GetCollection<QuestChain>("quest_chains");
        var mainChains = chainCol.Find(c => c.IsMainStory).ToList();
        if (mainChains.Count == 0) return false;

        var completedIds = GetCompletedChainIds(character.Id);
        return mainChains.All(c => completedIds.Contains(c.Id));
    }

    public List<string> GetCompletedChainIds(int characterId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        return col.Find(s => s.CharacterId == characterId && s.Status == "completed")
            .Select(s => s.ChainId)
            .ToList();
    }

    public List<BranchOption> GetAvailableBranchOptions(Character character, string chainId, string stepId)
    {
        var chain = GetChainById(chainId);
        var step = chain?.Steps.FirstOrDefault(s => s.Id == stepId);
        if (step?.Branch == null) return new List<BranchOption>();

        var completedChainIds = GetCompletedChainIds(character.Id);
        return step.Branch.Options
            .Where(o => ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds))
            .ToList();
    }
}
