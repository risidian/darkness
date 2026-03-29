using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class CreateUserViewModelTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<ISettingsService> _settingsServiceMock;
        private readonly CreateUserViewModel _viewModel;

        public CreateUserViewModelTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _settingsServiceMock = new Mock<ISettingsService>();

            _viewModel = new CreateUserViewModel(
                _userServiceMock.Object,
                _navigationServiceMock.Object,
                _dialogServiceMock.Object,
                _sessionServiceMock.Object,
                _settingsServiceMock.Object);
        }

        [Fact]
        public async Task CreateAccountAsync_WithEmptyUsername_ShowsError()
        {
            _viewModel.Username = "";
            _viewModel.Password = "pass";
            _viewModel.Email = "test@test.com";

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "All fields are required.", "OK"), Times.Once);
            _userServiceMock.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_WithEmptyPassword_ShowsError()
        {
            _viewModel.Username = "user";
            _viewModel.Password = "";
            _viewModel.Email = "test@test.com";

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "All fields are required.", "OK"), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_WithEmptyEmail_ShowsError()
        {
            _viewModel.Username = "user";
            _viewModel.Password = "pass";
            _viewModel.Email = "";

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "All fields are required.", "OK"), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_OnSuccess_ShowsSuccessAndNavigates()
        {
            _viewModel.Username = "newuser";
            _viewModel.Password = "password";
            _viewModel.Email = "new@user.com";
            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(true);

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Success", It.Is<string>(s => s.Contains("newuser")), "OK"), Times.Once);
            _sessionServiceMock.VerifySet(x => x.CurrentUser = It.Is<User>(u => u.Username == "newuser"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///CharacterGenPage", null), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_OnSuccess_PassesCorrectUserData()
        {
            _viewModel.Username = "newuser";
            _viewModel.Password = "password";
            _viewModel.Email = "new@user.com";

            User? createdUser = null;
            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                .Callback<User>(u => createdUser = u)
                .ReturnsAsync(true);

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            Assert.NotNull(createdUser);
            Assert.Equal("newuser", createdUser!.Username);
            Assert.Equal("password", createdUser.Password);
            Assert.Equal("new@user.com", createdUser.EmailAddress);
            Assert.Equal(18, createdUser.Age);
        }

        [Fact]
        public async Task CreateAccountAsync_OnFailure_ShowsError()
        {
            _viewModel.Username = "user";
            _viewModel.Password = "pass";
            _viewModel.Email = "e@e.com";
            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ReturnsAsync(false);

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "Failed to create user.", "OK"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_OnException_ShowsErrorMessage()
        {
            _viewModel.Username = "user";
            _viewModel.Password = "pass";
            _viewModel.Email = "e@e.com";
            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<User>())).ThrowsAsync(new Exception("DB error"));

            await _viewModel.CreateAccountCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", It.Is<string>(s => s.Contains("DB error")), "OK"), Times.Once);
        }

        [Fact]
        public async Task GoBackAsync_NavigatesToLoadUserPage()
        {
            await _viewModel.GoBackCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("///LoadUserPage", null), Times.Once);
        }
    }
}
