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
            NotifyCommandStates();
        }

        private bool CanUpgrade() => Character != null && Character.AttributePoints > 0;

        private void NotifyCommandStates()
        {
            UpgradeStrengthCommand.NotifyCanExecuteChanged();
            UpgradeDexterityCommand.NotifyCanExecuteChanged();
            UpgradeConstitutionCommand.NotifyCanExecuteChanged();
            UpgradeIntelligenceCommand.NotifyCanExecuteChanged();
            UpgradeWisdomCommand.NotifyCanExecuteChanged();
            UpgradeCharismaCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeStrength()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Strength++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }
        
        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeDexterity()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Dexterity++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeConstitution()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Constitution++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeIntelligence()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Intelligence++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeWisdom()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Wisdom++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }

        [RelayCommand(CanExecute = nameof(CanUpgrade))]
        public async Task UpgradeCharisma()
        {
            if (Character == null || Character.AttributePoints <= 0) return;
            Character.Charisma++;
            Character.AttributePoints--;
            OnPropertyChanged(nameof(Character));
            NotifyCommandStates();
            await _characterService.SaveCharacterAsync(Character);
        }
    }
}
