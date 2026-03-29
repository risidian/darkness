using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class BattlePageViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly StoryController _storyController;

        private Character? _currentPlayer;
        private List<Character> _party = new();

        [ObservableProperty]
        private string _statusText = "Battle in progress...";

        [ObservableProperty]
        private bool _isContinueVisible;

        [ObservableProperty]
        private string _statusColor = "White";

        public BattlePageViewModel(
            ICharacterService characterService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService,
            StoryController storyController)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _storyController = storyController;
        }

        public void Initialize(int storyBeat)
        {
            // TODO: Load from session once character selection is wired up
            _currentPlayer = new Character
            {
                Name = "The Wanderer",
                Class = "Survivor",
                Strength = 12,
                Dexterity = 10,
                Constitution = 12,
                Intelligence = 8,
                Wisdom = 10,
                Charisma = 8,
                MaxHP = 100,
                CurrentHP = 100,
                Defense = 5,
                Speed = 10
            };

            _party = new List<Character> { _currentPlayer };
            _storyController.SetBeat(storyBeat);

            SetupBattle();
        }

        private void SetupBattle()
        {
            var encounter = _storyController.GetEncounterForBeat(_storyController.CurrentBeat);
            var enemies = encounter.Enemies;

            if (enemies == null || enemies.Count == 0)
            {
                StatusText = "No enemies encountered. The path is clear.";
                IsContinueVisible = true;
                return;
            }

            foreach (var member in encounter.AdditionalPartyMembers)
            {
                if (!_party.Any(p => p.Name == member.Name))
                {
                    _party.Add(member);
                }
            }

            StatusText = $"Story Beat {_storyController.CurrentBeat}: Encountered {enemies[0].Name}!";
            if (encounter.SurvivalTurns.HasValue)
            {
                StatusText += $" Survive for {encounter.SurvivalTurns} turns!";
            }

            if (_party.Count > 1)
            {
                StatusText += $"\nJoined by: {string.Join(", ", _party.Skip(1).Select(p => p.Name))}";
            }
        }

        public void OnBattleEnded(bool victory)
        {
            if (victory)
            {
                StatusText = "Victory! The hounds have been silenced.";
                StatusColor = "Gold";
            }
            else
            {
                StatusText = "Defeat... Darkness consumes you.";
                StatusColor = "DarkRed";
            }

            IsContinueVisible = true;
        }

        [RelayCommand]
        public async Task FleeAsync()
        {
            bool confirmed = await _dialogService.DisplayConfirmAsync("Flee", "Are you sure you want to attempt to flee?", "Yes", "No");
            if (confirmed)
            {
                await _navigationService.NavigateToAsync("///GamePage");
            }
        }

        [RelayCommand]
        public async Task ContinueAsync()
        {
            await _navigationService.NavigateToAsync("///GamePage");
        }
    }
}
