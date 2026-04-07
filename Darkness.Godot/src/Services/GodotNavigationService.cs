using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Godot.Core;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.Services;

public class GodotNavigationService : INavigationService
{
    private readonly Global _global;
    private bool _isNavigating = false;

    public GodotNavigationService(Global global)
    {
        _global = global;
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (_isNavigating) 
        {
            GD.PrintErr($"[Navigation] REJECTED navigation to {route} (dict) because _isNavigating is TRUE. Concurrent navigation is blocked.");
            return;
        }
        _isNavigating = true;

        try
        {
            GD.Print($"[Navigation] Navigating to route: {route}");

            string loadingText = route.Replace("Page", "").Replace("Scene", "");
            if (parameters != null)
            {
                if (parameters.ContainsKey("LoadingText") && parameters["LoadingText"] != null)
                    loadingText = parameters["LoadingText"]!.ToString()!;
                else if (parameters.ContainsKey("QuestChainId") && parameters["QuestChainId"] != null)
                    loadingText = parameters["QuestChainId"]!.ToString()!;
            }
            
            if (loadingText!.StartsWith("beat_"))
                loadingText = "Beat " + loadingText.Substring(5);

            // 1. Fade Out
            await _global.Transition.FadeOut(loadingText);

            // 2. Convert route name to Godot scene path
            string sceneName = route.Replace("Page", "Scene");
            string path = $"res://scenes/{sceneName}.tscn";
            GD.Print($"[Navigation] Target path: {path}");

            // 3. Load and instantiate the new scene
            var packedScene = GD.Load<PackedScene>(path);
            if (packedScene == null)
            {
                GD.PrintErr($"[Navigation] Failed to load scene {path}");
                await _global.Transition.FadeIn();
                return;
            }

            var newScene = packedScene.Instantiate();

            // 4. Inject parameters BEFORE adding to the tree (so Initialize runs before _Ready)
            if (newScene is IInitializable initializable && parameters != null)
            {
                GD.Print($"[Navigation] Calling Initialize on {newScene.Name}");
                initializable.Initialize(parameters);
            }
            else if (parameters != null)
            {
                GD.PrintErr($"[Navigation] ERROR: Scene {newScene.Name} does NOT implement IInitializable!");
            }

            // 5. Swap scenes
            var tree = _global.GetTree();
            var oldScene = tree.CurrentScene;
            if (oldScene != null)
            {
                tree.Root.RemoveChild(oldScene);
                oldScene.QueueFree();
            }
            
            tree.Root.AddChild(newScene);
            tree.CurrentScene = newScene;

            GD.Print($"[Navigation] Scene change completed. CurrentScene is now: {newScene.Name}");

            // Wait a frame for Godot to settle
            await _global.ToSignal(tree, "process_frame");

            // 6. Fade In
            await _global.Transition.FadeIn();
        }
        finally
        {
            _isNavigating = false;
            GD.Print($"[Navigation] Finished navigation to {route}. _isNavigating set to FALSE.");
        }
    }

    public async Task NavigateToAsync<T>(string route, T parameters) where T : NavigationArgs
    {
        if (_isNavigating) 
        {
            GD.PrintErr($"[Navigation] REJECTED navigation to {route} (typed) because _isNavigating is TRUE. Concurrent navigation is blocked.");
            return;
        }
        _isNavigating = true;

        try
        {
            GD.Print($"[Navigation] Navigating to {route} with typed args {typeof(T).Name}");

            string loadingText = route.Replace("Page", "").Replace("Scene", "");
            if (parameters is BattleArgs bArgs && !string.IsNullOrEmpty(bArgs.QuestChainId))
            {
                loadingText = bArgs.QuestChainId;
            }
            else if (parameters is StealthArgs sArgs && !string.IsNullOrEmpty(sArgs.QuestChainId))
            {
                loadingText = sArgs.QuestChainId;
            }

            if (loadingText.StartsWith("beat_"))
                loadingText = "Beat " + loadingText.Substring(5);

            // 1. Fade Out
            await _global.Transition.FadeOut(loadingText);

            // 2. Convert route name to Godot scene path
            string sceneName = route.Replace("Page", "Scene");
            string path = $"res://scenes/{sceneName}.tscn";

            // 3. Load and instantiate the new scene
            var packedScene = GD.Load<PackedScene>(path);
            if (packedScene == null)
            {
                GD.PrintErr($"[Navigation] Failed to load scene {path}");
                await _global.Transition.FadeIn();
                return;
            }

            var newScene = packedScene.Instantiate();

            // 4. Inject parameters BEFORE adding to the tree
            if (newScene is IInitializable initializable)
            {
                GD.Print($"[Navigation] Calling Initialize on {newScene.Name} with typed args");
                var dict = new Dictionary<string, object> { { "Args", parameters } };
                initializable.Initialize(dict);
            }
            else
            {
                GD.PrintErr($"[Navigation] ERROR: Scene {newScene.Name} does NOT implement IInitializable!");
            }

            // 5. Swap scenes
            var tree = _global.GetTree();
            var oldScene = tree.CurrentScene;
            if (oldScene != null)
            {
                tree.Root.RemoveChild(oldScene);
                oldScene.QueueFree();
            }
            
            tree.Root.AddChild(newScene);
            tree.CurrentScene = newScene;

            GD.Print($"[Navigation] Scene change completed. CurrentScene is now: {newScene.Name}");

            // Wait a frame for Godot to settle
            await _global.ToSignal(tree, "process_frame");

            // 6. Fade In
            await _global.Transition.FadeIn();
        }
        finally
        {
            _isNavigating = false;
            GD.Print($"[Navigation] Finished navigation to {route}. _isNavigating set to FALSE.");
        }
    }

    public Task GoBackAsync()
    {
        // Return to Hub
        return NavigateToAsync("MainMenuPage");
    }
}
