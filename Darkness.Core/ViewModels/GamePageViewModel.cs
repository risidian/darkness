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
        private DeathmatchEncounter? _encounter;

        [ObservableProperty]
        private string _mode = string.Empty;

        [ObservableProperty]
        private Character? _player1;

        [ObservableProperty]
        private Character? _player2;

        public GamePageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public async Task OpenCombatTestAsync()
        {
            await _navigationService.NavigateToAsync("///BattlePage");
        }
    }
}
