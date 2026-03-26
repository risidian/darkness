using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class CreateUserViewModel : ViewModelBase
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        public CreateUserViewModel(
            IUserService userService,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            _userService = userService;
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        public async Task CreateAccountAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(Email))
            {
                await _dialogService.DisplayAlertAsync("Error", "All fields are required.", "OK");
                return;
            }

            var newUser = new User
            {
                Username = Username,
                Password = Password,
                EmailAddress = Email,
                Age = 18
            };

            try
            {
                bool success = await _userService.CreateUserAsync(newUser);
                if (success)
                {
                    await _dialogService.DisplayAlertAsync("Success", $"Created user: {newUser.Username}", "OK");
                    await _navigationService.NavigateToAsync("///LoadUserPage");
                }
                else
                {
                    await _dialogService.DisplayAlertAsync("Error", "Failed to create user.", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Error", $"Failed to create: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await _navigationService.NavigateToAsync("///LoadUserPage");
        }
    }
}
