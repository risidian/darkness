using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Darkness.MAUI.Controls;
using Microsoft.Maui;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, SwapChainPanel>
    {
        private Microsoft.Xna.Framework.Game? _game;
        private bool _isInitialized;

        // COM interface for binding a DXGI swap chain to a WinUI3 SwapChainPanel
        [ComImport, Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISwapChainPanelNative
        {
            void SetSwapChain(IntPtr swapChain);
        }

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
                // Step 1: Let MonoGame initialize its GraphicsDevice normally.
                // This creates an internal D3D11 device and swap chain targeting a hidden WinForms window.
                var prepareMethod = game.GetType().GetMethod("PrepareForPlatform");
                prepareMethod?.Invoke(game, null);

                var initMethod = game.GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic);
                initMethod?.Invoke(game, null);

                var loadMethod = game.GetType().GetMethod("LoadContent", BindingFlags.Instance | BindingFlags.NonPublic);
                loadMethod?.Invoke(game, null);

                // Step 2: Bridge MonoGame's D3D11 device to the MAUI SwapChainPanel.
                if (game.GraphicsDevice != null)
                {
                    BridgeSwapChain(game.GraphicsDevice, panel);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] GraphicsDevice is null after initialization.");
                }

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Windows Game initialization complete.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] FATAL Error during Game initialization: {ex}");
            }
        }

        private void BridgeSwapChain(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, SwapChainPanel panel)
        {
            try
            {
                var gdType = graphicsDevice.GetType();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;

                // Get MonoGame's internal D3D11 device
                var d3dDeviceField = gdType.GetField("_d3dDevice", flags);
                var d3dDevice = d3dDeviceField?.GetValue(graphicsDevice);
                if (d3dDevice == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Could not access _d3dDevice.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Got D3D11 device from MonoGame.");

                // Get the DXGI device → adapter → factory for creating a composition swap chain
                var dxgiDeviceType = typeof(SharpDX.DXGI.Device1);
                var dxgiDevice = ((SharpDX.Direct3D11.Device)d3dDevice).QueryInterface<SharpDX.DXGI.Device1>();
                var dxgiAdapter = dxgiDevice.Adapter;
                var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>();

                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Got DXGI Factory2.");

                // Determine swap chain size
                int width = Math.Max(1, (int)panel.ActualWidth);
                int height = Math.Max(1, (int)panel.ActualHeight);

                var pp = graphicsDevice.PresentationParameters;
                if (pp.BackBufferWidth > 0) width = pp.BackBufferWidth;
                if (pp.BackBufferHeight > 0) height = pp.BackBufferHeight;

                // Create a new swap chain for composition (required for SwapChainPanel)
                var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1
                {
                    Width = width,
                    Height = height,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    BufferCount = 2,
                    SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                    Flags = SharpDX.DXGI.SwapChainFlags.None,
                    Scaling = SharpDX.DXGI.Scaling.Stretch,
                    AlphaMode = SharpDX.DXGI.AlphaMode.Ignore,
                };

                var newSwapChain = new SharpDX.DXGI.SwapChain1(
                    dxgiFactory,
                    (SharpDX.Direct3D11.Device)d3dDevice,
                    ref swapChainDesc);

                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Created composition swap chain ({width}x{height}).");

                // Bind the new swap chain to the WinUI3 SwapChainPanel via COM interop
                var panelNative = Marshal.GetComInterfaceForObject<SwapChainPanel, ISwapChainPanelNative>(panel);
                var native = Marshal.GetObjectForIUnknown(panelNative) as ISwapChainPanelNative;
                native?.SetSwapChain(newSwapChain.NativePointer);
                Marshal.Release(panelNative);

                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Bound swap chain to SwapChainPanel.");

                // Replace MonoGame's internal swap chain with our composition swap chain
                var swapChainField = gdType.GetField("_swapChain", flags);
                if (swapChainField != null)
                {
                    var oldSwapChain = swapChainField.GetValue(graphicsDevice) as IDisposable;
                    swapChainField.SetValue(graphicsDevice, newSwapChain);
                    oldSwapChain?.Dispose();
                    System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Replaced MonoGame swap chain.");
                }

                // Create a new render target view from the new swap chain's back buffer
                var renderTargetField = gdType.GetField("_renderTargetView", flags);
                SharpDX.Direct3D11.RenderTargetView? newRtv = null;
                if (renderTargetField != null)
                {
                    // Dispose old render target view
                    var oldRtv = renderTargetField.GetValue(graphicsDevice);
                    if (oldRtv is IDisposable disposableRtv)
                        disposableRtv.Dispose();

                    // Create new render target view from the new swap chain's back buffer
                    using var backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<SharpDX.Direct3D11.Texture2D>(newSwapChain, 0);
                    newRtv = new SharpDX.Direct3D11.RenderTargetView((SharpDX.Direct3D11.Device)d3dDevice, backBuffer);
                    renderTargetField.SetValue(graphicsDevice, newRtv);
                    System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Replaced render target view.");
                }

                // Bind the new render target to the D3D11 device context so draw calls go to it
                var d3dContextField = gdType.GetField("_d3dContext", flags);
                var d3dContext = d3dContextField?.GetValue(graphicsDevice) as SharpDX.Direct3D11.DeviceContext;
                if (d3dContext != null && newRtv != null)
                {
                    d3dContext.OutputMerger.SetRenderTargets(newRtv);
                    d3dContext.Rasterizer.SetViewport(0, 0, width, height);
                    System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] Bound new render target to D3D11 context.");
                }

                // Clean up DXGI objects (don't dispose device/adapter - MonoGame owns those)
                dxgiDevice.Dispose();

                System.Diagnostics.Debug.WriteLine("[MonoGameHostHandler] DXGI swap chain bridge complete.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonoGameHostHandler] Failed to bridge swap chain: {ex}");
            }
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (_game != null && _isInitialized)
            {
                // TODO: Resize the swap chain when the panel resizes
            }
        }

        private partial void UpdateGame(object game)
        {
            _game = game as Microsoft.Xna.Framework.Game;
            _isInitialized = false;

            if (PlatformView != null && _game != null)
            {
                InitializeGame(_game, PlatformView);
            }
        }
    }
}
