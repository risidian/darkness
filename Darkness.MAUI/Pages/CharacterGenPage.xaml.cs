using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class CharacterGenPage : ContentPage
    {
        public CharacterGenPage(CharacterGenViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
