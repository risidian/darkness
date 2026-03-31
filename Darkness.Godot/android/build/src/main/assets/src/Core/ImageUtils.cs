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
}
