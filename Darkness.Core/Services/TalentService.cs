using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System;
using System.Collections.Generic;

namespace Darkness.Core.Services;

public class TalentService : ITalentService
{
    private readonly LiteDatabase _db;

    public TalentService(LiteDatabase db)
    {
        _db = db;
    }

    public List<TalentTree> GetAvailableTrees(Character character)
    {
        return new List<TalentTree>();
    }

    public bool CanPurchaseTalent(Character character, string treeId, string nodeId)
    {
        return false;
    }

    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        throw new NotImplementedException();
    }

    public void ApplyTalentPassives(Character character)
    {
        // Skeletal implementation
    }
}
