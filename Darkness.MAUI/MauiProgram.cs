using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Services;
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
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Platform Services
		builder.Services.AddSingleton<IFileSystemService, MauiFileSystemService>();

		// Register Core Services
		builder.Services.AddSingleton<LocalDatabaseService>();
		builder.Services.AddSingleton<IUserService, UserService>();
		builder.Services.AddSingleton<ICharacterService, CharacterService>();
		builder.Services.AddSingleton<IRewardService, RewardService>();

		// Register Pages
		builder.Services.AddTransient<Pages.CreateUserPage>();
		builder.Services.AddTransient<Pages.LoadUserPage>();
		builder.Services.AddTransient<Pages.CharacterGenPage>();
		builder.Services.AddTransient<Pages.GamePage>();
		builder.Services.AddTransient<Pages.BattlePage>();
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
