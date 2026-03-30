using Darkness.Core.Interfaces;

namespace Darkness.MAUI.Services;

public class MauiDispatcherService : IDispatcherService
{
    public void InvokeOnMainThread(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }
}
