using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class SplashScene : Control
{
	private INavigationService _navigation;
	private ISessionService _session;
	private bool _isInitialized;

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_navigation = global.Services!.GetRequiredService<INavigationService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isInitialized && (@event is InputEventKey || @event is InputEventMouseButton))
		{
			_isInitialized = true;
			StartGame();
		}
	}

	private async void StartGame()
	{
		GetNode<Label>("VBoxContainer/Subtitle").Hide();
		GetNode<Label>("VBoxContainer/LoadingLabel").Show();

		// Ensure DI and session are ready
		await _session.InitializeAsync();

		if (_session.CurrentUser == null)
		{
			await _navigation.NavigateToAsync("LoadUserPage");
		}
		else
		{
			await _navigation.NavigateToAsync("MainMenuPage");
		}
	}
}
