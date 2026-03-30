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
using Microsoft.Xna.Framework.Graphics;

namespace Darkness.Tests.Scenes
{
    public class SceneDisposalTests
    {
        [Fact]
        public void BattleScene_Dispose_ShouldNullifyResourcesAndClearEvents()
        {
            // Arrange
            // Pass null for Game and InputManager to avoid SDL initialization in headless environment
            var scene = (BattleScene)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(BattleScene));

            // Set some dummy resources via reflection
            // We use GetUninitializedObject for Texture2D to avoid needing a GraphicsDevice
            var dummyTexture = (Texture2D)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Texture2D));
            SetPrivateField(scene, "_pixel", dummyTexture);
            
            // Register an event handler to check if it's cleared
            scene.BattleEnded += (s, e) => { };

            // Act
            scene.Dispose();

            // Assert
            Assert.Null(GetPrivateField(scene, "_pixel"));
            Assert.Null(GetPrivateField(scene, "_font"));
            Assert.True((bool)GetPrivateField(scene, "_disposed")!);
            Assert.Null(GetPrivateField(scene, "BattleEnded")); // Events are backed by a field of the same name
        }

        [Fact]
        public void WorldScene_Dispose_ShouldNullifyResourcesAndClearEvents()
        {
            // Arrange
            var scene = (WorldScene)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(WorldScene));

            var dummyTexture = (Texture2D)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Texture2D));
            SetPrivateField(scene, "_characterTexture", dummyTexture);
            SetPrivateField(scene, "_npcTexture", dummyTexture);
            SetPrivateField(scene, "_pixel", dummyTexture);

            scene.EncounterTriggered += (s, e) => { };

            // Act
            scene.Dispose();

            // Assert
            Assert.Null(GetPrivateField(scene, "_characterTexture"));
            Assert.Null(GetPrivateField(scene, "_npcTexture"));
            Assert.Null(GetPrivateField(scene, "_pixel"));
            Assert.Null(GetPrivateField(scene, "_font"));
            Assert.True((bool)GetPrivateField(scene, "_disposed")!);
            Assert.Null(GetPrivateField(scene, "EncounterTriggered"));
        }

        [Fact]
        public void DeathmatchScene_Dispose_ShouldNullifyResourcesAndClearEvents()
        {
            // Arrange
            var scene = (DeathmatchScene)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DeathmatchScene));

            var dummyTexture = (Texture2D)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Texture2D));
            SetPrivateField(scene, "_pixel", dummyTexture);

            scene.BattleEnded += (s, e) => { };

            // Act
            scene.Dispose();

            // Assert
            Assert.Null(GetPrivateField(scene, "_pixel"));
            Assert.Null(GetPrivateField(scene, "_font"));
            Assert.True((bool)GetPrivateField(scene, "_disposed")!);
            Assert.Null(GetPrivateField(scene, "BattleEnded"));
        }

        [Fact]
        public void PvpScene_Dispose_ShouldNullifyResourcesAndClearEvents()
        {
            // Arrange
            var scene = (PvpScene)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PvpScene));

            var dummyTexture = (Texture2D)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Texture2D));
            SetPrivateField(scene, "_pixel", dummyTexture);

            scene.BattleEnded += (s, e) => { };

            // Act
            scene.Dispose();

            // Assert
            Assert.Null(GetPrivateField(scene, "_pixel"));
            Assert.Null(GetPrivateField(scene, "_font"));
            Assert.True((bool)GetPrivateField(scene, "_disposed")!);
            Assert.Null(GetPrivateField(scene, "BattleEnded"));
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found in {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        private object? GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found in {obj.GetType().Name}");
            return field.GetValue(obj);
        }
    }
}
