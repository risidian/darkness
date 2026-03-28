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

            _catalogMock.Setup(x => x.HairStyles).Returns(new List<string> { "Long", "Plain", "Curly Long", "Shorthawk", "Spiked", "Bob" });
            _catalogMock.Setup(x => x.HairColors).Returns(new List<string> { "Blonde", "Black", "Dark Brown", "Redhead", "White", "Gray", "Platinum", "Chestnut" });
            _catalogMock.Setup(x => x.SkinColors).Returns(new List<string> { "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black" });
            _catalogMock.Setup(x => x.ArmorTypes).Returns(new List<string> { "Plate (Steel)", "Leather", "Longsleeve (Blue)" });
            _catalogMock.Setup(x => x.WeaponTypes).Returns(new List<string> { "Arming Sword (Steel)", "None" });
            _catalogMock.Setup(x => x.GetDefaultAppearanceForClass(It.IsAny<string>()))
                .Returns<string>(cls => cls switch
                {
                    "Mage" => new CharacterAppearance { ArmorType = "Longsleeve (Blue)", WeaponType = "None" },
                    "Rogue" => new CharacterAppearance { ArmorType = "Leather (Black)", WeaponType = "Arming Sword (Iron)" },
                    _ => new CharacterAppearance { ArmorType = "Plate (Steel)", WeaponType = "Arming Sword (Steel)" },
                });
            _catalogMock.Setup(x => x.GetLayersForAppearance(It.IsAny<CharacterAppearance>()))
                .Returns(new List<SpriteLayerDefinition>());

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
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///LoadUserPage", null), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterAsync_WithValidWarrior_SavesCorrectStats()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>())).ReturnsAsync(true);

            _viewModel.CharacterName = "Conan";
            _viewModel.SelectedClass = "Warrior";
            _viewModel.SelectedHairColor = "Black";
            _viewModel.SelectedSkinColor = "Tan";

            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal("Conan", savedCharacter!.Name);
            Assert.Equal("Warrior", savedCharacter.Class);
            Assert.Equal(15, savedCharacter.Strength);
            Assert.Equal(13, savedCharacter.Dexterity);
            Assert.Equal(14, savedCharacter.Constitution);
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
            Assert.Equal(10, savedCharacter.Wisdom);
            Assert.Equal(9, savedCharacter.Strength);
            Assert.Equal(90, savedCharacter.MaxHP); // Constitution(9) * 10
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
            Assert.Equal(10, savedCharacter.Charisma);
            Assert.Equal(15, savedCharacter.Speed); // Dexterity
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
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///MainPage", null), Times.Once);
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
            _navigationServiceMock.Verify(x => x.NavigateToAsync("///MainPage", null), Times.Never);
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            Assert.Equal("Warrior", _viewModel.SelectedClass);
            Assert.Equal("Black", _viewModel.SelectedHairColor);
            Assert.Equal("Light", _viewModel.SelectedSkinColor);
            Assert.Contains("Warrior", _viewModel.Classes);
            Assert.Contains("Mage", _viewModel.Classes);
            Assert.Contains("Rogue", _viewModel.Classes);
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
            // Warrior: Constitution=14, Dexterity=13, Wisdom=10
            Assert.Equal(70, savedCharacter!.Stamina);   // Constitution * 5
            Assert.Equal(50, savedCharacter.Mana);       // Wisdom * 5
            Assert.Equal(13, savedCharacter.Speed);      // Dexterity
            Assert.Equal(86, savedCharacter.Accuracy);   // 80 + Dexterity/2
            Assert.Equal(6, savedCharacter.Evasion);     // Dexterity / 2
            Assert.Equal(7, savedCharacter.Defense);     // Constitution / 2
            Assert.Equal(5, savedCharacter.MagicDefense); // Wisdom / 2
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
        public async Task CreateCharacterAsync_SavesHairStyle()
        {
            var user = new User { Id = 1 };
            _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
            Character? savedCharacter = null;
            _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
                .Callback<Character>(c => savedCharacter = c)
                .ReturnsAsync(true);

            _viewModel.CharacterName = "Test";
            _viewModel.SelectedHairStyle = "Shorthawk";

            await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

            Assert.NotNull(savedCharacter);
            Assert.Equal("Shorthawk", savedCharacter!.HairStyle);
        }

        [Fact]
        public void ChangingClass_UpdatesArmorAndWeapon()
        {
            _viewModel.SelectedClass = "Mage";
            Assert.Equal("Longsleeve (Blue)", _viewModel.SelectedArmor);
            Assert.Equal("None", _viewModel.SelectedWeapon);

            _viewModel.SelectedClass = "Rogue";
            Assert.Equal("Leather (Black)", _viewModel.SelectedArmor);
            Assert.Equal("Arming Sword (Iron)", _viewModel.SelectedWeapon);
        }
    }
}
