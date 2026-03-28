using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class LoadUserViewModelTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly LoadUserViewModel _viewModel;

        public LoadUserViewModelTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _characterServiceMock = new Mock<ICharacterService>();

            _viewModel = new LoadUserViewModel(
                _userServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _dialogServiceMock.Object,
                _characterServiceMock.Object);
        }

        [Fact]
        public async Task LoginAsync_WithEmptyCredentials_ShowsError()
        {
            // Arrange
            _viewModel.Username = "";
            _viewModel.Password = "";

            // Act
            await _viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", It.Is<string>(s => s.Contains("required")), "OK"), Times.Once);
            _userServiceMock.Verify(x => x.GetUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_NoCharacters_NavigatesToCharacterGen()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            _viewModel.Username = "testuser";
            _viewModel.Password = "password";
            _userServiceMock.Setup(x => x.GetUserAsync("testuser", "password")).ReturnsAsync(user);
            _characterServiceMock.Setup(x => x.GetCharactersForUserAsync(1)).ReturnsAsync(new List<Character>());

            // Act
            await _viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _sessionServiceMock.VerifySet(x => x.CurrentUser = user, Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///CharacterGenPage", null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_HasCharacters_NavigatesToMainPage()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser" };
            _viewModel.Username = "testuser";
            _viewModel.Password = "password";
            _userServiceMock.Setup(x => x.GetUserAsync("testuser", "password")).ReturnsAsync(user);
            _characterServiceMock.Setup(x => x.GetCharactersForUserAsync(1))
                .ReturnsAsync(new List<Character> { new Character { Id = 1, Name = "Hero" } });

            // Act
            await _viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _sessionServiceMock.VerifySet(x => x.CurrentUser = user, Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///MainPage", null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ShowsError()
        {
            // Arrange
            _viewModel.Username = "wrong";
            _viewModel.Password = "wrong";
            _userServiceMock.Setup(x => x.GetUserAsync("wrong", "wrong")).ReturnsAsync((User?)null);

            // Act
            await _viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", It.Is<string>(s => s.Contains("Invalid")), "OK"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task CreateUserAsync_NavigatesToCreatePage()
        {
            // Act
            await _viewModel.CreateUserCommand.ExecuteAsync(null);

            // Assert
            _navigationServiceMock.Verify(x => x.NavigateToAsync("//CreateUserPage", null), Times.Once);
        }
    }
}
