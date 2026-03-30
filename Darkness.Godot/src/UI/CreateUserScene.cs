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
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services!.GetRequiredService<INavigationService>();
        _userService = global.Services!.GetRequiredService<IUserService>();
        _session = global.Services!.GetRequiredService<ISessionService>();

        _usernameEdit = GetNode<LineEdit>("VBoxContainer/UsernameEdit");
        GetNode<Button>("VBoxContainer/CreateButton").Pressed += OnCreatePressed;
        GetNode<Button>("VBoxContainer/BackButton").Pressed += OnBackPressed;
    }

    private async void OnCreatePressed()
    {
        if (string.IsNullOrWhiteSpace(_usernameEdit.Text)) return;

        var user = new User { Username = _usernameEdit.Text };
        var success = await _userService.CreateUserAsync(user);
        
        if (success)
        {
            _session.CurrentUser = user;
            await _navigation.NavigateToAsync("CharactersPage");
        }
    }

    private void OnBackPressed()
    {
        _navigation.GoBackAsync();
    }
}
