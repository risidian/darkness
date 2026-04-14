using Godot;

namespace Darkness.Godot.UI;

public partial class Tooltip : PanelContainer
{
    private Label _label = null!;

    public override void _Ready()
    {
        _label = GetNode<Label>("MarginContainer/Label");
        Hide();
    }

    public void ShowTooltip(string text, Vector2 position)
    {
        _label.Text = text;
        GlobalPosition = position + new Vector2(15, 15);
        Show();
    }

    public void HideTooltip()
    {
        Hide();
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            GlobalPosition = GetGlobalMousePosition() + new Vector2(15, 15);
        }
    }
}
