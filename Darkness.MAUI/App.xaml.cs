using Darkness.Core.Interfaces;

namespace Darkness.MAUI;

public partial class App : Application
{
    private readonly ISessionService _sessionService;

	public App(ISessionService sessionService)
	{
		InitializeComponent();

        _sessionService = sessionService;
		MainPage = new AppShell();
	}

    protected override async void OnStart()
    {
        base.OnStart();
        await _sessionService.InitializeAsync();
    }
}
