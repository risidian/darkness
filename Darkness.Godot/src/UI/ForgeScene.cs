using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Godot.UI;

public partial class ForgeScene : Control
{
    private ICraftingService _craftingService = null!;
    private ISessionService _session = null!;
    private ICharacterService _characterService = null!;
    private INavigationService _navigation = null!;
    private IDialogService _dialogService = null!;

    // Craft Tab
    private ItemList _recipeList = null!;
    private Label _craftItemName = null!;
    private Label _craftDesc = null!;
    private Label _craftMaterials = null!;
    private Button _craftButton = null!;

    // Upgrade Tab
    private ItemList _upgradeInventoryList = null!;
    private Label _upgradeItemName = null!;
    private Label _upgradeStats = null!;
    private Label _upgradeCost = null!;
    private Button _upgradeButton = null!;

    // Infuse Tab
    private ItemList _infuseEquipList = null!;
    private ItemList _infuseEssenceList = null!;
    private Label _infusionInfo = null!;
    private Button _infuseButton = null!;

    // Global
    private Label _goldLabel = null!;
    private Button _backButton = null!;

    private List<Recipe> _recipes = new();
    private List<Item> _upgradeableItems = new();
    private List<Item> _equipmentItems = new();
    private List<Item> _essenceItems = new();

    private Recipe? _selectedRecipe;
    private Item? _selectedUpgradeItem;
    private Item? _selectedInfuseEquip;
    private Item? _selectedInfuseEssence;
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

        // Bind Nodes
        _goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/Header/GoldLabel");
        
        // Craft
        _recipeList = GetNode<ItemList>("MarginContainer/VBoxContainer/TabContainer/Craft/RecipeList");
        _craftItemName = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Craft/DetailsArea/ItemName");
        _craftDesc = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Craft/DetailsArea/Description");
        _craftMaterials = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Craft/DetailsArea/Materials");
        _craftButton = GetNode<Button>("MarginContainer/VBoxContainer/TabContainer/Craft/DetailsArea/CraftButton");

        // Upgrade
        _upgradeInventoryList = GetNode<ItemList>("MarginContainer/VBoxContainer/TabContainer/Upgrade/InventoryList");
        _upgradeItemName = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Upgrade/DetailsArea/ItemName");
        _upgradeStats = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Upgrade/DetailsArea/Stats");
        _upgradeCost = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Upgrade/DetailsArea/Cost");
        _upgradeButton = GetNode<Button>("MarginContainer/VBoxContainer/TabContainer/Upgrade/DetailsArea/UpgradeButton");

        // Infuse
        _infuseEquipList = GetNode<ItemList>("MarginContainer/VBoxContainer/TabContainer/Infuse/VBoxContainer/EquipList");
        _infuseEssenceList = GetNode<ItemList>("MarginContainer/VBoxContainer/TabContainer/Infuse/VBoxContainer/EssenceList");
        _infusionInfo = GetNode<Label>("MarginContainer/VBoxContainer/TabContainer/Infuse/DetailsArea/InfusionInfo");
        _infuseButton = GetNode<Button>("MarginContainer/VBoxContainer/TabContainer/Infuse/DetailsArea/InfuseButton");

        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/Footer/BackButton");

        // Events
        _recipeList.ItemSelected += OnRecipeSelected;
        _craftButton.Pressed += OnCraftPressed;

        _upgradeInventoryList.ItemSelected += OnUpgradeItemSelected;
        _upgradeButton.Pressed += OnUpgradePressed;

        _infuseEquipList.ItemSelected += OnInfuseEquipSelected;
        _infuseEssenceList.ItemSelected += OnInfuseEssenceSelected;
        _infuseButton.Pressed += OnInfusePressed;

        _backButton.Pressed += async () => await _navigation.GoBackAsync();

