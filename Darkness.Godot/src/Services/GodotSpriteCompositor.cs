using Godot;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Darkness.Godot.Services;

public class GodotSpriteCompositor : ISpriteCompositor
{
    public async Task<byte[]> CompositeFullSheet(IReadOnlyList<StitchLayer> layers, IFileSystemService fileSystem)
    {
        GD.Print($"[Compositor] Starting full sheet composition with {layers.Count} layers.");
        var composite = Image.CreateEmpty(1536, 2112, false, Image.Format.Rgba8);
        composite.Fill(new Color(0, 0, 0, 0));

        // Map internal action names to LPC rows
        var animMap = new Dictionary<string, int>
        {
            { "spellcast", 0 },
            { "thrust", 4 },
            { "walk", 8 },
            { "slash", 12 },
            { "shoot", 16 },
            { "hurt", 20 }
        };

        foreach (var layer in layers)
        {
            var layerImage = Image.CreateEmpty(1536, 2112, false, Image.Format.Rgba8);
            layerImage.Fill(new Color(0, 0, 0, 0));

            var tint = Color.FromHtml(layer.TintHex);
            bool layerHasContent = false;

            foreach (var anim in animMap)
            {
                byte[]? data = await LoadLayerData(layer, anim.Key, fileSystem);

                // Fallback to walk if specific action is missing (except for Body)
                if (data == null && anim.Key != "walk")
                {
                    data = await LoadLayerData(layer, "walk", fileSystem);
                }

                if (data != null)
                {
                    var img = new Image();
                    if (img.LoadPngFromBuffer(data) == Error.Ok)
                    {
                        layerHasContent = true;
                        if (img.IsCompressed()) img.Decompress();
                        img.Convert(Image.Format.Rgba8);

                        if (layer.IsFlipped)
                        {
                            img.FlipX();
                        }

                        // Apply Tint if not White
                        if (tint.R < 0.99f || tint.G < 0.99f || tint.B < 0.99f)
                        {
                            for (int y = 0; y < img.GetHeight(); y++)
                            {
                                for (int x = 0; x < img.GetWidth(); x++)
                                {
                                    var pixel = img.GetPixel(x, y);
                                    if (pixel.A > 0)
                                    {
                                        var newPixel = new Color(pixel.R * tint.R, pixel.G * tint.G, pixel.B * tint.B,
                                            pixel.A);
                                        img.SetPixel(x, y, newPixel);
                                    }
                                }
                            }
                        }

                        // Handle tiny legacy face assets (96x128)
                        if (img.GetWidth() == 96 && img.GetHeight() == 128)
                        {
                            // Face sheet is 3 cols x 4 rows of 32x32.
                            // Row 0: Up (y=0)
                            // Row 1: Right (y=32)
                            // Row 2: Down (y=64)
                            // Row 3: Left (y=96)

                            // We need to map this to the full sheet's 64x64 grid.
                            // The current animation base row in the full sheet is `anim.Value`.
                            // But wait, the face only has one 'action' (the file we loaded).
                            // Let's just blit the appropriate row to the "walk" rows if we are doing walk.
                            // Actually, to make it simple: `animMap["walk"]` = 8.
                            // Row 8=Up, Row 9=Left, Row 10=Down, Row 11=Right in the FULL sheet.

                            int destRowBase = anim.Value;

                            // Map full sheet rows to face image Y offset
                            // 0 = Up, 1 = Left, 2 = Down, 3 = Right (in full sheet order)
                            int[] faceYOffset = { 0, 96, 64, 32 };

                            for (int dir = 0; dir < 4; dir++)
                            {
                                // Face has 3 frames: standing, step 1, step 2.
                                // We'll just use the standing frame (x=0) for all frames in the animation.
                                var faceFrame =
                                    img.GetRegion(new Rect2I(32, faceYOffset[dir], 32,
                                        32)); // col 1 is usually standing

                                // Blit to all 9 columns of this row in the full sheet
                                for (int col = 0; col < 9; col++)
                                {
                                    layerImage.BlendRect(faceFrame, new Rect2I(0, 0, 32, 32),
                                        new Vector2I(col * 64 + 16, (destRowBase + dir) * 64 + 16));
                                }
                            }
                        }
                        else
                        {
                            // Blit into the correct row
                            layerImage.BlendRect(img, new Rect2I(0, 0, img.GetWidth(), img.GetHeight()),
                                new Vector2I(0, anim.Value * 64));
                        }
                    }
                }
            }

            if (layerHasContent)
            {
                GD.Print($"[Compositor] Blending layer: {layer.RootPath}");
                composite.BlendRect(layerImage, new Rect2I(0, 0, 1536, 2112), Vector2I.Zero);
            }
        }

        return composite.SavePngToBuffer();
    }

