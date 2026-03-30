using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class DeathmatchViewModel : ViewModelBase
    {
        private readonly IDeathmatchService _deathmatchService;
        private readonly INavigationService _navigationService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<DeathmatchEncounter> _encounters = new();

        [ObservableProperty]
        private DeathmatchEncounter? _selectedEncounter;

        public DeathmatchViewModel(IDeathmatchService deathmatchService, INavigationService navigationService, ISessionService sessionService)
        {
            _deathmatchService = deathmatchService;
            _navigationService = navigationService;
            _sessionService = sessionService;
        }

        [RelayCommand]
        public async Task LoadEncountersAsync()
        {
            var encounters = await _deathmatchService.GetEncountersAsync();
            Encounters = new ObservableCollection<DeathmatchEncounter>(encounters);
        }

        [RelayCommand]
        public async Task StartDeathmatchAsync()
        {
            if (SelectedEncounter == null) return;

            // In a real implementation, we would pass the encounter to the GamePage
            // For now, we'll navigate to GamePage and pass the encounter
            await _navigationService.NavigateToAsync("GamePage", new Dictionary<string, object>
            {
                { "Encounter", SelectedEncounter }
            });
        }
    }
}
