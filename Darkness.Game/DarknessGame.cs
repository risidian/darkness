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
        private GraphicsDeviceManager? _graphics;
        private SpriteBatch? _spriteBatch;
        private WorldScene? _worldScene;
        private BattleScene? _battleScene;
        private DeathmatchScene? _deathmatchScene;
        private PvpScene? _pvpScene;
        private bool _isBattleActive = false;
        private bool _isDeathmatchActive = false;
        private bool _isPvpActive = false;
        private bool _isPaused = false;

        public DarknessGame(ICombatService combatService, ISessionService sessionService, StoryController storyController)
        {
            System.Diagnostics.Debug.WriteLine("[DarknessGame] Constructor starting...");
            _combatService = combatService ?? throw new System.ArgumentNullException(nameof(combatService));
            _sessionService = sessionService ?? throw new System.ArgumentNullException(nameof(sessionService));
            _storyController = storyController ?? throw new System.ArgumentNullException(nameof(storyController));
            
            try
            {
                // In some hosted scenarios, creating the GraphicsDeviceManager in the constructor 
                // triggers window creation which crashes the app.
                _graphics = new GraphicsDeviceManager(this);
                
                // Set default buffer sizes to avoid 0x0 initialization
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                
                System.Diagnostics.Debug.WriteLine("[DarknessGame] GraphicsDeviceManager created.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DarknessGame] Warning: Failed to initialize GraphicsDeviceManager in constructor: {ex.Message}");
            }
            
            IsMouseVisible = true;
            System.Diagnostics.Debug.WriteLine("[DarknessGame] Constructor complete.");
        }

        public void Pause() 
        {
            _isPaused = true;
            System.Diagnostics.Debug.WriteLine("[DarknessGame] Game Paused");
        }

        public void Resume() 
        {
            _isPaused = false;
            System.Diagnostics.Debug.WriteLine("[DarknessGame] Game Resumed");
        }

        public void StartBattle(BattleSnapshot snapshot)
        {
            // Create fresh character instances from the snapshot to decouple from MAUI ViewModels
            var party = snapshot.Party.Select(s => new Character
            {
                Name = s.Name,
                Class = s.Class,
                CurrentHP = s.CurrentHP,
                MaxHP = s.MaxHP,
                Level = s.Level,
                Thumbnail = s.Thumbnail,
                HairColor = s.HairColor,
                HairStyle = s.HairStyle,
                SkinColor = s.SkinColor
            }).ToList();

            _battleScene = new BattleScene(this, party, snapshot.Enemies, snapshot.SurvivalTurns);
            _battleScene.BattleEnded += (s, e) => _isBattleActive = false;
            _battleScene.LoadContent(Content);
            _isBattleActive = true;
            _isDeathmatchActive = false;
            _isPvpActive = false;
            _isPaused = false;
        }

        public void StartBattle(List<Character> party, List<Enemy> enemies, int? survivalTurns = null)
        {
            _battleScene = new BattleScene(this, party, enemies, survivalTurns);
            _battleScene.BattleEnded += (s, e) => _isBattleActive = false;
            _battleScene.LoadContent(Content);
            _isBattleActive = true;
            _isDeathmatchActive = false;
            _isPvpActive = false;
            _isPaused = false;
        }

        public void StartDeathmatch(List<Character> party, DeathmatchEncounter encounter)
        {
            _deathmatchScene = new DeathmatchScene(this, party, encounter);
            _deathmatchScene.BattleEnded += (s, e) => _isDeathmatchActive = false;
            _deathmatchScene.LoadContent(Content);
            _isDeathmatchActive = true;
            _isBattleActive = false;
            _isPvpActive = false;
            _isPaused = false;
        }

        public void StartPvp(Character player1, Character player2)
        {
            _pvpScene = new PvpScene(this, _combatService, player1, player2);
            _pvpScene.BattleEnded += (s, e) => _isPvpActive = false;
            _pvpScene.LoadContent(Content);
            _isPvpActive = true;
            _isBattleActive = false;
            _isDeathmatchActive = false;
            _isPaused = false;
        }

        public GraphicsDeviceManager? GraphicsManager => _graphics;

        public new void Tick()
        {
            if (_isPaused) return;

            if (GraphicsDevice == null)
            {
                PrepareForPlatform();
            }

            if (GraphicsDevice == null) return;

            // Drive the game loop manually
            var gameTime = new GameTime(
                System.TimeSpan.FromMilliseconds(System.Environment.TickCount),
                System.TimeSpan.FromMilliseconds(16.6) // Assume 60fps
            );

            try
            {
                Update(gameTime);
                Draw(gameTime);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DarknessGame] Error during loop: {ex.Message}");
            }
        }

        public void PrepareForPlatform()
        {
            if (Content == null)
            {
                System.Diagnostics.Debug.WriteLine("[DarknessGame] Initializing ContentManager...");
                Content = new Microsoft.Xna.Framework.Content.ContentManager(Services, "Content");
            }

            if (GraphicsDevice == null && _graphics != null)
            {
                System.Diagnostics.Debug.WriteLine("[DarknessGame] GraphicsDevice is null, attempting initialization...");
                try
                {
                    // For WindowsDX, we must ensure we don't call ApplyChanges if a window hasn't been 
                    // assigned or if we're not ready, but since the Handler set the SwapChainPanel,
                    // this should now be safe.
                    _graphics.ApplyChanges();
                    
                    if (GraphicsDevice != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DarknessGame] GraphicsDevice created successfully: {GraphicsDevice.Adapter.Description}");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DarknessGame] Warning: ApplyChanges failed (this is expected if surface not ready): {ex.Message}");
                }
            }
        }

        protected override void Initialize()
        {
            PrepareForPlatform();

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
            if (GraphicsDevice == null) return;
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
            if (GraphicsDevice == null) return;
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
