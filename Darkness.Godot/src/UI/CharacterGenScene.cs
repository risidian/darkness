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
    private Character _character = new();

    public override void _Ready()
    {
        if (!IsInsideTree()) 
            return;
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

        _classOption.ItemSelected += (idx) => OnClassChanged((int)idx);
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

        OnClassChanged(0);
    }

    private void SetupOptions()
    {
        Populate(_classOption, new[] { "Knight", "Rogue", "Mage", "Warrior", "Cleric" });
        Populate(_skinOption, _catalog.GetOptionNames("Skin", "male"));
        Populate(_headOption, _catalog.GetOptionNames("Head", "male"));
        Populate(_hairStyleOption, _catalog.GetOptionNames("Hair", "male"));
        Populate(_hairColorOption, _catalog.GetOptionNames("HairColor", "male"));
        Populate(_faceOption, _catalog.GetOptionNames("Face", "male"));
        Populate(_eyesOption, _catalog.GetOptionNames("Eyes", "male"));
        Populate(_legsOption, _catalog.GetOptionNames("Legs", "male"));
        Populate(_feetOption, _catalog.GetOptionNames("Feet", "male"));
        Populate(_armsOption, _catalog.GetOptionNames("Arms", "male"));
        Populate(_armorOption, _catalog.GetOptionNames("Armor", "male"));
        Populate(_weaponOption, _catalog.GetOptionNames("Weapon", "male"));
        Populate(_shieldOption, _catalog.GetOptionNames("Shield", "male"));
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

    private void OnClassChanged(int index)
    {
        var className = _classOption.GetItemText(index);
        var defaults = _catalog.GetDefaultAppearanceForClass(className);

        _character.WeaponType = defaults.WeaponType;
        _character.ArmorType = defaults.ArmorType;
        _character.ShieldType = defaults.ShieldType;

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
        try
        {
            if (string.IsNullOrWhiteSpace(_nameEdit.Text))
            {
                GD.Print("[CharacterGen] Name is empty.");
                var global = GetNode<Global>("/root/Global");
                var dialog = global.Services!.GetRequiredService<IDialogService>();
                await dialog.DisplayAlertAsync("Validation", "Please enter a character name.", "OK");
                return;
            }

            if (_session.CurrentUser == null)
            {
                GD.PrintErr("[CharacterGen] No current user in session!");
                return;
            }

            var appearance = GetCurrentAppearance();

            _character.UserId = _session.CurrentUser.Id;
            _character.Name = _nameEdit.Text;
            _character.Class = _classOption.GetItemText(_classOption.Selected);
            _character.SkinColor = appearance.SkinColor;
            _character.Head = appearance.Head;
            _character.Face = appearance.Face;
            _character.Eyes = appearance.Eyes;
            _character.HairStyle = appearance.HairStyle;
            _character.HairColor = appearance.HairColor;
            _character.Legs = appearance.Legs;
            _character.Feet = appearance.Feet;
            _character.Arms = appearance.Arms;
            _character.ArmorType = appearance.ArmorType;
            _character.WeaponType = appearance.WeaponType;
            _character.ShieldType = appearance.ShieldType;
            _character.Thumbnail = _previewBytes;
            _character.Level = 1;

            // Starter Inventory: 5 Health Potions
            _character.Inventory = new List<Item>();
            for (int i = 0; i < 5; i++)
            {
                _character.Inventory.Add(new Item 
                { 
                    Name = "Health Potion", 
                    Type = "Consumable", 
                    Description = "Restores 50 HP.",
                    Value = 50 
                });
            }

            // Starter Gear in Inventory
            if (!string.IsNullOrEmpty(_character.WeaponType) && _character.WeaponType != "None")
                _character.Inventory.Add(new Item { Name = _character.WeaponType, Type = "Weapon", Value = 100 });
            if (!string.IsNullOrEmpty(_character.ArmorType) && _character.ArmorType != "None")
                _character.Inventory.Add(new Item { Name = _character.ArmorType, Type = "Armor", Value = 150 });
            if (!string.IsNullOrEmpty(_character.ShieldType) && _character.ShieldType != "None")
                _character.Inventory.Add(new Item { Name = _character.ShieldType, Type = "Shield", Value = 75 });

            // Generate Full Sprite Sheet
            try
            {
                var stitchLayers = _catalog.GetStitchLayers(appearance);
                GD.Print($"[CharacterGen] Stitching {stitchLayers.Count} layers for {_character.Name}...");
                _character.FullSpriteSheet = await _compositor.CompositeFullSheet(stitchLayers, _fileSystem);
                GD.Print($"[CharacterGen] Full sheet generated: {_character.FullSpriteSheet?.Length ?? 0} bytes");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[CharacterGen] Failed to generate full sheet: {ex.Message}");
            }

            SetStats(_character, _character.Class);

            await _characterService.SaveCharacterAsync(_character);
            GD.Print($"[CharacterGen] Character '{_character.Name}' saved.");

            // CRITICAL: Update session state so WorldScene knows who to render
            _session.CurrentCharacter = _character;

            await _navigation.NavigateToAsync("MainMenuPage");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[CharacterGen] EXCEPTION in OnCreatePressed: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }

    private void SetStats(Character c, string cls)
    {
        switch (cls)
        {
            case "Knight":
                c.Strength = 12;
                c.Dexterity = 10;
                c.Constitution = 14;
                c.Intelligence = 8;
                c.Wisdom = 10;
                c.Charisma = 12;
                c.ArmorClass = 5;
                break;
            case "Warrior":
                c.Strength = 15;
                c.Dexterity = 12;
                c.Constitution = 14;
                c.Intelligence = 8;
                c.Wisdom = 8;
                c.Charisma = 10;
                c.ArmorClass = 3;
                break;
            case "Mage":
                c.Strength = 8;
                c.Dexterity = 10;
                c.Constitution = 10;
                c.Intelligence = 15;
                c.Wisdom = 14;
                c.Charisma = 12;
                c.ArmorClass = 1;
                break;
            case "Rogue":
                c.Strength = 12;
                c.Dexterity = 15;
                c.Constitution = 10;
                c.Intelligence = 10;
                c.Wisdom = 8;
                c.Charisma = 12;
                c.ArmorClass = 2;
                break;
            case "Cleric":
                c.Strength = 10;
                c.Dexterity = 8;
                c.Constitution = 14;
                c.Intelligence = 10;
                c.Wisdom = 15;
                c.Charisma = 12;
                c.ArmorClass = 4;
                break;
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