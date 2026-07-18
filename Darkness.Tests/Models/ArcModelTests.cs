using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models
{
    public class ArcModelTests
    {
        private static JsonSerializerOptions Options()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        [Fact]
        public void Arc_DefaultsAreEmpty()
        {
            var arc = new Arc();

            Assert.Equal(string.Empty, arc.Id);
            Assert.Equal(string.Empty, arc.TitleKey);
            Assert.Equal(0, arc.Order);
            Assert.NotNull(arc.Prerequisites);
            Assert.Empty(arc.Prerequisites);
            Assert.NotNull(arc.MainChainIds);
            Assert.Empty(arc.MainChainIds);
            Assert.NotNull(arc.SideChainIds);
            Assert.Empty(arc.SideChainIds);
        }

        [Fact]
        public void Arc_CanBeInstantiated()
        {
            var arc = new Arc
            {
                Id = "arc_1",
                TitleKey = "arc.arc_1.title",
                Order = 1,
                Prerequisites = new List<string>(),
                MainChainIds = new List<string> { "beat_1", "beat_2" },
                SideChainIds = new List<string> { "side_1" }
            };

            Assert.Equal("arc_1", arc.Id);
            Assert.Equal("arc.arc_1.title", arc.TitleKey);
            Assert.Equal(1, arc.Order);
            Assert.Equal(2, arc.MainChainIds.Count);
            Assert.Single(arc.SideChainIds);
        }

        [Fact]
        public void Arc_JsonRoundTrips()
        {
            var arc = new Arc
            {
                Id = "arc_2",
                TitleKey = "arc.arc_2.title",
                Order = 2,
                Prerequisites = new List<string> { "arc_1" },
                MainChainIds = new List<string> { "beat_11", "beat_12" },
                SideChainIds = new List<string> { "side_taverns" }
            };

            var json = JsonSerializer.Serialize(arc, Options());
            var restored = JsonSerializer.Deserialize<Arc>(json, Options());

            Assert.NotNull(restored);
            Assert.Equal(arc.Id, restored!.Id);
            Assert.Equal(arc.TitleKey, restored.TitleKey);
            Assert.Equal(arc.Order, restored.Order);
            Assert.Equal(arc.Prerequisites, restored.Prerequisites);
            Assert.Equal(arc.MainChainIds, restored.MainChainIds);
            Assert.Equal(arc.SideChainIds, restored.SideChainIds);
        }

        [Fact]
        public void Arc_DeserializesFromLowerCaseJson_WithoutOptionalArrays()
        {
            const string json = "{ \"id\": \"arc_9\", \"titleKey\": \"arc.arc_9.title\", \"order\": 9 }";

            var arc = JsonSerializer.Deserialize<Arc>(json, Options());

            Assert.NotNull(arc);
            Assert.Equal("arc_9", arc!.Id);
            Assert.Equal(9, arc.Order);
            Assert.NotNull(arc.Prerequisites);
            Assert.Empty(arc.Prerequisites);
            Assert.NotNull(arc.MainChainIds);
            Assert.Empty(arc.MainChainIds);
        }
    }
}
