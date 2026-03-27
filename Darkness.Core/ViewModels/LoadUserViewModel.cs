using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;

namespace Darkness.Core.ViewModels
{
    public partial class LoadUserViewModel : ViewModelBase
    {
        private readonly IUserService _userService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private string _username = string.Empty;

        [ObservableProperty] private string _password = string.Empty;

        public LoadUserViewModel(
            IUserService userService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _userService = userService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await _dialogService.DisplayAlertAsync("Error", "Username and password are required.", "OK");
                return;
            }

            try
            {
                var user = await _userService.GetUserAsync(Username, Password);
                if (user != null)
                {
                    _sessionService.CurrentUser = user;
                    await _navigationService.NavigateToAsync("///MainPage");
                }
                else
                {
                    await _dialogService.DisplayAlertAsync("Error", "Invalid username or password.", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Error", $"Login failed: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task CreateUserAsync()
        {
            await _navigationService.NavigateToAsync("//CreateUserPage");
        }
    }
}