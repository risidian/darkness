using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Darkness.MAUI.Controls;
using Microsoft.Maui;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, SwapChainPanel>
    {
        private Microsoft.Xna.Framework.Game? _game;
        private bool _isInitialized;

        protected override void ConnectHandler(SwapChainPanel platformView)
        {
            base.ConnectHandler(platformView);
            platformView.SizeChanged += OnSizeChanged;
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;

            if (_game != null)
            {
                InitializeGame(_game, platformView);
            }
        }

        protected override void DisconnectHandler(SwapChainPanel platformView)
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
            platformView.SizeChanged -= OnSizeChanged;
            base.DisconnectHandler(platformView);
        }

        private void OnRendering(object? sender, object e)
        {
            if (_game != null && _isInitialized && PlatformView?.Visibility == Microsoft.UI.Xaml.Visibility.Visible)
            {
                try
                {
                    if (_game is Darkness.Game.DarknessGame darknessGame)
                    {
                        darknessGame.Tick();
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during Tick: {ex.Message}");
                }
            }
        }

        private void InitializeGame(Microsoft.Xna.Framework.Game game, SwapChainPanel panel)
        {
            if (_isInitialized) return;

            System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Initializing Game for Windows (WinUI 3)...");
            
            try
            {
                // 1. Set the SwapChainPanel on the GraphicsDeviceManager via Reflection
                // This is the CRITICAL STEP to prevent MonoGame from creating a new window.
                var graphicsField = game.GetType().GetField("_graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?? game.GetType().GetField("graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                var graphics = graphicsField?.GetValue(game);
                if (graphics != null)
                {
                    // Many modern MonoGame forks/variants have a SwapChainPanel property for WinUI
                    var panelProp = graphics.GetType().GetProperty("SwapChainPanel") ?? graphics.GetType().GetProperty("Panel");
                    if (panelProp != null)
                    {
                        panelProp.SetValue(graphics, panel);
                        System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Successfully bound SwapChainPanel to GraphicsDeviceManager.");
                    }
                }

                // 2. Call PrepareForPlatform to set up Content and GraphicsDevice
                var prepareMethod = game.GetType().GetMethod("PrepareForPlatform");
                prepareMethod?.Invoke(game, null);

                // 3. Manually trigger Initialize and LoadContent
                var initMethod = game.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                initMethod?.Invoke(game, null);

                var loadMethod = game.GetType().GetMethod("LoadContent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                loadMethod?.Invoke(game, null);
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Windows Game initialization complete.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] FATAL Error during Game initialization: {ex}");
            }
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (_game != null && _isInitialized)
            {
                // Update resolution if needed
            }
        }

        private partial void UpdateGame(object game)
        {
            _game = game as Microsoft.Xna.Framework.Game;
            _isInitialized = false; // Reset initialization state for new game instance
        }
    }
}
