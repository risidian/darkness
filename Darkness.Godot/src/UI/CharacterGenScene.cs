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
    private OptionButton _classOption;
    private OptionButton _skinOption;
    private OptionButton _hairStyleOption;
    private OptionButton _hairColorOption;

    public override void _Ready()
    {
    	var global = GetNode<Global>("/root/Global");
    	_navigation = global.Services!.GetRequiredService<INavigationService>();
    	_compositor = global.Services!.GetRequiredService<ISpriteCompositor>();
    	_catalog = global.Services!.GetRequiredService<ISpriteLayerCatalog>();
    	_characterService = global.Services!.GetRequiredService<ICharacterService>();
    	_session = global.Services!.GetRequiredService<ISessionService>();

    	_spritePreview = GetNode<TextureRect>("HSplitContainer/PreviewArea/SpritePreview");
    	_nameEdit = GetNode<LineEdit>("HSplitContainer/ScrollContainer/ControlsArea/NameEdit");
    	_classOption = GetNode<OptionButton>("HSplitContainer/ScrollContainer/ControlsArea/ClassOption");
    	_skinOption = GetNode<OptionButton>("HSplitContainer/ScrollContainer/ControlsArea/SkinOption");
    	_hairStyleOption = GetNode<OptionButton>("HSplitContainer/ScrollContainer/ControlsArea/HairStyleOption");
    	_hairColorOption = GetNode<OptionButton>("HSplitContainer/ScrollContainer/ControlsArea/HairColorOption");

    	SetupOptions();

    	_classOption.ItemSelected += (_) => UpdatePreview();
    	_skinOption.ItemSelected += (_) => UpdatePreview();
    	_hairStyleOption.ItemSelected += (_) => UpdatePreview();
    	_hairColorOption.ItemSelected += (_) => UpdatePreview();

    	GetNode<Button>("HSplitContainer/ScrollContainer/ControlsArea/CreateButton").Pressed += OnCreatePressed;
    	GetNode<Button>("HSplitContainer/ScrollContainer/ControlsArea/BackButton").Pressed += OnBackPressed;

    	UpdatePreview();
    }

    private void SetupOptions()
    {
    	string[] skins = { "Light", "Tan", "Dark" };
    	foreach (var s in skins) _skinOption.AddItem(s);

    	string[] styles = { "Short", "Long", "Bald", "Mohawk" };
    	foreach (var s in styles) _hairStyleOption.AddItem(s);

    	string[] colors = { "Brown", "Black", "Blonde", "Red" };
    	foreach (var c in colors) _hairColorOption.AddItem(c);
    }

    private async void UpdatePreview()
    {
    	var appearance = new CharacterAppearance
    	{
    		SkinColor = _skinOption.GetItemText(_skinOption.Selected),
    		HairStyle = _hairStyleOption.GetItemText(_hairStyleOption.Selected),
    		HairColor = _hairColorOption.GetItemText(_hairColorOption.Selected)
    	};

    	try
    	{
    		var layers = _catalog.GetLayersForAppearance(appearance);
    		var streams = new List<System.IO.Stream>();
    		foreach (var layer in layers)
    		{
    			var stream = await ((Global)GetNode("/root/Global")).Services!.GetRequiredService<IFileSystemService>().OpenAppPackageFileAsync(layer.ResourcePath);
    			streams.Add(stream);
    		}
    		if (streams.Count > 0)
    		{
    			// CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight)
    			// Assuming standard 1024x1024 sheet for now
    			var bytes = _compositor.CompositeLayers(streams, 1024, 1024);

    			// Now we need to extract a frame from the composite sheet
    			// ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale)
    			// Let's assume frame (0,0) of 64x64 scaled by 1
    			var frameBytes = _compositor.ExtractFrame(bytes, 0, 0, 64, 64, 1);

    			var img = Image.CreateFromData(64, 64, false, Image.Format.Rgba8, frameBytes);
    			var tex = ImageTexture.CreateFromImage(img);
    			_spritePreview.Texture = tex;
    		}
    	}

    	catch (System.Exception ex)
    	{
    		GD.PrintErr($"Failed to update preview: {ex.Message}");
    		// Fallback to red box
    		var img = Image.Create(64, 64, false, Image.Format.Rgba8);
    		img.Fill(Colors.Red);
    		_spritePreview.Texture = ImageTexture.CreateFromImage(img);
    	}
    }

    private async void OnCreatePressed()
    {
    	if (string.IsNullOrWhiteSpace(_nameEdit.Text) || _session.CurrentUser == null) return;

    	var charClass = _classOption.GetItemText(_classOption.Selected);
    	var character = new Character
    	{
    		Name = _nameEdit.Text,
    		UserId = _session.CurrentUser.Id,
    		Class = charClass,
    		Level = 1,
    		MaxHP = charClass == "WARRIOR" ? 120 : 80,
    		CurrentHP = charClass == "WARRIOR" ? 120 : 80,
    		Strength = charClass == "WARRIOR" ? 15 : 10,
    		Intelligence = charClass == "MAGE" ? 15 : 10,
    		Dexterity = charClass == "ROGUE" ? 15 : 10,
    		SkinColor = _skinOption.GetItemText(_skinOption.Selected),
    		HairStyle = _hairStyleOption.GetItemText(_hairStyleOption.Selected),
    		HairColor = _hairColorOption.GetItemText(_hairColorOption.Selected)
    	};

    	await _characterService.SaveCharacterAsync(character);
    	await _navigation.NavigateToAsync("CharactersPage");
    }


    private void OnBackPressed()
    {
        _navigation.GoBackAsync();
    }
}
