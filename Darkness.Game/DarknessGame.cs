using System.Collections.Generic;
using System.Linq;
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
        private readonly Input.InputManager _inputManager;
        private GraphicsDeviceManager? _graphics;
        private SpriteBatch? _spriteBatch;
        private WorldScene? _worldScene;
        private BattleScene? _battleScene;
        private DeathmatchScene? _deathmatchScene;
        private PvpScene? _pvpScene;
        private volatile bool _isBattleActive = false;
        private volatile bool _isDeathmatchActive = false;
        private volatile bool _isPvpActive = false;
        private volatile bool _isPaused = false;
        private volatile bool _disposed = false;
        private readonly object _sceneLock = new object();

        public DarknessGame(ICombatService combatService, ISessionService sessionService, StoryController storyController)
        {
            System.Diagnostics.Debug.WriteLine("[DarknessGame] Constructor starting...");
            _combatService = combatService ?? throw new System.ArgumentNullException(nameof(combatService));
            _sessionService = sessionService ?? throw new System.ArgumentNullException(nameof(sessionService));
            _storyController = storyController ?? throw new System.ArgumentNullException(nameof(storyController));
            _inputManager = new Input.InputManager();
            
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
            lock (_sceneLock)
            {
                DisposeTransientScenes();
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

                _battleScene = new BattleScene(this, _inputManager, party, snapshot.Enemies, snapshot.SurvivalTurns);
                _battleScene.BattleEnded += OnBattleEnded;
                _battleScene.LoadContent(Content);
                _isBattleActive = true;
                _isDeathmatchActive = false;
                _isPvpActive = false;
                _isPaused = false;
            }
        }

        public void StartBattle(List<Character> party, List<Enemy> enemies, int? survivalTurns = null)
        {
            lock (_sceneLock)
            {
                DisposeTransientScenes();
                _battleScene = new BattleScene(this, _inputManager, party, enemies, survivalTurns);
                _battleScene.BattleEnded += OnBattleEnded;
                _battleScene.LoadContent(Content);
                _isBattleActive = true;
                _isDeathmatchActive = false;
                _isPvpActive = false;
                _isPaused = false;
            }
        }

        public void StartDeathmatch(List<Character> party, DeathmatchEncounter encounter)
        {
            lock (_sceneLock)
            {
                DisposeTransientScenes();
                _deathmatchScene = new DeathmatchScene(this, _inputManager, party, encounter);
                _deathmatchScene.BattleEnded += OnDeathmatchEnded;
                _deathmatchScene.LoadContent(Content);
                _isDeathmatchActive = true;
                _isBattleActive = false;
                _isPvpActive = false;
                _isPaused = false;
            }
        }

        public void StartPvp(Character player1, Character player2)
        {
            lock (_sceneLock)
            {
                DisposeTransientScenes();
                _pvpScene = new PvpScene(this, _inputManager, _combatService, player1, player2);
                _pvpScene.BattleEnded += OnPvpEnded;
                _pvpScene.LoadContent(Content);
                _isPvpActive = true;
                _isBattleActive = false;
                _isDeathmatchActive = false;
                _isPaused = false;
            }
        }

        private void OnBattleEnded(object? sender, System.EventArgs e)
        {
            lock (_sceneLock)
            {
                _isBattleActive = false;
                if (_battleScene != null)
                {
                    _battleScene.BattleEnded -= OnBattleEnded;
                    _battleScene.Dispose();
                    _battleScene = null;
                }
            }
        }

        private void OnDeathmatchEnded(object? sender, System.EventArgs e)
        {
            lock (_sceneLock)
            {
                _isDeathmatchActive = false;
                if (_deathmatchScene != null)
                {
                    _deathmatchScene.BattleEnded -= OnDeathmatchEnded;
                    _deathmatchScene.Dispose();
                    _deathmatchScene = null;
                }
            }
        }

        private void OnPvpEnded(object? sender, System.EventArgs e)
        {
            lock (_sceneLock)
            {
                _isPvpActive = false;
                if (_pvpScene != null)
                {
                    _pvpScene.BattleEnded -= OnPvpEnded;
                    _pvpScene.Dispose();
                    _pvpScene = null;
                }
            }
        }

        private void OnEncounterTriggered(object? sender, System.EventArgs e)
        {
            lock (_sceneLock)
            {
                if (_sessionService.CurrentCharacter == null) return;

                // Load encounter for story beat 3
                _storyController.SetBeat(3);
                var (enemies, survivalTurns, additionalMembers) = _storyController.GetEncounterForBeat(3);
                
                var party = new List<Character> { _sessionService.CurrentCharacter };
                party.AddRange(additionalMembers);

                StartBattle(party, enemies, survivalTurns);
            }
        }

        private void DisposeTransientScenes()
        {
            if (_battleScene != null)
            {
                _battleScene.BattleEnded -= OnBattleEnded;
                _battleScene.Dispose();
                _battleScene = null;
            }
            if (_deathmatchScene != null)
            {
                _deathmatchScene.BattleEnded -= OnDeathmatchEnded;
                _deathmatchScene.Dispose();
                _deathmatchScene = null;
            }
            if (_pvpScene != null)
            {
                _pvpScene.BattleEnded -= OnPvpEnded;
                _pvpScene.Dispose();
                _pvpScene = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                lock (_sceneLock)
                {
                    if (_worldScene != null)
                    {
                        _worldScene.EncounterTriggered -= OnEncounterTriggered;
                        _worldScene.Dispose();
                        _worldScene = null;
                    }
                    _spriteBatch?.Dispose();
                    _spriteBatch = null;
                    DisposeTransientScenes();
                    
                    if (_graphics is System.IDisposable disposableGraphics)
                    {
                        disposableGraphics.Dispose();
                    }
                    _graphics = null;
                }
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public GraphicsDeviceManager? GraphicsManager => _graphics;

        public new void Tick()
        {
            if (_isPaused || _disposed) return;

            if (GraphicsDevice == null)
            {
                PrepareForPlatform();
            }

            if (GraphicsDevice == null) return;

            // Late-initialization check: ensure _spriteBatch exists AND content for active scenes is loaded.
            // We call LoadContent() to re-verify state even if _spriteBatch is already present.
            LoadContent();

            // Drive the game loop manually
            var gameTime = new GameTime(
                System.TimeSpan.FromMilliseconds(System.Environment.TickCount),
                System.TimeSpan.FromMilliseconds(16.6) // Assume 60fps
            );

            try
            {
                lock (_sceneLock)
                {
                    _inputManager.Update();
                    Update(gameTime);
                    Draw(gameTime);
                    // Present the frame — Tick() bypasses MonoGame's normal BeginDraw/EndDraw
                    // pipeline, so we must call Present explicitly.
                    GraphicsDevice.Present();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DarknessGame] Error during loop: {ex.Message}");
            }
        }

        public void PrepareForPlatform()
        {
            lock (_sceneLock)
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
        }

        protected override void Initialize()
        {
            lock (_sceneLock)
            {
                PrepareForPlatform();

                _worldScene = new WorldScene(this, _inputManager, _sessionService);
                _worldScene.EncounterTriggered += OnEncounterTriggered;
                base.Initialize();
            }
        }

        protected override void LoadContent()
        {
            lock (_sceneLock)
            {
                if (GraphicsDevice == null || Content == null) return;
                
                if (_spriteBatch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DarknessGame] Creating SpriteBatch...");
                    _spriteBatch = new SpriteBatch(GraphicsDevice);
                }
                
                // Always attempt to load content for all non-null scenes to ensure they are ready for the next frame.
                // Individual scenes handle redundant LoadContent calls via internal null-checks for resources.
                _worldScene?.LoadContent(Content);
                _battleScene?.LoadContent(Content);
                _deathmatchScene?.LoadContent(Content);
                _pvpScene?.LoadContent(Content);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Note: Update and Draw are called within _sceneLock from Tick()
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
            // Note: Update and Draw are called within _sceneLock from Tick()
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
