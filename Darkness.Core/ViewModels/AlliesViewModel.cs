using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Core.ViewModels
{
    public partial class AlliesViewModel : ViewModelBase
    {
        private readonly IAllyService _allyService;
        private readonly ISessionService _sessionService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Ally> _allies = new();

        [ObservableProperty]
        private bool _isRefreshing;

        public AlliesViewModel(IAllyService allyService, ISessionService sessionService, IDialogService dialogService)
        {
            _allyService = allyService;
            _sessionService = sessionService;
            _dialogService = dialogService;
        }

        public async Task OnAppearingAsync()
        {
            await RefreshAlliesAsync();
        }

        [RelayCommand]
        private async Task RefreshAlliesAsync()
        {
            if (_sessionService.CurrentUser == null) return;

            IsRefreshing = true;
            try
            {
                var alliesList = await _allyService.GetAlliesForUserAsync(_sessionService.CurrentUser.Id);
                Allies.Clear();
                foreach (var ally in alliesList)
                {
                    Allies.Add(ally);
                }
            }
            catch (System.Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Error", $"Failed to load allies: {ex.Message}", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task AcceptRequestAsync(Ally ally)
        {
            if (ally == null) return;
            var success = await _allyService.RespondToAllyRequestAsync(ally.Id, true);
            if (success)
            {
                await RefreshAlliesAsync();
                await _dialogService.DisplayAlertAsync("Success", $"You are now allies with {ally.AllyUsername}.", "OK");
            }
        }

        [RelayCommand]
        private async Task DeclineRequestAsync(Ally ally)
        {
            if (ally == null) return;
            var success = await _allyService.RespondToAllyRequestAsync(ally.Id, false);
            if (success)
            {
                await RefreshAlliesAsync();
                await _dialogService.DisplayAlertAsync("Declined", $"Request from {ally.AllyUsername} has been declined.", "OK");
            }
        }
    }
}
