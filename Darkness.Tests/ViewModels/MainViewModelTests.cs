using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class MainViewModelTests
    {
        private readonly Mock<IRewardService> _rewardServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _rewardServiceMock = new Mock<IRewardService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _characterServiceMock = new Mock<ICharacterService>();
            _dialogServiceMock = new Mock<IDialogService>();

            _viewModel = new MainViewModel(
                _rewardServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _characterServiceMock.Object,
                _dialogServiceMock.Object);
        }

        [Fact]
        public async Task OnAppearingAsync_WithNoUser_NavigatesToLogin()
        {
            // Arrange
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns((User?)null);

            // Act
            await _viewModel.OnAppearingAsync();

            // Assert
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///LoadUserPage", null), Times.Once);
        }

        [Fact]
        public async Task OnAppearingAsync_WithUserButNoCharacters_NavigatesToGenPage()
        {
            // Arrange
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            _characterServiceMock.Setup(x => x.GetCharactersByUserIdAsync(1)).ReturnsAsync(new List<Character>());

            // Act
            await _viewModel.OnAppearingAsync();

            // Assert
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///CharacterGenPage", null), Times.Once);
        }

        [Fact]
        public async Task OnAppearingAsync_WithUserAndCharacters_ChecksDailyReward()
        {
            // Arrange
            var user = new User { Id = 1 };
            var reward = new Item { Name = "Gold", Description = "Some gold" };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            _characterServiceMock.Setup(x => x.GetCharactersByUserIdAsync(1)).ReturnsAsync(new List<Character> { new Character() });
            _rewardServiceMock.Setup(x => x.CheckDailyRewardAsync(user)).ReturnsAsync(reward);

            // Act
            await _viewModel.OnAppearingAsync();

            // Assert
            Assert.True(_viewModel.IsDailyRewardVisible);
            Assert.Contains("Gold", _viewModel.RewardText);
            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Daily Bonus!", It.Is<string>(s => s.Contains("Gold")), "Excellent"), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ClearsSessionAndNavigates()
        {
            // Act
            await _viewModel.LogoutCommand.ExecuteAsync(null);

            // Assert
            _sessionServiceMock.VerifySet(x => x.CurrentUser = null, Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///LoadUserPage", null), Times.Once);
        }
    }
}
