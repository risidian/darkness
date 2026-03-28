using Darkness.MAUI.Pages;

namespace Darkness.MAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(CharacterGenPage), typeof(CharacterGenPage));
		Routing.RegisterRoute(nameof(CreateUserPage), typeof(CreateUserPage));
		Routing.RegisterRoute(nameof(LoadUserPage), typeof(LoadUserPage));
		Routing.RegisterRoute(nameof(CharactersPage), typeof(CharactersPage));
		Routing.RegisterRoute(nameof(StudyPage), typeof(StudyPage));
		Routing.RegisterRoute(nameof(ForgePage), typeof(ForgePage));
		Routing.RegisterRoute(nameof(AlliesPage), typeof(AlliesPage));
		Routing.RegisterRoute(nameof(DeathmatchPage), typeof(DeathmatchPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
