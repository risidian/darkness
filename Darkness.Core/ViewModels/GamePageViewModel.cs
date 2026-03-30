using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class GamePageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private object? _gameInstance;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEngineUnavailable))]
        private bool _isEngineAvailable;

        public bool IsEngineUnavailable => !IsEngineAvailable;

        [ObservableProperty]
        private DeathmatchEncounter? _encounter;

        [ObservableProperty]
        private string? _mode;

        [ObservableProperty]
        private Character? _player1;

        [ObservableProperty]
        private Character? _player2;

        public GamePageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void SetGame(object game)
        {
            GameInstance = game;
            IsEngineAvailable = true;
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await _navigationService.GoBackAsync();
        }

        [RelayCommand]
        public async Task OpenCombatTestAsync()
        {
            await _navigationService.NavigateToAsync("BattlePage");
        }
    }
}
