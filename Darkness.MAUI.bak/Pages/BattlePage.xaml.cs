using Darkness.Core.ViewModels;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Darkness.Game;

namespace Darkness.MAUI.Pages
{
    public partial class BattlePage : ContentPage
    {
        private readonly BattlePageViewModel _viewModel;
        private DarknessGame? _game;
        private readonly ISessionService _sessionService;
        private readonly ICombatService _combatService;
        private readonly StoryController _storyController;

        public BattlePage(BattlePageViewModel viewModel, ISessionService sessionService, ICombatService combatService, StoryController storyController)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _sessionService = sessionService;
            _combatService = combatService;
            _storyController = storyController;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.Initialize(4);

            if (_game == null)
            {
                _game = new DarknessGame(_combatService, _sessionService, _storyController);
                _viewModel.GameInstance = _game;
            }

            _game.Resume();

            // Start the actual game engine battle using a snapshot to decouple state
            var encounter = _viewModel.GetEncounter();
            var party = _viewModel.GetParty();
            
            if (encounter.Enemies != null && encounter.Enemies.Count > 0)
            {
                var snapshot = new BattleSnapshot(
                    party.Select(p => p.ToSnapshot()).ToList(),
                    encounter.Enemies,
                    encounter.SurvivalTurns
                );
                _game.StartBattle(snapshot);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _game?.Pause();
        }
    }
}
