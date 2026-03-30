using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class CharacterGenPage : ContentPage
    {
        private readonly CharacterGenViewModel _viewModel;

        public CharacterGenPage(CharacterGenViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.UpdatePreviewAsync();
        }
    }
}
