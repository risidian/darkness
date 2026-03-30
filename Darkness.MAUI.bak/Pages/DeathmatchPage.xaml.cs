using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class DeathmatchPage : ContentPage
    {
        private readonly DeathmatchViewModel _viewModel;

        public DeathmatchPage(DeathmatchViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadEncountersAsync();
        }
    }
}
