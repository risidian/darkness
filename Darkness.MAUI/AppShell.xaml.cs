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
	}
}
