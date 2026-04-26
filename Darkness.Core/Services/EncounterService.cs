using System;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class EncounterService : IEncounterService
{
    private readonly LiteDatabase _db;

    public EncounterService(LiteDatabase db)
    {
        _db = db;
    }

    public CombatData? RollForEncounter(string backgroundKey, double distanceMoved, out bool didRoll)
    {
        didRoll = false;

        var col = _db.GetCollection<EncounterTable>("encounter_tables");
        var table = col.FindOne(t => t.BackgroundKey == backgroundKey);

        if (table == null || table.Encounters.Count == 0 || distanceMoved < table.EncounterDistance)
        {
            return null;
        }

        // Distance threshold was met — flag so caller resets distance
        didRoll = true;

        int roll = Random.Shared.Next(1, 101);
        if (roll <= table.EncounterChance)
        {
            int totalWeight = table.Encounters.Sum(e => e.Weight);
            if (totalWeight <= 0)
            {
                return table.Encounters.First().Combat;
            }

            int weightRoll = Random.Shared.Next(totalWeight);
            int currentWeight = 0;

            foreach (var entry in table.Encounters)
            {
                currentWeight += entry.Weight;
                if (weightRoll < currentWeight)
                {
                    return entry.Combat;
                }
            }

            return table.Encounters.Last().Combat;
        }

        return null;
    }
}
