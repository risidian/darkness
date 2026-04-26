using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class StudyScene : Control
{
    private ICharacterService _characterService = null!;
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;

    private Character? _character;
    private Label _charNameLabel = null!;
    private Label _pointsLabel = null!;
    private Button _saveButton = null!;
    private Button _backButton = null!;
    private ConfirmationDialog _unsavedDialog = null!;
    private Button _discardButton = null!;

    // Derived stat labels
    private Label _lblMaxHP = null!;
    private Label _lblMaxMana = null!;
    private Label _lblArmorClass = null!;
    private Label _lblEvasion = null!;
    private Label _lblAccuracy = null!;
    private Label _lblMeleeDamage = null!;
    private Label _lblMagicDamage = null!;

    // Initial state snapshot
    private int _initialPoints;
    private int _initialStr;
    private int _initialDex;
    private int _initialCon;
    private int _initialInt;
    private int _initialWis;
    private int _initialCha;

    private Dictionary<string, (Label Label, Button PlusBtn, Button MinusBtn)> _statControls = new();

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _characterService = global.Services!.GetRequiredService<ICharacterService>();
        _session = global.Services!.GetRequiredService<ISessionService>();
        _navigation = global.Services!.GetRequiredService<INavigationService>();

        _character = _session.CurrentCharacter;
        if (_character == null) return;

        SnapshotInitialState();

        _charNameLabel = GetNode<Label>("MainLayout/LeftPanel/VBoxContainer/CharName");
        _pointsLabel = GetNode<Label>("MainLayout/LeftPanel/VBoxContainer/Points");
        _saveButton = GetNode<Button>("MainLayout/LeftPanel/VBoxContainer/ActionButtons/SaveButton");
        _backButton = GetNode<Button>("MainLayout/LeftPanel/VBoxContainer/ActionButtons/BackButton");
        _unsavedDialog = GetNode<ConfirmationDialog>("UnsavedDialog");
        _discardButton = GetNode<Button>("UnsavedDialog/DiscardButton");

        _lblMaxHP = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MaxHP");
        _lblMaxMana = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MaxMana");
        _lblArmorClass = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/ArmorClass");
        _lblEvasion = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/Evasion");
        _lblAccuracy = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/Accuracy");
        _lblMeleeDamage = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MeleeDamage");
        _lblMagicDamage = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MagicDamage");

        SetupStatControl("Strength");
        SetupStatControl("Dexterity");
        SetupStatControl("Constitution");
        SetupStatControl("Intelligence");
        SetupStatControl("Wisdom");
        SetupStatControl("Charisma");

        _saveButton.Pressed += async () => await SaveChangesAsync();
        _backButton.Pressed += OnBackButtonPressed;
        
        _unsavedDialog.Confirmed += async () => {
            await SaveChangesAsync();
            await _navigation.GoBackAsync();
        };
        
        // Add custom Discard button to the dialog
        _unsavedDialog.AddButton("Discard", true, "discard_action");
        _unsavedDialog.CustomAction += async (actionName) => {
            if (actionName == "discard_action")
            {
                DiscardChanges();
                _unsavedDialog.Hide();
                await _navigation.GoBackAsync();
            }
        };

        UpdateUI();
    }

    private void SnapshotInitialState()
    {
        if (_character == null) return;
        _initialPoints = _character.AttributePoints;
        _initialStr = _character.BaseStrength;
        _initialDex = _character.BaseDexterity;
        _initialCon = _character.BaseConstitution;
        _initialInt = _character.BaseIntelligence;
        _initialWis = _character.BaseWisdom;
        _initialCha = _character.BaseCharisma;
    }

    private bool HasUnsavedChanges()
    {
        return _character != null && (_character.AttributePoints != _initialPoints ||
               _character.BaseStrength != _initialStr ||
               _character.BaseDexterity != _initialDex ||
               _character.BaseConstitution != _initialCon ||
               _character.BaseIntelligence != _initialInt ||
               _character.BaseWisdom != _initialWis ||
               _character.BaseCharisma != _initialCha);
    }

    private void SetupStatControl(string attr)
    {
        var label = GetNode<Label>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/Label");
        var plusBtn = GetNode<Button>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/PlusButton");
        var minusBtn = GetNode<Button>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/MinusButton");
        
        plusBtn.Pressed += () => ModifyAttribute(attr, 1);
        minusBtn.Pressed += () => ModifyAttribute(attr, -1);
        
        _statControls[attr] = (label, plusBtn, minusBtn);
    }

    private void UpdateUI()
    {
        if (_character == null) return;

        _charNameLabel.Text = _character.Name;
        _pointsLabel.Text = $"Attribute Points: {_character.AttributePoints}";

        UpdateStatRow("Strength", _character.BaseStrength, _initialStr);
        UpdateStatRow("Dexterity", _character.BaseDexterity, _initialDex);
        UpdateStatRow("Constitution", _character.BaseConstitution, _initialCon);
        UpdateStatRow("Intelligence", _character.BaseIntelligence, _initialInt);
        UpdateStatRow("Wisdom", _character.BaseWisdom, _initialWis);
        UpdateStatRow("Charisma", _character.BaseCharisma, _initialCha);

        UpdateDerivedStats();

        _saveButton.Disabled = !HasUnsavedChanges();
    }

    private void UpdateStatRow(string attr, int value, int initialValue)
    {
        var controls = _statControls[attr];
        controls.Label.Text = $"{attr}: {value}";
        controls.PlusBtn.Disabled = _character!.AttributePoints <= 0;
        controls.MinusBtn.Disabled = value <= initialValue;
    }

    private void UpdateDerivedStats()
    {
        if (_character == null) return;
        _lblMaxHP.Text = $"Max HP: {_character.MaxHP}";
        _lblMaxMana.Text = $"Max Mana: {_character.MaxMana}";
        _lblArmorClass.Text = $"Armor Class: {_character.ArmorClass}";
        _lblEvasion.Text = $"Evasion: {_character.Evasion}";
        _lblAccuracy.Text = $"Accuracy: {_character.Accuracy}";
        // Character does not natively expose raw BaseMelee/Magic damage without weapon,
        // but we can estimate or show raw attribute scaling if desired.
        // For now we'll just show the raw attribute values for damage scaling to keep it simple,
        // or call combat engine calculations if needed. 
        // We'll approximate purely based on attributes as done in standard D20.
        _lblMeleeDamage.Text = $"Melee Dmg Mod: +{(_character.Strength - 10) / 2}";
        _lblMagicDamage.Text = $"Magic Dmg Mod: +{(_character.Intelligence - 10) / 2}";
    }

    private void ModifyAttribute(string attribute, int delta)
    {
        if (_character == null) return;

        // If delta is positive, we must have points.
        if (delta > 0 && _character.AttributePoints <= 0) return;

        int currentValue = 0;
        int initialValue = 0;
        switch (attribute)
        {
            case "Strength": currentValue = _character.BaseStrength; initialValue = _initialStr; break;
            case "Dexterity": currentValue = _character.BaseDexterity; initialValue = _initialDex; break;
            case "Constitution": currentValue = _character.BaseConstitution; initialValue = _initialCon; break;
            case "Intelligence": currentValue = _character.BaseIntelligence; initialValue = _initialInt; break;
            case "Wisdom": currentValue = _character.BaseWisdom; initialValue = _initialWis; break;
            case "Charisma": currentValue = _character.BaseCharisma; initialValue = _initialCha; break;
        }

        // If delta is negative, we can't go below initial value.
        if (delta < 0 && currentValue <= initialValue) return;

        switch (attribute)
        {
            case "Strength": _character.BaseStrength += delta; break;
            case "Dexterity": _character.BaseDexterity += delta; break;
            case "Constitution": _character.BaseConstitution += delta; break;
            case "Intelligence": _character.BaseIntelligence += delta; break;
            case "Wisdom": _character.BaseWisdom += delta; break;
            case "Charisma": _character.BaseCharisma += delta; break;
        }

        _character.AttributePoints -= delta;
        _character.RecalculateDerivedStats();
        UpdateUI();
    }

    private async Task SaveChangesAsync()
    {
        if (_character == null || !HasUnsavedChanges()) return;
        
        await Task.Run(() => _characterService.SaveCharacter(_character));
        SnapshotInitialState();
        UpdateUI();
    }

    private void DiscardChanges()
    {
        if (_character == null) return;
        _character.BaseStrength = _initialStr;
        _character.BaseDexterity = _initialDex;
        _character.BaseConstitution = _initialCon;
        _character.BaseIntelligence = _initialInt;
        _character.BaseWisdom = _initialWis;
        _character.BaseCharisma = _initialCha;
        _character.AttributePoints = _initialPoints;
        _character.RecalculateDerivedStats();
    }

    private void OnBackButtonPressed()
    {
        if (HasUnsavedChanges())
        {
            _unsavedDialog.PopupCentered();
        }
        else
        {
            _navigation.GoBackAsync();
        }
    }
}