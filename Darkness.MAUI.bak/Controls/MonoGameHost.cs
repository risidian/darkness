using Microsoft.Maui.Controls;

namespace Darkness.MAUI.Controls
{
    public class MonoGameHost : View
    {
        public static readonly BindableProperty GameProperty =
            BindableProperty.Create(nameof(Game), typeof(object), typeof(MonoGameHost), null);

        public object Game
        {
            get => GetValue(GameProperty);
            set => SetValue(GameProperty, value);
        }
    }
}
