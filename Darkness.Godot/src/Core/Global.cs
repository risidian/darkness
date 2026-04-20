using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using LiteDB;
using Darkness.Core.Data;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Godot.Services;
using Darkness.Godot.UI;

namespace Darkness.Godot.Core;

public partial class Global : Node
{
    public IServiceProvider? Services { get; private set; }
    public Task SeedingTask { get; private set; } = Task.CompletedTask;
    public TransitionLayer Transition { get; private set; } = null!;

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
            services.AddSingleton<ILiteDatabase>(sp =>
            {
                var dbService = sp.GetRequiredService<LocalDatabaseService>();
                return dbService.OpenDatabase();
            });
            services.AddSingleton<LiteDatabase>(sp => (LiteDatabase)sp.GetRequiredService<ILiteDatabase>());
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<ICraftingService>(sp => new CraftingService(sp.GetRequiredService<ILiteDatabase>()));
            services.AddSingleton<IDeathmatchService, DeathmatchService>();
            services.AddSingleton<IAllyService, AllyService>();
            services.AddSingleton<IQuestService, QuestService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRewardService, RewardService>();
            services.AddSingleton<ICombatService, CombatEngine>();
            services.AddSingleton<ISpriteCompositor, SkiaSharpSpriteCompositor>();
            services.AddSingleton<ISheetDefinitionCatalog, SheetDefinitionCatalog>();
            services.AddSingleton<IWeaponSkillService, WeaponSkillService>();
            services.AddSingleton<ILevelingService, LevelingService>();
            services.AddSingleton<ITalentService, TalentService>();
            services.AddSingleton<ITriggerService, TriggerService>();
            services.AddSingleton<IEquipmentService, EquipmentService>();
            services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));

            Services = services.BuildServiceProvider();
            GD.Print("[Global] DI Container initialized.");

            // Initialize Transition Overlay
            Transition = new TransitionLayer();
            AddChild(Transition);

            // Seed data synchronously to ensure it's available before scenes load
            try 
            {
                var db = Services.GetRequiredService<ILiteDatabase>();
                var fs = Services.GetRequiredService<IFileSystemService>();

                GD.Print("[Global] Seeding data...");
                new SheetDefinitionSeeder(fs).Seed(db);
                new AppearanceSeeder(fs).Seed(db);
                new QuestSeeder(fs).Seed(db);
                new LevelSeeder(fs).Seed(db);
                new TalentSeeder(fs).Seed(db);
                new SkillSeeder(fs).Seed(db);
                new RecipeSeeder(fs).Seed(db);
                new ItemSeeder(fs).Seed(db);
                new RewardSeeder(fs).Seed(db);
                GD.Print("[Global] Data seeding complete.");

                // Create runtime indexes (once at startup, not per operation)
                db.GetCollection<Character>("characters").EnsureIndex(c => c.UserId);
                db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.CharacterId);
                db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.Status);
            }
catch (Exception ex)
{
    GD.PrintErr($"[Global] Data seeding failed: {ex.Message}");
}
}
        catch (Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
}