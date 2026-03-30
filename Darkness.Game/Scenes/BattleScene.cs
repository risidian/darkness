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
    public class BattleScene : IDisposable
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly ICombatService _combatService;
        private readonly Input.InputManager _inputManager;
        private List<Character> _party;
        private List<Enemy> _enemies;
        private List<object> _turnOrder;
        private int _currentTurnIndex;
        
        private enum BattleState
        {
            PlayerTurn,
            EnemyAction,
            Delaying
        }
        private BattleState _turnState;
        private double _turnDelayTimer;

        private string _battleLog = "A battle begins!";
        private bool _battleOver = false;
        private string _victoryMessage = "";

        private int? _survivalTurns;
        private int _roundsCompleted = 0;
        private int _participantsActedThisRound = 0;

        private Texture2D? _pixel;
        private SpriteFont? _font;
        private bool _disposed = false;

        public event EventHandler? BattleEnded;

        public BattleScene(Microsoft.Xna.Framework.Game game, Input.InputManager inputManager, List<Character> party, List<Enemy> enemies, int? survivalTurns = null)
        {
            _game = game;
            _combatService = new CombatEngine();
            _inputManager = inputManager;
            _party = party;
            _enemies = enemies;
            _survivalTurns = survivalTurns;
            _turnOrder = _combatService.CalculateTurnOrder(_party, _enemies);
            _currentTurnIndex = 0;

            if (_turnOrder.Count == 0)
            {
                _battleOver = true;
                _victoryMessage = "The battlefield is silent...";
                return;
            }

            DetermineTurn();
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

        private Rectangle GetButtonRegion(int index)
        {
            var viewport = _game.GraphicsDevice.Viewport;
            int buttonWidth = (int)(viewport.Width * 0.25f);
            int buttonHeight = (int)(viewport.Height * 0.15f);
            int spacing = (int)(viewport.Width * 0.05f);
            int startX = (int)(viewport.Width * 0.05f);
            int y = (int)(viewport.Height * 0.75f);

            return new Rectangle(startX + index * (buttonWidth + spacing), y, buttonWidth, buttonHeight);
        }

        private bool IsParticipantAlive(object participant)
        {
            if (participant is Character character) return character.CurrentHP > 0;
            if (participant is Enemy enemy) return enemy.CurrentHP > 0;
            return false;
        }

        private void DetermineTurn()
        {
            if (_battleOver || _turnOrder.Count == 0) return;

            int initialIndex = _currentTurnIndex;
            int participantsChecked = 0;

            while (!IsParticipantAlive(_turnOrder[_currentTurnIndex]))
            {
                // Current participant is incapacitated, move to next
                _participantsActedThisRound++;
                if (_participantsActedThisRound >= _turnOrder.Count)
                {
                    _participantsActedThisRound = 0;
                    _roundsCompleted++;
                    if (_survivalTurns.HasValue && _roundsCompleted >= _survivalTurns.Value)
                    {
                        _battleOver = true;
                        _victoryMessage = "You survived! The enemies vanish into the darkness.";
                        return;
                    }
                }

                _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
                participantsChecked++;

                if (participantsChecked >= _turnOrder.Count)
                {
                    _battleOver = true;
                    _victoryMessage = "The battle has ended.";
                    return;
                }
            }

            var currentParticipant = _turnOrder[_currentTurnIndex];
            if (currentParticipant is Character character)
            {
                _turnState = BattleState.PlayerTurn;
                _battleLog = $"{character.Name}'s turn! Choose an action.";
            }
            else if (currentParticipant is Enemy enemy)
            {
                _turnState = BattleState.EnemyAction;
            }
        }

        private void ExecuteEnemyTurn(Enemy enemy)
        {
            // Simple AI: attack a random living party member
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
                _turnState = BattleState.Delaying;
                _turnDelayTimer = 1.5;
            }
        }

        private void NextParticipant()
        {
            if (_battleOver || _turnOrder.Count == 0) return;

            _participantsActedThisRound++;
            if (_participantsActedThisRound >= _turnOrder.Count)
            {
                _participantsActedThisRound = 0;
                _roundsCompleted++;
                if (_survivalTurns.HasValue && _roundsCompleted >= _survivalTurns.Value)
                {
                    _battleOver = true;
                    _victoryMessage = "You survived! The enemies vanish into the darkness.";
                    return;
                }
            }

            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
            DetermineTurn();
        }

        public void LoadContent(ContentManager content)
        {
            if (content == null) return;
            
            // Safely check if graphics device is ready without triggering InvalidOperationException
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
                System.Diagnostics.Debug.WriteLine("[BattleScene] GraphicsDevice is not ready. Skipping LoadContent.");    
                return;
            }

            if (_pixel == null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            
            if (_font == null)
            {
                // Try to load a font, fallback to a default if not found
                try 
                {
                    _font = content.Load<SpriteFont>("font");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleScene] Failed to load font: {ex.Message}");
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

            if (_turnState == BattleState.Delaying)
            {
                _turnDelayTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_turnDelayTimer <= 0)
                {
                    NextParticipant();
                }
                return;
            }

            if (_turnState == BattleState.EnemyAction)
            {
                var enemy = _turnOrder[_currentTurnIndex] as Enemy;
                if (enemy != null)
                {
                    ExecuteEnemyTurn(enemy);
                }
                return;
            }

            if (_turnState == BattleState.PlayerTurn)
            {
                var currentParticipant = _turnOrder[_currentTurnIndex] as Character;
                if (currentParticipant == null) return;

                int enemyIndex = -1;

                if (_inputManager.IsKeyJustPressed(Keys.D1)) enemyIndex = 0;
                else if (_inputManager.IsKeyJustPressed(Keys.D2)) enemyIndex = 1;
                else if (_inputManager.IsKeyJustPressed(Keys.D3)) enemyIndex = 2;

                if (enemyIndex != -1)
                {
                    ExecutePlayerAttack(currentParticipant, enemyIndex);
                }
                else if (_inputManager.IsTouchJustPressed())
                {
                    // Check button regions with multi-touch support
                    if (_inputManager.IsRegionJustPressed(GetButtonRegion(0))) ExecutePlayerAttack(currentParticipant, 0);
                    else if (_inputManager.IsRegionJustPressed(GetButtonRegion(1))) ExecutePlayerAttack(currentParticipant, 1);
                    else if (_inputManager.IsRegionJustPressed(GetButtonRegion(2))) ExecutePlayerAttack(currentParticipant, 2);
                }
            }
        }

        private void ExecutePlayerAttack(Character attacker, int enemyIndex)
        {
            if (enemyIndex >= _enemies.Count || _enemies[enemyIndex].CurrentHP <= 0) return;

            var target = _enemies[enemyIndex];
            int damage = _combatService.CalculateDamage(attacker, target);
            
            if (target.IsInvincible)
            {
                damage = 0;
                _battleLog = $"{attacker.Name} attacks {target.Name}, but it has no effect!";
            }
            else
            {
                target.CurrentHP -= damage;
                _battleLog = $"{attacker.Name} attacks {target.Name} for {damage} damage!";
            }

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                _battleLog += $" {target.Name} is defeated!";
            }

            if (_enemies.All(e => e.CurrentHP <= 0))
            {
                _battleOver = true;
                _victoryMessage = "Victory! All enemies defeated.";
            }
            else
            {
                _turnState = BattleState.Delaying;
                _turnDelayTimer = 1.0;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_disposed || _pixel == null) return;

            // Draw Background (Dark Gray)
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, _game.GraphicsDevice.Viewport.Width, _game.GraphicsDevice.Viewport.Height), new Color(20, 20, 20));

            // Draw Party Health Bars
            for (int i = 0; i < _party.Count; i++)
            {
                var member = _party[i];
                DrawHealthBar(spriteBatch, new Vector2(50, 50 + (i * 80)), member.Name, member.CurrentHP, member.MaxHP, Color.Green);
            }

            // Draw Enemies Health Bars
            for (int i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                Color barColor = enemy.CurrentHP > 0 ? Color.Red : Color.Gray;
                DrawHealthBar(spriteBatch, new Vector2(400, 50 + (i * 80)), $"{enemy.Name} ({i + 1})", enemy.CurrentHP, enemy.MaxHP, barColor);
            }

            // Draw Battle Log and Instructions
            if (_font != null)
            {
                spriteBatch.DrawString(_font, _battleLog, new Vector2(50, 400), Color.White);
                
                if (_survivalTurns.HasValue && !_battleOver)
                {
                    spriteBatch.DrawString(_font, $"Survival: Round {_roundsCompleted + 1}/{_survivalTurns}", new Vector2(50, 370), Color.Orange);
                }

                if (_battleOver)
                {
                    spriteBatch.DrawString(_font, _victoryMessage, new Vector2(50, 450), Color.Gold);
                    spriteBatch.DrawString(_font, "Press ENTER to continue...", new Vector2(50, 500), Color.White);
                }
                else if (_turnState == BattleState.PlayerTurn)
                {
                    spriteBatch.DrawString(_font, "Press 1, 2, or 3 to attack enemies", new Vector2(50, 450), Color.LightBlue);
                    
                    // Draw Attack Buttons
                    for (int i = 0; i < Math.Min(3, _enemies.Count); i++)
                    {
                        var region = GetButtonRegion(i);
                        Color buttonColor = _enemies[i].CurrentHP > 0 ? Color.DarkRed : Color.DimGray;

                        if (_enemies[i].CurrentHP > 0 && _inputManager.IsRegionTouched(region))
                        {
                            buttonColor = Color.Red;
                        }

                        spriteBatch.Draw(_pixel, region, buttonColor);
                        spriteBatch.DrawString(_font, $"Attack {i + 1}", new Vector2(region.X + 10, region.Y + 10), Color.White);
                    }
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

            // Background of bar
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight), Color.DarkGray);
            // Foreground of bar
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(barWidth * percentage), barHeight), color);
        }
    }
}
