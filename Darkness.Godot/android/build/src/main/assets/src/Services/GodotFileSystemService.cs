using Darkness.Core.Interfaces;
using Godot;
using System.IO;
using System.Threading.Tasks;

namespace Darkness.Godot.Services;

public class GodotFileSystemService : IFileSystemService
{
    public string AppDataDirectory => ProjectSettings.GlobalizePath("user://");

    public Task<Stream> OpenAppPackageFileAsync(string filename)
    {
        // Godot assets are prefixed with "res://"
        string path = filename.StartsWith("res://") ? filename : $"res://{filename}";
        
        using var file = global::Godot.FileAccess.Open(path, global::Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new FileNotFoundException($"Could not find asset: {path}");
        }

        var bytes = file.GetBuffer((long)file.GetLength());
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }
}
