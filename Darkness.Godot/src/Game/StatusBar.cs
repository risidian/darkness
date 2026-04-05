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
        
        _nameLabel!.Text = unitName;
        UpdateValue(current, max);
        UpdateTheme();
    }

    public void UpdateValue(int current, int max)
    {
        if (_progressBar == null) _Ready();
        
        int safeMax = Math.Max(1, max);
        int displayCurrent = Math.Max(0, current);

        _progressBar!.MaxValue = safeMax;
        _progressBar.Value = displayCurrent;
        _valueLabel!.Text = $"{displayCurrent} / {safeMax}";

        // Dynamic coloring for HP
        if (Type == StatusType.HP)
        {
            float ratio = (float)displayCurrent / safeMax;
            Color statusColor = ratio switch
            {
                < 0.20f => Colors.Red,
                < 0.50f => Colors.Yellow,
                _ => Colors.White
            };

            _nameLabel.SelfModulate = statusColor;
            _valueLabel.SelfModulate = statusColor;
        }
        else
        {
            _nameLabel.SelfModulate = Colors.White;
            _valueLabel.SelfModulate = Colors.White;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlighted)
        {
            _nameLabel.AddThemeColorOverride("font_outline_color", Colors.RoyalBlue);
            _nameLabel.AddThemeConstantOverride("outline_size", 8);
        }
        else
        {
            _nameLabel.RemoveThemeColorOverride("font_outline_color");
            _nameLabel.RemoveThemeConstantOverride("outline_size");
        }
    }
}
