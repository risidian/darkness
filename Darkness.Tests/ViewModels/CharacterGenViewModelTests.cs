using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class CharacterGenViewModelTests
    {
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<ISpriteLayerCatalog> _catalogMock;
        private readonly Mock<ISpriteCompositor> _compositorMock;
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly CharacterGenViewModel _viewModel;

        public CharacterGenViewModelTests()
        {
            _characterServiceMock = new Mock<ICharacterService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _catalogMock = new Mock<ISpriteLayerCatalog>();
            _compositorMock = new Mock<ISpriteCompositor>();
            _fileSystemMock = new Mock<IFileSystemService>();

            _catalogMock.Setup(x => x.GetOptionNames("Hair")).Returns(new List<string> { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob", "Afro" });
            _catalogMock.Setup(x => x.GetOptionNames("HairColor")).Returns(new List<string> { "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut", "Blue", "Green", "Purple" });
            _catalogMock.Setup(x => x.GetOptionNames("Skin")).Returns(new List<string> { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" });
            _catalogMock.Setup(x => x.GetOptionNames("Face")).Returns(new List<string> { "Default", "Female" });
            _catalogMock.Setup(x => x.GetOptionNames("Eyes")).Returns(new List<string> { "Default", "Neutral", "Anger", "Sad", "Shock" });
            _catalogMock.Setup(x => x.GetOptionNames("Head")).Returns(new List<string> { "Human Male", "Human Female" });
            _catalogMock.Setup(x => x.GetOptionNames("Feet")).Returns(new List<string> { "Boots (Basic)", "Boots (Fold)", "Boots (Rimmed)", "Shoes", "Sandals", "None" });
            _catalogMock.Setup(x => x.GetOptionNames("Arms")).Returns(new List<string> { "Gloves", "None" });
            _catalogMock.Setup(x => x.GetOptionNames("Legs")).Returns(new List<string> { "Slacks", "Leggings", "Formal", "Cuffed", "Pantaloons", "None" });
            _catalogMock.Setup(x => x.GetOptionNames("Armor")).Returns(new List<string> { "Plate (Steel)", "Leather", "Longsleeve (Blue)" });
            _catalogMock.Setup(x => x.GetOptionNames("Weapon")).Returns(new List<string> { "Arming Sword (Steel)", "None" });
            _catalogMock.Setup(x => x.GetOptionNames("Shield")).Returns(new List<string> { "Crusader", "Spartan", "None" });
            _catalogMock.Setup(x => x.GetDefaultAppearanceForClass(It.IsAny<string>()))
                .Returns<string>(cls => cls switch
                {
                    "Mage" => new CharacterAppearance { ArmorType = "Longsleeve (Blue)", WeaponType = "None", ShieldType = "None", Feet = "Sandals", Arms = "None", Legs = "Formal" },
                    "Rogue" => new CharacterAppearance { ArmorType = "Leather (Black)", WeaponType = "Arming Sword (Iron)", ShieldType = "None", Feet = "Boots (Fold)", Arms = "Gloves", Legs = "Leggings" },
                    _ => new CharacterAppearance { ArmorType = "Plate (Steel)", WeaponType = "Arming Sword (Steel)", ShieldType = "Crusader", Feet = "Boots (Basic)", Arms = "Gloves", Legs = "Slacks" },
                });
            _catalogMock.Setup(x => x.GetStitchLayers(It.IsAny<CharacterAppearance>()))
                .Returns(new List<StitchLayer>());

            _viewModel = new CharacterGenViewModel(
                _characterServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _dialogServiceMock.Object,
                _catalogMock.Object,
                _compositorMock.Object,
                _fileSystemMock.Object);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithEmptyName_ShowsError()
        {
            _viewModel.CharacterName = "";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "Please enter a name.", "OK"), Times.Once);
            _characterServiceMock.Verify(x => x.SaveCharacterAsync(It.IsAny<Character>()), Times.Never);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithNoSession_ShowsErrorAndNavigatesToLogin()
        {
            _viewModel.CharacterName = "TestChar";
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns((User?)null);

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", It.Is<string>(s => s.Contains("No user session")), "OK"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("LoadUserPage", null), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidWarrior_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);

            _viewModel.CharacterName = "Conan";
            _viewModel.SelectedClass = "Warrior";

            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal("Conan", savedCharacter!.Name);
            Assert.Equal("Warrior", savedCharacter.Class);
            Assert.Equal(15, savedCharacter.Strength);
            Assert.Equal(12, savedCharacter.Dexterity);
            Assert.Equal(14, savedCharacter.Constitution);
            Assert.Equal(8, savedCharacter.Intelligence);
            Assert.Equal(8, savedCharacter.Wisdom);
            Assert.Equal(10, savedCharacter.Charisma);
            Assert.Equal(2, savedCharacter.ArmorClass);
            Assert.Equal(140, savedCharacter.MaxHP); // Constitution * 10
            Assert.Equal(140, savedCharacter.CurrentHP);
            Assert.Equal(1, savedCharacter.UserId);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidMage_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Gandalf";
            _viewModel.SelectedClass = "Mage";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal(15, savedCharacter!.Intelligence);
            Assert.Equal(14, savedCharacter.Wisdom);
            Assert.Equal(8, savedCharacter.Strength);
            Assert.Equal(10, savedCharacter.Dexterity);
            Assert.Equal(10, savedCharacter.Constitution);
            Assert.Equal(12, savedCharacter.Charisma);
            Assert.Equal(0, savedCharacter.ArmorClass);
            Assert.Equal(100, savedCharacter.MaxHP); // Constitution(10) * 10
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidRogue_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Shadow";
            _viewModel.SelectedClass = "Rogue";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal(15, savedCharacter!.Dexterity);
            Assert.Equal(12, savedCharacter.Strength);
            Assert.Equal(10, savedCharacter.Constitution);
            Assert.Equal(10, savedCharacter.Intelligence);
            Assert.Equal(8, savedCharacter.Wisdom);
            Assert.Equal(12, savedCharacter.Charisma);
            Assert.Equal(1, savedCharacter.ArmorClass);
            Assert.Equal(15, savedCharacter.Speed); // Dexterity
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidKnight_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Lancelot";
            _viewModel.SelectedClass = "Knight";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal(12, savedCharacter!.Strength);
            Assert.Equal(10, savedCharacter.Dexterity);
            Assert.Equal(14, savedCharacter.Constitution);
            Assert.Equal(8, savedCharacter.Intelligence);
            Assert.Equal(10, savedCharacter.Wisdom);
            Assert.Equal(12, savedCharacter.Charisma);
            Assert.Equal(2, savedCharacter.ArmorClass);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidCleric_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Elara";
            _viewModel.SelectedClass = "Cleric";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal(10, savedCharacter!.Strength);
            Assert.Equal(8, savedCharacter.Dexterity);
            Assert.Equal(14, savedCharacter.Constitution);
            Assert.Equal(10, savedCharacter.Intelligence);
            Assert.Equal(15, savedCharacter.Wisdom);
            Assert.Equal(12, savedCharacter.Charisma);
            Assert.Equal(2, savedCharacter.ArmorClass);
        }

        [Fact]
        public async Task CreateCharacterAsync_OnSuccess_ShowsSuccessAndNavigates()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>())).ReturnsAsync(true);

            _viewModel.CharacterName = "Hero";
            _viewModel.SelectedClass = "Warrior";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Success", "Character Created!", "OK"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("MainMenuPage", null), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterAsync_OnFailure_ShowsError()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>())).ReturnsAsync(false);

            _viewModel.CharacterName = "Hero";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            _dialogServiceMock.Verify(x => x.DisplayAlertAsync("Error", "Failed to save character.", "OK"), Times.Once);
            _navigationServiceMock.Verify(x => x.NavigateToAsync("MainMenuPage", null), Times.Never);
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            Assert.Equal("Warrior", _viewModel.SelectedClass);
            Assert.Equal("Black", _viewModel.SelectedHairColor);
            Assert.Equal("Light", _viewModel.SelectedSkinColor);
            Assert.Equal("Default", _viewModel.SelectedFace);
            Assert.Equal("Default", _viewModel.SelectedEyes);
            Assert.Equal("Human Male", _viewModel.SelectedHead);
            Assert.Equal("Crusader", _viewModel.SelectedShield);
            Assert.Contains("Warrior", _viewModel.Classes);
            Assert.Contains("Mage", _viewModel.Classes);
            Assert.Contains("Rogue", _viewModel.Classes);
            Assert.Contains("Knight", _viewModel.Classes);
            Assert.Contains("Cleric", _viewModel.Classes);
        }

        [Fact]
        public async Task CreateCharacterAsync_DerivedStats_CalculatedCorrectly()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Test";
            _viewModel.SelectedClass = "Warrior";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            // Warrior: Constitution=14, Dexterity=12, Wisdom=8
            Assert.Equal(70, savedCharacter!.Stamina);   // Constitution * 5
            Assert.Equal(40, savedCharacter.Mana);       // Wisdom * 5
            Assert.Equal(12, savedCharacter.Speed);      // Dexterity
            Assert.Equal(86, savedCharacter.Accuracy);   // 80 + Dexterity/2
            Assert.Equal(6, savedCharacter.Evasion);     // Dexterity / 2
            Assert.Equal(7, savedCharacter.Defense);     // Constitution / 2
            Assert.Equal(4, savedCharacter.MagicDefense); // Wisdom / 2
        }

        [Fact]
        public void HairStyles_ContainsExpectedOptions()
        {
            Assert.Contains("Long", _viewModel.HairStyles);
            Assert.Contains("Plain", _viewModel.HairStyles);
            Assert.Contains("Spiked", _viewModel.HairStyles);
            Assert.Contains("Bob", _viewModel.HairStyles);
        }

        [Fact]
        public void DefaultValues_HairStyleIsLong()
        {
            Assert.Equal("Long", _viewModel.SelectedHairStyle);
        }

        [Fact]
        public async Task CreateCharacterAsync_SavesAllAppearanceFields()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Test";
            _viewModel.SelectedHairStyle = "Shorthawk";
            _viewModel.SelectedHairColor = "Redhead";
            _viewModel.SelectedSkinColor = "Bronze";
            _viewModel.SelectedHead = "Human Female";
            _viewModel.SelectedFace = "Female";
            _viewModel.SelectedEyes = "Anger";
            _viewModel.SelectedFeet = "Sandals";
            _viewModel.SelectedArms = "Gloves";
            _viewModel.SelectedLegs = "Formal";
            _viewModel.SelectedArmor = "Plate (Steel)";
            _viewModel.SelectedWeapon = "Arming Sword (Steel)";
            _viewModel.SelectedShield = "Spartan";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal("Shorthawk", savedCharacter!.HairStyle);
            Assert.Equal("Redhead", savedCharacter.HairColor);
            Assert.Equal("Bronze", savedCharacter.SkinColor);
            Assert.Equal("Human Female", savedCharacter.Head);
            Assert.Equal("Female", savedCharacter.Face);
            Assert.Equal("Anger", savedCharacter.Eyes);
            Assert.Equal("Sandals", savedCharacter.Feet);
            Assert.Equal("Gloves", savedCharacter.Arms);
            Assert.Equal("Formal", savedCharacter.Legs);
            Assert.Equal("Plate (Steel)", savedCharacter.ArmorType);
            Assert.Equal("Arming Sword (Steel)", savedCharacter.WeaponType);
            Assert.Equal("Spartan", savedCharacter.ShieldType);
        }

        [Fact]
        public void ChangingClass_UpdatesArmorWeaponAndShield()
        {
            _viewModel.SelectedClass = "Mage";
            Assert.Equal("Longsleeve (Blue)", _viewModel.SelectedArmor);
            Assert.Equal("None", _viewModel.SelectedWeapon);
            Assert.Equal("None", _viewModel.SelectedShield);

            _viewModel.SelectedClass = "Rogue";
            Assert.Equal("Leather (Black)", _viewModel.SelectedArmor);
            Assert.Equal("Arming Sword (Iron)", _viewModel.SelectedWeapon);
            Assert.Equal("None", _viewModel.SelectedShield);

            _viewModel.SelectedClass = "Warrior";
            Assert.Equal("Plate (Steel)", _viewModel.SelectedArmor);
            Assert.Equal("Arming Sword (Steel)", _viewModel.SelectedWeapon);
            Assert.Equal("Crusader", _viewModel.SelectedShield);
        }
    }
}
