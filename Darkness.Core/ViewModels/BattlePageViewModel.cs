using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class BattlePageViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IQuestService _questService;

        private Character? _currentPlayer;
        private List<Character> _party = new();
        private string? _currentQuestId;

        [ObservableProperty]
        private object? _gameInstance;

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
            IQuestService questService)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _questService = questService;
        }

        public (List<Enemy> Enemies, int? SurvivalTurns, List<Character> AdditionalPartyMembers) GetEncounter()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return (new(), null, new());
            var quest = _questService.GetQuestById(_currentQuestId);
            if (quest?.Encounter == null) return (new(), null, new());
            
            return (quest.Encounter.Enemies, quest.Encounter.SurvivalTurns, quest.Encounter.AdditionalPartyMembers);
        }

        public List<Character> GetParty()
        {
            return _party;
        }

        public void Initialize(string questId)
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
            _currentQuestId = questId;

            SetupBattle();
        }

        private void SetupBattle()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            var quest = _questService.GetQuestById(_currentQuestId);
            var encounter = quest?.Encounter;

            if (encounter == null || encounter.Enemies == null || encounter.Enemies.Count == 0)
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

            StatusText = $"Quest: {quest!.Title}\nEncountered {encounter.Enemies[0].Name}!";
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
                await _navigationService.GoBackAsync();
            }
        }

        [RelayCommand]
        public async Task ContinueAsync()
        {
            await _navigationService.GoBackAsync();
        }
    }
}
