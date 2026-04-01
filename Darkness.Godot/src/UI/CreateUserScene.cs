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
        GD.Print("[CreateUserScene] Create pressed.");
        if (string.IsNullOrWhiteSpace(_usernameEdit.Text)) 
        {
            GD.Print("[CreateUserScene] Username is empty.");
            return;
        }

        try
        {
            var user = new User { Username = _usernameEdit.Text };
            GD.Print($"[CreateUserScene] Creating user: {user.Username}");
            
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
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CreateUserScene] EXCEPTION: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
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
