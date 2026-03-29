using Darkness.Core.ViewModels;
using Darkness.Game;

namespace Darkness.MAUI.Pages
{
    public partial class BattlePage : ContentPage
    {
        private readonly BattlePageViewModel _viewModel;
        private readonly DarknessGame _game;

        public BattlePage(BattlePageViewModel viewModel, DarknessGame game)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _game = game;
            BindingContext = _viewModel;

            _viewModel.GameInstance = _game;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.Initialize(4);

            // Start the actual game engine battle
            var encounter = _viewModel.GetEncounter();
            var party = _viewModel.GetParty();
            
            if (encounter.Enemies != null && encounter.Enemies.Count > 0)
            {
                _game.StartBattle(party, encounter.Enemies, encounter.SurvivalTurns);
            }
        }
    }
}
