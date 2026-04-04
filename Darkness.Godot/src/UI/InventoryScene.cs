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

		// Add starter equipment for testing Task 4
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Dagger (Steel)", Type = "Weapon", AttackBonus = 5 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Recurve Bow", Type = "Weapon", AttackBonus = 6 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Mage Wand", Type = "Weapon", AttackBonus = 4 });
		_session.CurrentCharacter.Inventory.Add(new Item { Name = "Plate (Steel)", Type = "Armor", DefenseBonus = 10 });
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

			var isEquipped = item.Type == "Weapon" ? _session.CurrentCharacter.WeaponType == item.Name : _session.CurrentCharacter.ArmorType == item.Name;
			var labelText = isEquipped ? $"{item.Name} (EQUIPPED)" : item.Name;

			var label = new Label { Text = labelText, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeFontSizeOverride("font_size", 24);
			hbox.AddChild(label);
			
			var equipBtn = new Button { Text = isEquipped ? "UNEQUIP" : "EQUIP", CustomMinimumSize = new Vector2(180, 0) };
			equipBtn.AddThemeFontSizeOverride("font_size", 20);
			equipBtn.Pressed += () => OnEquipPressed(item);
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
				c.FullSpriteSheet = _compositor.CompositeLayers(streams, 1536, 2112);
			}

			foreach (var s in streams) s.Dispose();
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Inventory] Failed to regenerate full sheet: {ex.Message}");
		}
	}
}
