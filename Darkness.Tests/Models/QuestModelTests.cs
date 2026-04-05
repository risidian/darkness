using Darkness.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Darkness.Tests.Models
{
    public class QuestModelTests
    {
        [Fact]
        public void CombatData_CanBeInstantiated()
        {
            var combat = new CombatData
            {
                SurvivalTurns = 5,
                BackgroundKey = "forest_night",
                Enemies = new List<EnemySpawn> { new EnemySpawn { Name = "Wolf", MaxHP = 30 } },
                Rewards = new List<RewardData> { new RewardData { ItemName = "Wolf Pelt", Quantity = 1 } }
            };

            Assert.Equal(5, combat.SurvivalTurns);
            Assert.Equal("forest_night", combat.BackgroundKey);
            Assert.Single(combat.Enemies);
            Assert.Single(combat.Rewards!);
        }

        [Fact]
        public void QuestChain_CanBeInstantiated()
        {
            var chain = new QuestChain
            {
                Id = "quest_01",
                Title = "The First Step",
                IsMainStory = true,
                Prerequisites = new List<string> { "intro" },
                Steps = new List<QuestStep>
                {
                    new QuestStep
                    {
                        Id = "step_1",
                        Type = "combat",
                        Combat = new CombatData()
                    }
                }
            };

            Assert.Equal("quest_01", chain.Id);
            Assert.Equal("The First Step", chain.Title);
            Assert.True(chain.IsMainStory);
            Assert.Single(chain.Prerequisites);
            Assert.Single(chain.Steps);
            Assert.NotNull(chain.Steps[0].Combat);
        }

        [Fact]
        public void QuestStep_HasDialogueAndBranch()
        {
            var step = new QuestStep
            {
                Id = "step_dialogue",
                Type = "dialogue",
                Dialogue = new DialogueData
                {
                    Speaker = "Old Man",
                    Lines = new List<string> { "Hello there." }
                },
                Branch = new BranchData
                {
                    Options = new List<BranchOption>
                    {
                        new BranchOption { Text = "Fight", NextStepId = "step_combat", MoralityImpact = -5 }
                    }
                }
            };

            Assert.Equal("dialogue", step.Type);
            Assert.NotNull(step.Dialogue);
            Assert.Single(step.Dialogue.Lines);
            Assert.NotNull(step.Branch);
            Assert.Single(step.Branch.Options);
            Assert.Equal(-5, step.Branch.Options[0].MoralityImpact);
        }
    }
}
