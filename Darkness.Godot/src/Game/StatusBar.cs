using Godot;
using System;

namespace Darkness.Godot.Game;

public enum StatusType { HP, Mana, Stamina, EXP }

public partial class StatusBar : VBoxContainer
{
    private Label _nameLabel = null!;
    private ProgressBar _progressBar = null!;
    private Label _valueLabel = null!;

    [Export] public StatusType Type = StatusType.HP;
    [Export] public Color CustomColor = Colors.White;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("NameLabel");
        _progressBar = GetNode<ProgressBar>("BarWrapper/ProgressBar");
        _valueLabel = GetNode<Label>("BarWrapper/ValueLabel");
        
        UpdateTheme();
    }

    private void UpdateTheme()
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(2);
        
        Color barColor = Type switch
        {
            StatusType.HP => new Color(0.7f, 0.1f, 0.1f), // Dark Red
            StatusType.Mana => new Color(0.1f, 0.1f, 0.7f), // Dark Blue
            StatusType.Stamina => new Color(0.1f, 0.6f, 0.1f), // Green
            StatusType.EXP => new Color(0.7f, 0.7f, 0.1f), // Yellow
            _ => CustomColor
        };

        style.BgColor = barColor;
        _progressBar.AddThemeStyleboxOverride("fill", style);
    }

    public void Setup(string unitName, int current, int max, StatusType type = StatusType.HP)
    {
        Type = type;
        if (_nameLabel == null) _Ready(); // Ensure nodes are ready if called early
        
        _nameLabel.Text = unitName;
        UpdateValue(current, max);
        UpdateTheme();
    }

    public void UpdateValue(int current, int max)
    {
        if (_progressBar == null) _Ready();
        
        _progressBar.MaxValue = max;
        _progressBar.Value = current;
        _valueLabel.Text = $"{current} / {max}";

        // Simple color pulse if low health
        if (Type == StatusType.HP && (float)current / max < 0.25f)
        {
            _nameLabel.SelfModulate = Colors.Red;
        }
        else
        {
            _nameLabel.SelfModulate = Colors.White;
        }
    }
}
