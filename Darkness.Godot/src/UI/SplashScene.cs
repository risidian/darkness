using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class SplashScene : Control
{
	private INavigationService _navigation = null!;
	private ISessionService _session = null!;
	private IDialogService _dialog = null!;
	private bool _isInitialized;

	public override void _Ready()
	{
		if (!IsInsideTree()) return;
		var global = GetNode<Global>("/root/Global");
		_navigation = global.Services!.GetRequiredService<INavigationService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_dialog = global.Services!.GetRequiredService<IDialogService>();

		// Apply simple shader to Background
		var bg = GetNode<ColorRect>("ColorRect");
		if (bg != null)
		{
			var shader = GD.Load<Shader>("res://src/Shaders/simple_rect.gdshader");
			bg.Material = new ShaderMaterial { Shader = shader };
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isInitialized && (@event is InputEventKey || @event is InputEventMouseButton || @event is InputEventScreenTouch))
		{
			_isInitialized = true;
			StartGame();
		}
	}

	private async void StartGame()
	{
		GetNode<Label>("VBoxContainer/Subtitle").Hide();
		var loading = GetNode<Label>("VBoxContainer/LoadingLabel");
		loading.Show();

		try 
		{
			// Ensure DI and session are ready
			GD.Print("[Splash] Initializing Session/Database...");
			await _session.InitializeAsync();
			GD.Print("[Splash] Initialization Successful.");

			if (_session.CurrentUser == null)
			{
				await _navigation.NavigateToAsync("LoadUserPage");
			}
			else
			{
				await _navigation.NavigateToAsync("MainMenuPage");
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Splash] CRITICAL INITIALIZATION FAILURE: {ex.Message}");
			await _dialog.DisplayAlertAsync("System Error", "Failed to initialize game database. Please ensure the app has storage permissions.", "OK");
			_isInitialized = false;
			GetNode<Label>("VBoxContainer/Subtitle").Show();
			loading.Hide();
		}
	}
}
