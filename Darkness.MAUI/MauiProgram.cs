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
