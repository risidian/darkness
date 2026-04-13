using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Godot.Game;

namespace Darkness.Godot.UI;

public partial class InventoryScene : Control
{
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;
    private ISpriteCompositor _compositor = null!;
    private ISpriteLayerCatalog _catalog = null!;
    private ICharacterService _characterService = null!;
    private IFileSystemService _fileSystem = null!;
    private VBoxContainer _itemList = null!;
    private VBoxContainer _equipmentList = null!;
    private LayeredSprite _charSprite = null!;
    private Label _goldLabel = null!;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        var sp = global.Services!;
        _session = sp.GetRequiredService<ISessionService>();
        _navigation = sp.GetRequiredService<INavigationService>();
        _compositor = sp.GetRequiredService<ISpriteCompositor>();
        _catalog = sp.GetRequiredService<ISpriteLayerCatalog>();
        _characterService = sp.GetRequiredService<ICharacterService>();
        _fileSystem = sp.GetRequiredService<IFileSystemService>();

        _itemList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/HSplitContainer/TabContainer/ITEMS/ItemList");
        _equipmentList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/HSplitContainer/TabContainer/EQUIPMENT/EquipmentList");
        _goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/Header/GoldLabel");
        GetNode<Button>("MarginContainer/VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

        var previewContainer = GetNode<Control>("MarginContainer/VBoxContainer/HSplitContainer/PreviewArea");
        var layeredSpriteScene = GD.Load<PackedScene>("res://scenes/LayeredSprite.tscn");
        _charSprite = layeredSpriteScene.Instantiate<LayeredSprite>();
        previewContainer.AddChild(_charSprite);
        _charSprite.Position = new Vector2(150, 200);
        _charSprite.Scale = new Vector2(4, 4);

        if (_session.CurrentCharacter != null)
        {
            _session.CurrentCharacter.ConsolidateInventory();
        }

        EnsureInventory();
        LoadInventory();
        UpdateCharacterPreview();
    }

    private async void UpdateCharacterPreview()
    {
        if (_session.CurrentCharacter != null)
        {
            await _charSprite.SetupCharacter(_session.CurrentCharacter, _catalog, _fileSystem, _compositor);
            _charSprite.Play("idle_down");
            _goldLabel.Text = $"GOLD: {_session.CurrentCharacter.Gold}";
        }
    }

    private void EnsureInventory()
    {
        if (_session.CurrentCharacter == null) return;
        if (_session.CurrentCharacter.Inventory.Count > 0) return;

        // Add starter equipment with requirements
        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Dagger (Steel)", Type = "Weapon", AttackBonus = 5 });
        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Recurve Bow", Type = "Weapon", AttackBonus = 6, RequiredDexterity = 12 });
        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Mage Wand", Type = "Weapon", AttackBonus = 4, RequiredIntelligence = 10 });
        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Greatsword", Type = "Weapon", AttackBonus = 12, RequiredStrength = 15 });

        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Health Potion", Type = "Consumable", Value = 50, Quantity = 5 });

        _session.CurrentCharacter.Inventory.Add(new Item
            { Name = "Mage Robes (Blue)", Type = "Armor", DefenseBonus = 2, ArmorClass = 0 });
        _session.CurrentCharacter.Inventory.Add(new Item
            { Name = "Leather", Type = "Armor", DefenseBonus = 5, ArmorClass = 1 });
        _session.CurrentCharacter.Inventory.Add(new Item
            { Name = "Plate (Steel)", Type = "Armor", DefenseBonus = 10, ArmorClass = 2 });

        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Crusader", Type = "Shield", DefenseBonus = 5 });
        _session.CurrentCharacter.Inventory.Add(new Item { Name = "Spartan", Type = "Shield", DefenseBonus = 7 });
    }

