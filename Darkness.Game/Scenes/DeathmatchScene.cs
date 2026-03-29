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
    public class DeathmatchScene
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly ICombatService _combatService;
        private readonly DeathmatchEncounter _encounter;
        private List<Character> _party;
        private List<Enemy> _enemies;
        private List<object> _turnOrder;
        private int _currentTurnIndex;
        private bool _isPlayerTurn;
        private string _battleLog = "A deathmatch begins!";
        private bool _battleOver = false;
        private string _victoryMessage = "";

        private Texture2D? _pixel;
        private SpriteFont? _font;

        public event EventHandler? BattleEnded;

        public DeathmatchScene(Microsoft.Xna.Framework.Game game, List<Character> party, DeathmatchEncounter encounter)
        {
            _game = game;
            _combatService = new CombatEngine();
            _party = party;
            _encounter = encounter;
            _enemies = encounter.Enemies;
            _turnOrder = _combatService.CalculateTurnOrder(_party, _enemies);
            _currentTurnIndex = 0;
            DetermineTurn();
        }

        private void DetermineTurn()
        {
            if (_battleOver) return;

            var currentParticipant = _turnOrder[_currentTurnIndex];
            if (currentParticipant is Character character)
            {
                if (character.CurrentHP > 0)
                {
                    _isPlayerTurn = true;
                    _battleLog = $"{character.Name}'s turn! Choose an action.";
                }
                else
                {
                    NextParticipant();
                }
            }
            else if (currentParticipant is Enemy enemy)
            {
                _isPlayerTurn = false;
                if (enemy.CurrentHP > 0)
                {
                    ExecuteEnemyTurn(enemy);
                }
                else
                {
                    NextParticipant();
                }
            }
        }

        private void ExecuteEnemyTurn(Enemy enemy)
        {
            var livingParty = _party.Where(p => p.CurrentHP > 0).ToList();
            if (livingParty.Count == 0)
            {
                _battleOver = true;
                _victoryMessage = "The party has fallen...";
                return;
            }

            var target = livingParty[new Random().Next(livingParty.Count)];
            int damage = _combatService.CalculateDamage(enemy, target);
            target.CurrentHP -= damage;
            _battleLog = $"{enemy.Name} attacks {target.Name} for {damage} damage!";

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                _battleLog += $" {target.Name} falls!";
            }

            if (_party.All(p => p.CurrentHP <= 0))
            {
                _battleOver = true;
                _victoryMessage = "You have been defeated...";
            }
            else
            {
                NextParticipant();
            }
        }

        private void NextParticipant()
        {
            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
            DetermineTurn();
        }

        public void LoadContent(ContentManager content)
        {
            if (_game == null || _game.GraphicsDevice == null || content == null)
            {
                System.Diagnostics.Debug.WriteLine("[DeathmatchScene] GraphicsDevice or Content is not ready. Skipping LoadContent.");
                return;
            }

            _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            
            try 
            {
                _font = content.Load<SpriteFont>("font");
            }
            catch
            {
                // Fallback font loading here if necessary
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

            if (_isPlayerTurn)
            {
                var currentParticipant = _turnOrder[_currentTurnIndex] as Character;
                if (currentParticipant == null) return;

                var kState = Keyboard.GetState();
                if (kState.IsKeyDown(Keys.D1))
                {
                    ExecutePlayerAttack(currentParticipant, 0);
                }
                else if (kState.IsKeyDown(Keys.D2) && _enemies.Count > 1)
                {
                    ExecutePlayerAttack(currentParticipant, 1);
                }
                else if (kState.IsKeyDown(Keys.D3) && _enemies.Count > 2)
                {
                    ExecutePlayerAttack(currentParticipant, 2);
                }
            }
        }

        private void ExecutePlayerAttack(Character attacker, int enemyIndex)
        {
            if (enemyIndex >= _enemies.Count || _enemies[enemyIndex].CurrentHP <= 0) return;

            var target = _enemies[enemyIndex];
            int damage = _combatService.CalculateDamage(attacker, target);
            
            target.CurrentHP -= damage;
            _battleLog = $"{attacker.Name} attacks {target.Name} for {damage} damage!";

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                _battleLog += $" {target.Name} is defeated!";
            }

            if (_enemies.All(e => e.CurrentHP <= 0))
            {
                _battleOver = true;
                _victoryMessage = $"Victory! All enemies defeated. You gained {_encounter.Rewards.Count} items!";
            }
            else
            {
                _isPlayerTurn = false;
                NextParticipant();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_pixel == null) return;

            spriteBatch.Draw(_pixel, new Rectangle(0, 0, _game.GraphicsDevice.Viewport.Width, _game.GraphicsDevice.Viewport.Height), new Color(30, 10, 10)); // Dark Reddish Background for Deathmatch

            for (int i = 0; i < _party.Count; i++)
            {
                var member = _party[i];
                DrawHealthBar(spriteBatch, new Vector2(50, 50 + (i * 80)), member.Name, member.CurrentHP, member.MaxHP, Color.Green);
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                Color barColor = enemy.CurrentHP > 0 ? Color.Red : Color.Gray;
                DrawHealthBar(spriteBatch, new Vector2(400, 50 + (i * 80)), $"{enemy.Name} ({i + 1})", enemy.CurrentHP, enemy.MaxHP, barColor);
            }

            if (_font != null)
            {
                spriteBatch.DrawString(_font, $"Deathmatch: {_encounter.Name}", new Vector2(50, 20), Color.Yellow);
                spriteBatch.DrawString(_font, _battleLog, new Vector2(50, 400), Color.White);
                
                if (_battleOver)
                {
                    spriteBatch.DrawString(_font, _victoryMessage, new Vector2(50, 450), Color.Gold);
                    spriteBatch.DrawString(_font, "Press ENTER to continue...", new Vector2(50, 500), Color.White);
                }
                else if (_isPlayerTurn)
                {
                    spriteBatch.DrawString(_font, "Press 1, 2, or 3 to attack enemies", new Vector2(50, 450), Color.LightBlue);
                }
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Vector2 position, string name, int current, int max, Color color)
        {
            if (_pixel == null) return;

            float percentage = (float)current / max;
            int barWidth = 200;
            int barHeight = 20;

            if (_font != null)
            {
                spriteBatch.DrawString(_font, $"{name}: {current}/{max}", position - new Vector2(0, 25), Color.White);
            }

            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight), Color.DarkGray);
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(barWidth * percentage), barHeight), color);
        }
    }
}
