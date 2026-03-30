using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Darkness.Core.Models;
using Darkness.Core.Logic;
using Darkness.Core.Interfaces;

namespace Darkness.Game.Scenes
{
    public class PvpScene : IDisposable
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly Input.InputManager _inputManager;
        private readonly ICombatService _combatService;
        private Character _player1;
        private Character _player2;
        private List<Character> _turnOrder;
        private int _currentTurnIndex;
        private string _battleLog = "A duel begins!";
        private bool _battleOver = false;
        private string _victoryMessage = "";

        private Texture2D? _pixel;
        private SpriteFont? _font;
        private bool _disposed = false;

        public event EventHandler? BattleEnded;

        public PvpScene(Microsoft.Xna.Framework.Game game, Input.InputManager inputManager, ICombatService combatService, Character player1, Character player2)
        {
            _game = game;
            _inputManager = inputManager;
            _combatService = combatService;
            _player1 = player1;
            _player2 = player2;
            
            // Use the combat service to calculate turn order
            _turnOrder = _combatService.CalculateTurnOrder(new List<Character> { _player1, _player2 }, new List<Enemy>())
                .Cast<Character>()
                .ToList();
            
            _currentTurnIndex = 0;
            _battleLog = $"{_turnOrder[_currentTurnIndex].Name}'s turn! Press 1 to Attack.";
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
                    _pixel?.Dispose();
                    _pixel = null;
                    _font = null;
                    // Clear event invocation list to prevent leaks
                    BattleEnded = null;
                }
                _disposed = true;
            }
        }

        public void LoadContent(ContentManager content)
        {
            if (content == null) return;
            
            var graphicsDevice = _game?.GraphicsDevice;
            if (graphicsDevice == null)
            {
                var deviceService = _game?.Services?.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                graphicsDevice = deviceService?.GraphicsDevice;
            }

            if (graphicsDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("[PvpScene] GraphicsDevice is not ready. Skipping LoadContent.");       
                return;
            }

            if (_pixel == null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            
            if (_font == null)
            {
                try 
                {
                    _font = content.Load<SpriteFont>("font");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PvpScene] Failed to load font: {ex.Message}");
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_disposed) return;

            if (_battleOver)
            {
                if (_inputManager.IsKeyJustPressed(Keys.Enter) || _inputManager.IsTouchJustPressed())
                {
                    BattleEnded?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            var currentPlayer = _turnOrder[_currentTurnIndex];
            var opponent = _turnOrder[(_currentTurnIndex + 1) % 2];

            if (_inputManager.IsKeyJustPressed(Keys.D1) || _inputManager.IsTouchJustPressed()) 
            {
                ExecuteAttack(currentPlayer, opponent);
            }
        }

        private void ExecuteAttack(Character attacker, Character defender)
        {
            if (_battleOver) return;

            int damage = _combatService.CalculateDamage(attacker, defender);
            defender.CurrentHP -= damage;
            _battleLog = $"{attacker.Name} attacks {defender.Name} for {damage} damage!";

            if (defender.CurrentHP <= 0)
            {
                defender.CurrentHP = 0;
                _battleOver = true;
                _victoryMessage = $"{attacker.Name} is victorious!";
            }
            else
            {
                _currentTurnIndex = (_currentTurnIndex + 1) % 2;
                _battleLog += $"\n{_turnOrder[_currentTurnIndex].Name}'s turn!";
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_disposed || _pixel == null) return;

            // Draw Background (Dark Blue-Gray for PvP)
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, _game.GraphicsDevice.Viewport.Width, _game.GraphicsDevice.Viewport.Height), new Color(20, 20, 40));

            // Draw Players Health Bars
            DrawHealthBar(spriteBatch, new Vector2(50, 100), _player1.Name, _player1.CurrentHP, _player1.MaxHP, Color.Blue);
            DrawHealthBar(spriteBatch, new Vector2(450, 100), _player2.Name, _player2.CurrentHP, _player2.MaxHP, Color.Red);

            // Draw Battle Log and Instructions
            if (_font != null)
            {
                spriteBatch.DrawString(_font, "LOCAL PVP (HOT-SEAT)", new Vector2(50, 30), Color.Gold);
                spriteBatch.DrawString(_font, _battleLog, new Vector2(50, 300), Color.White);

                if (_battleOver)
                {
                    spriteBatch.DrawString(_font, _victoryMessage, new Vector2(50, 400), Color.Gold);
                    spriteBatch.DrawString(_font, "Press ENTER to return to menu...", new Vector2(50, 450), Color.White);
                }
                else
                {
                    spriteBatch.DrawString(_font, $"Current Turn: {_turnOrder[_currentTurnIndex].Name}", new Vector2(50, 250), Color.Cyan);
                    spriteBatch.DrawString(_font, "Press 1 to Attack", new Vector2(50, 380), Color.LightBlue);
                }
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Vector2 position, string name, int current, int max, Color color)
        {
            if (_pixel == null) return;

            float percentage = (float)current / max;
            int barWidth = 300;
            int barHeight = 30;

            if (_font != null)
            {
                spriteBatch.DrawString(_font, $"{name}", position - new Vector2(0, 35), Color.White);
                spriteBatch.DrawString(_font, $"{current}/{max}", position + new Vector2(barWidth / 2 - 20, 5), Color.White);
            }

            // Background of bar
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight), Color.DarkGray);
            // Foreground of bar
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(barWidth * percentage), barHeight), color);
        }
    }
}
