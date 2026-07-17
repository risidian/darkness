using Xunit;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Tests.Services
{
    public class AllyServiceTests
    {
        private static LiteDatabase CreateEmptyDb() => new LiteDatabase(new MemoryStream(), new BsonMapper());

        // ── GetAlliesForUserAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetAllies_Returns_Only_Allies_For_User()
        {
            using var db = CreateEmptyDb();
            var col = db.GetCollection<Ally>("allies");
            col.Insert(new Ally { Id = 1, UserId = 10, AllyUserId = 20, AllyUsername = "Alice", Status = "Accepted" });
            col.Insert(new Ally { Id = 2, UserId = 10, AllyUserId = 30, AllyUsername = "Bob",   Status = "Pending"  });
            col.Insert(new Ally { Id = 3, UserId = 99, AllyUserId = 10, AllyUsername = "Other", Status = "Accepted" });

            var service = new AllyService(db);
            var allies = await service.GetAlliesForUserAsync(10);

            Assert.Equal(2, allies.Count);
            Assert.All(allies, a => Assert.Equal(10, a.UserId));
        }

        [Fact]
        public async Task GetAllies_Returns_Empty_List_When_No_Allies()
        {
            using var db = CreateEmptyDb();
            var service = new AllyService(db);

            var allies = await service.GetAlliesForUserAsync(42);

            Assert.NotNull(allies);
            Assert.Empty(allies);
        }

        // ── SendAllyRequestAsync ──────────────────────────────────────────────

        [Fact]
        public async Task SendAllyRequest_Persists_Pending_Record()
        {
            using var db = CreateEmptyDb();
            var service = new AllyService(db);

            var result = await service.SendAllyRequestAsync(1, 2, "Charlie");

            Assert.True(result);
            var col = db.GetCollection<Ally>("allies");
            var saved = col.FindOne(a => a.UserId == 1 && a.AllyUserId == 2);
            Assert.NotNull(saved);
            Assert.Equal("Pending", saved.Status);
            Assert.Equal("Charlie", saved.AllyUsername);
        }

        [Fact]
        public async Task SendAllyRequest_Returns_False_When_Request_Already_Exists()
        {
            using var db = CreateEmptyDb();
            var col = db.GetCollection<Ally>("allies");
            col.Insert(new Ally { Id = 1, UserId = 1, AllyUserId = 2, AllyUsername = "Charlie", Status = "Pending" });

            var service = new AllyService(db);

            // Sending same request again should not create a duplicate
            var result = await service.SendAllyRequestAsync(1, 2, "Charlie");

            Assert.False(result);
            Assert.Equal(1, col.Count());
        }

        // ── RespondToAllyRequestAsync ─────────────────────────────────────────

        [Fact]
        public async Task RespondToAllyRequest_Accept_Changes_Status_To_Accepted()
        {
            using var db = CreateEmptyDb();
            var col = db.GetCollection<Ally>("allies");
            col.Insert(new Ally { Id = 1, UserId = 1, AllyUserId = 2, AllyUsername = "Dave", Status = "Pending" });

            var service = new AllyService(db);
            var result = await service.RespondToAllyRequestAsync(1, accepted: true);

            Assert.True(result);
            var updated = col.FindById(1);
            Assert.Equal("Accepted", updated.Status);
        }

        [Fact]
        public async Task RespondToAllyRequest_Decline_Removes_Record()
        {
            using var db = CreateEmptyDb();
            var col = db.GetCollection<Ally>("allies");
            col.Insert(new Ally { Id = 1, UserId = 1, AllyUserId = 2, AllyUsername = "Eve", Status = "Pending" });

            var service = new AllyService(db);
            var result = await service.RespondToAllyRequestAsync(1, accepted: false);

            Assert.True(result);
            Assert.Null(col.FindById(1));
        }

        [Fact]
        public async Task RespondToAllyRequest_Returns_False_For_Unknown_Id()
        {
            using var db = CreateEmptyDb();
            var service = new AllyService(db);

            var result = await service.RespondToAllyRequestAsync(999, accepted: true);

            Assert.False(result);
        }
    }
}
