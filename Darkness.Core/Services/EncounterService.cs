using System;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class EncounterService : IEncounterService
{
    private readonly LiteDatabase _db;
    private readonly Random _random = new();

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
        int roll = _random.Next(totalWeight);
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
        return null;
    }
}
