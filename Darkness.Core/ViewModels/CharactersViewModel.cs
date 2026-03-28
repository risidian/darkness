using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class CharactersViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<Character> _characters = new();

        [ObservableProperty]
        private Character? _selectedCharacter;

        public CharactersViewModel(ICharacterService characterService, ISessionService sessionService)
        {
            _characterService = characterService;
            _sessionService = sessionService;
        }

        [RelayCommand]
        public async Task LoadCharactersAsync()
        {
            if (_sessionService.CurrentUser == null) return;
            var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
            Characters = new ObservableCollection<Character>(characters);
        }
    }
}
