using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class ForgePage : ContentPage
    {
        private readonly ForgeViewModel _viewModel;

        public ForgePage(ForgeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }
    }
}
