using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Darkness.Godot.UI;

public partial class MainScene : Control
{
	private INavigationService _navigation;

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_navigation = global.Services.GetRequiredService<INavigationService>();

		GetNode<Button>("VBoxContainer/StartButton").Pressed += OnStartPressed;
		GetNode<Button>("VBoxContainer/SettingsButton").Pressed += OnSettingsPressed;
	}

	private void OnStartPressed()
	{
		GD.Print("[MainScene] Start Game pressed.");
		_navigation.NavigateToAsync("LoadUserPage");
	}

	private void OnSettingsPressed()
	{
		_navigation.NavigateToAsync("SettingsPage");
	}
}
