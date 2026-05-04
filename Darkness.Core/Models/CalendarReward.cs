using System.Collections.Generic;
using LiteDB;

namespace Darkness.Core.Models;

public class CalendarReward
{
    [BsonId]
    public int Month { get; set; }
    public List<string> Items { get; set; } = new();
}
