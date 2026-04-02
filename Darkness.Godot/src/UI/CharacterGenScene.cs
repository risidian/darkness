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
	private INavigationService _navigation = null!;
	private ISpriteCompositor _compositor = null!;
	private ISpriteLayerCatalog _catalog = null!;
	private ICharacterService _characterService = null!;
	private ISessionService _session = null!;
	private IFileSystemService _fileSystem = null!;

	private TextureRect _spritePreview = null!;
	private LineEdit _nameEdit = null!;
	private OptionButton _classOption = null!;
	private OptionButton _skinOption = null!;
	private OptionButton _hairStyleOption = null!;
	private OptionButton _hairColorOption = null!;
	private OptionButton _faceOption = null!;
	private OptionButton _eyesOption = null!;
	private OptionButton _legsOption = null!;
	private OptionButton _feetOption = null!;
	private OptionButton _armsOption = null!;
	private OptionButton _armorOption = null!;
	private OptionButton _weaponOption = null!;

	private byte[]? _previewBytes;

	public override void _Ready()
	{
		if (!IsInsideTree()) return;
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
		_legsOption = GetNode<OptionButton>(container + "LegsOption");
		_feetOption = GetNode<OptionButton>(container + "FeetOption");
		_armsOption = GetNode<OptionButton>(container + "ArmsOption");
		_armorOption = GetNode<OptionButton>(container + "ArmorOption");
		_weaponOption = GetNode<OptionButton>(container + "WeaponOption");

		SetupOptions();

		_classOption.ItemSelected += (_) => OnClassChanged();
		_skinOption.ItemSelected += (_) => UpdatePreview();
		_hairStyleOption.ItemSelected += (_) => UpdatePreview();
		_hairColorOption.ItemSelected += (_) => UpdatePreview();
		_faceOption.ItemSelected += (_) => UpdatePreview();
		_eyesOption.ItemSelected += (_) => UpdatePreview();
		_legsOption.ItemSelected += (_) => UpdatePreview();
		_feetOption.ItemSelected += (_) => UpdatePreview();
		_armsOption.ItemSelected += (_) => UpdatePreview();
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
		Populate(_legsOption, _catalog.LegsTypes);
		Populate(_feetOption, _catalog.FeetTypes);
		Populate(_armsOption, _catalog.ArmsTypes);
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
		SelectByText(_legsOption, defaults.Legs);
		SelectByText(_feetOption, defaults.Feet);
		SelectByText(_armsOption, defaults.Arms);
		
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
			Legs = _legsOption.GetItemText(_legsOption.Selected),
			Feet = _feetOption.GetItemText(_feetOption.Selected),
			Arms = _armsOption.GetItemText(_armsOption.Selected),
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
			Legs = _legsOption.GetItemText(_legsOption.Selected),
			Feet = _feetOption.GetItemText(_feetOption.Selected),
			Arms = _armsOption.GetItemText(_armsOption.Selected),
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
