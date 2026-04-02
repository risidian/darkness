using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using Darkness.Core.Data;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Godot.Services;
using System.Runtime.InteropServices;

namespace Darkness.Godot.Core;

public partial class Global : Node
{
    public IServiceProvider? Services { get; private set; }

    [DllImport("libdl.so", EntryPoint = "dlopen")]
    private static extern IntPtr DlOpen(string? filename, int flags);

    [DllImport("libdl.so", EntryPoint = "dlerror")]
    private static extern IntPtr DlError();

    private static string GetDlErrorStr()
    {
        var ptr = DlError();
        return ptr == IntPtr.Zero ? "(no error)" : Marshal.PtrToStringAnsi(ptr) ?? "(null)";
    }

    static Global()
    {
        var providerAssembly = typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly;
        NativeLibrary.SetDllImportResolver(providerAssembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName != "e_sqlite3")
                return IntPtr.Zero;

            // Try standard loading
            string[] names = { "libe_sqlite3.so", "e_sqlite3" };
            foreach (var name in names)
            {
                if (NativeLibrary.TryLoad(name, assembly, searchPath, out IntPtr h))
                    return h;
            }

            // Try dlopen directly and report the exact error
            const int RTLD_LAZY = 0x0001;
            try
            {
                var h = DlOpen("libe_sqlite3.so", RTLD_LAZY);
                if (h != IntPtr.Zero) return h;
                GD.PrintErr($"[Global] dlopen('libe_sqlite3.so'): {GetDlErrorStr()}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Global] dlopen exception: {ex.Message}");
            }

            GD.PrintErr("[Global] Failed to load e_sqlite3 native library.");
            return IntPtr.Zero;
        });
    }

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
            services.AddSingleton<ISpriteCompositor, SpriteCompositor>();
            services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
            services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));
            services.AddSingleton<StoryController>();
            
            Services = services.BuildServiceProvider();
            GD.Print("[Global] DI Container initialized.");

            // Initialize SQLite
            try
            {
                GD.Print("[Global] Initializing SQLite batteries...");
                SQLitePCL.Batteries_V2.Init();
                GD.Print("[Global] SQLite initialized successfully.");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Global] SQLite initialization failed: {ex.Message}");
                GD.PrintErr($"[Global] Stack trace: {ex.StackTrace}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
}
