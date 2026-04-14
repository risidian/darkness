using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Logic;
using SkiaSharp;

namespace Darkness.Core.Services;

public class SkiaSharpSpriteCompositor : ISpriteCompositor
{
    private readonly LpcAnimationHelper _animationHelper = new();
    private const string BASE_PATH = "assets/sprites/full/";

    public async Task<byte[]> CompositeFullSheet(
        IReadOnlyList<SheetDefinition> definitions,
        CharacterAppearance appearance,
        IFileSystemService fileSystem)
    {
        // Total height = standard + oversize
        int totalHeight = SheetConstants.SHEET_HEIGHT + (12 * SheetConstants.OVERSIZE_FRAME_SIZE);
        using var surface = SKSurface.Create(new SKImageInfo(SheetConstants.SHEET_WIDTH, totalHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";

        // 1. Render Standard Sheet (64x64 frames)
        foreach (var animation in SheetConstants.AnimationRows.Keys)
        {
            if (animation.EndsWith("_oversize")) continue;

            int frameCount = _animationHelper.GetFrameCount(animation);
            int startRow = SheetConstants.AnimationRows[animation];

            for (int direction = 0; direction < 4; direction++)
            {
                int row = startRow + direction;
                var layersForAnimation = ResolveLayers(definitions, animation, gender, appearance);
                var sortedLayers = layersForAnimation.OrderBy(l => l.ZPos).ToList();

                foreach (var layer in sortedLayers)
                {
                    string variant = ExtractVariant(layer.DefinitionName);
                    string path = ResolveAssetPath(layer.Layer, animation, variant, gender);
                    
                    if (!fileSystem.FileExists(BASE_PATH + path))
                    {
                        // Fallback: try attack_ prefix if it's a combat animation
                        if (animation == "slash" || animation == "thrust" || animation == "shoot")
                        {
                            path = ResolveAssetPath(layer.Layer, "attack_" + animation, variant, gender);
                        }
                    }

                    if (fileSystem.FileExists(BASE_PATH + path))
                    {
                        using var stream = await fileSystem.OpenAppPackageFileAsync(BASE_PATH + path);
                        using var bitmap = SKBitmap.Decode(stream);
                        if (bitmap != null)
                        {
                            for (int frame = 0; frame < frameCount; frame++)
                            {
                                var rect = _animationHelper.GetFrameRect(animation, direction, frame);
                                var srcRect = new SKRectI(frame * SheetConstants.FRAME_SIZE, direction * SheetConstants.FRAME_SIZE, 
                                                        (frame + 1) * SheetConstants.FRAME_SIZE, (direction + 1) * SheetConstants.FRAME_SIZE);
                                var destRect = new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);

                                using var paint = new SKPaint();
                                if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                                {
                                    if (SKColor.TryParse(layer.TintHex, out var color))
                                    {
                                        paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
                                    }
                                }

                                if (layer.IsFlipped)
                                {
                                    canvas.Save();
                                    canvas.Scale(-1, 1, destRect.MidX, destRect.MidY);
                                    canvas.DrawBitmap(bitmap, srcRect, destRect, paint);
                                    canvas.Restore();
                                }
                                else
                                {
                                    canvas.DrawBitmap(bitmap, srcRect, destRect, paint);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 2. Render Oversize Region (192x192 frames)
        string[] oversizeAnimations = { "slash_oversize", "slash_reverse_oversize", "thrust_oversize" };
        foreach (var customAnim in oversizeAnimations)
        {
            int frameCount = _animationHelper.GetFrameCount(customAnim);
            string baseAnim = customAnim.Replace("_oversize", "").Replace("_reverse", "");
            
            for (int direction = 0; direction < 4; direction++)
            {
                // First, draw the character body from standard frames into the center of the 192x192 frame
                for (int frame = 0; frame < frameCount; frame++)
                {
                    var destRect = _animationHelper.GetOversizeFrameRect(customAnim, direction, frame);
                    var skDestRect = new SKRect(destRect.X, destRect.Y, destRect.X + destRect.Width, destRect.Y + destRect.Height);
                    
                    // Center the 64x64 frame (offset 64,64)
                    var innerDestRect = new SKRect(skDestRect.Left + 64, skDestRect.Top + 64, skDestRect.Left + 128, skDestRect.Top + 128);
                    
                    // Draw standard layers centered
                    var standardLayers = ResolveLayers(definitions, baseAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();
                    foreach (var layer in standardLayers)
                    {
                        string variant = ExtractVariant(layer.DefinitionName);
                        string path = ResolveAssetPath(layer.Layer, baseAnim, variant, gender);
                        if (fileSystem.FileExists(BASE_PATH + path))
                        {
                            using var stream = await fileSystem.OpenAppPackageFileAsync(BASE_PATH + path);
                            using var bitmap = SKBitmap.Decode(stream);
                            if (bitmap != null)
                            {
                                var srcRect = new SKRectI(frame * SheetConstants.FRAME_SIZE, direction * SheetConstants.FRAME_SIZE, 
                                                        (frame + 1) * SheetConstants.FRAME_SIZE, (direction + 1) * SheetConstants.FRAME_SIZE);
                                
                                using var paint = new SKPaint();
                                if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                                {
                                    if (SKColor.TryParse(layer.TintHex, out var color))
                                    {
                                        paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
                                    }
                                }

                                if (layer.IsFlipped)
                                {
                                    canvas.Save();
                                    canvas.Scale(-1, 1, innerDestRect.MidX, innerDestRect.MidY);
                                    canvas.DrawBitmap(bitmap, srcRect, innerDestRect, paint);
                                    canvas.Restore();
                                }
                                else
                                {
                                    canvas.DrawBitmap(bitmap, srcRect, innerDestRect, paint);
                                }
                            }
                        }
                    }

                    // Now draw the custom oversize layers on top/behind
                    var customLayers = ResolveLayers(definitions, customAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();
                    foreach (var layer in customLayers)
                    {
                        string variant = ExtractVariant(layer.DefinitionName);
                        string path = ResolveAssetPath(layer.Layer, customAnim, variant, gender);
                        if (fileSystem.FileExists(BASE_PATH + path))
                        {
                            using var stream = await fileSystem.OpenAppPackageFileAsync(BASE_PATH + path);
                            using var bitmap = SKBitmap.Decode(stream);
                            if (bitmap != null)
                            {
                                var srcRect = new SKRectI(frame * SheetConstants.OVERSIZE_FRAME_SIZE, direction * SheetConstants.OVERSIZE_FRAME_SIZE, 
                                                        (frame + 1) * SheetConstants.OVERSIZE_FRAME_SIZE, (direction + 1) * SheetConstants.OVERSIZE_FRAME_SIZE);
                                
                                using var paint = new SKPaint();
                                if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                                {
                                    if (SKColor.TryParse(layer.TintHex, out var color))
                                    {
                                        paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
                                    }
                                }

                                if (layer.IsFlipped)
                                {
                                    canvas.Save();
                                    canvas.Scale(-1, 1, skDestRect.MidX, skDestRect.MidY);
                                    canvas.DrawBitmap(bitmap, srcRect, skDestRect, paint);
                                    canvas.Restore();
                                }
                                else
                                {
                                    canvas.DrawBitmap(bitmap, srcRect, skDestRect, paint);
                                }
                            }
                        }
                    }
                }
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> CompositePreviewFrame(
        IReadOnlyList<SheetDefinition> definitions,
        CharacterAppearance appearance,
        IFileSystemService fileSystem)
    {
        // Use the first definition's preview info, or default to walk (10, 0)
        var previewDef = definitions.FirstOrDefault() ?? new SheetDefinition();
        int row = previewDef.PreviewRow > 0 ? previewDef.PreviewRow : 10; // Row 10 is walk south
        int col = previewDef.PreviewColumn;

        using var surface = SKSurface.Create(new SKImageInfo(SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";
        
        // Find animation name from row
        string animation = "walk";
        int direction = 2; // South
        foreach (var kvp in SheetConstants.AnimationRows)
        {
            if (row >= kvp.Value && row < kvp.Value + 4)
            {
                animation = kvp.Key;
                direction = row - kvp.Value;
                break;
            }
        }

        var layers = ResolveLayers(definitions, animation, gender, appearance).OrderBy(l => l.ZPos).ToList();
        foreach (var layer in layers)
        {
            string variant = ExtractVariant(layer.DefinitionName);
            string path = ResolveAssetPath(layer.Layer, animation, variant, gender);
            if (fileSystem.FileExists(BASE_PATH + path))
            {
                using var stream = await fileSystem.OpenAppPackageFileAsync(BASE_PATH + path);
                using var bitmap = SKBitmap.Decode(stream);
                if (bitmap != null)
                {
                    var srcRect = new SKRectI(col * SheetConstants.FRAME_SIZE, direction * SheetConstants.FRAME_SIZE, 
                                            (col + 1) * SheetConstants.FRAME_SIZE, (direction + 1) * SheetConstants.FRAME_SIZE);
                    var destRect = new SKRect(0, 0, SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE);

                    using var paint = new SKPaint();
                    if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                    {
                        if (SKColor.TryParse(layer.TintHex, out var color))
                        {
                            paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
                        }
                    }

                    if (layer.IsFlipped)
                    {
                        canvas.Save();
                        canvas.Scale(-1, 1, destRect.MidX, destRect.MidY);
                        canvas.DrawBitmap(bitmap, srcRect, destRect, paint);
                        canvas.Restore();
                    }
                    else
                    {
                        canvas.DrawBitmap(bitmap, srcRect, destRect, paint);
                    }
                }
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public byte[] ExtractFrame(byte[] spriteSheetPng, string animation, int frameIndex, int direction)
    {
        using var bitmap = SKBitmap.Decode(spriteSheetPng);
        if (bitmap == null) return Array.Empty<byte>();

        bool isOversize = _animationHelper.IsOversize(animation);
        var rect = isOversize ? _animationHelper.GetOversizeFrameRect(animation, direction, frameIndex) 
                               : _animationHelper.GetFrameRect(animation, direction, frameIndex);

        using var frameBitmap = new SKBitmap(rect.Width, rect.Height);
        using var canvas = new SKCanvas(frameBitmap);
        canvas.Clear(SKColors.Transparent);

        var srcRect = new SKRectI(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
        var destRect = new SKRect(0, 0, rect.Width, rect.Height);
        canvas.DrawBitmap(bitmap, srcRect, destRect);

        using var image = SKImage.FromBitmap(frameBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private List<ResolvedLayer> ResolveLayers(IReadOnlyList<SheetDefinition> definitions, string animation, string gender, CharacterAppearance appearance)
    {
        var result = new List<ResolvedLayer>();
        foreach (var def in definitions)
        {
            string tint = "#FFFFFF";
            if (def.Slot == "Body" || def.Slot == "Head" || def.Slot == "Face") tint = appearance.SkinColor;
            else if (def.Slot == "Hair") tint = appearance.HairColor;
            
            foreach (var kvp in def.Layers)
            {
                var layer = kvp.Value;
                // Only include if no custom animation, or it matches the current custom animation
                if (string.IsNullOrEmpty(layer.CustomAnimation))
                {
                    // Standard layers don't apply to oversize render passes except when we are rendering the base character
                    if (!animation.EndsWith("_oversize"))
                    {
                        result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, tint));
                    }
                }
                else if (layer.CustomAnimation == animation)
                {
                    result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, tint));
                }
            }
        }
        return result;
    }

    private string ResolveAssetPath(SheetLayer layer, string animation, string variant, string gender)
    {
        string layerPath = layer.GetPath(gender);
        if (string.IsNullOrEmpty(layerPath)) return string.Empty;

        return $"{layerPath.TrimEnd('/')}/{animation}/{variant}.png";
    }

    private string ExtractVariant(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "default";

        // "Arming Sword (Steel)" -> "steel"
        int start = displayName.LastIndexOf('(');
        int end = displayName.LastIndexOf(')');
        if (start != -1 && end != -1 && end > start)
        {
            return displayName.Substring(start + 1, end - start - 1).Trim().ToLower();
        }
        return "default";
    }

    private class ResolvedLayer
    {
        public string DefinitionName { get; }
        public SheetLayer Layer { get; }
        public bool IsFlipped { get; }
        public string TintHex { get; }
        public int ZPos => Layer.ZPos;

        public ResolvedLayer(string defName, SheetLayer layer, bool isFlipped, string tintHex)
        {
            DefinitionName = defName;
            Layer = layer;
            IsFlipped = isFlipped;
            TintHex = tintHex;
        }
    }
}
