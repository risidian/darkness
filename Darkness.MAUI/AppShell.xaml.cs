using Darkness.MAUI.Pages;
using Darkness.Core.Interfaces;

namespace Darkness.MAUI;

public partial class AppShell : Shell
{
    private readonly ISessionService _sessionService;

	public AppShell(ISessionService sessionService)
	{
		InitializeComponent();
        _sessionService = sessionService;

		Routing.RegisterRoute(nameof(CharacterGenPage), typeof(CharacterGenPage));
		Routing.RegisterRoute(nameof(CreateUserPage), typeof(CreateUserPage));
		Routing.RegisterRoute(nameof(LoadUserPage), typeof(LoadUserPage));
		Routing.RegisterRoute(nameof(CharactersPage), typeof(CharactersPage));
		Routing.RegisterRoute(nameof(StudyPage), typeof(StudyPage));
		Routing.RegisterRoute(nameof(ForgePage), typeof(ForgePage));
		Routing.RegisterRoute(nameof(AlliesPage), typeof(AlliesPage));
		Routing.RegisterRoute(nameof(DeathmatchPage), typeof(DeathmatchPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
		Routing.RegisterRoute(nameof(BattlePage), typeof(BattlePage));
	}
}
