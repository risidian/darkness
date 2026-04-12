using System;
using System.Linq;
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

        if (!global::Godot.FileAccess.FileExists(path) && !ResourceLoader.Exists(path))
        {
            // Do not GD.PrintErr here, as callers like SpriteCompositor use this exception 
            // gracefully as a fallback mechanism for missing animation layers.
            throw new FileNotFoundException($"Could not find asset: {path}");
        }

        // Check if it's an image that might be remapped
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // ResourceLoader is the correct way to get images from res:// in exported builds
                // because it handles remapping (.ctex) automatically.
                var tex = ResourceLoader.Load<Texture2D>(path);
                if (tex != null)
                {
                    var img = tex.GetImage();

                    // ON ANDROID: Textures are often VRAM compressed (ETC2/ASTC).
                    // SavePngToBuffer() will FAIL on compressed formats.
                    if (img.IsCompressed())
                    {
                        img.Decompress();
                    }

                    // Ensure we are in a standard format for saving
                    if (img.GetFormat() != Image.Format.Rgba8)
                    {
                        img.Convert(Image.Format.Rgba8);
                    }

                    byte[] pngBytes = img.SavePngToBuffer();

                    if (pngBytes == null || pngBytes.Length == 0)
                    {
                        GD.PrintErr(
                            $"[FileSystem] SavePngToBuffer returned EMPTY for {path}. Format: {img.GetFormat()}");
                    }
                    else
                    {
                        GD.Print(
                            $"[FileSystem] Loaded image via ResourceLoader: {path} ({pngBytes.Length} bytes, Format: {img.GetFormat()})");
                        return Task.FromResult<Stream>(new MemoryStream(pngBytes));
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileSystem] ResourceLoader FAILED for {path}: {ex.Message}");
            }
        }

        // Fallback for non-images or if ResourceLoader failed
        byte[] bytes = global::Godot.FileAccess.GetFileAsBytes(path);
        if (bytes == null || bytes.Length == 0)
        {
            GD.PrintErr($"[FileSystem] FAILED TO READ bytes from asset: {path}");
            throw new IOException($"Failed to read bytes from asset: {path}");
        }

        GD.Print($"[FileSystem] Successfully read {bytes.Length} bytes from {path} (Fallback/Non-Image)");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public string ReadAllText(string filename)
    {
        string path = filename.StartsWith("res://") ? filename : $"res://{filename}";
        if (global::Godot.FileAccess.FileExists(path))
        {
            using var file = global::Godot.FileAccess.Open(path, global::Godot.FileAccess.ModeFlags.Read);
            return file.GetAsText();
        }

        throw new FileNotFoundException($"Could not find file: {path}");
    }

    public bool FileExists(string path)
    {
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return global::Godot.FileAccess.FileExists(resPath) || ResourceLoader.Exists(resPath);
    }

    public bool DirectoryExists(string path)
    {
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return DirAccess.DirExistsAbsolute(resPath);
    }

    public string[] GetFiles(string path, string searchPattern)
    {
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        if (!DirAccess.DirExistsAbsolute(resPath))
            return Array.Empty<string>();

        var files = DirAccess.GetFilesAt(resPath);
        var extension = Path.GetExtension(searchPattern);

        return files
            .Where(f => string.IsNullOrEmpty(extension) || f.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.Combine(resPath, f).Replace("\\", "/"))
            .ToArray();
    }
}