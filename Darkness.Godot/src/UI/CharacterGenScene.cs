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
	private OptionButton _headOption = null!;
	private OptionButton _hairStyleOption = null!;
	private OptionButton _hairColorOption = null!;
	private OptionButton _faceOption = null!;
	private OptionButton _eyesOption = null!;
	private OptionButton _legsOption = null!;
	private OptionButton _feetOption = null!;
	private OptionButton _armsOption = null!;
	private OptionButton _armorOption = null!;
	private OptionButton _weaponOption = null!;
	private OptionButton _shieldOption = null!;

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
		_headOption = GetNode<OptionButton>(container + "HeadOption");
		_hairStyleOption = GetNode<OptionButton>(container + "HairStyleOption");
		_hairColorOption = GetNode<OptionButton>(container + "HairColorOption");
		_faceOption = GetNode<OptionButton>(container + "FaceOption");
		_eyesOption = GetNode<OptionButton>(container + "EyesOption");
		_legsOption = GetNode<OptionButton>(container + "LegsOption");
		_feetOption = GetNode<OptionButton>(container + "FeetOption");
		_armsOption = GetNode<OptionButton>(container + "ArmsOption");
		_armorOption = GetNode<OptionButton>(container + "ArmorOption");
		_weaponOption = GetNode<OptionButton>(container + "WeaponOption");
		_shieldOption = GetNode<OptionButton>(container + "ShieldOption");

		SetupOptions();

		_classOption.ItemSelected += (_) => OnClassChanged();
		_skinOption.ItemSelected += (_) => UpdatePreview();
		_headOption.ItemSelected += (_) => UpdatePreview();
		_hairStyleOption.ItemSelected += (_) => UpdatePreview();
		_hairColorOption.ItemSelected += (_) => UpdatePreview();
		_faceOption.ItemSelected += (_) => UpdatePreview();
		_eyesOption.ItemSelected += (_) => UpdatePreview();
		_legsOption.ItemSelected += (_) => UpdatePreview();
		_feetOption.ItemSelected += (_) => UpdatePreview();
		_armsOption.ItemSelected += (_) => UpdatePreview();
		_armorOption.ItemSelected += (_) => UpdatePreview();
		_weaponOption.ItemSelected += (_) => UpdatePreview();
		_shieldOption.ItemSelected += (_) => UpdatePreview();

		GetNode<Button>(container + "CreateButton").Pressed += OnCreatePressed;
		GetNode<Button>(container + "BackButton").Pressed += () => _navigation.GoBackAsync();

		UpdatePreview();
	}

	private void SetupOptions()
	{
		Populate(_classOption, new[] { "Knight", "Rogue", "Mage", "Warrior", "Cleric" });
		Populate(_skinOption, _catalog.SkinColors);
		Populate(_headOption, _catalog.HeadTypes);
		Populate(_hairStyleOption, _catalog.HairStyles);
		Populate(_hairColorOption, _catalog.HairColors);
		Populate(_faceOption, _catalog.FaceTypes);
		Populate(_eyesOption, _catalog.EyeTypes);
		Populate(_legsOption, _catalog.LegsTypes);
		Populate(_feetOption, _catalog.FeetTypes);
		Populate(_armsOption, _catalog.ArmsTypes);
		Populate(_armorOption, _catalog.ArmorTypes);
		Populate(_weaponOption, _catalog.WeaponTypes);
		Populate(_shieldOption, _catalog.ShieldTypes);
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
		SelectByText(_shieldOption, defaults.ShieldType);
		SelectByText(_legsOption, defaults.Legs);
		SelectByText(_feetOption, defaults.Feet);
		SelectByText(_armsOption, defaults.Arms);
		SelectByText(_headOption, defaults.Head);
		SelectByText(_faceOption, defaults.Face);
		
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
		var appearance = GetCurrentAppearance();

		try
		{
			var stitchLayers = _catalog.GetStitchLayers(appearance);
			var previewFrameBytes = await _compositor.CompositePreviewFrame(stitchLayers, _fileSystem);

			if (previewFrameBytes != null && previewFrameBytes.Length > 0)
			{
				// Scale it by 4 for the UI portrait.
				_previewBytes = _compositor.ExtractFrame(previewFrameBytes, 0, 0, 64, 64, 4);

				var img = new Image();
				img.LoadPngFromBuffer(_previewBytes);
				_spritePreview.Texture = ImageTexture.CreateFromImage(img);
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[CharacterGen] Failed to update preview: {ex.Message}");
		}
	}

	private CharacterAppearance GetCurrentAppearance()
	{
		return new CharacterAppearance
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
			ShieldType = _shieldOption.GetItemText(_shieldOption.Selected),
			Head = _headOption.GetItemText(_headOption.Selected)
		};
	}

	private async void OnCreatePressed()
	{
		if (string.IsNullOrWhiteSpace(_nameEdit.Text) || _session.CurrentUser == null) return;

		var appearance = GetCurrentAppearance();
		
		var character = new Character
		{
			UserId = _session.CurrentUser.Id,
			Name = _nameEdit.Text,
			Class = _classOption.GetItemText(_classOption.Selected),
			SkinColor = appearance.SkinColor,
			Head = appearance.Head,
			Face = appearance.Face,
			Eyes = appearance.Eyes,
			HairStyle = appearance.HairStyle,
			HairColor = appearance.HairColor,
			Legs = appearance.Legs,
			Feet = appearance.Feet,
			Arms = appearance.Arms,
			ArmorType = appearance.ArmorType,
			WeaponType = appearance.WeaponType,
			ShieldType = appearance.ShieldType,
			Thumbnail = _previewBytes,
			Level = 1
		};

		// Generate Full Sprite Sheet
		try
		{
			var stitchLayers = _catalog.GetStitchLayers(appearance);
			GD.Print($"[CharacterGen] Stitching {stitchLayers.Count} layers for {character.Name}...");
			character.FullSpriteSheet = await _compositor.CompositeFullSheet(stitchLayers, _fileSystem);
			GD.Print($"[CharacterGen] Full sheet generated: {character.FullSpriteSheet?.Length ?? 0} bytes");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[CharacterGen] Failed to generate full sheet: {ex.Message}");
		}

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
			case "Knight": 
                c.Strength = 12; c.Dexterity = 10; c.Constitution = 14; 
                c.Intelligence = 8; c.Wisdom = 10; c.Charisma = 12;
                c.ArmorClass = 2; break;
			case "Warrior": 
                c.Strength = 15; c.Dexterity = 12; c.Constitution = 14;
                c.Intelligence = 8; c.Wisdom = 8; c.Charisma = 10;
                c.ArmorClass = 2; break;
			case "Mage": 
                c.Strength = 8; c.Dexterity = 10; c.Constitution = 10;
                c.Intelligence = 15; c.Wisdom = 14; c.Charisma = 12;
                c.ArmorClass = 0; break;
			case "Rogue": 
                c.Strength = 12; c.Dexterity = 15; c.Constitution = 10;
                c.Intelligence = 10; c.Wisdom = 8; c.Charisma = 12;
                c.ArmorClass = 1; break;
			case "Cleric": 
                c.Strength = 10; c.Dexterity = 8; c.Constitution = 14;
                c.Intelligence = 10; c.Wisdom = 15; c.Charisma = 12;
                c.ArmorClass = 2; break;
		}

        c.MaxHP = c.Constitution * 10;
		c.CurrentHP = c.MaxHP;
        c.Mana = c.Wisdom * 5;
        c.Stamina = c.Constitution * 5;
        c.Speed = c.Dexterity;
        c.Accuracy = 80 + c.Dexterity / 2;
        c.Evasion = c.Dexterity / 2;
        c.Defense = c.Constitution / 2;
        c.MagicDefense = c.Wisdom / 2;
	}
}
