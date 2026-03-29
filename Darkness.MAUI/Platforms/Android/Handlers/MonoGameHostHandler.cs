using Microsoft.Maui.Handlers;
using Darkness.MAUI.Controls;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, Android.Widget.FrameLayout>
    {
        private Microsoft.Xna.Framework.Game? _game;
        private bool _isInitialized;
        private bool _isDisposed;

        protected override void ConnectHandler(Android.Widget.FrameLayout platformView)
        {
            base.ConnectHandler(platformView);
            _isDisposed = false;
            StartRenderingLoop();
        }

        protected override void DisconnectHandler(Android.Widget.FrameLayout platformView)
        {
            _isDisposed = true;
            base.DisconnectHandler(platformView);
        }

        private async void StartRenderingLoop()
        {
            while (!_isDisposed)
            {
                if (_game != null && PlatformView?.Visibility == Android.Views.ViewStates.Visible)
                {
                    if (!_isInitialized)
                    {
                        _isInitialized = true;
                        InitializeGame(_game);
                    }

                    try
                    {
                        var tickMethod = _game.GetType().GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        tickMethod?.Invoke(_game, null);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during Android Tick: {ex.Message}");
                    }
                }
                await System.Threading.Tasks.Task.Delay(16); // ~60 FPS
            }
        }

        private void InitializeGame(Microsoft.Xna.Framework.Game game)
        {
            System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Android Game initialization starting...");
            try
            {
                // Ensure Game Activity is set if needed (using reflection to be safe across versions)
                var activityProp = typeof(Microsoft.Xna.Framework.Game).GetProperty("Activity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (activityProp != null && activityProp.GetValue(null) == null)
                {
                    activityProp.SetValue(null, Microsoft.Maui.ApplicationModel.Platform.CurrentActivity);
                }

                // Call PrepareForPlatform to set up Content and GraphicsDevice
                var prepareMethod = game.GetType().GetMethod("PrepareForPlatform");
                prepareMethod?.Invoke(game, null);

                // Try to get the Android view
                Android.Views.View? gameView = null;
                if (game.Services != null)
                {
                    gameView = game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View;
                }

                if (gameView == null && game.Window != null)
                {
                    var viewProp = game.Window.GetType().GetProperty("GameView") ?? game.Window.GetType().GetProperty("View");
                    gameView = viewProp?.GetValue(game.Window) as Android.Views.View;
                }

                if (gameView != null && gameView.Parent == null)
                {
                    PlatformView.AddView(gameView);
                }

                // Manually trigger Initialize and LoadContent
                var initMethod = game.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                initMethod?.Invoke(game, null);

                var loadMethod = game.GetType().GetMethod("LoadContent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                loadMethod?.Invoke(game, null);

                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Android Game initialization complete.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during Android Game initialization: {ex.Message}");
            }
        }

        private partial void UpdateGame(object game)
        {
            _game = game as Microsoft.Xna.Framework.Game;
            if (_game != null)
            {
                System.Diagnostics.Debug.WriteLine("Game assigned to Android MonoGameHostHandler");
                _isInitialized = false;
            }
        }
    }
}
