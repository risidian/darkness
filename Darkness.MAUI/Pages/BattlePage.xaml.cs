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
        }
    }
}
