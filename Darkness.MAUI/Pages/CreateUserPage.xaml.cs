using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages;

public partial class CreateUserPage : ContentPage
{
    public CreateUserPage(CreateUserViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
