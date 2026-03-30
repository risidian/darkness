using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class CharactersPage : ContentPage
    {
        private readonly CharactersViewModel _viewModel;

        public CharactersPage(CharactersViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCharactersAsync();
        }
    }
}
