using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Maui.Controls;

namespace Darkness.MAUI.Pages
{
    public partial class CharacterGenPage : ContentPage
    {
        private readonly ICharacterService _characterService;
        private readonly IUserService _userService;

        public CharacterGenPage(ICharacterService characterService, IUserService userService)
        {
            InitializeComponent();
            _characterService = characterService;
            _userService = userService;
        }

        private async void OnCreateCharacterClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CharacterNameEntry.Text) || ClassPicker.SelectedIndex == -1)
            {
                await DisplayAlertAsync("Error", "Please enter a name and select a class.", "OK");
                return;
            }

            // Get the first user for now as a placeholder
            var users = await _userService.GetAllUsersAsync();
            if (users.Count == 0)
            {
                await DisplayAlertAsync("Error", "No user found. Please create an account first.", "OK");
                await Shell.Current.GoToAsync("//CreateUserPage");
                return;
            }

            var userId = users[0].Id;
            var selectedClass = ClassPicker.SelectedItem.ToString();

            var character = new Character
            {
                UserId = userId,
                Name = CharacterNameEntry.Text,
                Class = selectedClass ?? "Warrior",
                HairColor = HairColorPicker.SelectedItem?.ToString() ?? "Black",
                SkinColor = SkinColorPicker.SelectedItem?.ToString() ?? "Light",
                Level = 1,
                Experience = 0
            };

            // Set base stats based on class
            SetBaseStats(character, selectedClass);

            bool success = await _characterService.SaveCharacterAsync(character);

            if (success)
            {
                await DisplayAlertAsync("Success", "Character Created!", "OK");
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to save character.", "OK");
            }
        }

        private void SetBaseStats(Character character, string? className)
        {
            switch (className)
            {
                case "Warrior":
                    character.STR = 15; character.DEX = 10; character.CON = 15;
                    character.INT = 5; character.WIS = 8; character.CHA = 10;
                    break;
                case "Mage":
                    character.STR = 5; character.DEX = 10; character.CON = 8;
                    character.INT = 18; character.WIS = 15; character.CHA = 10;
                    break;
                case "Rogue":
                    character.STR = 10; character.DEX = 18; character.CON = 10;
                    character.INT = 10; character.WIS = 8; character.CHA = 12;
                    break;
                default:
                    character.STR = 10; character.DEX = 10; character.CON = 10;
                    character.INT = 10; character.WIS = 10; character.CHA = 10;
                    break;
            }

            // Initialize derived stats
            character.MaxHP = character.CON * 10;
            character.CurrentHP = character.MaxHP;
            character.Mana = character.WIS * 5;
            character.Stamina = character.CON * 5;
            character.Speed = character.DEX;
            character.Accuracy = 80 + character.DEX / 2;
            character.Evasion = character.DEX / 2;
            character.Defense = character.CON / 2;
            character.MagicDefense = character.WIS / 2;
        }
    }
}
