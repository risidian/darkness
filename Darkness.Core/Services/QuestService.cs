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
            return null;
        }

        var stateCol = _db.GetCollection<QuestState>("quest_states");
        var state = GetQuestState(character.Id, chainId);
        var currentStep = state != null
            ? chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId)
            : chain.Steps.FirstOrDefault();

        if (currentStep == null)
        {
            return null;
        }

        string? nextStepId = null;

        if (choiceStepId != null && currentStep.Branch != null)
        {
            var completedChainIds = GetCompletedChainIds(character.Id);
            var option = currentStep.Branch.Options.FirstOrDefault(o =>
                o.NextStepId == choiceStepId &&
                ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds));
            if (option != null)
            {
                character.Morality += option.MoralityImpact;
                nextStepId = option.NextStepId;
            }
        }
        else
        {
            nextStepId = currentStep.NextStepId;
        }

        if (nextStepId != null)
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
                    stateCol.Update(state);
                }
                return nextStep;
            }
        }

        // No next step — chain is complete
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
            stateCol.Update(state);
        }

        return null;
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
