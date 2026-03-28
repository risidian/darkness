using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private double _masterVolume;

        [ObservableProperty]
        private double _musicVolume;

        [ObservableProperty]
        private double _sfxVolume;

        public SettingsViewModel(
            ISettingsService settingsService, 
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _settingsService = settingsService;
            _navigationService = navigationService;
            _dialogService = dialogService;

            _masterVolume = _settingsService.MasterVolume;
            _musicVolume = _settingsService.MusicVolume;
            _sfxVolume = _settingsService.SfxVolume;
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            _settingsService.MasterVolume = MasterVolume;
            _settingsService.MusicVolume = MusicVolume;
            _settingsService.SfxVolume = SfxVolume;
            await _settingsService.SaveSettingsAsync();
            await _navigationService.GoBackAsync();
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await _navigationService.GoBackAsync();
        }

        [RelayCommand]
        public Task GraphicsSettingsAsync() => _dialogService.DisplayAlertAsync("Graphics", "Graphics settings coming soon!", "OK");

        [RelayCommand]
        public Task KeybindingsAsync() => _dialogService.DisplayAlertAsync("Keybindings", "Keybindings settings coming soon!", "OK");
    }
}
