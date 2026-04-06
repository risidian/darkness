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
        stateCol.EnsureIndex(s => s.CharacterId);
        stateCol.EnsureIndex(s => s.Status);

        var completedIds = GetCompletedChainIds(character.Id);
        var allChains = chainCol.FindAll().ToList();

        Console.Error.WriteLine($"[QuestService] GetAvailableChains for Char {character.Id} ({character.Name})");
        Console.Error.WriteLine($"[QuestService]   Completed IDs found: [{string.Join(", ", completedIds)}]");

        var available = allChains.Where(c =>
            !completedIds.Contains(c.Id) &&
            c.Prerequisites.All(p => completedIds.Contains(p))
        ).OrderBy(c => c.SortOrder).ToList();

        Console.Error.WriteLine($"[QuestService]   Available Chains: [{string.Join(", ", available.Select(a => a.Id))}]");

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
            Console.Error.WriteLine($"[QuestService] GetCurrentStep: No state for {chainId}, returning first step {chain.Steps[0].Id}");
            return chain.Steps[0];
        }

        var step = chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId);
        Console.Error.WriteLine($"[QuestService] GetCurrentStep: {chainId} state exists. CurrentStepId={state.CurrentStepId}, Status={state.Status}. Returning step: {step?.Id ?? "NULL"}");
        return step;
    }

    public QuestStep? AdvanceStep(Character character, string chainId, string? choiceStepId = null)
    {
        Console.Error.WriteLine($"[QuestService] AdvanceStep START: Char={character.Id}, Chain={chainId}, Choice={choiceStepId ?? "None"}");
        var chain = GetChainById(chainId);
        if (chain == null) 
        {
            Console.Error.WriteLine($"[QuestService] AdvanceStep ERROR: Chain {chainId} not found!");
            return null;
        }

        var stateCol = _db.GetCollection<QuestState>("quest_states");
        var state = GetQuestState(character.Id, chainId);
        var currentStep = state != null
            ? chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId)
            : chain.Steps.FirstOrDefault();

        if (currentStep == null) 
        {
            Console.Error.WriteLine($"[QuestService] AdvanceStep ERROR: Current step not found for chain {chainId}");
            return null;
        }

        string? nextStepId = null;

        if (choiceStepId != null && currentStep.Branch != null)
        {
            var option = currentStep.Branch.Options.FirstOrDefault(o => o.NextStepId == choiceStepId);
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
                    var newId = stateCol.Insert(state);
                    Console.Error.WriteLine($"[QuestService] AdvanceStep: Inserted new state for {chainId}. Step={nextStepId}. DB ID={newId}");
                }
                else
                {
                    state.CurrentStepId = nextStepId;
                    state.Status = "in_progress";
                    bool ok = stateCol.Update(state);
                    Console.Error.WriteLine($"[QuestService] AdvanceStep: Updated state for {chainId} to {nextStepId}. Success={ok}");
                }
                return nextStep;
            }
        }

        // No next step — chain is complete
        Console.Error.WriteLine($"[QuestService] AdvanceStep: No next step. Marking chain {chainId} as COMPLETED.");
        if (state == null)
        {
            state = new QuestState
            {
                CharacterId = character.Id,
                ChainId = chainId,
                CurrentStepId = currentStep.Id,
                Status = "completed"
            };
            var newId = stateCol.Insert(state);
            Console.Error.WriteLine($"[QuestService] AdvanceStep: Inserted COMPLETED state for {chainId}. DB ID={newId}");
        }
        else
        {
            state.Status = "completed";
            state.CurrentStepId = currentStep.Id;
            bool ok = stateCol.Update(state);
            Console.Error.WriteLine($"[QuestService] AdvanceStep: Updated state for {chainId} to COMPLETED. Success={ok}");
        }

        return null;
    }

    public QuestState? GetQuestState(int characterId, string chainId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        var state = col.FindOne(s => s.CharacterId == characterId && s.ChainId == chainId);
        if (state != null)
            Console.Error.WriteLine($"[QuestService] GetQuestState Found: Chain={state.ChainId}, Char={state.CharacterId}, Step={state.CurrentStepId}, Status={state.Status}");
        return state;
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
        var completed = col.Find(s => s.CharacterId == characterId && s.Status == "completed").ToList();
        
        Console.Error.WriteLine($"[QuestService] GetCompletedChainIds for Char {characterId}: Found {completed.Count} records.");
        foreach(var s in completed)
        {
            Console.Error.WriteLine($"[QuestService]   -> {s.ChainId} (Status: {s.Status})");
        }

        // Diagnostic: What about other states?
        var allStates = col.Find(s => s.CharacterId == characterId).ToList();
        if (allStates.Count > completed.Count)
        {
            Console.Error.WriteLine($"[QuestService]   (Total states for char: {allStates.Count})");
            foreach(var s in allStates.Where(x => x.Status != "completed"))
            {
                Console.Error.WriteLine($"[QuestService]   (Other: {s.ChainId} Status={s.Status} Step={s.CurrentStepId})");
            }
        }

        return completed.Select(s => s.ChainId).ToList();
    }
}
