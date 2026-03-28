using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    [QueryProperty(nameof(Encounter), "Encounter")]
    public partial class GamePageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private DeathmatchEncounter? _encounter;

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
