using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Darkness.MAUI.Controls;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, SwapChainPanel>
    {
        private object? _game;

        protected override void ConnectHandler(SwapChainPanel platformView)
        {
            base.ConnectHandler(platformView);
            platformView.SizeChanged += OnSizeChanged;
        }

        protected override void DisconnectHandler(SwapChainPanel platformView)
        {
            platformView.SizeChanged -= OnSizeChanged;
            base.DisconnectHandler(platformView);
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
        }

        private partial void UpdateGame(object game)
        {
            _game = game;
            if (_game != null)
            {
                System.Diagnostics.Debug.WriteLine("Game assigned to Windows MonoGameHostHandler");
            }
        }
    }
}
