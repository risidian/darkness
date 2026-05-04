using System;
using System.Collections.Concurrent;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class EncounterService : IEncounterService
{
    private readonly LiteDatabase _db;
    private readonly ConcurrentDictionary<string, EncounterTable?> _tableCache = new();

    public EncounterService(LiteDatabase db)
    {
        _db = db;
    }

    public CombatData? RollForEncounter(string backgroundKey, double distanceMoved, out bool didRoll)
    {
        didRoll = false;

        var table = _tableCache.GetOrAdd(backgroundKey, key =>
            _db.GetCollection<EncounterTable>("encounter_tables").FindOne(t => t.BackgroundKey == key));

        if (table == null) return null;

        if (distanceMoved < table.EncounterDistance) return null;

        // Distance threshold met — reset counter regardless of outcome (or empty table).
        didRoll = true;

        if (table.Encounters.Count == 0) return null;

        int roll = Random.Shared.Next(1, 101);
        if (roll > table.EncounterChance) return null;

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
}
