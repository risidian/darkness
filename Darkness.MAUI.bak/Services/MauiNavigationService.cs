using Darkness.Core.Interfaces;

namespace Darkness.MAUI.Services
{
    public class MauiNavigationService : INavigationService
    {
        public Task GoBackAsync()
        {
            return Shell.Current.GoToAsync("..");
        }

        public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (parameters != null)
            {
                return Shell.Current.GoToAsync(route, parameters);
            }

            return Shell.Current.GoToAsync(route);
        }
    }
}
