using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class CharacterGenViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _characterName = string.Empty;

        [ObservableProperty]
        private string _selectedClass = "Warrior";

        [ObservableProperty]
        private string _selectedHairColor = "Black";

        [ObservableProperty]
        private string _selectedSkinColor = "Light";

        public List<string> Classes { get; } = new() { "Warrior", "Mage", "Rogue" };
        public List<string> HairColors { get; } = new() { "Black", "Blonde", "Brown", "Red", "White" };
        public List<string> SkinColors { get; } = new() { "Light", "Tan", "Dark" };

        public CharacterGenViewModel(
            ICharacterService characterService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task CreateCharacterAsync()
        {
            if (string.IsNullOrWhiteSpace(CharacterName))
            {
                await _dialogService.DisplayAlertAsync("Error", "Please enter a name.", "OK");
                return;
            }

            if (_sessionService.CurrentUser == null)
            {
                await _dialogService.DisplayAlertAsync("Error", "No user session found. Please login again.", "OK");
                await _navigationService.NavigateToAsync("///LoadUserPage");
                return;
            }

            var character = new Character
            {
                UserId = _sessionService.CurrentUser.Id,
                Name = CharacterName,
                Class = SelectedClass,
                HairColor = SelectedHairColor,
                SkinColor = SelectedSkinColor,
                Level = 1,
                Experience = 0
            };

            SetBaseStats(character, SelectedClass);

            bool success = await _characterService.SaveCharacterAsync(character);

            if (success)
            {
                await _dialogService.DisplayAlertAsync("Success", "Character Created!", "OK");
                await _navigationService.NavigateToAsync("///MainPage");
            }
            else
            {
                await _dialogService.DisplayAlertAsync("Error", "Failed to save character.", "OK");
            }
        }

        private void SetBaseStats(Character character, string className)
        {
            switch (className)
            {
                case "Warrior":
                    character.STR = 15; character.DEX = 10; character.CON = 15;
                    character.INT = 5; character.WIS = 8; character.CHA = 10;
                    break;
                case "Mage":
                    character.STR = 5; character.DEX = 10; character.CON = 8;
                    character.INT = 18; character.WIS = 15; character.CHA = 10;
                    break;
                case "Rogue":
                    character.STR = 10; character.DEX = 18; character.CON = 10;
                    character.INT = 10; character.WIS = 8; character.CHA = 12;
                    break;
                default:
                    character.STR = 10; character.DEX = 10; character.CON = 10;
                    character.INT = 10; character.WIS = 10; character.CHA = 10;
                    break;
            }

            character.MaxHP = character.CON * 10;
            character.CurrentHP = character.MaxHP;
            character.Mana = character.WIS * 5;
            character.Stamina = character.CON * 5;
            character.Speed = character.DEX;
            character.Accuracy = 80 + character.DEX / 2;
            character.Evasion = character.DEX / 2;
            character.Defense = character.CON / 2;
            character.MagicDefense = character.WIS / 2;
        }
    }
}
