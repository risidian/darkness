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
        GD.Print("[CharactersScene] _Ready started.");
        try
        {
            var global = GetNode<Global>("/root/Global");
            _navigation = global.Services!.GetRequiredService<INavigationService>();
            _characterService = global.Services!.GetRequiredService<ICharacterService>();
            _session = global.Services!.GetRequiredService<ISessionService>();

            _characterList = GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/CharacterList");
            GetNode<Button>("VBoxContainer/NewButton").Pressed += OnNewPressed;
            GetNode<Button>("VBoxContainer/BackButton").Pressed += OnBackPressed;

            LoadCharacters();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CharactersScene] Error in _Ready: {ex.Message}");
        }
    }

    private async void LoadCharacters()
    {
        GD.Print("[CharactersScene] LoadCharacters started.");
        if (_session.CurrentUser == null)
        {
            GD.Print("[CharactersScene] CurrentUser is null.");
            return;
        }

        try
        {
            var characters = await _characterService.GetCharactersForUserAsync(_session.CurrentUser.Id);
            GD.Print($"[CharactersScene] Loaded {characters?.Count ?? 0} characters.");

            if (characters != null)
            {
                foreach (var character in characters)
                {
                    var hbox = new HBoxContainer();

                    var tex = ImageUtils.ByteArrayToTexture(character.Thumbnail);
                    if (tex != null)
                    {
                        var rect = new TextureRect
                        {
                            Texture = tex,
                            CustomMinimumSize = new Vector2(50, 50),
                            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
                        };
                        hbox.AddChild(rect);
                    }

                    var btn = new Button
                    {
                        Text = $"{character.Name} (Level {character.Level})",
                        CustomMinimumSize = new Vector2(0, 50),
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    btn.Pressed += () => OnCharacterSelected(character);
                    hbox.AddChild(btn);

                    _characterList.AddChild(hbox);
                }
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CharactersScene] Error loading characters: {ex.Message}");
        }
    }


    private void OnCharacterSelected(Character character)
    {
        _session.CurrentCharacter = character;
        _navigation.NavigateToAsync("MainMenuPage");
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
