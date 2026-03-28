using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class BattlePageViewModelTests
    {
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly StoryController _storyController;
        private readonly BattlePageViewModel _viewModel;

        public BattlePageViewModelTests()
        {
            _characterServiceMock = new Mock<ICharacterService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _storyController = new StoryController();

            _viewModel = new BattlePageViewModel(
                _characterServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _dialogServiceMock.Object,
                _storyController);
        }

        [Fact]
        public void Initialize_Beat4_ShowsDarkWarriorEncounter()
        {
            _viewModel.Initialize(4);

            Assert.Contains("Dark Warrior", _viewModel.StatusText);
            Assert.Contains("Story Beat 4", _viewModel.StatusText);
            Assert.Contains("Survive for 5 turns", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_Beat8_ShowsAraknosDemonWithTywin()
        {
            _viewModel.Initialize(8);

            Assert.Contains("Araknos Demon", _viewModel.StatusText);
            Assert.Contains("Tywin", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_Beat9_ShowsFinalBossWithTywin()
        {
            _viewModel.Initialize(9);

            Assert.Contains("Kyarias the Undead King", _viewModel.StatusText);
            Assert.Contains("Tywin", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_DefaultBeat_ShowsShadowMinion()
        {
            _viewModel.Initialize(1);

            Assert.Contains("Shadow Minion", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_BeatAfter8_IncludesTywin()
        {
            _viewModel.Initialize(8);

            Assert.Contains("Tywin", _viewModel.StatusText);
        }

        [Fact]
        public void OnBattleEnded_Victory_SetsVictoryState()
        {
            _viewModel.Initialize(4);

            _viewModel.OnBattleEnded(true);

            Assert.Contains("Victory", _viewModel.StatusText);
            Assert.Equal("Gold", _viewModel.StatusColor);
            Assert.True(_viewModel.IsContinueVisible);
        }

        [Fact]
        public void OnBattleEnded_Defeat_SetsDefeatState()
        {
            _viewModel.Initialize(4);

            _viewModel.OnBattleEnded(false);

            Assert.Contains("Defeat", _viewModel.StatusText);
            Assert.Equal("DarkRed", _viewModel.StatusColor);
            Assert.True(_viewModel.IsContinueVisible);
        }

        [Fact]
        public async Task FleeAsync_WhenConfirmed_NavigatesToGamePage()
        {
            _dialogServiceMock.Setup(x => x.DisplayConfirmAsync("Flee", It.IsAny<string>(), "Yes", "No"))
                .ReturnsAsync(true);

            await _viewModel.FleeCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("///GamePage", null), Times.Once);
        }

        [Fact]
        public async Task FleeAsync_WhenCancelled_DoesNotNavigate()
        {
            _dialogServiceMock.Setup(x => x.DisplayConfirmAsync("Flee", It.IsAny<string>(), "Yes", "No"))
                .ReturnsAsync(false);

            await _viewModel.FleeCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task ContinueAsync_NavigatesToGamePage()
        {
            await _viewModel.ContinueCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("///GamePage", null), Times.Once);
        }

        [Fact]
        public void Initialize_DefaultState_ContinueNotVisible()
        {
            _viewModel.Initialize(4);

            Assert.False(_viewModel.IsContinueVisible);
            Assert.Equal("White", _viewModel.StatusColor);
        }
    }
}
