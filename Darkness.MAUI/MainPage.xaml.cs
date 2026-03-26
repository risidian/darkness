using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.MAUI;

public partial class MainPage : ContentPage
{
	int count = 0;
	private readonly IUserService _userService;
	private readonly IRewardService _rewardService;

	public MainPage(IUserService userService, IRewardService rewardService)
	{
		InitializeComponent();
		_userService = userService;
		_rewardService = rewardService;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CheckDailyReward();
	}

	private async Task CheckDailyReward()
	{
		try
		{
			// Get the first user as a placeholder for current session
			var users = await _userService.GetAllUsersAsync();
			if (users != null && users.Count > 0)
			{
				var currentUser = users[0];
				var reward = await _rewardService.CheckDailyRewardAsync(currentUser);

				if (reward != null)
				{
					DailyRewardSection.IsVisible = true;
					RewardLabel.Text = $"You received: {reward.Name} - {reward.Description}";
					
					await DisplayAlertAsync("Daily Bonus!", $"You received a {reward.Name}!", "Excellent");
				}
			}
		}
		catch (Exception ex)
		{
			// Silently fail or log in a real app
			System.Diagnostics.Debug.WriteLine($"Error checking daily reward: {ex.Message}");
		}
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("///LoadUserPage");
	}
}
