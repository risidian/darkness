using Microsoft.Maui.Controls;

namespace Darkness.MAUI.Pages
{
    public partial class GamePage : ContentPage
    {
        public GamePage()
        {
            InitializeComponent();
        }

        private async void OnCombatTestClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///BattlePage");
        }
    }
}
