using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using LiteDB;
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
            services.AddSingleton<LiteDatabase>(sp =>
            {
                var dbService = sp.GetRequiredService<LocalDatabaseService>();
                return dbService.OpenDatabase();
            });
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<ICraftingService, CraftingService>();
            services.AddSingleton<IDeathmatchService, DeathmatchService>();
            services.AddSingleton<IAllyService, AllyService>();
            services.AddSingleton<IQuestService, QuestService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRewardService, RewardService>();
            services.AddSingleton<ICombatService, CombatEngine>();
            services.AddSingleton<ISpriteCompositor, GodotSpriteCompositor>();
            services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
            services.AddSingleton<IWeaponSkillService, WeaponSkillService>();
            services.AddSingleton<ILevelingService, LevelingService>();
            services.AddSingleton<ITriggerService, TriggerService>();
            services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));

            Services = services.BuildServiceProvider();
            GD.Print("[Global] DI Container initialized.");

            // Seed data
            var db = Services.GetRequiredService<LiteDatabase>();
            var fs = Services.GetRequiredService<IFileSystemService>();
            new SpriteSeeder(fs).Seed(db);
            new QuestSeeder(fs).Seed(db);
            new LevelSeeder(fs).Seed(db);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
}