using Darkness.Core.Interfaces;

namespace Darkness.MAUI.Pages;

public partial class LoadUserPage : ContentPage
{
    private readonly IUserService _userService;

    public LoadUserPage(IUserService userService)
    {
        InitializeComponent();
        _userService = userService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || 
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlertAsync("Error", "Username and password are required.", "OK");
            return;
        }

        try
        {
            var user = await _userService.GetUserAsync(UsernameEntry.Text, PasswordEntry.Text);
            if (user != null)
            {
                await DisplayAlertAsync("Success", $"Welcome back, {user.Username}!", "OK");
                await Shell.Current.GoToAsync("///MainPage");
            }
            else
            {
                await DisplayAlertAsync("Error", "Invalid username or password.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CreateUserPage");
    }
}
