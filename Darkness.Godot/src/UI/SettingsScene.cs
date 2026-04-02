using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class SettingsScene : Control
{
	private ISettingsService _settingsService;
	private INavigationService _navigation;

	private HSlider _masterSlider;
	private HSlider _musicSlider;
	private HSlider _sfxSlider;

	public override void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_settingsService = global.Services!.GetRequiredService<ISettingsService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();

		_masterSlider = GetNode<HSlider>("ScrollContainer/MarginContainer/VBoxContainer/MasterVol/Slider");
		_musicSlider = GetNode<HSlider>("ScrollContainer/MarginContainer/VBoxContainer/MusicVol/Slider");
		_sfxSlider = GetNode<HSlider>("ScrollContainer/MarginContainer/VBoxContainer/SfxVol/Slider");

		_masterSlider.Value = _settingsService.MasterVolume;
		_musicSlider.Value = _settingsService.MusicVolume;
		_sfxSlider.Value = _settingsService.SfxVolume;

		GetNode<Button>("ScrollContainer/MarginContainer/VBoxContainer/HBoxContainer/SaveButton").Pressed += OnSavePressed;
		GetNode<Button>("ScrollContainer/MarginContainer/VBoxContainer/HBoxContainer/CancelButton").Pressed += OnCancelPressed;
	}

	private async void OnSavePressed()
	{
		_settingsService.MasterVolume = _masterSlider.Value;
		_settingsService.MusicVolume = _musicSlider.Value;
		_settingsService.SfxVolume = _sfxSlider.Value;
		
		await _settingsService.SaveSettingsAsync();
		await _navigation.GoBackAsync();
	}

	private async void OnCancelPressed()
	{
		await _navigation.GoBackAsync();
	}
}
