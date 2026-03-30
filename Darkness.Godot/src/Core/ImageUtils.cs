using Godot;
using System;

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
}
