using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface INavigationService
{
    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
    Task NavigateToAsync<T>(string route, T parameters) where T : NavigationArgs;
    Task GoBackAsync();
}