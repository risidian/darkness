using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using Darkness.Core.Data;
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
        GD.Print("[Global] _Ready started.");
        try
        {
            var services = new ServiceCollection();

            // Infrastructure
            services.AddSingleton<IDispatcherService, GodotDispatcherService>();
            services.AddSingleton<IFileSystemService, GodotFileSystemService>();
            services.AddSingleton<IDialogService>(sp => new GodotDialogService(this));

            // Core Services
            services.AddSingleton<LocalDatabaseService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<ICraftingService, CraftingService>();
            services.AddSingleton<IDeathmatchService, DeathmatchService>();
            services.AddSingleton<IAllyService, AllyService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRewardService, RewardService>();
            services.AddSingleton<ICombatService, CombatEngine>();
            services.AddSingleton<ISpriteCompositor, GodotSpriteCompositor>();
            services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
            services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));
            services.AddSingleton<StoryController>();

            Services = services.BuildServiceProvider();
            GD.Print("[Global] DI Container initialized.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
}
