using Darkness.Core.ViewModels;
using Darkness.Core.Models;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
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
        private DarknessGame? _game;
        private readonly ISessionService _sessionService;
        private readonly ICombatService _combatService;
        private readonly StoryController _storyController;

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

        public GamePage(GamePageViewModel viewModel, ISessionService sessionService, ICombatService combatService, StoryController storyController)
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

            // Lazy initialization of the game engine
            if (_game == null)
            {
                try
                {
                    _game = new DarknessGame(_combatService, _sessionService, _storyController);
                    _viewModel.SetGame(_game);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GamePage] Failed to create DarknessGame: {ex.Message}");
                    return;
                }
            }

            _game.Resume();

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
                System.Diagnostics.Debug.WriteLine("Starting Story/World mode");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Pause game engine to prevent background processing
            System.Diagnostics.Debug.WriteLine("Leaving World - pausing Game engine");
            _game?.Pause();
        }
    }
}
