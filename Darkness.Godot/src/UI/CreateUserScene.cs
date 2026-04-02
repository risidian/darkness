using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Darkness.Godot.UI;

public partial class CreateUserScene : Control
{
    private INavigationService _navigation;
    private IUserService _userService;
    private ISessionService _session;
    private LineEdit _usernameEdit;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        GD.Print("[CreateUserScene] _Ready started.");
        try
        {
            var global = GetNode<Global>("/root/Global");
            _navigation = global.Services!.GetRequiredService<INavigationService>();
            _userService = global.Services!.GetRequiredService<IUserService>();
            _session = global.Services!.GetRequiredService<ISessionService>();

            _usernameEdit = GetNode<LineEdit>("VBoxContainer/UsernameEdit");
            GetNode<Button>("VBoxContainer/CreateButton").Pressed += OnCreatePressed;
            GetNode<Button>("VBoxContainer/BackButton").Pressed += OnBackPressed;
            GD.Print("[CreateUserScene] UI wired up successfully.");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CreateUserScene] Error in _Ready: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }

    private async void OnCreatePressed()
    {
        GD.Print("[CreateUserScene] Create button pressed.");
        var global = GetNode<Global>("/root/Global");
        var dialog = global.Services!.GetRequiredService<IDialogService>();

        // DEBUG: Immediate confirmation that button was hit
        await dialog.DisplayAlertAsync("Debug", $"Create button hit! Username entered: '{_usernameEdit.Text}'", "OK");

        if (string.IsNullOrWhiteSpace(_usernameEdit.Text)) 
        {
            GD.Print("[CreateUserScene] Username is empty.");
            await dialog.DisplayAlertAsync("Validation Error", "Please enter a username.", "OK");
            return;
        }

        try
        {
            var user = new User { Username = _usernameEdit.Text };
            GD.Print($"[CreateUserScene] Creating user: '{user.Username}'");
            
            var success = await _userService.CreateUserAsync(user);
            GD.Print($"[CreateUserScene] Create success: {success}");
            
            if (success)
            {
                _session.CurrentUser = user;
                GD.Print("[CreateUserScene] Navigating to CharactersPage.");
                await _navigation.NavigateToAsync("CharactersPage");
            }
            else
            {
                GD.PrintErr("[CreateUserScene] CreateUserAsync returned false.");
                await dialog.DisplayAlertAsync("Create Failed", "Failed to create user. The username might already be in use.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CreateUserScene] EXCEPTION: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
            string stackTrace = ex.StackTrace ?? "No stack trace available";
            await dialog.DisplayAlertAsync("System Error", $"An error occurred during user creation: {ex.Message}\n\nStack Trace: {stackTrace.Substring(0, System.Math.Min(stackTrace.Length, 200))}...", "OK");
        }
    }

    private async void OnBackPressed()
    {
        GD.Print("[CreateUserScene] Back pressed.");
        if (_session.CurrentUser == null)
        {
            var global = GetNode<Global>("/root/Global");
            var dialog = global.Services!.GetRequiredService<IDialogService>();
            await dialog.DisplayAlertAsync("Login Required", "You must create a user to continue.", "OK");
            return;
        }
        await _navigation.GoBackAsync();
    }
}
