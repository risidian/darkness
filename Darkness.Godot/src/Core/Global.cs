using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Godot.Services;

namespace Darkness.Godot.Core;

public partial class Global : Node
{
    public IServiceProvider? Services { get; private set; }

    public override void _Ready()
    {
        var services = new ServiceCollection();
        
        // Infrastructure
        services.AddSingleton<IDispatcherService, GodotDispatcherService>();
        services.AddSingleton<IFileSystemService, GodotFileSystemService>();
        
        // Core Services
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<ICharacterService, CharacterService>();
        services.AddSingleton<ICombatService, CombatEngine>();
        services.AddSingleton<ISpriteCompositor, SpriteCompositor>();
        services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
        services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));
        services.AddSingleton<StoryController>();
        
        Services = services.BuildServiceProvider();
    }
}
