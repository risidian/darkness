using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Darkness.MAUI.Controls;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, SwapChainPanel>
    {
        private Microsoft.Xna.Framework.Game? _game;

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
                // Drive the game loop
                try
                {
                    // MonoGame.Framework.Game has a Tick() method
                    var tickMethod = _game.GetType().GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    tickMethod?.Invoke(_game, null);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during Tick: {ex.Message}");
                }
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
                // Most MonoGame WinUI implementations have a way to set the panel.
                // We'll search for a property or field related to 'SwapChainPanel' on the GraphicsDeviceManager.
                var graphicsField = game.GetType().GetField("_graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?? game.GetType().GetField("graphics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                var graphics = graphicsField?.GetValue(game);
                
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
                
                if (PlatformView != null)
                {
                    ConnectGameToPanel(_game, PlatformView);
                }

                try
                {
                    // Call PrepareForPlatform to set up Content and GraphicsDevice
                    var prepareMethod = _game.GetType().GetMethod("PrepareForPlatform");
                    prepareMethod?.Invoke(_game, null);

                    // Manually trigger Initialize and LoadContent if they haven't been called
                    // This is necessary because we're not calling Run()
                    var initMethod = _game.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    initMethod?.Invoke(_game, null);

                    var loadMethod = _game.GetType().GetMethod("LoadContent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    loadMethod?.Invoke(_game, null);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Error during manual Game initialization: {ex.Message}");
                }
            }
        }
    }
}
