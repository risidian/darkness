using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Darkness.Core.ViewModels
{
    public partial class ForgeViewModel : ViewModelBase
    {
        private readonly ICraftingService _craftingService;
        private readonly ISessionService _sessionService;
        private readonly ICharacterService _characterService;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty] private Recipe? _selectedRecipe;

        [ObservableProperty] private Character? _currentCharacter;

        public ForgeViewModel(
            ICraftingService craftingService,
            ISessionService sessionService,
            ICharacterService characterService,
            IDialogService dialogService)
        {
            _craftingService = craftingService;
            _sessionService = sessionService;
            _characterService = characterService;
            _dialogService = dialogService;
        }

        public async Task LoadDataAsync()
        {
            var recipes = await _craftingService.GetAvailableRecipesAsync();
            Recipes = new ObservableCollection<Recipe>(recipes);

            if (_sessionService.CurrentUser != null)
            {
                var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
                if (characters != null && characters.Count > 0)
                {
                    CurrentCharacter = characters[0]; // Just take the first character for now
                }
            }
        }

        [RelayCommand]
        public async Task CraftAsync()
        {
            if (SelectedRecipe == null)
            {
                await _dialogService.DisplayAlertAsync("Forge", "Please select a recipe first.", "OK");
                return;
            }

            if (CurrentCharacter == null)
            {
                await _dialogService.DisplayAlertAsync("Forge", "No character found.", "OK");
                return;
            }

            var success = await _craftingService.CraftItemAsync(CurrentCharacter, SelectedRecipe);
            if (success)
            {
                await _dialogService.DisplayAlertAsync("Success!",
                    $"Successfully crafted {SelectedRecipe.Result.Name}!", "Excellent");
            }
            else
            {
                await _dialogService.DisplayAlertAsync("Forge Failed", "You don't have enough materials.", "OK");
            }
        }
    }
}