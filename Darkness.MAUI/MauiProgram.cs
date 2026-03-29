using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Core.Services;
using Darkness.Core.ViewModels;
using Darkness.MAUI.Services;
using Microsoft.Extensions.Logging;

namespace Darkness.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(typeof(Darkness.MAUI.Controls.MonoGameHost), typeof(Darkness.MAUI.Handlers.MonoGameHostHandler));
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Platform Services
		builder.Services.AddSingleton<IDispatcherService, MauiDispatcherService>();
		builder.Services.AddSingleton<IFileSystemService, MauiFileSystemService>();

		// Register Core Services
		builder.Services.AddSingleton<LocalDatabaseService>();
		builder.Services.AddSingleton<IUserService, UserService>();
		builder.Services.AddSingleton<ICharacterService, CharacterService>();
		builder.Services.AddSingleton<IRewardService, RewardService>();
		builder.Services.AddSingleton<ISessionService, SessionService>();
		builder.Services.AddSingleton<ICraftingService, CraftingService>();
		builder.Services.AddSingleton<IDeathmatchService, DeathmatchService>();
		builder.Services.AddSingleton<IAllyService, AllyService>();
		builder.Services.AddSingleton<ISettingsService, SettingsService>();
		builder.Services.AddSingleton<ICombatService, CombatEngine>();
		builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
		builder.Services.AddSingleton<IDialogService, MauiDialogService>();
		builder.Services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
		builder.Services.AddSingleton<ISpriteCompositor, SpriteCompositor>();
		builder.Services.AddSingleton<StoryController>();
		builder.Services.AddSingleton<Darkness.Game.DarknessGame>();

		// Register ViewModels
		builder.Services.AddTransient<LoadUserViewModel>();
		builder.Services.AddTransient<CharacterGenViewModel>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<CreateUserViewModel>();
		builder.Services.AddTransient<BattlePageViewModel>();
		builder.Services.AddTransient<GamePageViewModel>();
		builder.Services.AddTransient<CharactersViewModel>();
		builder.Services.AddTransient<StudyViewModel>();
		builder.Services.AddTransient<ForgeViewModel>();
		builder.Services.AddTransient<AlliesViewModel>();
		builder.Services.AddTransient<DeathmatchViewModel>();

		// Register Pages
		builder.Services.AddTransient<Pages.CreateUserPage>();
		builder.Services.AddTransient<Pages.LoadUserPage>();
		builder.Services.AddTransient<Pages.CharacterGenPage>();
		builder.Services.AddTransient<Pages.SettingsPage>();
		builder.Services.AddTransient<Pages.GamePage>();
		builder.Services.AddTransient<Pages.BattlePage>();
		builder.Services.AddTransient<Pages.CharactersPage>();
		builder.Services.AddTransient<Pages.StudyPage>();
		builder.Services.AddTransient<Pages.ForgePage>();
		builder.Services.AddTransient<Pages.AlliesPage>();
		builder.Services.AddTransient<Pages.DeathmatchPage>();
		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}

public class MauiFileSystemService : IFileSystemService
{
	public string AppDataDirectory => FileSystem.Current.AppDataDirectory;

	public Task<Stream> OpenAppPackageFileAsync(string filename)
	{
		return FileSystem.Current.OpenAppPackageFileAsync(filename);
	}
}
