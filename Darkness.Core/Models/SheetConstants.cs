namespace Darkness.Core.Models;

public static class SheetConstants
{
    public const int FRAME_SIZE = 64;
    public const int OVERSIZE_FRAME_SIZE = 192;
    public const int SHEET_WIDTH = 832;
    public const int SHEET_HEIGHT = 3456;
    public const int COLUMNS = 13;
    public const int ROWS = 54;
    public const int OVERSIZE_Y_OFFSET = 3456;

    public static readonly Dictionary<string, int> AnimationRows = new()
    {
        { "spellcast", 0 },
        { "thrust", 4 },
        { "walk", 8 },
        { "slash", 12 },
        { "shoot", 16 },
        { "hurt", 20 },
        { "climb", 21 },
        { "idle", 22 },
        { "jump", 26 },
        { "sit", 30 },
        { "emote", 34 },
        { "run", 38 },
        { "combat_idle", 42 },
        { "backslash", 46 },
        { "halfslash", 49 }
    };

    public static readonly Dictionary<string, int> FrameCounts = new()
    {
        { "spellcast", 7 },
        { "thrust", 8 },
        { "walk", 9 },
        { "slash", 6 },
        { "shoot", 13 },
        { "hurt", 6 },
        { "climb", 6 },
        { "idle", 3 },
        { "jump", 6 },
        { "sit", 15 },
        { "emote", 15 },
        { "run", 8 },
        { "combat_idle", 3 },
        { "backslash", 12 },
        { "halfslash", 6 },
        { "slash_oversize", 6 },
        { "slash_reverse_oversize", 6 },
        { "thrust_oversize", 8 }
    };
}
