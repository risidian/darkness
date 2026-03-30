using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Darkness.Core.Models;
using Darkness.Game.Scenes;
using Darkness.Game.Input;
using Darkness.Core.Interfaces;
using Xunit;
using Moq;
using Microsoft.Xna.Framework.Content;

namespace Darkness.Tests.Scenes
{
    public class BattleSceneTests
    {
        [Fact]
        public void LoadContent_ShouldNotThrow_WhenGraphicsDeviceIsNull()
        {
            // Arrange
            var mockGame = new Mock<Microsoft.Xna.Framework.Game>();
            var party = new List<Character> { new Character { Name = "Hero", MaxHP = 100, CurrentHP = 100 } };
            var enemies = new List<Enemy> { new Enemy { Name = "Enemy", MaxHP = 50, CurrentHP = 50 } };
            var inputManager = new InputManager();
            var scene = new BattleScene(mockGame.Object, inputManager, party, enemies);            
            var mockServiceProvider = new Mock<IServiceProvider>();
            var content = new ContentManager(mockServiceProvider.Object);

            // Act & Assert
            var exception = Record.Exception(() => scene.LoadContent(content));
            Assert.Null(exception);
        }

        [Fact]
        public void Update_ShouldDecrementTimer_WhenInDelayingState()
        {
            // Arrange
            var mockGame = new Mock<Microsoft.Xna.Framework.Game>();
            var party = new List<Character> { new Character { Name = "Hero", MaxHP = 100, CurrentHP = 100 } };
            var enemies = new List<Enemy> { new Enemy { Name = "Enemy", MaxHP = 50, CurrentHP = 50 } };
            var inputManager = new InputManager();
            var scene = new BattleScene(mockGame.Object, inputManager, party, enemies);

            // Set state to Delaying and timer to 1.0 via reflection
            SetPrivateField(scene, "_turnState", "Delaying");
            SetPrivateField(scene, "_turnDelayTimer", 1.0);

            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5));

            // Act
            scene.Update(gameTime);

            // Assert
            var timerValue = (double)GetPrivateField(scene, "_turnDelayTimer");
            Assert.Equal(0.5, timerValue, 3);
        }

        [Fact]
        public void Update_ShouldTransitionFromDelayingToNextTurn_WhenTimerExpires()
        {
            // Arrange
            var mockGame = new Mock<Microsoft.Xna.Framework.Game>();
            // Hero with high dexterity to ensure it goes first
            var party = new List<Character> { new Character { Name = "Hero", MaxHP = 100, CurrentHP = 100, Dexterity = 100, Speed = 100 } };
            var enemies = new List<Enemy> { new Enemy { Name = "Enemy", MaxHP = 50, CurrentHP = 50, DEX = 0, Speed = 0 } };
            var inputManager = new InputManager();
            var scene = new BattleScene(mockGame.Object, inputManager, party, enemies);

            // Initial state is PlayerTurn
            Assert.Equal("PlayerTurn", GetPrivateField(scene, "_turnState").ToString());

            // Set state to Delaying and timer to 0.1
            SetPrivateField(scene, "_turnState", "Delaying");
            SetPrivateField(scene, "_turnDelayTimer", 0.1);
            SetPrivateField(scene, "_currentTurnIndex", 0); // Hero's turn

            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.2));

            // Act
            scene.Update(gameTime);

            // Assert
            // After timer expires, NextParticipant should be called, moving to next participant (Enemy)
            // Enemy turn will set state to EnemyAction
            var state = GetPrivateField(scene, "_turnState").ToString();
            Assert.Equal("EnemyAction", state);
            Assert.Equal(1, (int)GetPrivateField(scene, "_currentTurnIndex"));
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found");
            
            if (field.FieldType.IsEnum)
            {
                if (value is string stringValue)
                {
                    field.SetValue(obj, Enum.Parse(field.FieldType, stringValue));
                }
                else
                {
                    field.SetValue(obj, Enum.ToObject(field.FieldType, value));
                }
            }
            else
            {
                field.SetValue(obj, value);
            }
        }

        private object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found");
            return field.GetValue(obj)!;
        }
    }
}
