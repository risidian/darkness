using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Darkness.Godot.UI;

public partial class MainMenuScene : Control
{
	private INavigationService _navigation;
	private IRewardService _rewardService;
	private ISessionService _session;
	private ICharacterService _characterService;
	private IDialogService _dialogService;
	private ISettingsService _settingsService;

	public override async void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_navigation = global.Services!.GetRequiredService<INavigationService>();
		_rewardService = global.Services!.GetRequiredService<IRewardService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_characterService = global.Services!.GetRequiredService<ICharacterService>();
		_dialogService = global.Services!.GetRequiredService<IDialogService>();
		_settingsService = global.Services!.GetRequiredService<ISettingsService>();

		// Apply simple shader to Background
		var bg = GetNode<ColorRect>("ColorRect");
		if (bg != null)
		{
			var shader = GD.Load<Shader>("res://src/Shaders/simple_rect.gdshader");
			bg.Material = new ShaderMaterial { Shader = shader };
		}

		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/StoryButton").Pressed += () => _navigation.NavigateToAsync("WorldPage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/BattleButton").Pressed += () => _navigation.NavigateToAsync("DeathmatchPage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/PvpButton").Pressed += OnPvpPressed;
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/CharactersButton").Pressed += () => _navigation.NavigateToAsync("CharactersPage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/ForgeButton").Pressed += () => _navigation.NavigateToAsync("ForgePage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/StudyButton").Pressed += () => _navigation.NavigateToAsync("StudyPage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/AlliesButton").Pressed += () => _navigation.NavigateToAsync("AlliesPage");
		GetNode<Button>("MarginContainer/VBoxContainer/GridContainer/SettingsButton").Pressed += () => _navigation.NavigateToAsync("SettingsPage");
		GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/LogoutButton").Pressed += OnLogoutPressed;

		await RunStartupChecks();
	}

	private async Task RunStartupChecks()
	{
		if (_session.CurrentUser == null)
		{
			await _session.InitializeAsync();
		}

		if (_session.CurrentUser != null)
		{
			// Check for character
			var characters = await _characterService.GetCharactersForUserAsync(_session.CurrentUser.Id);
			if (characters == null || characters.Count == 0)
			{
				await _navigation.NavigateToAsync("CharacterGenPage");
				return;
			}

			if (_session.CurrentCharacter == null)
			{
				_session.CurrentCharacter = characters[0];
			}

			// Check daily reward
			await CheckDailyReward();
		}
	}

	private async Task CheckDailyReward()
	{
		if (_session.CurrentUser == null) return;

		var reward = await _rewardService.CheckDailyRewardAsync(_session.CurrentUser);
		if (reward != null)
		{
			await _dialogService.DisplayAlertAsync("Daily Bonus!", $"You received a {reward.Name}!", "Excellent");
		}
	}

	private async void OnPvpPressed()
	{
		if (_session.CurrentUser == null) return;
		var characters = await _characterService.GetCharactersForUserAsync(_session.CurrentUser.Id);
		if (characters == null || characters.Count < 2)
		{
			await _dialogService.DisplayAlertAsync("PVP", "You need at least two characters to play PVP.", "OK");
			return;
		}

		var parameters = new Dictionary<string, object>
		{
			{ "Mode", "PVP" },
			{ "Player1", characters[0] },
			{ "Player2", characters[1] }
		};

		await _navigation.NavigateToAsync("WorldPage", parameters);
	}

	private async void OnLogoutPressed()
	{
		_session.CurrentUser = null;
		_settingsService.LastUserId = 0;
		await _settingsService.SaveSettingsAsync();
		await _navigation.NavigateToAsync("LoadUserPage");
	}
}
