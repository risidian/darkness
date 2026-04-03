using Darkness.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Darkness.Tests.Models
{
    public class QuestModelTests
    {
        [Fact]
        public void EncounterData_CanBeInstantiated()
        {
            var encounter = new EncounterData
            {
                SurvivalTurns = 5,
                BackgroundKey = "forest_night",
                Enemies = new List<Enemy> { new Enemy { Name = "Wolf" } },
                Rewards = new List<Item> { new Item { Name = "Wolf Pelt" } }
            };

            Assert.Equal(5, encounter.SurvivalTurns);
            Assert.Equal("forest_night", encounter.BackgroundKey);
            Assert.Single(encounter.Enemies);
            Assert.Single(encounter.Rewards);
        }

        [Fact]
        public void QuestNode_CanBeInstantiated()
        {
            var node = new QuestNode
            {
                Id = "quest_01",
                Title = "The First Step",
                IsMainStory = true,
                Prerequisites = new List<string> { "intro" },
                Encounter = new EncounterData(),
                DialogueKey = "quest_01_dialog"
            };

            Assert.Equal("quest_01", node.Id);
            Assert.Equal("The First Step", node.Title);
            Assert.True(node.IsMainStory);
            Assert.Single(node.Prerequisites);
            Assert.NotNull(node.Encounter);
            Assert.Equal("quest_01_dialog", node.DialogueKey);
        }

        [Fact]
        public void Character_HasCompletedQuestIds()
        {
            var character = new Character
            {
                CompletedQuestIds = new List<string> { "quest_01" }
            };

            Assert.Single(character.CompletedQuestIds);
            Assert.Equal("quest_01", character.CompletedQuestIds[0]);
        }
    }
}
