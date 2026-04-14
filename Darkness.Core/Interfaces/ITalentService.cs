using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ITalentService
{
    List<TalentTree> GetAvailableTrees(Character character);
    TalentPurchaseResult CanPurchaseTalent(Character character, string treeId, string nodeId);
    void PurchaseTalent(Character character, string treeId, string nodeId);
    void UnlockStartingTalents(Character character);
    void ApplyTalentPassives(Character character);
}
