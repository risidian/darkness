using Godot;
using Darkness.Core.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Darkness.Godot.Services;

public class GodotSpriteCompositor : ISpriteCompositor
{
    // This method is called by the Core logic.
    // We are going to ignore the streams and use the paths if possible, 
    // but for now let's try to make the Image-based approach actually work by being extremely careful.
    public async Task<byte[]> CompositeFullSheet(IReadOnlyList<string> layerBasePaths, IFileSystemService fileSystem)
    {
        var composite = Image.CreateEmpty(1536, 2112, false, Image.Format.Rgba8);
        composite.Fill(new Color(0, 0, 0, 0));

        foreach (var basePath in layerBasePaths)
        {
            var layerImage = Image.CreateEmpty(1536, 2112, false, Image.Format.Rgba8);
            layerImage.Fill(new Color(0, 0, 0, 0));

            // Map animation files to LPC rows
            var animMap = new Dictionary<string, int>
            {
                { "spellcast.png", 0 },
                { "thrust.png", 4 },
                { "walk.png", 8 },
                { "slash.png", 12 },
                { "shoot.png", 16 },
                { "hurt.png", 20 }
            };

            foreach (var anim in animMap)
            {
                try
                {
                    string fullPath = basePath.EndsWith("/") ? basePath + anim.Key : basePath + "/" + anim.Key;
                    var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    
                    var img = new Image();
                    if (img.LoadPngFromBuffer(ms.ToArray()) == Error.Ok)
                    {
                        if (img.IsCompressed()) img.Decompress();
                        img.Convert(Image.Format.Rgba8);
                        
                        // Blit into the correct row (each row is 64px high)
                        layerImage.BlendRect(img, new Rect2I(0, 0, img.GetWidth(), img.GetHeight()), new Vector2I(0, anim.Value * 64));
                    }
                }
                catch
                {
                    // Skip missing animations for specific layers
                }
            }

            composite.BlendRect(layerImage, new Rect2I(0, 0, 1536, 2112), Vector2I.Zero);
        }

        return composite.SavePngToBuffer();
    }

    public byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight)
    {
        // Fallback to the Image blending logic, but with extra safety for Android
        var composite = Image.CreateEmpty(sheetWidth, sheetHeight, false, Image.Format.Rgba8);
        composite.Fill(new Color(0,0,0,0));

        foreach (var stream in layerStreams)
        {
            byte[] data;
            if (stream is MemoryStream ms) data = ms.ToArray();
            else {
                using var temp = new MemoryStream();
                stream.CopyTo(temp);
                data = temp.ToArray();
            }

            if (data.Length == 0) continue;

            var img = new Image();
            Error err = img.LoadPngFromBuffer(data);
            
            // If LoadPng fails, it might be a remapped texture (.ctex)
            if (err != Error.Ok && data.Length > 4 && data[0] == 'G' && data[1] == 'D' && data[2] == 'T' && data[3] == 'C')
            {
                GD.PrintErr("[GodotSpriteCompositor] Received GDTC (remapped) data in stream. This shouldn't happen with the new FileSystem service, but handling anyway.");
                // We can't easily use ResourceLoader here without a path.
            }
            
            if (err == Error.Ok)
            {
                if (img.IsCompressed()) img.Decompress();
                if (img.GetFormat() != Image.Format.Rgba8) img.Convert(Image.Format.Rgba8);
                
                composite.BlendRect(img, new Rect2I(0, 0, img.GetWidth(), img.GetHeight()), Vector2I.Zero);
            }
            else
            {
                GD.PrintErr($"[GodotSpriteCompositor] FAILED to load layer. Error: {err}. Size: {data.Length}");
            }
        }

        return composite.SavePngToBuffer();
    }

    public byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale)
    {
        var sheet = new Image();
        var err = sheet.LoadPngFromBuffer(spriteSheetPng);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[GodotSpriteCompositor] ExtractFrame: FAILED to load sheet. Error: {err}");
            return Array.Empty<byte>();
        }

        if (sheet.IsCompressed()) sheet.Decompress();
        if (sheet.GetFormat() != Image.Format.Rgba8) sheet.Convert(Image.Format.Rgba8);

        var rect = new Rect2I(frameX, frameY, frameWidth, frameHeight);
        var frame = sheet.GetRegion(rect);

        if (scale > 1)
        {
            frame.Resize(frameWidth * scale, frameHeight * scale, Image.Interpolation.Nearest);
        }

        return frame.SavePngToBuffer();
    }
}
