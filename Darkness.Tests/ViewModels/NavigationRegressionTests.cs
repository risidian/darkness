using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.ViewModels;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Darkness.Tests.ViewModels
{
    public class NavigationRegressionTests
    {
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<IRewardService> _rewardServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IDeathmatchService> _deathmatchServiceMock;
        private readonly Mock<ISettingsService> _settingsServiceMock;

        public NavigationRegressionTests()
        {
            _navigationServiceMock = new Mock<INavigationService>();
            _characterServiceMock = new Mock<ICharacterService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _rewardServiceMock = new Mock<IRewardService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _deathmatchServiceMock = new Mock<IDeathmatchService>();
            _settingsServiceMock = new Mock<ISettingsService>();
        }

        [Fact]
        public async Task MainViewModel_DeathmatchAsync_UsesAbsoluteRoute()
        {
            var viewModel = new MainViewModel(
                _rewardServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _characterServiceMock.Object,
                _dialogServiceMock.Object,
                _settingsServiceMock.Object);

            await viewModel.DeathmatchCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("DeathmatchPage", null), Times.Once);
        }

        [Fact]
        public async Task MainViewModel_CharactersAsync_UsesAbsoluteRoute()
        {
            var viewModel = new MainViewModel(
                _rewardServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _characterServiceMock.Object,
                _dialogServiceMock.Object,
                _settingsServiceMock.Object);

            await viewModel.CharactersCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("CharactersPage", null), Times.Once);
        }

        [Fact]
        public async Task MainViewModel_SettingsAsync_UsesAbsoluteRoute()
        {
            var viewModel = new MainViewModel(
                _rewardServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _characterServiceMock.Object,
                _dialogServiceMock.Object,
                _settingsServiceMock.Object);

            await viewModel.SettingsCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("SettingsPage", null), Times.Once);
        }

        [Fact]
        public async Task DeathmatchViewModel_StartDeathmatchAsync_UsesAbsoluteRoute()
        {
            var viewModel = new DeathmatchViewModel(
                _deathmatchServiceMock.Object,
                _navigationServiceMock.Object,
                _sessionServiceMock.Object);

            viewModel.SelectedEncounter = new DeathmatchEncounter { Name = "Test" };

            await viewModel.StartDeathmatchCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("GamePage", It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task CharactersViewModel_GoToStudy_UsesAbsoluteRoute()
        {
            var viewModel = new CharactersViewModel(
                _characterServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object);

            viewModel.SelectedCharacter = new Character { Name = "Test" };

            await viewModel.GoToStudyCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("StudyPage", It.IsAny<IDictionary<string, object>>()), Times.Once);
        }
    }
}
