using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages;

public partial class LoadUserPage : ContentPage
{
    public LoadUserPage(LoadUserViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
