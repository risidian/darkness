using System.Collections.Generic;

namespace Darkness.Core.Models;

public class EncounterTable
{
    [LiteDB.BsonId]
    public string BackgroundKey { get; set; } = string.Empty;
    public int EncounterChance { get; set; } = 5; // Default 5%
    public float EncounterDistance { get; set; } = 1000f; // Default 1000px
    public List<EncounterEntry> Encounters { get; set; } = new();
}