    private async Task<byte[]?> LoadLayerData(StitchLayer layer, string action, IFileSystemService fileSystem)
    {
        try
        {
            string fileName = layer.FileNameTemplate.Replace("{action}", action);
            string fullPath = layer.RootPath.EndsWith("/")
                ? layer.RootPath + fileName
                : layer.RootPath + "/" + fileName;

            if (fileSystem.FileExists(fullPath))
            {
                var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }

            // Fallback 1: Try "attack_" prefix for actions like slash/thrust (LPC weapon structure)
            if (action == "slash" || action == "thrust" || action == "shoot")
            {
                string altAction = "attack_" + action;
                string altFileName = layer.FileNameTemplate.Replace("{action}", altAction);
                string altFullPath = layer.RootPath.EndsWith("/")
                    ? layer.RootPath + altFileName
                    : layer.RootPath + "/" + altFileName;
                
                if (fileSystem.FileExists(altFullPath))
                {
                    var stream = await fileSystem.OpenAppPackageFileAsync(altFullPath);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    return ms.ToArray();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]> CompositePreviewFrame(IReadOnlyList<StitchLayer> layers, IFileSystemService fileSystem)
    {
        var composite = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        composite.Fill(new Color(0, 0, 0, 0));

        foreach (var layer in layers)
        {
            var data = await LoadLayerData(layer, "walk", fileSystem);
            if (data != null)
            {
                var img = new Image();
                if (img.LoadPngFromBuffer(data) == Error.Ok)
                {
                    if (img.IsCompressed()) img.Decompress();
                    img.Convert(Image.Format.Rgba8);

                    if (layer.IsFlipped)
                    {
                        img.FlipX();
                    }

                    var tint = Color.FromHtml(layer.TintHex);
                    if (tint.R < 0.99f || tint.G < 0.99f || tint.B < 0.99f)
                    {
                        for (int y = 0; y < img.GetHeight(); y++)
                        {
                            for (int x = 0; x < img.GetWidth(); x++)
                            {
                                var pixel = img.GetPixel(x, y);
                                if (pixel.A > 0)
                                {
                                    img.SetPixel(x, y,
                                        new Color(pixel.R * tint.R, pixel.G * tint.G, pixel.B * tint.B, pixel.A));
                                }
                            }
                        }
                    }

                    if (img.GetWidth() == 96 && img.GetHeight() == 128)
                    {
                        // Legacy Face Down Frame is Row 2 (y=64) Col 1 (x=32)
                        var faceFrame = img.GetRegion(new Rect2I(32, 64, 32, 32));
                        composite.BlendRect(faceFrame, new Rect2I(0, 0, 32, 32), new Vector2I(16, 16));
                    }
                    else
                    {
                        // "walk" sheet is 576x256. Down idle frame is Row 2 (y=128), Col 0 (x=0).
                        var walkFrame = img.GetRegion(new Rect2I(0, 128, 64, 64));
                        composite.BlendRect(walkFrame, new Rect2I(0, 0, 64, 64), Vector2I.Zero);
                    }
                }
            }
        }

        return composite.SavePngToBuffer();
    }

    public byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight)
    {
        var composite = Image.CreateEmpty(sheetWidth, sheetHeight, false, Image.Format.Rgba8);
        composite.Fill(new Color(0, 0, 0, 0));

        foreach (var stream in layerStreams)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var img = new Image();
            if (img.LoadPngFromBuffer(ms.ToArray()) == Error.Ok)
            {
                if (img.IsCompressed()) img.Decompress();
                img.Convert(Image.Format.Rgba8);

                int w = img.GetWidth();
                int h = img.GetHeight();

                // If it's a small asset (like face 96x128), it's 32x32 frames.
                if (h <= 128 && h < sheetHeight)
                {
                    // Extract the Down-pose frame (Row 3, Column 1 of 32x32 grid)
                    // Row 3 starts at y = 64.
                    var frame = img.GetRegion(new Rect2I(0, 64, 32, 32));

                    // Do not resize it! A 32x32 face should be drawn at an offset within the 64x64 bounds.
                    // The preview extracts a 64x64 block from y=128.
                    // Offset of (16, 16) centers the 32x32 head within the 64x64 extraction block.
                    composite.BlendRect(frame, new Rect2I(0, 0, 32, 32), new Vector2I(16, 128 + 16));
                }
                else
                {
                    // Full sheet or already 64x64 based. Blit entire sheet.
                    composite.BlendRect(img, new Rect2I(0, 0, Math.Min(w, sheetWidth), Math.Min(h, sheetHeight)),
                        Vector2I.Zero);
                }
            }
        }

        return composite.SavePngToBuffer();
    }

    public byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight,
        int scale)
    {
        var sheet = new Image();
        sheet.LoadPngFromBuffer(spriteSheetPng);

        var rect = new Rect2I(frameX, frameY, frameWidth, frameHeight);
        var frame = sheet.GetRegion(rect);

        if (scale > 1)
        {
            frame.Resize(frameWidth * scale, frameHeight * scale, Image.Interpolation.Nearest);
        }

        return frame.SavePngToBuffer();
    }
}