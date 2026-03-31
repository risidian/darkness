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
	private IFileSystemService _fileSystem;

	private TextureRect _spritePreview;
	private LineEdit _nameEdit;
	private OptionButton _classOption;
	private OptionButton _skinOption;
	private OptionButton _hairStyleOption;
	private OptionButton _hairColorOption;
	private OptionButton _faceOption;
	private OptionButton _eyesOption;
	private OptionButton _armorOption;
	private OptionButton _weaponOption;

	private byte[]? _previewBytes;

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		var sp = global.Services!;
		_navigation = sp.GetRequiredService<INavigationService>();
		_compositor = sp.GetRequiredService<ISpriteCompositor>();
		_catalog = sp.GetRequiredService<ISpriteLayerCatalog>();
		_characterService = sp.GetRequiredService<ICharacterService>();
		_session = sp.GetRequiredService<ISessionService>();
		_fileSystem = sp.GetRequiredService<IFileSystemService>();

		var container = "HSplitContainer/ScrollContainer/MarginContainer/ControlsArea/";
		_spritePreview = GetNode<TextureRect>("HSplitContainer/PreviewArea/SpritePreview");
		_nameEdit = GetNode<LineEdit>(container + "NameEdit");
		_classOption = GetNode<OptionButton>(container + "ClassOption");
		_skinOption = GetNode<OptionButton>(container + "SkinOption");
		_hairStyleOption = GetNode<OptionButton>(container + "HairStyleOption");
		_hairColorOption = GetNode<OptionButton>(container + "HairColorOption");
		_faceOption = GetNode<OptionButton>(container + "FaceOption");
		_eyesOption = GetNode<OptionButton>(container + "EyesOption");
		_armorOption = GetNode<OptionButton>(container + "ArmorOption");
		_weaponOption = GetNode<OptionButton>(container + "WeaponOption");

		SetupOptions();

		_classOption.ItemSelected += (_) => OnClassChanged();
		_skinOption.ItemSelected += (_) => UpdatePreview();
		_hairStyleOption.ItemSelected += (_) => UpdatePreview();
		_hairColorOption.ItemSelected += (_) => UpdatePreview();
		_faceOption.ItemSelected += (_) => UpdatePreview();
		_eyesOption.ItemSelected += (_) => UpdatePreview();
		_armorOption.ItemSelected += (_) => UpdatePreview();
		_weaponOption.ItemSelected += (_) => UpdatePreview();

		GetNode<Button>(container + "CreateButton").Pressed += OnCreatePressed;
		GetNode<Button>(container + "BackButton").Pressed += () => _navigation.GoBackAsync();

		UpdatePreview();
	}

	private void SetupOptions()
	{
		Populate(_classOption, new[] { "Knight", "Rogue", "Mage", "Warrior", "Cleric" });
		Populate(_skinOption, _catalog.SkinColors);
		Populate(_hairStyleOption, _catalog.HairStyles);
		Populate(_hairColorOption, _catalog.HairColors);
		Populate(_faceOption, _catalog.FaceTypes);
		Populate(_eyesOption, _catalog.EyeTypes);
		Populate(_armorOption, _catalog.ArmorTypes);
		Populate(_weaponOption, _catalog.WeaponTypes);
	}

	private void Populate(OptionButton node, List<string> items)
	{
		node.Clear();
		foreach (var item in items) node.AddItem(item);
	}

	private void Populate(OptionButton node, string[] items)
	{
		node.Clear();
		foreach (var item in items) node.AddItem(item);
	}

	private void OnClassChanged()
	{
		var className = _classOption.GetItemText(_classOption.Selected);
		var defaults = _catalog.GetDefaultAppearanceForClass(className);
		
		SelectByText(_armorOption, defaults.ArmorType);
		SelectByText(_weaponOption, defaults.WeaponType);
		
		UpdatePreview();
	}

	private void SelectByText(OptionButton node, string text)
	{
		for (int i = 0; i < node.ItemCount; i++)
		{
			if (node.GetItemText(i) == text)
			{
				node.Select(i);
				return;
			}
		}
	}

	private async void UpdatePreview()
	{
		var appearance = new CharacterAppearance
		{
			SkinColor = _skinOption.GetItemText(_skinOption.Selected),
			Face = _faceOption.GetItemText(_faceOption.Selected),
			Eyes = _eyesOption.GetItemText(_eyesOption.Selected),
			HairStyle = _hairStyleOption.GetItemText(_hairStyleOption.Selected),
			HairColor = _hairColorOption.GetItemText(_hairColorOption.Selected),
			ArmorType = _armorOption.GetItemText(_armorOption.Selected),
			WeaponType = _weaponOption.GetItemText(_weaponOption.Selected),
			Head = "Human Male"
		};

		try
		{
			var layers = _catalog.GetLayersForAppearance(appearance);
			var streams = new List<System.IO.Stream>();
			foreach (var layer in layers)
			{
				var stream = await _fileSystem.OpenAppPackageFileAsync(layer.ResourcePath);
				streams.Add(stream);
			}

			if (streams.Count > 0)
			{
				var sheetBytes = _compositor.CompositeLayers(streams, 576, 256);
				_previewBytes = _compositor.ExtractFrame(sheetBytes, 0, 2 * 64, 64, 64, 4);

				var img = new Image();
				img.LoadPngFromBuffer(_previewBytes);
				_spritePreview.Texture = ImageTexture.CreateFromImage(img);
			}
			
			foreach (var s in streams) s.Dispose();
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[CharacterGen] Failed to update preview: {ex.Message}");
		}
	}

	private async void OnCreatePressed()
	{
		if (string.IsNullOrWhiteSpace(_nameEdit.Text) || _session.CurrentUser == null) return;

		var character = new Character
		{
			UserId = _session.CurrentUser.Id,
			Name = _nameEdit.Text,
			Class = _classOption.GetItemText(_classOption.Selected),
			SkinColor = _skinOption.GetItemText(_skinOption.Selected),
			Face = _faceOption.GetItemText(_faceOption.Selected),
			Eyes = _eyesOption.GetItemText(_eyesOption.Selected),
			HairStyle = _hairStyleOption.GetItemText(_hairStyleOption.Selected),
			HairColor = _hairColorOption.GetItemText(_hairColorOption.Selected),
			ArmorType = _armorOption.GetItemText(_armorOption.Selected),
			WeaponType = _weaponOption.GetItemText(_weaponOption.Selected),
			Thumbnail = _previewBytes,
			Level = 1
		};

		SetStats(character, character.Class);

		await _characterService.SaveCharacterAsync(character);
		
		// CRITICAL: Update session state so WorldScene knows who to render
		_session.CurrentCharacter = character;
		
		await _navigation.NavigateToAsync("MainMenuPage");
	}

	private void SetStats(Character c, string cls)
	{
		switch (cls)
		{
			case "Knight": c.Strength = 12; c.Constitution = 14; break;
			case "Warrior": c.Strength = 15; c.Constitution = 14; break;
			case "Mage": c.Intelligence = 15; c.Wisdom = 14; break;
			case "Rogue": c.Dexterity = 15; c.Strength = 12; break;
			case "Cleric": c.Wisdom = 15; c.Constitution = 14; break;
		}
		c.MaxHP = c.Constitution * 10;
		c.CurrentHP = c.MaxHP;
	}
}
