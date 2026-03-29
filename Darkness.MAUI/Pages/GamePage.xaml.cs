using Darkness.Core.ViewModels;
using Darkness.Core.Models;
using Darkness.Core.Interfaces;
using Darkness.Game;

namespace Darkness.MAUI.Pages
{
    [QueryProperty(nameof(Encounter), "Encounter")]
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Player1), "Player1")]
    [QueryProperty(nameof(Player2), "Player2")]
    public partial class GamePage : ContentPage
    {
        private readonly GamePageViewModel _viewModel;
        private readonly DarknessGame _game;
        private readonly ISessionService _sessionService;

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

        public GamePage(GamePageViewModel viewModel, DarknessGame game, ISessionService sessionService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _game = game;
            _sessionService = sessionService;
            BindingContext = _viewModel;

            _viewModel.SetGame(_game);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            if (_viewModel.Mode == "PVP")
            {
                if (_viewModel.Player1 != null && _viewModel.Player2 != null)
                {
                    _game.StartPvp(_viewModel.Player1, _viewModel.Player2);
                }
            }
            else if (_viewModel.Encounter != null)
            {
                if (_sessionService.CurrentCharacter != null)
                {
                    _game.StartDeathmatch(new List<Character> { _sessionService.CurrentCharacter }, _viewModel.Encounter);
                }
            }
            else
            {
                // Story mode or World mode
                System.Diagnostics.Debug.WriteLine("Starting Story/World mode");
            }

            // In a real implementation with a proper MonoGame host, 
            // the game would be running inside the MonoGameHost control.
        }
    }
}
