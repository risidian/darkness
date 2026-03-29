using Microsoft.Maui.Handlers;
using Darkness.MAUI.Controls;

#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Widget.FrameLayout;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.SwapChainPanel;
#else
using PlatformView = System.Object;
#endif

namespace Darkness.MAUI.Handlers
{
    public partial class MonoGameHostHandler : ViewHandler<MonoGameHost, PlatformView>
    {
        public static IPropertyMapper<MonoGameHost, MonoGameHostHandler> Mapper = new PropertyMapper<MonoGameHost, MonoGameHostHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MonoGameHost.Game)] = MapGame,
#if WINDOWS
            [nameof(Microsoft.Maui.IView.Background)] = (handler, view) => { },
#endif
        };

        public MonoGameHostHandler() : base(Mapper)
        {
        }

        public static void MapGame(MonoGameHostHandler handler, MonoGameHost view)
        {
            handler.UpdateGame(view.Game);
        }

        protected override PlatformView CreatePlatformView()
        {
#if ANDROID
            return new Android.Widget.FrameLayout(Context);
#elif WINDOWS
            return new Microsoft.UI.Xaml.Controls.SwapChainPanel();
#else
            throw new System.NotImplementedException();
#endif
        }

        private partial void UpdateGame(object game);
    }
}
