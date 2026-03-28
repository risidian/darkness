using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class StudyViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;

        [ObservableProperty]
        private Character? _character;

        public StudyViewModel(ICharacterService characterService)
        {
            _characterService = characterService;
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            UpgradeAttributeCommand.NotifyCanExecuteChanged();
        }

        private bool CanUpgrade(string? attribute) => Character != null && Character.AttributePoints > 0;

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeAttribute(string? attribute)
        {
            if (Character == null || Character.AttributePoints <= 0 || string.IsNullOrEmpty(attribute)) return;
            
            switch (attribute)
            {
                case "Strength": Character.Strength++; break;
                case "Dexterity": Character.Dexterity++; break;
                case "Constitution": Character.Constitution++; break;
                case "Intelligence": Character.Intelligence++; break;
                case "Wisdom": Character.Wisdom++; break;
                case "Charisma": Character.Charisma++; break;
            }
            
            Character.AttributePoints--;
            UpgradeAttributeCommand.NotifyCanExecuteChanged();
            await _characterService.SaveCharacterAsync(Character);
        }
    }
}
