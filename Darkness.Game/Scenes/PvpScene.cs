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
    public class PvpScene
    {
        private readonly Microsoft.Xna.Framework.Game _game;
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

        public event EventHandler? BattleEnded;

        public PvpScene(Microsoft.Xna.Framework.Game game, Character player1, Character player2)
        {
            _game = game;
            _combatService = new CombatEngine();
            _player1 = player1;
            _player2 = player2;
            
            // For PvP, we use a simplified turn order calculation for now
            _turnOrder = new List<Character> { _player1, _player2 }
                .OrderByDescending(c => c.Dexterity + c.Speed + new Random().Next(1, 11))
                .ToList();
            
            _currentTurnIndex = 0;
            _battleLog = $"{_turnOrder[_currentTurnIndex].Name}'s turn! Press 1 to Attack.";
        }

        public void LoadContent(ContentManager content)
        {
            _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            
            try 
            {
                _font = content.Load<SpriteFont>("font");
            }
            catch
            {
                // Fallback handled in Draw
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_battleOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    BattleEnded?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            var currentPlayer = _turnOrder[_currentTurnIndex];
            var opponent = _turnOrder[(_currentTurnIndex + 1) % 2];

            var kState = Keyboard.GetState();
            if (kState.IsKeyDown(Keys.D1)) 
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
            if (_pixel == null) return;

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
