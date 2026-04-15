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
        Console.WriteLine($"[Compositor] CompositeFullSheet: {definitions.Count} defs, Gender: {appearance.Head}");
        
        int totalHeight = SheetConstants.SHEET_HEIGHT + (12 * SheetConstants.OVERSIZE_FRAME_SIZE);
        var info = new SKImageInfo(SheetConstants.SHEET_WIDTH, totalHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface == null)
        {
            Console.WriteLine("[Compositor] ERROR: Failed to create SKSurface (Dimensions too large?)");
            return Array.Empty<byte>();
        }
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";
        int totalBitmapsDrawn = 0;
        var bitmapCache = new Dictionary<string, SKBitmap?>();

        // 1. Render Standard Sheet (64x64 frames)
        foreach (var animation in SheetConstants.AnimationRows.Keys)
        {
            if (animation.EndsWith("_oversize")) continue;

            int frameCount = _animationHelper.GetFrameCount(animation);
            int startRow = SheetConstants.AnimationRows[animation];

            var layersForAnimation = ResolveLayers(definitions, animation, gender, appearance);
            var sortedLayers = layersForAnimation.OrderBy(l => l.ZPos).ToList();

            if (sortedLayers.Count == 0) continue;

            for (int direction = 0; direction < 4; direction++)
            {
                int row = startRow + direction;
                foreach (var layer in sortedLayers)
                {
                    string variant = ResolveVariant(layer);
                    var candidates = GetCandidatePaths(layer.Layer, animation, variant, gender);
                    SKBitmap? bitmap = null;
                    foreach (var candidate in candidates)
                    {
                        bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
                        if (bitmap != null) break;
                    }

                    if (bitmap != null)
                    {
                        totalBitmapsDrawn++;
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

        // 2. Render Oversize Region (192x192 frames)
        // ... (Skipping for brevity in log check, but keeping implementation)
        string[] oversizeAnimations = { "slash_oversize", "slash_reverse_oversize", "thrust_oversize" };
        foreach (var customAnim in oversizeAnimations)
        {
            int frameCount = _animationHelper.GetFrameCount(customAnim);
            string baseAnim = customAnim.Replace("_oversize", "").Replace("_reverse", "");
            
            var standardLayers = ResolveLayers(definitions, baseAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();
            var customLayers = ResolveLayers(definitions, customAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();

            for (int direction = 0; direction < 4; direction++)
            {
                for (int frame = 0; frame < frameCount; frame++)
                {
                    var destRect = _animationHelper.GetOversizeFrameRect(customAnim, direction, frame);
                    var skDestRect = new SKRect(destRect.X, destRect.Y, destRect.X + destRect.Width, destRect.Y + destRect.Height);
                    var innerDestRect = new SKRect(skDestRect.Left + 64, skDestRect.Top + 64, skDestRect.Left + 128, skDestRect.Top + 128);
                    
                    foreach (var layer in standardLayers)
                    {
                        string variant = ResolveVariant(layer);
                        var candidates = GetCandidatePaths(layer.Layer, baseAnim, variant, gender);
                        SKBitmap? bitmap = null;
                        foreach (var candidate in candidates)
                        {
                            bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
                            if (bitmap != null) break;
                        }
                        if (bitmap != null)
                        {
                            var srcRect = new SKRectI(frame * SheetConstants.FRAME_SIZE, direction * SheetConstants.FRAME_SIZE,
                                                    (frame + 1) * SheetConstants.FRAME_SIZE, (direction + 1) * SheetConstants.FRAME_SIZE);

                            using var paint = new SKPaint();
                            if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                            {
                                if (SKColor.TryParse(layer.TintHex, out var color))
                                    paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
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

                    foreach (var layer in customLayers)
                    {
                        string variant = ResolveVariant(layer);
                        var candidates = GetCandidatePaths(layer.Layer, customAnim, variant, gender);
                        SKBitmap? bitmap = null;
                        foreach (var candidate in candidates)
                        {
                            bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
                            if (bitmap != null) break;
                        }
                        if (bitmap != null)
                        {
                            var srcRect = new SKRectI(frame * SheetConstants.OVERSIZE_FRAME_SIZE, direction * SheetConstants.OVERSIZE_FRAME_SIZE,
                                                    (frame + 1) * SheetConstants.OVERSIZE_FRAME_SIZE, (direction + 1) * SheetConstants.OVERSIZE_FRAME_SIZE);

                            using var paint = new SKPaint();
                            if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                            {
                                if (SKColor.TryParse(layer.TintHex, out var color))
                                    paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
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

        Console.WriteLine($"[Compositor] Finished CompositeFullSheet. Bitmaps Drawn: {totalBitmapsDrawn}");

        foreach (var bmp in bitmapCache.Values)
            bmp?.Dispose();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> CompositePreviewFrame(
        IReadOnlyList<SheetDefinition> definitions,
        CharacterAppearance appearance,
        IFileSystemService fileSystem)
    {
        var previewDef = definitions.FirstOrDefault() ?? new SheetDefinition();
        int row = previewDef.PreviewRow > 0 ? previewDef.PreviewRow : 10;
        int col = previewDef.PreviewColumn;

        var info = new SKImageInfo(SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface == null) return Array.Empty<byte>();
        
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";
        string animation = "walk";
        int direction = 2;
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
        Console.WriteLine($"[Compositor] CompositePreviewFrame: {layers.Count} layers resolved for {animation}");

        var bitmapCache = new Dictionary<string, SKBitmap?>();

        foreach (var layer in layers)
        {
            string variant = ResolveVariant(layer);
            var candidates = GetCandidatePaths(layer.Layer, animation, variant, gender);
            SKBitmap? bitmap = null;
            foreach (var candidate in candidates)
            {
                bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
                if (bitmap != null) break;
            }
            if (bitmap != null)
            {
                var srcRect = new SKRectI(col * SheetConstants.FRAME_SIZE, direction * SheetConstants.FRAME_SIZE,
                                        (col + 1) * SheetConstants.FRAME_SIZE, (direction + 1) * SheetConstants.FRAME_SIZE);
                var destRect = new SKRect(0, 0, SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE);

                using var paint = new SKPaint();
                if (!string.IsNullOrEmpty(layer.TintHex) && layer.TintHex != "#FFFFFF")
                {
                    if (SKColor.TryParse(layer.TintHex, out var color))
                        paint.ColorFilter = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
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

        foreach (var bmp in bitmapCache.Values)
            bmp?.Dispose();

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
            // Compute default variant: layer-level DefaultVariant takes priority, then definition-level Variants[0]
            string? defVariant = def.Variants?.Count > 0 ? def.Variants[0] : null;

            foreach (var kvp in def.Layers)
            {
                var layer = kvp.Value;
                string? variant = layer.DefaultVariant ?? defVariant;

                // Only include if no custom animation, or it matches the current custom animation
                if (string.IsNullOrEmpty(layer.CustomAnimation))
                {
                    // Standard layers don't apply to oversize render passes except when we are rendering the base character
                    if (!animation.EndsWith("_oversize"))
                    {
                        result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, layer.TintHex, variant));
                    }
                }
                else if (layer.CustomAnimation == animation)
                {
                    result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, layer.TintHex, variant));
                }
            }
        }
        return result;
    }

    private async Task<SKBitmap?> LoadBitmap(string fullPath, IFileSystemService fileSystem, Dictionary<string, SKBitmap?> cache)
    {
        if (cache.TryGetValue(fullPath, out var cached)) return cached;
        try
        {
            using var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);
            var bitmap = SKBitmap.Decode(stream);
            cache[fullPath] = bitmap;
            return bitmap;
        }
        catch
        {
            cache[fullPath] = null;
            return null;
        }
    }

    private List<string> GetCandidatePaths(SheetLayer layer, string animation, string variant, string gender)
    {
        var candidates = new List<string>();
        string layerPath = layer.GetPath(gender);
        if (string.IsNullOrEmpty(layerPath)) return candidates;

        if (layerPath.StartsWith("assets/sprites/full/"))
            layerPath = layerPath.Replace("assets/sprites/full/", "");

        string basePath = layerPath.TrimEnd('/');

        // Strategy 1: {animation}/{variant}.png (weapons/gear with variants)
        candidates.Add($"{basePath}/{animation}/{variant}.png");

        // Strategy 2: {animation}.png (body/head/face single files)
        candidates.Add($"{basePath}/{animation}.png");

        // Strategy 3: attack_{animation}/{variant}.png (slash/thrust/shoot)
        if (animation == "slash" || animation == "thrust" || animation == "shoot")
        {
            candidates.Add($"{basePath}/attack_{animation}/{variant}.png");
            candidates.Add($"{basePath}/attack_{animation}.png");
        }

        // Strategy 4: Direct file path
        if (layerPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            candidates.Add(layerPath);

        return candidates;
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

    private string ResolveVariant(ResolvedLayer layer)
    {
        string extracted = ExtractVariant(layer.DefinitionName);
        if (extracted != "default") return extracted;
        return layer.DefaultVariant ?? "default";
    }

    private class ResolvedLayer
    {
        public string DefinitionName { get; }
        public SheetLayer Layer { get; }
        public bool IsFlipped { get; }
        public string TintHex { get; }
        public string? DefaultVariant { get; }
        public int ZPos => Layer.ZPos;

        public ResolvedLayer(string defName, SheetLayer layer, bool isFlipped, string tintHex, string? defaultVariant = null)
        {
            DefinitionName = defName;
            Layer = layer;
            IsFlipped = isFlipped;
            TintHex = tintHex;
            DefaultVariant = defaultVariant;
        }
    }
}
