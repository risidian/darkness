using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class DeathmatchScene : Control
{
	private IDeathmatchService _deathmatchService = null!;
	private INavigationService _navigation = null!;

	private ItemList _encounterList = null!;
	private Label _nameLabel = null!;
	private Label _reqLevelLabel = null!;
	private Button _startButton = null!;

	private List<DeathmatchEncounter> _encounters = new();
	private DeathmatchEncounter? _selectedEncounter;

	public override async void _Ready()
	{
		if (!IsInsideTree()) return;
		var global = GetNode<Global>("/root/Global");
		_deathmatchService = global.Services!.GetRequiredService<IDeathmatchService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();

		_encounterList = GetNode<ItemList>("HSplitContainer/EncounterList");
		_nameLabel = GetNode<Label>("HSplitContainer/DetailsArea/EncounterName");
		_reqLevelLabel = GetNode<Label>("HSplitContainer/DetailsArea/ReqLevel");
		_startButton = GetNode<Button>("HSplitContainer/DetailsArea/StartButton");

		_encounterList.ItemSelected += OnEncounterSelected;
		_startButton.Pressed += OnStartPressed;
		GetNode<Button>("HSplitContainer/DetailsArea/BackButton").Pressed += () => _navigation.GoBackAsync();

		await LoadEncounters();
	}

	private async Task LoadEncounters()
	{
		var encounters = await _deathmatchService.GetEncountersAsync();
		_encounters = new List<DeathmatchEncounter>(encounters);

		_encounterList.Clear();
		foreach (var encounter in _encounters)
		{
			_encounterList.AddItem(encounter.Name);
		}
	}

	private void OnEncounterSelected(long index)
	{
		_selectedEncounter = _encounters[(int)index];
		_nameLabel.Text = _selectedEncounter.Name;
		_reqLevelLabel.Text = $"Required Level: {_selectedEncounter.RequiredLevel}";
	}

	private async void OnStartPressed()
	{
		if (_selectedEncounter == null) return;

		await _navigation.NavigateToAsync("BattlePage", new Dictionary<string, object>
		{
			{ "Encounter", _selectedEncounter }
		});
	}
}
