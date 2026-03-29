using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Darkness.Core.Models;
using Darkness.Game.Scenes;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
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
            // By default, a mock's properties return null unless setup,
            // so mockGame.Object.GraphicsDevice will be null.

            var party = new List<Character> { new Character { Name = "Test Hero", MaxHP = 100, CurrentHP = 100 } };
            var enemies = new List<Enemy> { new Enemy { Name = "Test Enemy", MaxHP = 50, CurrentHP = 50 } };
            
            var scene = new BattleScene(mockGame.Object, party, enemies);
            
            // We need a ContentManager, but we don't need a real one for this test since it should return early
            var mockServiceProvider = new Mock<System.IServiceProvider>();
            var content = new ContentManager(mockServiceProvider.Object);

            // Act & Assert
            // This should not throw NullReferenceException
            var exception = Record.Exception(() => scene.LoadContent(content));
            
            Assert.Null(exception);
        }
    }
}
