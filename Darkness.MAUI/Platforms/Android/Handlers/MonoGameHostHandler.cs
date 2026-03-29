using Microsoft.Maui.Handlers;
using Darkness.MAUI.Controls;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, Android.Views.View>
    {
        private object? _game;

        protected override void ConnectHandler(Android.Views.View platformView)
        {
            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(Android.Views.View platformView)
        {
            base.DisconnectHandler(platformView);
        }

        private partial void UpdateGame(object game)
        {
            _game = game;
            if (_game != null)
            {
                System.Diagnostics.Debug.WriteLine("Game assigned to Android MonoGameHostHandler");
            }
        }
    }
}
