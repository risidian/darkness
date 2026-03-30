using Darkness.Core.Interfaces;

namespace Darkness.MAUI.Services
{
    public class MauiDialogService : IDialogService
    {
        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (App.Current?.MainPage != null)
            {
                return App.Current.MainPage.DisplayAlert(title, message, cancel);
            }

            return Task.CompletedTask;
        }

        public async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
        {
            if (App.Current?.MainPage != null)
            {
                return await App.Current.MainPage.DisplayAlert(title, message, accept, cancel);
            }

            return false;
        }
    }
}
