using Godot;

namespace Darkness.Godot.Core;

public static class UiUtils
{
    public const int MobileMinHeight = 80;
    public const int MobileFontSize = 24;

    public static void ApplyMobileSizing(this Control node)
    {
        if (node == null) return;
        
        // Ensure height is at least 80px
        var minSize = node.CustomMinimumSize;
        if (minSize.Y < MobileMinHeight)
        {
            node.CustomMinimumSize = new Vector2(minSize.X, MobileMinHeight);
        }

        // Apply font size overrides to common interactive nodes
        if (node is Button btn)
        {
            btn.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
        else if (node is LineEdit le)
        {
            le.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
        else if (node is OptionButton ob)
        {
            ob.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
    }
}
