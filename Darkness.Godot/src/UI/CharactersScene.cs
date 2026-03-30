using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Darkness.Godot.UI;

public partial class CharactersScene : Control
{
    private INavigationService _navigation;
    private ICharacterService _characterService;
    private ISessionService _session;
    private VBoxContainer _characterList;

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services!.GetRequiredService<INavigationService>();
        _characterService = global.Services!.GetRequiredService<ICharacterService>();
        _session = global.Services!.GetRequiredService<ISessionService>();

        _characterList = GetNode<VBoxContainer>("VBoxContainer/CharacterList");
        GetNode<Button>("VBoxContainer/NewButton").Pressed += OnNewPressed;
        GetNode<Button>("VBoxContainer/BackButton").Pressed += OnBackPressed;

        LoadCharacters();
    }

    private async void LoadCharacters()
    {
        if (_session.CurrentUser == null) return;

        var characters = await _characterService.GetCharactersForUserAsync(_session.CurrentUser.Id);
        foreach (var character in characters)
        {
            var btn = new Button
            {
                Text = $"{character.Name} (Level {character.Level})",
                CustomMinimumSize = new Vector2(0, 50)
            };
            btn.Pressed += () => OnCharacterSelected(character);
            _characterList.AddChild(btn);
        }
    }

    private void OnCharacterSelected(Character character)
    {
        _session.CurrentCharacter = character;
        _navigation.NavigateToAsync("WorldPage");
    }


    private async void OnNewPressed()
    {
        await _navigation.NavigateToAsync("CharacterGenPage");
    }

    private async void OnBackPressed()
    {
        await _navigation.NavigateToAsync("LoadUserPage");
    }
}
