using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class CharacterGenScene : Control
{
    private INavigationService _navigation;
    private ISpriteCompositor _compositor;
    private ISpriteLayerCatalog _catalog;
    private ICharacterService _characterService;
    private ISessionService _session;
    private TextureRect _spritePreview;
    private LineEdit _nameEdit;

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services.GetRequiredService<INavigationService>();
        _compositor = global.Services.GetRequiredService<ISpriteCompositor>();
        _catalog = global.Services.GetRequiredService<ISpriteLayerCatalog>();
        _characterService = global.Services.GetRequiredService<ICharacterService>();
        _session = global.Services.GetRequiredService<ISessionService>();

        _spritePreview = GetNode<TextureRect>("HSplitContainer/PreviewArea/SpritePreview");
        _nameEdit = GetNode<LineEdit>("HSplitContainer/ControlsArea/NameEdit");

        GetNode<Button>("HSplitContainer/ControlsArea/CreateButton").Pressed += OnCreatePressed;
        GetNode<Button>("HSplitContainer/ControlsArea/BackButton").Pressed += OnBackPressed;

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        // For PoC: generate a simple preview using SpriteCompositor
        // In reality, we'd use the selected appearance from the UI
        var appearance = new CharacterAppearance
        {
            SkinColor = "Light",
            HairStyle = "Short",
            HairColor = "Brown"
        };

        var layers = _catalog.GetLayersForAppearance(appearance);
        var streams = new List<System.IO.Stream>();
        foreach (var layer in layers)
        {
            // We need a way to open assets in Godot
            // For now, let's assume we can get streams
        }

        // Mock preview for now until asset loading is fully implemented
        var img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        img.Fill(Colors.Red);
        var tex = ImageTexture.CreateFromImage(img);
        _spritePreview.Texture = tex;
    }

    private async void OnCreatePressed()
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text) || _session.CurrentUser == null) return;

        var character = new Character
        {
            Name = _nameEdit.Text,
            UserId = _session.CurrentUser.Id,
            Level = 1,
            MaxHP = 100,
            CurrentHP = 100
        };

        await _characterService.SaveCharacterAsync(character);
        _navigation.NavigateToAsync("CharactersPage");
    }

    private void OnBackPressed()
    {
        _navigation.GoBackAsync();
    }
}
