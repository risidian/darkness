using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Darkness.Game.Scenes
{
    public class WorldScene
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private Texture2D? _characterTexture;
        private Texture2D? _enemyTexture;
        private Vector2 _characterPosition;
        private readonly float _moveSpeed = 200f;

        // Encounter Animation
        private bool _isEncountering = false;
        private float _encounterTimer = 0f;
        private int _pulses = 0;
        private float _pulseScale = 1f;
        private Vector2 _enemyPosition;

        public event EventHandler? EncounterTriggered;

        public WorldScene(Microsoft.Xna.Framework.Game game)
        {
            _game = game;
            _characterPosition = new Vector2(100, 100);
        }

        public void LoadContent(ContentManager content)
        {
            _characterTexture = new Texture2D(_game.GraphicsDevice, 1, 1);
            _characterTexture.SetData(new[] { Color.White });

            _enemyTexture = new Texture2D(_game.GraphicsDevice, 1, 1);
            _enemyTexture.SetData(new[] { Color.White });
        }

        public void TriggerEncounter()
        {
            if (_isEncountering) return;
            
            _isEncountering = true;
            _encounterTimer = 0f;
            _pulses = 0;
            _pulseScale = 1f;
            
            // Spawn enemy at a distance
            _enemyPosition = _characterPosition + new Vector2(300, 0);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isEncountering)
            {
                UpdateEncounterAnimation(dt);
                return;
            }

            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                _characterPosition.X -= _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                _characterPosition.X += _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                _characterPosition.Y -= _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                _characterPosition.Y += _moveSpeed * dt;

            // Random encounter test (Press E to trigger)
            if (keyboardState.IsKeyDown(Keys.E))
            {
                TriggerEncounter();
            }
        }

        private void UpdateEncounterAnimation(float dt)
        {
            _encounterTimer += dt;
            
            // Each pulse is 0.5 seconds
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

            // Pulse effect: scale up and then down
            float progress = _encounterTimer / pulseDuration;
            _pulseScale = 1f + (float)Math.Sin(progress * Math.PI) * 0.5f;

            // Bring enemy closer to center (or character) with each pulse
            Vector2 targetPos = _characterPosition;
            float totalAnimationProgress = (_pulses + progress) / 3f;
            _enemyPosition = Vector2.Lerp(_characterPosition + new Vector2(300, 0), targetPos, totalAnimationProgress);
        }

        public void Draw(SpriteBatch? spriteBatch)
        {
            if (spriteBatch == null) return;

            if (_characterTexture != null)
            {
                spriteBatch.Draw(_characterTexture, new Rectangle((int)_characterPosition.X, (int)_characterPosition.Y, 64, 64), Color.Red);
            }

            if (_isEncountering && _enemyTexture != null)
            {
                // Draw pulsing enemy
                int size = (int)(64 * _pulseScale);
                spriteBatch.Draw(_enemyTexture, new Rectangle((int)_enemyPosition.X - (size/2) + 32, (int)_enemyPosition.Y - (size/2) + 32, size, size), Color.Black);
            }
        }
    }
}
