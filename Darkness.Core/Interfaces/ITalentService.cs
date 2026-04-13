using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ITalentService
{
    List<TalentTree> GetAvailableTrees(Character character);
    bool CanPurchaseTalent(Character character, string treeId, string nodeId);
    void PurchaseTalent(Character character, string treeId, string nodeId);
    void ApplyTalentPassives(Character character);
}
