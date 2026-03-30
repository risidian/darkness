using Darkness.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Darkness.Godot.Services;

public class GodotDispatcherService : IDispatcherService
{
    public void InvokeOnMainThread(Action action)
    {
        action();
    }
}
