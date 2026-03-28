using Darkness.Core.ViewModels;
using Darkness.Core.Models;

namespace Darkness.MAUI.Pages
{
    [QueryProperty(nameof(Character), "Character")]
    public partial class StudyPage : ContentPage
    {
        private StudyViewModel _viewModel;
        
        public Character Character { get; set; }

        public StudyPage(StudyViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Character != null)
            {
                _viewModel.SetCharacter(Character);
            }
        }
    }
}
