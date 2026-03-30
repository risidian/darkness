using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Game;

namespace Darkness.MAUI.Services
{
    public class GameEngineFactory : IGameEngineFactory
    {
        private readonly ICombatService _combatService;
        private readonly ISessionService _sessionService;
        private readonly StoryController _storyController;

        public GameEngineFactory(
            ICombatService combatService,
            ISessionService sessionService,
            StoryController storyController)
        {
            _combatService = combatService;
            _sessionService = sessionService;
            _storyController = storyController;
        }

        public object CreateGameEngine()
        {
            return new DarknessGame(_combatService, _sessionService, _storyController);
        }
    }
}
