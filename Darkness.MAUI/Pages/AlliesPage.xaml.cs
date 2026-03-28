using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class AlliesPage : ContentPage
    {
        private readonly AlliesViewModel _viewModel;

        public AlliesPage(AlliesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.OnAppearingAsync();
        }
    }
}
