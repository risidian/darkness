using Godot;
using System;
using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Godot.Core;

public static class ImageUtils
{
    public static Texture2D? ByteArrayToTexture(byte[]? data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            var img = new Image();
            var error = img.LoadPngFromBuffer(data);
            if (error != Error.Ok)
            {
                error = img.LoadJpgFromBuffer(data);
            }

            if (error == Error.Ok)
            {
                return ImageTexture.CreateFromImage(img);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to convert byte array to texture: {ex.Message}");
        }

        return null;
    }

    public static SpriteFrames? CreateSpriteFrames(byte[] sheetBytes, int frameWidth, int frameHeight)
    {
        var tex = ByteArrayToTexture(sheetBytes);
        if (tex == null) return null;

        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");

        // Use SheetConstants to map all standard animations
        foreach (var kvp in SheetConstants.AnimationRows)
        {
            string anim = kvp.Key;
            int startRow = kvp.Value;
            int frameCount = SheetConstants.FrameCounts[anim];

            AddLpcRow(frames, tex, $"{anim}_up", startRow + 0, frameCount, frameWidth, frameHeight);
            AddLpcRow(frames, tex, $"{anim}_left", startRow + 1, frameCount, frameWidth, frameHeight);
            AddLpcRow(frames, tex, $"{anim}_down", startRow + 2, frameCount, frameWidth, frameHeight);
            AddLpcRow(frames, tex, $"{anim}_right", startRow + 3, frameCount, frameWidth, frameHeight);

            // Special case for single-row animations
            if (anim == "hurt" || anim == "climb")
            {
                AddLpcRow(frames, tex, anim, startRow, frameCount, frameWidth, frameHeight);
            }
        }

        // Generate idles from frame 0 of each walk direction (after the loop, not inside it)
        AddSingleFrame(frames, tex, $"idle_up", SheetConstants.AnimationRows["walk"] + 0, 0, frameWidth, frameHeight);
        AddSingleFrame(frames, tex, $"idle_left", SheetConstants.AnimationRows["walk"] + 1, 0, frameWidth, frameHeight);
        AddSingleFrame(frames, tex, $"idle_down", SheetConstants.AnimationRows["walk"] + 2, 0, frameWidth, frameHeight);
        AddSingleFrame(frames, tex, $"idle_right", SheetConstants.AnimationRows["walk"] + 3, 0, frameWidth, frameHeight);

        // Handle Oversize Region (fixed at Y offset 3456)
        if (tex.GetSize().Y > SheetConstants.SHEET_HEIGHT)
        {
            int oversizeW = SheetConstants.OVERSIZE_FRAME_SIZE;
            int oversizeH = SheetConstants.OVERSIZE_FRAME_SIZE;
            int yBase = SheetConstants.SHEET_HEIGHT;

            AddOversizeRows(frames, tex, "slash_oversize", yBase + 0, 6, oversizeW, oversizeH);
            AddOversizeRows(frames, tex, "slash_reverse_oversize", yBase + (4 * oversizeH), 6, oversizeW, oversizeH);
            AddOversizeRows(frames, tex, "thrust_oversize", yBase + (8 * oversizeH), 8, oversizeW, oversizeH);
        }

        return frames;
    }

    private static void AddOversizeRows(SpriteFrames frames, Texture2D tex, string animBase, int yStart, int count, int w, int h)
    {
        AddLpcRowByPixel(frames, tex, $"{animBase}_up", yStart + (0 * h), count, w, h);
        AddLpcRowByPixel(frames, tex, $"{animBase}_left", yStart + (1 * h), count, w, h);
        AddLpcRowByPixel(frames, tex, $"{animBase}_down", yStart + (2 * h), count, w, h);
        AddLpcRowByPixel(frames, tex, $"{animBase}_right", yStart + (3 * h), count, w, h);
    }

    private static void AddLpcRow(SpriteFrames frames, Texture2D tex, string animName, int row, int count, int frameW,
        int frameH, double speed = 12.0, bool loop = true)
    {
        AddLpcRowByPixel(frames, tex, animName, row * frameH, count, frameW, frameH, speed, loop);
    }

    private static void AddLpcRowByPixel(SpriteFrames frames, Texture2D tex, string animName, int y, int count, int frameW, int frameH, double speed = 12.0, bool loop = true)
    {
        if (frames.HasAnimation(animName)) frames.RemoveAnimation(animName);
        frames.AddAnimation(animName);
        frames.SetAnimationSpeed(animName, speed);
        frames.SetAnimationLoop(animName, loop);

        for (int col = 0; col < count; col++)
        {
            var atlas = new AtlasTexture
                { Atlas = tex, Region = new Rect2(col * frameW, y, frameW, frameH) };
            frames.AddFrame(animName, atlas);
        }
    }

    public static void AddLpcRowWeapon(SpriteFrames frames, Texture2D tex, string animName, int row, int count, int frameW, int frameH, double speed = 12.0, bool loop = true)
    {
        AddLpcRow(frames, tex, animName, row, count, frameW, frameH, speed, loop);
    }

    private static void AddSingleFrame(SpriteFrames frames, Texture2D tex, string animName, int row, int col,
        int frameW, int frameH)
    {
        if (frames.HasAnimation(animName)) frames.RemoveAnimation(animName);
        frames.AddAnimation(animName);
        frames.SetAnimationLoop(animName, false);
        var atlas = new AtlasTexture { Atlas = tex, Region = new Rect2(col * frameW, row * frameH, frameW, frameH) };
        frames.AddFrame(animName, atlas);
    }

    public static void AddAnimationFromBytes(SpriteFrames frames, string animName, byte[] data, int frameW, int frameH,
        double speed = 10.0, bool loop = true)
    {
        var tex = ByteArrayToTexture(data);
        if (tex == null)
        {
            GD.PrintErr($"[ImageUtils] Failed to create texture for {animName}");
            return;
        }

        if (frames.HasAnimation(animName)) frames.RemoveAnimation(animName);
        frames.AddAnimation(animName);
        frames.SetAnimationSpeed(animName, speed);
        frames.SetAnimationLoop(animName, loop);

        int cols = (int)(tex.GetSize().X / frameW);
        int rows = (int)(tex.GetSize().Y / frameH);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var atlas = new AtlasTexture
                    { Atlas = tex, Region = new Rect2(c * frameW, r * frameH, frameW, frameH) };
                frames.AddFrame(animName, atlas);
            }
        }
    }

    public static void AddStaticAnimationFromBytes(SpriteFrames frames, string animName, byte[] data, int frameW, int frameH)
    {
        AddAnimationFromBytes(frames, animName, data, frameW, frameH, 1.0, false);
    }
}
