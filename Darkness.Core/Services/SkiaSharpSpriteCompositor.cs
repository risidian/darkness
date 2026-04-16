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
        if (surface == null) return Array.Empty<byte>();
        
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        string gender = appearance.Head.ToLower().Contains("female") ? "female" : "male";
        int totalBitmapsDrawn = 0;
        var bitmapCache = new Dictionary<string, SKBitmap?>();

        // 1. Render Standard Sheet (64x64 frames)
        foreach (var animation in SheetConstants.AnimationRows.Keys)
        {
            if (animation.EndsWith("_oversize")) continue;

            int targetFrameCount = _animationHelper.GetFrameCount(animation);
            int startRow = SheetConstants.AnimationRows[animation];

            var layersForAnimation = ResolveLayers(definitions, animation, gender, appearance);
            var sortedLayers = layersForAnimation.OrderBy(l => l.ZPos).ToList();

            if (sortedLayers.Count == 0) continue;

            for (int direction = 0; direction < 4; direction++)
            {
                int targetRow = startRow + direction;
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
                        // DYNAMIC DETECTION
                        int sourceFrameW = bitmap.Width / targetFrameCount;
                        if (sourceFrameW == 0) sourceFrameW = SheetConstants.FRAME_SIZE;
                        
                        int sourceRows = bitmap.Height / sourceFrameW;
                        int sourceRow = (direction < sourceRows) ? direction : 0;

                        totalBitmapsDrawn++;
                        for (int frame = 0; frame < targetFrameCount; frame++)
                        {
                            var targetRect = _animationHelper.GetFrameRect(animation, direction, frame);
                            var skTargetRect = new SKRect(targetRect.X, targetRect.Y, targetRect.X + targetRect.Width, targetRect.Y + targetRect.Height);

                            var srcRect = new SKRectI(frame * sourceFrameW, sourceRow * sourceFrameW,
                                                    (frame + 1) * sourceFrameW, (sourceRow + 1) * sourceFrameW);

                            var destRect = skTargetRect;
                            if (sourceFrameW != SheetConstants.FRAME_SIZE)
                            {
                                float offsetX = (SheetConstants.FRAME_SIZE - sourceFrameW) / 2f;
                                float offsetY = (SheetConstants.FRAME_SIZE - sourceFrameW) / 2f;
                                destRect = new SKRect(skTargetRect.Left + offsetX, skTargetRect.Top + offsetY, 
                                                     skTargetRect.Left + offsetX + sourceFrameW, skTargetRect.Top + offsetY + sourceFrameW);
                            }

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
                }
            }
        }

        // 2. Render Oversize Region (192x192 frames)
        string[] oversizeAnimations = { "slash_oversize", "slash_reverse_oversize", "thrust_oversize" };
        foreach (var customAnim in oversizeAnimations)
        {
            int targetFrameCount = _animationHelper.GetFrameCount(customAnim);
            string baseAnim = customAnim.Replace("_oversize", "").Replace("_reverse", "");
            
            var standardLayers = ResolveLayers(definitions, baseAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();
            var customLayers = ResolveLayers(definitions, customAnim, gender, appearance).OrderBy(l => l.ZPos).ToList();

            for (int direction = 0; direction < 4; direction++)
            {
                for (int frame = 0; frame < targetFrameCount; frame++)
                {
                    var targetRect = _animationHelper.GetOversizeFrameRect(customAnim, direction, frame);
                    var skTargetRect = new SKRect(targetRect.X, targetRect.Y, targetRect.X + targetRect.Width, targetRect.Y + targetRect.Height);
                    
                    // Render 64x64 standard layers centered in 192x192
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
                            int sourceFrameW = bitmap.Width / targetFrameCount;
                            if (sourceFrameW == 0) sourceFrameW = SheetConstants.FRAME_SIZE;
                            int sourceRows = bitmap.Height / sourceFrameW;
                            int sourceRow = (direction < sourceRows) ? direction : 0;

                            var srcRect = new SKRectI(frame * sourceFrameW, sourceRow * sourceFrameW,
                                                    (frame + 1) * sourceFrameW, (sourceRow + 1) * sourceFrameW);

                            var innerDestRect = new SKRect(skTargetRect.Left + 64, skTargetRect.Top + 64, skTargetRect.Left + 128, skTargetRect.Top + 128);

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

                    // Render Custom Oversize Layers (Weapons)
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
                            int sourceFrameW = bitmap.Width / targetFrameCount;
                            int sourceRows = bitmap.Height / sourceFrameW;
                            int sourceRow = (direction < sourceRows) ? direction : 0;

                            var srcRect = new SKRectI(frame * sourceFrameW, sourceRow * sourceFrameW,
                                                    (frame + 1) * sourceFrameW, (sourceRow + 1) * sourceFrameW);

                            float offsetX = (SheetConstants.OVERSIZE_FRAME_SIZE - sourceFrameW) / 2f;
                            float offsetY = (SheetConstants.OVERSIZE_FRAME_SIZE - sourceFrameW) / 2f;
                            var destRect = new SKRect(skTargetRect.Left + offsetX, skTargetRect.Top + offsetY, 
                                                     skTargetRect.Left + offsetX + sourceFrameW, skTargetRect.Top + offsetY + sourceFrameW);

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
        int direction = 2; // Down
        
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
        
        if (appearance.WeaponType?.Contains("Wand") == true)
        {
            var wandDef = definitions.FirstOrDefault(d => d.Slot == "Weapon" && d.Name == appearance.WeaponType);
            if (wandDef != null && wandDef.Layers.TryGetValue("main", out var wandLayer))
            {
                layers.Add(new ResolvedLayer(wandDef.Name, wandLayer, wandDef.IsFlipped, "#FFFFFF", wandLayer.DefaultVariant ?? wandDef.Variants?.FirstOrDefault() ?? "wand")
                {
                    CustomPreviewAnimationOverride = "shoot"
                });
                layers = layers.OrderBy(l => l.ZPos).ToList();
            }
        }

        var bitmapCache = new Dictionary<string, SKBitmap?>();

        foreach (var layer in layers)
        {
            string variant = ResolveVariant(layer);
            string targetAnim = layer.CustomPreviewAnimationOverride ?? animation;
            var candidates = GetCandidatePaths(layer.Layer, targetAnim, variant, gender);
            SKBitmap? bitmap = null;
            foreach (var candidate in candidates)
            {
                bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
                if (bitmap != null) break;
            }
            if (bitmap != null)
            {
                int sourceCol = targetAnim == "shoot" ? 9 : col;
                int sourceFrameW = bitmap.Width / (targetAnim == "shoot" ? 13 : _animationHelper.GetFrameCount(targetAnim));
                if (sourceFrameW == 0) sourceFrameW = SheetConstants.FRAME_SIZE;
                
                var srcRect = new SKRectI(sourceCol * sourceFrameW, direction * sourceFrameW,
                                        (sourceCol + 1) * sourceFrameW, (direction + 1) * sourceFrameW);
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
            string? defVariant = def.Variants?.Count > 0 ? def.Variants[0] : null;

            foreach (var kvp in def.Layers)
            {
                var layer = kvp.Value;
                string? variant = layer.DefaultVariant ?? defVariant;

                if (string.IsNullOrEmpty(layer.CustomAnimation))
                {
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

        candidates.Add($"{basePath}/{animation}/{variant}.png");
        if (variant != "default") candidates.Add($"{basePath}/{variant}.png");
        candidates.Add($"{basePath}/{variant}/{animation}.png");
        candidates.Add($"{basePath}/{animation}.png");

        if (animation == "slash" || animation == "thrust" || animation == "shoot")
        {
            candidates.Add($"{basePath}/attack_{animation}/{variant}.png");
            candidates.Add($"{basePath}/attack_{animation}.png");
        }

        if (layerPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            candidates.Add(layerPath);

        return candidates;
    }

    private string ExtractVariant(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "default";
        int start = displayName.LastIndexOf('(');
        int end = displayName.LastIndexOf(')');
        if (start != -1 && end != -1 && end > start)
        {
            return displayName.Substring(start + 1, end - start - 1).Trim().ToLower();
        }
        return displayName.ToLower();
    }

    private string ResolveVariant(ResolvedLayer layer)
    {
        if (!string.IsNullOrEmpty(layer.DefaultVariant) && layer.DefaultVariant != "default")
            return layer.DefaultVariant;
        string extracted = ExtractVariant(layer.DefinitionName);
        if (extracted != "default") return extracted;
        return "default";
    }

    private class ResolvedLayer
    {
        public string DefinitionName { get; }
        public SheetLayer Layer { get; }
        public bool IsFlipped { get; }
        public string TintHex { get; }
        public string? DefaultVariant { get; }
        public string? CustomPreviewAnimationOverride { get; set; }
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
