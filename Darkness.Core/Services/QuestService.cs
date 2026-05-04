using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class QuestService : IQuestService
{
    private readonly LiteDatabase _db;
    private readonly ConcurrentDictionary<(int CharacterId, string Key), bool> _flagCache = new();

    public QuestService(LiteDatabase db)
    {
        _db = db;

        var col = _db.GetCollection<WorldFlag>("world_flags");
        col.EnsureIndex(f => f.CharacterId);
        col.EnsureIndex(f => f.Key);
    }

    public List<QuestChain> GetAvailableChains(Character character)
    {
        var chainCol = _db.GetCollection<QuestChain>("quest_chains");
        var stateCol = _db.GetCollection<QuestState>("quest_states");

        var completedIds = GetCompletedChainIds(character.Id);
        var blockedIds = GetBlockedChainIds(character.Id);
        var allChains = chainCol.FindAll().ToList();

        var available = allChains.Where(c =>
            !completedIds.Contains(c.Id) &&
            !blockedIds.Contains(c.Id) &&
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

        // If state is already completed or blocked, do nothing
        if (state?.Status == "completed")
        {
            Console.Error.WriteLine($"[QuestService] WARN: Attempted to advance a completed quest chain: {chainId}");
            return null;
        }
        if (state?.Status == "blocked")
        {
            Console.Error.WriteLine($"[QuestService] WARN: Attempted to advance a blocked quest chain: {chainId}");
            return null;
        }

        var currentStepId = state?.CurrentStepId;
        var currentStep = currentStepId != null
            ? chain.Steps.FirstOrDefault(s => s.Id == currentStepId)
            : chain.Steps.FirstOrDefault();

        if (currentStep == null)
        {
            Console.Error.WriteLine($"[QuestService] ERROR: Current step '{currentStepId}' not found in chain '{chainId}'. Marking blocked.");
            MarkBlocked(stateCol, state, character.Id, chainId, currentStepId ?? string.Empty);
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
                ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds, key => GetWorldFlag(character.Id, key)));

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
                var completedChainIds = GetCompletedChainIds(character.Id);
                if (!ConditionEvaluator.EvaluateAll(nextStep.Requirements, character, completedChainIds, key => GetWorldFlag(character.Id, key)))
                {
                    Console.Error.WriteLine($"[QuestService] WARN: Requirements for next step '{nextStepId}' not met for character {character.Id}");
                    return null;
                }

                // Grant rewards from the step we are finishing
                GrantRewards(character, currentStep.Rewards);

                if (state == null)
                {
                    state = new QuestState
                    {
                        CharacterId = character.Id,
                        ChainId = chainId,
                        CurrentStepId = nextStepId,
                        Status = "in_progress",
                        CurrentCombatSnapshot = null
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
                // Broken NextStepId — mark chain blocked so we don't soft-loop replaying step 0.
                Console.Error.WriteLine($"[QuestService] ERROR: Next step '{nextStepId}' defined in data but missing from chain '{chainId}' steps. Marking blocked.");
                MarkBlocked(stateCol, state, character.Id, chainId, currentStep.Id);
                return null;
            }
        }

        // Truly no next step — chain is complete
        // Grant rewards from the last step
        GrantRewards(character, currentStep.Rewards);

        if (state == null)
        {
            state = new QuestState
            {
                CharacterId = character.Id,
                ChainId = chainId,
                CurrentStepId = currentStep.Id,
                Status = "completed",
                CurrentCombatSnapshot = null
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

    private static void MarkBlocked(ILiteCollection<QuestState> stateCol, QuestState? state, int characterId, string chainId, string currentStepId)
    {
        if (state == null)
        {
            stateCol.Insert(new QuestState
            {
                CharacterId = characterId,
                ChainId = chainId,
                CurrentStepId = currentStepId,
                Status = "blocked",
                CurrentCombatSnapshot = null
            });
        }
        else
        {
            state.Status = "blocked";
            state.CurrentStepId = currentStepId;
            state.CurrentCombatSnapshot = null;
            stateCol.Update(state);
        }
    }

    private void GrantRewards(Character character, List<QuestReward> rewards)
    {
        if (rewards == null || rewards.Count == 0) return;

        foreach (var reward in rewards)
        {
            switch (reward.Type)
            {
                case QuestRewardType.Experience:
                    if (int.TryParse(reward.Value, out var xp))
                        character.Experience += xp;
                    break;
                case QuestRewardType.Gold:
                    if (int.TryParse(reward.Value, out var gold))
                        character.Gold += gold;
                    break;
                case QuestRewardType.Item:
                    var itemCol = _db.GetCollection<Item>("items");
                    var item = itemCol.FindOne(i => i.Name == reward.Value);
                    if (item != null)
                    {
                        for (int i = 0; i < reward.Amount; i++)
                        {
                            // Clone() keeps a faithful copy; we explicitly set Quantity = 1
                            // so reward.Amount loops produce the right total after consolidation.
                            var copy = item.Clone();
                            copy.Quantity = 1;
                            character.Inventory.Add(copy);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"[QuestService] ERROR: Reward item '{reward.Value}' not found in database.");
                    }
                    break;
                case QuestRewardType.WorldFlag:
                    SetWorldFlag(character.Id, reward.Value, true);
                    break;
                case QuestRewardType.AttributePoint:
                    if (int.TryParse(reward.Value, out var pts))
                        character.AttributePoints += pts;
                    break;
            }
        }
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

    private List<string> GetBlockedChainIds(int characterId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        return col.Find(s => s.CharacterId == characterId && s.Status == "blocked")
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
            .Where(o => ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds, key => GetWorldFlag(character.Id, key)))
            .ToList();
    }

    public bool GetWorldFlag(int characterId, string key)
    {
        if (_flagCache.TryGetValue((characterId, key), out bool cachedValue))
            return cachedValue;

        var col = _db.GetCollection<WorldFlag>("world_flags");
        var flag = col.FindById(WorldFlag.MakeCompoundKey(characterId, key));
        bool value = flag != null && flag.Value;

        _flagCache[(characterId, key)] = value;
        return value;
    }

    public void SetWorldFlag(int characterId, string key, bool value)
    {
        _flagCache[(characterId, key)] = value;

        var col = _db.GetCollection<WorldFlag>("world_flags");
        var compoundKey = WorldFlag.MakeCompoundKey(characterId, key);
        col.Upsert(new WorldFlag
        {
            CompoundKey = compoundKey,
            CharacterId = characterId,
            Key = key,
            Value = value
        });
    }

}
