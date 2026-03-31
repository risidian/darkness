using System.Threading.Tasks;
using Godot;

namespace Darkness.Godot.Core;

public static class TaskExtensions
{
    public static async void FireAndForget(this Task task)
    {
        try
        {
            await task;
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"FireAndForget task failed: {ex.Message}");
        }
    }
}
