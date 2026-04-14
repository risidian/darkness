using Darkness.Core.Models;

namespace Darkness.Core.Logic;

public class LpcAnimationHelper
{
    public (int X, int Y, int Width, int Height) GetFrameRect(string animation, int direction, int frameIndex)
    {
        if (!SheetConstants.AnimationRows.TryGetValue(animation, out var startRow)) return (0, 0, 0, 0);
        int row = startRow + direction;
        return (frameIndex * SheetConstants.FRAME_SIZE, row * SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE);
    }

    public (int X, int Y, int Width, int Height) GetOversizeFrameRect(string customAnimation, int direction, int frameIndex)
    {
        int rowOffset = 0;
        if (customAnimation == "slash_oversize") rowOffset = 0;
        else if (customAnimation == "slash_reverse_oversize") rowOffset = 4;
        else if (customAnimation == "thrust_oversize") rowOffset = 8;
        else return (0, 0, 0, 0);

        int row = rowOffset + direction;
        return (frameIndex * SheetConstants.OVERSIZE_FRAME_SIZE, 
                SheetConstants.OVERSIZE_Y_OFFSET + (row * SheetConstants.OVERSIZE_FRAME_SIZE), 
                SheetConstants.OVERSIZE_FRAME_SIZE, SheetConstants.OVERSIZE_FRAME_SIZE);
    }

    public int GetFrameCount(string animation) => SheetConstants.FrameCounts.GetValueOrDefault(animation, 0);
    
    public bool IsOversize(string animation) => animation.EndsWith("_oversize");
}
