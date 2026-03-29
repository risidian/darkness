using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Darkness.Core.Models;
using Darkness.Game.Scenes;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;

namespace Darkness.Game
{
    public class DarknessGame : Microsoft.Xna.Framework.Game
    {
        private readonly ICombatService _combatService;
        private readonly ISessionService _sessionService;
        private readonly StoryController _storyController;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch? _spriteBatch;
        private WorldScene? _worldScene;
        private BattleScene? _battleScene;
        private DeathmatchScene? _deathmatchScene;
        private PvpScene? _pvpScene;
        private bool _isBattleActive = false;
        private bool _isDeathmatchActive = false;
        private bool _isPvpActive = false;

        public DarknessGame(ICombatService combatService, ISessionService sessionService, StoryController storyController)
        {
            _combatService = combatService;
            _sessionService = sessionService;
            _storyController = storyController;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public void StartBattle(List<Character> party, List<Enemy> enemies, int? survivalTurns = null)
        {
            _battleScene = new BattleScene(this, party, enemies, survivalTurns);
            _battleScene.BattleEnded += (s, e) => _isBattleActive = false;
            _battleScene.LoadContent(Content);
            _isBattleActive = true;
            _isDeathmatchActive = false;
            _isPvpActive = false;
        }

        public void StartDeathmatch(List<Character> party, DeathmatchEncounter encounter)
        {
            _deathmatchScene = new DeathmatchScene(this, party, encounter);
            _deathmatchScene.BattleEnded += (s, e) => _isDeathmatchActive = false;
            _deathmatchScene.LoadContent(Content);
            _isDeathmatchActive = true;
            _isBattleActive = false;
            _isPvpActive = false;
        }

        public void StartPvp(Character player1, Character player2)
        {
            _pvpScene = new PvpScene(this, _combatService, player1, player2);
            _pvpScene.BattleEnded += (s, e) => _isPvpActive = false;
            _pvpScene.LoadContent(Content);
            _isPvpActive = true;
            _isBattleActive = false;
            _isDeathmatchActive = false;
        }

        protected override void Initialize()
        {
            _worldScene = new WorldScene(this, _sessionService);
            _worldScene.EncounterTriggered += (s, e) =>
            {
                if (_sessionService.CurrentCharacter == null) return;

                // Load encounter for story beat 3
                _storyController.SetBeat(3);
                var (enemies, survivalTurns, additionalMembers) = _storyController.GetEncounterForBeat(3);
                
                var party = new List<Character> { _sessionService.CurrentCharacter };
                party.AddRange(additionalMembers);

                StartBattle(party, enemies, survivalTurns);
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _worldScene?.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_isBattleActive)
            {
                _battleScene?.Update(gameTime);
            }
            else if (_isDeathmatchActive)
            {
                _deathmatchScene?.Update(gameTime);
            }
            else if (_isPvpActive)
            {
                _pvpScene?.Update(gameTime);
            }
            else
            {
                _worldScene?.Update(gameTime);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch?.Begin();
            if (_isBattleActive)
            {
                if (_spriteBatch != null) _battleScene?.Draw(_spriteBatch);
            }
            else if (_isDeathmatchActive)
            {
                if (_spriteBatch != null) _deathmatchScene?.Draw(_spriteBatch);
            }
            else if (_isPvpActive)
            {
                if (_spriteBatch != null) _pvpScene?.Draw(_spriteBatch);
            }
            else
            {
                _worldScene?.Draw(_spriteBatch);
            }
            _spriteBatch?.End();

            base.Draw(gameTime);
        }
    }
}
