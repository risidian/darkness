using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Darkness.Godot.UI;

public partial class MainScene : Control
{
	private INavigationService _navigation;
	private IRewardService _rewardService;
	private ISessionService _session;
	private ICharacterService _characterService;
	private IDialogService _dialogService;

	public override async void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_navigation = global.Services!.GetRequiredService<INavigationService>();
		_rewardService = global.Services!.GetRequiredService<IRewardService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_characterService = global.Services!.GetRequiredService<ICharacterService>();
		_dialogService = global.Services!.GetRequiredService<IDialogService>();

		GetNode<Button>("VBoxContainer/StartButton").Pressed += OnStartPressed;
		GetNode<Button>("VBoxContainer/SettingsButton").Pressed += OnSettingsPressed;

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

	private void OnStartPressed()
	{
		GD.Print("[MainScene] Start Game pressed.");
		if (_session.CurrentUser == null)
		{
			_navigation.NavigateToAsync("LoadUserPage");
		}
		else
		{
			_navigation.NavigateToAsync("WorldPage");
		}
	}

	private void OnSettingsPressed()
	{
		_navigation.NavigateToAsync("SettingsPage");
	}
}
