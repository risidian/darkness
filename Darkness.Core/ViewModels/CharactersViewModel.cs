using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class CharactersViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<Character> _characters = new();

        [ObservableProperty]
        private Character? _selectedCharacter;

        public CharactersViewModel(ICharacterService characterService, ISessionService sessionService, INavigationService navigationService)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        public async Task LoadCharactersAsync()
        {
            if (_sessionService.CurrentUser == null) return;
            var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
            Characters = new ObservableCollection<Character>(characters);
        }

        [RelayCommand]
        public async Task GoToStudy()
        {
            if (SelectedCharacter == null) return;
            await _navigationService.NavigateToAsync("///StudyPage", new Dictionary<string, object>
            {
                { "Character", SelectedCharacter }
            });
        }
    }
}
