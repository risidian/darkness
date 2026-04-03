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

        // LPC Layout (576x256):
        // Row 0: Walk Up (9 frames)
        // Row 1: Walk Left (9 frames)
        // Row 2: Walk Down (9 frames)
        // Row 3: Walk Right (9 frames)
        string[] anims = { "walk_up", "walk_left", "walk_down", "walk_right" };

        for (int row = 0; row < 4; row++)
        {
            string animName = anims[row];
            frames.AddAnimation(animName);
            frames.SetAnimationSpeed(animName, 12.0);
            frames.SetAnimationLoop(animName, true);

            for (int col = 0; col < 9; col++)
            {
                var atlas = new AtlasTexture();
                atlas.Atlas = tex;
                atlas.Region = new Rect2(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                frames.AddFrame(animName, atlas);
            }
        }

        // Add idle animations (frame 0 of each walk cycle)
        string[] idles = { "idle_up", "idle_left", "idle_down", "idle_right" };
        for (int i = 0; i < 4; i++)
        {
            frames.AddAnimation(idles[i]);
            frames.SetAnimationLoop(idles[i], false);
            var atlas = new AtlasTexture();
            atlas.Atlas = tex;
            atlas.Region = new Rect2(0, i * frameHeight, frameWidth, frameHeight);
            frames.AddFrame(idles[i], atlas);
        }

        return frames;
    }

    public static void AddAnimationFromBytes(SpriteFrames frames, string animName, byte[] data, int frameW, int frameH, double speed = 10.0, bool loop = true)
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
        GD.Print($"[ImageUtils] Slicing {animName}: {tex.GetSize().X}x{tex.GetSize().Y} into {cols}x{rows} frames of {frameW}x{frameH}");

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var atlas = new AtlasTexture { Atlas = tex, Region = new Rect2(c * frameW, r * frameH, frameW, frameH) };
                frames.AddFrame(animName, atlas);
            }
        }
        GD.Print($"[ImageUtils] Added {frames.GetFrameCount(animName)} frames to {animName}");
    }

    private static int row; // This was a typo in my thought process, corrected above.

    public static void AddStaticAnimationFromBytes(SpriteFrames frames, string animName, byte[] data, int frameW, int frameH)
    {
        AddAnimationFromBytes(frames, animName, data, frameW, frameH, 1.0, false);
    }
}
