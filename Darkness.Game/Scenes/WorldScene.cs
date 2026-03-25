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
        private Vector2 _characterPosition;
        private readonly float _moveSpeed = 200f;

        public WorldScene(Microsoft.Xna.Framework.Game game)
        {
            _game = game;
            _characterPosition = new Vector2(100, 100);
        }

        public void LoadContent(ContentManager content)
        {
            // Placeholder: Use a basic color texture if real assets aren't loaded yet
            _characterTexture = new Texture2D(_game.GraphicsDevice, 1, 1);
            _characterTexture.SetData(new[] { Color.White });
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                _characterPosition.X -= _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                _characterPosition.X += _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                _characterPosition.Y -= _moveSpeed * dt;
            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                _characterPosition.Y += _moveSpeed * dt;
        }

        public void Draw(SpriteBatch? spriteBatch)
        {
            if (_characterTexture != null && spriteBatch != null)
            {
                // Draw a 64x64 square for the character
                spriteBatch.Draw(_characterTexture, new Rectangle((int)_characterPosition.X, (int)_characterPosition.Y, 64, 64), Color.Red);
            }
        }
    }
}
