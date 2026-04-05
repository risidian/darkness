using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Darkness.Godot.UI;

public partial class PauseMenu : CanvasLayer
{
    private INavigationService _navigation = null!;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services!.GetRequiredService<INavigationService>();

        GetNode<Button>("VBoxContainer/ResumeButton").Pressed += OnResumePressed;
        GetNode<Button>("VBoxContainer/InventoryButton").Pressed += OnInventoryPressed;
        GetNode<Button>("VBoxContainer/SettingsButton").Pressed += OnSettingsPressed;
        GetNode<Button>("VBoxContainer/MenuButton").Pressed += OnMenuPressed;

        Hide();
    }

    public void Toggle()
    {
        Visible = !Visible;
        GetTree().Paused = Visible;
    }

    private void OnResumePressed()
    {
        Toggle();
    }

    private void OnInventoryPressed()
    {
        GetTree().Paused = false;
        _navigation.NavigateToAsync("InventoryPage");
    }

    private void OnSettingsPressed()
    {
        GetTree().Paused = false;
        _navigation.NavigateToAsync("SettingsPage");
    }

    private void OnMenuPressed()
    {
        GetTree().Paused = false;
        _navigation.NavigateToAsync("MainMenuPage");
    }
}