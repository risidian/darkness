using Darkness.Core.Interfaces;
using Darkness.Core.ViewModels;
using Moq;

namespace Darkness.Tests.ViewModels
{
    public class GamePageViewModelTests
    {
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly GamePageViewModel _viewModel;

        public GamePageViewModelTests()
        {
            _navigationServiceMock = new Mock<INavigationService>();
            _viewModel = new GamePageViewModel(_navigationServiceMock.Object);
        }

        [Fact]
        public async Task OpenCombatTestAsync_NavigatesToBattlePage()
        {
            await _viewModel.OpenCombatTestCommand.ExecuteAsync(null);

            _navigationServiceMock.Verify(x => x.NavigateToAsync("///BattlePage", null), Times.Once);
        }
    }
}
