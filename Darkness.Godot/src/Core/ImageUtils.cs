using Godot;
using System;
using System.Collections.Generic;

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

        float height = tex.GetSize().Y;
        bool isFullLpc = height >= 1344;

        if (isFullLpc)
        {
            // Spellcast (7 frames)
            AddLpcRow(frames, tex, "spellcast_up", 0, 7, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "spellcast_left", 1, 7, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "spellcast_down", 2, 7, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "spellcast_right", 3, 7, frameWidth, frameHeight);

            // Thrust (8 frames)
            AddLpcRow(frames, tex, "thrust_up", 4, 8, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "thrust_left", 5, 8, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "thrust_down", 6, 8, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "thrust_right", 7, 8, frameWidth, frameHeight);

            // Walk (9 frames)
            AddLpcRow(frames, tex, "walk_up", 8, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_left", 9, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_down", 10, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_right", 11, 9, frameWidth, frameHeight);

            // Slash (6 frames)
            AddLpcRow(frames, tex, "slash_up", 12, 6, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "slash_left", 13, 6, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "slash_down", 14, 6, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "slash_right", 15, 6, frameWidth, frameHeight);

            // Shoot (13 frames)
            AddLpcRow(frames, tex, "shoot_up", 16, 13, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "shoot_left", 17, 13, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "shoot_down", 18, 13, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "shoot_right", 19, 13, frameWidth, frameHeight);

            // Hurt (6 frames)
            AddLpcRow(frames, tex, "hurt", 20, 6, frameWidth, frameHeight, 12.0, false);

            // Idles from Walk cycle
            AddSingleFrame(frames, tex, "idle_up", 8, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_left", 9, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_down", 10, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_right", 11, 0, frameWidth, frameHeight);
        }
        else
        {
            // Cropped LPC Layout (usually just walk)
            AddLpcRow(frames, tex, "walk_up", 0, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_left", 1, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_down", 2, 9, frameWidth, frameHeight);
            AddLpcRow(frames, tex, "walk_right", 3, 9, frameWidth, frameHeight);

            AddSingleFrame(frames, tex, "idle_up", 0, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_left", 1, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_down", 2, 0, frameWidth, frameHeight);
            AddSingleFrame(frames, tex, "idle_right", 3, 0, frameWidth, frameHeight);
        }

        return frames;
    }

    private static void AddLpcRow(SpriteFrames frames, Texture2D tex, string animName, int row, int count, int frameW,
        int frameH, double speed = 12.0, bool loop = true)
    {
        if (frames.HasAnimation(animName)) frames.RemoveAnimation(animName);
        frames.AddAnimation(animName);
        frames.SetAnimationSpeed(animName, speed);
        frames.SetAnimationLoop(animName, loop);

        for (int col = 0; col < count; col++)
        {
            var atlas = new AtlasTexture
                { Atlas = tex, Region = new Rect2(col * frameW, row * frameH, frameW, frameH) };
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
        GD.Print(
            $"[ImageUtils] Slicing {animName}: {tex.GetSize().X}x{tex.GetSize().Y} into {cols}x{rows} frames of {frameW}x{frameH}");

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var atlas = new AtlasTexture
                    { Atlas = tex, Region = new Rect2(c * frameW, r * frameH, frameW, frameH) };
                frames.AddFrame(animName, atlas);
            }
        }
        GD.Print($"[ImageUtils] Added {frames.GetFrameCount(animName)} frames to {animName}");
    }

    public static void AddStaticAnimationFromBytes(SpriteFrames frames, string animName, byte[] data, int frameW, int frameH)
    {
        AddAnimationFromBytes(frames, animName, data, frameW, frameH, 1.0, false);
    }
}