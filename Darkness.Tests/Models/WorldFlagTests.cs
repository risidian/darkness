using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

namespace Darkness.Tests.Models
{
    public class WorldFlagTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly LocalDatabaseService _dbService;
        private readonly string _testAppDataDir;

        public WorldFlagTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "DarknessTests_" + Guid.NewGuid().ToString());
            _fileSystemMock.Setup(f => f.AppDataDirectory).Returns(_testAppDataDir);
            _dbService = new LocalDatabaseService(_fileSystemMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testAppDataDir))
            {
                Directory.Delete(_testAppDataDir, true);
            }
        }

        [Fact]
        public void WorldFlag_CanBeStoredAndRetrieved()
        {
            using var db = _dbService.OpenDatabase();
            var collection = db.GetCollection<WorldFlag>("world_flags");

            var flag = new WorldFlag
            {
                CompoundKey = WorldFlag.MakeCompoundKey(1, "test_flag"),
                CharacterId = 1,
                Key = "test_flag",
                Value = true
            };
            collection.Insert(flag);

            var retrieved = collection.FindOne(f => f.CharacterId == 1 && f.Key == "test_flag");
            Assert.NotNull(retrieved);
            Assert.Equal("test_flag", retrieved.Key);
            Assert.True(retrieved.Value);
            Assert.Equal(1, retrieved.CharacterId);
        }

        [Fact]
        public void WorldFlag_CompoundKey_PreventsDuplicates()
        {
            using var db = _dbService.OpenDatabase();
            var collection = db.GetCollection<WorldFlag>("world_flags");

            var key = WorldFlag.MakeCompoundKey(7, "dup_flag");
            collection.Insert(new WorldFlag { CompoundKey = key, CharacterId = 7, Key = "dup_flag", Value = false });

            // A second Insert with the same compound key must fail (uniqueness via BsonId).
            Assert.Throws<LiteDB.LiteException>(() =>
                collection.Insert(new WorldFlag { CompoundKey = key, CharacterId = 7, Key = "dup_flag", Value = true })
            );

            // Upsert should succeed and overwrite.
            collection.Upsert(new WorldFlag { CompoundKey = key, CharacterId = 7, Key = "dup_flag", Value = true });
            var all = collection.Find(f => f.CharacterId == 7 && f.Key == "dup_flag");
            Assert.Single(all);
            Assert.True(collection.FindById(key).Value);
        }

        [Fact]
        public void QuestStep_SupportsRequirementsAndRewards()
        {
            var step = new QuestStep
            {
                Id = "step1",
                Requirements = new List<BranchCondition>
                {
                    new BranchCondition { Type = "morality", Operator = ">=", Value = "10" }
                },
                Rewards = new List<QuestReward>
                {
                    new QuestReward { Type = QuestRewardType.Experience, Amount = 100 }
                }
            };

            Assert.Single(step.Requirements);
            Assert.Single(step.Rewards);
            Assert.Equal("morality", step.Requirements[0].Type);
            Assert.Equal(QuestRewardType.Experience, step.Rewards[0].Type);
        }

        [Fact]
        public void ZoneConfig_SupportsFlagVisibility()
        {
            var zone = new ZoneConfig
            {
                RequiredFlag = "has_key",
                ForbiddenFlag = "gate_open"
            };

            Assert.Equal("has_key", zone.RequiredFlag);
            Assert.Equal("gate_open", zone.ForbiddenFlag);
        }
    }
}
