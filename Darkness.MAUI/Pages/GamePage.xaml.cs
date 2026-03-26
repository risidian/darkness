using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class GamePage : ContentPage
    {
        public GamePage(GamePageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
