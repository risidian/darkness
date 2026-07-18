using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models
{
    public class QuestChainArcIdTests
    {
        private static JsonSerializerOptions Options()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        [Fact]
        public void ArcId_DefaultsToNull()
        {
            var chain = new QuestChain();
            Assert.Null(chain.ArcId);
        }

        [Fact]
        public void ExistingStyleJson_WithoutArcId_DeserializesWithNullArcId()
        {
            // Mirrors the shape of a current beat_*.json file (no arcId field present).
            const string json = @"{
                ""id"": ""beat_1"",
                ""title"": ""The Awakening"",
                ""isMainStory"": true,
                ""sortOrder"": 1,
                ""prerequisites"": [],
                ""steps"": [
                    { ""id"": ""step_1"", ""type"": ""Dialogue"", ""nextStepId"": null }
                ]
            }";

            var chain = JsonSerializer.Deserialize<QuestChain>(json, Options());

            Assert.NotNull(chain);
            Assert.Equal("beat_1", chain!.Id);
            Assert.Null(chain.ArcId);
            Assert.Single(chain.Steps);
        }

        [Fact]
        public void ArcId_RoundTripsWhenSet()
        {
            var chain = new QuestChain
            {
                Id = "beat_2",
                Title = "The Dark Warrior",
                ArcId = "arc_1"
            };

            var json = JsonSerializer.Serialize(chain, Options());
            var restored = JsonSerializer.Deserialize<QuestChain>(json, Options());

            Assert.NotNull(restored);
            Assert.Equal("arc_1", restored!.ArcId);
            Assert.Equal("beat_2", restored.Id);
        }
    }
}
