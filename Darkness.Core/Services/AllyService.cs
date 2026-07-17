using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Core.Services;

public class AllyService : IAllyService
{
    private readonly LiteDatabase _db;

    public AllyService(LiteDatabase db)
    {
        _db = db;
    }

    public Task<List<Ally>> GetAlliesForUserAsync(int userId)
    {
        var col = _db.GetCollection<Ally>("allies");
        var allies = col.Find(a => a.UserId == userId).ToList();
        return Task.FromResult(allies);
    }

    public Task<bool> SendAllyRequestAsync(int userId, int allyUserId, string allyUsername)
    {
        var col = _db.GetCollection<Ally>("allies");

        // Prevent duplicate pending/accepted requests between same pair
        var existing = col.FindOne(a => a.UserId == userId && a.AllyUserId == allyUserId);
        if (existing != null)
            return Task.FromResult(false);

        col.Insert(new Ally
        {
            UserId = userId,
            AllyUserId = allyUserId,
            AllyUsername = allyUsername,
            Status = "Pending"
        });

        return Task.FromResult(true);
    }

    public Task<bool> RespondToAllyRequestAsync(int allyId, bool accepted)
    {
        var col = _db.GetCollection<Ally>("allies");
        var ally = col.FindById(allyId);

        if (ally == null)
            return Task.FromResult(false);

        if (accepted)
        {
            ally.Status = "Accepted";
            col.Update(ally);
        }
        else
        {
            col.Delete(allyId);
        }

        return Task.FromResult(true);
    }
}