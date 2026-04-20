using System.Collections.Generic;

namespace Darkness.Core.Models;

public class CalendarReward
{
    public int Month { get; set; }
    public List<string> Items { get; set; } = new();
}
