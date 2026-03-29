using Darkness.Core.ViewModels;
using Darkness.Core.Models;

namespace Darkness.MAUI.Pages
{
    [QueryProperty(nameof(Encounter), "Encounter")]
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Player1), "Player1")]
    [QueryProperty(nameof(Player2), "Player2")]
    public partial class GamePage : ContentPage
    {
        private readonly GamePageViewModel _viewModel;

        public DeathmatchEncounter? Encounter
        {
            get => _viewModel.Encounter;
            set => _viewModel.Encounter = value;
        }

        public string? Mode
        {
            get => _viewModel.Mode;
            set => _viewModel.Mode = value;
        }

        public Character? Player1
        {
            get => _viewModel.Player1;
            set => _viewModel.Player1 = value;
        }

        public Character? Player2
        {
            get => _viewModel.Player2;
            set => _viewModel.Player2 = value;
        }

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