    private void LoadInventory()
    {
        foreach (Node child in _itemList.GetChildren()) child.QueueFree();
        foreach (Node child in _equipmentList.GetChildren()) child.QueueFree();

        if (_session.CurrentCharacter == null) return;

        foreach (var item in _session.CurrentCharacter.Inventory)
        {
            var hbox = new HBoxContainer();
            hbox.CustomMinimumSize = new Vector2(0, 80);
            hbox.AddThemeConstantOverride("separation", 20);

            bool isEquipped = false;
            bool isConsumable = item.Type == "Consumable";
            bool isEquipment = item.Type == "Weapon" || item.Type == "Armor" || item.Type == "Shield";

            if (item.Type == "Weapon") isEquipped = _session.CurrentCharacter.WeaponType == item.Name;
            else if (item.Type == "Armor") isEquipped = _session.CurrentCharacter.ArmorType == item.Name;
            else if (item.Type == "Shield") isEquipped = _session.CurrentCharacter.ShieldType == item.Name;

            var labelText = isEquipped ? $"{item.Name} (EQUIPPED)" : item.Name;
            if (item.Quantity > 1) labelText += $" (x{item.Quantity})";
            if (item.Type == "Armor") labelText += $" [AC: {item.ArmorClass}]";

            var label = new Label { Text = labelText, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            label.AddThemeFontSizeOverride("font_size", 24);
            hbox.AddChild(label);

            var equipBtn = new Button
                { Text = isConsumable ? "USE" : (isEquipped ? "UNEQUIP" : "EQUIP"), CustomMinimumSize = new Vector2(180, 0) };
            equipBtn.AddThemeFontSizeOverride("font_size", 20);

            // Check stat and proficiency requirements
            bool meetsStats = item.CanEquip(_session.CurrentCharacter, out var missing);
            bool meetsAC = !(item.Type == "Armor" && item.ArmorClass > _session.CurrentCharacter.ArmorClass);

            if (!isEquipped && (!meetsStats || !meetsAC))
            {
                equipBtn.Disabled = true;
                equipBtn.Text = "LOCKED";
                label.Modulate = new Color(0.5f, 0.5f, 0.5f);
                
                if (!meetsStats)
                {
                    label.TooltipText = $"Requires: {string.Join(", ", missing)}";
                }
                else if (!meetsAC)
                {
                    label.TooltipText = $"Requires Armor Proficiency Level {item.ArmorClass}";
                }
            }
            else
            {
                equipBtn.Pressed += () => OnEquipPressed(item);
            }

            hbox.AddChild(equipBtn);

            if (isConsumable)
            {
                var hotbarLabel = new Label { Text = "MAP:", VerticalAlignment = VerticalAlignment.Center };
                hotbarLabel.AddThemeFontSizeOverride("font_size", 18);
                hbox.AddChild(hotbarLabel);

                for (int i = 1; i <= 5; i++)
                {
                    int slot = i;
                    var hotbarBtn = new Button { Text = slot.ToString(), CustomMinimumSize = new Vector2(40, 40) };
                    hotbarBtn.AddThemeFontSizeOverride("font_size", 16);
                    
                    // Check if already mapped
                    if (_session.CurrentCharacter.Hotbar[slot - 1] == item.Name)
                    {
                        hotbarBtn.Modulate = new Color(0, 1, 0); // Green highlight
                    }
                    
                    hotbarBtn.Pressed += () => OnHotbarMapPressed(item, slot);
                    hbox.AddChild(hotbarBtn);
                }
            }

            if (isEquipment)
            {
                _equipmentList.AddChild(hbox);
            }
            else
            {
                _itemList.AddChild(hbox);
            }
        }
    }

    private async void OnHotbarMapPressed(Item item, int slot)
    {
        if (_session.CurrentCharacter == null) return;
        
        _session.CurrentCharacter.Hotbar[slot - 1] = item.Name;
        await _characterService.SaveCharacterAsync(_session.CurrentCharacter);
        LoadInventory();
    }

    private async void OnEquipPressed(Item item)
    {
        if (_session.CurrentCharacter == null) return;

        if (item.Type == "Consumable")
        {
            if (item.Name.Contains("Health") || item.Name.Contains("HP") || item.Name.Contains("Potion"))
            {
                _session.CurrentCharacter.CurrentHP += item.Value;
                if (_session.CurrentCharacter.CurrentHP > _session.CurrentCharacter.MaxHP)
                    _session.CurrentCharacter.CurrentHP = _session.CurrentCharacter.MaxHP;
            }
            else if (item.Name.Contains("Mana") || item.Name.Contains("MP"))
            {
                _session.CurrentCharacter.Mana += item.Value;
            }
            
            item.Quantity--;
            if (item.Quantity <= 0)
            {
                _session.CurrentCharacter.Inventory.Remove(item);
            }
        }
        else if (item.Type == "Weapon")
        {
            if (_session.CurrentCharacter.WeaponType == item.Name)
                _session.CurrentCharacter.WeaponType = "None";
            else
                _session.CurrentCharacter.WeaponType = item.Name;
        }
        else if (item.Type == "Armor")
        {
            if (_session.CurrentCharacter.ArmorType == item.Name)
                _session.CurrentCharacter.ArmorType = "None";
            else
                _session.CurrentCharacter.ArmorType = item.Name;
        }
        else if (item.Type == "Shield")
        {
            if (_session.CurrentCharacter.ShieldType == item.Name)
                _session.CurrentCharacter.ShieldType = "None";
            else
                _session.CurrentCharacter.ShieldType = item.Name;
        }

        await RegenerateFullSheet();
        await _characterService.SaveCharacterAsync(_session.CurrentCharacter);

        LoadInventory();
        UpdateCharacterPreview();
    }

    private async Task RegenerateFullSheet()
    {
        var c = _session.CurrentCharacter;
        if (c == null) return;

        var appearance = new CharacterAppearance
        {
            SkinColor = c.SkinColor,
            Face = c.Face,
            Eyes = c.Eyes,
            HairStyle = c.HairStyle,
            HairColor = c.HairColor,
            Legs = c.Legs,
            Feet = c.Feet,
            Arms = c.Arms,
            ArmorType = c.ArmorType,
            WeaponType = c.WeaponType,
            ShieldType = c.ShieldType,
            Head = c.Head
        };

        try
        {
            var stitchLayers = _catalog.GetStitchLayers(appearance);
            c.FullSpriteSheet = await _compositor.CompositeFullSheet(stitchLayers, _fileSystem);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Inventory] Failed to regenerate full sheet: {ex.Message}");
        }
    }
}