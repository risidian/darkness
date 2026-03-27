using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class CharacterGenViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISpriteLayerCatalog _catalog;
        private readonly ISpriteCompositor _compositor;
        private readonly IFileSystemService _fileSystem;

        [ObservableProperty]
        private string _characterName = string.Empty;

        [ObservableProperty]
        private string _selectedClass = "Warrior";

        [ObservableProperty]
        private string _selectedHairColor = "Black";

        [ObservableProperty]
        private string _selectedSkinColor = "Light";

        [ObservableProperty]
        private string _selectedHairStyle = "Long";

        [ObservableProperty]
        private string _selectedArmor = "Plate (Steel)";

        [ObservableProperty]
        private string _selectedWeapon = "Arming Sword (Steel)";

        [ObservableProperty]
        private byte[]? _previewImageBytes;

        public List<string> Classes { get; } = new() { "Warrior", "Mage", "Rogue" };
        public List<string> HairColors => _catalog.HairColors;
        public List<string> SkinColors => _catalog.SkinColors;
        public List<string> HairStyles => _catalog.HairStyles;
        public List<string> ArmorTypes => _catalog.ArmorTypes;
        public List<string> WeaponTypes => _catalog.WeaponTypes;

        public CharacterGenViewModel(
            ICharacterService characterService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISpriteLayerCatalog catalog,
            ISpriteCompositor compositor,
            IFileSystemService fileSystem)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _catalog = catalog;
            _compositor = compositor;
            _fileSystem = fileSystem;
        }

        partial void OnSelectedClassChanged(string value)
        {
            var defaults = _catalog.GetDefaultAppearanceForClass(value);
            SelectedArmor = defaults.ArmorType;
            SelectedWeapon = defaults.WeaponType;
        }

        partial void OnSelectedHairColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedSkinColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedHairStyleChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedArmorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedWeaponChanged(string value) => UpdatePreviewAsync().FireAndForget();

        public async Task UpdatePreviewAsync()
        {
            try
            {
                var appearance = new CharacterAppearance
                {
                    SkinColor = SelectedSkinColor,
                    HairStyle = SelectedHairStyle,
                    HairColor = SelectedHairColor,
                    ArmorType = SelectedArmor,
                    WeaponType = SelectedWeapon,
                };

                var layerDefs = _catalog.GetLayersForAppearance(appearance);
                var streams = new List<Stream>();

                foreach (var layer in layerDefs)
                {
                    try
                    {
                        var stream = await _fileSystem.OpenAppPackageFileAsync(layer.ResourcePath);
                        streams.Add(stream);
                    }
                    catch
                    {
                        // Skip missing layer files gracefully
                    }
                }

                if (streams.Count > 0)
                {
                    // Walk animation sheets are 576x256 (9 frames x 4 directions of 64x64)
                    // South-facing idle = row 2 (Up=0, Left=1, Down=2, Right=3), col 0
                    var sheetBytes = _compositor.CompositeLayers(streams, 576, 256);
                    PreviewImageBytes = _compositor.ExtractFrame(sheetBytes, 0, 2 * 64, 64, 64, 4);
                }

                foreach (var s in streams) s.Dispose();
            }
            catch
            {
                // Preview failure is non-critical
            }
        }

        [RelayCommand]
        public async Task CreateCharacterAsync()
        {
            if (string.IsNullOrWhiteSpace(CharacterName))
            {
                await _dialogService.DisplayAlertAsync("Error", "Please enter a name.", "OK");
                return;
            }

            if (_sessionService.CurrentUser == null)
            {
                await _dialogService.DisplayAlertAsync("Error", "No user session found. Please login again.", "OK");
                await _navigationService.NavigateToAsync("///LoadUserPage");
                return;
            }

            var character = new Character
            {
                UserId = _sessionService.CurrentUser.Id,
                Name = CharacterName,
                Class = SelectedClass,
                HairColor = SelectedHairColor,
                HairStyle = SelectedHairStyle,
                SkinColor = SelectedSkinColor,
                Level = 1,
                Experience = 0
            };

            SetBaseStats(character, SelectedClass);

            bool success = await _characterService.SaveCharacterAsync(character);

            if (success)
            {
                await _dialogService.DisplayAlertAsync("Success", "Character Created!", "OK");
                await _navigationService.NavigateToAsync("///MainPage");
            }
            else
            {
                await _dialogService.DisplayAlertAsync("Error", "Failed to save character.", "OK");
            }
        }

        private void SetBaseStats(Character character, string className)
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

    internal static class TaskExtensions
    {
        public static async void FireAndForget(this Task task)
        {
            try { await task; } catch { }
        }
    }
}
