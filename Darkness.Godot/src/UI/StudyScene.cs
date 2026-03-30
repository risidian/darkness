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
	private ICharacterService _characterService;
	private ISessionService _session;
	private INavigationService _navigation;

	private Character? _character;
	private Label _charNameLabel;
	private Label _pointsLabel;

	private Dictionary<string, (Label Label, Button Button)> _statControls = new();

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_characterService = global.Services!.GetRequiredService<ICharacterService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();

		_charNameLabel = GetNode<Label>("VBoxContainer/CharName");
		_pointsLabel = GetNode<Label>("VBoxContainer/Points");

		SetupStatControl("Strength");
		SetupStatControl("Dexterity");
		SetupStatControl("Constitution");
		SetupStatControl("Intelligence");
		SetupStatControl("Wisdom");
		SetupStatControl("Charisma");

		GetNode<Button>("VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

		_character = _session.CurrentCharacter;
		UpdateUI();
	}

	private void SetupStatControl(string attr)
	{
		var label = GetNode<Label>($"VBoxContainer/StatsGrid/{attr}/Label");
		var button = GetNode<Button>($"VBoxContainer/StatsGrid/{attr}/PlusButton");
		button.Pressed += () => UpgradeAttribute(attr);
		_statControls[attr] = (label, button);
	}

	private void UpdateUI()
	{
		if (_character == null) return;

		_charNameLabel.Text = _character.Name;
		_pointsLabel.Text = $"Attribute Points: {_character.AttributePoints}";

		UpdateStatRow("Strength", _character.Strength);
		UpdateStatRow("Dexterity", _character.Dexterity);
		UpdateStatRow("Constitution", _character.Constitution);
		UpdateStatRow("Intelligence", _character.Intelligence);
		UpdateStatRow("Wisdom", _character.Wisdom);
		UpdateStatRow("Charisma", _character.Charisma);
	}

	private void UpdateStatRow(string attr, int value)
	{
		var controls = _statControls[attr];
		controls.Label.Text = $"{attr}: {value}";
		controls.Button.Disabled = _character!.AttributePoints <= 0;
	}

	private async void UpgradeAttribute(string attribute)
	{
		if (_character == null || _character.AttributePoints <= 0) return;

		switch (attribute)
		{
			case "Strength": _character.Strength++; break;
			case "Dexterity": _character.Dexterity++; break;
			case "Constitution": _character.Constitution++; break;
			case "Intelligence": _character.Intelligence++; break;
			case "Wisdom": _character.Wisdom++; break;
			case "Charisma": _character.Charisma++; break;
		}

		_character.AttributePoints--;
		UpdateUI();
		await _characterService.SaveCharacterAsync(_character);
	}
}
