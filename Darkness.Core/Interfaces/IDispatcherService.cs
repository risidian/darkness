namespace Darkness.Core.Interfaces;

public interface IDispatcherService
{
    void InvokeOnMainThread(Action action);
}
