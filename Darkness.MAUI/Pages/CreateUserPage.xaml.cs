using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.MAUI.Pages;

public partial class CreateUserPage : ContentPage
{
    private readonly IUserService _userService;

    public CreateUserPage(IUserService userService)
    {
        InitializeComponent();
        _userService = userService;
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || 
            string.IsNullOrWhiteSpace(PasswordEntry.Text) || 
            string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            await DisplayAlertAsync("Error", "All fields are required.", "OK");
            return;
        }

        var newUser = new User
        {
            Username = UsernameEntry.Text,
            Password = PasswordEntry.Text,
            EmailAddress = EmailEntry.Text,
            Age = 18 // Default age as in the original code
        };

        try
        {
            bool success = await _userService.CreateUserAsync(newUser);
            if (success)
            {
                await DisplayAlertAsync("Success", $"Created user: {newUser.Username}", "OK");
                await Shell.Current.GoToAsync("///LoadUserPage");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to create user.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create: {ex.Message}", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///LoadUserPage");
    }
}
