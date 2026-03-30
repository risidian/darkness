using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Darkness.Godot.UI;

public partial class LoadUserScene : Control
{
    private INavigationService _navigation;
    private IUserService _userService;
    private ISessionService _session;
    private VBoxContainer _userList;

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services.GetRequiredService<INavigationService>();
        _userService = global.Services.GetRequiredService<IUserService>();
        _session = global.Services.GetRequiredService<ISessionService>();

        _userList = GetNode<VBoxContainer>("VBoxContainer/UserList");
        GetNode<Button>("VBoxContainer/CreateButton").Pressed += OnCreatePressed;
        GetNode<Button>("VBoxContainer/BackButton").Pressed += OnBackPressed;

        LoadUsers();
    }

    private async void LoadUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        foreach (var user in users)
        {
            var btn = new Button
            {
                Text = user.Username,
                CustomMinimumSize = new Vector2(0, 50)
            };
            btn.Pressed += () => OnUserSelected(user);
            _userList.AddChild(btn);
        }
    }

    private void OnUserSelected(User user)
    {
        _session.CurrentUser = user;
        _navigation.NavigateToAsync("CharactersPage");
    }

    private void OnCreatePressed()
    {
        _navigation.NavigateToAsync("CreateUserPage");
    }

    private void OnBackPressed()
    {
        _navigation.GoBackAsync();
    }
}
