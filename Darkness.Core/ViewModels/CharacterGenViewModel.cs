using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class CharacterGenViewModel : ObservableObject
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISpriteLayerCatalog _catalog;
        private readonly ISpriteCompositor _compositor;
        private readonly IFileSystemService _fileSystem;
        private CancellationTokenSource? _previewCts;

        [ObservableProperty] private string _characterName;

        [ObservableProperty] private string _selectedClass;

        [ObservableProperty] private string _selectedHairColor;

        [ObservableProperty] private string _selectedSkinColor;

        [ObservableProperty] private string _selectedFace;

        [ObservableProperty] private string _selectedEyes;

        [ObservableProperty] private string _selectedHead;

        [ObservableProperty] private string _selectedFeet;

        [ObservableProperty] private string _selectedArms;

        [ObservableProperty] private string _selectedLegs;

        [ObservableProperty] private string _selectedHairStyle;

        [ObservableProperty] private string _selectedArmor;

        [ObservableProperty] private string _selectedWeapon;

        [ObservableProperty] private string _selectedShield;

        [ObservableProperty] private byte[]? _previewImageBytes;
        public List<string> Classes { get; } = new() { "Knight", "Rogue", "Mage", "Warrior", "Cleric" };
        public List<string> HairColors => _catalog.GetOptionNames("HairColor");
        public List<string> SkinColors => _catalog.GetOptionNames("Skin");
        public List<string> FaceTypes => _catalog.GetOptionNames("Face");
        public List<string> EyeTypes => _catalog.GetOptionNames("Eyes");
        public List<string> HeadTypes => _catalog.GetOptionNames("Head");
        public List<string> FeetTypes => _catalog.GetOptionNames("Feet");
        public List<string> ArmsTypes => _catalog.GetOptionNames("Arms");
        public List<string> LegsTypes => _catalog.GetOptionNames("Legs");
        public List<string> HairStyles => _catalog.GetOptionNames("Hair");
        public List<string> ArmorTypes => _catalog.GetOptionNames("Armor");
        public List<string> WeaponTypes => _catalog.GetOptionNames("Weapon");
        public List<string> ShieldTypes => _catalog.GetOptionNames("Shield");

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
            SelectedShield = "Crusader";

            SchedulePreviewUpdate();
        }

        partial void OnSelectedClassChanged(string value)
        {
            var defaults = _catalog.GetDefaultAppearanceForClass(value);
            SelectedArmor = defaults.ArmorType;
            SelectedWeapon = defaults.WeaponType;
            SelectedShield = defaults.ShieldType;
            SelectedFeet = defaults.Feet;
            SelectedArms = defaults.Arms;
            SelectedLegs = defaults.Legs;
        }

        partial void OnSelectedHairColorChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedSkinColorChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedFaceChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedEyesChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedHeadChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedFeetChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedArmsChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedLegsChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedHairStyleChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedArmorChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedWeaponChanged(string value) => SchedulePreviewUpdate();
        partial void OnSelectedShieldChanged(string value) => SchedulePreviewUpdate();

        private void SchedulePreviewUpdate()
        {
            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            var token = _previewCts.Token;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, token);
                    if (!token.IsCancellationRequested)
                        await UpdatePreviewAsync();
                }
                catch (TaskCanceledException)
                {
                }
            });
        }

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
                    ShieldType = SelectedShield,
                };

                var stitchLayers = _catalog.GetStitchLayers(appearance);
                var previewFrameBytes = await _compositor.CompositePreviewFrame(stitchLayers, _fileSystem);

                if (previewFrameBytes != null && previewFrameBytes.Length > 0)
                {
                    PreviewImageBytes = _compositor.ExtractFrame(previewFrameBytes, 0, 0, 64, 64, 4);
                }
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
                await _navigationService.NavigateToAsync("LoadUserPage");
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
                Head = SelectedHead,
                Face = SelectedFace,
                Eyes = SelectedEyes,
                Feet = SelectedFeet,
                Arms = SelectedArms,
                Legs = SelectedLegs,
                ArmorType = SelectedArmor,
                WeaponType = SelectedWeapon,
                ShieldType = SelectedShield,
                Level = 1,
                Experience = 0,
                Thumbnail = PreviewImageBytes
            };

            SetBaseStats(character, SelectedClass);

            bool success = await _characterService.SaveCharacterAsync(character);

            if (success)
            {
                _sessionService.CurrentCharacter = character;
                await _dialogService.DisplayAlertAsync("Success", "Character Created!", "OK");
                await _navigationService.NavigateToAsync("MainMenuPage");
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
                    character.Dexterity = 10;
                    character.Constitution = 14;
                    character.Intelligence = 8;
                    character.Wisdom = 10;
                    character.Charisma = 12;
                    character.ArmorClass = 2;
                    break;
                case "Warrior":
                    character.Strength = 15;
                    character.Dexterity = 12;
                    character.Constitution = 14;
                    character.Intelligence = 8;
                    character.Wisdom = 8;
                    character.Charisma = 10;
                    character.ArmorClass = 2;
                    break;
                case "Mage":
                    character.Strength = 8;
                    character.Dexterity = 10;
                    character.Constitution = 10;
                    character.Intelligence = 15;
                    character.Wisdom = 14;
                    character.Charisma = 12;
                    character.ArmorClass = 0;
                    break;
                case "Rogue":
                    character.Strength = 12;
                    character.Dexterity = 15;
                    character.Constitution = 10;
                    character.Intelligence = 10;
                    character.Wisdom = 8;
                    character.Charisma = 12;
                    character.ArmorClass = 1;
                    break;
                case "Cleric":
                    character.Strength = 10;
                    character.Dexterity = 8;
                    character.Constitution = 14;
                    character.Intelligence = 10;
                    character.Wisdom = 15;
                    character.Charisma = 12;
                    character.ArmorClass = 2;
                    break;
                default:
                    throw new ArgumentException($"Unknown class: {className}");
            }

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
