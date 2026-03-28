using System.Diagnostics;
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
        private string _characterName;

        [ObservableProperty]
        private string _selectedClass;

        [ObservableProperty]
        private string _selectedHairColor;

        [ObservableProperty]
        private string _selectedSkinColor;

        [ObservableProperty]
        private string _selectedFace;

        [ObservableProperty]
        private string _selectedEyes;

        [ObservableProperty]
        private string _selectedHead;

        [ObservableProperty]
        private string _selectedFeet;

        [ObservableProperty]
        private string _selectedArms;

        [ObservableProperty]
        private string _selectedLegs;

        [ObservableProperty]
        private string _selectedHairStyle;

        [ObservableProperty]
        private string _selectedArmor;

        [ObservableProperty]
        private string _selectedWeapon;

        [ObservableProperty]
        private byte[]? _previewImageBytes;
        public List<string> Classes { get; } = new() { "Knight", "Rogue", "Mage", "Warrior", "Cleric" };
        public List<string> HairColors => _catalog.HairColors;
        public List<string> SkinColors => _catalog.SkinColors;
        public List<string> FaceTypes => _catalog.FaceTypes;
        public List<string> EyeTypes => _catalog.EyeTypes;
        public List<string> HeadTypes => _catalog.HeadTypes;
        public List<string> FeetTypes => _catalog.FeetTypes;
        public List<string> ArmsTypes => _catalog.ArmsTypes;
        public List<string> LegsTypes => _catalog.LegsTypes;
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

            // Set default values here to ensure OnPropertyChanged is raised
            CharacterName = string.Empty;
            SelectedClass = "Warrior";
            SelectedHairColor = "Black";
            SelectedSkinColor = "Light";
            SelectedFace = "Default";
            SelectedEyes = "Default";
            SelectedHead = "Human Male";
            SelectedFeet = "Boots (Basic)";
            SelectedArms = "None";
            SelectedLegs = "Slacks";
            SelectedHairStyle = "Long";
            SelectedArmor = "Plate (Steel)";
            SelectedWeapon = "Arming Sword (Steel)";

            UpdatePreviewAsync().FireAndForget();
        }

        partial void OnSelectedClassChanged(string value)
        {
            var defaults = _catalog.GetDefaultAppearanceForClass(value);
            SelectedArmor = defaults.ArmorType;
            SelectedWeapon = defaults.WeaponType;
            SelectedFeet = defaults.Feet;
            SelectedArms = defaults.Arms;
            SelectedLegs = defaults.Legs;
        }

        partial void OnSelectedHairColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedSkinColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedFaceChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedEyesChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedHeadChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedFeetChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedArmsChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedLegsChanged(string value) => UpdatePreviewAsync().FireAndForget();
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
                    Face = SelectedFace,
                    Eyes = SelectedEyes,
                    Head = SelectedHead,
                    Feet = SelectedFeet,
                    Arms = SelectedArms,
                    Legs = SelectedLegs,
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
                        // MAUI raw assets on Windows are at Resources/Raw/{path} in the package
                        var stream = await _fileSystem.OpenAppPackageFileAsync(layer.ResourcePath);
                        streams.Add(stream);
                        System.Diagnostics.Debug.WriteLine(
                            $"[SpritePreview] Loaded: {layer.ResourcePath} ({stream.Length} bytes)");
                    }
                    catch
                    {
                        try
                        {
                            // Fallback: try with Resources/Raw/ prefix (Windows MSIX)
                            var altPath = "Resources/Raw/" + layer.ResourcePath;
                            var stream = await _fileSystem.OpenAppPackageFileAsync(altPath);
                            streams.Add(stream);
                            System.Diagnostics.Debug.WriteLine(
                                $"[SpritePreview] Loaded (alt): {altPath} ({stream.Length} bytes)");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[SpritePreview] FAILED: {layer.ResourcePath} - {ex.Message}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[SpritePreview] Loaded {streams.Count}/{layerDefs.Count} layers");

                if (streams.Count > 0)
                {
                    // Walk animation sheets are 576x256 (9 frames x 4 directions of 64x64)
                    // South-facing idle = row 2 (Up=0, Left=1, Down=2, Right=3), col 0
                    var sheetBytes = _compositor.CompositeLayers(streams, 576, 256);
                    System.Diagnostics.Debug.WriteLine($"[SpritePreview] Composite sheet: {sheetBytes.Length} bytes");
                    PreviewImageBytes = _compositor.ExtractFrame(sheetBytes, 0, 2 * 64, 64, 64, 4);
                    System.Diagnostics.Debug.WriteLine(
                        $"[SpritePreview] Preview frame: {PreviewImageBytes?.Length ?? 0} bytes");
                }

                foreach (var s in streams) s.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SpritePreview] OUTER ERROR: {ex}");
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
                case "Knight":
                    character.Strength = 12;
                    character.Dexterity = 8;
                    character.Constitution = 14;
                    character.Intelligence = 10;
                    character.Wisdom = 12;
                    character.Charisma = 12;
                    break;
                case "Warrior":
                    character.Strength = 15;
                    character.Dexterity = 13;
                    character.Constitution = 14;
                    character.Intelligence = 8;
                    character.Wisdom = 10;
                    character.Charisma = 8;
                    break;
                case "Mage":
                    character.Strength = 9;
                    character.Dexterity = 12;
                    character.Constitution = 9;
                    character.Intelligence = 15;
                    character.Wisdom = 10;
                    character.Charisma = 13;
                    break;
                case "Rogue":
                    character.Strength = 12;
                    character.Dexterity = 15;
                    character.Constitution = 9;
                    character.Intelligence = 14;
                    character.Wisdom = 8;
                    character.Charisma = 10;
                    break;
                case "Cleric":
                    character.Strength = 12;
                    character.Dexterity = 9;
                    character.Constitution = 14;
                    character.Intelligence = 8;
                    character.Wisdom = 14;
                    character.Charisma = 9;
                    break;
                default:
                    throw new ArgumentException($"Unknown class: {className}");
                    character.Strength = 10;
                    character.Dexterity = 10;
                    character.Constitution = 10;
                    character.Intelligence = 10;
                    character.Wisdom = 10;
                    character.Charisma = 10;
                    break;
            }

            //TODO: Modifiers
            character.MaxHP = character.Constitution * 10;
            character.CurrentHP = character.MaxHP;
            character.Mana = character.Wisdom * 5;
            character.Stamina = character.Constitution * 5;
            character.Speed = character.Dexterity;
            character.Accuracy = 80 + character.Dexterity / 2;
            character.Evasion = character.Dexterity / 2;
            character.Defense = character.Constitution / 2;
            character.MagicDefense = character.Wisdom / 2;
        }
    }

    internal static class TaskExtensions
    {
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error with Task Extension {e.Message}");
            }
        }
    }
}