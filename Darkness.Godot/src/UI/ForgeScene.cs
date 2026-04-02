using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class ForgeScene : Control
{
	private ICraftingService _craftingService = null!;
	private ISessionService _session = null!;
	private ICharacterService _characterService = null!;
	private INavigationService _navigation = null!;
	private IDialogService _dialogService = null!;

	private ItemList _recipeList = null!;
	private Label _titleLabel = null!;
	private Label _descLabel = null!;
	private Label _materialsLabel = null!;
	private Label _statsLabel = null!;
	private Button _forgeButton = null!;

	private List<Recipe> _recipes = new();
	private Recipe? _selectedRecipe;
	private Character? _currentCharacter;

	public override async void _Ready()
	{
		if (!IsInsideTree()) return;
		var global = GetNode<Global>("/root/Global");
		_craftingService = global.Services!.GetRequiredService<ICraftingService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_characterService = global.Services!.GetRequiredService<ICharacterService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();
		_dialogService = global.Services!.GetRequiredService<IDialogService>();

		_recipeList = GetNode<ItemList>("HSplitContainer/RecipeList");
		_titleLabel = GetNode<Label>("HSplitContainer/DetailsArea/Title");
		_descLabel = GetNode<Label>("HSplitContainer/DetailsArea/Description");
		_materialsLabel = GetNode<Label>("HSplitContainer/DetailsArea/Materials");
		_statsLabel = GetNode<Label>("HSplitContainer/DetailsArea/Stats");
		_forgeButton = GetNode<Button>("HSplitContainer/DetailsArea/ForgeButton");

		_recipeList.ItemSelected += OnRecipeSelected;
		_forgeButton.Pressed += OnForgePressed;
		GetNode<Button>("HSplitContainer/DetailsArea/BackButton").Pressed += () => _navigation.GoBackAsync();

		await LoadData();
	}

	private async Task LoadData()
	{
		var recipes = await _craftingService.GetAvailableRecipesAsync();
		_recipes = new List<Recipe>(recipes);

		_recipeList.Clear();
		foreach (var recipe in _recipes)
		{
			_recipeList.AddItem(recipe.Name);
		}

		if (_session.CurrentUser != null)
		{
			var characters = await _characterService.GetCharactersForUserAsync(_session.CurrentUser.Id);
			if (characters != null && characters.Count > 0)
			{
				_currentCharacter = _session.CurrentCharacter ?? characters[0];
			}
		}
	}

	private void OnRecipeSelected(long index)
	{
		_selectedRecipe = _recipes[(int)index];
		_titleLabel.Text = _selectedRecipe.Name;
		_descLabel.Text = _selectedRecipe.Result.Description;
		_materialsLabel.Text = $"Type: {_selectedRecipe.Result.Type}";
		_statsLabel.Text = $"Attack Bonus: {_selectedRecipe.Result.AttackBonus}";
	}

	private async void OnForgePressed()
	{
		if (_selectedRecipe == null || _currentCharacter == null) return;

		var success = await _craftingService.CraftItemAsync(_currentCharacter, _selectedRecipe);
		if (success)
		{
			await _dialogService.DisplayAlertAsync("Success!", $"Successfully crafted {_selectedRecipe.Result.Name}!", "Excellent");
		}
		else
		{
			await _dialogService.DisplayAlertAsync("Forge Failed", "You don't have enough materials.", "OK");
		}
	}
}
