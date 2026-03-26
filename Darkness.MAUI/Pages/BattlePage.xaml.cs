using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class BattlePage : ContentPage
    {
        private readonly BattlePageViewModel _viewModel;

        public BattlePage(BattlePageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.Initialize(4);
        }
    }
}
