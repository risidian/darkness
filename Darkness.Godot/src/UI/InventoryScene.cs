using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

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

		_itemList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/ItemList");
		GetNode<Button>("MarginContainer/VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

		EnsureInventory();
		LoadInventory();
	}

	private void EnsureInventory()
	{
		if (_session.CurrentCharacter == null) return;
		if (_session.CurrentCharacter.Inventory.Count > 0) return;

		// Add starter equipment with ArmorClass restrictions
		// ArmorClass: 0=Cloth, 1=Leather, 2=Plate
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Dagger (Steel)", Type = "Weapon", AttackBonus = 5 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Recurve Bow", Type = "Weapon", AttackBonus = 6 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Mage Wand", Type = "Weapon", AttackBonus = 4 });
		
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Mage Robes (Blue)", Type = "Armor", DefenseBonus = 2, ArmorClass = 0 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Leather", Type = "Armor", DefenseBonus = 5, ArmorClass = 1 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Plate (Steel)", Type = "Armor", DefenseBonus = 10, ArmorClass = 2 });
		
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Crusader", Type = "Shield", DefenseBonus = 5 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Spartan", Type = "Shield", DefenseBonus = 7 });
	}

	private void LoadInventory()
	{
		foreach (Node child in _itemList.GetChildren()) child.QueueFree();

		if (_session.CurrentCharacter == null) return;

		foreach (var item in _session.CurrentCharacter.Inventory)
		{
			var hbox = new HBoxContainer();
			hbox.CustomMinimumSize = new Vector2(0, 80);
			hbox.AddThemeConstantOverride("separation", 20);

			bool isEquipped = false;
			if (item.Type == "Weapon") isEquipped = _session.CurrentCharacter.WeaponType == item.Name;
			else if (item.Type == "Armor") isEquipped = _session.CurrentCharacter.ArmorType == item.Name;
			else if (item.Type == "Shield") isEquipped = _session.CurrentCharacter.ShieldType == item.Name;

			var labelText = isEquipped ? $"{item.Name} (EQUIPPED)" : item.Name;
			if (item.Type == "Armor") labelText += $" [AC: {item.ArmorClass}]";

			var label = new Label { Text = labelText, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeFontSizeOverride("font_size", 24);
			hbox.AddChild(label);
			
			var equipBtn = new Button { Text = isEquipped ? "UNEQUIP" : "EQUIP", CustomMinimumSize = new Vector2(180, 0) };
			equipBtn.AddThemeFontSizeOverride("font_size", 20);
			
			// Disable if character doesn't have required ArmorClass proficiency
			if (!isEquipped && item.Type == "Armor" && item.ArmorClass > _session.CurrentCharacter.ArmorClass)
			{
				equipBtn.Disabled = true;
				equipBtn.Text = "LOCKED";
				label.Modulate = new Color(0.5f, 0.5f, 0.5f);
			}
			else
			{
				equipBtn.Pressed += () => OnEquipPressed(item);
			}
			
			hbox.AddChild(equipBtn);
			
			_itemList.AddChild(hbox);
		}
	}

	private async void OnEquipPressed(Item item)
	{
		if (_session.CurrentCharacter == null) return;

		if (item.Type == "Weapon")
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
			Head = "Human Male"
		};

		try
		{
			var basePaths = _catalog.GetLayerBasePaths(appearance);
			c.FullSpriteSheet = await _compositor.CompositeFullSheet(basePaths, _fileSystem);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Inventory] Failed to regenerate full sheet: {ex.Message}");
		}
	}
}
