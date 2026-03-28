using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class GamePage : ContentPage
    {
        private readonly GamePageViewModel _viewModel;

        public GamePage(GamePageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            if (_viewModel.Mode == "PVP")
            {
                // In a real implementation, this would trigger the MonoGame StartPvp
                System.Diagnostics.Debug.WriteLine($"Starting PVP: {_viewModel.Player1?.Name} vs {_viewModel.Player2?.Name}");
            }
        }
    }
}
