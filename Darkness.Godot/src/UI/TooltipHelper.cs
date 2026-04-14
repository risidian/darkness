using Godot;

namespace Darkness.Godot.UI;

public static class TooltipHelper
{
    public static void Bind(Control control, string text, Tooltip tooltipInstance)
    {
        control.SetMeta("TooltipText", text);

        if (control.HasMeta("HasTooltipBound")) return;
        control.SetMeta("HasTooltipBound", true);

        global::Godot.Timer? touchTimer = null;

        control.MouseEntered += () =>
        {
            var currentText = control.GetMeta("TooltipText").AsString();
            if (!string.IsNullOrEmpty(currentText))
                tooltipInstance.ShowTooltip(currentText, control.GetGlobalMousePosition());
        };

        control.MouseExited += () => tooltipInstance.HideTooltip();

        control.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventScreenTouch touch)
            {
                if (touch.Pressed)
                {
                    if (touchTimer == null)
                    {
                        touchTimer = new global::Godot.Timer { WaitTime = 0.5f, OneShot = true };
                        control.AddChild(touchTimer);
                        touchTimer.Timeout += () =>
                        {
                            var currentText = control.GetMeta("TooltipText").AsString();
                            if (!string.IsNullOrEmpty(currentText))
                                tooltipInstance.ShowTooltip(currentText, touch.Position);
                        };
                    }
                    touchTimer.Start();
                }
                else
                {
                    touchTimer?.Stop();
                    tooltipInstance.HideTooltip();
                }
            }
        };    }
}
