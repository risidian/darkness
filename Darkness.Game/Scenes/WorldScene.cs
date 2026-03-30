using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Darkness.Game.Scenes
{
    public class WorldScene : IDisposable
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly ISessionService _sessionService;
        private readonly Input.InputManager _inputManager;
        
        private Texture2D? _characterTexture;
        private Texture2D? _npcTexture;
        private Texture2D? _pixel;
        private SpriteFont? _font;
    private bool _fontLoadAttempted;

        private Vector2 _characterPosition;
        private readonly float _moveSpeed = 250f;
        private Rectangle _worldBounds;
        private Rectangle _exitTrigger;

        // NPC
        private Vector2 _npcPosition;
        private string _npcName = "Old Man";
        private string[] _dialogue = new[] {
            "Welcome to the Shore of Camelot, Wanderer.",
            "The path to the castle is blocked by shadows.",
            "You'll find only hounds and darkness to the east."
        };
        private int _currentDialogueIndex = -1;
        private bool _isNearNpc = false;

        // Encounter Animation
        private bool _isEncountering = false;
        private float _encounterTimer = 0f;
        private int _pulses = 0;
        private float _pulseScale = 1f;
        private Vector2 _enemyPosition;
        private bool _disposed = false;

        public event EventHandler? EncounterTriggered;

        public WorldScene(Microsoft.Xna.Framework.Game game, Input.InputManager inputManager, ISessionService sessionService)
        {
            _game = game;
            _inputManager = inputManager;
            _sessionService = sessionService;
            _characterPosition = new Vector2(200, 300);
            _npcPosition = new Vector2(400, 200);
            
            // Define world boundaries (fixed area)
            _worldBounds = new Rectangle(0, 0, 1280, 720);
            _exitTrigger = new Rectangle(1200, 0, 80, 720);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _characterTexture?.Dispose();
                    _characterTexture = null;
                    _npcTexture?.Dispose();
                    _npcTexture = null;
                    _pixel?.Dispose();
                    _pixel = null;
                    _font = null;
                    // Clear event invocation list
                    EncounterTriggered = null;
                }
                _disposed = true;
            }
        }

        public void LoadContent(ContentManager content)
        {
            if (content == null) return;
            
            // Safely check if graphics device is ready
            GraphicsDevice? graphicsDevice = null;
            try
            {
                var deviceService = _game?.Services?.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                graphicsDevice = deviceService?.GraphicsDevice ?? _game?.GraphicsDevice;
            }
            catch (InvalidOperationException)
            {
                // GraphicsDevice service not available yet
            }

            if (graphicsDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("[WorldScene] GraphicsDevice is not ready. Skipping LoadContent.");    
                return;
            }

            if (_pixel == null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            // Load character texture from session thumbnail if available
            if (_characterTexture == null)
            {
                if (_sessionService.CurrentCharacter?.Thumbnail != null)
                {
                    _characterTexture = LoadTextureFromBytes(graphicsDevice, _sessionService.CurrentCharacter.Thumbnail);
                }
                else
                {
                    _characterTexture = new Texture2D(graphicsDevice, 64, 64);
                    Color[] data = new Color[64 * 64];
                    for(int i=0; i<data.Length; i++) data[i] = Color.Red;
                    _characterTexture.SetData(data);
                }
            }

            // NPC texture (Blue placeholder)
            if (_npcTexture == null)
            {
                _npcTexture = new Texture2D(graphicsDevice, 64, 64);
                Color[] npcData = new Color[64 * 64];
                for(int i=0; i<npcData.Length; i++) npcData[i] = Color.Blue;
                _npcTexture.SetData(npcData);
            }

            if (_font == null && !_fontLoadAttempted)
            {
                _fontLoadAttempted = true;
                try { _font = content.Load<SpriteFont>("font"); } catch { }
            }
        }

        private Texture2D LoadTextureFromBytes(GraphicsDevice graphicsDevice, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return Texture2D.FromStream(graphicsDevice, stream);
            }
        }

        public void TriggerEncounter()
        {
            if (_isEncountering) return;
            
            _isEncountering = true;
            _encounterTimer = 0f;
            _pulses = 0;
            _pulseScale = 1f;
            _enemyPosition = _characterPosition + new Vector2(300, 0);
        }

        public void Update(GameTime gameTime)
        {
            if (_disposed) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isEncountering)
            {
                UpdateEncounterAnimation(dt);
                return;
            }

            if (_currentDialogueIndex >= 0)
            {
                if (_inputManager.IsKeyJustPressed(Keys.Space) || _inputManager.IsKeyJustPressed(Keys.Enter) || _inputManager.IsTouchJustPressed())
                {
                    _currentDialogueIndex++;
                    if (_currentDialogueIndex >= _dialogue.Length)
                        _currentDialogueIndex = -1;
                }
                return;
            }

            Vector2 movement = Vector2.Zero;

            if (_inputManager.IsKeyDown(Keys.Left) || _inputManager.IsKeyDown(Keys.A)) movement.X -= 1;
            if (_inputManager.IsKeyDown(Keys.Right) || _inputManager.IsKeyDown(Keys.D)) movement.X += 1;
            if (_inputManager.IsKeyDown(Keys.Up) || _inputManager.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (_inputManager.IsKeyDown(Keys.Down) || _inputManager.IsKeyDown(Keys.S)) movement.Y += 1;

            var activeTouches = _inputManager.GetActiveTouches();
            foreach (var touch in activeTouches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    if (touch.Position.X < _characterPosition.X) movement.X -= 1;
                    else if (touch.Position.X > _characterPosition.X + 64) movement.X += 1;
                    
                    if (touch.Position.Y < _characterPosition.Y) movement.Y -= 1;
                    else if (touch.Position.Y > _characterPosition.Y + 64) movement.Y += 1;
                }
            }

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                Vector2 newPos = _characterPosition + movement * _moveSpeed * dt;
                
                // Boundary Check
                if (newPos.X >= 0 && newPos.X <= _worldBounds.Width - 64)
                    _characterPosition.X = newPos.X;
                if (newPos.Y >= 0 && newPos.Y <= _worldBounds.Height - 64)
                    _characterPosition.Y = newPos.Y;
            }

            // NPC Proximity
            float dist = Vector2.Distance(_characterPosition, _npcPosition);
            _isNearNpc = dist < 100f;

            if (_isNearNpc && (_inputManager.IsKeyJustPressed(Keys.Space) || _inputManager.IsKeyJustPressed(Keys.Enter) || _inputManager.IsTouchJustPressed()))
            {
                _currentDialogueIndex = 0;
            }

            // Exit Trigger
            if (new Rectangle((int)_characterPosition.X, (int)_characterPosition.Y, 64, 64).Intersects(_exitTrigger))
            {
                TriggerEncounter();
            }
        }

        private void UpdateEncounterAnimation(float dt)
        {
            _encounterTimer += dt;
            float pulseDuration = 0.5f;
            if (_encounterTimer >= pulseDuration)
            {
                _encounterTimer -= pulseDuration;
                _pulses++;
                
                if (_pulses >= 3)
                {
                    _isEncountering = false;
                    EncounterTriggered?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            float progress = _encounterTimer / pulseDuration;
            _pulseScale = 1f + (float)Math.Sin(progress * Math.PI) * 0.5f;
            float totalAnimationProgress = (_pulses + progress) / 3f;
            _enemyPosition = Vector2.Lerp(_characterPosition + new Vector2(300, 0), _characterPosition, totalAnimationProgress);
        }

        public void Draw(SpriteBatch? spriteBatch)
        {
            if (_disposed || spriteBatch == null || _pixel == null) return;

            // Draw Background (Sandy Shore) - This should cover the Black clear from DarknessGame
            spriteBatch.Draw(_pixel, _worldBounds, new Color(194, 178, 128));
            
            // Draw Water (South)
            spriteBatch.Draw(_pixel, new Rectangle(0, 600, 1280, 120), new Color(0, 105, 148));

            // Draw Exit Zone (East - subtle highlight)
            spriteBatch.Draw(_pixel, _exitTrigger, new Color(Color.Black, 0.1f));

            // Draw NPC
            if (_npcTexture != null)
                spriteBatch.Draw(_npcTexture, _npcPosition, Color.White);

            // Draw Player
            if (_characterTexture != null)
                spriteBatch.Draw(_characterTexture, _characterPosition, Color.White);

            // Draw UI
            if (_isNearNpc && _currentDialogueIndex < 0 && _font != null)
            {
                spriteBatch.DrawString(_font, "Press SPACE to talk", _npcPosition - new Vector2(20, 30), Color.White);
            }

            if (_currentDialogueIndex >= 0 && _font != null)
            {
                // Dialogue Box
                Rectangle box = new Rectangle(100, 500, 1080, 150);
                spriteBatch.Draw(_pixel, box, new Color(Color.Black, 0.8f));
                spriteBatch.DrawString(_font, _npcName + ":", new Vector2(120, 510), Color.MediumPurple);
                spriteBatch.DrawString(_font, _dialogue[_currentDialogueIndex], new Vector2(120, 550), Color.White);
                spriteBatch.DrawString(_font, "[SPACE TO CONTINUE]", new Vector2(950, 620), Color.Gray);
            }

            if (_isEncountering && _pixel != null)
            {
                int size = (int)(64 * _pulseScale);
                spriteBatch.Draw(_pixel, new Rectangle((int)_enemyPosition.X - (size/2) + 32, (int)_enemyPosition.Y - (size/2) + 32, size, size), Color.Black);
            }
        }
    }
}