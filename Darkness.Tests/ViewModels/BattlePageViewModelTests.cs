using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class BattlePageViewModelTests
    {
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IQuestService> _questServiceMock;
        private readonly BattlePageViewModel _viewModel;

        public BattlePageViewModelTests()
        {
            _characterServiceMock = new Mock<ICharacterService>();
            _sessionServiceMock = new Mock<ISessionService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _questServiceMock = new Mock<IQuestService>();

            _viewModel = new BattlePageViewModel(
                _characterServiceMock.Object,
                _sessionServiceMock.Object,
                _navigationServiceMock.Object,
                _dialogServiceMock.Object,
                _questServiceMock.Object);
        }

        [Fact]
        public void Initialize_ValidQuest_ShowsEncounter()
        {
            var quest = new QuestNode 
            { 
                Id = "main_1", 
                Title = "The Beginning",
                Encounter = new EncounterData 
                { 
                    Enemies = new List<Enemy> { new Enemy { Name = "Shadow Minion" } } 
                }
            };
            _questServiceMock.Setup(x => x.GetQuestById("main_1")).Returns(quest);

            _viewModel.Initialize("main_1");

            Assert.Contains("Shadow Minion", _viewModel.StatusText);
            Assert.Contains("The Beginning", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_QuestWithSurvival_ShowsSurvivalText()
        {
            var quest = new QuestNode 
            { 
                Id = "survival_quest", 
                Title = "Survive",
                Encounter = new EncounterData 
                { 
                    Enemies = new List<Enemy> { new Enemy { Name = "Dark Warrior" } },
                    SurvivalTurns = 5
                }
            };
            _questServiceMock.Setup(x => x.GetQuestById("survival_quest")).Returns(quest);

            _viewModel.Initialize("survival_quest");

            Assert.Contains("Dark Warrior", _viewModel.StatusText);
            Assert.Contains("Survive for 5 turns", _viewModel.StatusText);
        }

        [Fact]
        public void Initialize_QuestWithAlly_IncludesAllyInStatus()
        {
            var quest = new QuestNode 
            { 
                Id = "boss_quest", 
                Title = "Boss Battle",
                Encounter = new EncounterData 
                { 
                    Enemies = new List<Enemy> { new Enemy { Name = "Big Bad" } },
                    AdditionalPartyMembers = new List<Character> { new Character { Name = "Tywin" } }
                }
            };
            _questServiceMock.Setup(x => x.GetQuestById("boss_quest")).Returns(quest);

            _viewModel.Initialize("boss_quest");

            Assert.Contains("Joined by: Tywin", _viewModel.StatusText);
        }

        [Fact]
        public void OnBattleEnded_Victory_SetsVictoryState()
        {
            _viewModel.OnBattleEnded(true);

            Assert.Contains("Victory", _viewModel.StatusText);
            Assert.Equal("Gold", _viewModel.StatusColor);
            Assert.True(_viewModel.IsContinueVisible);
        }

        [Fact]
        public void OnBattleEnded_Defeat_SetsDefeatState()
        {
            _viewModel.OnBattleEnded(false);

            Assert.Contains("Defeat", _viewModel.StatusText);
            Assert.Equal("DarkRed", _viewModel.StatusColor);
            Assert.True(_viewModel.IsContinueVisible);
        }

        [Fact]
        public async Task FleeAsync_WhenConfirmed_NavigatesToGamePage()
        {
            _dialogServiceMock.Setup(x => x.DisplayConfirmAsync("Flee", It.IsAny<string>(), "Yes", "No"))
                .ReturnsAsync(true);

            await _viewModel.FleeCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.GoBackAsync(), Times.Once);
        }

        [Fact]
        public async Task FleeAsync_WhenCancelled_DoesNotNavigate()
        {
            _dialogServiceMock.Setup(x => x.DisplayConfirmAsync("Flee", It.IsAny<string>(), "Yes", "No"))
                .ReturnsAsync(false);

            await _viewModel.FleeCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.GoBackAsync(), Times.Never);
        }

        [Fact]
        public async Task ContinueAsync_NavigatesToGamePage()
        {
            await _viewModel.ContinueCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.GoBackAsync(), Times.Once);
        }
    }
}
