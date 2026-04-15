using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class CharacterGenScene : Control
{
    private INavigationService _navigation = null!;
    private ISpriteCompositor _compositor = null!;
    private ISheetDefinitionCatalog _catalog = null!;
    private ICharacterService _characterService = null!;
    private ITalentService _talentService = null!;
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
    private OptionButton _offHandOption = null!;

    private VBoxContainer _step1Container = null!;
    private VBoxContainer _step2Container = null!;
    private VBoxContainer _step3Container = null!;
    private Button _nextButton = null!;
    private Button _backButton = null!;

    private int _currentStep = 1;
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
        _catalog = sp.GetRequiredService<ISheetDefinitionCatalog>();
        _characterService = sp.GetRequiredService<ICharacterService>();
        _talentService = sp.GetRequiredService<ITalentService>();
        _session = sp.GetRequiredService<ISessionService>();
        _fileSystem = sp.GetRequiredService<IFileSystemService>();

        var container = "HSplitContainer/ScrollContainer/MarginContainer/ControlsArea/";
        var step1 = container + "Step1Container/";
        var step2 = container + "Step2Container/";
        var step3 = container + "Step3Container/";
        var hidden = container + "HiddenEquipmentContainer/";

        _spritePreview = GetNode<TextureRect>("HSplitContainer/PreviewArea/SpritePreview");
        
        _step1Container = GetNode<VBoxContainer>(container + "Step1Container");
        _step2Container = GetNode<VBoxContainer>(container + "Step2Container");
        _step3Container = GetNode<VBoxContainer>(container + "Step3Container");
        
        _nextButton = GetNode<Button>(container + "NextButton");
        _backButton = GetNode<Button>(container + "BackButton");

        _nameEdit = GetNode<LineEdit>(step1 + "NameEdit");
        
        _headOption = GetNode<OptionButton>(step2 + "HeadOption");

        _classOption = GetNode<OptionButton>(step3 + "ClassOption");
        _skinOption = GetNode<OptionButton>(step3 + "SkinOption");
        _faceOption = GetNode<OptionButton>(step3 + "FaceOption");
        _eyesOption = GetNode<OptionButton>(step3 + "EyesOption");
        _hairStyleOption = GetNode<OptionButton>(step3 + "HairStyleOption");
        _hairColorOption = GetNode<OptionButton>(step3 + "HairColorOption");

        _legsOption = GetNode<OptionButton>(hidden + "LegsOption");
        _feetOption = GetNode<OptionButton>(hidden + "FeetOption");
        _armsOption = GetNode<OptionButton>(hidden + "ArmsOption");
        _armorOption = GetNode<OptionButton>(hidden + "ArmorOption");
        _weaponOption = GetNode<OptionButton>(hidden + "WeaponOption");
        _shieldOption = GetNode<OptionButton>(hidden + "ShieldOption");
        _offHandOption = GetNode<OptionButton>(hidden + "OffHandOption");

        SetupOptions();

        _classOption.ItemSelected += (idx) => OnClassChanged((int)idx);
        _skinOption.ItemSelected += (_) => UpdatePreview();
        _headOption.ItemSelected += (_) => {
            UpdateGenderFiltering();
            UpdatePreview();
        };
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
        _offHandOption.ItemSelected += (_) => UpdatePreview();

        _nextButton.Pressed += OnNextPressed;
        _backButton.Pressed += OnBackPressed;

        SetStep(1);
        OnClassChanged(0);
    }

    private void SetStep(int step)
    {
        _currentStep = step;
        _step1Container.Visible = _currentStep == 1;
        _step2Container.Visible = _currentStep == 2;
        _step3Container.Visible = _currentStep == 3;

        _spritePreview.Visible = _currentStep > 1;

        _nextButton.Text = _currentStep == 3 ? "FINISH" : "NEXT";
        _backButton.Text = _currentStep == 1 ? "MENU" : "BACK";
    }

    private async void OnNextPressed()
    {
        if (_currentStep == 1)
        {
            if (string.IsNullOrWhiteSpace(_nameEdit.Text))
            {
                var global = GetNode<Global>("/root/Global");
                var dialog = global.Services!.GetRequiredService<IDialogService>();
                await dialog.DisplayAlertAsync("Validation", "Please enter a character name.", "OK");
                return;
            }
            SetStep(2);
        }
        else if (_currentStep == 2)
        {
            UpdateGenderFiltering();
            SetStep(3);
        }
        else if (_currentStep == 3)
        {
            OnCreatePressed();
        }
    }

    private void OnBackPressed()
    {
        if (_currentStep == 1)
        {
            _navigation.GoBackAsync();
        }
        else if (_currentStep == 2)
        {
            SetStep(1);
        }
        else if (_currentStep == 3)
        {
            SetStep(2);
        }
    }

    private void SetupOptions()
    {
        Populate(_classOption, new[] { "Knight", "Rogue", "Mage", "Warrior", "Cleric" });
        
        // Populate head option with both genders to allow switching via head selection
        var allHeads = _catalog.GetOptionNames("Head", "male")
            .Union(_catalog.GetOptionNames("Head", "female"))
            .ToList();
        Populate(_headOption, allHeads);

        UpdateGenderFiltering();
    }

    private void UpdateGenderFiltering()
    {
        // Correctly determine gender from currently selected head
        string head = (_headOption.Selected != -1) ? _headOption.GetItemText(_headOption.Selected) : "Human Male";
        string gender = head.ToLower().Contains("female") ? "female" : "male";

        // Preserve current selections if possible
        string currentSkin = _skinOption.Selected != -1 ? _skinOption.GetItemText(_skinOption.Selected) : "";
        string currentArmor = _armorOption.Selected != -1 ? _armorOption.GetItemText(_armorOption.Selected) : "";
        string currentWeapon = _weaponOption.Selected != -1 ? _weaponOption.GetItemText(_weaponOption.Selected) : "";
        string currentShield = _shieldOption.Selected != -1 ? _shieldOption.GetItemText(_shieldOption.Selected) : "";
        string currentOffHand = _offHandOption.Selected != -1 ? _offHandOption.GetItemText(_offHandOption.Selected) : "";
        string currentLegs = _legsOption.Selected != -1 ? _legsOption.GetItemText(_legsOption.Selected) : "";

        // Re-populate everything that is gender-dependent (except Head, which is the gender selector)
        Populate(_skinOption, _catalog.GetOptionNames("Skin", gender));
        Populate(_hairStyleOption, _catalog.GetOptionNames("Hair", gender));
        Populate(_hairColorOption, _catalog.GetOptionNames("HairColor", gender));
        Populate(_faceOption, _catalog.GetOptionNames("Face", gender));
        Populate(_eyesOption, _catalog.GetOptionNames("Eyes", gender));
        Populate(_legsOption, _catalog.GetOptionNames("Legs", gender));
        Populate(_feetOption, _catalog.GetOptionNames("Feet", gender));
        Populate(_armsOption, _catalog.GetOptionNames("Arms", gender));
        Populate(_armorOption, _catalog.GetOptionNames("Armor", gender));
        Populate(_weaponOption, _catalog.GetOptionNames("Weapon", gender));
        Populate(_shieldOption, _catalog.GetOptionNames("Shield", gender));
        Populate(_offHandOption, _catalog.GetOptionNames("Weapon", gender));

        // Restore selections
        if (!string.IsNullOrEmpty(currentSkin)) SelectByText(_skinOption, currentSkin);
        if (!string.IsNullOrEmpty(currentArmor)) SelectByText(_armorOption, currentArmor);
        if (!string.IsNullOrEmpty(currentWeapon)) SelectByText(_weaponOption, currentWeapon);
        if (!string.IsNullOrEmpty(currentShield)) SelectByText(_shieldOption, currentShield);
        if (!string.IsNullOrEmpty(currentOffHand)) SelectByText(_offHandOption, currentOffHand);
        if (!string.IsNullOrEmpty(currentLegs)) SelectByText(_legsOption, currentLegs);
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

        // Preserve current gender if we are already in Step 2 or 3
        string currentHead = (_headOption.Selected != -1) ? _headOption.GetItemText(_headOption.Selected) : "";
        
        if (string.IsNullOrEmpty(currentHead))
        {
            SelectByText(_headOption, defaults.Head);
        }
        else
        {
            // We have a selection, keep the user's gender but maybe use the class's default head type (if they differ)
            // Most classes use "Human Male" or "Human Female". 
            // We'll keep the user's gender but pick the class default's head if it's for the same gender.
            string currentGender = currentHead.ToLower().Contains("female") ? "female" : "male";
            string defaultGender = defaults.Head.ToLower().Contains("female") ? "female" : "male";
            
            if (currentGender != defaultGender)
            {
                // Find the appropriate head for the current gender
                string newHead = currentGender == "female" ? "Human Female" : "Human Male";
                SelectByText(_headOption, newHead);
            }
            else
            {
                SelectByText(_headOption, defaults.Head);
            }
        }

        UpdateGenderFiltering();

        _character.WeaponType = defaults.WeaponType;
        _character.ArmorType = defaults.ArmorType;
        _character.ShieldType = defaults.ShieldType;
        _character.OffHandType = defaults.OffHandType;

        SelectByText(_armorOption, defaults.ArmorType);
        SelectByText(_weaponOption, defaults.WeaponType);
        SelectByText(_shieldOption, defaults.ShieldType);
        SelectByText(_offHandOption, defaults.OffHandType ?? "None");
        SelectByText(_legsOption, defaults.Legs);
        SelectByText(_feetOption, defaults.Feet);
        SelectByText(_armsOption, defaults.Arms);
        SelectByText(_faceOption, defaults.Face);
        
        // Aesthetic defaults
        if (!string.IsNullOrEmpty(defaults.HairStyle)) SelectByText(_hairStyleOption, defaults.HairStyle);
        if (!string.IsNullOrEmpty(defaults.HairColor)) SelectByText(_hairColorOption, defaults.HairColor);
        if (!string.IsNullOrEmpty(defaults.SkinColor)) SelectByText(_skinOption, defaults.SkinColor);
        if (!string.IsNullOrEmpty(defaults.Eyes)) SelectByText(_eyesOption, defaults.Eyes);

        // Ensure visibility of relevant equipment slots
        bool isMage = className == "Mage";
        var offHandLabel = GetNodeOrNull<Label>(_offHandOption.GetParent().GetPath() + "/OffHandLabel");
        if (offHandLabel != null) offHandLabel.Visible = isMage;
        _offHandOption.Visible = isMage;

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
            var definitions = _catalog.GetSheetDefinitions(appearance);
            var previewFrameBytes = await _compositor.CompositePreviewFrame(definitions, appearance, _fileSystem);

            if (previewFrameBytes != null && previewFrameBytes.Length > 0)
            {
                _previewBytes = previewFrameBytes;

                var img = new Image();
                var error = img.LoadPngFromBuffer(_previewBytes);
                if (error == Error.Ok)
                {
                    _spritePreview.Texture = ImageTexture.CreateFromImage(img);
                }
                else
                {
                    GD.PrintErr($"[CharacterGen] Failed to load preview PNG: {error}");
                }
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
            SkinColor = GetSelectedText(_skinOption),
            Face = GetSelectedText(_faceOption),
            Eyes = GetSelectedText(_eyesOption),
            HairStyle = GetSelectedText(_hairStyleOption),
            HairColor = GetSelectedText(_hairColorOption),
            Legs = GetSelectedText(_legsOption),
            Feet = GetSelectedText(_feetOption),
            Arms = GetSelectedText(_armsOption),
            ArmorType = GetSelectedText(_armorOption),
            WeaponType = GetSelectedText(_weaponOption),
            ShieldType = GetSelectedText(_shieldOption),
            OffHandType = GetSelectedText(_offHandOption),
            Head = GetSelectedText(_headOption)
        };
    }

    private string GetSelectedText(OptionButton ob)
    {
        if (ob == null || ob.ItemCount == 0 || ob.Selected < 0 || ob.Selected >= ob.ItemCount) return "None";
        return ob.GetItemText(ob.Selected);
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
            _character.OffHandType = appearance.OffHandType;
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
            AddStarterGear(_character.WeaponType, "Weapon", 100);
            AddStarterGear(_character.ArmorType, "Armor", 150);
            AddStarterGear(_character.ShieldType, "Shield", 75);
            AddStarterGear(_character.OffHandType, "Weapon", 100);

            // Generate Full Sprite Sheet
            try
            {
                var definitions = _catalog.GetSheetDefinitions(appearance);
                GD.Print($"[CharacterGen] Compositing {definitions.Count} definitions for {_character.Name}...");
                _character.FullSpriteSheet = await _compositor.CompositeFullSheet(definitions, appearance, _fileSystem);
                GD.Print($"[CharacterGen] Full sheet generated: {_character.FullSpriteSheet?.Length ?? 0} bytes");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[CharacterGen] Failed to generate full sheet: {ex.Message}");
            }

            SetStats(_character, _character.Class);
            _talentService.UnlockStartingTalents(_character);

            await Task.Run(() => _characterService.SaveCharacter(_character));
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

    private void AddStarterGear(string? name, string type, int value)
    {
        if (string.IsNullOrEmpty(name) || name == "None") return;

        var item = new Item { Name = name, Type = type, Value = value };

        // Try to look up requirements from catalog
        var def = _catalog.GetSheetDefinitionByName(type, name);
        if (def != null)
        {
            // SheetDefinition doesn't have stats yet, but we could add them if needed.
            // For now, keeping it simple.
        }

        _character.Inventory.Add(item);
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
