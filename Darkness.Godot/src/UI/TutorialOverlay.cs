using Godot;
using System;


namespace Darkness.Godot.UI;

public partial class TutorialOverlay : Control
{
    private Action? _onDismiss;

    public override void _Ready()
    {
        // Full rect
        SetAnchorsPreset(LayoutPreset.FullRect);
        
        // Semi-transparent backdrop
        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.75f)
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);
        
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);
        
        var panel = new PanelContainer();
        // apply some custom styling if necessary, otherwise rely on default theme
        center.AddChild(panel);
        
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 32);
        margin.AddThemeConstantOverride("margin_bottom", 32);
        panel.AddChild(margin);
        
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        margin.AddChild(vbox);
        
        var title = new Label
        {
            Text = "Welcome to Darkness: Reforged",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 32);
        title.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0)); // Gold-ish
        vbox.AddChild(title);
        
        var separator = new HSeparator();
        vbox.AddChild(separator);
        
        var guideText = @"🕹️ Movement: WASD / Arrow Keys or Tap/Click anywhere to move
⚔️ Combat: Turn-based D20 combat — use Hotbar Skills & Consumables
📜 Quests: Follow your Active Quest Tracker in top-left
🎒 Hub Menu: Access Forge (Crafting), Study (Stats & Talents), Allies, & Deathmatch";
        
        var label = new Label
        {
            Text = guideText,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(label);
        
        var separator2 = new HSeparator();
        vbox.AddChild(separator2);
        
        var btn = new Button
        {
            Text = "Begin Adventure"
        };
        btn.AddThemeFontSizeOverride("font_size", 24);
        btn.Pressed += () => 
        {
            _onDismiss?.Invoke();
            QueueFree();
        };
        vbox.AddChild(btn);
    }

    public void Setup(Action onDismiss)
    {
        _onDismiss = onDismiss;
    }
}
