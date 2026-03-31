using Darkness.Core.Interfaces;
using Darkness.Godot.Core;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.Services;

public class GodotNavigationService : INavigationService
{
    private readonly Global _global;

    public GodotNavigationService(Global global)
    {
        _global = global;
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        GD.Print($"[Navigation] Navigating to route: {route}");
        // 1. Convert route name to Godot scene path (e.g. "MainPage" -> "res://scenes/MainScene.tscn")
        // We'll follow a convention for now
        string sceneName = route.Replace("Page", "Scene");
        string path = $"res://scenes/{sceneName}.tscn";
        GD.Print($"[Navigation] Target path: {path}");

        // 2. Change scene
        var error = _global.GetTree().ChangeSceneToFile(path);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Navigation] Failed to change scene to {path}: {error}");
            return;
        }

        GD.Print($"[Navigation] Scene change initiated for {path}");

        // 3. Handle parameter injection after scene enters tree
        // Note: ChangeSceneToFile is async/delayed. We need to wait for the next frame.
        if (parameters != null)
        {
            // Wait for the scene to be ready in the next frame
            await _global.ToSignal(_global.GetTree(), "process_frame");
            
            var root = _global.GetTree().CurrentScene;
            if (root is IInitializable initializable)
            {
                initializable.Initialize(parameters);
            }
        }
    }

    public Task GoBackAsync()
    {
        // For now, go back to main menu
        return NavigateToAsync("MainPage");
    }
}
