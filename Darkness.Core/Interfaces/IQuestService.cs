using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IQuestService
{
    List<QuestChain> GetAvailableChains(Character character);
    QuestChain? GetChainById(string chainId);
    QuestStep? GetCurrentStep(Character character, string chainId);
    QuestStep? AdvanceStep(Character character, string chainId, string? choiceStepId = null);
    QuestState? GetQuestState(int characterId, string chainId);
    void UpdateQuestState(QuestState state);
    bool IsMainStoryComplete(Character character);
    List<string> GetCompletedChainIds(int characterId);
    List<BranchOption> GetAvailableBranchOptions(Character character, string chainId, string stepId);
    bool GetWorldFlag(int characterId, string key);
    void SetWorldFlag(int characterId, string key, bool value);

    // Arc-level availability (additive; side chains never gate completion).
    List<Arc> GetAvailableArcs(Character character);
    bool IsArcComplete(Character character, string arcId);
    Arc? GetCurrentArc(Character character);
}
