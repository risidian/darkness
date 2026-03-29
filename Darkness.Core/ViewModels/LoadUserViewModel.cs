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
        private readonly ICharacterService _characterService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty] private string _username = string.Empty;

        [ObservableProperty] private string _password = string.Empty;

        public LoadUserViewModel(
            IUserService userService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService,
            ICharacterService characterService,
            ISettingsService settingsService)
        {
            _userService = userService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _characterService = characterService;
            _settingsService = settingsService;
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
                    
                    // Save last user ID
                    _settingsService.LastUserId = user.Id;
                    await _settingsService.SaveSettingsAsync();

                    var characters = await _characterService.GetCharactersForUserAsync(user.Id);
                    if (characters == null || characters.Count == 0)
                    {
                        await _navigationService.NavigateToAsync("///CharacterGenPage");
                    }
                    else
                    {
                        await _navigationService.NavigateToAsync("///MainPage");
                    }
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