        await LoadData();
    }

    private async Task LoadData()
    {
        _currentCharacter = _session.CurrentCharacter;
        if (_currentCharacter == null) return;

        _goldLabel.Text = $"Gold: {_currentCharacter.Gold}";

        // Load Recipes
        var recipes = await _craftingService.GetAvailableRecipesAsync();
        _recipes = new List<Recipe>(recipes);
        _recipeList.Clear();
        foreach (var r in _recipes) _recipeList.AddItem(r.Name);

        // Load Upgradeable Items
        _upgradeableItems = _currentCharacter.Inventory
            .Where(i => i.Type == "Weapon" || i.Type == "Armor")
            .ToList();
        _upgradeInventoryList.Clear();
        foreach (var i in _upgradeableItems) 
            _upgradeInventoryList.AddItem($"{i.Name} (Tier {i.Tier})");

        // Load Infusion Items
        _equipmentItems = _currentCharacter.Inventory
            .Where(i => i.Type == "Weapon" || i.Type == "Armor")
            .ToList();
        _infuseEquipList.Clear();
        foreach (var i in _equipmentItems) 
            _infuseEquipList.AddItem($"{i.Name}{(string.IsNullOrEmpty(i.Infusion) ? "" : " [" + i.Infusion + "]")}");

        _essenceItems = _currentCharacter.Inventory
            .Where(i => i.Name.Contains("Essence"))
            .ToList();
        _infuseEssenceList.Clear();
        foreach (var i in _essenceItems) 
            _infuseEssenceList.AddItem(i.Name);

        ResetDetails();
    }

    private void ResetDetails()
    {
        _selectedRecipe = null;
        _selectedUpgradeItem = null;
        _selectedInfuseEquip = null;
        _selectedInfuseEssence = null;

        _craftItemName.Text = "SELECT A RECIPE";
        _craftDesc.Text = "";
        _craftMaterials.Text = "";

        _upgradeItemName.Text = "SELECT AN ITEM";
        _upgradeStats.Text = "";
        _upgradeCost.Text = "";

        _infusionInfo.Text = "Select an item and an essence to infuse.";
    }

    private void OnRecipeSelected(long index)
    {
        _selectedRecipe = _recipes[(int)index];
        _craftItemName.Text = _selectedRecipe.Name;
        _craftDesc.Text = _selectedRecipe.Result.Description;
        
        var mats = string.Join(", ", _selectedRecipe.Materials.Select(m => $"{m.Value}x {m.Key}"));
        _craftMaterials.Text = $"Materials Required: {mats}";
    }

    private async void OnCraftPressed()
    {
        if (_selectedRecipe == null || _currentCharacter == null) return;

        var success = await _craftingService.CraftItemAsync(_currentCharacter, _selectedRecipe);
        if (success)
        {
            await _characterService.SaveCharacterAsync(_currentCharacter);
            await _dialogService.DisplayAlertAsync("Success", $"Crafted {_selectedRecipe.Name}!", "OK");
            await LoadData();
        }
        else
        {
            await _dialogService.DisplayAlertAsync("Failed", "Missing materials.", "OK");
        }
    }

    private void OnUpgradeItemSelected(long index)
    {
        _selectedUpgradeItem = _upgradeableItems[(int)index];
        _upgradeItemName.Text = $"{_selectedUpgradeItem.Name} (Tier {_selectedUpgradeItem.Tier})";
        
        var statText = _selectedUpgradeItem.Type == "Weapon" 
            ? $"Attack Bonus: {_selectedUpgradeItem.AttackBonus}" 
            : $"Defense Bonus: {_selectedUpgradeItem.DefenseBonus}";
        _upgradeStats.Text = $"Current Stats: {statText}";
        
        var cost = 500 * (_selectedUpgradeItem.Tier + 1);
        _upgradeCost.Text = $"Upgrade Cost: {cost} Gold";
    }

    private async void OnUpgradePressed()
    {
        if (_selectedUpgradeItem == null || _currentCharacter == null) return;

        var cost = 500 * (_selectedUpgradeItem.Tier + 1);
        var success = await _craftingService.UpgradeItemAsync(_currentCharacter, _selectedUpgradeItem, new List<Item>(), cost);
        
        if (success)
        {
            await _characterService.SaveCharacterAsync(_currentCharacter);
            await _dialogService.DisplayAlertAsync("Success", $"Upgraded {_selectedUpgradeItem.Name} to Tier {_selectedUpgradeItem.Tier}!", "OK");
            await LoadData();
        }
        else
        {
            await _dialogService.DisplayAlertAsync("Failed", "Not enough gold.", "OK");
        }
    }

    private void OnInfuseEquipSelected(long index)
    {
        _selectedInfuseEquip = _equipmentItems[(int)index];
        UpdateInfusionInfo();
    }

    private void OnInfuseEssenceSelected(long index)
    {
        _selectedInfuseEssence = _essenceItems[(int)index];
        UpdateInfusionInfo();
    }

    private void UpdateInfusionInfo()
    {
        if (_selectedInfuseEquip != null && _selectedInfuseEssence != null)
        {
            _infusionInfo.Text = $"Infusing {_selectedInfuseEquip.Name} with {_selectedInfuseEssence.Name}.\n\nThis will apply a permanent elemental effect.";
        }
        else if (_selectedInfuseEquip != null)
        {
            _infusionInfo.Text = $"Selected: {_selectedInfuseEquip.Name}\n\nNow select an essence.";
        }
        else if (_selectedInfuseEssence != null)
        {
            _infusionInfo.Text = $"Selected: {_selectedInfuseEssence.Name}\n\nNow select an item to infuse.";
        }
    }

    private async void OnInfusePressed()
    {
        if (_selectedInfuseEquip == null || _selectedInfuseEssence == null || _currentCharacter == null)
        {
            await _dialogService.DisplayAlertAsync("Selection Required", "Please select both an item and an essence.", "OK");
            return;
        }

        var success = await _craftingService.InfuseItemAsync(_currentCharacter, _selectedInfuseEquip, _selectedInfuseEssence);
        if (success)
        {
            await _characterService.SaveCharacterAsync(_currentCharacter);
            await _dialogService.DisplayAlertAsync("Success", $"Infused {_selectedInfuseEquip.Name} with {_selectedInfuseEquip.Infusion}!", "OK");
            await LoadData();
        }
        else
        {
            await _dialogService.DisplayAlertAsync("Failed", "Infusion failed.", "OK");
        }
    }
}
