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
	private ISessionService _session;
	private INavigationService _navigation;
	private VBoxContainer _itemList;

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_session = global.Services!.GetRequiredService<ISessionService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();

		_itemList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/ItemList");
		GetNode<Button>("MarginContainer/VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

		LoadInventory();
	}

	private void LoadInventory()
	{
		foreach (Node child in _itemList.GetChildren()) child.QueueFree();

		if (_session.CurrentCharacter == null) return;

		// Note: Using a mock list for now as Character.Items is a complex model
		// In a real scenario, we'd loop through _session.CurrentCharacter.Inventory
		
		var items = new List<string> { "Steel Sword", "Leather Armor", "Health Potion", "Mana Potion" };
		
		foreach (var item in items)
		{
			var hbox = new HBoxContainer();
			hbox.AddChild(new Label { Text = item, SizeFlagsHorizontal = SizeFlags.ExpandFill });
			
			var equipBtn = new Button { Text = "Use/Equip" };
			hbox.AddChild(equipBtn);
			
			_itemList.AddChild(hbox);
		}
	}
}
