using LiteDB;

namespace Darkness.Core.Models;

public class WorldFlag
{
    [BsonId]
    public string CompoundKey { get; set; } = string.Empty;
    public int CharacterId { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool Value { get; set; }

    public static string MakeCompoundKey(int characterId, string key) => $"{characterId}:{key}";
}
