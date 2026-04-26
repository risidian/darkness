using System;
using System.Collections.Generic;

namespace Darkness.Core.Models;

public class EncounterTable
{
    [LiteDB.BsonId]
    public string BackgroundKey { get; set; } = string.Empty;

    private int _encounterChance = 5;
    public int EncounterChance
    {
        get => _encounterChance;
        set => _encounterChance = Math.Clamp(value, 0, 100);
    }

    private float _encounterDistance = 1000f;
    public float EncounterDistance
    {
        get => _encounterDistance;
        set => _encounterDistance = Math.Max(1f, value);
    }

    public List<EncounterEntry> Encounters { get; set; } = new();
}
