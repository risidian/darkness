using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Darkness.MAUI.Controls;

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

            // Try to connect the SwapChainPanel to the game's graphics device
            if (_game != null)
            {
                ConnectGameToPanel(_game, platformView);
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
            if (_game != null)
            {
                // Defer initialization until we have a rendering frame
                if (!_isInitialized && PlatformView != null)
                {
                    _isInitialized = true;
                    InitializeGame(_game, PlatformView);
                }

                // Drive the game loop
                try
                {
                    if (_game is Darkness.Game.DarknessGame darknessGame)
                    {
                        darknessGame.Tick();
                    }
                    else
                    {
                        // Fallback for other game types
                        var tickMethod = _game.GetType().GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        tickMethod?.Invoke(_game, null);
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
            System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Deferred Game initialization starting...");
            
            try
            {
                ConnectGameToPanel(game, panel);

                // Call PrepareForPlatform to set up Content and GraphicsDevice
                var prepareMethod = game.GetType().GetMethod("PrepareForPlatform");
                prepareMethod?.Invoke(game, null);

                // Manually trigger Initialize and LoadContent if they haven't been called
                // This is necessary because we're not calling Run()
                var initMethod = game.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                initMethod?.Invoke(game, null);

                var loadMethod = game.GetType().GetMethod("LoadContent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                loadMethod?.Invoke(game, null);
                
                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Deferred Game initialization complete.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during manual Game initialization: {ex.Message}");
            }
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (_game != null)
            {
                // Optionally update viewport size if game supports it
            }
        }

        private void ConnectGameToPanel(Microsoft.Xna.Framework.Game? game, SwapChainPanel? panel)
        {
            if (game == null || panel == null) return;

            try
            {
                // Try to get the GraphicsDeviceManager
                var graphics = (game as Darkness.Game.DarknessGame)?.GraphicsManager;
                
                // Fallback to Reflection if not a DarknessGame
                if (graphics == null)
                {
                    var graphicsField = game.GetType().GetField("_graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                        ?? game.GetType().GetField("graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    graphics = graphicsField?.GetValue(game) as Microsoft.Xna.Framework.GraphicsDeviceManager;
                }
                
                if (graphics != null)
                {
                    // Look for a property like 'SwapChainPanel' or 'Panel'
                    var panelProperty = graphics.GetType().GetProperty("SwapChainPanel") ?? graphics.GetType().GetProperty("Panel");
                    panelProperty?.SetValue(graphics, panel);
                    
                    // Some versions use a service
                    if (game.Services != null)
                    {
                        try { game.Services.AddService(typeof(SwapChainPanel), panel); } catch { }
                    }
                    
                    // Force the back buffer size to match the panel
                    graphics.PreferredBackBufferWidth = (int)panel.ActualWidth;
                    graphics.PreferredBackBufferHeight = (int)panel.ActualHeight;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error connecting game to panel: {ex.Message}");
            }
        }

        private partial void UpdateGame(object game)
        {
            _game = game as Microsoft.Xna.Framework.Game;
            if (_game != null)
            {
                System.Diagnostics.Debug.WriteLine("Game assigned to Windows MonoGameHostHandler");
                _isInitialized = false; // Reset initialization state for new game
            }
        }
    }
}
