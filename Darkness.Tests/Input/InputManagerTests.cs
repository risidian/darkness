using Xunit;
using Microsoft.Xna.Framework.Input;
using Darkness.Game.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Darkness.Tests.Input
{
    public class InputManagerTests
    {
        [Fact]
        public void IsKeyJustPressed_ShouldReturnTrue_WhenKeyIsPressedThisFrameButNotLast()
        {
            // Arrange
            var keysPressedThisFrame = new List<Keys> { Keys.Space };
            var inputManager = new InputManager(
                () => new KeyboardState(keysPressedThisFrame.ToArray())
            );

            // First update: Last = None, Current = Space
            inputManager.Update();

            // Act
            bool result = inputManager.IsKeyJustPressed(Keys.Space);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsKeyJustPressed_ShouldReturnFalse_WhenKeyIsPressedBothFrames()
        {
            // Arrange
            var keysPressed = new List<Keys> { Keys.Space };
            var inputManager = new InputManager(
                () => new KeyboardState(keysPressed.ToArray())
            );

            // First update: Last = None, Current = Space
            inputManager.Update();
            
            // Second update: Last = Space, Current = Space
            inputManager.Update();

            // Act
            bool result = inputManager.IsKeyJustPressed(Keys.Space);

            // Assert
            Assert.False(result); // Should be false because it was already pressed
        }

        [Fact]
        public void IsTouchJustPressed_ShouldReturnTrue_WhenTouchStateIsPressed()
        {
            // Arrange
            var touchPosition = new Vector2(100, 200);
            var touchLocations = new[] { new TouchLocation(1, TouchLocationState.Pressed, touchPosition) };
            var inputManager = new InputManager(
                null,
                () => new TouchCollection(touchLocations)
            );

            // Act
            inputManager.Update();
            bool result = inputManager.IsTouchJustPressed();

            // Assert
            Assert.True(result);
            Assert.Equal(touchPosition, inputManager.GetTouchPosition());
        }

        [Fact]
        public void IsTouchJustPressed_ShouldReturnFalse_WhenNoTouch()
        {
            // Arrange
            var inputManager = new InputManager(
                null,
                () => new TouchCollection()
            );

            // Act
            inputManager.Update();
            bool result = inputManager.IsTouchJustPressed();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetTouchPosition_ShouldReturnZero_WhenNoTouchesArePresent()
        {
            // Arrange
            var inputManager = new InputManager(
                null,
                () => new TouchCollection()
            );

            // Act
            inputManager.Update();
            var result = inputManager.GetTouchPosition();

            // Assert
            Assert.Equal(Vector2.Zero, result);
        }

        [Fact]
        public void IsRegionJustPressed_ShouldReturnTrue_WhenTouchIsInsideRegion()
        {
            // Arrange
            var region = new Rectangle(50, 50, 100, 100);
            var touchPosition = new Vector2(75, 75);
            var touchLocations = new[] { new TouchLocation(1, TouchLocationState.Pressed, touchPosition) };
            var inputManager = new InputManager(
                null,
                () => new TouchCollection(touchLocations)
            );

            // Act
            inputManager.Update();
            bool result = inputManager.IsRegionJustPressed(region);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRegionJustPressed_ShouldReturnFalse_WhenTouchIsOutsideRegion()
        {
            // Arrange
            var region = new Rectangle(50, 50, 100, 100);
            var touchPosition = new Vector2(200, 200);
            var touchLocations = new[] { new TouchLocation(1, TouchLocationState.Pressed, touchPosition) };
            var inputManager = new InputManager(
                null,
                () => new TouchCollection(touchLocations)
            );

            // Act
            inputManager.Update();
            bool result = inputManager.IsRegionJustPressed(region);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRegionTouched_ShouldReturnTrue_WhenAnyTouchIsInsideRegion()
        {
            // Arrange
            var region = new Rectangle(50, 50, 100, 100);
            var touchLocations = new[] {
                new TouchLocation(1, TouchLocationState.Pressed, new Vector2(10, 10)),
                new TouchLocation(2, TouchLocationState.Moved, new Vector2(75, 75))
            };
            var inputManager = new InputManager(
                null,
                () => new TouchCollection(touchLocations)
            );

            // Act
            inputManager.Update();
            bool result = inputManager.IsRegionTouched(region);

            // Assert
            Assert.True(result);
        }
    }
}
