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
        
        GD.Print($"[FileSystem] Attempting to open asset: {path}");

        if (!global::Godot.FileAccess.FileExists(path))
        {
            GD.PrintErr($"[FileSystem] Asset NOT FOUND: {path}");
            throw new FileNotFoundException($"Could not find asset: {path}");
        }

        byte[] bytes = global::Godot.FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0)
        {
            GD.PrintErr($"[FileSystem] FAILED TO READ bytes from asset: {path}");
            throw new IOException($"Failed to read bytes from asset: {path}");
        }

        GD.Print($"[FileSystem] Successfully read {bytes.Length} bytes from {path}");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }
}
