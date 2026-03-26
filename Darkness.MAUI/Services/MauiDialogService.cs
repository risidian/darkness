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
    }
}
