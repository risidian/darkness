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

    public CombatData? GetRandomEncounter(string backgroundKey)
    {
        var col = _db.GetCollection<EncounterTable>("encounter_tables");
        var table = col.FindOne(t => t.BackgroundKey == backgroundKey);

        if (table == null || table.Encounters.Count == 0)
        {
            return null;
        }

        int totalWeight = table.Encounters.Sum(e => e.Weight);
        if (totalWeight <= 0)
        {
            return table.Encounters.First().Combat;
        }

        int roll = Random.Shared.Next(totalWeight);
        int currentWeight = 0;

        foreach (var entry in table.Encounters)
        {
            currentWeight += entry.Weight;
            if (roll < currentWeight)
            {
                return entry.Combat;
            }
        }

        return table.Encounters.Last().Combat;
    }

    public CombatData? RollForEncounter(string backgroundKey, double distanceMoved)
    {
        var col = _db.GetCollection<EncounterTable>("encounter_tables");
        var table = col.FindOne(t => t.BackgroundKey == backgroundKey);

        if (table == null || table.Encounters.Count == 0 || distanceMoved < table.EncounterDistance)
        {
            return null;
        }

        int roll = Random.Shared.Next(1, 101);
        if (roll <= table.EncounterChance)
        {
            // Consolidate logic from GetRandomEncounter to avoid another lookup
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
