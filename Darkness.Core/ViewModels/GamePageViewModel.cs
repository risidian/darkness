using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;

namespace Darkness.Core.ViewModels
{
    public partial class GamePageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

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
